using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Infrastructure.Services.Handlers;

/// <summary>
/// Handles document binding side-effects for the <c>PropertyPhoto</c> business entity.
/// Registered in DI as <see cref="IDocumentBindingHandler"/> so that
/// <c>DocumentApplicationService</c> remains fully ignorant of PropertyPhoto-specific logic.
///
/// <para>
/// Responsibilities:
/// <list type="bullet">
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

    /// <summary>
    /// Links the newly created <c>DocumentBindingId</c> back to the <c>PropertyPhoto</c> row
    /// identified by <paramref name="referenceTableId"/>.
    /// </summary>
    public async Task OnAfterUploadAsync(
        int documentId,
        int bindingId,
        int referenceTableId,
        int uploadedBy,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "PropertyPhotoDocumentBindingHandler.OnAfterUploadAsync: linking BindingId={BindingId} to PropertyPhotoId={PhotoId}",
            bindingId, referenceTableId);

        await _propertyPhotoService.UpdateDocumentBindingAsync(
            referenceTableId,
            bindingId,
            uploadedBy,
            cancellationToken);
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

        _logger.LogDebug(
            "PropertyPhotoDocumentBindingHandler.OnBeforeDeleteAsync: deleting PropertyPhotoId={PhotoId}",
            binding.ReferenceTableId.Value);

        await _propertyPhotoService.DeleteAsync(
            binding.ReferenceTableId.Value,
            deletedBy,
            cancellationToken);
    }
}
