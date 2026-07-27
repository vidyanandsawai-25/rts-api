using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

public class AssetTypeOfUseGroupEntity : BaseEntity, IHardDeletable
{
    public string TypeOfUseGroupCode { get; set; } = string.Empty;  
    public string GroupName { get; set; } = string.Empty;           
    public string GroupIcon { get; set; } = string.Empty;           
    public bool IsFloorWiseRateApplicable { get; set; }             
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
