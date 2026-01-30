using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class TaxZoneService
    : BaseCommonCrudService<TaxZoneEntity, TaxZoneDto, CreateTaxZoneDto, UpdateTaxZoneDto, TaxZoneQueryParameters, string>,
      ITaxZoneService
{
    public TaxZoneService(
        IRepository<TaxZoneEntity, string> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
