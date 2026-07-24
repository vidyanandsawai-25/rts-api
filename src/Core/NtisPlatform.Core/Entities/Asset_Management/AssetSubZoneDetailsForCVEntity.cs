using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

public class AssetSubZoneDetailsForCVEntity : BaseEntity, IHardDeletable
{
    public int MoujaId { get; set; }
    public string SubZoneNo { get; set; } = string.Empty;
    public string SubZoneName { get; set; } = string.Empty;
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
