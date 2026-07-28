using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.Asset_Management;

public class AssetPhotoApplicationServiceTests
{
    private static AssetPhotoApplicationService CreateService(
        out Mock<IAssetPhotoService> photoService,
        out Mock<IUnitOfWork> unitOfWork,
        out Mock<IRepository<AssetPhotoTypeEntity, int>> photoTypeRepo,
        out Mock<IRepository<AssetMasterEntity, int>> assetMasterRepo)
    {
        photoService = new Mock<IAssetPhotoService>();
        unitOfWork = new Mock<IUnitOfWork>();
        photoTypeRepo = new Mock<IRepository<AssetPhotoTypeEntity, int>>();
        assetMasterRepo = new Mock<IRepository<AssetMasterEntity, int>>();
        var logger = new Mock<ILogger<AssetPhotoApplicationService>>();

        return new AssetPhotoApplicationService(
            photoService.Object,
            unitOfWork.Object,
            photoTypeRepo.Object,
            assetMasterRepo.Object,
            logger.Object);
    }

    [Fact]
    public async Task GetPhotosByAssetAsync_Throws_WhenAssetIdInvalid()
    {
        var service = CreateService(out _, out _, out _, out _);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetPhotosByAssetAsync(0));
    }

    [Fact]
    public async Task GetPhotosByAssetAsync_ReturnsMappedDtos()
    {
        var service = CreateService(out var photoService, out _, out _, out _);
        var entity = AssetPhotoEntity.Create(10, 1);

        photoService.Setup(s => s.GetLatestByAssetIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetPhotoEntity> { entity });

        var result = await service.GetPhotosByAssetAsync(10);

        Assert.Single(result);
        Assert.Equal(10, result[0].AssetId);
    }

    [Fact]
    public async Task GetGroupedPhotosByAssetAsync_Throws_WhenAssetNotFound()
    {
        var service = CreateService(out _, out _, out _, out var assetRepo);
        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetMasterEntity>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetGroupedPhotosByAssetAsync(10));
    }

    [Fact]
    public async Task GetGroupedPhotosByAssetAsync_ReturnsGroupedGallery_WhenAssetExists()
    {
        var service = CreateService(out var photoService, out _, out var photoTypeRepo, out var assetRepo);
        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetMasterEntity> { new AssetMasterEntity() });

        photoTypeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetPhotoTypeEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetPhotoTypeEntity>
            {
                new AssetPhotoTypeEntity { Id = 1, PhotoTypeCode = "FRONT", PhotoTypeName = "Front Elevation" }
            });

        photoService.Setup(s => s.GetLatestByAssetIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetPhotoEntity>());

        var result = await service.GetGroupedPhotosByAssetAsync(10);

        Assert.Equal(10, result.AssetId);
        Assert.Single(result.PhotoTypes);
    }

    [Fact]
    public async Task GetPhotoTypesWithStatusAsync_Throws_WhenAssetNotFound()
    {
        var service = CreateService(out _, out _, out _, out var assetRepo);
        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetMasterEntity>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetPhotoTypesWithStatusAsync(10));
    }

    [Fact]
    public async Task GetPhotoTypesWithStatusAsync_ReturnsTypesWithStatusAndDocumentData()
    {
        var service = CreateService(out var photoService, out _, out var photoTypeRepo, out var assetRepo);

        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetMasterEntity> { new AssetMasterEntity() });

        photoTypeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetPhotoTypeEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetPhotoTypeEntity>
            {
                new AssetPhotoTypeEntity { Id = 1, PhotoTypeCode = "FRONT", PhotoTypeName = "Front Elevation" }
            });

        var docGuid = Guid.NewGuid();
        var photo = AssetPhotoEntity.CreateWithDocument(10, 1, 100);
        var binding = new DocumentBindingEntity
        {
            Id = 100,
            Document = new DocumentEntity
            {
                DocumentGuid = docGuid,
                OriginalFileName = "photo.jpg",
                MimeType = "image/jpeg",
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        typeof(AssetPhotoEntity).GetProperty(nameof(AssetPhotoEntity.DocumentBinding))!
            .SetValue(photo, binding);

        photoService.Setup(s => s.GetLatestByAssetIdIncludingInactiveAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetPhotoEntity> { photo });

        var result = await service.GetPhotoTypesWithStatusAsync(10);

        Assert.Single(result);
        Assert.True(result[0].HasPhoto);
        Assert.Equal(docGuid, result[0].DocumentGuid);
        Assert.Equal("photo.jpg", result[0].FileName);
        Assert.Equal("image/jpeg", result[0].MimeType);
    }

    [Fact]
    public async Task BulkSaveAllAsync_ExecutesBulkSave_AndDisablesExistingPhoto()
    {
        var service = CreateService(out var photoService, out var unitOfWork, out var photoTypeRepo, out var assetRepo);

        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetMasterEntity> { new AssetMasterEntity() });

        photoTypeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetPhotoTypeEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetPhotoTypeEntity>());

        var existingPhoto = AssetPhotoEntity.Create(10, 1);
        photoService.Setup(s => s.GetLatestByAssetIdIncludingInactiveAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetPhotoEntity> { existingPhoto });

        var bulkDto = new AssetPhotoBulkSaveDto
        {
            AssetId = 10,
            Photos = new List<AssetPhotoItemDto>
            {
                new AssetPhotoItemDto
                {
                    PhotoTypeId = 1,
                    IsEnabled = false
                }
            }
        };

        var response = await service.BulkSaveAllAsync(bulkDto, 42);

        Assert.Equal(1, response.DisabledCount);
        photoService.Verify(s => s.ToggleEnabledAsync(existingPhoto.Id, false, 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkSaveAllAsync_CapturesItemException_InErrorsList()
    {
        var service = CreateService(out var photoService, out var unitOfWork, out var photoTypeRepo, out var assetRepo);

        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetMasterEntity> { new AssetMasterEntity() });

        photoTypeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetPhotoTypeEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetPhotoTypeEntity>());

        photoService.Setup(s => s.GetLatestByAssetIdIncludingInactiveAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetPhotoEntity>());

        photoService.Setup(s => s.CreateAsync(10, 1, It.IsAny<int?>(), It.IsAny<string?>(), 42, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var bulkDto = new AssetPhotoBulkSaveDto
        {
            AssetId = 10,
            Photos = new List<AssetPhotoItemDto>
            {
                new AssetPhotoItemDto
                {
                    PhotoTypeId = 1,
                    IsEnabled = true
                }
            }
        };

        var response = await service.BulkSaveAllAsync(bulkDto, 42);

        Assert.Single(response.Errors);
        Assert.Contains("DB error", response.Errors[0]);
    }

    [Fact]
    public async Task BulkSaveAllAsync_RollsBackTransaction_OnUnhandledException()
    {
        var service = CreateService(out var photoService, out var unitOfWork, out var photoTypeRepo, out var assetRepo);

        photoService.Setup(s => s.GetLatestByAssetIdIncludingInactiveAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetPhotoEntity>());

        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Fatal error"));

        var bulkDto = new AssetPhotoBulkSaveDto { AssetId = 10 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BulkSaveAllAsync(bulkDto, 42));

        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
