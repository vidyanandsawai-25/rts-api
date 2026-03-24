using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class WingService : BaseCommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>, IWingService
{
    public WingService(
        IRepository<WingEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}