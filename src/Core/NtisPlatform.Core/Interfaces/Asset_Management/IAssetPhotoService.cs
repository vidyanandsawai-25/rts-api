using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Core.Interfaces.Asset_Management;

/// <summary>
/// Service for AMS.AssetPhoto operations.
/// </summary>
public interface IAssetPhotoService
{
    Task<int> CreateAsync(
        int assetId,
        int photoTypeId,
        int? subUnitDetailsId,
        int? displayOrder,
        string? remarks,
        int createdBy,
        CancellationToken cancellationToken = default);

    Task UpdateDocumentBindingAsync(
        int photoId,
        int documentBindingId,
        int updatedBy,
        CancellationToken cancellationToken = default);

    Task<AssetPhotoEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<AssetPhotoEntity>> GetLatestByAssetIdAsync(
        int assetId,
        CancellationToken cancellationToken = default);

    Task<List<AssetPhotoEntity>> GetLatestByAssetIdIncludingInactiveAsync(
        int assetId,
        CancellationToken cancellationToken = default);

    Task MarkAsSupersededAsync(
        int photoId,
        int updatedBy,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        int photoId,
        int? displayOrder,
        string? remarks,
        int updatedBy,
        CancellationToken cancellationToken = default);

    Task ToggleEnabledAsync(
        int photoId,
        bool isEnabled,
        int updatedBy,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        int deletedBy,
        CancellationToken cancellationToken = default);
}
