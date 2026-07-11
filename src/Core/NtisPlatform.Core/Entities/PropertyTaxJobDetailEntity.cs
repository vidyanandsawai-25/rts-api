using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Per-property execution row for a <see cref="PropertyTaxJobEntity"/>. Captures the outcome
/// (added / failed / skipped) and a display snapshot for the Audit &amp; Monitor screen.
/// </summary>
public class PropertyTaxJobDetailEntity : BaseEntity, IHardDeletable
{
    public int JobId { get; set; }
    public int PropertyId { get; set; }

    // Display snapshots (point-in-time, for the audit grid)
    public string? PropertyNo { get; set; }
    public string? TaxHead { get; set; }
    public decimal? Amount { get; set; }

    public DateTime? ExecutionStartTime { get; set; }
    public DateTime? ExecutionEndTime { get; set; }

    public string Status { get; set; } = "Pending"; // see JobDetailStatus
    public string? SkipReason { get; set; }          // see SkipReason; null unless skipped
    public string? Message { get; set; }

    // IHardDeletable
    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }
}
