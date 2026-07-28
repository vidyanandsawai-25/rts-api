using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Infrastructure.Services.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services.Asset_Management;

public class AssetPhotoServiceTests
{
    private static ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        
        // Seed default photo type
        context.AssetPhotoTypeMaster.Add(new AssetPhotoTypeEntity
        {
            Id = 2,
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front Elevation",
            IsActive = true,
            CreatedDate = DateTime.Now
        });
        context.SaveChanges();

        return context;
    }

    private static AssetPhotoEntity CreateTestPhoto(int assetId = 10, int photoTypeId = 2)
    {
        var photo = AssetPhotoEntity.Create(assetId, photoTypeId);
        photo.CreatedDate = DateTime.Now;
        return photo;
    }

    [Fact]
    public async Task CreateAsync_ValidParameters_CreatesPhotoRecord()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        context.AssetMaster.Add(new AssetMasterEntity { Id = 10, CreatedDate = DateTime.Now });
        await context.SaveChangesAsync();

        var service = new AssetPhotoService(context, uow.Object);

        var photoId = await service.CreateAsync(10, 2, 1, "Front photo", 42);

        Assert.True(photoId > 0);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AssetNotFound_ThrowsArgumentException()
    {
        var context = GetInMemoryDbContext();
        var service = new AssetPhotoService(context, Mock.Of<IUnitOfWork>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(999, 2, 1, "test", 42));
    }

    [Fact]
    public async Task CreateAsync_PhotoTypeNotFound_ThrowsArgumentException()
    {
        var context = GetInMemoryDbContext();
        context.AssetMaster.Add(new AssetMasterEntity { Id = 10, CreatedDate = DateTime.Now });
        await context.SaveChangesAsync();

        var service = new AssetPhotoService(context, Mock.Of<IUnitOfWork>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(10, 999, 1, "test", 42));
    }

    [Fact]
    public async Task UpdateDocumentBindingAsync_ValidPhoto_LinksDocument()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var photo = CreateTestPhoto();
        context.AssetPhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = new AssetPhotoService(context, uow.Object);

        await service.UpdateDocumentBindingAsync(photo.Id, 100, 42);

        Assert.Equal(100, photo.DocumentBindingId);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDocumentBindingAsync_NotFound_ThrowsAssetPhotoNotFoundException()
    {
        var context = GetInMemoryDbContext();
        var service = new AssetPhotoService(context, Mock.Of<IUnitOfWork>());

        await Assert.ThrowsAsync<AssetPhotoNotFoundException>(() => service.UpdateDocumentBindingAsync(999, 100, 42));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPhoto_WhenActive()
    {
        var context = GetInMemoryDbContext();
        var photo = CreateTestPhoto();
        context.AssetPhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = new AssetPhotoService(context, Mock.Of<IUnitOfWork>());

        var result = await service.GetByIdAsync(photo.Id);

        Assert.NotNull(result);
        Assert.Equal(photo.Id, result!.Id);
    }

    [Fact]
    public async Task GetLatestByAssetIdAsync_ReturnsActiveLatestPhotos()
    {
        var context = GetInMemoryDbContext();
        var photo = CreateTestPhoto();
        context.AssetPhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = new AssetPhotoService(context, Mock.Of<IUnitOfWork>());

        var result = await service.GetLatestByAssetIdAsync(10);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetLatestByAssetIdIncludingInactiveAsync_ReturnsAllLatestPhotos()
    {
        var context = GetInMemoryDbContext();
        var photo = CreateTestPhoto();
        context.AssetPhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = new AssetPhotoService(context, Mock.Of<IUnitOfWork>());

        var result = await service.GetLatestByAssetIdIncludingInactiveAsync(10);

        Assert.Single(result);
    }

    [Fact]
    public async Task MarkAsSupersededAsync_Valid_MarksSuperseded()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var photo = CreateTestPhoto();
        context.AssetPhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = new AssetPhotoService(context, uow.Object);

        await service.MarkAsSupersededAsync(photo.Id, 42);

        Assert.False(photo.IsLatest);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsSupersededAsync_AlreadySuperseded_ThrowsArgumentException()
    {
        var context = GetInMemoryDbContext();
        var photo = CreateTestPhoto();
        photo.MarkAsSuperseded();
        context.AssetPhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = new AssetPhotoService(context, Mock.Of<IUnitOfWork>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.MarkAsSupersededAsync(photo.Id, 42));
    }

    [Fact]
    public async Task UpdateAsync_Valid_UpdatesDisplayOrderAndRemarks()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var photo = CreateTestPhoto();
        context.AssetPhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = new AssetPhotoService(context, uow.Object);

        await service.UpdateAsync(photo.Id, 5, "Updated remarks", 42);

        Assert.Equal(5, photo.DisplayOrder);
        Assert.Equal("Updated remarks", photo.Remarks);
    }

    [Fact]
    public async Task ToggleEnabledAsync_EnablesAndDisablesPhoto()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var photo = CreateTestPhoto();
        context.AssetPhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = new AssetPhotoService(context, uow.Object);

        // Disable
        await service.ToggleEnabledAsync(photo.Id, false, 42);
        Assert.True(photo.MarkedForDeletion);

        // Enable (Restore)
        await service.ToggleEnabledAsync(photo.Id, true, 42);
        Assert.False(photo.MarkedForDeletion);
        Assert.True(photo.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_ValidPhoto_MarksForDeletion()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var photo = CreateTestPhoto();
        context.AssetPhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = new AssetPhotoService(context, uow.Object);

        await service.DeleteAsync(photo.Id, 42);

        Assert.True(photo.MarkedForDeletion);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
