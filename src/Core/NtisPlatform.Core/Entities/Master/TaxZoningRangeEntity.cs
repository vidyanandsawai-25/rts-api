using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a persisted, named property-number-range (or whole-ward) tax zone assignment.
/// One row governs anywhere from a single property to an entire ward's worth of properties —
/// the actual <c>PTIS.PropertyMast</c> rows it covers are resolved via a matching predicate
/// (natural-sort range match on <see cref="FromPropertyNo"/>/<see cref="ToPropertyNo"/>, or
/// unconditionally when <see cref="AssignEntireWard"/> is set) rather than an FK list.
/// </summary>
public class TaxZoningRangeEntity : BaseEntity, IHardDeletable
{
    public int WardId { get; set; }

    public int TaxZoneId { get; set; }

    /// <summary>Null when <see cref="AssignEntireWard"/> is true.</summary>
    public string? FromPropertyNo { get; set; }

    /// <summary>Null when <see cref="AssignEntireWard"/> is true.</summary>
    public string? ToPropertyNo { get; set; }

    public bool AssignEntireWard { get; set; }

    public string ZoneDescription { get; set; } = string.Empty;

    // IHardDeletable
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    public virtual WardEntity? Ward { get; set; }
    public virtual TaxZoneEntity? TaxZone { get; set; }
}
