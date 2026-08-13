using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management.InventoryAssetDetail;

/// <summary>
/// DTO for InventoryAssetDetailEntity - Individual inventory unit details.
/// </summary>
public class InventoryAssetDetailDto : BaseDtos
{
    public int AssetId { get; set; }
    public int BatchId { get; set; }
    public int UnitNumber { get; set; }
    public int? InventoryItemCategoryId { get; set; }
    public int? InventoryItemNameId { get; set; }
    public int? InventoryItemModelId { get; set; }
    public int? InventoryItemConditionId { get; set; }
    public int? OwningDepartmentId { get; set; }
    public string? Specifications { get; set; }
    public string? PhotoFileId { get; set; }
    public decimal UnitPurchaseValue { get; set; }
    public decimal? UnitCapitalValue { get; set; }

    // Navigation property names
    public string? AssetName { get; set; }
    public string? AssetNo { get; set; }
    public string? CategoryName { get; set; }
    public string? ItemName { get; set; }
    public string? ModelName { get; set; }
    public string? ConditionName { get; set; }
    public string? DepartmentName { get; set; }
}

public class CreateInventoryAssetDetailDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AMS_InventoryAssetDetail_AssetId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_AssetId_InvalidRange")]
    public int AssetId { get; set; }

    [Required(ErrorMessage = "AMS_InventoryAssetDetail_BatchId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_BatchId_InvalidRange")]
    public int BatchId { get; set; }

    [Required(ErrorMessage = "AMS_InventoryAssetDetail_UnitNumber_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_UnitNumber_InvalidRange")]
    public int UnitNumber { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_InventoryItemCategoryId_InvalidRange")]
    public int? InventoryItemCategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_InventoryItemNameId_InvalidRange")]
    public int? InventoryItemNameId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_InventoryItemModelId_InvalidRange")]
    public int? InventoryItemModelId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_InventoryItemConditionId_InvalidRange")]
    public int? InventoryItemConditionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_OwningDepartmentId_InvalidRange")]
    public int? OwningDepartmentId { get; set; }

    [StringLength(500, ErrorMessage = "AMS_InventoryAssetDetail_Specifications_MaxLengthExceeded_500")]
    public string? Specifications { get; set; }

    [StringLength(300, ErrorMessage = "AMS_InventoryAssetDetail_PhotoFileId_MaxLengthExceeded_300")]
    public string? PhotoFileId { get; set; }

    [Required(ErrorMessage = "AMS_InventoryAssetDetail_UnitPurchaseValue_Required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_UnitPurchaseValue_InvalidRange")]
    public decimal UnitPurchaseValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_UnitCapitalValue_InvalidRange")]
    public decimal? UnitCapitalValue { get; set; }
}

public class UpdateInventoryAssetDetailDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AMS_InventoryAssetDetail_UnitNumber_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_UnitNumber_InvalidRange")]
    public int UnitNumber { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_InventoryItemCategoryId_InvalidRange")]
    public int? InventoryItemCategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_InventoryItemNameId_InvalidRange")]
    public int? InventoryItemNameId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_InventoryItemModelId_InvalidRange")]
    public int? InventoryItemModelId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_InventoryItemConditionId_InvalidRange")]
    public int? InventoryItemConditionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_OwningDepartmentId_InvalidRange")]
    public int? OwningDepartmentId { get; set; }

    [StringLength(500, ErrorMessage = "AMS_InventoryAssetDetail_Specifications_MaxLengthExceeded_500")]
    public string? Specifications { get; set; }

    [StringLength(300, ErrorMessage = "AMS_InventoryAssetDetail_PhotoFileId_MaxLengthExceeded_300")]
    public string? PhotoFileId { get; set; }

    [Required(ErrorMessage = "AMS_InventoryAssetDetail_UnitPurchaseValue_Required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_UnitPurchaseValue_InvalidRange")]
    public decimal UnitPurchaseValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_InventoryAssetDetail_UnitCapitalValue_InvalidRange")]
    public decimal? UnitCapitalValue { get; set; }
}
