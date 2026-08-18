using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAction;

namespace NtisPlatform.Application.Constants.RetrospectiveTax;

/// <summary>
/// Static option lists for the CHECK-constraint-backed dropdowns on RetrospectiveRuleAction
/// (TaxStartMode, RetrospectiveLimitType, TaxCalculationMode). Kept in one place so the API
/// contract for these fields (code sent to the server + label shown to the user + which extra
/// input the form needs) can't drift out of sync between backend and frontend.
/// </summary>
public static class RetrospectiveRuleActionOptions
{
    /// <summary>Options for the "Tax starts from" dropdown (TaxStartMode).</summary>
    public static IReadOnlyList<RetrospectiveRuleActionOptionDto> TaxStartModes { get; } = new[]
    {
        new RetrospectiveRuleActionOptionDto { Code = "EVIDENCE_DATE", Label = "Selected evidence date", RequiredInput = "EVIDENCE_TYPE" },
        new RetrospectiveRuleActionOptionDto { Code = "FY_START", Label = "1 April of evidence year", RequiredInput = "EVIDENCE_TYPE" },
        new RetrospectiveRuleActionOptionDto { Code = "NEXT_FINANCIAL_YEAR", Label = "Next financial year", RequiredInput = "EVIDENCE_TYPE" },
        new RetrospectiveRuleActionOptionDto { Code = "MONTHS_AFTER", Label = "After specified months", RequiredInput = "EVIDENCE_TYPE_AND_MONTHS" },
        new RetrospectiveRuleActionOptionDto { Code = "FIXED_CUTOFF", Label = "Fixed policy date", RequiredInput = "CUTOFF_DATE" },
        new RetrospectiveRuleActionOptionDto { Code = "MAX_LOOK_BACK_DATE", Label = "Maximum look-back date", RequiredInput = "NONE" },
        new RetrospectiveRuleActionOptionDto { Code = "CONSTRUCTION_YEAR", Label = "Construction date/year", RequiredInput = "NONE" },
        new RetrospectiveRuleActionOptionDto { Code = "CONSTRUCTION_OR_CAP", Label = "Later of construction date or limit", RequiredInput = "NONE" },
    };

    /// <summary>
    /// Options for the "Retrospective limit" dropdown (RetrospectiveLimitType). "Earliest
    /// chargeable date" and TaxStartMode's "Fixed policy date" share the same CutoffDate column —
    /// if both are set to their date-driven option at once, they use the same stored date.
    /// </summary>
    public static IReadOnlyList<RetrospectiveRuleActionOptionDto> RetrospectiveLimitTypes { get; } = new[]
    {
        new RetrospectiveRuleActionOptionDto { Code = "MAXIMUM_YEARS", Label = "Maximum years", RequiredInput = "MAXIMUM_YEARS" },
        new RetrospectiveRuleActionOptionDto { Code = "FIXED_CUTOFF_DATE", Label = "Earliest chargeable date", RequiredInput = "CUTOFF_DATE" },
        new RetrospectiveRuleActionOptionDto { Code = "NONE", Label = "No additional corporation limit", RequiredInput = "NONE" },
    };

    /// <summary>
    /// Options for the "Tax calculation" dropdown (TaxCalculationMode) — one flat multiplier for
    /// the whole retrospective period, or a multiplier that changes partway through (e.g. x1.5
    /// from CC date to OC date, then x1 after OC date).
    /// </summary>
    public static IReadOnlyList<RetrospectiveRuleActionOptionDto> TaxCalculationModes { get; } = new[]
    {
        new RetrospectiveRuleActionOptionDto { Code = "SINGLE", Label = "One multiplier for entire period", RequiredInput = "SINGLE_MULTIPLIER" },
        new RetrospectiveRuleActionOptionDto { Code = "SPLIT", Label = "Different multiplier between two dates", RequiredInput = "SPLIT_MULTIPLIER" },
    };
}
