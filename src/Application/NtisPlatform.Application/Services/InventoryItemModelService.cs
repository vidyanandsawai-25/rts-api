using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
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
    private readonly IReferenceValidationService _referenceValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItemModelService"/> class.
    /// </summary>
    /// <param name="repository">The repository for inventory item model entities.</param>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="mapper">The AutoMapper instance for object mapping.</param>
    /// <param name="referenceValidator">The reference validation service for checking entity references.</param>
    public InventoryItemModelService(
        IRepository<InventoryItemModelEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        InventoryItemModelEntity currentEntity,
        InventoryItemModelEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
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
}
