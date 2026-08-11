using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;
public class InventoryItemCategoryDto : BaseDtos
{
    public int AssetCategoryId { get; set; }
    public string? AssetCategoryName { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
    public decimal DepreciationRate { get; set; }
}
public class CreateInventoryItemCategoryDto : CreateBaseDtos
{
    // FK to AMS.AssetCategoryMaster -- required NOT NULL column on the live DB table.
    [Required(ErrorMessage = "InventoryItemCategory_AssetCategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "InventoryItemCategory_AssetCategoryId_InvalidRange")]
    public int AssetCategoryId { get; set; }

    // Live DB column AMS.InventoryItemCategoryMaster.TypeCode is varchar(20) NOT NULL (ASCII-only,
    // non-unicode) -- required + ASCII-only regex here so a request can't slip past DTO validation
    // and fail with a raw NOT NULL / truncation / unicode-conversion error at the database.
    [Required(ErrorMessage = "InventoryItemCategory_TypeCode_Required")]
    [StringLength(20, ErrorMessage = "InventoryItemCategory_TypeCode_MaxLen_20")]
    [RegularExpression(@"^[A-Za-z0-9\-_]+$", ErrorMessage = "InventoryItemCategory_TypeCode_Invalid")]
    public string TypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "InventoryItemCategory_TypeName_Required")]
    [StringLength(100, ErrorMessage = "InventoryItemCategory_TypeName_MaxLen_100")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "InventoryItemCategory_TypeName_Invalid")]
    public string TypeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "InventoryItemCategory_DisplayOrder_Required")]
    public int? DisplayOrder { get; set; }

    [StringLength(500, ErrorMessage = "InventoryItemCategory_Description_MaxLen_500")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "InventoryItemCategory_Description_Invalid")]
    public string? Description { get; set; }

    [Range(0, 1, ErrorMessage = "InventoryItemCategory_DepreciationRate_Range")]
    public decimal DepreciationRate { get; set; } = 0.10m;
}
public class UpdateInventoryItemCategoryDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "InventoryItemCategory_AssetCategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "InventoryItemCategory_AssetCategoryId_InvalidRange")]
    public int AssetCategoryId { get; set; }

    [Required(ErrorMessage = "InventoryItemCategory_TypeCode_Required")]
    [StringLength(20, ErrorMessage = "InventoryItemCategory_TypeCode_MaxLen_20")]
    [RegularExpression(@"^[A-Za-z0-9\-_]+$", ErrorMessage = "InventoryItemCategory_TypeCode_Invalid")]
    public string TypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "InventoryItemCategory_TypeName_Required")]
    [StringLength(100, ErrorMessage = "InventoryItemCategory_TypeName_MaxLen_100")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "InventoryItemCategory_TypeName_Invalid")]
    public string TypeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "InventoryItemCategory_DisplayOrder_Required")]
    public int? DisplayOrder { get; set; }

    [StringLength(500, ErrorMessage = "InventoryItemCategory_Description_MaxLen_500")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "InventoryItemCategory_Description_Invalid")]
    public string? Description { get; set; }

    [Range(0, 1, ErrorMessage = "InventoryItemCategory_DepreciationRate_Range")]
    public decimal DepreciationRate { get; set; } = 0.10m;
}




