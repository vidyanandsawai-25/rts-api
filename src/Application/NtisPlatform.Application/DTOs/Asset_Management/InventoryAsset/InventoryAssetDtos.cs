using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace NtisPlatform.Application.DTOs.Asset_Management.InventoryAsset;

// ══════════════════════════════════════════════════════════════════════════════
// REQUEST DTOs
// ══════════════════════════════════════════════════════════════════════════════

public class CreateInventoryBatchDto
{
    [Required(ErrorMessage = "AMS_InventoryBatch_ParentAssetId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryBatch_ParentAssetId_InvalidRange")]
    public int ParentAssetId { get; set; }

    [Required(ErrorMessage = "AMS_InventoryBatch_InventoryItemCategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryBatch_InventoryItemCategoryId_InvalidRange")]
    public int InventoryItemCategoryId { get; set; }

    [Required(ErrorMessage = "AMS_InventoryBatch_InventoryItemNameId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryBatch_InventoryItemNameId_InvalidRange")]
    public int InventoryItemNameId { get; set; }

    // Nullable: some item names have no preset models in master data.
    public int? InventoryItemModelId { get; set; }

    [StringLength(500, ErrorMessage = "AMS_InventoryBatch_Specifications_MaxLengthExceeded_500")]
    public string? Specifications { get; set; }

    [Required(ErrorMessage = "AMS_InventoryBatch_PurchaseDate_Required")]
    public DateTime PurchaseDate { get; set; }

    [Required(ErrorMessage = "AMS_InventoryBatch_InventoryItemConditionId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryBatch_InventoryItemConditionId_InvalidRange")]
    public int InventoryItemConditionId { get; set; }

    [Required(ErrorMessage = "AMS_InventoryBatch_Quantity_Required")]
    [Range(1, 10000, ErrorMessage = "AMS_InventoryBatch_Quantity_InvalidRange")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "AMS_InventoryBatch_UnitValue_Required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "AMS_InventoryBatch_UnitValue_InvalidRange")]
    public decimal UnitValue { get; set; }

    [StringLength(100, ErrorMessage = "AMS_InventoryBatch_InvoiceNumber_MaxLengthExceeded_100")]
    public string? InvoiceNumber { get; set; }

    public DateTime? InvoiceDate { get; set; }

    [StringLength(300, ErrorMessage = "AMS_InventoryBatch_InvoiceFileName_MaxLengthExceeded_300")]
    public string? InvoiceFileName { get; set; }

    public int? OwningDepartmentId { get; set; }

    [StringLength(300, ErrorMessage = "AMS_InventoryBatch_PhotoFileName_MaxLengthExceeded_300")]
    public string? PhotoFileName { get; set; }

    /// <summary>
    /// Per-unit overrides. If not provided, default values from batch are used.
    /// </summary>
    public List<RegisterInventoryUnitDto> Units { get; set; } = new();

    // JSON string for per-unit overrides (supports passing the units list as serialized JSON)
    public string? UnitsJson { get; set; }

    // Optional photos/documents uploaded alongside inventory batch creation.
    public List<IFormFile>? DocumentFiles { get; set; }

    // JSON metadata string (same order as DocumentFiles)
    // Example:
    // [{"documentTypeId":1,"displayOrder":1,"remarks":"Invoice document"}]
    public string? DocumentMetadataJson { get; set; }
}

public class RegisterInventoryUnitDto
{
    [Range(1, 10000, ErrorMessage = "AMS_RegisterInventoryUnit_UnitNumber_InvalidRange")]
    public int UnitNumber { get; set; }

    // Per-unit condition override (FK to InventoryItemConditionMaster). Falls back to the batch condition.
    public int? InventoryItemConditionId { get; set; }

