using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class FloorService : BaseCommonCrudService<FloorEntity, FloorDto, CreateFloorDto, UpdateFloorDto, FloorQueryParameters, int>, IFloorService
{
    public FloorService(
        IRepository<FloorEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
