using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a building plan type record in the PTIS system.
/// Maps to PTIS.BuildingPlanType.
/// </summary>
public class BuildingPlanTypeEntity : BaseEntity, IHardDeletable
{
    public int WardId { get; set; }

    public string PropertyNo { get; set; } = string.Empty;

    /// <summary>
    /// Building plan type value (varchar(5) in the database).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public int? DocumentBindingId { get; set; }

    public bool MarkedForDeletion { get; set; } = false;

    public DateTime? MarkedForDeletionDate { get; set; }
}
