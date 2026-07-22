using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IRateService : ICommonCrudService<RateEntity, RateDto, CreateRateDto, UpdateRateDto, RateQueryParameters, int>
{
    Task<PagedResult<DetailedRateDto>> GetDetailedAllAsync(RateQueryParameters queryParameters, CancellationToken cancellationToken = default);
    Task<PagedResult<TypeOfUseDetailsDto>> GetTypeOfUseDetailsAsync(TypeOfUseQueryParameters queryParameters, CancellationToken cancellationToken = default);
    Task<PagedResult<TypeOfUseDetailsDto>> GetOpenPlotTypeOfUseDetailsAsync(TypeOfUseQueryParameters queryParameters, CancellationToken cancellationToken = default);
    Task<RateDto> CreateOpenPlotAsync(CreateOpenPlotRateDto createDto, CancellationToken cancellationToken = default);
    Task<BulkResult<RateDto>> BulkCreateOpenPlotAsync(CreateOpenPlotRateDto[] items, CancellationToken cancellationToken = default);
}

