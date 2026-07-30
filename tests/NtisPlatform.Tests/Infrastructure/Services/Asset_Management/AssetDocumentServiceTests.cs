using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services.Asset_Management;

public class AssetDocumentServiceTests
{
    private static ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        
        // Seed default document definition
        context.AssetDocumentDefinitions.Add(new AssetDocumentDefinitionEntity
        {
            Id = 2,
            DocumentCode = "DOC_CODE",
            DocumentName = "Document Name",
            IsActive = true,
            CreatedDate = DateTime.Now
        });
        context.SaveChanges();

        return context;
    }

    private static AssetDocumentEntity CreateTestDocument(int assetId = 10, int docDefId = 2)
    {
        var doc = AssetDocumentEntity.Create(assetId, docDefId);
        doc.CreatedDate = DateTime.Now;
        return doc;
    }

    [Fact]
    public async Task CreateAsync_ValidParameters_CreatesDocumentRecord()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        context.AssetMaster.Add(new AssetMasterEntity { Id = 10, CreatedDate = DateTime.Now });
        await context.SaveChangesAsync();

        var service = new AssetDocumentService(context, uow.Object);

        var docId = await service.CreateAsync(10, 2, 1, "Front doc", 42);

        Assert.True(docId > 0);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AssetNotFound_ThrowsArgumentException()
    {
        var context = GetInMemoryDbContext();
        var service = new AssetDocumentService(context, Mock.Of<IUnitOfWork>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(999, 2, 1, "test", 42));
    }

    [Fact]
    public async Task CreateAsync_DocumentDefinitionNotFound_ThrowsArgumentException()
    {
        var context = GetInMemoryDbContext();
        context.AssetMaster.Add(new AssetMasterEntity { Id = 10, CreatedDate = DateTime.Now });
        await context.SaveChangesAsync();

        var service = new AssetDocumentService(context, Mock.Of<IUnitOfWork>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(10, 999, 1, "test", 42));
    }

    [Fact]
    public async Task UpdateDocumentBindingAsync_ValidDocument_LinksDocument()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var doc = CreateTestDocument();
        context.AssetDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new AssetDocumentService(context, uow.Object);

        await service.UpdateDocumentBindingAsync(doc.Id, 100, 42);

        Assert.Equal(100, doc.DocumentBindingId);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDocumentBindingAsync_NotFound_ThrowsKeyNotFoundException()
    {
        var context = GetInMemoryDbContext();
        var service = new AssetDocumentService(context, Mock.Of<IUnitOfWork>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateDocumentBindingAsync(999, 100, 42));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDocument_WhenActive()
    {
        var context = GetInMemoryDbContext();
        var doc = CreateTestDocument();
        context.AssetDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new AssetDocumentService(context, Mock.Of<IUnitOfWork>());

        var result = await service.GetByIdAsync(doc.Id);

        Assert.NotNull(result);
        Assert.Equal(doc.Id, result!.Id);
    }

    [Fact]
    public async Task GetLatestByAssetIdAsync_ReturnsActiveLatestDocuments()
    {
        var context = GetInMemoryDbContext();
        var doc = CreateTestDocument();
        context.AssetDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new AssetDocumentService(context, Mock.Of<IUnitOfWork>());

        var result = await service.GetLatestByAssetIdAsync(10);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetLatestByAssetIdIncludingInactiveAsync_ReturnsAllLatestDocuments()
    {
        var context = GetInMemoryDbContext();
        var doc = CreateTestDocument();
        context.AssetDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new AssetDocumentService(context, Mock.Of<IUnitOfWork>());

        var result = await service.GetLatestByAssetIdIncludingInactiveAsync(10);

        Assert.Single(result);
    }

    [Fact]
    public async Task MarkAsSupersededAsync_Valid_MarksSuperseded()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var doc = CreateTestDocument();
        context.AssetDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new AssetDocumentService(context, uow.Object);

        await service.MarkAsSupersededAsync(doc.Id, 42);

        Assert.False(doc.IsLatest);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsSupersededAsync_AlreadySuperseded_ThrowsArgumentException()
    {
        var context = GetInMemoryDbContext();
        var doc = CreateTestDocument();
        doc.MarkAsSuperseded();
        context.AssetDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new AssetDocumentService(context, Mock.Of<IUnitOfWork>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.MarkAsSupersededAsync(doc.Id, 42));
    }

    [Fact]
    public async Task UpdateAsync_Valid_UpdatesDisplayOrderAndRemarks()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var doc = CreateTestDocument();
        context.AssetDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new AssetDocumentService(context, uow.Object);

        await service.UpdateAsync(doc.Id, 5, "Updated remarks", 42);

        Assert.Equal(5, doc.DisplayOrder);
        Assert.Equal("Updated remarks", doc.Remarks);
    }

    [Fact]
    public async Task ToggleEnabledAsync_EnablesAndDisablesDocument()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var doc = CreateTestDocument();
        context.AssetDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new AssetDocumentService(context, uow.Object);

        // Disable
        await service.ToggleEnabledAsync(doc.Id, false, 42);
        Assert.True(doc.MarkedForDeletion);

        // Enable (Restore)
        await service.ToggleEnabledAsync(doc.Id, true, 42);
        Assert.False(doc.MarkedForDeletion);
        Assert.True(doc.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_ValidDocument_MarksForDeletion()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var doc = CreateTestDocument();
        context.AssetDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new AssetDocumentService(context, uow.Object);

        await service.DeleteAsync(doc.Id, 42);

        Assert.True(doc.MarkedForDeletion);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SupersedesExistingLatestDocument()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        context.AssetMaster.Add(new AssetMasterEntity { Id = 10, CreatedDate = DateTime.Now });
        await context.SaveChangesAsync();

        var oldDoc = CreateTestDocument(10, 2);
        context.AssetDocuments.Add(oldDoc);
        await context.SaveChangesAsync();

        var service = new AssetDocumentService(context, uow.Object);

        var newDocId = await service.CreateAsync(10, 2, 1, "New doc", 42);

        var retrievedOld = await context.AssetDocuments.FindAsync(oldDoc.Id);
        var retrievedNew = await context.AssetDocuments.FindAsync(newDocId);

        Assert.NotNull(retrievedOld);
        Assert.NotNull(retrievedNew);
        Assert.False(retrievedOld.IsLatest);
        Assert.True(retrievedNew.IsLatest);
    }
}
