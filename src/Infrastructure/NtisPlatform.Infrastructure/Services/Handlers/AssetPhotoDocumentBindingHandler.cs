using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Asset_Management;

namespace NtisPlatform.Infrastructure.Services.Handlers;

/// <summary>
/// Handles document binding side-effects for the <c>AssetPhoto</c> business entity.
/// Registered in DI as <see cref="IDocumentBindingHandler"/>.
/// </summary>
public sealed class AssetPhotoDocumentBindingHandler : IDocumentBindingHandler
{
    private readonly IAssetPhotoService _assetPhotoService;
    private readonly ILogger<AssetPhotoDocumentBindingHandler> _logger;

    public string ReferenceTableName => "AssetPhoto";

    public AssetPhotoDocumentBindingHandler(
        IAssetPhotoService assetPhotoService,
        ILogger<AssetPhotoDocumentBindingHandler> logger)
    {
        _assetPhotoService = assetPhotoService;
        _logger = logger;
    }

    public bool Handles(string referenceTableName)
        => string.Equals(referenceTableName, "AssetPhoto", StringComparison.OrdinalIgnoreCase)
        || string.Equals(referenceTableName, "AssetPhotos", StringComparison.OrdinalIgnoreCase);

    public async Task OnAfterUploadAsync(
        int documentId,
        int bindingId,
        int referenceTableId,
        int uploadedBy,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "AssetPhotoDocumentBindingHandler.OnAfterUploadAsync: linking BindingId={BindingId} to AssetPhotoId={PhotoId}",
            bindingId, referenceTableId);

        await _assetPhotoService.UpdateDocumentBindingAsync(
            referenceTableId,
            bindingId,
            uploadedBy,
            cancellationToken);
    }

    public async Task OnBeforeDeleteAsync(
        DocumentBindingEntity binding,
        int deletedBy,
        CancellationToken cancellationToken)
    {
        if (!binding.ReferenceTableId.HasValue || binding.ReferenceTableId.Value <= 0)
            return;

        _logger.LogDebug(
            "AssetPhotoDocumentBindingHandler.OnBeforeDeleteAsync: deleting AssetPhotoId={PhotoId}",
            binding.ReferenceTableId.Value);

        await _assetPhotoService.DeleteAsync(
            binding.ReferenceTableId.Value,
            deletedBy,
            cancellationToken);
    }
}
