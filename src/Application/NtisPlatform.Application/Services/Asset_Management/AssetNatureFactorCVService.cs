using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management.AssetNatureFactorCVMaster;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

/// <summary>
/// CRUD service for [AMS].[NatureFactorCVMaster] — CV nature factors scoped to an
/// [AMS].[ConstructionTypeMaster] row for a given assessment year range.
/// </summary>
public class AssetNatureFactorCVService :
    BaseCommonCrudService<
        AssetNatureFactorCVMasterEntity,
        AssetNatureFactorCVMasterDto,
        CreateAssetNatureFactorCVMasterDto,
        UpdateAssetNatureFactorCVMasterDto,
        AssetNatureFactorCVMasterQueryParameters,
        int>,
    IAssetNatureFactorCVService
{
    private readonly IRepository<ConstructionTypeEntity, int> _constructionTypeRepository;
    private readonly IRepository<AssetAssessmentYearRangeMasterCVEntity, int> _yearRangeRepository;
    private readonly IReferenceValidationService _referenceValidator;

    public AssetNatureFactorCVService(
        IRepository<AssetNatureFactorCVMasterEntity, int> repository,
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

    /// <summary>
    /// Overridden solely to enrich the response with ConstructionTypeDescription via a SQL JOIN
    /// against ConstructionTypeMaster - AssetNatureFactorCVMasterEntity stays a pure POCO with only
    /// ConstructionTypeId (no navigation property), so ProjectTo can't reach the description on its
    /// own. Preserves the base pipeline order: ApplyFilters -> ApplySearch -> ApplySort -> Count ->
    /// Skip/Take -> project.
    /// </summary>
    public override async Task<PagedResult<AssetNatureFactorCVMasterDto>> GetAllAsync(
        AssetNatureFactorCVMasterQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable()
            .ApplyFilters(queryParameters)
            .ApplySearch(queryParameters)
            .ApplySort(queryParameters);

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedQuery = query
            .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize);

        var items = await (
            from n in pagedQuery
            join ct in _constructionTypeRepository.GetQueryable() on n.ConstructionTypeId equals ct.Id into ctJoin
            from ct in ctJoin.DefaultIfEmpty()
            select new AssetNatureFactorCVMasterDto
            {
                Id = n.Id,
                ConstructionTypeId = n.ConstructionTypeId,
                ConstructionTypeDescription = ct != null ? (ct.Description ?? string.Empty) : string.Empty,
                Factor = n.Factor,
                YearRangeCVId = n.YearRangeCVId,
                IsActive = n.IsActive,
                CreatedDate = n.CreatedDate,
                UpdatedDate = n.UpdatedDate
            }
        ).ToListAsync(cancellationToken);

        var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

        return new PagedResult<AssetNatureFactorCVMasterDto>(items, totalCount, pageNumber, pageSize);
    }

    public override async Task<AssetNatureFactorCVMasterDto> CreateAsync(
        CreateAssetNatureFactorCVMasterDto createDto, CancellationToken cancellationToken = default)
    {
        // DTO properties are validated as [Required] by model binding before the service is reached,
        // so !.Value is safe here — see CreateAssetNatureFactorCVMasterDto.
        await EnsureConstructionTypeExistsAsync(createDto.ConstructionTypeId!.Value, OperationType.Create, cancellationToken);
        await EnsureYearRangeExistsAsync(createDto.YearRangeCVId!.Value, OperationType.Create, cancellationToken);
        return await base.CreateAsync(createDto, cancellationToken);
    }

    public override async Task<AssetNatureFactorCVMasterDto?> UpdateAsync(
        int id, UpdateAssetNatureFactorCVMasterDto updateDto, CancellationToken cancellationToken = default)
    {
        await EnsureConstructionTypeExistsAsync(updateDto.ConstructionTypeId!.Value, OperationType.Update, cancellationToken);
        await EnsureYearRangeExistsAsync(updateDto.YearRangeCVId!.Value, OperationType.Update, cancellationToken);
        return await base.UpdateAsync(id, updateDto, cancellationToken);
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetNatureFactorCVMasterEntity entity, CancellationToken cancellationToken = default)
    {
        var duplicate = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.ConstructionTypeId == entity.ConstructionTypeId
                        && x.YearRangeCVId == entity.YearRangeCVId, cancellationToken);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.YearRangeCVId), "NatureFactorCV_Combination_Duplicate")
            : ValidationResult.Success();
    }

    // Note: the base service only invokes this hook (not ValidateForCreateAsync) on Update/BulkUpdate,
    // so the duplicate-combination check is duplicated here — matching AssetAgeFactorCVService's
    // "validate on any update" convention — with the duplicate check excluding the record being updated.
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        AssetNatureFactorCVMasterEntity currentEntity,
        AssetNatureFactorCVMasterEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        var duplicate = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id != id
                        && x.ConstructionTypeId == updatedEntity.ConstructionTypeId
                        && x.YearRangeCVId == updatedEntity.YearRangeCVId, cancellationToken);

        if (duplicate)
            return ValidationResult.Failure(nameof(updatedEntity.YearRangeCVId), "NatureFactorCV_Combination_Duplicate");

        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssetNatureFactorCVMasterEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, AssetNatureFactorCVMasterEntity entity, CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<AssetNatureFactorCVMasterEntity>(id, cancellationToken);
    }

    private async Task EnsureConstructionTypeExistsAsync(int constructionTypeId, OperationType operationType, CancellationToken cancellationToken)
    {
        var exists = await _constructionTypeRepository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id == constructionTypeId && x.IsActive, cancellationToken);

        if (!exists)
            throw new ValidationException(
                nameof(CreateAssetNatureFactorCVMasterDto.ConstructionTypeId),
                $"Construction type with ID {constructionTypeId} not found.",
                operationType);
    }

    private async Task EnsureYearRangeExistsAsync(int yearRangeCVId, OperationType operationType, CancellationToken cancellationToken)
    {
        var exists = await _yearRangeRepository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id == yearRangeCVId && x.IsActive, cancellationToken);

        if (!exists)
            throw new ValidationException(
                nameof(CreateAssetNatureFactorCVMasterDto.YearRangeCVId),
                $"Assessment year range with ID {yearRangeCVId} not found.",
                operationType);
    }
}
