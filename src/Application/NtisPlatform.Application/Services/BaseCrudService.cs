using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public abstract class BaseCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams> 
    : ICrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams>
    where TEntity : BaseEntity
    where TQueryParams : BaseQueryParameters
{
    protected readonly IRepository<TEntity> _repository;
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IMapper _mapper;

    protected BaseCrudService(
        IRepository<TEntity> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

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

    public virtual async Task<TDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity == null ? default : _mapper.Map<TDto>(entity);
    }

    public virtual async Task<TDto> CreateAsync(TCreateDto createDto, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<TEntity>(createDto);
        entity.CreatedDate = DateTime.Now;
        
        // Run validation before persisting
        var validationResult = await ValidateForCreateAsync(entity, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException("Validation failed for create operation", validationResult.ToDictionary(), OperationType.Create);
        }
        
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return _mapper.Map<TDto>(entity);
    }

    public virtual async Task<TDto?> UpdateAsync(int id, TUpdateDto updateDto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return default;

        // Create a copy of the current entity state for validation comparison
        var currentEntitySnapshot = _mapper.Map<TEntity>(_mapper.Map<TDto>(entity));
        
        _mapper.Map(updateDto, entity);
        entity.UpdatedDate = DateTime.Now;
        
        // Run validation before persisting
        var validationResult = await ValidateForUpdateAsync(id, currentEntitySnapshot, entity, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException("Validation failed for update operation", validationResult.ToDictionary(), OperationType.Update);
        }
        
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return _mapper.Map<TDto>(entity);
    }

    public virtual async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        // Run validation before deleting
        var validationResult = await ValidateForDeleteAsync(id, entity, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException("Validation failed for delete operation", validationResult.ToDictionary(), OperationType.Delete);
        }

        await _repository.DeleteAsync(id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }

    #region Validation Hooks

    /// <summary>
    /// Validates an entity before creating it. Override in derived services to add custom validation logic.
    /// </summary>
    /// <param name="entity">The entity to be created</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A validation result indicating success or containing errors</returns>
    protected virtual Task<ValidationResult> ValidateForCreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidationResult.Success());
    }

    /// <summary>
    /// Validates an entity before updating it. Override in derived services to add custom validation logic.
    /// </summary>
    /// <param name="id">The ID of the entity being updated</param>
    /// <param name="currentEntity">The entity state before the update</param>
    /// <param name="updatedEntity">The entity state after mapping the update DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A validation result indicating success or containing errors</returns>
    protected virtual Task<ValidationResult> ValidateForUpdateAsync(int id, TEntity currentEntity, TEntity updatedEntity, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidationResult.Success());
    }

    /// <summary>
    /// Validates an entity before deleting it. Override in derived services to add custom validation logic.
    /// Use this to check for referential integrity (e.g., prevent deletion if related records exist).
    /// </summary>
    /// <param name="id">The ID of the entity being deleted</param>
    /// <param name="entity">The entity to be deleted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A validation result indicating success or containing errors</returns>
    protected virtual Task<ValidationResult> ValidateForDeleteAsync(int id, TEntity entity, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidationResult.Success());
    }

    #endregion
}
