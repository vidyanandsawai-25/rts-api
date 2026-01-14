using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

public interface ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>
    where TQueryParams : BaseQueryParameters
{
    Task<PagedResult<TDto>> GetAllAsync(TQueryParams queryParameters, CancellationToken cancellationToken = default);
    Task<TDto?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<TDto> CreateAsync(TCreateDto createDto, CancellationToken cancellationToken = default);
    Task<TDto?> UpdateAsync(TKey id, TUpdateDto updateDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}
