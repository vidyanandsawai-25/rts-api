using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAuditLog;

/// <summary>
/// Custom DTOs (not <see cref="BaseDtos"/>): RetrospectiveRuleAuditLogEntity uses a BIGINT key
/// and has no IsActive/UpdatedBy/UpdatedDate columns — it is an append-only audit trail.
/// </summary>
public class RetrospectiveRuleAuditLogDto
{
    public long Id { get; set; }
    public int? RuleId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateRetrospectiveRuleAuditLogDto
{
    public int? RuleId { get; set; }

    /// <summary>CREATE / UPDATE / SAVE_DRAFT / PUBLISH / DEACTIVATE / TEST / EXPORT</summary>
    [Required(ErrorMessage = "RetrospectiveRuleAuditLog_ActionType_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveRuleAuditLog_ActionType_MaxLen_50")]
    public string ActionType { get; set; } = string.Empty;

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleAuditLog_Remarks_MaxLen_1000")]
    public string? Remarks { get; set; }

    public int? CreatedBy { get; set; }
}

public class UpdateRetrospectiveRuleAuditLogDto
{
    public int? RuleId { get; set; }

    [Required(ErrorMessage = "RetrospectiveRuleAuditLog_ActionType_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveRuleAuditLog_ActionType_MaxLen_50")]
    public string ActionType { get; set; } = string.Empty;

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleAuditLog_Remarks_MaxLen_1000")]
    public string? Remarks { get; set; }
}
