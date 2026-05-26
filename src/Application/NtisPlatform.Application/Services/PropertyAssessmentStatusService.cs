using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyAssessmentStatus;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PropertyAssessmentStatusService
    : BaseCommonCrudService<PropertyAssessmentStatusEntity, PropertyAssessmentStatusDto, CreatePropertyAssessmentStatusDto, UpdatePropertyAssessmentStatusDto, PropertyAssessmentStatusQueryParameters, int>,
      IPropertyAssessmentStatusService
{
    private readonly IReferenceValidationService _referenceValidator;

    public PropertyAssessmentStatusService(
        IRepository<PropertyAssessmentStatusEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        PropertyAssessmentStatusEntity currentEntity,
        PropertyAssessmentStatusEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<PropertyAssessmentStatusEntity>(id, cancellationToken);
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        PropertyAssessmentStatusEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<PropertyAssessmentStatusEntity>(id, cancellationToken);
    }
}
