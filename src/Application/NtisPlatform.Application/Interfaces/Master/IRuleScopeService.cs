using NtisPlatform.Application.DTOs.Master.RuleScopeMaster;
using NtisPlatform.Core.Entities.Rules;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface IRuleScopeService : ICommonCrudService<RuleScopeEntity, RuleScopeDto, CreateRuleScopeDto, UpdateRuleScopeDto, RuleScopeQueryParameters, int>
    {
    }
}