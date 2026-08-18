using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Audit trail for rule create/update/draft/publish/deactivate/test/export actions.
/// </summary>
[Table("RetrospectiveRuleAuditLog", Schema = "PTIS")]
public class RetrospectiveRuleAuditLogEntity
{
    public long Id { get; set; }

    public int? RuleId { get; set; }

    /// <summary>CREATE / UPDATE / SAVE_DRAFT / PUBLISH / DEACTIVATE / TEST / EXPORT</summary>
    public string ActionType { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Remarks { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual RetrospectiveRuleMasterEntity? Rule { get; set; }
}
