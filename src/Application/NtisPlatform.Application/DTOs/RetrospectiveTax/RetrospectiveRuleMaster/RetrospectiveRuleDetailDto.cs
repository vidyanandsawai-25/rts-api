using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectivePenaltyRule;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAction;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleDateCondition;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleSummary;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleMaster;

/// <summary>
/// Everything the Rule Library's "View" panel (and "Edit" form's initial load) needs for one
/// rule, in a single call: the header plus every section built across the rule builder screens.
/// Each section is null/empty only because that section hasn't been configured for this rule yet
/// (e.g. a Draft rule with no RetrospectiveRuleAction row) — not an error.
/// </summary>
public class RetrospectiveRuleDetailDto
{
    /// <summary>Rule header (RuleCode, RuleName, RuleStatus, LegalCapYears, etc.).</summary>
    public RetrospectiveRuleMasterDto Rule { get; set; } = null!;

    /// <summary>"Available evidence" / "Unavailable evidence" panels — one entry per active evidence type.</summary>
    public List<RetrospectiveRuleEvidenceConditionStateDto> EvidenceConditions { get; set; } = new();

    /// <summary>"Compare evidence dates" section. Null if not configured (defaults to "No date comparison").</summary>
    public RetrospectiveRuleDateConditionDto? DateCondition { get; set; }

    /// <summary>"Retrospective Tax" section (Tax starts from / Use date / Retrospective limit / Tax calculation). Null if not configured yet.</summary>
    public RetrospectiveRuleActionDto? Action { get; set; }

    /// <summary>"Unauthorized Construction Penalty" section. Null if not configured yet.</summary>
    public RetrospectivePenaltyRuleDto? PenaltyRule { get; set; }

    /// <summary>"Rule Summary" panel. Null if no summary has been generated for this rule yet.</summary>
    public RetrospectiveRuleSummaryViewDto? Summary { get; set; }
}
