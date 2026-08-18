using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectivePenaltyRule;

namespace NtisPlatform.Application.Constants.RetrospectiveTax;

/// <summary>
/// Static option lists for the CHECK-constraint-backed dropdowns on RetrospectivePenaltyRule
/// (PenaltyMode, PenaltyDateSourceType, PenaltyDateCondition). Kept in one place so the API
/// contract for these fields (code sent to the server + label shown to the user + which extra
/// input the form needs) can't drift out of sync between backend and frontend.
/// </summary>
public static class RetrospectivePenaltyRuleOptions
{
    /// <summary>
    /// Options for the "Penalty rule" dropdown (PenaltyMode). This section only shows when both
    /// OC and CC evidence are UNAVAILABLE for the rule — check
    /// GET api/RetrospectiveRuleEvidenceCondition/rule/{ruleId}/evidence-state and confirm both
    /// evidence types have SelectedState = "UNAVAILABLE" before displaying it.
    /// </summary>
    public static IReadOnlyList<RetrospectivePenaltyRuleOptionDto> PenaltyModes { get; } = new[]
    {
        new RetrospectivePenaltyRuleOptionDto { Code = "NONE", Label = "Do not apply penalty", RequiredInput = "NONE" },
        new RetrospectivePenaltyRuleOptionDto { Code = "ACT_UNLAWFUL", Label = "Apply penalty as per the Act", RequiredInput = "OPTIONAL_PERCENT" },
        new RetrospectivePenaltyRuleOptionDto { Code = "DATE_VALIDATION", Label = "Apply penalty based on a date", RequiredInput = "DATE_CONDITION" },
    };

    /// <summary>Options shown when PenaltyMode = DATE_VALIDATION, for "which date to check".</summary>
    public static IReadOnlyList<RetrospectivePenaltyRuleOptionDto> PenaltyDateSourceTypes { get; } = new[]
    {
        new RetrospectivePenaltyRuleOptionDto { Code = "EVIDENCE_DATE", Label = "Evidence date", RequiredInput = "EVIDENCE_TYPE" },
        new RetrospectivePenaltyRuleOptionDto { Code = "ASSESSMENT_DATE", Label = "Assessment date", RequiredInput = "NONE" },
        new RetrospectivePenaltyRuleOptionDto { Code = "FIXED_DATE", Label = "Fixed date", RequiredInput = "COMPARE_DATE" },
    };

    /// <summary>Options shown when PenaltyMode = DATE_VALIDATION, for "how to compare the date".</summary>
    public static IReadOnlyList<RetrospectivePenaltyRuleOptionDto> PenaltyDateConditions { get; } = new[]
    {
        new RetrospectivePenaltyRuleOptionDto { Code = "ON_OR_AFTER", Label = "On or after", RequiredInput = "COMPARE_DATE" },
        new RetrospectivePenaltyRuleOptionDto { Code = "AFTER", Label = "After", RequiredInput = "COMPARE_DATE" },
        new RetrospectivePenaltyRuleOptionDto { Code = "ON_OR_BEFORE", Label = "On or before", RequiredInput = "COMPARE_DATE" },
        new RetrospectivePenaltyRuleOptionDto { Code = "BEFORE", Label = "Before", RequiredInput = "COMPARE_DATE" },
        new RetrospectivePenaltyRuleOptionDto { Code = "BETWEEN", Label = "Between", RequiredInput = "COMPARE_DATE_RANGE" },
    };
}
