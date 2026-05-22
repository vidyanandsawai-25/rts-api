using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class OwningDepartmentService : BaseCommonCrudService<
    OwningDepartmentEntity,
    OwningDepartmentDto,
    CreateOwningDepartmentDto,
    UpdateOwningDepartmentDto,
    OwningDepartmentQueryParameters,
    int>, IOwningDepartmentService
{
    public OwningDepartmentService(
        IRepository<OwningDepartmentEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
