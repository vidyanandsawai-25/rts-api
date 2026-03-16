using NtisPlatform.Application.DTOs.Master.ConfigKeyMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for ConfigKeyMaster CRUD operations
/// </summary>
public interface IConfigKeyMasterService : ICommonCrudService<ConfigKeyMasterEntity, ConfigKeyMasterDto, CreateConfigKeyMasterDto, UpdateConfigKeyMasterDto, ConfigKeyMasterQueryParameters, int>
{
}
