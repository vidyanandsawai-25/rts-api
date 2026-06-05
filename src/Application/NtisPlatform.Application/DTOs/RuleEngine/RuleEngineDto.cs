
namespace NtisPlatform.Application.DTOs.RuleEngine
{
    /// <summary>
    /// DTO for retrieving rule engine configuration
    /// </summary>
    public class RuleEngineDto : BaseDtos
    {
        public string RuleCode { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RuleCategory { get; set; }
        public string RuleJson { get; set; } = string.Empty;

        /// <summary>Serialized ConditionGroupState — for re-hydrating the visual rule builder on edit</summary>
        public string? ConditionsJson { get; set; }

        /// <summary>Serialized EffectState — for re-hydrating the effect panel on edit</summary>
        public string? EffectJson { get; set; }

        /// <summary>Serialized TargetFilterState — for re-hydrating the target filters on edit</summary>
        public string? TargetFiltersJson { get; set; }

        public int Priority { get; set; }
        public bool IsEnabled { get; set; }
        public bool StopProcessing { get; set; }

        /// <summary>
        /// List of rule IDs that should be skipped when this rule is applied.
        /// Used for rule exclusion logic.
        /// </summary>
        public List<int> SkipRuleIds { get; set; } = new();

        /// <summary>
        /// Detailed information about skip rules (for display purposes).
        /// Includes RuleCode and RuleName of each skipped rule.
        /// </summary>
        public List<SkipRuleInfo> SkipRules { get; set; } = new();
    }

    /// <summary>
    /// Information about a rule that will be skipped
    /// </summary>
    public class SkipRuleInfo
    {
        public int RuleId { get; set; }
        public string RuleCode { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}

