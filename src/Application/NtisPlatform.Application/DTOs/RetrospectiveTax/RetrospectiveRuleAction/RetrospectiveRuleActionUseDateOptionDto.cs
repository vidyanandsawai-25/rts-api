namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAction;

/// <summary>
/// One choice for the "Use date" field in the "Retrospective Tax" section (RetrospectiveRuleAction.
/// StartEvidenceTypeId). Unlike the static CHECK-constraint dropdowns (TaxStartMode, ComparatorCode,
/// etc.), this list is DB-driven: one row per active EvidenceTypeMaster (OC, CC, Electricity, Change
/// Detection, Construction Year — kept in sync automatically if evidence types are added/renamed via
/// EvidenceTypeMaster's own CRUD API), plus one synthetic "Cutoff date" entry representing the rule's
/// own fixed CutoffDate instead of an evidence date.
/// </summary>
public class RetrospectiveRuleActionUseDateOptionDto
{
    /// <summary>
    /// Send this back as StartEvidenceTypeId when the option is an evidence type.
    /// Null when IsCutoffDate is true (Cutoff date has no EvidenceTypeMaster row).
    /// </summary>
    public int? EvidenceTypeId { get; set; }

    /// <summary>Display text for the dropdown, e.g. "OC date", "Cutoff date".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// True for the synthetic "Cutoff date" entry. When selected: leave StartEvidenceTypeId null,
    /// set TaxStartMode to "FIXED_CUTOFF" (see GET api/RetrospectiveRuleAction/tax-start-modes),
    /// and show the CutoffDate picker. When false: set StartEvidenceTypeId to EvidenceTypeId and
    /// use whichever TaxStartMode needs an evidence date (EVIDENCE_DATE / FY_START /
    /// NEXT_FINANCIAL_YEAR / MONTHS_AFTER).
    /// </summary>
    public bool IsCutoffDate { get; set; }
}
