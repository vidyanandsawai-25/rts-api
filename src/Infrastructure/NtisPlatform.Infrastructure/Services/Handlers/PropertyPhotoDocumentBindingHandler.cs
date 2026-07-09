using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Infrastructure.Services.Handlers;

/// <summary>
/// Handles document binding side-effects for the <c>PropertyPhoto</c> business entity.
/// Registered in DI as <see cref="IDocumentBindingHandler"/> so that
/// <c>DocumentApplicationService</c> remains fully ignorant of PropertyPhoto-specific logic.
///
/// <para>
/// <b>Clean Architecture boundary:</b> this handler NEVER creates a <c>PropertyPhoto</c> row.
/// Creating the business row is the sole responsibility of
/// <c>PropertyPhotoApplicationService.UploadPhotoAsync</c>, which creates the row FIRST
/// (validating PropertyId/PhotoTypeId) and only then calls the global Document API with the
/// resulting <c>PropertyPhotoId</c> as <c>ReferenceTableId</c>. Letting the generic
/// <c>/api/documents/upload</c> endpoint spawn business rows on the caller's say-so would
/// bypass that validation and let any caller of the generic endpoint create orphaned
/// PropertyPhoto rows for arbitrary PropertyIds. This handler therefore only verifies and
/// links an EXISTING row.
/// </para>
///
/// <para>
/// Responsibilities:
/// <list type="bullet">
///   <item>Before upload commits — verify the target <c>PropertyPhoto</c> row exists (<see cref="ReferenceExistsAsync"/>).</item>
///   <item>After upload — link the new <c>DocumentBindingId</c> back to the <c>PropertyPhoto</c> row.</item>
///   <item>Before delete — soft-delete the associated <c>PropertyPhoto</c> row.</item>
/// </list>
/// </para>
/// </summary>
public sealed class PropertyPhotoDocumentBindingHandler : IDocumentBindingHandler
{
    private readonly IPropertyPhotoService _propertyPhotoService;
    private readonly ILogger<PropertyPhotoDocumentBindingHandler> _logger;

    /// <inheritdoc/>
    public string ReferenceTableName => "PropertyPhoto";

    public PropertyPhotoDocumentBindingHandler(
        IPropertyPhotoService propertyPhotoService,
        ILogger<PropertyPhotoDocumentBindingHandler> logger)
    {
        _propertyPhotoService = propertyPhotoService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool Handles(string referenceTableName)
        => string.Equals(referenceTableName, "PropertyPhoto", StringComparison.OrdinalIgnoreCase)
        || string.Equals(referenceTableName, "PropertyPhotos", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public async Task<bool> ReferenceExistsAsync(int referenceTableId, CancellationToken cancellationToken)
    {
        var photo = await _propertyPhotoService.GetByIdAsync(referenceTableId, cancellationToken);
        var exists = photo != null;

        if (!exists)
        {
            _logger.LogWarning(
                "PropertyPhotoDocumentBindingHandler.ReferenceExistsAsync: no active PropertyPhoto found with ID={PhotoId}.",
                referenceTableId);
        }

        return exists;
    }

    /// <summary>
    /// Links the newly created <c>DocumentBindingId</c> back to the EXISTING <c>PropertyPhoto</c>
    /// row identified by <paramref name="referenceTableId"/>. The row is guaranteed to exist at
    /// this point because <see cref="ReferenceExistsAsync"/> is checked by
    /// <c>DocumentApplicationService</c> before the transaction/file write even started.
    /// </summary>
    public async Task OnAfterUploadAsync(
        int documentId,
        int bindingId,
        int referenceTableId,
        int uploadedBy,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "PropertyPhotoDocumentBindingHandler.OnAfterUploadAsync: linking BindingId={BindingId} to PropertyPhotoId={PhotoId}, DocumentId={DocumentId}",
            bindingId, referenceTableId, documentId);

        await _propertyPhotoService.UpdateDocumentBindingAsync(
            referenceTableId,
            bindingId,
            uploadedBy,
            cancellationToken);

        _logger.LogInformation(
            "PropertyPhotoDocumentBindingHandler: linked BindingId={BindingId} to PropertyPhotoId={PhotoId}",
            bindingId, referenceTableId);
    }

    /// <summary>
    /// Soft-deletes the <c>PropertyPhoto</c> row referenced by <paramref name="binding"/>
    /// when its associated document is being deleted.
    /// </summary>
    public async Task OnBeforeDeleteAsync(
        DocumentBindingEntity binding,
        int deletedBy,
        CancellationToken cancellationToken)
    {
        if (!binding.ReferenceTableId.HasValue || binding.ReferenceTableId.Value <= 0)
            return;

        var photo = await _propertyPhotoService.GetByIdAsync(binding.ReferenceTableId.Value, cancellationToken);
        if (photo == null)
        {
            _logger.LogWarning(
                "PropertyPhotoDocumentBindingHandler.OnBeforeDeleteAsync: PropertyPhotoId={PhotoId} not found (already deleted?). Skipping.",
                binding.ReferenceTableId.Value);
            return;
        }

        _logger.LogDebug(
            "PropertyPhotoDocumentBindingHandler.OnBeforeDeleteAsync: deleting PropertyPhotoId={PhotoId}",
            binding.ReferenceTableId.Value);

        await _propertyPhotoService.DeleteAsync(binding.ReferenceTableId.Value, deletedBy, cancellationToken);
    }
}
