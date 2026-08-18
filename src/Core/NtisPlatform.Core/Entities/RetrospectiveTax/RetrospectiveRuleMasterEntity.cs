using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Header/root table for a retrospective rule (e.g. THA-01, PCM-03, FUR-03).
/// Rules apply to the current ULB only.
/// </summary>
[Table("RetrospectiveRuleMaster", Schema = "PTIS")]
public class RetrospectiveRuleMasterEntity : BaseEntity
{
    public string RuleCode { get; set; } = string.Empty;

    public string RuleName { get; set; } = string.Empty;

    public string? RuleDescription { get; set; }

    public int PriorityNo { get; set; }

    /// <summary>CONDITION_BASED / EXACT_EVIDENCE_MATCH / PRIORITY_BASED</summary>
    public string MatchType { get; set; } = "CONDITION_BASED";

    public bool IsFallbackRule { get; set; }

    /// <summary>Draft / Active / Review / NeedsClarification</summary>
    public string RuleStatus { get; set; } = "Draft";

    /// <summary>AUTHORIZED / UNAUTHORIZED / UNDETERMINED</summary>
    public string? AuthorizationStatus { get; set; }

    public bool LegalCapEnabled { get; set; } = true;

    public int LegalCapYears { get; set; } = 6;

    public int NoticeDays { get; set; } = 15;

    public string? VersionNo { get; set; }

    public string? ResolutionRef { get; set; }

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public string? Remarks { get; set; }
}
