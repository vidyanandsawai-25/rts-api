using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

public class AssetSubTypeOfUseEntity : BaseEntity, IHardDeletable
{
    public int TypeOfUseId { get; set; }             
    public string Description { get; set; } = string.Empty;  
    public int? SearchSequence { get; set; }          
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
