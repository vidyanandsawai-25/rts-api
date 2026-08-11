using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;
public class InventoryItemModelEntity : BaseEntity, IHardDeletable
{
    public int InventoryItemNameId { get; set; }
    public string ModelName { get; set; } = "";
    public int DisplayOrder { get; set; } = 0;
    public string? Description { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
