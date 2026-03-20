using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class SubFloorService : BaseCommonCrudService<SubFloorEntity, SubFloorDto, CreateSubFloorDto, UpdateSubFloorDto, SubFloorQueryParameters, int>, ISubFloorService
{
    public SubFloorService(
        IRepository<SubFloorEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
