using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

public class TaxPendingDetailsRetroEntity : BaseEntity, IHardDeletable
{
    public int PropertyId { get; set; }
    
    public bool MarkedForDeletion { get; set; } = false;
    
    public DateTime? MarkedForDeletionDate { get; set; }
    
    // Navigation property
    public virtual PropertyEntity? PropertyMast { get; set; }
}
