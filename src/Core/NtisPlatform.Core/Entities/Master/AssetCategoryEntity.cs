using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

public class AssetCategoryEntity : BaseEntity, IHardDeletable
{
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ValuationType { get; set; } = "GENERIC";

    public bool IsMovable { get; set; }
    public bool HasFloorDetails { get; set; }
    public bool HasInventory { get; set; }
    public bool IsInventoryMandatory { get; set; }
    public bool HasLegalCompliance { get; set; }

    // IHardDeletable implementation
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
