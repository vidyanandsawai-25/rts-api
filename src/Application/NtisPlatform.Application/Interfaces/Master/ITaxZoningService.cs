using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface ITaxZoningService
{
    Task<PagedResult<TaxZoningDto>> GetFromToPropertyNo(TaxZoningQueryParameters queryParams, CancellationToken cancellationToken = default);
    Task<PagedResult<TaxZoningDto>> GetAllPropertyNo(TaxZoningQueryParameters queryParams, CancellationToken cancellationToken = default);
    Task<TaxZoningDto?> UpdateAsync(UpdateTaxZoningDto updateDto, CancellationToken cancellationToken = default);
}
