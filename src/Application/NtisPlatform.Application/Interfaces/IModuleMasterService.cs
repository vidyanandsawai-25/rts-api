using NtisPlatform.Application.DTOs.Master.ModuleMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for ModuleMaster CRUD operations
/// </summary>
public interface IModuleMasterService : ICommonCrudService<ModuleMasterEntity, ModuleMasterDto, CreateModuleMasterDto, UpdateModuleMasterDto, ModuleMasterQueryParameters, int>
{
}
