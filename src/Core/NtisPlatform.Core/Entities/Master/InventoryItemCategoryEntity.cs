namespace NtisPlatform.Core.Entities.Master;
public class InventoryItemCategoryEntity : BaseEntity
{
    public string? TypeCode { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}