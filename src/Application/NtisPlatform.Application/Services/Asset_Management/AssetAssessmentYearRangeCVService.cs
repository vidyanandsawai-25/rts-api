using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management.AssetAssessmentYearRangeMasterCV;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

/// <summary>CRUD service for [AMS].[AssessmentYearRangeMaster].</summary>
public class AssetAssessmentYearRangeCVService :
    BaseCommonCrudService<
        AssetAssessmentYearRangeMasterCVEntity,
        AssetAssessmentYearRangeMasterCVDto,
        CreateAssetAssessmentYearRangeMasterCVDto,
        UpdateAssetAssessmentYearRangeMasterCVDto,
        AssetAssessmentYearRangeMasterCVQueryParameters,
        int>,
    IAssetAssessmentYearRangeCVService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetAssessmentYearRangeCVService(
        IRepository<AssetAssessmentYearRangeMasterCVEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetAssessmentYearRangeMasterCVEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity.FromYear > entity.ToYear)
            return ValidationResult.Failure(nameof(entity.ToYear), "AssessmentYearRangeCV_ToYear_BeforeFromYear");

        var duplicate = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.FromYear == entity.FromYear && x.ToYear == entity.ToYear, cancellationToken);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.ToYear), "AssessmentYearRangeCV_Range_Duplicate")
            : ValidationResult.Success();
    }

    // Note: the base service only invokes this hook (not ValidateForCreateAsync) on Update/BulkUpdate,
    // so the range and duplicate-range checks are duplicated here — matching AssetAgeFactorCVService's
    // "validate on any update" convention — with the duplicate check excluding the record being updated.
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        AssetAssessmentYearRangeMasterCVEntity currentEntity,
        AssetAssessmentYearRangeMasterCVEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (updatedEntity.FromYear > updatedEntity.ToYear)
            return ValidationResult.Failure(nameof(updatedEntity.ToYear), "AssessmentYearRangeCV_ToYear_BeforeFromYear");

        var duplicate = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id != id && x.FromYear == updatedEntity.FromYear && x.ToYear == updatedEntity.ToYear, cancellationToken);

        if (duplicate)
            return ValidationResult.Failure(nameof(updatedEntity.ToYear), "AssessmentYearRangeCV_Range_Duplicate");

        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssetAssessmentYearRangeMasterCVEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, AssetAssessmentYearRangeMasterCVEntity entity, CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<AssetAssessmentYearRangeMasterCVEntity>(id, cancellationToken);
    }
}
