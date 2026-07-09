using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
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
/// Responsibilities:
/// <list type="bullet">
///   <item>Before upload commits — verify the target <c>PropertyPhoto</c> or <c>PropertyMast</c> exists (<see cref="ReferenceExistsAsync"/>).</item>
///   <item>After upload — link the new <c>DocumentBindingId</c> back to the <c>PropertyPhoto</c> row, dynamically creating it if pointing to a <c>PropertyId</c>.</item>
///   <item>Before delete — soft-delete the associated <c>PropertyPhoto</c> row.</item>
/// </list>
/// </para>
/// </summary>
public sealed class PropertyPhotoDocumentBindingHandler : IDocumentBindingHandler
{
    private readonly IPropertyPhotoService _propertyPhotoService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PropertyPhotoDocumentBindingHandler> _logger;

    /// <inheritdoc/>
    public string ReferenceTableName => "PropertyPhoto";

    public PropertyPhotoDocumentBindingHandler(
        IPropertyPhotoService propertyPhotoService,
        ApplicationDbContext context,
        ILogger<PropertyPhotoDocumentBindingHandler> logger)
    {
        _propertyPhotoService = propertyPhotoService;
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool Handles(string referenceTableName)
        => string.Equals(referenceTableName, "PropertyPhoto", StringComparison.OrdinalIgnoreCase)
        || string.Equals(referenceTableName, "PropertyPhotos", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Task<bool> ReferenceExistsAsync(int referenceTableId, CancellationToken cancellationToken)
        => ReferenceExistsAsync(referenceTableId, null, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> ReferenceExistsAsync(int referenceTableId, string? referencePropertyName, CancellationToken cancellationToken)
    {
        if (string.Equals(referencePropertyName, "PropertyId", StringComparison.OrdinalIgnoreCase))
        {
            var propertyExists = await _context.PropertyMast
                .AnyAsync(x => x.Id == referenceTableId && x.IsActive && !x.MarkedForDeletion, cancellationToken);

            if (!propertyExists)
            {
                _logger.LogWarning(
                    "PropertyPhotoDocumentBindingHandler.ReferenceExistsAsync: no active Property found with ID={PropertyId}.",
                    referenceTableId);
            }

            return propertyExists;
        }

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
    /// Links the newly created <c>DocumentBindingId</c> back to the <c>PropertyPhoto</c> row.
    /// If the binding is currently pointing to a <c>PropertyId</c>, a new <c>PropertyPhoto</c>
    /// row is created on the fly and the binding is updated to reference it.
    /// </summary>
    public async Task OnAfterUploadAsync(
        int documentId,
        int bindingId,
        int referenceTableId,
        int uploadedBy,
        CancellationToken cancellationToken)
    {
        var binding = await _context.DocumentBindings
            .Include(db => db.Document)
            .FirstOrDefaultAsync(db => db.Id == bindingId, cancellationToken);

        if (binding != null && string.Equals(binding.ReferencePropertyName, "PropertyId", StringComparison.OrdinalIgnoreCase))
        {
            var docType = binding.Document?.DocumentType;
            if (string.IsNullOrEmpty(docType))
            {
                throw new InvalidOperationException("Cannot resolve photo type: DocumentType is empty.");
            }

            PropertyPhotoTypeEntity? photoType = null;
            if (int.TryParse(docType, out var typeId))
            {
                photoType = await _context.PropertyPhotoTypes
                    .FirstOrDefaultAsync(t => t.Id == typeId && t.IsActive, cancellationToken);
            }

            if (photoType == null)
            {
                var docTypeLower = docType.ToLower();
                photoType = await _context.PropertyPhotoTypes
                    .FirstOrDefaultAsync(t => t.PhotoTypeCode.ToLower() == docTypeLower && t.IsActive, cancellationToken);
            }

            if (photoType == null)
            {
                throw new InvalidOperationException($"Invalid or inactive photo type code/ID: '{docType}'");
            }

            var photo = PropertyPhotoEntity.CreateWithDocument(
                propertyId: referenceTableId,
                photoTypeId: photoType.Id,
                documentBindingId: bindingId,
                remarks: binding.BindingPurpose);

            photo.CreatedBy = uploadedBy;
            photo.CreatedDate = DateTime.Now;

            _context.PropertyPhotos.Add(photo);
            await _context.SaveChangesAsync(cancellationToken);

            binding.ReferenceTableId = photo.Id;
            binding.ReferencePropertyName = "Id";
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "PropertyPhotoDocumentBindingHandler: Created new PropertyPhoto ID={PhotoId} for Property ID={PropertyId} and Type={TypeCode}, updated binding reference.",
                photo.Id, referenceTableId, photoType.PhotoTypeCode);
        }
        else
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
