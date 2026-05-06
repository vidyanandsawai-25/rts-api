using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RuleEffectTypeMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class RuleEffectTypeService : BaseCommonCrudService<RuleEffectTypeEntity, RuleEffectTypeDto, CreateRuleEffectTypeDto, UpdateRuleEffectTypeDto, RuleEffectTypeQueryParameters, int>, IRuleEffectTypeService
    {
        public RuleEffectTypeService(IRepository<RuleEffectTypeEntity, int> repository, IUnitOfWork unitOfWork, IMapper mapper) : base(repository, unitOfWork, mapper)
        {
        }
    }
}
