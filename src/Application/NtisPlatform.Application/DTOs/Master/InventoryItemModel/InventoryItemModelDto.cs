using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;
public class InventoryItemModelDto : BaseDtos
{
    public int InventoryItemNameId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }
}
public class CreateInventoryItemModelDto : CreateBaseDtos
{
    [Required(ErrorMessage = "InventoryItemModel_InventoryItemNameId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "InventoryItemModel_InventoryItemNameId_Required")]
    public int InventoryItemNameId { get; set; }

    [Required(ErrorMessage = "InventoryItemModel_ModelName_Required")]
    [StringLength(100, ErrorMessage = "InventoryItemModel_ModelName_MaxLen_100")]
    public string ModelName { get; set; } = string.Empty;

    [Required(ErrorMessage = "InventoryItemModel_DisplayOrder_Required")]
    public int? DisplayOrder { get; set; }
}
public class UpdateInventoryItemModelDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "InventoryItemModel_InventoryItemNameId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "InventoryItemModel_InventoryItemNameId_Required")]
    public int InventoryItemNameId { get; set; }

    [Required(ErrorMessage = "InventoryItemModel_ModelName_Required")]
    [StringLength(100, ErrorMessage = "InventoryItemModel_ModelName_MaxLen_100")]
    public string ModelName { get; set; } = string.Empty;

    [Required(ErrorMessage = "InventoryItemModel_DisplayOrder_Required")]
    public int? DisplayOrder { get; set; }
}
