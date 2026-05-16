using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;
public class InventoryItemConditionDto : BaseDtos
{
    public int InventoryItemCategoryId { get; set; }
    public string ConditionName { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }
}

public class CreateInventoryItemConditionMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "InventoryItemCondition_InventoryItemCategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "InventoryItemCondition_InventoryItemCategoryId_Required")]
    public int InventoryItemCategoryId { get; set; }

    [Required(ErrorMessage = "InventoryItemCondition_ConditionName_Required")]
    [StringLength(100, ErrorMessage = "InventoryItemCondition_ConditionName_MaxLen_100")]
    public string ConditionName { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }
}

public class UpdateInventoryItemConditionMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "InventoryItemCondition_InventoryItemCategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "InventoryItemCondition_InventoryItemCategoryId_Required")]
    public int InventoryItemCategoryId { get; set; }

    [Required(ErrorMessage = "InventoryItemCondition_ConditionName_Required")]
    [StringLength(100, ErrorMessage = "InventoryItemCondition_ConditionName_MaxLen_100")]
    public string ConditionName { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }
}
