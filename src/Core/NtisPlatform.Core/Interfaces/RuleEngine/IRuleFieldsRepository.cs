using NtisPlatform.Core.Models.RuleEngine;

namespace NtisPlatform.Core.Interfaces.RuleEngine
{
    public interface IRuleFieldsRepository
    {
        Task<List<RuleFieldDetailsDto>> GetByFieldIdAsync(int RuleScopeId, CancellationToken cancellationToken = default);
    }
}