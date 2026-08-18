namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition;

/// <summary>
/// One row of the "Available evidence" / "Unavailable evidence" checkbox screen for a single rule.
/// One entry per active EvidenceTypeMaster row (OC, CC, Electricity, Change Detection, Construction
/// Year), ordered by DisplayOrder, so the UI can render both checkbox panels from a single call.
/// </summary>
public class RetrospectiveRuleEvidenceConditionStateDto
{
    public int EvidenceTypeId { get; set; }
    public string EvidenceCode { get; set; } = string.Empty;
    public string EvidenceName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    /// <summary>
    /// AVAILABLE  -> checkbox checked in the "Available evidence" panel.
    /// UNAVAILABLE -> checkbox checked in the "Unavailable evidence" panel.
    /// null        -> unchecked in both panels (no condition configured for this evidence type on this rule).
    /// </summary>
    public string? SelectedState { get; set; }
}

/// <summary>
/// Request body for saving both checkbox panels for a rule in one call. An EvidenceTypeMaster.Id
/// left out of both lists is treated as unchecked in both panels (its condition row, if any, is
/// deactivated). The same id must not appear in both lists.
/// </summary>
public class SetRetrospectiveRuleEvidenceConditionStateDto
{
    /// <summary>EvidenceTypeMaster.Id values checked in the "Available evidence" panel.</summary>
    public List<int> AvailableEvidenceTypeIds { get; set; } = new();

    /// <summary>EvidenceTypeMaster.Id values checked in the "Unavailable evidence" panel.</summary>
    public List<int> UnavailableEvidenceTypeIds { get; set; } = new();

    public int? UpdatedBy { get; set; }
}
