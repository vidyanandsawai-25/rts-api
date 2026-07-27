using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

public class AssetTypeOfUseMasterEntity : BaseEntity, IHardDeletable
{
    public int AssetCategoryId { get; set; }          
    public int AssetTypeId { get; set; }              
    public string TypeOfUseCode { get; set; } = string.Empty; 
    public string? Description { get; set; }        
    public string? Type { get; set; }               
    public int? TypeOfUseGroupId { get; set; }      
    public int? SearchSequence { get; set; }        
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
