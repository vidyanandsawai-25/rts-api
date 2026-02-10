using NtisPlatform.Application.DTOs.Master.DepartmentMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for DepartmentMaster CRUD operations
/// </summary>
public interface IDepartmentMasterService : ICommonCrudService<DepartmentMasterEntity, DepartmentMasterDto, CreateDepartmentMasterDto, UpdateDepartmentMasterDto, DepartmentMasterQueryParameters, int>
{
}
