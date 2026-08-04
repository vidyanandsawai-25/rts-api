using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for managing inventory item category operations.
/// Provides CRUD operations for inventory item categories.
/// </summary>
public class InventoryItemCategoryService : BaseCommonCrudService<InventoryItemCategoryEntity, InventoryItemCategoryDto, CreateInventoryItemCategoryDto, UpdateInventoryItemCategoryDto, InventoryItemCategoryQueryParameters, int>,
    IInventoryItemCategoryService
{
    private readonly IReferenceValidationService _referenceValidator;
    private readonly IRepository<AssetCategoryEntity, int> _assetCategoryRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItemCategoryService"/> class.
    /// </summary>
    /// <param name="repository">The repository for inventory item category entities.</param>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="mapper">The AutoMapper instance for object mapping.</param>
    /// <param name="referenceValidator">The reference validation service for checking entity references.</param>
    /// <param name="assetCategoryRepository">The repository for the AssetCategoryId FK, used to enrich list results with AssetCategoryName.</param>
    public InventoryItemCategoryService(
        IRepository<InventoryItemCategoryEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator,
        IRepository<AssetCategoryEntity, int> assetCategoryRepository)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
        _assetCategoryRepository = assetCategoryRepository;
    }

    /// <summary>
    /// Overrides the base list query to enrich each row with AssetCategoryName via a SQL join
    /// against AssetCategoryMaster (per CLAUDE.md Section 7 -- avoid in-memory lookups; join in SQL).
    /// InventoryItemCategoryEntity deliberately holds only the AssetCategoryId FK (no navigation
    /// property), so this can't be done through the base class's ProjectTo/ApplyIncludes path.
    /// Preserves the base order: Filter -> Search -> Sort -> Count -> Skip/Take -> Project.
    /// </summary>
    public override async Task<PagedResult<InventoryItemCategoryDto>> GetAllAsync(
        InventoryItemCategoryQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable();

        query = query.ApplyFilters(queryParameters);
        query = query.ApplySearch(queryParameters);
        query = query.ApplySort(queryParameters);

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedQuery = query
            .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize);

        var items = await (
            from c in pagedQuery
            join ac in _assetCategoryRepository.GetQueryable().AsNoTracking()
                on c.AssetCategoryId equals ac.Id into acJoin
            from ac in acJoin.DefaultIfEmpty()
            select new InventoryItemCategoryDto
            {
                Id = c.Id,
                IsActive = c.IsActive,
                CreatedDate = c.CreatedDate,
                UpdatedDate = c.UpdatedDate,
                AssetCategoryId = c.AssetCategoryId,
                AssetCategoryName = ac != null ? ac.CategoryName : null,
                TypeCode = c.TypeCode,
                TypeName = c.TypeName,
                Description = c.Description,
                DisplayOrder = c.DisplayOrder,
                DepreciationRate = c.DepreciationRate
            }
        ).ToListAsync(cancellationToken);

        var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

        return new PagedResult<InventoryItemCategoryDto>(items, totalCount, pageNumber, pageSize);
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        InventoryItemCategoryEntity entity, CancellationToken cancellationToken = default)
    {
        return await CheckDuplicateAsync(entity, excludeId: null, cancellationToken);
    }

    // Note: mirrors ValidateForCreateAsync's duplicate check because the base service only invokes
    // this hook (not ValidateForCreateAsync) on Update/BulkUpdate — the duplicate check excludes the
    // row being updated so renaming a category to its own current name/code is not flagged.
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        InventoryItemCategoryEntity currentEntity,
        InventoryItemCategoryEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        var duplicateResult = await CheckDuplicateAsync(updatedEntity, excludeId: id, cancellationToken);
        if (!duplicateResult.IsValid)
            return duplicateResult;

        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<InventoryItemCategoryEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
    int id,
          InventoryItemCategoryEntity entity,
          CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<InventoryItemCategoryEntity>(id, cancellationToken);
    }

    /// <summary>
    /// Checks TypeName and TypeCode (both required, per the DB's
    /// UQ_InventoryItemCategoryMaster_TypeName / _TypeCode constraints) for duplicates,
    /// excluding <paramref name="excludeId"/> on update.
    /// </summary>
    /// <remarks>
    /// Deliberately does NOT exclude <c>MarkedForDeletion</c> rows. Those unique constraints are
    /// plain (unfiltered) in the live DB -- a row that's only marked-for-deletion (pending the
    /// nightly <c>HardDeleteCleanupService</c> purge) still physically occupies its TypeCode/TypeName.
    /// Excluding it here would let this check pass while the actual INSERT still fails with a raw
    /// DB unique-constraint violation.
    /// </remarks>
    private async Task<ValidationResult> CheckDuplicateAsync(
        InventoryItemCategoryEntity entity, int? excludeId, CancellationToken cancellationToken)
    {
        var duplicateName = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id != (excludeId ?? 0)
                        && x.TypeName == entity.TypeName, cancellationToken);

        if (duplicateName)
            return ValidationResult.Failure(nameof(entity.TypeName), "InventoryItemCategory_TypeName_Duplicate");

        var duplicateCode = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id != (excludeId ?? 0)
                        && x.TypeCode == entity.TypeCode, cancellationToken);

        if (duplicateCode)
            return ValidationResult.Failure(nameof(entity.TypeCode), "InventoryItemCategory_TypeCode_Duplicate");

        return ValidationResult.Success();
    }
}
