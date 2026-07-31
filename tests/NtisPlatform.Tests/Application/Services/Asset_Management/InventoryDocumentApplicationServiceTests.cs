using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.Asset_Management;

public class InventoryDocumentApplicationServiceTests
{
    private static InventoryDocumentApplicationService CreateService(
        out Mock<IInventoryDocumentService> docService)
    {
        docService = new Mock<IInventoryDocumentService>();
        var logger = new Mock<ILogger<InventoryDocumentApplicationService>>();

        return new InventoryDocumentApplicationService(docService.Object, logger.Object);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetDocumentsByInventoryBatchAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDocumentsByInventoryBatchAsync_Throws_WhenBatchIdIsZero()
    {
        var service = CreateService(out _);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetDocumentsByInventoryBatchAsync(0));
    }

    [Fact]
    public async Task GetDocumentsByInventoryBatchAsync_Throws_WhenBatchIdIsNegative()
    {
        var service = CreateService(out _);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetDocumentsByInventoryBatchAsync(-1));
    }

    [Fact]
    public async Task GetDocumentsByInventoryBatchAsync_ReturnsMappedDtos()
    {
        var service = CreateService(out var docService);

        var entity = InventoryDocumentEntity.Create(10, 2, displayOrder: 1, remarks: "Invoice");
        typeof(InventoryDocumentEntity)
            .GetProperty(nameof(InventoryDocumentEntity.DocumentType))!
            .SetValue(entity, new InventoryDocumentTypeEntity
            {
                Id = 2,
                DocumentTypeCode = "INV_INVOICE",
                DocumentTypeName = "Invoice"
            });

        docService.Setup(s => s.GetLatestByInventoryBatchIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryDocumentEntity> { entity });

        var result = await service.GetDocumentsByInventoryBatchAsync(10);

        Assert.Single(result);
        Assert.Equal(10, result[0].InventoryBatchId);
        Assert.Equal(2, result[0].DocumentTypeId);
        Assert.Equal("INV_INVOICE", result[0].DocumentTypeCode);
        Assert.Equal("Invoice", result[0].DocumentTypeName);
        Assert.Equal(1, result[0].DisplayOrder);
        Assert.Equal("Invoice", result[0].Remarks);
    }

    [Fact]
    public async Task GetDocumentsByInventoryBatchAsync_ReturnsEmptyList_WhenNoneFound()
    {
        var service = CreateService(out var docService);

        docService.Setup(s => s.GetLatestByInventoryBatchIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryDocumentEntity>());

        var result = await service.GetDocumentsByInventoryBatchAsync(99);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDocumentsByInventoryBatchAsync_MapsDocumentGuidAndFileName_WhenBindingHasActiveDoc()
    {
        var service = CreateService(out var docService);

        var docGuid = Guid.NewGuid();
        var entity = InventoryDocumentEntity.CreateWithDocument(10, 2, documentBindingId: 50);
        var binding = new DocumentBindingEntity
        {
            Id = 50,
            Document = new DocumentEntity
            {
                DocumentGuid = docGuid,
                OriginalFileName = "invoice.pdf",
                MimeType = "application/pdf",
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        typeof(InventoryDocumentEntity)
            .GetProperty(nameof(InventoryDocumentEntity.DocumentBinding))!
            .SetValue(entity, binding);

        docService.Setup(s => s.GetLatestByInventoryBatchIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryDocumentEntity> { entity });

        var result = await service.GetDocumentsByInventoryBatchAsync(10);

        Assert.Single(result);
        Assert.Equal(docGuid, result[0].DocumentGuid);
        Assert.Equal("invoice.pdf", result[0].FileName);
        Assert.Equal("application/pdf", result[0].MimeType);
        Assert.Equal(50, result[0].DocumentBindingId);
    }

    [Fact]
    public async Task GetDocumentsByInventoryBatchAsync_ReturnsNullDocumentFields_WhenDocIsInactive()
    {
        var service = CreateService(out var docService);

        var entity = InventoryDocumentEntity.CreateWithDocument(10, 2, documentBindingId: 50);
        var binding = new DocumentBindingEntity
        {
            Id = 50,
            Document = new DocumentEntity
            {
                DocumentGuid = Guid.NewGuid(),
                OriginalFileName = "old.pdf",
                MimeType = "application/pdf",
                IsActive = false,
                MarkedForDeletion = false
            }
        };

        typeof(InventoryDocumentEntity)
            .GetProperty(nameof(InventoryDocumentEntity.DocumentBinding))!
            .SetValue(entity, binding);

        docService.Setup(s => s.GetLatestByInventoryBatchIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryDocumentEntity> { entity });

        var result = await service.GetDocumentsByInventoryBatchAsync(10);

        Assert.Single(result);
        Assert.Null(result[0].DocumentGuid);
        Assert.Null(result[0].FileName);
        Assert.Null(result[0].MimeType);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BulkSaveAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BulkSaveAsync_Throws_WhenInventoryBatchIdIsZero()
    {
        var service = CreateService(out _);
        var dto = new InventoryDocumentBulkSaveDto { InventoryBatchId = 0, Documents = new() };

        await Assert.ThrowsAsync<ArgumentException>(() => service.BulkSaveAsync(dto, createdBy: 1));
    }

    [Fact]
    public async Task BulkSaveAsync_Throws_WhenCreatedByIsZero()
    {
        var service = CreateService(out _);
        var dto = new InventoryDocumentBulkSaveDto { InventoryBatchId = 10, Documents = new() };

        await Assert.ThrowsAsync<ArgumentException>(() => service.BulkSaveAsync(dto, createdBy: 0));
    }

    [Fact]
    public async Task BulkSaveAsync_CreatesNewSlot_WhenInventoryDocumentIdIsNull()
    {
        var service = CreateService(out var docService);

        var newEntity = InventoryDocumentEntity.Create(10, 3);

        docService.Setup(s => s.CreateAsync(10, 3, null, null, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(99);
        docService.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newEntity);

        var dto = new InventoryDocumentBulkSaveDto
        {
            InventoryBatchId = 10,
            Documents = new List<InventoryDocumentItemDto>
            {
                new() { DocumentTypeId = 3, IsEnabled = true }
            }
        };

        var result = await service.BulkSaveAsync(dto, createdBy: 42);

        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(1, result.EnabledCount);
        Assert.Equal(0, result.DisabledCount);
        Assert.Empty(result.Errors);
        docService.Verify(s => s.CreateAsync(10, 3, null, null, 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkSaveAsync_ReusesExistingSlot_WhenInventoryDocumentIdProvided()
    {
        var service = CreateService(out var docService);

        var existingEntity = InventoryDocumentEntity.Create(10, 3);
        typeof(InventoryDocumentEntity).GetProperty(nameof(InventoryDocumentEntity.Id))!.SetValue(existingEntity, 77);

        docService.Setup(s => s.GetByIdAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        var dto = new InventoryDocumentBulkSaveDto
        {
            InventoryBatchId = 10,
            Documents = new List<InventoryDocumentItemDto>
            {
                new() { InventoryDocumentId = 77, DocumentTypeId = 3, IsEnabled = true }
            }
        };

        var result = await service.BulkSaveAsync(dto, createdBy: 42);

        Assert.Equal(1, result.TotalProcessed);
        docService.Verify(s => s.CreateAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkSaveAsync_CapturesError_WhenExistingDocumentIdMismatch()
    {
        var service = CreateService(out var docService);

        var existingEntity = InventoryDocumentEntity.Create(99, 3); // BatchId 99 instead of 10
        typeof(InventoryDocumentEntity).GetProperty(nameof(InventoryDocumentEntity.Id))!.SetValue(existingEntity, 77);

        docService.Setup(s => s.GetByIdAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        var dto = new InventoryDocumentBulkSaveDto
        {
            InventoryBatchId = 10,
            Documents = new List<InventoryDocumentItemDto>
            {
                new() { InventoryDocumentId = 77, DocumentTypeId = 3, IsEnabled = true }
            }
        };

        var result = await service.BulkSaveAsync(dto, createdBy: 42);

        Assert.Single(result.Errors);
        Assert.Contains("InventoryDocumentId does not match", result.Errors[0]);
    }

    [Fact]
    public async Task BulkSaveAsync_CountsEnabledAndDisabledItems_Correctly()
    {
        var service = CreateService(out var docService);

        var entity1 = InventoryDocumentEntity.Create(10, 1);
        var entity2 = InventoryDocumentEntity.Create(10, 2);

        docService.Setup(s => s.CreateAsync(10, 1, null, null, 1, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        docService.Setup(s => s.CreateAsync(10, 2, null, null, 1, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        docService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity1);
        docService.Setup(s => s.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(entity2);

        var dto = new InventoryDocumentBulkSaveDto
        {
            InventoryBatchId = 10,
            Documents = new List<InventoryDocumentItemDto>
            {
                new() { DocumentTypeId = 1, IsEnabled = true },
                new() { DocumentTypeId = 2, IsEnabled = false }
            }
        };

        var result = await service.BulkSaveAsync(dto, createdBy: 1);

        Assert.Equal(1, result.EnabledCount);
        Assert.Equal(1, result.DisabledCount);
        Assert.Equal(2, result.TotalProcessed);
    }

    [Fact]
    public async Task BulkSaveAsync_CapturesItemException_InErrorsList()
    {
        var service = CreateService(out var docService);

        docService.Setup(s => s.CreateAsync(10, 3, null, null, 42, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Document type not found"));

        var dto = new InventoryDocumentBulkSaveDto
        {
            InventoryBatchId = 10,
            Documents = new List<InventoryDocumentItemDto>
            {
                new() { DocumentTypeId = 3, IsEnabled = true }
            }
        };

        var result = await service.BulkSaveAsync(dto, createdBy: 42);

        Assert.Single(result.Errors);
        Assert.Contains("Document type not found", result.Errors[0]);
        Assert.Equal(0, result.TotalProcessed);
    }

    [Fact]
    public async Task BulkSaveAsync_ProcessesMultipleItems_ContinuesOnSingleError()
    {
        var service = CreateService(out var docService);

        var goodEntity = InventoryDocumentEntity.Create(10, 2);

        docService.Setup(s => s.CreateAsync(10, 1, null, null, 1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));
        docService.Setup(s => s.CreateAsync(10, 2, null, null, 1, It.IsAny<CancellationToken>())).ReturnsAsync(22);
        docService.Setup(s => s.GetByIdAsync(22, It.IsAny<CancellationToken>())).ReturnsAsync(goodEntity);

        var dto = new InventoryDocumentBulkSaveDto
        {
            InventoryBatchId = 10,
            Documents = new List<InventoryDocumentItemDto>
            {
                new() { DocumentTypeId = 1, IsEnabled = true },
                new() { DocumentTypeId = 2, IsEnabled = true }
            }
        };

        var result = await service.BulkSaveAsync(dto, createdBy: 1);

        Assert.Single(result.Errors);
        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(1, result.EnabledCount);
    }

    [Fact]
    public async Task BulkSaveAsync_ReturnsCorrectInventoryBatchId()
    {
        var service = CreateService(out _);

        var dto = new InventoryDocumentBulkSaveDto { InventoryBatchId = 42, Documents = new() };

        var result = await service.BulkSaveAsync(dto, createdBy: 1);

        Assert.Equal(42, result.InventoryBatchId);
    }
}
