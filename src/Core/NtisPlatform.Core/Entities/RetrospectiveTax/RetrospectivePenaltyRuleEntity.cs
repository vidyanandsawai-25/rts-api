using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Unauthorized construction penalty configuration, kept separate from the
/// retrospective tax multiplier configured in <see cref="RetrospectiveRuleActionEntity"/>.
/// </summary>
[Table("RetrospectivePenaltyRule", Schema = "PTIS")]
public class RetrospectivePenaltyRuleEntity : BaseEntity
{
    public int RuleId { get; set; }

    public bool IsPenaltyApplicable { get; set; }

    /// <summary>NONE / ACT_PENALTY / DATE_VALIDATION</summary>
    public string PenaltyMode { get; set; } = "NONE";

    public decimal? PenaltyPercent { get; set; }

    /// <summary>EVIDENCE_DATE / ASSESSMENT_DATE / FIXED_DATE</summary>
    public string? PenaltyDateSourceType { get; set; }

    public int? PenaltyDateEvidenceTypeId { get; set; }

    /// <summary>ON_OR_AFTER / AFTER / ON_OR_BEFORE / BEFORE / BETWEEN</summary>
    public string? PenaltyDateCondition { get; set; }

    public DateTime? CompareDate { get; set; }

    public DateTime? CompareDateTo { get; set; }

    /// <summary>NONE / MANUAL_REVIEW</summary>
    public string? ElseAction { get; set; }

    public bool RequiresManualReview { get; set; }

    public string? Remarks { get; set; }

    public virtual RetrospectiveRuleMasterEntity? Rule { get; set; }

    public virtual EvidenceTypeMasterEntity? PenaltyDateEvidenceType { get; set; }
}
