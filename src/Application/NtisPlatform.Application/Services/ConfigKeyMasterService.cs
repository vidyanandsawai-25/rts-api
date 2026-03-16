using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ConfigKeyMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for ConfigKeyMaster CRUD operations
/// </summary>
public class ConfigKeyMasterService : BaseCommonCrudService<ConfigKeyMasterEntity, ConfigKeyMasterDto, CreateConfigKeyMasterDto, UpdateConfigKeyMasterDto, ConfigKeyMasterQueryParameters, int>, IConfigKeyMasterService
{
    public ConfigKeyMasterService(
        IRepository<ConfigKeyMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
