using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NtisPlatform.Application.DTOs.Asset_Management;

namespace NtisPlatform.Application.Interfaces.Asset_Management;

/// <summary>
/// Application service for AMS.AssetPhoto operations.
/// </summary>
public interface IAssetPhotoApplicationService
{
    Task<List<AssetPhotoDto>> GetPhotosByAssetAsync(
        int assetId,
        CancellationToken cancellationToken = default);

    Task<AssetPhotoGalleryDto> GetGroupedPhotosByAssetAsync(
        int assetId,
        CancellationToken cancellationToken = default);

    Task<List<AssetPhotoTypeWithStatusDto>> GetPhotoTypesWithStatusAsync(
        int assetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk save all photo types for an asset (single Save button).
    /// Documents are uploaded separately via upload/replace endpoints.
    /// </summary>
    Task<AssetPhotoBulkSaveResponseDto> BulkSaveAllAsync(
        AssetPhotoBulkSaveDto bulkDto,
        int userId,
        CancellationToken cancellationToken = default);
}
