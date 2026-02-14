using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ModuleMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for ModuleMaster CRUD operations
/// </summary>
public class ModuleMasterService : BaseCommonCrudService<ModuleMasterEntity, ModuleMasterDto, CreateModuleMasterDto, UpdateModuleMasterDto, ModuleMasterQueryParameters, int>, IModuleMasterService
{
    public ModuleMasterService(
        IRepository<ModuleMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
