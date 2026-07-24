using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management.AssetAgeFactorCVMaster;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

/// <summary>
/// CRUD service for [AMS].[AgeFactorCVMaster] — CV age factors scoped to an
/// [AMS].[ConstructionTypeMaster] row for a given age band and assessment year range.
/// </summary>
public class AssetAgeFactorCVService :
    BaseCommonCrudService<
        AssetAgeFactorCVMasterEntity,
        AssetAgeFactorCVMasterDto,
        CreateAssetAgeFactorCVMasterDto,
        UpdateAssetAgeFactorCVMasterDto,
        AssetAgeFactorCVMasterQueryParameters,
        int>,
    IAssetAgeFactorCVService
{
    private readonly IRepository<ConstructionTypeEntity, int> _constructionTypeRepository;
    private readonly IRepository<AssetAssessmentYearRangeMasterCVEntity, int> _yearRangeRepository;
    private readonly IReferenceValidationService _referenceValidator;

    public AssetAgeFactorCVService(
        IRepository<AssetAgeFactorCVMasterEntity, int> repository,
        IRepository<ConstructionTypeEntity, int> constructionTypeRepository,
        IRepository<AssetAssessmentYearRangeMasterCVEntity, int> yearRangeRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _constructionTypeRepository = constructionTypeRepository;
        _yearRangeRepository = yearRangeRepository;
        _referenceValidator = referenceValidator;
    }

    public override async Task<AssetAgeFactorCVMasterDto> CreateAsync(
        CreateAssetAgeFactorCVMasterDto createDto, CancellationToken cancellationToken = default)
    {
        // DTO properties are validated as [Required] by model binding before the service is reached,
        // so !.Value is safe here — see CreateAssetAgeFactorCVMasterDto.
        await EnsureConstructionTypeExistsAsync(createDto.ConstructionTypeId!.Value, OperationType.Create, cancellationToken);
        await EnsureYearRangeExistsAsync(createDto.YearRangeCVId!.Value, OperationType.Create, cancellationToken);
        return await base.CreateAsync(createDto, cancellationToken);
    }

    public override async Task<AssetAgeFactorCVMasterDto?> UpdateAsync(
        int id, UpdateAssetAgeFactorCVMasterDto updateDto, CancellationToken cancellationToken = default)
    {
        await EnsureConstructionTypeExistsAsync(updateDto.ConstructionTypeId!.Value, OperationType.Update, cancellationToken);
        await EnsureYearRangeExistsAsync(updateDto.YearRangeCVId!.Value, OperationType.Update, cancellationToken);
        return await base.UpdateAsync(id, updateDto, cancellationToken);
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetAgeFactorCVMasterEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity.AgeFrom > entity.AgeTo)
            return ValidationResult.Failure(nameof(entity.AgeTo), "AgeFactorCV_AgeRange_Invalid");

        var duplicate = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.ConstructionTypeId == entity.ConstructionTypeId
                        && x.AgeFrom == entity.AgeFrom
                        && x.AgeTo == entity.AgeTo
                        && x.YearRangeCVId == entity.YearRangeCVId, cancellationToken);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.AgeTo), "AgeFactorCV_Combination_Duplicate")
            : ValidationResult.Success();
    }

    // Note: the base service only invokes this hook (not ValidateForCreateAsync) on Update/BulkUpdate,
    // so the range and duplicate-combination checks are duplicated here — matching AssessmentYearRangeCVService's
    // "validate on any update" convention — with the duplicate check excluding the record being updated.
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        AssetAgeFactorCVMasterEntity currentEntity,
        AssetAgeFactorCVMasterEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (updatedEntity.AgeFrom > updatedEntity.AgeTo)
            return ValidationResult.Failure(nameof(updatedEntity.AgeTo), "AgeFactorCV_AgeRange_Invalid");

        var duplicate = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id != id
                        && x.ConstructionTypeId == updatedEntity.ConstructionTypeId
                        && x.AgeFrom == updatedEntity.AgeFrom
                        && x.AgeTo == updatedEntity.AgeTo
                        && x.YearRangeCVId == updatedEntity.YearRangeCVId, cancellationToken);

        if (duplicate)
            return ValidationResult.Failure(nameof(updatedEntity.AgeTo), "AgeFactorCV_Combination_Duplicate");

        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssetAgeFactorCVMasterEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, AssetAgeFactorCVMasterEntity entity, CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<AssetAgeFactorCVMasterEntity>(id, cancellationToken);
    }

    private async Task EnsureConstructionTypeExistsAsync(int constructionTypeId, OperationType operationType, CancellationToken cancellationToken)
    {
        var exists = await _constructionTypeRepository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id == constructionTypeId && x.IsActive, cancellationToken);

        if (!exists)
            throw new ValidationException(nameof(CreateAssetAgeFactorCVMasterDto.ConstructionTypeId), $"Construction type with ID {constructionTypeId} not found.", operationType);
    }

    private async Task EnsureYearRangeExistsAsync(int yearRangeCVId, OperationType operationType, CancellationToken cancellationToken)
    {
        var exists = await _yearRangeRepository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id == yearRangeCVId && x.IsActive, cancellationToken);

        if (!exists)
            throw new ValidationException(nameof(CreateAssetAgeFactorCVMasterDto.YearRangeCVId), $"Assessment year range with ID {yearRangeCVId} not found.", operationType);
    }
}
