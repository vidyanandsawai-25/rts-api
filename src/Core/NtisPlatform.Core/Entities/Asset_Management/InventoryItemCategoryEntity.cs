using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

public class InventoryItemCategoryEntity : BaseEntity, IHardDeletable
{
    public int AssetCategoryId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }
    public decimal DepreciationRate { get; set; } = 0.10m;
    public string? Description { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}