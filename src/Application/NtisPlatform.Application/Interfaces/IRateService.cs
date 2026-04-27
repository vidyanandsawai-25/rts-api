using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IRateService : ICommonCrudService<RateEntity, RateDto, CreateRateDto, UpdateRateDto, RateQueryParameters, int>
{
    Task<PagedResult<DetailedRateDto>> GetDetailedAllAsync(RateQueryParameters queryParameters, CancellationToken cancellationToken = default);
}

