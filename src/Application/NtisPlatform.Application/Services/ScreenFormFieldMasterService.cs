using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for managing Screen Form Field operations.
/// Provides CRUD operations for screen form fields with reference validation.
/// </summary>
public class ScreenFormFieldMasterService : BaseCommonCrudService<ScreenFormFieldMasterEntity, ScreenFormFieldMasterDto, CreateScreenFormFieldMasterDto, UpdateScreenFormFieldMasterDto,
         ScreenFormFieldMasterQueryParameters, int>, IScreenFormFieldMasterService
{
    private readonly IReferenceValidationService _referenceValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenFormFieldMasterService"/> class.
    /// </summary>
    /// <param name="repository">The repository for screen form field entities.</param>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="mapper">The AutoMapper instance for object mapping.</param>
    /// <param name="referenceValidator">The reference validation service for checking entity references.</param>
    public ScreenFormFieldMasterService(
        IRepository<ScreenFormFieldMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
   IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        ScreenFormFieldMasterEntity currentEntity,
        ScreenFormFieldMasterEntity updatedEntity,
  CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<ScreenFormFieldMasterEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        ScreenFormFieldMasterEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<ScreenFormFieldMasterEntity>(id, cancellationToken);
    }
}