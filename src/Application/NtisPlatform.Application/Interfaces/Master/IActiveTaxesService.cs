using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IActiveTaxesService
    : ICommonCrudService<ActiveTaxesEntity, ActiveTaxesDto, CreateActiveTaxesDto, UpdateActiveTaxesDto, ActiveTaxesQueryParameters, int>
{
}
