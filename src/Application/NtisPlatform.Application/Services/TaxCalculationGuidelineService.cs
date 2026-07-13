using AutoMapper;
using NtisPlatform.Application.DTOs.Master.TaxCalculationGuideline;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class TaxCalculationGuidelineService
    : BaseCommonCrudService<TaxCalculationGuidelineEntity, TaxCalculationGuidelineDto, CreateTaxCalculationGuidelineDto, UpdateTaxCalculationGuidelineDto, TaxCalculationGuidelineQueryParameters, int>,
      ITaxCalculationGuidelineService
{
    private readonly IReferenceValidationService _referenceValidator;

    public TaxCalculationGuidelineService(
        IRepository<TaxCalculationGuidelineEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        TaxCalculationGuidelineEntity currentEntity,
        TaxCalculationGuidelineEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<TaxCalculationGuidelineEntity>(id, cancellationToken);

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        TaxCalculationGuidelineEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<TaxCalculationGuidelineEntity>(id, cancellationToken);
    }
}
