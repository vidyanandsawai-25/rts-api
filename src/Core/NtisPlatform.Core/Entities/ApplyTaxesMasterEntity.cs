using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

public class ApplyTaxesMasterEntity : BaseEntity, IHardDeletable
{
    public int PropertyId { get; set; }
    public int TaxId { get; set; }
    /// <summary>
    /// Indicates whether the entity is marked for deletion
    /// </summary>

    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Date when marked for deletion
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation property
    public virtual PropertyEntity? PropertyMast { get; set; }
}
