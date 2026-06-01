using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PolicyConfigurationService
    : BaseCommonCrudService<PolicyConfigurationEntity, PolicyConfigurationDto, CreatePolicyConfigurationDto, UpdatePolicyConfigurationDto, PolicyConfigurationQueryParameters, int>,
      IPolicyConfigurationService
{
    public PolicyConfigurationService(
        IRepository<PolicyConfigurationEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
