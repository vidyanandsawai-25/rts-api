namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents a rule engine master configuration entity
/// </summary>
public class RuleEngineEntity : BaseEntity
{
    public string RuleCode { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RuleCategory { get; set; }

    /// <summary>Microsoft Rules Engine–compatible policy JSON (executed at runtime)</summary>
    public string RuleJson { get; set; } = string.Empty;

    /// <summary>Serialized ConditionGroupState — used to re-hydrate the visual rule builder on edit</summary>
    public string? ConditionsJson { get; set; }

    /// <summary>Serialized EffectState — used to re-hydrate the effect panel on edit</summary>
    public string? EffectJson { get; set; }

    /// <summary>Serialized TargetFilterState — used to re-hydrate the target filters on edit</summary>
    public string? TargetFiltersJson { get; set; }

    public int Priority { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// When true, stops processing all remaining rules after this rule is successfully applied.
    /// Useful for terminal rules that should halt the rule chain (e.g., full exemptions).
    /// </summary>
    public bool StopProcessing { get; set; } = false;

    // Navigation properties
    /// <summary>Exclusions where this rule triggers skipping of other rules</summary>
    public virtual ICollection<RuleExclusionEntity> ExclusionsTriggered { get; set; } = new List<RuleExclusionEntity>();

    /// <summary>Exclusions where this rule gets skipped by other rules</summary>
    public virtual ICollection<RuleExclusionEntity> ExclusionsSkippedBy { get; set; } = new List<RuleExclusionEntity>();
}

