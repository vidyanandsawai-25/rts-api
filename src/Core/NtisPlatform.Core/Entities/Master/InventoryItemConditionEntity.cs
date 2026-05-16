namespace NtisPlatform.Core.Entities.Master;
public class InventoryItemConditionEntity : BaseEntity
{
    public int InventoryItemCategoryId { get; set; }
    public string ConditionName { get; set; } = "";
    public int DisplayOrder { get; set; } = 0;
}
