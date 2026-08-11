using NtisPlatform.Application.DTOs;

namespace NtisPlatform.Application.DTOs.Rules.RuleEngine
{
    /// <summary>
    /// Lightweight DTO for rule engine configuration summaries
    /// Excludes the heavy JSON properties (RuleJson, ConditionsJson, EffectJson, TargetFiltersJson)
    /// </summary>
    public class RuleEngineSummaryDto : BaseDtos
    {
        public string RuleCode { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RuleCategory { get; set; }
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
