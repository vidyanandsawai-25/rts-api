using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

public class AssetMoujaMasterEntity : BaseEntity, IHardDeletable
{
    public string MoujaNo { get; set; } = string.Empty;
    public string MoujaName { get; set; } = string.Empty;
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
