namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAction;

/// <summary>
/// One dropdown choice for a CHECK-constraint-backed enum field on RetrospectiveRuleAction
/// (TaxStartMode, RetrospectiveLimitType, or TaxCalculationMode). Not a DB-backed lookup table —
/// mirrors the fixed list documented on RetrospectiveRuleActionEntity, so the UI has a single
/// source of truth for what's valid and the API developer has a single place to update if the
/// list changes.
/// </summary>
public class RetrospectiveRuleActionOptionDto
{
    /// <summary>The value to send back on Create/Update, e.g. "MONTHS_AFTER".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display text for the dropdown, e.g. "After specified months".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Which extra field(s) the UI should show/collect for this selection, so the form can react
    /// without hardcoding the mapping itself:
    /// NONE                  -> no extra input; server derives the date/boundary (max look-back,
    ///                          construction date/cap, or "no limit") at calculation time.
    /// EVIDENCE_TYPE          -> show the evidence-type picker bound to StartEvidenceTypeId
    ///                          (options from GET api/RetrospectiveRuleAction/use-date-options).
    /// EVIDENCE_TYPE_AND_MONTHS -> show the evidence-type picker (StartEvidenceTypeId) AND a
    ///                          months number input bound to OffsetMonths.
    /// CUTOFF_DATE            -> show a single date picker bound to CutoffDate (shared column —
    ///                          used by both TaxStartMode = FIXED_CUTOFF and
    ///                          RetrospectiveLimitType = FIXED_CUTOFF_DATE).
    /// MAXIMUM_YEARS          -> show a whole-number "years" input bound to MaximumYears.
    /// SINGLE_MULTIPLIER      -> show one multiplier input bound to TaxMultiplier.
    /// SPLIT_MULTIPLIER       -> show two evidence-date pickers (SplitStartEvidenceTypeId,
    ///                          SplitEndEvidenceTypeId — options from
    ///                          GET api/RetrospectiveRuleAction/use-date-options) and two
    ///                          multiplier inputs: SplitMultiplier (applies from
    ///                          SplitStartEvidenceTypeId's date up to SplitEndEvidenceTypeId's
    ///                          date) and AfterSplitMultiplier (applies after that).
    /// </summary>
    public string RequiredInput { get; set; } = string.Empty;
}
