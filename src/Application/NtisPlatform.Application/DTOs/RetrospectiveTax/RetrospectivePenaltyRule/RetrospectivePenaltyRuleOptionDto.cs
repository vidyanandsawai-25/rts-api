namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectivePenaltyRule;

/// <summary>
/// One dropdown choice for a CHECK-constraint-backed enum field on RetrospectivePenaltyRule
/// (PenaltyMode, PenaltyDateSourceType, or PenaltyDateCondition). Not a DB-backed lookup table —
/// mirrors the fixed list documented on RetrospectivePenaltyRuleEntity, so the UI has a single
/// source of truth for what's valid and the API developer has a single place to update if the
/// list changes.
/// </summary>
public class RetrospectivePenaltyRuleOptionDto
{
    /// <summary>The value to send back on Create/Update, e.g. "DATE_VALIDATION".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display text for the dropdown, e.g. "Apply penalty based on a date".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Which extra field(s) the UI should show/collect for this selection, so the form can react
    /// without hardcoding the mapping itself:
    /// NONE              -> no extra input.
    /// OPTIONAL_PERCENT  -> show an optional percent input bound to PenaltyPercent (leave null to
    ///                     let the Act's own percentage apply elsewhere).
    /// DATE_CONDITION    -> show the nested date-condition builder: PenaltyDateSourceType picker
    ///                     (options from GET api/RetrospectivePenaltyRule/penalty-date-source-types),
    ///                     then PenaltyDateCondition picker (options from
    ///                     GET api/RetrospectivePenaltyRule/penalty-date-conditions), plus a
    ///                     required PenaltyPercent input.
    /// EVIDENCE_TYPE     -> show the evidence-type picker bound to PenaltyDateEvidenceTypeId
    ///                     (options from GET api/EvidenceTypeMaster).
    /// COMPARE_DATE      -> show a single date picker bound to CompareDate.
    /// COMPARE_DATE_RANGE -> show two date pickers bound to CompareDate (from) and CompareDateTo (to).
    /// </summary>
    public string RequiredInput { get; set; } = string.Empty;
}
