using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

public class PropertyTaxCalculationSection129ResultsEntity : BaseEntity,IHardDeletable
{
    public int PropertyId { get; set; }

    public int PropertyDetailsId { get; set; }
    /// <summary>
    /// Indicates whether the entity is marked for deletion
    /// </summary>

    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Date when marked for deletion
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation properties
    public virtual PropertyEntity? PropertyMast { get; set; }
    public virtual PropertyDetailsEntity? PropertyDetails { get; set; }
}
