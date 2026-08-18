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
    /// ELECTRICITY_BEFORE_CC / ELECTRICITY_AFTER_CC / ELECTRICITY_BEFORE_CUTOFF / ELECTRICITY_AFTER_CUTOFF
    /// </summary>
    public string ComparatorCode { get; set; } = string.Empty;

    public int? LeftEvidenceTypeId { get; set; }

    public int? RightEvidenceTypeId { get; set; }

    /// <summary>BEFORE / AFTER / ON_OR_BEFORE / ON_OR_AFTER / BETWEEN / OLDER_THAN_YEARS / WITHIN_YEARS</summary>
    public string? CompareOperator { get; set; }

    public DateTime? CompareDate { get; set; }

    public DateTime? CompareDateTo { get; set; }

    public int? CompareYears { get; set; }

    public virtual RetrospectiveRuleMasterEntity? Rule { get; set; }

    public virtual EvidenceTypeMasterEntity? LeftEvidenceType { get; set; }

    public virtual EvidenceTypeMasterEntity? RightEvidenceType { get; set; }
}
