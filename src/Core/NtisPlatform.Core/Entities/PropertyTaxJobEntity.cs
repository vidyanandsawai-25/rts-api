using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a single bulk property-tax operation execution (e.g. Add Tax) run across a
/// set of properties chosen by scope. Example JobCode: JOB-ADD-2025-0001.
/// </summary>
public class PropertyTaxJobEntity : BaseEntity, IHardDeletable
{
    // Identity / classification
    public string JobCode { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty; // see JobOperation (Application layer)

    // Finance period
    public int FinanceYearId { get; set; }
    public virtual YearMasterEntity FinanceYear { get; set; } = null!;

    // Scope
    public string ScopeType { get; set; } = string.Empty; // see JobScopeType
    public string? ScopeParamsJson { get; set; }
    public string? ScopeDescription { get; set; }

    // Actor (business "started by" — distinct from the row's CreatedBy)
    public int StartedByUserId { get; set; }
    public string? StartedByUserName { get; set; }
    public string? UserRole { get; set; }

    // Timing
    public DateTime StartTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public long? DurationMs { get; set; }

    // Counters
    public int RecordsSelected { get; set; }
    public int RecordsProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }

    // Status / messaging
    public string Status { get; set; } = "Pending"; // see JobStatus
    public string? ErrorMessage { get; set; }
    public string? Remarks { get; set; }

    // IHardDeletable
    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }
}
