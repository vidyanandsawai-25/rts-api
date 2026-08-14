using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Infrastructure.Services.Handlers;

/// <summary>
/// Handles document binding side-effects for the <c>ULBDocument</c> business entity (ULB-wide
/// documents such as the certified Tax Zoning List/Map). Registered in DI as
/// <see cref="IDocumentBindingHandler"/> so that <c>DocumentApplicationService</c> remains fully
/// ignorant of this module's logic. Structured exactly like
/// <c>PropertyCertificateDocumentBindingHandler</c>.
///
/// <para>
/// Responsibilities:
/// <list type="bullet">
///   <item>After upload — link the new <c>DocumentBindingId</c> back to the row.</item>
///   <item>Before delete — unlink the document binding from the row.</item>
/// </list>
/// </para>
/// </summary>
public sealed class ULBDocumentBindingHandler : IDocumentBindingHandler
{
    private readonly IULBDocumentService _ulbDocumentService;
    private readonly ILogger<ULBDocumentBindingHandler> _logger;

    /// <inheritdoc/>
    public string ReferenceTableName => "ULBDocument";

    public ULBDocumentBindingHandler(
        IULBDocumentService ulbDocumentService,
        ILogger<ULBDocumentBindingHandler> logger)
    {
        _ulbDocumentService = ulbDocumentService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool Handles(string referenceTableName)
        => string.Equals(referenceTableName, "ULBDocument", StringComparison.OrdinalIgnoreCase)
        || string.Equals(referenceTableName, "ULBDocuments", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public async Task<bool> ReferenceExistsAsync(int referenceTableId, CancellationToken cancellationToken)
    {
        var exists = await _ulbDocumentService.ExistsAsync(referenceTableId, cancellationToken);

        if (!exists)
        {
            _logger.LogWarning(
                "ULBDocumentBindingHandler.ReferenceExistsAsync: no ULBDocument found with ID={Id}.",
                referenceTableId);
        }

        return exists;
    }

    /// <summary>
    /// Links the newly created <c>DocumentBindingId</c> back to the row identified by
    /// <paramref name="referenceTableId"/>. The row is guaranteed to exist at this point because
    /// <see cref="ReferenceExistsAsync"/> is checked before the transaction/file write even started.
    /// </summary>
    public async Task OnAfterUploadAsync(
        int documentId,
        int bindingId,
        int referenceTableId,
        int uploadedBy,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "ULBDocumentBindingHandler.OnAfterUploadAsync: linking BindingId={BindingId} to ULBDocumentId={Id}",
            bindingId, referenceTableId);

        await _ulbDocumentService.LinkDocumentBindingAsync(
            referenceTableId,
            bindingId,
            uploadedBy,
            cancellationToken);
    }

    /// <summary>
    /// Unlinks the document binding from the row when its associated document is being deleted.
    /// </summary>
    public async Task OnBeforeDeleteAsync(
        DocumentBindingEntity binding,
        int deletedBy,
        CancellationToken cancellationToken)
    {
        if (!binding.ReferenceTableId.HasValue || binding.ReferenceTableId.Value <= 0)
            return;

        _logger.LogDebug(
            "ULBDocumentBindingHandler.OnBeforeDeleteAsync: unlinking ULBDocumentId={Id}",
            binding.ReferenceTableId.Value);

        // Verify the row's binding ID still points to this binding before unlinking.
        var entity = await _ulbDocumentService.GetEntityByIdAsync(binding.ReferenceTableId.Value, cancellationToken);

        if (entity != null && entity.DocumentBindingId == binding.Id)
        {
            await _ulbDocumentService.UnlinkDocumentBindingAsync(
                binding.ReferenceTableId.Value,
                deletedBy,
                cancellationToken);
        }
    }
}
