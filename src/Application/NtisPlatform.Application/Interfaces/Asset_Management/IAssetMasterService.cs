using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Asset_Management.AssetFieldValue;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Interfaces.Asset_Management;

/// <summary>
/// Service interface for asset master CRUD operations and dashboard statistics.
/// </summary>
public interface IAssetMasterService : ICommonCrudService<AssetMasterEntity, AssetMasterDto, CreateAssetMasterDto, UpdateAssetMasterDto, AssetMasterQueryParameters, int>
{


    /// <summary>
    /// Activates an asset, all field values linked to the asset, and child assets that reference it as parent.
    /// </summary>
    Task<bool> ActivateAssetAndFieldValuesAsync(int assetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all child assets by parent asset ID and floor details ID.
    /// </summary>
    Task<List<AssetMasterDto>> GetByParentAssetIdAsync(int parentAssetId, int floorDetailsId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shop-wise details including asset and renter information by parent asset ID.
    /// </summary>
    Task<List<ShopWiseDetailsDto>> GetShopWiseDetailsByParentAssetIdAsync(int parentAssetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sub-assets (child assets) grouped by parent asset ID with their related
    /// floor details, room-wise submissions, and renter details.
    /// </summary>
    /// <param name="parentAssetId">The parent asset ID to get sub-assets for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A grouped response containing parent asset info and all sub-assets with related details.</returns>
    Task<SubAssetGroupedResponseDto> GetSubAssetsGroupedByParentAsync(int parentAssetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the combined payload used by the floor and other details tab for a single asset.
    /// </summary>
    Task<AssetFloorAndOtherDetailsResponseDto?> GetAssetFloorAndOtherDetailsAsync(int assetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a unique AssetNo using the ULB code prefix, category segment, type segment, and incremental sequence.
    /// </summary>
    Task<string> GenerateAssetNoAsync(int assetCategoryId, int assetTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a batch of unique AssetNos.
    /// </summary>
    Task<List<string>> GenerateAssetNosAsync(int assetCategoryId, int assetTypeId, int count, string? subunitPrefix = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the ULB code prefix from ULBMasterEntity.
    /// </summary>
    Task<string> GetUlbCodeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a batch of unique AssetNos starting with the specified prefix and padded sequence numbers.
    /// </summary>
    Task<List<string>> GenerateAssetNosWithPrefixAsync(string prefix, int count, int padding, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk saves field values for an asset, updating existing ones and inserting new ones.
    /// </summary>
    Task<bool> BulkSaveFieldValuesAsync(int assetId, List<CreateAssetFieldValueDto> fieldValues, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports filtered asset master records to an Excel spreadsheet.
    /// </summary>
    Task<byte[]> ExportToExcelAsync(AssetMasterQueryParameters queryParameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Same as <see cref="ICommonCrudService{TEntity,TDto,TCreateDto,TUpdateDto,TQueryParams,TKey}.GetByIdAsync"/>,
    /// but returns null if the asset's owning department is outside <paramref name="currentUserId"/>'s allowed scope
    /// (prevents IDOR — a non-admin user guessing/incrementing ids outside their departments).
    /// </summary>
    Task<AssetMasterDto?> GetByIdForUserAsync(int id, int currentUserId, CancellationToken cancellationToken = default);
}
