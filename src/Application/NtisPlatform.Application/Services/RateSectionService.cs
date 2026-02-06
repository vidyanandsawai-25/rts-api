using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RateSectionService : BaseCommonCrudService<RateSectionEntity, RateSectionDto, CreateRateSectionDto, UpdateRateSectionDto, RateSectionQueryParameters, string>, IRateSectionService
{
    public RateSectionService(
        IRepository<RateSectionEntity, string> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}

