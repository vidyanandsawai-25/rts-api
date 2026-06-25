using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a building plan type record in the PTIS system.
/// Maps to PTIS.BuildingPlanType.
/// </summary>
public class BuildingPlanTypeEntity : BaseEntity, IHardDeletable
{
    public int PropertyId { get; set; }

    /// <summary>
    /// Building plan type value (varchar(5) in the database).
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Optional reference to a CORE.Document. Nullable; left null when no document is associated.
    /// </summary>
    public Guid? DocumentGuid { get; set; }

    public bool MarkedForDeletion { get; set; } = false;

    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation property to the owning property.
    public virtual PropertyEntity? Property { get; set; }
}
