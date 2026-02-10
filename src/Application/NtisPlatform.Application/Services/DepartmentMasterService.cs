using AutoMapper;
using NtisPlatform.Application.DTOs.Master.DepartmentMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for DepartmentMaster CRUD operations
/// </summary>
public class DepartmentMasterService : BaseCommonCrudService<DepartmentMasterEntity, DepartmentMasterDto, CreateDepartmentMasterDto, UpdateDepartmentMasterDto, DepartmentMasterQueryParameters, int>, IDepartmentMasterService
{
    public DepartmentMasterService(
        IRepository<DepartmentMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
