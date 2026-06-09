using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RuleOperatorMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class RuleOperatorService : BaseCommonCrudService<RuleOperatorEntity, RuleOperatorDto, CreateRuleOperatorDto, UpdateRuleOperatorDto, RuleOperatorQueryParameters, int>, IRuleOperatorService
    {
        public RuleOperatorService(IRepository<RuleOperatorEntity, int> repository, IUnitOfWork unitOfWork, IMapper mapper) : base(repository, unitOfWork, mapper)
        {
        }
    }
}
