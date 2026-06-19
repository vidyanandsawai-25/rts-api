using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;
public class PolicyTaxDetailsEntity : BaseEntity, IHardDeletable
{

    public virtual PropertyEntity? PropertyMast { get; set; }
    public virtual TaxMasterEntity? TaxMaster { get; set; }
    
    public int PropertyId { get; set; }
    
    public string PolicyCode { get; set; } = string.Empty;
    
    public DateTime? PolicyDate { get; set; }
    
    public short? PolicyYear { get; set; }   
    public string? PolicyReason { get; set; }
    
    public decimal? PolicyRVorCVvalue { get; set; }
    
    public int TaxId { get; set; }
    
    public decimal? TaxAmount { get; set; }

 
    public bool MarkedForDeletion { get; set; } = false;
    
    public DateTime? MarkedForDeletionDate { get; set; }
    
    // Navigation properties
   
}