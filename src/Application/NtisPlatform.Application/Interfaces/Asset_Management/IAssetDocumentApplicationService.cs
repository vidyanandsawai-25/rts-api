using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDocument;

namespace NtisPlatform.Application.Interfaces.Asset_Management;

/// <summary>
/// Application service for AMS.AssetDocument operations.
/// </summary>
public interface IAssetDocumentApplicationService
{
    Task<List<AssetDocumentDto>> GetDocumentsByAssetAsync(
        int assetId,
        CancellationToken cancellationToken = default);

    Task<AssetDocumentGalleryDto> GetGroupedDocumentsByAssetAsync(
        int assetId,
        CancellationToken cancellationToken = default);

    Task<List<AssetDocumentTypeWithStatusDto>> GetDocumentTypesWithStatusAsync(
        int assetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk save all document definitions for an asset (single Save button).
    /// </summary>
    Task<AssetDocumentBulkSaveResponseDto> BulkSaveAllAsync(
        AssetDocumentBulkSaveDto bulkDto,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Single document save + upload. Binds directly from multipart form.
    /// </summary>
    Task<AssetDocumentDto> SaveWithUploadAsync(
        AssetDocumentSaveWithUploadDto request,
        int uploadedBy,
        CancellationToken cancellationToken = default);
}
