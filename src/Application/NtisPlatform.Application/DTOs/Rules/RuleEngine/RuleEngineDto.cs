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

        /// <summary>
        /// MS Rules Engine-compatible policy JSON.
        /// Populated only by GetById — stripped from GetAll to keep list payloads lightweight.
        /// </summary>
        public string? RuleJson { get; set; }

        /// <summary>
        /// Serialized ConditionGroupState — for re-hydrating the visual rule builder on edit.
        /// Populated only by GetById.
        /// </summary>
        public string? ConditionsJson { get; set; }

        /// <summary>
        /// Serialized EffectState — for re-hydrating the effect panel on edit.
        /// Populated only by GetById.
        /// </summary>
        public string? EffectJson { get; set; }

        /// <summary>
        /// Serialized TargetFilterState — for re-hydrating the target filters on edit.
        /// Populated only by GetById.
        /// </summary>
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

        /// <summary>
        /// FK to PTIS.PropertyRuleEvaluationMaster. Null means default parameter.
        /// </summary>
        public int? PropertyRuleEvaluationMasterId { get; set; }

        /// <summary>Display name of the evaluation parameter (from navigation property).</summary>
        public string? PropertyRuleEvaluationMasterName { get; set; }

        /// <summary>Code of the evaluation parameter (e.g. Rate, Rent, Maintenance).</summary>
        public string? ParameterCode { get; set; }

        /// <summary>
        /// Populated when ConditionsJson is a JSON array of sub-rules.
        /// Each entry carries the sub-rule id, description, enabled flag, and stopProcessing flag.
        /// Null when the RuleSet uses a single flat condition group.
        /// </summary>
        public List<SubRuleMetaDto>? SubRules { get; set; }
    }
}

