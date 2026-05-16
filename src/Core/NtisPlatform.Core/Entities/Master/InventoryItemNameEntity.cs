namespace NtisPlatform.Core.Entities.Master;
public class InventoryItemNameEntity : BaseEntity
{
    public int InventoryItemCategoryId { get; set; }
    public string SubTypeCode { get; set; } = "";
    public string SubTypeName { get; set; } = "";
    public int DisplayOrder { get; set; } = 0;
}

