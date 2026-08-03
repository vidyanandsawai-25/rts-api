using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for managing inventory item model operations.
/// Provides CRUD operations for inventory item models.
/// </summary>
public class InventoryItemModelService : BaseCommonCrudService<InventoryItemModelEntity, InventoryItemModelDto, CreateInventoryItemModelDto, UpdateInventoryItemModelDto, InventoryItemModelQueryParameters, int>,
  IInventoryItemModelService
{
    private readonly IRepository<InventoryItemNameEntity, int> _inventoryItemNameRepository;
    private readonly IReferenceValidationService _referenceValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItemModelService"/> class.
    /// </summary>
    /// <param name="repository">The repository for inventory item model entities.</param>
    /// <param name="inventoryItemNameRepository">The repository for the parent inventory item name, used to validate <see cref="InventoryItemModelEntity.InventoryItemNameId"/> on create/update.</param>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="mapper">The AutoMapper instance for object mapping.</param>
    /// <param name="referenceValidator">The reference validation service for checking entity references.</param>
    public InventoryItemModelService(
        IRepository<InventoryItemModelEntity, int> repository,
        IRepository<InventoryItemNameEntity, int> inventoryItemNameRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _inventoryItemNameRepository = inventoryItemNameRepository;
        _referenceValidator = referenceValidator;
    }

    public override async Task<InventoryItemModelDto> CreateAsync(
        CreateInventoryItemModelDto createDto, CancellationToken cancellationToken = default)
    {
        await EnsureInventoryItemNameExistsAsync(createDto.InventoryItemNameId, OperationType.Create, cancellationToken);
        return await base.CreateAsync(createDto, cancellationToken);
    }

    public override async Task<InventoryItemModelDto?> UpdateAsync(
        int id, UpdateInventoryItemModelDto updateDto, CancellationToken cancellationToken = default)
    {
        await EnsureInventoryItemNameExistsAsync(updateDto.InventoryItemNameId, OperationType.Update, cancellationToken);
        return await base.UpdateAsync(id, updateDto, cancellationToken);
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        InventoryItemModelEntity entity, CancellationToken cancellationToken = default)
    {
        return await CheckDuplicateAsync(entity, excludeId: null, cancellationToken);
    }

    // Note: mirrors ValidateForCreateAsync's duplicate check because the base service only invokes
    // this hook (not ValidateForCreateAsync) on Update/BulkUpdate — the duplicate check excludes the
    // row being updated so renaming a model to its own current name is not flagged.
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        InventoryItemModelEntity currentEntity,
        InventoryItemModelEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        var duplicateResult = await CheckDuplicateAsync(updatedEntity, excludeId: id, cancellationToken);
        if (!duplicateResult.IsValid)
            return duplicateResult;

        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<InventoryItemModelEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        InventoryItemModelEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<InventoryItemModelEntity>(id, cancellationToken);
    }

    /// <summary>
    /// A model name only needs to be unique within its parent item name (e.g. "Standard" can be a
    /// valid model under more than one item name), so the duplicate check is scoped to
    /// <see cref="InventoryItemModelEntity.InventoryItemNameId"/> + <see cref="InventoryItemModelEntity.ModelName"/>,
    /// excluding <paramref name="excludeId"/> on update.
    /// </summary>
    private async Task<ValidationResult> CheckDuplicateAsync(
        InventoryItemModelEntity entity, int? excludeId, CancellationToken cancellationToken)
    {
        var duplicate = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id != (excludeId ?? 0)
                        && x.InventoryItemNameId == entity.InventoryItemNameId
                        && x.ModelName == entity.ModelName
                        && !x.MarkedForDeletion, cancellationToken);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.ModelName), "InventoryItemModel_ModelName_Duplicate")
            : ValidationResult.Success();
    }

    private async Task EnsureInventoryItemNameExistsAsync(int inventoryItemNameId, OperationType operationType, CancellationToken cancellationToken)
    {
        var exists = await _inventoryItemNameRepository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id == inventoryItemNameId && x.IsActive && !x.MarkedForDeletion, cancellationToken);

        if (!exists)
            throw new ValidationException(nameof(CreateInventoryItemModelDto.InventoryItemNameId), $"Inventory item name with ID {inventoryItemNameId} not found.", operationType);
    }
}
