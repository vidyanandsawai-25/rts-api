namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleDateCondition;

/// <summary>
/// One dropdown choice for the "Compare evidence dates" field (RetrospectiveRuleDateCondition.
/// ComparatorCode). Not a DB-backed lookup table — mirrors the fixed comparator list documented
/// on RetrospectiveRuleDateConditionEntity.ComparatorCode, so the UI has a single source of truth
/// for what's valid and the API developer has a single place to update if the list changes.
/// </summary>
public class RetrospectiveRuleDateConditionOptionDto
{
    /// <summary>The value to send back in ComparatorCode on Create/Update, e.g. "ELECTRICITY_BEFORE_CC".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display text for the dropdown, e.g. "Electricity date before CC date".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Which extra fields the UI should show/collect for this comparator, so the form can react
    /// to the selection without hardcoding the mapping itself:
    /// NONE                 -> none.
    /// ELECTRICITY_BEFORE_CC / ELECTRICITY_AFTER_CC
    ///                      -> no extra input; server resolves Electricity/CC dates from the
    ///                         rule's configured evidence (LeftEvidenceTypeId/RightEvidenceTypeId
    ///                         are set to those two evidence types automatically).
    /// ELECTRICITY_BEFORE_CUTOFF / ELECTRICITY_AFTER_CUTOFF
    ///                      -> show a single date picker bound to CompareDate (the cutoff date).
    /// OC_OLDER_THAN_ALLOWED_PERIOD / OC_WITHIN_ALLOWED_PERIOD
    ///                      -> show a "years" number input bound to CompareYears (the allowed period).
    /// </summary>
    public string RequiredInput { get; set; } = string.Empty;
}
