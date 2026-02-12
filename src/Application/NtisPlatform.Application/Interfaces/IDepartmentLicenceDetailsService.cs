using NtisPlatform.Application.DTOs.Master.DepartmentLicenceDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for Department Licence Details operations
/// </summary>
public interface IDepartmentLicenceDetailsService : ICommonCrudService<DepartmentLicenceDetailsEntity, DepartmentLicenceDetailsDto, CreateDepartmentLicenceDetailsDto, UpdateDepartmentLicenceDetailsDto, DepartmentLicenceDetailsQueryParameters, int>
{
}
