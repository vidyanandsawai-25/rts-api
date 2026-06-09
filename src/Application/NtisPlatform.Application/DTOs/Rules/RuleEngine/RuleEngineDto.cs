namespace NtisPlatform.Application.DTOs.Rules.RuleEngine
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
        /// FK to PTIS.RuleScopeMaster. Null means the rule applies to all scopes.
        /// </summary>
        public int? RuleScopeId { get; set; }

        /// <summary>Display name of the linked scope (from navigation property).</summary>
        public string? RuleScopeName { get; set; }

    }
}
