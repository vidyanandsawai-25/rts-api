using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Action part of a rule: retrospective tax start point, look-back limit and multiplier(s).
/// </summary>
[Table("RetrospectiveRuleAction", Schema = "PTIS")]
public class RetrospectiveRuleActionEntity : BaseEntity
{
    public int RuleId { get; set; }

    /// <summary>
    /// EVIDENCE_DATE / FY_START / NEXT_FINANCIAL_YEAR / MONTHS_AFTER / FIXED_CUTOFF /
    /// MAX_LOOK_BACK_DATE / CONSTRUCTION_YEAR / CONSTRUCTION_OR_CAP
    /// </summary>
    public string TaxStartMode { get; set; } = string.Empty;

    public int? StartEvidenceTypeId { get; set; }

    public int? OffsetMonths { get; set; }

    /// <summary>MAXIMUM_YEARS / FIXED_CUTOFF_DATE / NONE</summary>
    public string RetrospectiveLimitType { get; set; } = string.Empty;

    public int? MaximumYears { get; set; }

    public DateTime? CutoffDate { get; set; }

    /// <summary>SINGLE / SPLIT</summary>
    public string TaxCalculationMode { get; set; } = "SINGLE";

    public decimal TaxMultiplier { get; set; } = 1.00m;

    public int? SplitStartEvidenceTypeId { get; set; }

    public int? SplitEndEvidenceTypeId { get; set; }

    public decimal? SplitMultiplier { get; set; }

    public decimal? AfterSplitMultiplier { get; set; }

    public virtual RetrospectiveRuleMasterEntity? Rule { get; set; }

    public virtual EvidenceTypeMasterEntity? StartEvidenceType { get; set; }

    public virtual EvidenceTypeMasterEntity? SplitStartEvidenceType { get; set; }

    public virtual EvidenceTypeMasterEntity? SplitEndEvidenceType { get; set; }
}
