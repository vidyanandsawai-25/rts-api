using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Service for PTIS.PropertyPhoto operations.
/// SEPARATE from the Document service - handles the business row only, not file storage.
/// A property can have zero, one or many photos per photo type.
/// </summary>
public interface IPropertyPhotoService
{
    /// <summary>
    /// Creates a property photo row without document binding.
    /// Use this when you need to create the photo before the DocumentBinding exists.
    /// </summary>
    Task<int> CreateAsync(
        int propertyId,
        int photoTypeId,
        int? displayOrder,
        string? remarks,
        int createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the document binding ID for an existing property photo.
    /// </summary>
    Task UpdateDocumentBindingAsync(
        int propertyPhotoId,
        int documentBindingId,
        int updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a property photo by ID without any related data.
    /// </summary>
    Task<PropertyPhotoEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all current (latest, active, non-deleted) photos for a property - all types and
    /// all photos per type - with PhotoType, DocumentBinding and Document loaded.
    /// Ordered by DisplayOrder then PhotoTypeId.
    /// </summary>
    Task<List<PropertyPhotoEntity>> GetLatestByPropertyIdAsync(
        int propertyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an existing photo as superseded (IsLatest = 0) so a replacement version can
    /// become the current one while the previous version is retained for audit.
    /// </summary>
    Task MarkAsSupersededAsync(
        int propertyPhotoId,
        int updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a superseded photo back to latest state (IsLatest = 1). Used for compensation
    /// if a replacement operation fails after superseding.
    /// </summary>
    Task RestoreFromSupersedingAsync(
        int id,
        int restoredBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a property photo (two-phase delete via MarkedForDeletion).
    /// </summary>
    Task DeleteAsync(
        int id,
        int deletedBy,
        CancellationToken cancellationToken = default);
}
