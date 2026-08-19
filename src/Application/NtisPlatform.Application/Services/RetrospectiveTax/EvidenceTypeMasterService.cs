using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.EvidenceTypeMaster;
using NtisPlatform.Application.DTOs.Range;
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

    public async Task<RangeResult<EvidenceTypeMasterDto>> CreateFromRangeAsync(RangeCreateRequest<CreateEvidenceTypeMasterDto> request, CancellationToken cancellationToken = default)
    {
        Func<CreateEvidenceTypeMasterDto, string, int, CreateEvidenceTypeMasterDto> transformer = (template, rangeValue, sequenceNo) =>
            new CreateEvidenceTypeMasterDto
            {
                EvidenceCode = rangeValue,
                EvidenceName = string.IsNullOrEmpty(template.EvidenceName) ? rangeValue : template.EvidenceName.Replace("{value}", rangeValue),
                IsCertificate = template.IsCertificate,
                DisplayOrder = sequenceNo,
                IsActive = template.IsActive,
                CreatedBy = template.CreatedBy
            };

        return await base.CreateFromRangeAsync(request, transformer, cancellationToken);
    }
}
