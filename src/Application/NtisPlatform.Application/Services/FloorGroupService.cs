using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class FloorGroupService : BaseCommonCrudService<FloorGroupMasterEntity, FloorGroupDto, CreateFloorGroupDto, UpdateFloorGroupDto, FloorGroupQueryParameters, int>, IFloorGroupService
{
    public FloorGroupService(
        IRepository<FloorGroupMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
