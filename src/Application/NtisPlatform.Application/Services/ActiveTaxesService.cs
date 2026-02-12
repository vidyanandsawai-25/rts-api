using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class ActiveTaxesService
    : BaseCommonCrudService<ActiveTaxesEntity, ActiveTaxesDto, CreateActiveTaxesDto, UpdateActiveTaxesDto, ActiveTaxesQueryParameters, int>,
      IActiveTaxesService
{
    public ActiveTaxesService(
        IRepository<ActiveTaxesEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
