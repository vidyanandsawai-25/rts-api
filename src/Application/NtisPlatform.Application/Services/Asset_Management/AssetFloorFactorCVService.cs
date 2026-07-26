using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

public class AssetFloorFactorCVService :
    BaseCommonCrudService<AssetFloorFactorCVEntity, AssetFloorFactorCVDto, CreateAssetFloorFactorCVDto, UpdateAssetFloorFactorCVDto, AssetFloorFactorCVQueryParameters, int>,
    IAssetFloorFactorCVService
{
    private readonly IRepository<FloorEntity, int> _floorRepository;
    private readonly IRepository<AssetAssessmentYearRangeMasterCVEntity, int> _yearRangeRepository;
    private readonly IReferenceValidationService _referenceValidator;

    public AssetFloorFactorCVService(
        IRepository<AssetFloorFactorCVEntity, int> repository,
        IRepository<FloorEntity, int> floorRepository,
        IRepository<AssetAssessmentYearRangeMasterCVEntity, int> yearRangeRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _floorRepository = floorRepository;
        _yearRangeRepository = yearRangeRepository;
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetFloorFactorCVEntity entity, CancellationToken ct = default)
    {
        var floorExists = await _floorRepository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(f => f.Id == entity.FloorId && f.IsActive, ct);

        if (!floorExists)
            return ValidationResult.Failure(nameof(entity.FloorId), "AssetFloorFactorCV_FloorId_Invalid");

        var yearRangeExists = await _yearRangeRepository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(y => y.Id == entity.YearRangeCVId && !y.MarkedForDeletion, ct);

        if (!yearRangeExists)
            return ValidationResult.Failure(nameof(entity.YearRangeCVId), "AssetFloorFactorCV_YearRangeCVId_Invalid");

        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.FloorId == entity.FloorId && x.YearRangeCVId == entity.YearRangeCVId, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.FloorId), "AssetFloorFactorCV_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id, AssetFloorFactorCVEntity currentEntity, AssetFloorFactorCVEntity updatedEntity, CancellationToken ct = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            var refResult = await _referenceValidator.ValidateReferencesAsync<AssetFloorFactorCVEntity>(id, ct);
            if (!refResult.IsValid) return refResult;
        }

        var floorExists = await _floorRepository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(f => f.Id == updatedEntity.FloorId && f.IsActive, ct);

        if (!floorExists)
            return ValidationResult.Failure(nameof(updatedEntity.FloorId), "AssetFloorFactorCV_FloorId_Invalid");

        var yearRangeExists = await _yearRangeRepository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(y => y.Id == updatedEntity.YearRangeCVId && !y.MarkedForDeletion, ct);

        if (!yearRangeExists)
            return ValidationResult.Failure(nameof(updatedEntity.YearRangeCVId), "AssetFloorFactorCV_YearRangeCVId_Invalid");

        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.Id != id && x.FloorId == updatedEntity.FloorId && x.YearRangeCVId == updatedEntity.YearRangeCVId, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(updatedEntity.FloorId), "AssetFloorFactorCV_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, AssetFloorFactorCVEntity entity, CancellationToken ct = default)
        => await _referenceValidator.ValidateReferencesAsync<AssetFloorFactorCVEntity>(id, ct);
}
