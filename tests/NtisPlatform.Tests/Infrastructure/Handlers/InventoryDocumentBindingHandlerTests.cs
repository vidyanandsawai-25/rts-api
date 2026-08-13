using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Asset_Management;
using NtisPlatform.Infrastructure.Services.Handlers;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Handlers;

public class InventoryDocumentBindingHandlerTests
{
    private static InventoryDocumentBindingHandler CreateHandler(
        out Mock<IInventoryDocumentService> docService)
    {
        docService = new Mock<IInventoryDocumentService>();
        var logger = new Mock<ILogger<InventoryDocumentBindingHandler>>();

        return new InventoryDocumentBindingHandler(docService.Object, logger.Object);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Handles
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("InventoryDocument")]
    [InlineData("inventorydocument")]
    [InlineData("INVENTORYDOCUMENT")]
    [InlineData("InventoryDocuments")]
    [InlineData("inventorydocuments")]
    public void Handles_ReturnsTrue_ForInventoryDocumentTableNames(string tableName)
    {
        var handler = CreateHandler(out _);

        Assert.True(handler.Handles(tableName));
    }

    [Theory]
    [InlineData("AssetDocument")]
    [InlineData("AssetPhoto")]
    [InlineData("PropertyCertificate")]
    [InlineData("")]
    [InlineData("Inventory")]
    public void Handles_ReturnsFalse_ForOtherTableNames(string tableName)
    {
        var handler = CreateHandler(out _);

        Assert.False(handler.Handles(tableName));
    }

    [Fact]
    public void ReferenceTableName_IsInventoryDocument()
    {
        var handler = CreateHandler(out _);

        Assert.Equal("InventoryDocument", handler.ReferenceTableName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ReferenceExistsAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReferenceExistsAsync_ReturnsTrue_WhenDocumentExists()
    {
        var handler = CreateHandler(out var docService);
        docService.Setup(s => s.GetByIdAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync(InventoryDocumentEntity.Create(10, 3));

        var exists = await handler.ReferenceExistsAsync(77, CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task ReferenceExistsAsync_ReturnsFalse_WhenDocumentDoesNotExist()
    {
        var handler = CreateHandler(out var docService);
        docService.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryDocumentEntity)null!);

        var exists = await handler.ReferenceExistsAsync(999, CancellationToken.None);

        Assert.False(exists);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OnAfterUploadAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task OnAfterUploadAsync_CallsUpdateDocumentBindingAsync_WithCorrectParameters()
    {
        var handler = CreateHandler(out var docService);

        await handler.OnAfterUploadAsync(
            documentId: 10,
            bindingId: 200,
            referenceTableId: 55,
            uploadedBy: 42,
            cancellationToken: CancellationToken.None);

        docService.Verify(s => s.UpdateDocumentBindingAsync(
            55,     // referenceTableId = inventoryDocumentId
            200,    // bindingId
            42,     // uploadedBy
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task OnAfterUploadAsync_PropagatesException_FromService()
    {
        var handler = CreateHandler(out var docService);

        docService
            .Setup(s => s.UpdateDocumentBindingAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Record not found"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.OnAfterUploadAsync(1, 2, 3, 4, CancellationToken.None));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OnBeforeDeleteAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task OnBeforeDeleteAsync_CallsDeleteAsync_WhenReferenceTableIdIsValid()
    {
        var handler = CreateHandler(out var docService);

        var binding = new DocumentBindingEntity { Id = 1, ReferenceTableId = 77 };

        await handler.OnBeforeDeleteAsync(binding, deletedBy: 42, CancellationToken.None);

        docService.Verify(s => s.DeleteAsync(77, 42, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task OnBeforeDeleteAsync_DoesNotCallDeleteAsync_WhenReferenceTableIdIsNull()
    {
        var handler = CreateHandler(out var docService);

        var binding = new DocumentBindingEntity { Id = 1, ReferenceTableId = null };

        await handler.OnBeforeDeleteAsync(binding, deletedBy: 42, CancellationToken.None);

        docService.Verify(s => s.DeleteAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnBeforeDeleteAsync_DoesNotCallDeleteAsync_WhenReferenceTableIdIsZero()
    {
        var handler = CreateHandler(out var docService);

        var binding = new DocumentBindingEntity { Id = 1, ReferenceTableId = 0 };

        await handler.OnBeforeDeleteAsync(binding, deletedBy: 42, CancellationToken.None);

        docService.Verify(s => s.DeleteAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnBeforeDeleteAsync_PropagatesException_FromService()
    {
        var handler = CreateHandler(out var docService);

        var binding = new DocumentBindingEntity { Id = 1, ReferenceTableId = 55 };

        docService
            .Setup(s => s.DeleteAsync(55, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Already deleted"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.OnBeforeDeleteAsync(binding, deletedBy: 1, CancellationToken.None));
    }
}
