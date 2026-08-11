using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces.Asset_Management;
using NtisPlatform.Infrastructure.Services.Handlers;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Handlers;

public class AssetDocumentBindingHandlerTests
{
    [Fact]
    public void ReferenceTableName_ReturnsAssetDocument()
    {
        var documentService = new Mock<IAssetDocumentService>();
        var logger = new Mock<ILogger<AssetDocumentBindingHandler>>();

        var handler = new AssetDocumentBindingHandler(documentService.Object, logger.Object);

        Assert.Equal("AssetDocument", handler.ReferenceTableName);
    }

    [Fact]
    public void Handles_ReturnsTrue_ForAssetDocumentAndAssetDocuments()
    {
        var handler = new AssetDocumentBindingHandler(Mock.Of<IAssetDocumentService>(), Mock.Of<ILogger<AssetDocumentBindingHandler>>());

        Assert.True(handler.Handles("AssetDocument"));
        Assert.True(handler.Handles("assetdocuments"));
        Assert.False(handler.Handles("OtherTable"));
    }

    [Fact]
    public async Task OnAfterUploadAsync_CallsUpdateDocumentBindingAsync()
    {
        var documentService = new Mock<IAssetDocumentService>();
        var logger = new Mock<ILogger<AssetDocumentBindingHandler>>();

        var handler = new AssetDocumentBindingHandler(documentService.Object, logger.Object);

        await handler.OnAfterUploadAsync(
            1, // documentId
            200, // bindingId
            50, // referenceTableId
            42, // uploadedBy
            CancellationToken.None);

        documentService.Verify(s => s.UpdateDocumentBindingAsync(50, 200, 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnAfterUploadAsync_PropagatesException_WhenDocumentNotFound()
    {
        var documentService = new Mock<IAssetDocumentService>();
        var logger = new Mock<ILogger<AssetDocumentBindingHandler>>();

        documentService.Setup(s => s.UpdateDocumentBindingAsync(999, 200, 42, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("AssetDocument with ID 999 not found"));

        var handler = new AssetDocumentBindingHandler(documentService.Object, logger.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.OnAfterUploadAsync(
            1,
            200,
            999,
            42,
            CancellationToken.None));
    }

    [Fact]
    public async Task OnBeforeDeleteAsync_CallsDeleteAsync_WhenReferenceTableIdHasValue()
    {
        var documentService = new Mock<IAssetDocumentService>();
        var logger = new Mock<ILogger<AssetDocumentBindingHandler>>();

        var handler = new AssetDocumentBindingHandler(documentService.Object, logger.Object);

        var binding = new DocumentBindingEntity
        {
            ReferenceTableId = 50,
            ReferenceTableName = "AssetDocument"
        };

        await handler.OnBeforeDeleteAsync(
            binding,
            42,
            CancellationToken.None);

        documentService.Verify(s => s.DeleteAsync(50, 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnBeforeDeleteAsync_DoesNothing_WhenReferenceTableIdNullOrZero()
    {
        var documentService = new Mock<IAssetDocumentService>();
        var logger = new Mock<ILogger<AssetDocumentBindingHandler>>();

        var handler = new AssetDocumentBindingHandler(documentService.Object, logger.Object);

        var binding1 = new DocumentBindingEntity { ReferenceTableId = null };
        var binding2 = new DocumentBindingEntity { ReferenceTableId = 0 };

        await handler.OnBeforeDeleteAsync(binding1, 42, CancellationToken.None);
        await handler.OnBeforeDeleteAsync(binding2, 42, CancellationToken.None);

        documentService.Verify(s => s.DeleteAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReferenceExistsAsync_ReturnsTrue_WhenAssetDocumentExists()
    {
        var documentService = new Mock<IAssetDocumentService>();
        var logger = new Mock<ILogger<AssetDocumentBindingHandler>>();

        documentService.Setup(s => s.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Core.Entities.Asset_Management.AssetDocumentEntity.Create(1, 2));

        var handler = new AssetDocumentBindingHandler(documentService.Object, logger.Object);

        var result = await handler.ReferenceExistsAsync(50, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task ReferenceExistsAsync_ReturnsFalse_WhenAssetDocumentDoesNotExist()
    {
        var documentService = new Mock<IAssetDocumentService>();
        var logger = new Mock<ILogger<AssetDocumentBindingHandler>>();

        documentService.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NtisPlatform.Core.Entities.Asset_Management.AssetDocumentEntity)null!);

        var handler = new AssetDocumentBindingHandler(documentService.Object, logger.Object);

        var result = await handler.ReferenceExistsAsync(999, CancellationToken.None);

        Assert.False(result);
    }
}
