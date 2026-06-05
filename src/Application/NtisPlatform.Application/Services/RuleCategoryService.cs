using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RuleCategory;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class RuleCategoryService : BaseCommonCrudService<RuleCategoryEntity, RuleCategoryDto, CreateRuleCategoryDto, UpdateRuleCategoryDto, RuleCategoryQueryParameters, int>, IRuleCategoryService
    {
        public RuleCategoryService(IRepository<RuleCategoryEntity, int> repository, IUnitOfWork unitOfWork, IMapper mapper)
            : base(repository, unitOfWork, mapper)
        {
        }
    }
}
