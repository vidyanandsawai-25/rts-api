using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services.Asset_Management;

public class InventoryDocumentServiceTests
{
    private static ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        // Seed default inventory document type
        context.InventoryDocumentTypes.Add(new InventoryDocumentTypeEntity
        {
            Id = 3,
            DocumentTypeCode = "INV_DOC_TYPE",
            DocumentTypeName = "Invoice Document",
            IsActive = true,
            CreatedDate = DateTime.Now
        });
        context.SaveChanges();

        return context;
    }

    private static InventoryDocumentEntity CreateTestDocument(int inventoryBatchId = 10, int docTypeId = 3)
    {
        var doc = InventoryDocumentEntity.Create(inventoryBatchId, docTypeId, displayOrder: 1, remarks: "Initial remark");
        doc.CreatedDate = DateTime.Now;
        return doc;
    }

    [Fact]
    public async Task CreateAsync_ValidParameters_CreatesDocumentRecord()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var service = new InventoryDocumentService(context, uow.Object);

        var docId = await service.CreateAsync(
            inventoryBatchId: 10,
            documentTypeId: 3,
            displayOrder: 1,
            remarks: "Test doc",
            createdBy: 42);

        Assert.True(docId > 0);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DocumentTypeNotFound_ThrowsArgumentException()
    {
        var context = GetInMemoryDbContext();
        var service = new InventoryDocumentService(context, Mock.Of<IUnitOfWork>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(10, documentTypeId: 999, displayOrder: 1, remarks: "test", createdBy: 42));
    }

    [Fact]
    public async Task UpdateDocumentBindingAsync_ValidRecord_LinksBinding()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var doc = CreateTestDocument();
        context.InventoryDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new InventoryDocumentService(context, uow.Object);

        await service.UpdateDocumentBindingAsync(doc.Id, documentBindingId: 100, updatedBy: 42);

        Assert.Equal(100, doc.DocumentBindingId);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDocumentBindingAsync_RecordNotFound_ThrowsArgumentException()
    {
        var context = GetInMemoryDbContext();
        var service = new InventoryDocumentService(context, Mock.Of<IUnitOfWork>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateDocumentBindingAsync(id: 999, documentBindingId: 100, updatedBy: 42));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDocument_WhenActiveAndNotDeleted()
    {
        var context = GetInMemoryDbContext();
        var doc = CreateTestDocument();
        context.InventoryDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new InventoryDocumentService(context, Mock.Of<IUnitOfWork>());

        var result = await service.GetByIdAsync(doc.Id);

        Assert.NotNull(result);
        Assert.Equal(doc.Id, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenRecordDeletedOrInactive()
    {
        var context = GetInMemoryDbContext();
        var doc = CreateTestDocument();
        doc.MarkForDeletion();
        context.InventoryDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new InventoryDocumentService(context, Mock.Of<IUnitOfWork>());

        var result = await service.GetByIdAsync(doc.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestByInventoryBatchIdAsync_ReturnsActiveLatestDocuments()
    {
        var context = GetInMemoryDbContext();
        var doc = CreateTestDocument(inventoryBatchId: 10, docTypeId: 3);
        context.InventoryDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new InventoryDocumentService(context, Mock.Of<IUnitOfWork>());

        var result = await service.GetLatestByInventoryBatchIdAsync(10);

        Assert.Single(result);
        Assert.Equal(10, result[0].InventoryBatchId);
    }

    [Fact]
    public async Task MarkAsSupersededAsync_ValidRecord_MarksAsSuperseded()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var doc = CreateTestDocument();
        context.InventoryDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new InventoryDocumentService(context, uow.Object);

        await service.MarkAsSupersededAsync(doc.Id, updatedBy: 42);

        Assert.False(doc.IsLatest);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsSupersededAsync_RecordNotFound_ThrowsArgumentException()
    {
        var context = GetInMemoryDbContext();
        var service = new InventoryDocumentService(context, Mock.Of<IUnitOfWork>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.MarkAsSupersededAsync(id: 999, updatedBy: 42));
    }

    [Fact]
    public async Task MarkAsSupersededAsync_AlreadySuperseded_ThrowsArgumentException()
    {
        var context = GetInMemoryDbContext();
        var doc = CreateTestDocument();
        doc.MarkAsSuperseded();
        context.InventoryDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new InventoryDocumentService(context, Mock.Of<IUnitOfWork>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.MarkAsSupersededAsync(doc.Id, updatedBy: 42));
    }

    [Fact]
    public async Task DeleteAsync_ValidRecord_MarksForDeletion()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var doc = CreateTestDocument();
        context.InventoryDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new InventoryDocumentService(context, uow.Object);

        await service.DeleteAsync(doc.Id, deletedBy: 42);

        Assert.True(doc.MarkedForDeletion);
        Assert.False(doc.IsActive);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SupersedesExistingLatestRecord_WhenFound()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var existingDoc = CreateTestDocument(inventoryBatchId: 10, docTypeId: 3);
        context.InventoryDocuments.Add(existingDoc);
        await context.SaveChangesAsync();

        var service = new InventoryDocumentService(context, uow.Object);

        var newDocId = await service.CreateAsync(
            inventoryBatchId: 10,
            documentTypeId: 3,
            displayOrder: 2,
            remarks: "New version",
            createdBy: 42);

        Assert.True(newDocId > 0);
        Assert.False(existingDoc.IsLatest);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ValidRecord_UpdatesDisplayOrderAndRemarks()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var doc = CreateTestDocument();
        context.InventoryDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new InventoryDocumentService(context, uow.Object);

        await service.UpdateAsync(doc.Id, displayOrder: 9, remarks: "Updated remarks", updatedBy: 42);

        Assert.Equal(9, doc.DisplayOrder);
        Assert.Equal("Updated remarks", doc.Remarks);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_RecordNotFound_ThrowsKeyNotFoundException()
    {
        var context = GetInMemoryDbContext();
        var service = new InventoryDocumentService(context, Mock.Of<IUnitOfWork>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateAsync(id: 999, displayOrder: 1, remarks: "test", updatedBy: 42));
    }

    [Fact]
    public async Task ToggleEnabledAsync_EnablesDeletedRecord_RestoresFromDeletion()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var doc = CreateTestDocument();
        doc.MarkForDeletion();
        context.InventoryDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new InventoryDocumentService(context, uow.Object);

        await service.ToggleEnabledAsync(doc.Id, isEnabled: true, updatedBy: 42);

        Assert.False(doc.MarkedForDeletion);
        Assert.True(doc.IsActive);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleEnabledAsync_EnablesActiveRecord_SetsIsActiveTrue()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var doc = CreateTestDocument();
        context.InventoryDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new InventoryDocumentService(context, uow.Object);

        await service.ToggleEnabledAsync(doc.Id, isEnabled: true, updatedBy: 42);

        Assert.False(doc.MarkedForDeletion);
        Assert.True(doc.IsActive);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleEnabledAsync_DisablesActiveRecord_MarksForDeletion()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var doc = CreateTestDocument();
        context.InventoryDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new InventoryDocumentService(context, uow.Object);

        await service.ToggleEnabledAsync(doc.Id, isEnabled: false, updatedBy: 42);

        Assert.True(doc.MarkedForDeletion);
        Assert.False(doc.IsActive);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleEnabledAsync_DisablesAlreadyDeletedRecord_KeepsMarkedForDeletion()
    {
        var context = GetInMemoryDbContext();
        var uow = new Mock<IUnitOfWork>();

        var doc = CreateTestDocument();
        doc.MarkForDeletion();
        context.InventoryDocuments.Add(doc);
        await context.SaveChangesAsync();

        var service = new InventoryDocumentService(context, uow.Object);

        await service.ToggleEnabledAsync(doc.Id, isEnabled: false, updatedBy: 42);

        Assert.True(doc.MarkedForDeletion);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleEnabledAsync_RecordNotFound_ThrowsKeyNotFoundException()
    {
        var context = GetInMemoryDbContext();
        var service = new InventoryDocumentService(context, Mock.Of<IUnitOfWork>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ToggleEnabledAsync(id: 999, isEnabled: true, updatedBy: 42));
    }
}
