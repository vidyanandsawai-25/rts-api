using NtisPlatform.Application.DTOs.Master.RuleCategory;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master
{
    public interface IRuleCategoryService : ICommonCrudService<RuleCategoryEntity, RuleCategoryDto, CreateRuleCategoryDto, UpdateRuleCategoryDto, RuleCategoryQueryParameters, int>
    {
    }
}
