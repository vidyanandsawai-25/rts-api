using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public abstract class BaseCommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>
    : ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>
    where TEntity : class
    where TQueryParams : BaseQueryParameters
    where TCreateDto : class
    where TUpdateDto : class
{
    protected readonly IRepository<TEntity, TKey> _repository;
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IMapper _mapper;

    protected BaseCommonCrudService(
        IRepository<TEntity, TKey> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    #region Single CRUD Operations

    public virtual async Task<PagedResult<TDto>> GetAllAsync(
        TQueryParams queryParameters,
        CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable();

        // Apply filters
        query = query.ApplyFilters(queryParameters);

        // Apply search
        query = query.ApplySearch(queryParameters);

        // Apply sorting
        query = query.ApplySort(queryParameters);

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await query
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ProjectTo<TDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PagedResult<TDto>(items, totalCount, queryParameters.PageNumber, queryParameters.PageSize);
    }

    public virtual async Task<TDto?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity == null ? default : _mapper.Map<TDto>(entity);
    }

    public virtual async Task<TDto> CreateAsync(TCreateDto createDto, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<TEntity>(createDto);
        
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return _mapper.Map<TDto>(entity);
    }

    public virtual async Task<TDto?> UpdateAsync(TKey id, TUpdateDto updateDto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return default;

        _mapper.Map(updateDto, entity);
        
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return _mapper.Map<TDto>(entity);
    }

    public virtual async Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        // Use entity overload to avoid redundant GetByIdAsync call in DeleteAsync
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    #endregion

    #region Bulk Operations

    public virtual async Task<BulkResult<TDto>> BulkCreateAsync(TCreateDto[] items,CancellationToken cancellationToken = default)
    {
        if (items.Length == 0)
            return new BulkResult<TDto>(0, 0, []);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var entities = _mapper.Map<TEntity[]>(items);

            await _repository.AddRangeAsync(entities, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var results = _mapper.Map<TDto[]>(entities);
            return new BulkResult<TDto>(results.Length, 0, results);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public virtual async Task<BulkResult<TDto>> BulkUpdateAsync(
    BulkUpdateItem<TKey, TUpdateDto>[] items,
    CancellationToken cancellationToken = default)
    {
        if (items.Length == 0)
            return new BulkResult<TDto>(0, 0, []);
        var updatedEntities = new List<TEntity>();
        var errors = new List<string>();
        var failedCount = 0;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var item in items)
            {
                var entity = await _repository.GetByIdAsync(item.Id, cancellationToken);
                if (entity == null)
                {
                    failedCount++;
                    errors.Add($"Record with Id '{item.Id}' not found.");
                    continue;
                }

                _mapper.Map(item.Data, entity);
                await _repository.UpdateAsync(entity, cancellationToken);
                updatedEntities.Add(entity);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var results = _mapper.Map<List<TDto>>(updatedEntities);

        return new BulkResult<TDto>(
            results.Count,
            failedCount,
            results,
            errors.Count > 0 ? errors : null);
    }

    public virtual async Task<BulkResult<TKey>> BulkDeleteAsync(
        TKey[] ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Length == 0)
            return new BulkResult<TKey>(0, 0, []);

        var deletedIds = new List<TKey>();
        var errors = new List<string>();
        var failedCount = 0;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var id in ids)
            {
                var entity = await _repository.GetByIdAsync(id, cancellationToken);
                if (entity == null)
                {
                    failedCount++;
                    errors.Add($"Record with Id '{id}' not found.");
                    continue;
                }

                await _repository.DeleteAsync(entity, cancellationToken);
                deletedIds.Add(id);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new BulkResult<TKey>(   
            deletedIds.Count,
            failedCount,
            deletedIds,
            errors.Count > 0 ? errors : null);
    }

    #endregion
}
