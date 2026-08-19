using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Audit trail for rule create/update/draft/publish/deactivate/test/export actions.
/// </summary>
[Table("RetrospectiveRuleAuditLog", Schema = "PTIS")]
public class RetrospectiveRuleAuditLogEntity : BaseEntity, IHardDeletable
{
    public int? RuleId { get; set; }

    /// <summary>CREATE / UPDATE / SAVE_DRAFT / PUBLISH / DEACTIVATE / TEST / EXPORT</summary>
    public string ActionType { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Remarks { get; set; }

    public bool MarkedForDeletion { get; set; }

    public DateTime? MarkedForDeletionDate { get; set; }

    public virtual RetrospectiveRuleMasterEntity? Rule { get; set; }
}
