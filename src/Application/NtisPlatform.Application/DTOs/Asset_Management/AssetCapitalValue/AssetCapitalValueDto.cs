using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.AssetCapitalValue;

/// <summary>
/// Request DTO for calculating asset capital value
/// </summary>
public class CalculateAssetCVRequestDto
{
    /// <summary>
    /// The ID of the asset for which to calculate capital value.
    /// </summary>
    [Required]
    public long AssetId { get; set; }

    /// <summary>
    /// Optional: Specific SubUnitsDetails ID to calculate. If 0 or not provided, all floor details will be calculated.
    /// </summary>
    public long SubUnitsDetailsId { get; set; }

    /// <summary>
    /// If true, calculates CV for all child assets (shops/units) and aggregates to parent (building)
    /// </summary>
    public bool IncludeChildAssets { get; set; } = false;

    /// <summary>
    /// User ID creating the capital value calculation.
    /// </summary>
    public int? CreatedBy { get; set; }
}

/// <summary>
/// Result DTO containing calculated capital value and factors
/// </summary>
public class AssetCapitalValueResultDto
{
    public long Id { get; set; }
    public long AssetId { get; set; }
    public long SubUnitsDetailsId { get; set; }

    // Floor details
    public int FloorId { get; set; }
    public string? FloorDescription { get; set; }
    public int? SubFloorId { get; set; }
    public string? SubFloorDescription { get; set; }

    // Construction details
    public string? ConstructionYear { get; set; }
    public string? AssessmentYear { get; set; }
    public int ConstructionTypeId { get; set; }
    public string? ConstructionTypeDescription { get; set; }

    // Use details
    public int TypeOfUseId { get; set; }
    public string? TypeOfUseDescription { get; set; }
    public int? SubTypeOfUseId { get; set; }
    public string? SubTypeOfUseDescription { get; set; }

    // Area details
    public decimal? CarpetAreaSqMeter { get; set; }
    public decimal? CarpetAreaSqFeet { get; set; }
    public decimal? BuiltUpAreaSqMeter { get; set; }
    public decimal? BuiltUpAreaSqFeet { get; set; }

    // CV calculation inputs
    public decimal? CVBaseRate { get; set; }

    // CV factors
    public decimal? CVNatureFactor { get; set; }
    public decimal? CVUseFactor { get; set; }
    public decimal? CVAgeFactor { get; set; }
    public decimal? CVFloorFactor { get; set; }

    // CV result
    public decimal? BaseValue { get; set; }
    public decimal? CapitalValue { get; set; }
    public decimal? MarketValue { get; set; }
    public DateTime? LastCVCalculationDate { get; set; }

    // Formula used for calculation
    public string? CVCalculationFormula { get; set; }

    // Calculation status
    public bool IsCalculated { get; set; }
    public string? CalculationMessage { get; set; }
}

/// <summary>
/// Summary DTO for individual asset capital value (shop/unit level)
/// </summary>
public class AssetCVSummaryDto
{
    public long AssetId { get; set; }
    public string? AssetNo { get; set; }
    public string? AssetName { get; set; }
    public string? AssetType { get; set; }
    public long? ParentAssetId { get; set; }
    public int HierarchyLevel { get; set; }
    public decimal TotalCapitalValue { get; set; }
    public decimal TotalCarpetAreaSqMeter { get; set; }
    public int FloorDetailsCount { get; set; }
    public int CalculatedFloorDetailsCount { get; set; }
    public DateTime? LastCVCalculationDate { get; set; }
    public bool IsFullyCalculated => FloorDetailsCount > 0 && FloorDetailsCount == CalculatedFloorDetailsCount;
    public List<AssetCapitalValueResultDto> FloorDetails { get; set; } = new();
}

/// <summary>
/// Building-level CV summary with breakdown by child assets (shops/units)
/// </summary>
public class BuildingCVSummaryDto
{
    // Building (Parent Asset) Info
    public long BuildingAssetId { get; set; }
    public string? BuildingAssetNo { get; set; }
    public string? BuildingName { get; set; }
    public string? Address { get; set; }
    public bool HasLift { get; set; }

    // Aggregate CV for entire building
    public decimal TotalBuildingCapitalValue { get; set; }
    public decimal TotalBuildingCarpetAreaSqMeter { get; set; }

    // Building's own floor details CV (if any)
    public decimal BuildingOwnCapitalValue { get; set; }
    public int BuildingOwnFloorDetailsCount { get; set; }

