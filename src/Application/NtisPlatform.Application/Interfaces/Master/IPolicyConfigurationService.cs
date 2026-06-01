using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IPolicyConfigurationService
    : ICommonCrudService<PolicyConfigurationEntity, PolicyConfigurationDto, CreatePolicyConfigurationDto, UpdatePolicyConfigurationDto, PolicyConfigurationQueryParameters, int>
{
}
