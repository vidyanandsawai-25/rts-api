using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for managing Screen Form Section operations.
/// Provides CRUD operations for screen form sections with reference validation.
/// </summary>
public class ScreenFormSectionMasterService : BaseCommonCrudService<ScreenFormSectionMasterEntity, ScreenFormSectionMasterDto, CreateScreenFormSectionMasterDto, UpdateScreenFormSectionMasterDto,
          ScreenFormSectionMasterQueryParameters, int>, IScreenFormSectionMasterService
{
    private readonly IReferenceValidationService _referenceValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenFormSectionMasterService"/> class.
    /// </summary>
    /// <param name="repository">The repository for screen form section entities.</param>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="mapper">The AutoMapper instance for object mapping.</param>
    /// <param name="referenceValidator">The reference validation service for checking entity references.</param>
    public ScreenFormSectionMasterService(
           IRepository<ScreenFormSectionMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
           IMapper mapper,
        IReferenceValidationService referenceValidator)
           : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        ScreenFormSectionMasterEntity currentEntity,
    ScreenFormSectionMasterEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<ScreenFormSectionMasterEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
    int id,
        ScreenFormSectionMasterEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<ScreenFormSectionMasterEntity>(id, cancellationToken);
    }
}