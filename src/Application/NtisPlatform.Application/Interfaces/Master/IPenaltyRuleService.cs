using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IPenaltyRuleService :
    ICommonCrudService<PenaltyRuleMasterEntity, PenaltyRuleDto, CreatePenaltyRuleDto, UpdatePenaltyRuleDto, PenaltyRuleQueryParameters, int>
{
}
