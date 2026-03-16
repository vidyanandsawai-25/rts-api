using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ConfigCategoryMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for ConfigCategoryMaster CRUD operations
/// </summary>
public class ConfigCategoryMasterService : BaseCommonCrudService<ConfigCategoryMasterEntity, ConfigCategoryMasterDto, CreateConfigCategoryMasterDto, UpdateConfigCategoryMasterDto, ConfigCategoryMasterQueryParameters, int>, IConfigCategoryMasterService
{
    public ConfigCategoryMasterService(
        IRepository<ConfigCategoryMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
