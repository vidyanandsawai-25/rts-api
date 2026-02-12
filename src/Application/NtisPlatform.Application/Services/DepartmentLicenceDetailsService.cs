using AutoMapper;
using NtisPlatform.Application.DTOs.Master.DepartmentLicenceDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for Department Licence Details operations
/// </summary>
public class DepartmentLicenceDetailsService : BaseCommonCrudService<DepartmentLicenceDetailsEntity, DepartmentLicenceDetailsDto, CreateDepartmentLicenceDetailsDto, UpdateDepartmentLicenceDetailsDto, DepartmentLicenceDetailsQueryParameters, int>, IDepartmentLicenceDetailsService
{
    public DepartmentLicenceDetailsService(
        IRepository<DepartmentLicenceDetailsEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper) : base(repository, unitOfWork, mapper)
    {
    }
}
