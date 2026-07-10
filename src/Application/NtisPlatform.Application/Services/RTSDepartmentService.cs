using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RTSDepartmentMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSDepartmentService : BaseCommonCrudService<RTSDepartmentEntity, RTSDepartmentDto, CreateRTSDepartmentDto, UpdateRTSDepartmentDto, RTSDepartmentQueryParameters, int>, IRTSDepartmentService
{
    public RTSDepartmentService(
        IRepository<RTSDepartmentEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
       : base(repository, unitOfWork, mapper)
    {
    }
}
