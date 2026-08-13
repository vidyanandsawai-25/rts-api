using NtisPlatform.Application.DTOs.AssetCapitalValue;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for Asset Capital Value calculations
/// Supports:
/// - Immovable assets (buildings, shops) with floor-based CV calculation
/// - Movable assets (vehicles, equipment) with depreciation-based CV calculation
/// </summary>
public interface IAssetCapitalValueService
{
    #region Immovable Assets (Buildings, Shops)

    /// <summary>
    /// Calculate and store capital value for asset floor details
    /// </summary>
    Task<AssetCVSummaryDto> CalculateAsync(CalculateAssetCVRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate CV for entire building including all child assets (shops/units)
    /// Aggregates CVs from all children to the building level
    /// </summary>
    Task<BuildingCVSummaryDto> CalculateBuildingCVAsync(CalculateBuildingCVRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get capital value summary for a specific asset (shop/unit)
    /// </summary>
    Task<AssetCVSummaryDto?> GetAssetCVSummaryAsync(long assetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get building-level CV summary with breakdown by child assets
    /// </summary>
    Task<BuildingCVSummaryDto?> GetBuildingCVSummaryAsync(long buildingAssetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get capital value for a specific asset floor detail
    /// </summary>
    Task<AssetCapitalValueResultDto?> GetByAssetFloorIdAsync(long assetFloorDetailId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all capital values for an asset
    /// </summary>
    Task<List<AssetCapitalValueResultDto>> GetByAssetIdAsync(long assetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all child assets (shops/units) for a building
    /// </summary>
    Task<List<AssetCVSummaryDto>> GetChildAssetsCVAsync(long parentAssetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get CV calculation history for an asset
    /// </summary>
    Task<List<AssetCVCalculationHistoryDto>> GetCalculationHistoryAsync(long assetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read-only valuation rollup of a parent asset's own base value plus the already-calculated
    /// CV of all its sub-units (child assets) and inventory batches. Nothing is calculated or
    /// persisted — this only sums values previously stored elsewhere.
    /// </summary>
    Task<ParentAssetValuationDto?> GetParentAssetValuationAsync(long parentAssetId, CancellationToken cancellationToken = default);

    #endregion

    #region Open Plot Assets

    /// <summary>
    /// Calculate CV for an open plot asset based on AMS.PlotDetails entries
    /// </summary>
    Task<PlotCVSummaryDto> CalculatePlotCVAsync(CalculatePlotCVRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the stored capital value for an open plot asset
    /// </summary>
    Task<PlotCVSummaryDto?> GetPlotCVAsync(long assetId, CancellationToken cancellationToken = default);

    #endregion

    #region Movable Assets (Vehicles, Equipment, Furniture)

    /// <summary>
    /// Calculate CV for a movable asset based on purchase value and depreciation
    /// </summary>
    Task<MovableAssetCVResultDto> CalculateMovableAssetCVAsync(CalculateMovableAssetCVRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate CV for multiple movable assets in bulk
    /// </summary>
    Task<MovableAssetsCVSummaryDto> CalculateBulkMovableAssetsCVAsync(CalculateBulkMovableAssetsCVRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get CV for a movable asset
    /// </summary>
    Task<MovableAssetCVResultDto?> GetMovableAssetCVAsync(long assetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get CV summary for all movable assets by category or type
    /// </summary>
    Task<MovableAssetsCVSummaryDto> GetMovableAssetsCVByCategoryAsync(int? categoryId, int? assetTypeId, CancellationToken cancellationToken = default);

    #endregion
}
