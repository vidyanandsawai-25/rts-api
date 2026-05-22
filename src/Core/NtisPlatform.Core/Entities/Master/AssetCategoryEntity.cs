using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

public class AssetCategoryEntity : BaseEntity, IHardDeletable
{
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryCode { get; set; }
    public string? Description { get; set; }

    // IHardDeletable implementation
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}