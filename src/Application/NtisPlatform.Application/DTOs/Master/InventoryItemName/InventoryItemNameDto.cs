using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;
public class InventoryItemNameDto : BaseDtos
{
    public int InventoryItemCategoryId { get; set; }
    public string SubTypeCode { get; set; } = string.Empty;
    public string SubTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
}
public class CreateInventoryItemNameDto : CreateBaseDtos
{

    [Required(ErrorMessage = "InventoryItemName_InventoryItemCategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "InventoryItemName_InventoryItemCategoryId_Required")]
    public int InventoryItemCategoryId { get; set; }

    [Required(ErrorMessage = "InventoryItemName_SubTypeCode_Required")]
    [StringLength(50, ErrorMessage = "InventoryItemName_SubTypeCode_MaxLen_50")]
    public string SubTypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "InventoryItemName_SubTypeName_Required")]
    [StringLength(50, ErrorMessage = "InventoryItemName_SubTypeName_MaxLen_50")]
    public string SubTypeName { get; set; } = string.Empty;
    [StringLength(500, ErrorMessage = "InventoryItemName_Description_MaxLen_500")]
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
}

public class UpdateInventoryItemNameDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "InventoryItemName_InventoryItemCategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "InventoryItemName_InventoryItemCategoryId_Required")]
    public int InventoryItemCategoryId { get; set; }

    [Required(ErrorMessage = "InventoryItemName_SubTypeCode_Required")]
    [StringLength(50, ErrorMessage = "InventoryItemName_SubTypeCode_MaxLen_50")]
    public string SubTypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "InventoryItemName_SubTypeName_Required")]
    [StringLength(50, ErrorMessage = "InventoryItemName_SubTypeName_MaxLen_50")]
    public string SubTypeName { get; set; } = string.Empty;
    [StringLength(500, ErrorMessage = "InventoryItemName_Description_MaxLen_500")]
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
}