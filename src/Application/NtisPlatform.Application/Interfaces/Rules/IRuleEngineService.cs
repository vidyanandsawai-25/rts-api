using NtisPlatform.Application.DTOs.Rules.RuleEngine;
using NtisPlatform.Application.Interfaces;
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
    }
}
