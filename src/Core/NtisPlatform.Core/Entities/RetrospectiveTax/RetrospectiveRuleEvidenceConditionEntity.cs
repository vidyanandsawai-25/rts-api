using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Selected available/unavailable evidence state configured per rule.
/// </summary>
[Table("RetrospectiveRuleEvidenceCondition", Schema = "PTIS")]
public class RetrospectiveRuleEvidenceConditionEntity : BaseEntity
{
    public int RuleId { get; set; }

    public int EvidenceTypeId { get; set; }

    /// <summary>AVAILABLE / UNAVAILABLE</summary>
    public string EvidenceState { get; set; } = string.Empty;

    public virtual RetrospectiveRuleMasterEntity? Rule { get; set; }

    public virtual EvidenceTypeMasterEntity? EvidenceType { get; set; }
}
