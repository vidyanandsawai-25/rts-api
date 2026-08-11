using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for managing inventory item name operations.
/// Provides CRUD operations for inventory item names and descriptions.
/// </summary>
public class InventoryItemNameService : BaseCommonCrudService<InventoryItemNameEntity, InventoryItemNameDto, CreateInventoryItemNameDto, UpdateInventoryItemNameDto, InventoryItemNameQueryParameters, int>,
    IInventoryItemNameService
{
    private readonly IRepository<InventoryItemCategoryEntity, int> _inventoryItemCategoryRepository;
    private readonly IReferenceValidationService _referenceValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItemNameService"/> class.
    /// </summary>
    /// <param name="repository">The repository for inventory item name entities.</param>
    /// <param name="inventoryItemCategoryRepository">The repository for the parent inventory item category, used to validate <see cref="InventoryItemNameEntity.InventoryItemCategoryId"/> on create/update.</param>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="mapper">The AutoMapper instance for object mapping.</param>
    /// <param name="referenceValidator">The reference validation service for checking entity references.</param>
    public InventoryItemNameService(
      IRepository<InventoryItemNameEntity, int> repository,
        IRepository<InventoryItemCategoryEntity, int> inventoryItemCategoryRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _inventoryItemCategoryRepository = inventoryItemCategoryRepository;
        _referenceValidator = referenceValidator;
    }

    /// <summary>
    /// Overrides the base list query to enrich each row with InventoryItemCategoryName (the parent
    /// InventoryItemCategoryEntity's TypeName) via a SQL join, reusing the same
    /// _inventoryItemCategoryRepository already injected for FK-existence validation on Create/Update.
    /// InventoryItemNameEntity deliberately holds only the InventoryItemCategoryId FK (no navigation
    /// property), so this can't be done through the base class's ProjectTo/ApplyIncludes path.
    /// Preserves the base order: Filter -> Search -> Sort -> Count -> Skip/Take -> Project.
    /// </summary>
    public override async Task<PagedResult<InventoryItemNameDto>> GetAllAsync(
        InventoryItemNameQueryParameters queryParameters, CancellationToken cancellationToken = default)
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
            from n in pagedQuery
            join c in _inventoryItemCategoryRepository.GetQueryable().AsNoTracking()
                on n.InventoryItemCategoryId equals c.Id into cJoin
            from c in cJoin.DefaultIfEmpty()
            select new InventoryItemNameDto
            {
                Id = n.Id,
                IsActive = n.IsActive,
                CreatedDate = n.CreatedDate,
                UpdatedDate = n.UpdatedDate,
                InventoryItemCategoryId = n.InventoryItemCategoryId,
                InventoryItemCategoryName = c != null ? c.TypeName : null,
                SubTypeCode = n.SubTypeCode,
                SubTypeName = n.SubTypeName,
                Description = n.Description,
                DisplayOrder = n.DisplayOrder
            }
        ).ToListAsync(cancellationToken);

        var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

        return new PagedResult<InventoryItemNameDto>(items, totalCount, pageNumber, pageSize);
    }

    public override async Task<InventoryItemNameDto> CreateAsync(
        CreateInventoryItemNameDto createDto, CancellationToken cancellationToken = default)
    {
        await EnsureInventoryItemCategoryExistsAsync(createDto.InventoryItemCategoryId, OperationType.Create, cancellationToken);
        return await base.CreateAsync(createDto, cancellationToken);
    }

    public override async Task<InventoryItemNameDto?> UpdateAsync(
        int id, UpdateInventoryItemNameDto updateDto, CancellationToken cancellationToken = default)
    {
        await EnsureInventoryItemCategoryExistsAsync(updateDto.InventoryItemCategoryId, OperationType.Update, cancellationToken);
        return await base.UpdateAsync(id, updateDto, cancellationToken);
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        InventoryItemNameEntity entity, CancellationToken cancellationToken = default)
    {
        return await CheckDuplicateAsync(entity, excludeId: null, cancellationToken);
    }

    // Note: mirrors ValidateForCreateAsync's duplicate check because the base service only invokes
    // this hook (not ValidateForCreateAsync) on Update/BulkUpdate — the duplicate check excludes the
    // row being updated so renaming an item name to its own current name/code is not flagged.
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
     int id,
        InventoryItemNameEntity currentEntity,
     InventoryItemNameEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        var duplicateResult = await CheckDuplicateAsync(updatedEntity, excludeId: id, cancellationToken);
        if (!duplicateResult.IsValid)
            return duplicateResult;

        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<InventoryItemNameEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
      InventoryItemNameEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<InventoryItemNameEntity>(id, cancellationToken);
    }

    /// <summary>
    /// SubTypeName and SubTypeCode only need to be unique within their parent category (e.g. two
    /// different categories can both have a "Standard" item name), so the duplicate check is scoped
    /// to <see cref="InventoryItemNameEntity.InventoryItemCategoryId"/>, excluding
    /// <paramref name="excludeId"/> on update.
    /// </summary>
    private async Task<ValidationResult> CheckDuplicateAsync(
        InventoryItemNameEntity entity, int? excludeId, CancellationToken cancellationToken)
    {
        var duplicateName = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id != (excludeId ?? 0)
                        && x.InventoryItemCategoryId == entity.InventoryItemCategoryId
                        && x.SubTypeName == entity.SubTypeName
                        && !x.MarkedForDeletion, cancellationToken);

        if (duplicateName)
            return ValidationResult.Failure(nameof(entity.SubTypeName), "InventoryItemName_SubTypeName_Duplicate");

        var duplicateCode = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id != (excludeId ?? 0)
                        && x.InventoryItemCategoryId == entity.InventoryItemCategoryId
                        && x.SubTypeCode == entity.SubTypeCode
                        && !x.MarkedForDeletion, cancellationToken);

        return duplicateCode
            ? ValidationResult.Failure(nameof(entity.SubTypeCode), "InventoryItemName_SubTypeCode_Duplicate")
            : ValidationResult.Success();
    }

    private async Task EnsureInventoryItemCategoryExistsAsync(int inventoryItemCategoryId, OperationType operationType, CancellationToken cancellationToken)
    {
        var exists = await _inventoryItemCategoryRepository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id == inventoryItemCategoryId && x.IsActive && !x.MarkedForDeletion, cancellationToken);

        if (!exists)
            throw new ValidationException(nameof(CreateInventoryItemNameDto.InventoryItemCategoryId), $"Inventory item category with ID {inventoryItemCategoryId} not found.", operationType);
    }
}
