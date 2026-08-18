using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleMaster;

/// <summary>
/// Request body for the "Publish Rule" button. Publishing moves RuleStatus from
/// Draft/Review/NeedsClarification to Active and writes a PUBLISH row to
/// RetrospectiveRuleAuditLog so the rule's publish history is auditable.
/// </summary>
public class PublishRetrospectiveRuleDto
{
    public int? PublishedBy { get; set; }

    /// <summary>Optional note stored on the audit log row, e.g. an approval/resolution reference.</summary>
    [StringLength(1000, ErrorMessage = "RetrospectiveRuleMaster_Publish_Remarks_MaxLen_1000")]
    public string? Remarks { get; set; }
}