    // Child assets (shops/units) summary
    public int TotalChildAssets { get; set; }
    public int CalculatedChildAssets { get; set; }
    public decimal ChildAssetsCapitalValue { get; set; }

    // Calculation status
    public DateTime? LastCVCalculationDate { get; set; }
    public bool IsFullyCalculated { get; set; }
    public string? CalculationMessage { get; set; }

    // Breakdown by child asset (shop/unit)
    public List<AssetCVSummaryDto> ChildAssets { get; set; } = new();

    // Building's own floor details (if any)
    public List<AssetCapitalValueResultDto> BuildingFloorDetails { get; set; } = new();
}

/// <summary>
/// Request DTO for calculating CV for entire building including all shops/units
/// </summary>
public class CalculateBuildingCVRequestDto
{
    /// <summary>
    /// The ID of the building (parent asset) for which to calculate capital value.
    /// </summary>
    [Required]
    public long BuildingAssetId { get; set; }

    /// <summary>
    /// If true, recalculates CV for all assets even if already calculated.
    /// </summary>
    public bool ForceRecalculate { get; set; } = false;

    /// <summary>
    /// User ID creating the capital value calculation.
    /// </summary>
    public int? CreatedBy { get; set; }
}

#region Open Plot CV

/// <summary>
/// Request DTO for calculating CV for an open plot asset
/// </summary>
public class CalculatePlotCVRequestDto
{
    /// <summary>
    /// The ID of the asset (open plot) for which to calculate capital value.
    /// </summary>
    [Required]
    public long AssetId { get; set; }

    /// <summary>
    /// User ID triggering the calculation.
    /// </summary>
    public int? CreatedBy { get; set; }
}

/// <summary>
/// CV result for a single plot record within an asset
/// </summary>
public class PlotCVDetailDto
{
    public int PlotId { get; set; }
    public double? PlotAreaSqMtr { get; set; }
    public double? PlotTaxableAreaSqMtr { get; set; }
    public string? OpenPlotType { get; set; }
    public string? OpenPlotSubmissionType { get; set; }
    public decimal? BaseRate { get; set; }
    public decimal? CapitalValue { get; set; }
    public string? CVCalculationFormula { get; set; }
    public bool IsCalculated { get; set; }
    public string? CalculationMessage { get; set; }
}

/// <summary>
/// Summary DTO for open plot asset capital value
/// </summary>
public class PlotCVSummaryDto
{
    public long AssetId { get; set; }
    public string? AssetNo { get; set; }
    public string? AssetName { get; set; }
    public int TotalPlots { get; set; }
    public int CalculatedPlots { get; set; }
    public double TotalPlotAreaSqMtr { get; set; }
    public decimal TotalCapitalValue { get; set; }
    public DateTime? LastCVCalculationDate { get; set; }
    public bool IsFullyCalculated => TotalPlots > 0 && TotalPlots == CalculatedPlots;
    public List<PlotCVDetailDto> PlotDetails { get; set; } = new();
}

#endregion

#region Movable Asset CVs

/// <summary>
/// Request DTO for calculating CV for movable assets (vehicles, equipment, furniture, etc.)
/// </summary>
public class CalculateMovableAssetCVRequestDto
{
    /// <summary>
    /// The ID of the movable asset for which to calculate capital value.
    /// </summary>
    [Required]
    public long AssetId { get; set; }

    /// <summary>
    /// Valuation method to use for CV calculation
    /// </summary>
    public MovableAssetValuationMethod ValuationMethod { get; set; } = MovableAssetValuationMethod.DepreciatedValue;

    /// <summary>
    /// Custom depreciation rate override (if not using asset's default rate)
    /// </summary>
    public decimal? CustomDepreciationRate { get; set; }

    /// <summary>
    /// Condition factor (1.0 = excellent, 0.8 = good, 0.6 = fair, 0.4 = poor)
    /// </summary>
    public decimal ConditionFactor { get; set; } = 1.0m;

    /// <summary>
    /// User ID creating the capital value calculation.
    /// </summary>
    public int? CreatedBy { get; set; }
}

/// <summary>
/// Valuation methods for movable assets
/// </summary>
public enum MovableAssetValuationMethod
{
    /// <summary>
    /// CV = PurchaseValue × (1 - TotalDepreciation)
    /// </summary>
    DepreciatedValue = 1,

    /// <summary>
    /// CV = CurrentMarketValue (from appraisal)
    /// </summary>
    MarketValue = 2,

