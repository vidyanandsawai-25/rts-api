using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class FloorService : BaseCommonCrudService<FloorEntity, FloorDto, CreateFloorDto, UpdateFloorDto, FloorQueryParameters, string>, IFloorService
{
    public FloorService(
        IRepository<FloorEntity, string> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
