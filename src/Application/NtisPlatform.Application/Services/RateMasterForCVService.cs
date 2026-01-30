using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;


public class RateMasterForCVService: BaseCommonCrudService<RateMasterForCVEntity, RateMasterForCVDto, CreateRateMasterForCVDto, UpdateRateMasterForCVDto, RateMasterForCVQueryParameters, int>, IRateMasterForCVService
{
    public RateMasterForCVService(
        IRepository<RateMasterForCVEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper) { }

}