    /// <summary>
    /// CV = CurrentBookValue (accounting value)
    /// </summary>
    BookValue = 3,

    /// <summary>
    /// CV = ReplacementCost × ConditionFactor
    /// </summary>
    ReplacementCost = 4
}

/// <summary>
/// Result DTO for movable asset CV calculation
/// </summary>
public class MovableAssetCVResultDto
{
    public long AssetId { get; set; }
    public string? AssetNo { get; set; }
    public string? AssetName { get; set; }
    public string? AssetCategory { get; set; }
    public string? AssetType { get; set; }

    // Purchase info
    public decimal? PurchaseValue { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public int AgeInYears { get; set; }
    public int AgeInMonths { get; set; }

    // Depreciation
    public decimal? DepreciationRate { get; set; }
    public decimal? AccumulatedDepreciation { get; set; }
    public decimal? DepreciationFactor { get; set; }

    // Condition
    public decimal ConditionFactor { get; set; }
    public string? ConditionDescription { get; set; }

    // Values
    public decimal? CurrentBookValue { get; set; }
    public decimal? MarketValue { get; set; }
    public decimal? CapitalValue { get; set; }

    // Calculation details
    public MovableAssetValuationMethod ValuationMethod { get; set; }
    public string? CVCalculationFormula { get; set; }
    public DateTime? LastCVCalculationDate { get; set; }

    // Status
    public bool IsCalculated { get; set; }
    public string? CalculationMessage { get; set; }
}

/// <summary>
/// Summary DTO for multiple movable assets CV
/// </summary>
public class MovableAssetsCVSummaryDto
{
    public int TotalAssets { get; set; }
    public int CalculatedAssets { get; set; }
    public decimal TotalPurchaseValue { get; set; }
    public decimal TotalCapitalValue { get; set; }
    public decimal TotalAccumulatedDepreciation { get; set; }
    public DateTime? LastCVCalculationDate { get; set; }
    public List<MovableAssetCVResultDto> Assets { get; set; } = new();
}

/// <summary>
/// Request DTO for bulk movable assets CV calculation
/// </summary>
public class CalculateBulkMovableAssetsCVRequestDto
{
    /// <summary>
    /// List of asset IDs to calculate CV for
    /// </summary>
    public List<long> AssetIds { get; set; } = new();

    /// <summary>
    /// Optional: Filter by category ID
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Optional: Filter by asset type ID
    /// </summary>
    public int? AssetTypeId { get; set; }

    /// <summary>
    /// Valuation method to use for all assets
    /// </summary>
    public MovableAssetValuationMethod ValuationMethod { get; set; } = MovableAssetValuationMethod.DepreciatedValue;

    /// <summary>
    /// User ID creating the capital value calculation.
    /// </summary>
    public int? CreatedBy { get; set; }
}

#endregion

#region Parent Asset Valuation

/// <summary>
/// Read-only valuation rollup of a parent asset's already-calculated capital value together with
/// all its sub-units (child assets) and inventory. Nothing is calculated or persisted here — it
/// only sums values previously stored by CalculateAsync / CalculateBuildingCVAsync (sub-units) and
/// AssetInventoryService's CV calculation (inventory batches).
/// </summary>
public class ParentAssetValuationDto
{
    public long ParentAssetId { get; set; }
    public string? ParentAssetNo { get; set; }
    public string? ParentAssetName { get; set; }

    /// <summary>Parent asset's own capital value (its own floor details only — excludes sub-units and inventory).</summary>
    public decimal BaseValue { get; set; }

    /// <summary>Count of distinct floor levels across the parent's own floor details and all its sub-units.</summary>
    public int FloorCount { get; set; }

    /// <summary>Sum of CapitalValue across all sub-units (child assets) under this parent.</summary>
    public decimal SubUnitsCapitalValue { get; set; }
    public int SubUnitsCount { get; set; }

    /// <summary>Sum of CV across all inventory batches owned directly by this parent asset.</summary>
    public decimal InventoryCapitalValue { get; set; }
    public int InventoryBatchesCount { get; set; }

    /// <summary>Sum of Quantity across all inventory batches — total physical items, not batch count.</summary>
    public int TotalInventoryCount { get; set; }

    /// <summary>BaseValue + SubUnitsCapitalValue + InventoryCapitalValue.</summary>
    public decimal TotalCapitalValue { get; set; }
}

#endregion
