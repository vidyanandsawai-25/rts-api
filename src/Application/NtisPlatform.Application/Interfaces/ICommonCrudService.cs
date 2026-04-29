using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

public interface ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>
    where TQueryParams : BaseQueryParameters
    where TCreateDto : class
    where TUpdateDto : class
{
    Task<PagedResult<TDto>> GetAllAsync(TQueryParams queryParameters, CancellationToken cancellationToken = default);
    Task<TDto?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<TDto> CreateAsync(TCreateDto createDto, CancellationToken cancellationToken = default);
    Task<TDto?> UpdateAsync(TKey id, TUpdateDto updateDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);

    // Bulk operations
    Task<BulkResult<TDto>> BulkCreateAsync(TCreateDto[] items, CancellationToken cancellationToken = default);
    Task<BulkResult<TDto>> BulkUpdateAsync(BulkUpdateItem<TKey, TUpdateDto>[] items, CancellationToken cancellationToken = default);
    Task<BulkResult<TKey>> BulkDeleteAsync(TKey[] ids, CancellationToken cancellationToken = default);

    // Range operations
    Task<RangeResult<TDto>> CreateFromRangeAsync(RangeCreateRequest<TCreateDto> request,Func<TCreateDto, string, int, TCreateDto> transformer,CancellationToken cancellationToken = default);
}
