using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RateSectionDetailsService : BaseCommonCrudService<RateSectionDetailsEntity, RateSectionDetailsDto, CreateRateSectionDetailsDto, UpdateRateSectionDetailsDto, RateSectionDetailsQueryParameters, int>, IRateSectionDetailsService
{
    public RateSectionDetailsService(
        IRepository<RateSectionDetailsEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }

}

