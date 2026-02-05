using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
namespace NtisPlatform.Application.Services;

public class ZoneService : BaseCommonCrudService<ZoneEntity, ZoneDto, CreateZoneDto, UpdateZoneDto, ZoneQueryParameters, string>, IZoneService
{
    public ZoneService(
        IRepository<ZoneEntity, string> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}

