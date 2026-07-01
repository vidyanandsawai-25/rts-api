using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;
public class InventoryItemCategoryDto : BaseDtos
{
    public string? TypeCode { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
    public decimal DepreciationRate { get; set; }
}
public class CreateInventoryItemCategoryDto : CreateBaseDtos
{
    [StringLength(100, ErrorMessage = "InventoryItemCategory_TypeCode_MaxLen_100")]
    public string? TypeCode { get; set; }

    [Required(ErrorMessage = "InventoryItemCategory_TypeName_Required")]
    [StringLength(100, ErrorMessage = "InventoryItemCategory_TypeName_MaxLen_100")]
    public string TypeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "InventoryItemCategory_DisplayOrder_Required")]
    public int? DisplayOrder { get; set; }
    [StringLength(500, ErrorMessage = "InventoryItemCategory_Description_MaxLen_500")]
    public string? Description { get; set; }

    [Range(0, 1, ErrorMessage = "InventoryItemCategory_DepreciationRate_Range")]
    public decimal DepreciationRate { get; set; } = 0.10m;
}
public class UpdateInventoryItemCategoryDto : UpdateBaseDtos
{
    [StringLength(100, ErrorMessage = "InventoryItemCategory_TypeCode_MaxLen_100")]
    public string? TypeCode { get; set; }

    [Required(ErrorMessage = "InventoryItemCategory_TypeName_Required")]
    [StringLength(100, ErrorMessage = "InventoryItemCategory_TypeName_MaxLen_100")]
    public string TypeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "InventoryItemCategory_DisplayOrder_Required")]
    public int? DisplayOrder { get; set; }
    [StringLength(500, ErrorMessage = "InventoryItemCategory_Description_MaxLen_500")]
    public string? Description { get; set; }

    [Range(0, 1, ErrorMessage = "InventoryItemCategory_DepreciationRate_Range")]
    public decimal DepreciationRate { get; set; } = 0.10m;
}




