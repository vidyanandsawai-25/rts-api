using NtisPlatform.Application.DTOs.PropertyPhoto;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Application service for PTIS.PropertyPhoto operations.
/// Orchestrates file storage + CORE.Document + CORE.DocumentBinding + PTIS.PropertyPhoto.
/// SEPARATE from the Document service.
/// </summary>
public interface IPropertyPhotoApplicationService
{
    /// <summary>
    /// Uploads a new photo for a property + photo type, creating the document, binding and
    /// PTIS.PropertyPhoto row. Multiple current photos are allowed per (PropertyId, PhotoTypeId).
    /// To replace a specific existing photo version, call <see cref="ReplacePhotoAsync"/> with
    /// the target PropertyPhotoId instead.
    /// </summary>
    Task<PropertyPhotoUploadResponseDto> UploadPhotoAsync(
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        int propertyId,
        int photoTypeId,
        int? displayOrder,
        string? remarks,
        int uploadedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces an existing photo's image. The current row is superseded (IsLatest = 0, retained
    /// for audit) and a new latest row is created with the new document. When remarks are null
    /// the previous value is carried forward.
    /// </summary>
    Task<PropertyPhotoUploadResponseDto> ReplacePhotoAsync(
        int propertyPhotoId,
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        string? remarks,
        int uploadedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all current photos for a property as a flat list (the "Additional Images" gallery).
    /// </summary>
    Task<List<PropertyPhotoDto>> GetPhotosByPropertyAsync(
        int propertyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full photo gallery for a property as a single grouped JSON: every active photo
    /// type with its current photos nested inside.
    /// </summary>
    Task<PropertyPhotoGalleryDto> GetGroupedPhotosByPropertyAsync(
        int propertyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active photo types with their current status for a property
    /// (drives the photo-slot picker / "Add Photo Plan Slot").
    /// </summary>
    Task<List<PropertyPhotoTypeWithStatusDto>> GetPhotoTypesWithStatusAsync(
        int propertyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a photo (two-phase delete).
    /// </summary>
    Task<bool> DeletePhotoAsync(
        int propertyPhotoId,
        int deletedBy,
        CancellationToken cancellationToken = default);
}
