using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

public interface ICrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams>
    where TQueryParams : BaseQueryParameters
{
    Task<PagedResult<TDto>> GetAllAsync(TQueryParams queryParameters, CancellationToken cancellationToken = default);
    Task<TDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TDto> CreateAsync(TCreateDto createDto, CancellationToken cancellationToken = default);
    Task<TDto?> UpdateAsync(int id, TUpdateDto updateDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
