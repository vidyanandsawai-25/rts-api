using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Core.Interfaces.Asset_Management;

/// <summary>
/// Service for AMS.AssetDocument operations.
/// </summary>
public interface IAssetDocumentService
{
    Task<int> CreateAsync(
        int assetId,
        int documentDefinitionId,
        int? displayOrder,
        string? remarks,
        int createdBy,
        CancellationToken cancellationToken = default);

    Task UpdateDocumentBindingAsync(
        int documentId,
        int documentBindingId,
        int updatedBy,
        CancellationToken cancellationToken = default);

    Task<AssetDocumentEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<AssetDocumentEntity>> GetLatestByAssetIdAsync(
        int assetId,
        CancellationToken cancellationToken = default);

    Task<List<AssetDocumentEntity>> GetLatestByAssetIdIncludingInactiveAsync(
        int assetId,
        CancellationToken cancellationToken = default);

    Task MarkAsSupersededAsync(
        int documentId,
        int updatedBy,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        int documentId,
        int? displayOrder,
        string? remarks,
        int updatedBy,
        CancellationToken cancellationToken = default);

    Task ToggleEnabledAsync(
        int documentId,
        bool isEnabled,
        int updatedBy,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        int deletedBy,
        CancellationToken cancellationToken = default);
}
