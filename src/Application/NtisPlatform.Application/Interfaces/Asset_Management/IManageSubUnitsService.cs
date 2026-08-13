using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Asset_Management.ManageSubUnits;

namespace NtisPlatform.Application.Interfaces.Asset_Management;

public interface IManageSubUnitsService
{
    Task<List<SubUnitListDto>> GetAllSubUnitsByParentIdAsync(
        int parentAssetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the complete details of a single sub-unit (asset, renter, room-wise and floor
    /// details) in one payload. Mirrors the per-sub-asset shape produced by
    /// <c>AssetMasterService.GetSubAssetsGroupedByParentAsync</c>; floor details are resolved
    /// against the parent asset and filtered by the sub-unit's room-wise submissions.
    /// </summary>
    Task<SubAssetDetailDto> GetSubUnitDetailsByIdAsync(
        int assetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk generates child assets (rooms/shops) under a parent asset with optional room and lease/rent details.
    /// </summary>
    /// <param name="dto">DTO containing parent asset ID, generation parameters, and optional room/lease-rent details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response containing generated asset IDs and any errors</returns>
    Task<BulkGenerateChildAssetsResponseDto> BulkGenerateChildAssetsAsync(
        BulkGenerateChildAssetsDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk generates child assets (rooms/shops) across multiple floors in one transaction.
    /// Creates one AssetMaster entry + one SubUnitsDetails row per generated unit.
    /// </summary>
    Task<BulkGenerateAcrossFloorsResponseDto> BulkGenerateAcrossFloorsAsync(
        BulkGenerateAcrossFloorsDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a single child asset (room/shop) under a parent asset with complete details from the form.
    ///
    /// FLOW:
    ///   STEP 1: Create the child asset (e.g., SHOP-101) under parent building and get the new AssetId
    ///   STEP 2: Use the new AssetId to create room-wise submission details in AssetRoomWiseSubmissionDetails table
    ///   STEP 3: Use the new AssetId to create lease/rent details in AssetLeaseRentDetails table
    ///
    /// All operations are wrapped in a transaction - if any step fails, everything is rolled back.
    /// </summary>
    /// <param name="dto">DTO containing all form data including basic info, rent info, floor config, and room details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response containing the created asset ID and related record IDs</returns>
    Task<CreateChildAssetResponseDto> CreateChildAssetAsync(
        CreateChildAssetDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a child asset's complete details by AssetId including lease/rent and room-wise details.
    /// </summary>
    /// <param name="assetId">The ID of the child asset to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response containing the asset details with lease/rent and room information</returns>
    Task<GetChildAssetResponseDto> GetChildAssetByIdAsync(
        int assetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the sub-unit floor details and matching lease/rent record.
    /// </summary>
    /// <param name="assetId">
    /// A child AssetMaster id. If no asset matches, this is retried as a SubUnitsDetails
    /// (floor details) id and the owning asset is resolved from that row instead — see the
    /// implementation's fallback lookup. Kept as <c>assetId</c> to match the primary lookup path
    /// and the controller's route segment; callers should normally pass an asset id.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<SubUnitLeaseRentDetailDto> GetSubUnitLeaseRentBySubUnitDetailsIdAsync(
        int assetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all child assets (subunits) under a parent asset.
    /// </summary>
    /// <param name="parentAssetId">The ID of the parent asset (building)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of subunits under the parent asset</returns>
    Task<List<SubUnitResponseDto>> GetSubUnitsByAssetIdAsync(
        int parentAssetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves complete details of all child assets (subunits) under a parent asset.
    /// Includes floor details, room-wise submissions, and room-wise minus details.
    /// </summary>
    Task<List<SubUnitCompleteDetailDto>> GetSubUnitsCompleteDetailsByParentIdAsync(
        int parentAssetId,
        CancellationToken cancellationToken = default);
}
