using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Asset_Management;

namespace NtisPlatform.Infrastructure.Services.Handlers;

/// <summary>
/// Handles document binding side-effects for the <c>AssetDocument</c> business entity.
/// Registered in DI as <see cref="IDocumentBindingHandler"/>.
/// </summary>
public sealed class AssetDocumentBindingHandler : IDocumentBindingHandler
{
    private readonly IAssetDocumentService _assetDocumentService;
    private readonly ILogger<AssetDocumentBindingHandler> _logger;

    public string ReferenceTableName => "AssetDocument";

    public AssetDocumentBindingHandler(
        IAssetDocumentService assetDocumentService,
        ILogger<AssetDocumentBindingHandler> logger)
    {
        _assetDocumentService = assetDocumentService;
        _logger = logger;
    }

    public bool Handles(string referenceTableName)
        => string.Equals(referenceTableName, "AssetDocument", StringComparison.OrdinalIgnoreCase)
        || string.Equals(referenceTableName, "AssetDocuments", StringComparison.OrdinalIgnoreCase);

    public async Task<bool> ReferenceExistsAsync(int referenceTableId, CancellationToken cancellationToken)
    {
        var document = await _assetDocumentService.GetByIdAsync(referenceTableId, cancellationToken);
        if (document == null)
        {
            _logger.LogWarning(
                "AssetDocumentBindingHandler.ReferenceExistsAsync: no AssetDocument found with ID={DocId}.",
                referenceTableId);
        }
        return document != null;
    }

    public async Task OnAfterUploadAsync(
        int documentId,
        int bindingId,
        int referenceTableId,
        int uploadedBy,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "AssetDocumentBindingHandler.OnAfterUploadAsync: linking BindingId={BindingId} to AssetDocumentId={DocId}",
            bindingId, referenceTableId);

        await _assetDocumentService.UpdateDocumentBindingAsync(
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
            "AssetDocumentBindingHandler.OnBeforeDeleteAsync: deleting AssetDocumentId={DocId}",
            binding.ReferenceTableId.Value);

        await _assetDocumentService.DeleteAsync(
            binding.ReferenceTableId.Value,
            deletedBy,
            cancellationToken);
    }
}
