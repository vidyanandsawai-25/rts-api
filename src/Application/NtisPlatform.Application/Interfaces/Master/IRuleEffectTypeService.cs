using NtisPlatform.Application.DTOs.Master.RuleEffectTypeMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface IRuleEffectTypeService : ICommonCrudService<RuleEffectTypeEntity, RuleEffectTypeDto, CreateRuleEffectTypeDto, UpdateRuleEffectTypeDto, RuleEffectTypeQueryParameters, int>
    {
    }
}