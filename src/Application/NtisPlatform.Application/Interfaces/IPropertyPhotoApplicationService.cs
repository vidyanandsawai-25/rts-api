using NtisPlatform.Application.DTOs.PropertyPhoto;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Application service for PTIS.PropertyPhoto operations (Property, Society, Wing).
/// </summary>
public interface IPropertyPhotoApplicationService
{
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

    Task<PropertyPhotoUploadResponseDto> ReplacePhotoAsync(
        int propertyPhotoId,
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        string? remarks,
        int uploadedBy,
        CancellationToken cancellationToken = default);

    Task<List<PropertyPhotoDto>> GetPhotosByPropertyAsync(
        int propertyId,
        CancellationToken cancellationToken = default);

    Task<PropertyPhotoGalleryDto> GetGroupedPhotosByPropertyAsync(
        int propertyId,
        CancellationToken cancellationToken = default);

    Task<List<PropertyPhotoTypeWithStatusDto>> GetPhotoTypesWithStatusAsync(
        int propertyId,
        CancellationToken cancellationToken = default);

    // ========== Society Photos ==========
    Task<List<PropertyPhotoDto>> GetPhotosBySocietyAsync(
        int societyId,
        CancellationToken cancellationToken = default);

    Task<PropertyPhotoGalleryDto> GetGroupedPhotosBySocietyAsync(
        int societyId,
        CancellationToken cancellationToken = default);

    Task<List<PropertyPhotoTypeWithStatusDto>> GetPhotoTypesWithStatusForSocietyAsync(
        int societyId,
        CancellationToken cancellationToken = default);

    // ========== Wing Photos ==========
    Task<List<PropertyPhotoDto>> GetPhotosByWingAsync(
        int wingId,
        CancellationToken cancellationToken = default);

    Task<PropertyPhotoGalleryDto> GetGroupedPhotosByWingAsync(
        int wingId,
        CancellationToken cancellationToken = default);

    Task<List<PropertyPhotoTypeWithStatusDto>> GetPhotoTypesWithStatusForWingAsync(
        int wingId,
        CancellationToken cancellationToken = default);

    Task<bool> DeletePhotoAsync(
        int propertyPhotoId,
        int deletedBy,
        CancellationToken cancellationToken = default);
}
