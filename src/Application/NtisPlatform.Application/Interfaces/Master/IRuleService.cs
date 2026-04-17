using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.Master;


namespace NtisPlatform.Application.Interfaces.Master;

public interface IRuleService : ICommonCrudService<RuleEntity, RuleDto, CreateRuleDto, UpdateRuleDto, RuleQueryParameters, int>
{
}

