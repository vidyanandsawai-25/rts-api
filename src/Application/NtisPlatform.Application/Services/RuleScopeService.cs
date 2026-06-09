using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RuleScopeMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services
{
    public class RuleScopeService : BaseCommonCrudService<RuleScopeEntity, RuleScopeDto, CreateRuleScopeDto, UpdateRuleScopeDto, RuleScopeQueryParameters, int>, IRuleScopeService
    {
        public RuleScopeService(IRepository<RuleScopeEntity, int> repository, IUnitOfWork unitOfWork, IMapper mapper) : base(repository, unitOfWork, mapper)
        {
        }
    }
}
