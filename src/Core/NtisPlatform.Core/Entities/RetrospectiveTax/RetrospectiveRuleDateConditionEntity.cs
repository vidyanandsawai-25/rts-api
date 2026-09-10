using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Optional date comparison/comparator selected for a rule
/// (e.g. OC older than allowed period, Electricity before CC).
/// </summary>
[Table("RetrospectiveRuleDateCondition", Schema = "PTIS")]
public class RetrospectiveRuleDateConditionEntity : BaseEntity
{
    public int RuleId { get; set; }

    /// <summary>
    /// NONE / OC_OLDER_THAN_ALLOWED_PERIOD / OC_WITHIN_ALLOWED_PERIOD /
    /// ELECTRICITY_BEFORE_CC / ELECTRICITY_AFTER_CC / ELECTRICITY_BEFORE_CUTOFF / ELECTRICITY_AFTER_CUTOFF /
    /// EVIDENCE_GAP_WITHIN_PERIOD (compares the gap between LeftEvidenceTypeId's and
    /// RightEvidenceTypeId's dates against CompareYears in CompareGapUnit units, e.g. "CC to OC gap
    /// within/older than 6 months" -- via CompareOperator = WITHIN_YEARS / OLDER_THAN_YEARS)
    /// </summary>
    public string ComparatorCode { get; set; } = string.Empty;

    public int? LeftEvidenceTypeId { get; set; }

    public int? RightEvidenceTypeId { get; set; }

    /// <summary>BEFORE / AFTER / ON_OR_BEFORE / ON_OR_AFTER / BETWEEN / OLDER_THAN_YEARS / WITHIN_YEARS</summary>
    public string? CompareOperator { get; set; }

    public DateTime? CompareDate { get; set; }

    public DateTime? CompareDateTo { get; set; }

    /// <summary>
    /// The allowed-period threshold. Its unit is YEARS for OC_OLDER_THAN_ALLOWED_PERIOD /
    /// OC_WITHIN_ALLOWED_PERIOD (fixed, historical meaning); for EVIDENCE_GAP_WITHIN_PERIOD the
    /// unit is whichever <see cref="CompareGapUnit"/> specifies.
    /// </summary>
    public int? CompareYears { get; set; }

    /// <summary>DAYS / MONTHS / YEARS — only consulted by EVIDENCE_GAP_WITHIN_PERIOD, to interpret CompareYears.</summary>
    public string? CompareGapUnit { get; set; }

    public virtual RetrospectiveRuleMasterEntity? Rule { get; set; }

    public virtual EvidenceTypeMasterEntity? LeftEvidenceType { get; set; }

    public virtual EvidenceTypeMasterEntity? RightEvidenceType { get; set; }
}
