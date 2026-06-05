using NtisPlatform.Application.DTOs.RuleEngine;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Models.RuleEngine;

namespace NtisPlatform.Application.Interfaces.RuleEngine
{
    public interface IRuleFieldsService : ICommonCrudService<RulesFieldEntity, RuleFieldsDto, CreateRuleFieldsDto, UpdateRuleFieldsDto, RuleFieldsQueryParameters, int>
    {
        Task<List<RuleFieldDetailsDto>> GetByFieldIdAsync(int RuleScopeId, CancellationToken cancellationToken = default);
    }
}