using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;
public class InventoryItemModelDto : BaseDtos
{
    public int InventoryItemNameId { get; set; }
    public string? InventoryItemName { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
}
public class CreateInventoryItemModelDto : CreateBaseDtos
{
    [Required(ErrorMessage = "InventoryItemModel_InventoryItemNameId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "InventoryItemModel_InventoryItemNameId_Required")]
    public int InventoryItemNameId { get; set; }

    [Required(ErrorMessage = "InventoryItemModel_ModelName_Required")]
    [StringLength(100, ErrorMessage = "InventoryItemModel_ModelName_MaxLen_100")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "InventoryItemModel_ModelName_Invalid")]
    public string ModelName { get; set; } = string.Empty;

    [Required(ErrorMessage = "InventoryItemModel_DisplayOrder_Required")]
    public int? DisplayOrder { get; set; }
    [StringLength(500, ErrorMessage = "InventoryItemModel_Description_MaxLen_500")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "InventoryItemModel_Description_Invalid")]
    public string? Description { get; set; }

}
public class UpdateInventoryItemModelDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "InventoryItemModel_InventoryItemNameId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "InventoryItemModel_InventoryItemNameId_Required")]
    public int InventoryItemNameId { get; set; }

    [Required(ErrorMessage = "InventoryItemModel_ModelName_Required")]
    [StringLength(100, ErrorMessage = "InventoryItemModel_ModelName_MaxLen_100")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "InventoryItemModel_ModelName_Invalid")]
    public string ModelName { get; set; } = string.Empty;

    [Required(ErrorMessage = "InventoryItemModel_DisplayOrder_Required")]
    public int? DisplayOrder { get; set; }
    [StringLength(500, ErrorMessage = "InventoryItemModel_Description_MaxLen_500")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "InventoryItemModel_Description_Invalid")]
    public string? Description { get; set; }

}
