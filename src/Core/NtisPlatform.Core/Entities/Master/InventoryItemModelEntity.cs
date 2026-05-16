namespace NtisPlatform.Core.Entities.Master;
public class InventoryItemModelEntity : BaseEntity
{
    public int InventoryItemNameId { get; set; }
    public string ModelName { get; set; } = "";
    public int DisplayOrder { get; set; } = 0;
}