    [Range(0.0, 1.0, ErrorMessage = "AMS_RegisterInventoryUnit_ConditionFactor_InvalidRange")]
    public decimal? ConditionFactor { get; set; }
}

// ══════════════════════════════════════════════════════════════════════════════
// RESPONSE DTOs - Batch Registration
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Display names resolved by joining an inventory batch/unit's FK ids against their
/// master tables (InventoryItemCategoryMaster/NameMaster/ModelMaster/ConditionMaster/
/// OwningDepartmentMaster). Not backed by columns on InventoryBatch/InventoryAssetDetail
/// themselves — kept separate from the entity-mirrored fields on the parent DTO.
/// </summary>
public class InventoryLookupNamesDto
{
    public string InventoryType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string ModelBrand { get; set; } = string.Empty;
    public string? Condition { get; set; }
    public string? OwningDepartment { get; set; }
}

public class InventoryBatchDto
{
    public int BatchId { get; set; }
    public int ParentAssetId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }
    public decimal TotalBatchValue { get; set; }
    public decimal TotalCapitalValue { get; set; }
    public decimal TotalDepreciation { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? Message { get; set; }
    public InventoryLookupNamesDto Names { get; set; } = new();
    public List<InventoryUnitResponseDto> Units { get; set; } = new();
}

public class InventoryUnitResponseDto
{
    public int AssetId { get; set; }
    public string AssetNo { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public int UnitNumber { get; set; }
    public string? Condition { get; set; }
    public decimal UnitPurchaseValue { get; set; }
    public decimal? UnitCapitalValue { get; set; }
    public decimal? DepreciationRate { get; set; }
    public decimal? ConditionFactor { get; set; }
    public int AgeInYears { get; set; }
    public string? CVFormula { get; set; }
}

// ══════════════════════════════════════════════════════════════════════════════
// RESPONSE DTOs - Grouped CV Display
// (GetInventoryCVAsync/GetInventoryRatesAsync are not yet routed by any controller —
// a routing gap, not a DTO defect — kept as-is so that service code keeps compiling.)
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Category-level grouping for CV display (e.g., "Furniture", "IT Equipment").
/// </summary>
public class InventoryCategoryGroupDto
{
    public string InventoryType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int TotalBatches { get; set; }
    public int TotalUnits { get; set; }
    public decimal TotalPurchaseValue { get; set; }
    public decimal TotalCapitalValue { get; set; }
    public decimal TotalDepreciation { get; set; }
    public decimal DepreciationPercent { get; set; }
    public List<InventoryBatchDto> Batches { get; set; } = new();
}

/// <summary>
/// Full inventory CV response for a parent asset.
/// </summary>
public class InventoryCVResponseDto
{
    public int ParentAssetId { get; set; }
    public string ParentAssetName { get; set; } = string.Empty;
    public int TotalBatches { get; set; }
    public int TotalUnitsRegistered { get; set; }
    public int TotalFailed { get; set; }
    public decimal GrandPurchaseValue { get; set; }
    public decimal GrandCapitalValue { get; set; }
    public decimal GrandDepreciation { get; set; }
    public List<InventoryCategoryGroupDto> CategoryGroups { get; set; } = new();
    public List<InventoryBatchDto> FailedBatches { get; set; } = new();
}

// ══════════════════════════════════════════════════════════════════════════════
// DTOs - Depreciation Rates (for UI)
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Depreciation rate info per inventory category.
/// </summary>
public class DepreciationRateDto
{
    public int CategoryId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public decimal DepreciationRate { get; set; }
}

/// <summary>
/// Combined rates response for UI CV calculations.
/// </summary>
public class InventoryRatesResponseDto
{
    public List<DepreciationRateDto> DepreciationRates { get; set; } = new();
}

// ══════════════════════════════════════════════════════════════════════════════
// DTOs - Update Inventory Batch
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// DTO for updating an existing inventory batch.
/// </summary>
public class UpdateInventoryBatchDto
{
    [Required(ErrorMessage = "AMS_InventoryBatch_BatchId_Required")]
    public int BatchId { get; set; }

    public int? InventoryItemNameId { get; set; }

    public int? InventoryItemModelId { get; set; }

    [StringLength(500, ErrorMessage = "AMS_InventoryBatch_Specifications_MaxLengthExceeded_500")]
    public string? Specifications { get; set; }

    public DateTime? PurchaseDate { get; set; }

    public int? InventoryItemConditionId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "AMS_InventoryBatch_UnitValue_InvalidRange")]
    public decimal? UnitValue { get; set; }

    [StringLength(100, ErrorMessage = "AMS_InventoryBatch_InvoiceNumber_MaxLengthExceeded_100")]
    public string? InvoiceNumber { get; set; }

    public DateTime? InvoiceDate { get; set; }

    [StringLength(300, ErrorMessage = "AMS_InventoryBatch_InvoiceFileName_MaxLengthExceeded_300")]
    public string? InvoiceFileName { get; set; }

    public int? OwningDepartmentId { get; set; }

    [StringLength(300, ErrorMessage = "AMS_InventoryBatch_PhotoFileName_MaxLengthExceeded_300")]
    public string? PhotoFileName { get; set; }
}

/// <summary>
/// Response for listing all inventory batches for a parent asset.
/// </summary>
public class InventoryBatchListResponseDto
{
    public int ParentAssetId { get; set; }
    public string ParentAssetName { get; set; } = string.Empty;
    public int TotalBatches { get; set; }
    public int TotalUnits { get; set; }
    public decimal TotalPurchaseValue { get; set; }
    public decimal TotalCapitalValue { get; set; }
    public List<InventoryBatchDetailDto> Batches { get; set; } = new();
}

/// <summary>
/// Detailed batch info for list view.
/// </summary>
public class InventoryBatchDetailDto
{
    public int BatchId { get; set; }
    public int ParentAssetId { get; set; }
    public string? Specifications { get; set; }
    public DateTime PurchaseDate { get; set; }
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }
    public decimal TotalBatchValue { get; set; }
    public decimal TotalBatchCV { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? InvoiceFileName { get; set; }
    public string? PhotoFileName { get; set; }
    public DateTime CreatedDate { get; set; }
    public InventoryLookupNamesDto Names { get; set; } = new();
    public List<InventoryUnitResponseDto> Units { get; set; } = new();
    public List<InventoryDocumentDto> Documents { get; set; } = new();
}
