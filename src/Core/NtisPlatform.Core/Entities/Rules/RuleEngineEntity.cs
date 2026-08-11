using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Rules;

/// <summary>
/// Represents a rule engine master configuration entity
/// </summary>
public class RuleEngineEntity : BaseEntity, IHardDeletable
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

    /// <summary>
    /// Optional foreign key to PTIS.RuleScopeMaster.
    /// Defines the scope/context in which this rule applies (e.g., Residential, Commercial).
    /// Null means the rule applies to all scopes.
    /// </summary>
    public int? RuleScopeId { get; set; }

    /// <summary>
    /// Optional foreign key to PTIS.PropertyRuleEvaluationMaster.
    /// Defines the parameter targeted by this rule (e.g., Rate, Rent, Maintenance).
    /// </summary>
    public int? PropertyRuleEvaluationMasterId { get; set; }

    // Navigation properties
    /// <summary>The scope this rule belongs to (optional)</summary>
    public virtual RuleScopeEntity? RuleScope { get; set; }

    /// <summary>The evaluation parameter master this rule belongs to (optional)</summary>
    public virtual Master.PropertyRuleEvaluationMasterEntity? PropertyRuleEvaluationMaster { get; set; }

    /// <summary>Indicates whether the entity is marked for deletion</summary>
    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }
}
