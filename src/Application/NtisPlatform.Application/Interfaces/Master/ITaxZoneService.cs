using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface ITaxZoneService
    : ICommonCrudService<TaxZoneEntity, TaxZoneDto, CreateTaxZoneDto, UpdateTaxZoneDto, TaxZoneQueryParameters, int>
{
}
