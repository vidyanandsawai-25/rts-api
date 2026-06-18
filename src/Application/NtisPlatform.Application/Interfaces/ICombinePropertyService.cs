using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface ICombinePropertyService : ICommonCrudService<PropertyEntity, CombinePropertyDto, CreateCombinePropertyDto, UpdateCombinePropertyDto, CombinePropertyQueryParameters, int>
{
    /// <summary>
    /// Get property details for combining by WardId, PropertyNo, and comma-separated PartitionNo
    /// </summary>
    Task<List<PropertyCombineDetailsDto>> GetPropertyCombineDetailsAsync(PropertyCombineDetailsQueryParameters queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Combine multiple properties into a main property
    /// </summary>
    Task<CombinePropertiesResponseDto> CombinePropertiesAsync(CombinePropertiesRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all combined property history based on query parameters (SourcePropertyId, WardId, PropertyNo, PartitionNo).
    /// </summary>
    Task<PagedResult<CombinePropertyHistoryDto>> GetCombinePropertyHistoryAsync(CombinePropertyHistoryQueryParameters queryParams, CancellationToken cancellationToken = default);
}