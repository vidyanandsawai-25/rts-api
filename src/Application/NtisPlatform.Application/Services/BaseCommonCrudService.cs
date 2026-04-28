using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
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

    public virtual async Task<PagedResult<TDto>> GetAllAsync(TQueryParams queryParameters,CancellationToken cancellationToken = default)
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
            .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize)
            .ProjectTo<TDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        // Normalize pagination metadata for unpaged results (PageSize = -1)
        var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

        return new PagedResult<TDto>(items, totalCount, pageNumber, pageSize);
    }

    public virtual async Task<TDto?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity == null ? default : _mapper.Map<TDto>(entity);
    }

    /// <summary>
    /// Creates a new entity from the given DTO.
    ///
    /// Validation flow:
    /// 1. DTO-level validation (syntax, format, basic rules) should be performed before calling this method (e.g., in the controller using DataAnnotations or FluentValidation).
    /// 2. Entity-level validation (business rules, database checks) is performed here via ValidateForCreateAsync after mapping.
    ///
    /// If DTO validation fails, return HTTP 400 before calling this method. If entity validation fails, a ValidationException is thrown.
    /// </summary>
    public virtual async Task<TDto> CreateAsync(TCreateDto createDto, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<TEntity>(createDto);
        
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

    public virtual async Task<TDto?> UpdateAsync(TKey id, TUpdateDto updateDto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return default;

        // Create a shallow clone of the entity before mapping to capture current state
        // This snapshot is used for validation logic (e.g., checking IsActive transitions)
        var currentEntitySnapshot = CloneEntity(entity);

        _mapper.Map(updateDto, entity);

        // Run deactivation validation only (checks IsActive transitions)
        var validationResult = await ValidateForDeactivationAsync(id, currentEntitySnapshot, entity, cancellationToken);
        if (!validationResult.IsValid)
        {
            // Use the first error message as the exception message for client visibility
            var firstError = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed for deactivation";
            throw new ValidationException(firstError, validationResult.ToDictionary(), OperationType.Update);
        }

        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<TDto>(entity);
    }

    public virtual async Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        // Run validation before deleting
        var validationResult = await ValidateForDeleteAsync(id, entity, cancellationToken);
        if (!validationResult.IsValid)
        {
            // Use the first error message as the exception message for client visibility
            var firstError = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed for delete operation";
            throw new ValidationException(firstError, validationResult.ToDictionary(), OperationType.Delete);
        }

        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }

    #region Validation Hooks

    /// <summary>
    /// Creates a shallow clone of an entity for validation purposes.
    /// This is sufficient for checking property changes like IsActive transitions.
    /// </summary>
    /// <param name="entity">The entity to clone</param>
    /// <returns>A shallow copy of the entity</returns>
    private TEntity CloneEntity(TEntity entity)
    {
        // MemberwiseClone creates a shallow copy which is sufficient for validation
        // since we only need to check simple property values (e.g., IsActive)
        var cloneMethod = entity.GetType().GetMethod("MemberwiseClone", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (TEntity)cloneMethod!.Invoke(entity, null)!;
    }

    /// <summary>
    /// Validates an entity before creating it.
    ///
    /// By default, this method returns success and does not perform any validation.
    /// Override this method in derived services to add custom business validation logic for create operations,
    /// such as duplicate prevention (e.g., same code/name already exists), business rule checks, or cross-entity constraints.
    ///
    /// If you rely solely on database constraints or controller-level DTO validation for create operations,
    /// you may leave this method unimplemented. In that case, document where validation is handled to avoid confusion.
    /// </summary>
    /// <param name="entity">The entity to be created</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A validation result indicating success or containing errors</returns>
    protected virtual Task<ValidationResult> ValidateForCreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidationResult.Success());
    }

    /// <summary>
    /// Validates an entity only for deactivation (when IsActive changes from true to false).
    /// Override in derived services to add custom deactivation validation logic.
    /// This is NOT called for general update validation.
    /// </summary>
    /// <param name="id">The ID of the entity being updated</param>
    /// <param name="currentEntity">The entity state before the update</param>
    /// <param name="updatedEntity">The entity state after mapping the update DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A validation result indicating success or containing errors</returns>
    protected virtual Task<ValidationResult> ValidateForDeactivationAsync(TKey id, TEntity currentEntity, TEntity updatedEntity, CancellationToken cancellationToken = default)
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
    protected virtual Task<ValidationResult> ValidateForDeleteAsync(TKey id, TEntity entity, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidationResult.Success());
    }

    #endregion

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Creates multiple entities from the given DTOs in a single transaction.
    ///
    /// Validation flow for each item:
    /// 1. DTO-level validation (syntax, format, basic rules) should be performed before calling this method (e.g., in the controller using DataAnnotations or FluentValidation).
    /// 2. Entity-level validation (business rules, database checks) is performed here via ValidateForCreateAsync after mapping.
    ///
    /// If DTO validation fails, return HTTP 400 before calling this method. If entity validation fails, the item is skipped and an error is returned for that item.
    /// The method is transactional: if an exception occurs, all changes are rolled back.
    /// </summary>
    public virtual async Task<BulkResult<TDto>> BulkCreateAsync(TCreateDto[] items, CancellationToken cancellationToken = default)
    {
        if (items.Length == 0)
            return new BulkResult<TDto>(0, 0, []);

        var createdEntities = new List<TEntity>();
        var validationErrors = new List<string>();
        var validationFailedCount = 0;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            for (int i = 0; i < items.Length; i++)
            {
                var entity = _mapper.Map<TEntity>(items[i]);

                // Run validation before persisting (same as single CreateAsync)
                var validationResult = await ValidateForCreateAsync(entity, cancellationToken);
                if (!validationResult.IsValid)
                {
                    validationFailedCount++;
                    var errorMessages = string.Join(", ", validationResult.Errors.Select(e =>
                        string.IsNullOrEmpty(e.PropertyName) ? e.ErrorMessage : $"{e.PropertyName}: {e.ErrorMessage}"));
                    validationErrors.Add($"Item {i}: {errorMessages}");
                    continue;
                }

                createdEntities.Add(entity);
            }

            if (createdEntities.Count > 0)
            {
                await _repository.AddRangeAsync(createdEntities.ToArray(), cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var results = _mapper.Map<List<TDto>>(createdEntities);
            return new BulkResult<TDto>(results.Count, validationFailedCount, results, validationErrors.Count > 0 ? validationErrors : null);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Updates multiple entities in a single transaction.
    ///
    /// Validation flow for each item:
    /// 1. The entity is loaded and a shallow clone is created before mapping.
    /// 2. The update DTO is mapped to the entity.
    /// 3. Entity-level validation (business rules, e.g., deactivation checks) is performed via ValidateForDeactivationAsync.
    ///
    /// If validation fails, the item is skipped and an error is returned for that item.
    /// The method is transactional: if an exception occurs, all changes are rolled back.
    /// </summary>
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

                // Create a shallow clone of the entity before mapping to capture current state
                var currentEntitySnapshot = CloneEntity(entity);

                _mapper.Map(item.Data, entity);

                // Run deactivation validation (same as single UpdateAsync)
                var validationResult = await ValidateForDeactivationAsync(item.Id, currentEntitySnapshot, entity, cancellationToken);
                if (!validationResult.IsValid)
                {
                    failedCount++;
                    var errorMessages = string.Join(", ", validationResult.Errors.Select(e =>
                        string.IsNullOrEmpty(e.PropertyName) ? e.ErrorMessage : $"{e.PropertyName}: {e.ErrorMessage}"));
                    errors.Add($"Record with Id '{item.Id}': {errorMessages}");
                    continue;
                }

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

    /// <summary>
    /// Deletes multiple entities in a single transaction.
    ///
    /// Validation flow for each item:
    /// 1. The entity is loaded by ID.
    /// 2. Entity-level validation (business rules, e.g., reference checks) is performed via ValidateForDeleteAsync.
    ///
    /// If validation fails, the item is skipped and an error is returned for that item.
    /// The method is transactional: if an exception occurs, all changes are rolled back.
    /// </summary>
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

                // Run validation before deleting (same as single DeleteAsync)
                var validationResult = await ValidateForDeleteAsync(id, entity, cancellationToken);
                if (!validationResult.IsValid)
                {
                    failedCount++;
                    var errorMessages = string.Join(", ", validationResult.Errors.Select(e =>
                        string.IsNullOrEmpty(e.PropertyName) ? e.ErrorMessage : $"{e.PropertyName}: {e.ErrorMessage}"));
                    errors.Add($"Record with Id '{id}': {errorMessages}");
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
