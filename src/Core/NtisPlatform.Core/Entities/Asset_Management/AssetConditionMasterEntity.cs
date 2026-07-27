using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Master of asset condition grades (e.g. Excellent, Good, Fair, Poor, Dilapidated).
/// AssetMaster.AssetConditionId references this table (AMS.AssetConditionMaster).
/// ConditionCategory is a discriminator ('Asset' | 'Inventory'); CategoryId is polymorphic —
/// for 'Asset' rows it is the AssetCategoryMaster.Id the condition scale applies to (e.g. a
/// building's condition scale differs from land's), for 'Inventory' rows it is
/// InventoryItemCategoryMaster.Id.
/// </summary>
public class AssetConditionMasterEntity : BaseEntity, IHardDeletable
{
    public string ConditionCategory { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string ConditionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? ConditionFactor { get; set; }
    public int? DisplayOrder { get; set; }

    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
