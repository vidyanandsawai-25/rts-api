using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces.Asset_Management;
using NtisPlatform.Infrastructure.Services.Handlers;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Handlers;

public class AssetPhotoDocumentBindingHandlerTests
{
    [Fact]
    public void ReferenceTableName_ReturnsAssetPhoto()
    {
        var photoService = new Mock<IAssetPhotoService>();
        var logger = new Mock<ILogger<AssetPhotoDocumentBindingHandler>>();

        var handler = new AssetPhotoDocumentBindingHandler(photoService.Object, logger.Object);

        Assert.Equal("AssetPhoto", handler.ReferenceTableName);
    }

    [Fact]
    public void Handles_ReturnsTrue_ForAssetPhotoAndAssetPhotos()
    {
        var handler = new AssetPhotoDocumentBindingHandler(Mock.Of<IAssetPhotoService>(), Mock.Of<ILogger<AssetPhotoDocumentBindingHandler>>());

        Assert.True(handler.Handles("AssetPhoto"));
        Assert.True(handler.Handles("assetphotos"));
        Assert.False(handler.Handles("OtherTable"));
    }

    [Fact]
    public async Task OnAfterUploadAsync_CallsUpdateDocumentBindingAsync()
    {
        var photoService = new Mock<IAssetPhotoService>();
        var logger = new Mock<ILogger<AssetPhotoDocumentBindingHandler>>();

        var handler = new AssetPhotoDocumentBindingHandler(photoService.Object, logger.Object);

        await handler.OnAfterUploadAsync(
            1, // documentId
            200, // bindingId
            50, // referenceTableId
            42, // uploadedBy
            CancellationToken.None);

        photoService.Verify(s => s.UpdateDocumentBindingAsync(50, 200, 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnAfterUploadAsync_PropagatesException_WhenPhotoNotFound()
    {
        var photoService = new Mock<IAssetPhotoService>();
        var logger = new Mock<ILogger<AssetPhotoDocumentBindingHandler>>();

        photoService.Setup(s => s.UpdateDocumentBindingAsync(999, 200, 42, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AssetPhotoNotFoundException(999));

        var handler = new AssetPhotoDocumentBindingHandler(photoService.Object, logger.Object);

        await Assert.ThrowsAsync<AssetPhotoNotFoundException>(() => handler.OnAfterUploadAsync(
            1,
            200,
            999,
            42,
            CancellationToken.None));
    }

    [Fact]
    public async Task OnBeforeDeleteAsync_CallsDeleteAsync_WhenReferenceTableIdHasValue()
    {
        var photoService = new Mock<IAssetPhotoService>();
        var logger = new Mock<ILogger<AssetPhotoDocumentBindingHandler>>();

        var handler = new AssetPhotoDocumentBindingHandler(photoService.Object, logger.Object);

        var binding = new DocumentBindingEntity
        {
            ReferenceTableId = 50,
            ReferenceTableName = "AssetPhoto"
        };

        await handler.OnBeforeDeleteAsync(
            binding,
            42,
            CancellationToken.None);

        photoService.Verify(s => s.DeleteAsync(50, 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnBeforeDeleteAsync_DoesNothing_WhenReferenceTableIdNullOrZero()
    {
        var photoService = new Mock<IAssetPhotoService>();
        var logger = new Mock<ILogger<AssetPhotoDocumentBindingHandler>>();

        var handler = new AssetPhotoDocumentBindingHandler(photoService.Object, logger.Object);

        var binding1 = new DocumentBindingEntity { ReferenceTableId = null };
        var binding2 = new DocumentBindingEntity { ReferenceTableId = 0 };

        await handler.OnBeforeDeleteAsync(binding1, 42, CancellationToken.None);
        await handler.OnBeforeDeleteAsync(binding2, 42, CancellationToken.None);

        photoService.Verify(s => s.DeleteAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
