using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ConfigValueMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for ConfigValueMaster CRUD operations
/// </summary>
public class ConfigValueMasterService : BaseCommonCrudService<ConfigValueMasterEntity, ConfigValueMasterDto, CreateConfigValueMasterDto, UpdateConfigValueMasterDto, ConfigValueMasterQueryParameters, int>, IConfigValueMasterService
{
    public ConfigValueMasterService(
        IRepository<ConfigValueMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
