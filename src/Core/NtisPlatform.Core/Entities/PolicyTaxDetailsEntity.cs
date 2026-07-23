using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;
public class PolicyTaxDetailsEntity : BaseEntity, IHardDeletable
{

    public virtual PropertyEntity? PropertyMast { get; set; }
    public virtual TaxMasterEntity? TaxMaster { get; set; }
    public virtual PolicyCodeMasterEntity? PolicyCodeMaster { get; set; }
    
    public int PropertyId { get; set; }
    public int PolicyCodeId { get; set; }   
    
    public decimal? CalculationValue { get; set; }   
    
    public int TaxId { get; set; }
    
    public decimal? TaxAmount { get; set; } 
    public bool MarkedForDeletion { get; set; } = false;
    
    public DateTime? MarkedForDeletionDate { get; set; }
    
    // Navigation properties
   
}