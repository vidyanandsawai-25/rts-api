namespace NtisPlatform.Core.Entities.Rules;

/// <summary>
/// Represents version history for rule engine master configurations
/// Tracks all changes to rules for audit and rollback purposes
/// </summary>
public class RuleVersionHistoryEntity
{
    public long Id { get; set; }
    public int RuleId { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public int Version { get; set; }

    // Snapshot of Rule at this version
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RuleJson { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsEnabled { get; set; }

    // Change Metadata
    public string ChangeType { get; set; } = string.Empty; // 'CREATED', 'UPDATED', 'DELETED', 'ENABLED', 'DISABLED'
    public string? ChangeReason { get; set; }
    public int ChangedBy { get; set; }
    public DateTime ChangedDate { get; set; }

    // Optional: What changed (for quick comparison)
    public string? ChangeSummary { get; set; }

    // Navigation property
    public virtual RuleEngineEntity? RuleEngine { get; set; }
}
