using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;
public class InventoryItemNameEntity : BaseEntity, IHardDeletable
{
    public int InventoryItemCategoryId { get; set; }
    public string SubTypeCode { get; set; } = "";
    public string SubTypeName { get; set; } = "";
    public int DisplayOrder { get; set; } = 0;
    public string? Description { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

