using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
namespace NtisPlatform.Application.Services;

public class RuleService : BaseCommonCrudService<RuleEntity, RuleDto, CreateRuleDto, UpdateRuleDto, RuleQueryParameters, int>, IRuleService
{
    public RuleService(
        IRepository<RuleEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}

