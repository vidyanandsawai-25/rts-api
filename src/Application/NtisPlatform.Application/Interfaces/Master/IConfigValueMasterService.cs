using NtisPlatform.Application.DTOs.Master.ConfigValueMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces.Master;

/// <summary>
/// Service interface for ConfigValueMaster CRUD operations
/// </summary>
public interface IConfigValueMasterService : ICommonCrudService<ConfigValueMasterEntity, ConfigValueMasterDto, CreateConfigValueMasterDto, UpdateConfigValueMasterDto, ConfigValueMasterQueryParameters, int>
{
}
