using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAuditLog;

/// <summary>
/// MarkedForDeletion/MarkedForDeletionDate are system-managed via the Purge endpoints, not user
/// input, so they're declared directly here rather than in <see cref="BaseDtos"/>.
/// </summary>
public class RetrospectiveRuleAuditLogDto : BaseDtos
{
    public int? RuleId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Remarks { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreateRetrospectiveRuleAuditLogDto : CreateBaseDtos
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
}

public class UpdateRetrospectiveRuleAuditLogDto : UpdateBaseDtos
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
