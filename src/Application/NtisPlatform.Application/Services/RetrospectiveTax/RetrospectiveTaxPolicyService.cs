using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxPolicy;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using OperationType = NtisPlatform.Application.Enums.OperationType;

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

    public async Task<RangeResult<RetrospectiveTaxPolicyDto>> CreateFromRangeAsync(RangeCreateRequest<CreateRetrospectiveTaxPolicyDto> request, CancellationToken cancellationToken = default)
    {
        Func<CreateRetrospectiveTaxPolicyDto, string, int, CreateRetrospectiveTaxPolicyDto> transformer = (template, rangeValue, sequenceNo) =>
            new CreateRetrospectiveTaxPolicyDto
            {
                TaxPolicyCode = rangeValue,
                TaxPolicyName = string.IsNullOrEmpty(template.TaxPolicyName) ? rangeValue : template.TaxPolicyName.Replace("{value}", rangeValue),
                RateMode = template.RateMode,
                PercentageMode = template.PercentageMode,
                FixedPercentage = template.FixedPercentage,
                FinancialYearStartMonth = template.FinancialYearStartMonth,
                FinancialYearStartDay = template.FinancialYearStartDay,
                EffectiveFrom = template.EffectiveFrom,
                EffectiveTo = template.EffectiveTo,
                IsActive = template.IsActive,
                CreatedBy = template.CreatedBy
            };

        return await base.CreateFromRangeAsync(request, transformer, cancellationToken);
    }

    public async Task<RetrospectiveTaxPolicyDto> SaveAsync(SaveRetrospectiveTaxPolicyDto request, CancellationToken cancellationToken = default)
    {
        if (request.PercentageMode == "FIXED_PERCENTAGE" && request.FixedPercentage == null)
        {
            var validationResult = ValidationResult.Failure(
                nameof(request.FixedPercentage), "FixedPercentage is required when PercentageMode is FIXED_PERCENTAGE.");
            throw new ValidationException(
                "FixedPercentage is required when PercentageMode is FIXED_PERCENTAGE.",
                validationResult.ToDictionary(), OperationType.Update);
        }

        var existing = await _repository.GetQueryable().FirstOrDefaultAsync(p => p.IsActive, cancellationToken);

        RetrospectiveTaxPolicyEntity policy;
        if (existing is not null)
        {
            policy = existing;
            if (!string.IsNullOrWhiteSpace(request.TaxPolicyCode))
                policy.TaxPolicyCode = request.TaxPolicyCode;
            if (!string.IsNullOrWhiteSpace(request.TaxPolicyName))
                policy.TaxPolicyName = request.TaxPolicyName;
            policy.RateMode = request.RateMode;
            policy.PercentageMode = request.PercentageMode;
            policy.FixedPercentage = request.FixedPercentage;
            policy.FinancialYearStartMonth = request.FinancialYearStartMonth;
            policy.FinancialYearStartDay = request.FinancialYearStartDay;
            policy.EffectiveFrom = request.EffectiveFrom;
            policy.EffectiveTo = request.EffectiveTo;
            policy.UpdatedBy = request.UpdatedBy;
            policy.UpdatedDate = DateTime.Now;
            await _repository.UpdateAsync(policy, cancellationToken);
        }
        else
        {
            policy = new RetrospectiveTaxPolicyEntity
            {
                TaxPolicyCode = string.IsNullOrWhiteSpace(request.TaxPolicyCode) ? "DEFAULT" : request.TaxPolicyCode,
                TaxPolicyName = string.IsNullOrWhiteSpace(request.TaxPolicyName) ? "Default Taxation Policy" : request.TaxPolicyName,
                RateMode = request.RateMode,
                PercentageMode = request.PercentageMode,
                FixedPercentage = request.FixedPercentage,
                FinancialYearStartMonth = request.FinancialYearStartMonth,
                FinancialYearStartDay = request.FinancialYearStartDay,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = request.EffectiveTo,
                IsActive = true,
                CreatedBy = request.UpdatedBy,
                CreatedDate = DateTime.Now
            };
            await _repository.AddAsync(policy, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<RetrospectiveTaxPolicyDto>(policy);
    }
}
