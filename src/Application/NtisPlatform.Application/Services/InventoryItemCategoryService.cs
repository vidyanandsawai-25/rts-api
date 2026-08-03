using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItemCategoryService"/> class.
    /// </summary>
    /// <param name="repository">The repository for inventory item category entities.</param>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="mapper">The AutoMapper instance for object mapping.</param>
    /// <param name="referenceValidator">The reference validation service for checking entity references.</param>
    public InventoryItemCategoryService(
        IRepository<InventoryItemCategoryEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
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
    /// Checks TypeName (always required) and TypeCode (only when supplied, since it's optional)
    /// for duplicates among non-deleted rows, excluding <paramref name="excludeId"/> on update.
    /// </summary>
    private async Task<ValidationResult> CheckDuplicateAsync(
        InventoryItemCategoryEntity entity, int? excludeId, CancellationToken cancellationToken)
    {
        var duplicateName = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id != (excludeId ?? 0)
                        && x.TypeName == entity.TypeName
                        && !x.MarkedForDeletion, cancellationToken);

        if (duplicateName)
            return ValidationResult.Failure(nameof(entity.TypeName), "InventoryItemCategory_TypeName_Duplicate");

        if (!string.IsNullOrWhiteSpace(entity.TypeCode))
        {
            var duplicateCode = await _repository.GetQueryable().AsNoTracking()
                .AnyAsync(x => x.Id != (excludeId ?? 0)
                            && x.TypeCode == entity.TypeCode
                            && !x.MarkedForDeletion, cancellationToken);

            if (duplicateCode)
                return ValidationResult.Failure(nameof(entity.TypeCode), "InventoryItemCategory_TypeCode_Duplicate");
        }

        return ValidationResult.Success();
    }
}
