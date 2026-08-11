using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace NtisPlatform.Application.DTOs.Asset_Management.InventoryBatch;

/// <summary>
/// Table-specific DTO for InventoryBatchEntity - Represents a batch of inventory items.
/// </summary>
public class InventoryBatchDto : BaseDtos
{
    public int ParentAssetId { get; set; }
    public int? InventoryItemCategoryId { get; set; }
    public int? InventoryItemNameId { get; set; }
    public int? InventoryItemModelId { get; set; }
    public int? InventoryItemConditionId { get; set; }
    public int? OwningDepartmentId { get; set; }
    public string? Specifications { get; set; }
    public DateTime PurchaseDate { get; set; }
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }
    public decimal TotalBatchValue { get; set; }
    public decimal? TotalBatchCV { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? InvoiceFileName { get; set; }
    public string? PhotoFileName { get; set; }

    // Navigation property names
    public string? ParentAssetName { get; set; }
    public string? CategoryName { get; set; }
    public string? ItemName { get; set; }
    public string? ModelName { get; set; }
    public string? ConditionName { get; set; }
    public string? DepartmentName { get; set; }
}

public class CreateInventoryBatchDto : CreateBaseDtos
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

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryBatch_InventoryItemModelId_InvalidRange")]
    public int? InventoryItemModelId { get; set; }

    [Required(ErrorMessage = "AMS_InventoryBatch_InventoryItemConditionId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryBatch_InventoryItemConditionId_InvalidRange")]
    public int InventoryItemConditionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryBatch_OwningDepartmentId_InvalidRange")]
    public int? OwningDepartmentId { get; set; }

    [StringLength(500, ErrorMessage = "AMS_InventoryBatch_Specifications_MaxLengthExceeded_500")]
    public string? Specifications { get; set; }

    [Required(ErrorMessage = "AMS_InventoryBatch_PurchaseDate_Required")]
    public DateTime? PurchaseDate { get; set; }

    [Required(ErrorMessage = "AMS_InventoryBatch_Quantity_Required")]
    [Range(1, 10000, ErrorMessage = "AMS_InventoryBatch_Quantity_InvalidRange")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "AMS_InventoryBatch_UnitValue_Required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "AMS_InventoryBatch_UnitValue_InvalidRange")]
    public decimal UnitValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_InventoryBatch_TotalBatchCV_InvalidRange")]
    public decimal? TotalBatchCV { get; set; }

    [StringLength(100, ErrorMessage = "AMS_InventoryBatch_InvoiceNumber_MaxLengthExceeded_100")]
    public string? InvoiceNumber { get; set; }

    public DateTime? InvoiceDate { get; set; }

    [StringLength(300, ErrorMessage = "AMS_InventoryBatch_InvoiceFileName_MaxLengthExceeded_300")]
    public string? InvoiceFileName { get; set; }

    [StringLength(300, ErrorMessage = "AMS_InventoryBatch_PhotoFileName_MaxLengthExceeded_300")]
    public string? PhotoFileName { get; set; }

    // Optional photos/documents uploaded alongside inventory batch creation.
    public List<IFormFile>? DocumentFiles { get; set; }

    // JSON metadata string (same order as DocumentFiles)
    // Example:
    // [{"documentTypeId":1,"displayOrder":1,"remarks":"Invoice document"}]
    public string? DocumentMetadataJson { get; set; }
}

public class UpdateInventoryBatchDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryBatch_InventoryItemCategoryId_InvalidRange")]
    public int? InventoryItemCategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryBatch_InventoryItemNameId_InvalidRange")]
    public int? InventoryItemNameId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryBatch_InventoryItemModelId_InvalidRange")]
    public int? InventoryItemModelId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryBatch_InventoryItemConditionId_InvalidRange")]
    public int? InventoryItemConditionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_InventoryBatch_OwningDepartmentId_InvalidRange")]
    public int? OwningDepartmentId { get; set; }

    [StringLength(500, ErrorMessage = "AMS_InventoryBatch_Specifications_MaxLengthExceeded_500")]
    public string? Specifications { get; set; }

    [Required(ErrorMessage = "AMS_InventoryBatch_PurchaseDate_Required")]
    public DateTime? PurchaseDate { get; set; }

    [Required(ErrorMessage = "AMS_InventoryBatch_Quantity_Required")]
    [Range(1, 10000, ErrorMessage = "AMS_InventoryBatch_Quantity_InvalidRange")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "AMS_InventoryBatch_UnitValue_Required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "AMS_InventoryBatch_UnitValue_InvalidRange")]
    public decimal UnitValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_InventoryBatch_TotalBatchCV_InvalidRange")]
    public decimal? TotalBatchCV { get; set; }

    [StringLength(100, ErrorMessage = "AMS_InventoryBatch_InvoiceNumber_MaxLengthExceeded_100")]
    public string? InvoiceNumber { get; set; }

    public DateTime? InvoiceDate { get; set; }

    [StringLength(300, ErrorMessage = "AMS_InventoryBatch_InvoiceFileName_MaxLengthExceeded_300")]
    public string? InvoiceFileName { get; set; }

    [StringLength(300, ErrorMessage = "AMS_InventoryBatch_PhotoFileName_MaxLengthExceeded_300")]
    public string? PhotoFileName { get; set; }
}
