using NtisPlatform.Core.Models.Rules;

namespace NtisPlatform.Core.Interfaces.Rules
{
    public interface IRuleFieldsRepository
    {
        Task<List<RuleFieldDetailsDto>> GetByFieldIdAsync(int RuleScopeId, CancellationToken cancellationToken = default);
    }
}
