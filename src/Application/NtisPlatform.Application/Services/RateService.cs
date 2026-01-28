using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RateService : BaseCommonCrudService<RateEntity, RateDto, CreateRateDto, UpdateRateDto, RateQueryParameters, int>, IRateService
{
    public RateService(IRepository<RateEntity, int> repository,IUnitOfWork unitOfWork,IMapper mapper): base(repository, unitOfWork, mapper)
    {
    }
}
