using NtisPlatform.Application.DTOs.RuleEngine;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.RuleEngine
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
