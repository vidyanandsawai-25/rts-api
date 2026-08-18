using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.EvidenceTypeMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.RetrospectiveTax;

public class EvidenceTypeMasterService : BaseCommonCrudService<EvidenceTypeMasterEntity, EvidenceTypeMasterDto, CreateEvidenceTypeMasterDto, UpdateEvidenceTypeMasterDto, EvidenceTypeMasterQueryParameters, int>, IEvidenceTypeMasterService
{
    private readonly IReferenceValidationService _referenceValidator;

    public EvidenceTypeMasterService(
        IRepository<EvidenceTypeMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        EvidenceTypeMasterEntity currentEntity,
        EvidenceTypeMasterEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<EvidenceTypeMasterEntity>(id, cancellationToken);
        }

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        EvidenceTypeMasterEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<EvidenceTypeMasterEntity>(id, cancellationToken);
    }
}
