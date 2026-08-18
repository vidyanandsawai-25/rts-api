namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleSummary;

/// <summary>
/// Everything the "Rule Summary" screen needs in one call: the rule's code (for the badge) plus
/// the three generated summary lines. Joins RetrospectiveRuleMaster.RuleCode with the rule's
/// active RetrospectiveRuleSummary row — the plain RetrospectiveRuleSummary CRUD endpoints only
/// return the summary table's own columns, which don't include RuleCode.
/// </summary>
public class RetrospectiveRuleSummaryViewDto
{
    public int RuleId { get; set; }

    /// <summary>Rule code shown as the badge, e.g. "THA-01".</summary>
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>"When" line, e.g. "Electricity, Change Detection, Construction Year available; OC, CC unavailable".</summary>
    public string? WhenSummary { get; set; }

    /// <summary>"Tax" line, e.g. "Start from Later of construction date or rolling cap; not before 01 Apr 2016; tax x 1.".</summary>
    public string? TaxSummary { get; set; }

    /// <summary>"Penalty" line, e.g. "Do not apply penalty".</summary>
    public string? PenaltySummary { get; set; }

    public DateTime? SummaryGeneratedDate { get; set; }
}
