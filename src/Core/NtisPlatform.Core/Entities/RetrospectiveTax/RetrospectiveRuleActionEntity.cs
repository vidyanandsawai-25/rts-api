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

    /// <summary>
    /// SINGLE (one flat TaxMultiplier for the whole period) / SPLIT (multiplier changes at
    /// SplitEndEvidenceTypeId's date, same chargeable years as TaxStartMode would give alone) /
    /// CC_THEN_OC_MERGE (SplitEndEvidenceTypeId, e.g. OC, can move which finance years are even
    /// chargeable -- TaxStartMode's own evidence, e.g. CC, governs every year before OC's onset
    /// year at SplitMultiplier; OC governs its onset year onward at AfterSplitMultiplier; the
    /// onset year itself splits by day between the two).
    /// </summary>
    public string TaxCalculationMode { get; set; } = "SINGLE";

    public decimal TaxMultiplier { get; set; } = 1.00m;

    /// <summary>
    /// YEAR_WISE (use each retrospective year's own historical rate/tax%) / CURRENT_YEAR (use the
    /// current assessment year's rate/tax% for every retrospective year).
    /// </summary>
    public string RateMode { get; set; } = "YEAR_WISE";

    public int? SplitStartEvidenceTypeId { get; set; }

    public int? SplitEndEvidenceTypeId { get; set; }

    public decimal? SplitMultiplier { get; set; }

    public decimal? AfterSplitMultiplier { get; set; }

    public virtual RetrospectiveRuleMasterEntity? Rule { get; set; }

    public virtual EvidenceTypeMasterEntity? StartEvidenceType { get; set; }

    public virtual EvidenceTypeMasterEntity? SplitStartEvidenceType { get; set; }

    public virtual EvidenceTypeMasterEntity? SplitEndEvidenceType { get; set; }
}
