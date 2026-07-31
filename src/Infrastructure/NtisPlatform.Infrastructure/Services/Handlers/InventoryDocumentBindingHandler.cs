using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Asset_Management;

namespace NtisPlatform.Infrastructure.Services.Handlers;

/// <summary>
/// Handles document binding side-effects for the <c>InventoryDocument</c> business entity.
/// Registered in DI as <see cref="IDocumentBindingHandler"/>.
/// </summary>
public sealed class InventoryDocumentBindingHandler : IDocumentBindingHandler
{
    private readonly IInventoryDocumentService _inventoryDocumentService;
    private readonly ILogger<InventoryDocumentBindingHandler> _logger;

    public string ReferenceTableName => "InventoryDocument";

    public InventoryDocumentBindingHandler(
        IInventoryDocumentService inventoryDocumentService,
        ILogger<InventoryDocumentBindingHandler> logger)
    {
        _inventoryDocumentService = inventoryDocumentService;
        _logger = logger;
    }

    public bool Handles(string referenceTableName)
        => string.Equals(referenceTableName, "InventoryDocument", StringComparison.OrdinalIgnoreCase)
        || string.Equals(referenceTableName, "InventoryDocuments", StringComparison.OrdinalIgnoreCase);

    public async Task<bool> ReferenceExistsAsync(int referenceTableId, CancellationToken cancellationToken)
    {
        var document = await _inventoryDocumentService.GetByIdAsync(referenceTableId, cancellationToken);
        if (document == null)
        {
            _logger.LogWarning(
                "InventoryDocumentBindingHandler.ReferenceExistsAsync: no active InventoryDocument found with ID={DocId}.",
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
            "InventoryDocumentBindingHandler.OnAfterUploadAsync: linking BindingId={BindingId} to InventoryDocumentId={DocId}",
            bindingId, referenceTableId);

        await _inventoryDocumentService.UpdateDocumentBindingAsync(
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
            "InventoryDocumentBindingHandler.OnBeforeDeleteAsync: deleting InventoryDocumentId={DocId}",
            binding.ReferenceTableId.Value);

        await _inventoryDocumentService.DeleteAsync(
            binding.ReferenceTableId.Value,
            deletedBy,
            cancellationToken);
    }
}
