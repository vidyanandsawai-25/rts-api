using NtisPlatform.Application.DTOs.Master.ConfigCategoryMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces.Master;

/// <summary>
/// Service interface for ConfigCategoryMaster CRUD operations
/// </summary>
public interface IConfigCategoryMasterService : ICommonCrudService<ConfigCategoryMasterEntity, ConfigCategoryMasterDto, CreateConfigCategoryMasterDto, UpdateConfigCategoryMasterDto, ConfigCategoryMasterQueryParameters, int>
{
}
