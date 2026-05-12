using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Interfaces;
using System.Collections.Concurrent;
using System.Reflection;

namespace NtisPlatform.Application.Services;

public abstract class BaseCommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>
    : ICommonCrudService<TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey>
    where TEntity : class
    where TDto : class
    where TQueryParams : BaseQueryParameters
    where TCreateDto : class
    where TUpdateDto : class
{
    protected readonly IRepository<TEntity, TKey> _repository;
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IMapper _mapper;
    private readonly LocalizationProcessor? _localizationProcessor;
    private readonly ILocalizedQueryService? _localizedQueryService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    // Primary constructor for non-localized services (backward compatible)
    protected BaseCommonCrudService(
        IRepository<TEntity, TKey> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : this(repository, unitOfWork, mapper, null, null, null)
    {
    }

    // Extended constructor for localized services
    protected BaseCommonCrudService(
        IRepository<TEntity, TKey> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        LocalizationProcessor? localizationProcessor,
        ILocalizedQueryService? localizedQueryService,
        IHttpContextAccessor? httpContextAccessor)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _localizationProcessor = localizationProcessor;
        _localizedQueryService = localizedQueryService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Determines if localization is enabled. Can be overridden in derived classes.
    /// </summary>
    protected virtual bool IsLocalizationEnabled => _localizationProcessor != null;

    /// <summary>
    /// Gets the current language from the request context.
    /// </summary>
    private string CurrentLanguage => _httpContextAccessor?.HttpContext?.Items[HttpContextKeys.CurrentLanguage] as string ?? "en";

    /// <summary>
    /// Override in derived classes to eagerly load navigation properties.
    /// When the returned query differs from the input (i.e. includes were added),
    /// the base class falls back to in-memory mapping instead of ProjectTo, which
    /// avoids EF Core SQL-translation failures for expressions like ToString("G29").
    /// </summary>
    protected virtual IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query) => query;

    #region Single CRUD Operations

    public virtual async Task<PagedResult<TDto>> GetAllAsync(TQueryParams queryParameters, CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable();

        // Pre-filter hook for localization
        if (IsLocalizationEnabled)
        {
            query = await PreFilterLocalizationAsync(query, queryParameters, cancellationToken);
        }

        // Apply filters
        query = query.ApplyFilters(queryParameters);

        // Apply search (with localization support if enabled)
        query = IsLocalizationEnabled
            ? await ApplyLocalizedSearchAsync(query, queryParameters, cancellationToken)
            : query.ApplySearch(queryParameters);

        // Apply sorting
        query = query.ApplySort(queryParameters);

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var pagedQuery = query
            .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize);

        var queryWithIncludes = ApplyIncludes(pagedQuery);
        List<TDto> items;
        if (ReferenceEquals(queryWithIncludes, pagedQuery))
        {
            items = await pagedQuery
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }
        else
        {
            var entities = await queryWithIncludes.ToListAsync(cancellationToken);
            items = _mapper.Map<List<TDto>>(entities);
        }

        // Post-read hook for localization
        if (IsLocalizationEnabled && items.Count > 0)
        {
            await PostReadLocalizationAsync(items, cancellationToken);
        }

        // Normalize pagination metadata for unpaged results (PageSize = -1)
        var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

        return new PagedResult<TDto>(items, totalCount, pageNumber, pageSize);
    }

    public virtual async Task<TDto?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return default;

        var dto = _mapper.Map<TDto>(entity);

        // Post-read hook for localization
        if (IsLocalizationEnabled && dto != null)
        {
            await PostReadLocalizationAsync(new[] { dto }, cancellationToken);
        }
		
        return dto;
    }


    /// <summary>
    /// Creates a new entity from the given DTO.
    /// </summary>
    public virtual async Task<TDto> CreateAsync(TCreateDto createDto, CancellationToken cancellationToken = default)
    {
        if (IsLocalizationEnabled)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. Map DTO to entity (WITHOUT localization processing first)
                var entity = _mapper.Map<TEntity>(createDto);

                // 2. Run validation before persisting
                var validationResult = await ValidateForCreateAsync(entity, cancellationToken);
                if (!validationResult.IsValid)
                {
                    throw new ValidationException("Validation failed for create operation", validationResult.ToDictionary(), OperationType.Create);
                }

                // 3. Persist entity FIRST to get auto-generated ID
                await _repository.AddAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 4. Get the real entity ID
                var entityId = GetEntityId(entity);

                // 5. NOW process localization with the REAL entity ID
                await PreSaveLocalizationAsync(createDto, entityId, cancellationToken);

                // 6. Re-map the DTO (now with localization keys) to update the entity
                _mapper.Map(createDto, entity);

                // 7. Update entity with localization keys
                await _repository.UpdateAsync(entity, cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // 8. Map to response DTO and localize for read
                var dto = _mapper.Map<TDto>(entity);
                await PostReadLocalizationAsync(new[] { dto }, cancellationToken);

                return dto;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        else
        {
            var entity = _mapper.Map<TEntity>(createDto);

            var validationResult = await ValidateForCreateAsync(entity, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException("Validation failed for create operation", validationResult.ToDictionary(), OperationType.Create);
            }

            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<TDto>(entity);
        }
    }

  
    private static readonly Lazy<PropertyInfo?> _idPropertyCache = new(() =>
        typeof(TEntity).GetProperty("Id")
        ?? typeof(TEntity).GetProperty("ID")
        ?? typeof(TEntity).GetProperty($"{typeof(TEntity).Name}Id"));

    /// <summary>
    /// Extracts the entity ID as a string. Override for custom ID types.
    /// </summary>
    protected virtual string? GetEntityId(TEntity entity)
    {
        var idProperty = _idPropertyCache.Value;
        if (idProperty == null)
            return null;

        var value = idProperty.GetValue(entity);
        return value?.ToString();
    }

    // Validation order is consistent in both paths:
    // 1. (Localized only) PreSaveLocalizationAsync - transforms DTO values
    // 2. Map DTO → Entity
    // 3. Validate (after mapping, comparing old vs new entity state)
    // 4. Save
    public virtual async Task<TDto?> UpdateAsync(TKey id, TUpdateDto updateDto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return default;

        // Create a shallow clone BEFORE mapping to capture current state
        var currentEntitySnapshot = CloneEntity(entity);

        if (IsLocalizationEnabled)
        {
            // Start transaction BEFORE localization writes to ensure atomicity
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Pre-save hook: converts display text to localization keys in updateDto
                await PreSaveLocalizationAsync(updateDto, Convert.ToString(id), cancellationToken);

                // Map AFTER localization processing
                _mapper.Map(updateDto, entity);

                // Validate AFTER mapping so we can compare old vs new state
                var validationResult = await ValidateForDeactivationAsync(id, currentEntitySnapshot, entity, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var firstError = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed for deactivation";
                    throw new ValidationException(firstError, validationResult.ToDictionary(), OperationType.Update);
                }

                await _repository.UpdateAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var dto = _mapper.Map<TDto>(entity);
                await PostReadLocalizationAsync(new[] { dto }, cancellationToken);
                return dto;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        else
        {
            // Non-localized path
            _mapper.Map(updateDto, entity);

            // Validate AFTER mapping
            var validationResult = await ValidateForDeactivationAsync(id, currentEntitySnapshot, entity, cancellationToken);
            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed for deactivation";
                throw new ValidationException(firstError, validationResult.ToDictionary(), OperationType.Update);
            }

            await _repository.UpdateAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return _mapper.Map<TDto>(entity);
        }
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

        if (IsLocalizationEnabled)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Get the actual localization keys from the entity before soft deleting
                var localizationKeys = ExtractLocalizationKeys(entity);

                // Deactivate localization entries (NOT delete)
                if (localizationKeys.Any())
                {
                    var resource = LocalizationProcessor.GetResource<TDto>();
                    await _localizationProcessor!.ProcessDeactivateAsync(resource, localizationKeys);
                }

                await _repository.DeleteAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        else
        {
            await _repository.DeleteAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }


    // Cache MemberwiseClone method per entity type
    private static readonly ConcurrentDictionary<Type, MethodInfo> _cloneMethodCache = new();

    /// <summary>
    /// Creates a shallow clone of an entity for validation purposes.
    /// This is sufficient for checking property changes like IsActive transitions.
    /// </summary>
    /// <param name="entity">The entity to clone</param>
    /// <returns>A shallow copy of the entity</returns>
    private TEntity CloneEntity(TEntity entity)
    {
        var cloneMethod = _cloneMethodCache.GetOrAdd(
            entity.GetType(),
            t => t.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!);

        return (TEntity)cloneMethod.Invoke(entity, null)!;
    }

    /// <summary>
    /// Validates an entity before creating it.
    /// </summary>
    /// <remarks>
    /// <para><b>Important:</b> Validation methods must be read-only.</para>
    /// <list type="bullet">
    ///   <item>Do NOT modify the <paramref name="entity"/> or any tracked entities.</item>
    ///   <item>Use <c>AsNoTracking()</c> for any repository queries.</item>
    /// </list>
    /// </remarks>
    protected virtual Task<ValidationResult> ValidateForCreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidationResult.Success());
    }

    /// <summary>
    /// Validates an entity for deactivation (when IsActive changes from true to false).
    /// </summary>
    /// <remarks>
    /// <para><b>Important:</b> Validation methods must be read-only.</para>
    /// <list type="bullet">
    ///   <item>Do NOT modify <paramref name="currentEntity"/>, <paramref name="updatedEntity"/>, or any tracked entities.</item>
    ///   <item>Use <c>AsNoTracking()</c> for any repository queries.</item>
    ///   <item><paramref name="updatedEntity"/> may be an untracked clone in bulk operations.</item>
    /// </list>
    /// </remarks>
    protected virtual Task<ValidationResult> ValidateForDeactivationAsync(TKey id, TEntity currentEntity, TEntity updatedEntity, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidationResult.Success());
    }

    /// <summary>
    /// Validates an entity before deleting it.
    /// </summary>
    /// <remarks>
    /// <para><b>Important:</b> Validation methods must be read-only.</para>
    /// <list type="bullet">
    ///   <item>Do NOT modify the <paramref name="entity"/> or any tracked entities.</item>
    ///   <item>Use <c>AsNoTracking()</c> for any repository queries.</item>
    /// </list>
    /// </remarks>
    protected virtual Task<ValidationResult> ValidateForDeleteAsync(TKey id, TEntity entity, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidationResult.Success());
    }

    #endregion



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
    /// <param name="items">The array of DTOs to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A bulk result containing created DTOs, error count, and validation errors</returns>
    public virtual async Task<BulkResult<TDto>> BulkCreateAsync(TCreateDto[] items, CancellationToken cancellationToken = default)
    {
        if (items.Length == 0)
            return new BulkResult<TDto>(0, 0, []);

        if (IsLocalizationEnabled)
        {
            return await BulkCreateWithLocalizationAsync(items, cancellationToken);
        }
        else
        {
            return await BulkCreateWithoutLocalizationAsync(items, cancellationToken);
        }
    }

    /// <summary>
    /// Bulk create without localization support (backward compatible).
    /// </summary>
    private async Task<BulkResult<TDto>> BulkCreateWithoutLocalizationAsync(TCreateDto[] items, CancellationToken cancellationToken)
    {
        var createdEntities = new List<TEntity>();
        var validationErrors = new List<string>();
        var validationFailedCount = 0;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            for (int i = 0; i < items.Length; i++)
            {
                var entity = _mapper.Map<TEntity>(items[i]);

                // Run validation before persisting
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
    /// Bulk create with localization support.
    /// 
    /// <para><b>Transaction Behavior:</b></para>
    /// <para>
    /// This method uses a single transaction for both entity creation and localization.
    /// If localization fails for ANY item, the entire transaction is rolled back - no entities are created.
    /// This ensures data consistency between entities and their localization entries.
    /// </para>
    /// </summary>
    private async Task<BulkResult<TDto>> BulkCreateWithLocalizationAsync(TCreateDto[] items, CancellationToken cancellationToken)
    {
        var createdEntities = new List<TEntity>();
        var validationErrors = new List<string>();
        var validationFailedCount = 0;
        var itemEntityMap = new List<(TCreateDto Dto, TEntity Entity, int Index)>();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Step 1: Map and validate all items
            for (int i = 0; i < items.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entity = _mapper.Map<TEntity>(items[i]);

                var validationResult = await ValidateForCreateAsync(entity, cancellationToken);
                if (!validationResult.IsValid)
                {
                    validationFailedCount++;
                    var errorMessages = string.Join(", ", validationResult.Errors.Select(e =>
                        string.IsNullOrEmpty(e.PropertyName) ? e.ErrorMessage : $"{e.PropertyName}: {e.ErrorMessage}"));
                    validationErrors.Add($"Item {i}: {errorMessages}");
                    continue;
                }

                itemEntityMap.Add((items[i], entity, i));
                createdEntities.Add(entity);
            }

            if (createdEntities.Count == 0)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return new BulkResult<TDto>(0, validationFailedCount, [], validationErrors.Count > 0 ? validationErrors : null);
            }

            // Step 2: Persist entities FIRST to get auto-generated IDs
            await _repository.AddRangeAsync(createdEntities.ToArray(), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Step 3: Process localization for all items with REAL entity IDs
            // FAIL-FAST: If any localization fails, rollback entire transaction
            foreach (var (dto, entity, index) in itemEntityMap)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entityId = GetEntityId(entity);
                await PreSaveLocalizationAsync(dto, entityId, cancellationToken);

                // Re-map the DTO (now with localization keys) to update the entity
                _mapper.Map(dto, entity);
            }

            // Step 4: Update entities with localization keys
            foreach (var entity in createdEntities)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _repository.UpdateAsync(entity, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Step 5: Map to response DTOs and localize for read
            var results = _mapper.Map<List<TDto>>(createdEntities);
            await PostReadLocalizationAsync(results, cancellationToken);

            return new BulkResult<TDto>(
                results.Count,
                validationFailedCount,
                results,
                validationErrors.Count > 0 ? validationErrors : null);
        }
        catch (OperationCanceledException)
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
    /// <summary>
    /// Creates multiple entities from a range of values in a single transaction.
    /// 
    /// <para><b>Transaction Behavior:</b></para>
    /// <para>
    /// This method uses a single transaction for both entity creation and localization.
    /// If localization fails for ANY item, the entire transaction is rolled back - no entities are created.
    /// This ensures data consistency between entities and their localization entries.
    /// </para>
    /// </summary>
    public virtual async Task<RangeResult<TDto>> CreateFromRangeAsync(RangeCreateRequest<TCreateDto> request, Func<TCreateDto, string, int, TCreateDto> transformer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(transformer);

        var rangeValues = RangeGenerator.GenerateRangeValues(request.RangeFrom, request.RangeTo, request.Prefix, request.Suffix);

        if (rangeValues.Count == 0)
            return new RangeResult<TDto>(0, 0, []);

        var createdEntities = new List<TEntity>();
        var itemDtoMap = new List<(TCreateDto Dto, TEntity Entity)>();
        var errors = new List<string>();
        var sequenceNo = request.StartSequenceNo;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Step 1: Create all entities
            foreach (var rangeValue in rangeValues)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var createDto = transformer(request.Template, rangeValue, sequenceNo);
                    var entity = _mapper.Map<TEntity>(createDto);

                    await _repository.AddAsync(entity, cancellationToken);
                    createdEntities.Add(entity);

                    if (IsLocalizationEnabled)
                    {
                        itemDtoMap.Add((createDto, entity));
                    }

                    sequenceNo++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to create record for value '{rangeValue}': {ex.Message}");
                }
            }

            if (createdEntities.Count == 0)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return new RangeResult<TDto>(0, errors.Count, [], errors.Count > 0 ? errors : null);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Step 2: Process localization with real entity IDs (if enabled)
            // FAIL-FAST: If any localization fails, rollback entire transaction
            if (IsLocalizationEnabled && itemDtoMap.Count > 0)
            {
                foreach (var (dto, entity) in itemDtoMap)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var entityId = GetEntityId(entity);
                    await PreSaveLocalizationAsync(dto, entityId, cancellationToken);
                    _mapper.Map(dto, entity);
                }

                // Update entities with localization keys
                foreach (var entity in createdEntities)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _repository.UpdateAsync(entity, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var results = _mapper.Map<List<TDto>>(createdEntities);

            if (IsLocalizationEnabled && results.Count > 0)
            {
                await PostReadLocalizationAsync(results, cancellationToken);
            }

            return new RangeResult<TDto>(results.Count, errors.Count, results, errors.Count > 0 ? errors : null);
        }
        catch (OperationCanceledException)
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return new RangeResult<TDto>(0, createdEntities.Count, [], [$"Database error: {ex.InnerException?.Message ?? ex.Message}"]);
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
                if (entity is null)
                {
                    failedCount++;
                    errors.Add($"Record with Id '{item.Id}' not found.");
                    continue;
                }

                // Create snapshot BEFORE any modification
                var currentEntitySnapshot = CloneEntity(entity);

                // Validate against an UNTRACKED clone to avoid corrupting the EF change tracker.
                // If we mapped directly onto the tracked entity and validation failed,
                // SaveChangesAsync would still persist the invalid mutation.
                var tempEntity = CloneEntity(entity);
                _mapper.Map(item.Data, tempEntity);

                var validationResult = await ValidateForDeactivationAsync(item.Id, currentEntitySnapshot, tempEntity, cancellationToken);
                if (!validationResult.IsValid)
                {
                    failedCount++;
                    var errorMessages = string.Join(", ", validationResult.Errors.Select(e =>
                        string.IsNullOrEmpty(e.PropertyName) ? e.ErrorMessage : $"{e.PropertyName}: {e.ErrorMessage}"));
                    errors.Add($"Record with Id '{item.Id}': {errorMessages}");
                    continue;
                }

                // Validation passed — now safe to process localization and apply to tracked entity
                if (IsLocalizationEnabled)
                {
                    await PreSaveLocalizationAsync(item.Data, Convert.ToString(item.Id), cancellationToken);
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

        if (IsLocalizationEnabled && results.Count > 0)
        {
            await PostReadLocalizationAsync(results, cancellationToken);
        }

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
        var allLocalizationKeys = new List<string>();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var id in ids)
            {
                var entity = await _repository.GetByIdAsync(id, cancellationToken);
                if (entity is null)
                {
                    failedCount++;
                    errors.Add($"Record with Id '{id}' not found.");
                    continue;
                }

                var validationResult = await ValidateForDeleteAsync(id, entity, cancellationToken);
                if (!validationResult.IsValid)
                {
                    failedCount++;
                    var errorMessages = string.Join(", ", validationResult.Errors.Select(e =>
                        string.IsNullOrEmpty(e.PropertyName) ? e.ErrorMessage : $"{e.PropertyName}: {e.ErrorMessage}"));
                    errors.Add($"Record with Id '{id}': {errorMessages}");
                    continue;
                }

                if (IsLocalizationEnabled)
                {
                    var keys = ExtractLocalizationKeys(entity);
                    allLocalizationKeys.AddRange(keys);
                }

                await _repository.DeleteAsync(entity, cancellationToken);
                deletedIds.Add(id);
            }

            if (IsLocalizationEnabled && allLocalizationKeys.Count > 0)
            {
                var resource = LocalizationProcessor.GetResource<TDto>();
                await _localizationProcessor!.ProcessDeactivateAsync(resource, allLocalizationKeys.Distinct());
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

    /// <summary>
    /// Pre-filter hook: Translates localized filter values to keys before query execution.
    /// Uses a single batched DB query for all filter properties instead of one query per property.
    /// </summary>

    // Cached: query parameter properties that overlap with localizable DTO properties (static per generic type)
    private static readonly Lazy<IReadOnlyList<PropertyInfo>> _localizableQueryPropsCache = new(() =>
    {
        var localizableNames = _localizablePropNamesCache!.Value;
        return typeof(TQueryParams).GetProperties()
            .Where(p => p.PropertyType == typeof(string)
                     && localizableNames.Contains(p.Name))
            .ToList();
    });

    protected virtual async Task<IQueryable<TEntity>> PreFilterLocalizationAsync(IQueryable<TEntity> query, TQueryParams queryParameters, CancellationToken cancellationToken)
    {
        if (_localizedQueryService == null || !HasLocalizableProperties<TDto>())
            return query;

        var queryProps = _localizableQueryPropsCache.Value;
        if (queryProps.Count == 0)
            return query;

        // Collect all non-empty filter values in one pass
        var propsWithValues = new List<(PropertyInfo Prop, string Value)>();
        for (int i = 0; i < queryProps.Count; i++)
        {
            var value = queryProps[i].GetValue(queryParameters) as string;
            if (!string.IsNullOrWhiteSpace(value))
                propsWithValues.Add((queryProps[i], value));
        }

        if (propsWithValues.Count == 0)
            return query;

        var resource = LocalizationProcessor.GetResource<TDto>();

        // Single batched DB query for all filter values
        var allValues = propsWithValues.Select(p => p.Value).Distinct();
        var batchResults = await _localizedQueryService.GetKeysByLocalizedValuesBatchAsync(
            resource,
            allValues,
            CurrentLanguage,
            exactMatch: false,
            cancellationToken);

        // Apply results back to query
        var entityType = typeof(TEntity);
        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "x");
        
        foreach (var (prop, value) in propsWithValues)
        {
            if (batchResults.TryGetValue(value, out var matchingKeys) && matchingKeys.Count > 0)
            {
                // Find corresponding entity property
                if (!_entityLocalizablePropsCache.Value.TryGetValue(prop.Name, out var entityProp))
                    continue;

                // Build: x => matchingKeys.Contains(x.Property)
                var propertyAccess = System.Linq.Expressions.Expression.Property(parameter, entityProp);
                var keySet = matchingKeys.ToHashSet();
                var keySetConstant = System.Linq.Expressions.Expression.Constant(keySet);
                var containsCall = System.Linq.Expressions.Expression.Call(keySetConstant, _hashSetContainsMethod, propertyAccess);

                query = query.Where(System.Linq.Expressions.Expression.Lambda<Func<TEntity, bool>>(containsCall, parameter));

                // Clear the property on queryParameters so ApplyFilters doesn't filter by the original text value
                prop.SetValue(queryParameters, null);
            }
        }

        return query;
    }

    /// <summary>
    /// Search hook: Applies search across localized fields by finding matching keys.
    /// Also searches non-localized fields using standard search logic.
    /// </summary>
    // ── Cached reflection results (static, shared across all instances of same generic type) ──
    private static readonly Lazy<HashSet<string>> _localizablePropNamesCache = new(() =>
        typeof(TDto).GetProperties()
            .Where(p => p.GetCustomAttribute<IsLocalizableAttribute>() != null)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase));

    private static readonly MethodInfo _toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
    private static readonly MethodInfo _containsStringMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
    private static readonly MethodInfo _hashSetContainsMethod = typeof(HashSet<string>).GetMethod("Contains", new[] { typeof(string) })!;

    protected virtual async Task<IQueryable<TEntity>> ApplyLocalizedSearchAsync(
        IQueryable<TEntity> query,
        TQueryParams queryParameters,
        CancellationToken cancellationToken)
    {
        if (_localizedQueryService == null || !HasLocalizableProperties<TDto>())
            return query.ApplySearch(queryParameters);

        var searchTerm = queryParameters.SearchTerm;
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var resource = LocalizationProcessor.GetResource<TDto>();
        var matchingKeys = await _localizedQueryService.SearchLocalizedKeysAsync(
            resource,
            searchTerm,
            CurrentLanguage,
            cancellationToken);

        var entityType = typeof(TEntity);
        var localizablePropNames = _localizablePropNamesCache.Value;

        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "x");
        System.Linq.Expressions.Expression? combinedExpression = null;

        // 1. Build expression for localized fields using matching keys
        if (matchingKeys.Count > 0)
        {
            var keySet = matchingKeys.ToHashSet();
            var keySetConstant = System.Linq.Expressions.Expression.Constant(keySet);

            foreach (var (propName, entityProp) in _entityLocalizablePropsCache.Value)
            {
                var propertyAccess = System.Linq.Expressions.Expression.Property(parameter, entityProp);
                var containsCall = System.Linq.Expressions.Expression.Call(keySetConstant, _hashSetContainsMethod, propertyAccess);

                combinedExpression = combinedExpression == null
                    ? containsCall
                    : System.Linq.Expressions.Expression.OrElse(combinedExpression, containsCall);
            }
        }

        // 2. Build expression for non-localized searchable fields (fallback/combined search)
        var searchableProps = GetSearchableProperties(entityType, queryParameters)
            .Where(p => !localizablePropNames.Contains(p.Name))
            .ToList();

        if (searchableProps.Count > 0)
        {
            var searchTermLower = searchTerm.ToLower();
            var searchTermConstant = System.Linq.Expressions.Expression.Constant(searchTermLower);

            foreach (var prop in searchableProps)
            {
                var propertyAccess = System.Linq.Expressions.Expression.Property(parameter, prop);

                var nullCheck = System.Linq.Expressions.Expression.NotEqual(
                    propertyAccess,
                    System.Linq.Expressions.Expression.Constant(null, typeof(string)));

                var toLowerCall = System.Linq.Expressions.Expression.Call(propertyAccess, _toLowerMethod);
                var containsCall = System.Linq.Expressions.Expression.Call(toLowerCall, _containsStringMethod, searchTermConstant);
                var safeContains = System.Linq.Expressions.Expression.AndAlso(nullCheck, containsCall);

                combinedExpression = combinedExpression == null
                    ? safeContains
                    : System.Linq.Expressions.Expression.OrElse(combinedExpression, safeContains);
            }
        }

        // 3. If no localized matches and no searchable props, fallback to standard search
        if (combinedExpression == null)
        {
            return query.ApplySearch(queryParameters);
        }

        var lambda = System.Linq.Expressions.Expression.Lambda<Func<TEntity, bool>>(combinedExpression, parameter);
        return query.Where(lambda);
    }

    
    // Cache searchable properties per entity type
    private static readonly Lazy<IReadOnlyList<PropertyInfo>> _defaultSearchablePropsCache = new(() =>
        typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string) && !p.Name.EndsWith("Id"))
            .ToList());

    private static readonly Lazy<PropertyInfo?> _searchFieldsPropertyCache = new(() =>
        typeof(TQueryParams).GetProperty("SearchFields"));


    // Add at class level
    private static readonly Lazy<Dictionary<string, PropertyInfo>> _entityStringPropsCache = new(() =>
        typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string))
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Gets searchable properties from the entity based on query parameters.
    /// </summary>
    private static IEnumerable<PropertyInfo> GetSearchableProperties(Type entityType, TQueryParams queryParameters)
    {
        var searchFieldsProp = _searchFieldsPropertyCache.Value;
        var searchFields = searchFieldsProp?.GetValue(queryParameters) as string[];

        if (searchFields != null && searchFields.Length > 0)
        {
            var entityProps = _entityStringPropsCache.Value;
            return searchFields
                .Select(f => entityProps.TryGetValue(f, out var prop) ? prop : null)
                .Where(p => p != null)!;
        }

        return _defaultSearchablePropsCache.Value;
    }

    /// <summary>
    /// Pre-save hook: Processes localization before saving (converts values to keys).
    /// </summary>
    protected virtual async Task PreSaveLocalizationAsync<TInput>(TInput dto, string? entityId, CancellationToken cancellationToken)
        where TInput : class
    {
        if (_localizationProcessor != null)
        {
            await _localizationProcessor.ProcessSaveAsync(dto, entityId);
        }
    }

    /// <summary>
    /// Post-read hook: Processes localization after reading (converts keys to values).
    /// </summary>
    protected virtual async Task PostReadLocalizationAsync(IEnumerable<TDto> dtos, CancellationToken cancellationToken)
    {
        if (_localizationProcessor != null)
        {
            await _localizationProcessor.ProcessGetAsync(dtos);
        }
    }

    /// <summary>
    /// Checks if the DTO has any localizable properties.
    /// Cached per type — reflection happens only once per type in the app lifetime.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, bool> _localizableTypeCache = new();
    private static bool HasLocalizableProperties<T>()
    {
        return _localizableTypeCache.GetOrAdd(typeof(T), static t =>
            t.GetProperties().Any(p => p.GetCustomAttribute<IsLocalizableAttribute>() != null));
    }
    
    // Add static cache for entity properties at class level
    private static readonly Lazy<Dictionary<string, PropertyInfo>> _entityLocalizablePropsCache = new(() =>
    {
        var entityType = typeof(TEntity);
        var localizableNames = _localizablePropNamesCache.Value;

        return localizableNames
            .Select(name => (Name: name, Prop: entityType.GetProperty(name)))
            .Where(x => x.Prop != null && x.Prop.PropertyType == typeof(string))
            .ToDictionary(x => x.Name, x => x.Prop!);
    });

    // Cache localizable property names per DTO type (already exists in _localizablePropNamesCache)
    // ExtractLocalizationKeys should use this cache
    private List<string> ExtractLocalizationKeys(TEntity entity)
    {
        var keys = new List<string>();

        // Use cached property info
        foreach (var (_, prop) in _entityLocalizablePropsCache.Value)
        {
            var keyValue = prop.GetValue(entity) as string;
            if (!string.IsNullOrWhiteSpace(keyValue))
            {
                keys.Add(keyValue);
            }
        }

        return keys;
    }
}
