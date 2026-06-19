using NtisPlatform.Application.DTOs.Rules.RuleEngine;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Rules;

namespace NtisPlatform.Application.Interfaces.Rules
{
    /// <summary>
    /// Service interface for rule engine configuration operations
    /// </summary>
    public interface IRuleEngineService : ICommonCrudService<RuleEngineEntity, RuleEngineDto, CreateRuleEngineDto, UpdateRuleEngineDto, RuleEngineQueryParameters, int>
    {
        /// <summary>
        /// Get version history for a specific rule
        /// </summary>
        Task<List<RuleVersionHistoryDto>> GetVersionHistoryAsync(int ruleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a lightweight, priority-ordered summary list of all active rules.
        /// Each item includes RuleCode, RuleName, Description, RuleCategory, Priority,
        /// IsEnabled, StopProcessing, RuleScopeId, RuleScopeName, and SubRules metadata.
        /// Heavy JSON blobs (RuleJson, ConditionsJson, EffectJson, TargetFiltersJson) are excluded.
        /// </summary>
        Task<PagedResult<RuleEngineSummaryDto>> GetSummaryAsync(RuleEngineQueryParameters queryParameters, CancellationToken cancellationToken = default);
    }
}
