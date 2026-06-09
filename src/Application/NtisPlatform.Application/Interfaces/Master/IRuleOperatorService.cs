using NtisPlatform.Application.DTOs.Master.RuleOperatorMaster;
using NtisPlatform.Core.Entities.Rules;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface IRuleOperatorService : ICommonCrudService<RuleOperatorEntity, RuleOperatorDto, CreateRuleOperatorDto, UpdateRuleOperatorDto, RuleOperatorQueryParameters, int>
    {
    }
}