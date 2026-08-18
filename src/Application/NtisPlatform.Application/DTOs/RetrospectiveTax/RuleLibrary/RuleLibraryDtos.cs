using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RuleLibrary;

/// <summary>
/// Query parameters for the "Corporation Rule Library" grid. Same paging/sorting/search as every
/// other master list (SearchTerm matches RuleCode or RuleName), plus a RuleStatus filter for the
/// STATUS column's implicit filter chips (Active / Review / NeedsClarification / Draft).
/// </summary>
public class RuleLibraryQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    public string? RuleStatus { get; set; }

    /// <summary>Not a real filter field — [Searchable] only, so SearchTerm matches RuleCode.</summary>
    [Searchable(EntityProperty = "RuleCode")]
    public string? RuleCode { get; set; }

    /// <summary>Not a real filter field — [Searchable] only, so SearchTerm matches RuleName.</summary>
    [Searchable(EntityProperty = "RuleName")]
    public string? RuleName { get; set; }
}

/// <summary>
/// One row of the Rule Library grid. Every text field here is composed live from the rule's own
/// structured configuration (RetrospectiveRuleAction / RetrospectivePenaltyRule / EvidenceTypeMaster)
/// — nothing here depends on RetrospectiveRuleSummary being pre-generated.
/// </summary>
public class RuleLibraryRowDto
{
    public int Id { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string RuleStatus { get; set; } = string.Empty;
    public string? AuthorizationStatus { get; set; }

    /// <summary>CONDITION column, bold line — RetrospectiveRuleMaster.RuleDescription verbatim.</summary>
    public string? ConditionDescription { get; set; }

    /// <summary>
    /// CONDITION column, small line, e.g. "Authorized: OC or CC available" /
    /// "Unauthorized: OC &amp; CC unavailable" — a fixed label per AuthorizationStatus (mirrors the
    /// AuthorizationStatus doc comments on RetrospectiveRuleMasterEntity, not per-rule evidence data).
    /// </summary>
    public string? ConditionTag { get; set; }

    /// <summary>
    /// START LOGIC column, main line, e.g. "From OC date", "6 months after Electricity date".
    /// Composed from RetrospectiveRuleAction.TaxStartMode + StartEvidenceTypeId/OffsetMonths/CutoffDate.
    /// Null if the rule has no RetrospectiveRuleAction row yet.
    /// </summary>
    public string? StartLogicSummary { get; set; }

    /// <summary>
    /// START LOGIC column, small line, e.g. "Boundary: 6 years", "Boundary: 2024-09-01". Composed
    /// from RetrospectiveRuleAction.RetrospectiveLimitType + MaximumYears/CutoffDate. Null when
    /// RetrospectiveLimitType = NONE or there's no RetrospectiveRuleAction row.
    /// </summary>
    public string? StartLogicBoundary { get; set; }

    /// <summary>
    /// COMMON TAXATION column, small line under the shared badge — per-rule multiplier note, e.g.
    /// "Retrospective tax x 1.5" (single mode, multiplier != 1) or "1.5x from CC date to OC date,
    /// then 1x" (split mode). Null for a plain x1 single multiplier.
    /// </summary>
    public string? TaxMultiplierNote { get; set; }

    /// <summary>
    /// UNAUTHORIZED PENALTY column, e.g. "Not applicable - OC/CC available", "Apply penalty as
    /// per the Act", "Apply when Electricity date is on or after 03 Mar 2026". Composed from
    /// AuthorizationStatus + RetrospectivePenaltyRule (PenaltyMode/PenaltyDateSourceType/
    /// PenaltyDateCondition/CompareDate/CompareDateTo).
    /// </summary>
    public string? PenaltySummary { get; set; }
}

/// <summary>
/// Shared "Common Taxation" badge shown on every row — sourced from the single active
/// RetrospectiveTaxPolicy row (see the Taxation Rate &amp; Percentage screen), not per-rule.
/// Null when no RetrospectiveTaxPolicy is active yet.
/// </summary>
public class RuleLibraryCommonTaxationDto
{
    public string? RateModeCode { get; set; }
    public string? RateModeLabel { get; set; }
    public string? PercentageModeCode { get; set; }
    public string? PercentageModeLabel { get; set; }
}

/// <summary>Top-level response for GET api/RuleLibrary.</summary>
public class RuleLibraryDto
{
    public RuleLibraryCommonTaxationDto? CommonTaxation { get; set; }
    public PagedResult<RuleLibraryRowDto> Rules { get; set; } = new();
}
