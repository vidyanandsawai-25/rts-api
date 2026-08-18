using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxPolicy;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.RetrospectiveTax;

public class RetrospectiveTaxPolicyService : BaseCommonCrudService<RetrospectiveTaxPolicyEntity, RetrospectiveTaxPolicyDto, CreateRetrospectiveTaxPolicyDto, UpdateRetrospectiveTaxPolicyDto, RetrospectiveTaxPolicyQueryParameters, int>, IRetrospectiveTaxPolicyService
{
    private readonly IReferenceValidationService _referenceValidator;

    public RetrospectiveTaxPolicyService(
        IRepository<RetrospectiveTaxPolicyEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    /// <summary>
    /// FixedPercentage is mandatory only when PercentageMode = FIXED_PERCENTAGE (mirrors the
    /// CK_RetrospectiveTaxPolicy_FixedPercentage DB constraint, so the client gets a friendly
    /// 400 instead of a raw SQL constraint error).
    /// </summary>
    protected override Task<ValidationResult> ValidateForCreateAsync(
        RetrospectiveTaxPolicyEntity entity,
        CancellationToken cancellationToken = default)
    {
        if (entity.PercentageMode == "FIXED_PERCENTAGE" && entity.FixedPercentage == null)
        {
            return Task.FromResult(ValidationResult.Failure(
                nameof(entity.FixedPercentage), "FixedPercentage is required when PercentageMode is FIXED_PERCENTAGE."));
        }

        return Task.FromResult(ValidationResult.Success());
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        RetrospectiveTaxPolicyEntity currentEntity,
        RetrospectiveTaxPolicyEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (updatedEntity.PercentageMode == "FIXED_PERCENTAGE" && updatedEntity.FixedPercentage == null)
        {
            return ValidationResult.Failure(
                nameof(updatedEntity.FixedPercentage), "FixedPercentage is required when PercentageMode is FIXED_PERCENTAGE.");
        }

        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<RetrospectiveTaxPolicyEntity>(id, cancellationToken);
        }

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        RetrospectiveTaxPolicyEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<RetrospectiveTaxPolicyEntity>(id, cancellationToken);
    }
}
