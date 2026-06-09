using NtisPlatform.Application.DTOs.Rules.RuleFields;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Models.Rules;

namespace NtisPlatform.Application.Interfaces.Rules
{
    public interface IRuleFieldsService : ICommonCrudService<RulesFieldEntity, RuleFieldsDto, CreateRuleFieldsDto, UpdateRuleFieldsDto, RuleFieldsQueryParameters, int>
    {
        Task<List<RuleFieldDetailsDto>> GetByFieldIdAsync(int RuleScopeId, CancellationToken cancellationToken = default);
    }
}
