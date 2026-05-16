using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;
public class InventoryItemCategoryDto : BaseDtos
{
    public string? TypeCode { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }
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
}
public class UpdateInventoryItemCategoryDto : UpdateBaseDtos
{
    [StringLength(100, ErrorMessage = "InventoryItemCategory_TypeCode_MaxLen_100")]
    public string? TypeCode { get; set; }

    [Required(ErrorMessage = "InventoryItemCategory_TypeName_Required")]
    [StringLength(100, ErrorMessage = "InventoryItemCategory_TypeName_MaxLen_100")]
    public string TypeName { get; set; }  = string.Empty;

    [Required(ErrorMessage = "InventoryItemCategory_DisplayOrder_Required")]
    public int? DisplayOrder { get; set; }
}




