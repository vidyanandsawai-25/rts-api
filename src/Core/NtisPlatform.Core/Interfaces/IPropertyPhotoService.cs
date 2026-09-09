using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Service for PTIS.PropertyPhoto operations (supporting Property 'P', Society 'S', and Wing 'W').
/// </summary>
public interface IPropertyPhotoService
{
    Task<int> CreateAsync(
        int propertyId,
        int photoTypeId,
        int? displayOrder,
        string? remarks,
        int createdBy,
        CancellationToken cancellationToken = default);

    Task<int> CreateWithDetailsAsync(
        int propertyId,
        int photoTypeId,
        int? displayOrder,
        string? remarks,
        int createdBy,
        CancellationToken cancellationToken = default);

    Task<int> CreateForSocietyAsync(
        int societyId,
        int photoTypeId,
        int? displayOrder,
        string? remarks,
        int createdBy,
        CancellationToken cancellationToken = default);

    Task<int> CreateForWingAsync(
        int wingId,
        int photoTypeId,
        int? displayOrder,
        string? remarks,
        int createdBy,
        CancellationToken cancellationToken = default);

    Task UpdateDocumentBindingAsync(
        int propertyPhotoId,
        int documentBindingId,
        int updatedBy,
        CancellationToken cancellationToken = default);

    Task<PropertyPhotoEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<PropertyPhotoEntity>> GetLatestByPropertyIdAsync(
        int propertyId,
        CancellationToken cancellationToken = default);

    Task<List<PropertyPhotoEntity>> GetLatestBySocietyIdAsync(
        int societyId,
        CancellationToken cancellationToken = default);

    Task<List<PropertyPhotoEntity>> GetLatestByWingIdAsync(
        int wingId,
        CancellationToken cancellationToken = default);

    Task MarkAsSupersededAsync(
        int propertyPhotoId,
        int updatedBy,
        CancellationToken cancellationToken = default);

    Task RestoreFromSupersedingAsync(
        int id,
        int restoredBy,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        int deletedBy,
        CancellationToken cancellationToken = default);
}
