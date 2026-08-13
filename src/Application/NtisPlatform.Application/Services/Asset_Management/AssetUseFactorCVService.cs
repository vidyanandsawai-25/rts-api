using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management.AssetUseFactorCVMaster;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

/// <summary>
/// CRUD service for [AMS].[UseFactorCVMaster] — CV use factors scoped to a
/// [AMS].[TypeOfUseMaster] / [AMS].[SubTypeOfUseMaster] combination for a given assessment year range.
/// </summary>
public class AssetUseFactorCVService :
    BaseCommonCrudService<
        AssetUseFactorCVMasterEntity,
        AssetUseFactorCVMasterDto,
        CreateAssetUseFactorCVMasterDto,
        UpdateAssetUseFactorCVMasterDto,
        AssetUseFactorCVMasterQueryParameters,
        int>,
    IAssetUseFactorCVService
{
    private readonly IRepository<AssetTypeOfUseMasterEntity, int> _typeOfUseRepository;
    private readonly IRepository<AssetSubTypeOfUseEntity, int> _subTypeOfUseRepository;
    private readonly IRepository<AssetAssessmentYearRangeMasterCVEntity, int> _yearRangeRepository;
    private readonly IReferenceValidationService _referenceValidator;

    public AssetUseFactorCVService(
        IRepository<AssetUseFactorCVMasterEntity, int> repository,
        IRepository<AssetTypeOfUseMasterEntity, int> typeOfUseRepository,
        IRepository<AssetSubTypeOfUseEntity, int> subTypeOfUseRepository,
        IRepository<AssetAssessmentYearRangeMasterCVEntity, int> yearRangeRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _typeOfUseRepository = typeOfUseRepository;
        _subTypeOfUseRepository = subTypeOfUseRepository;
        _yearRangeRepository = yearRangeRepository;
        _referenceValidator = referenceValidator;
    }

    /// <summary>
    /// Overridden solely to enrich the response with TypeOfUseDescription and
    /// SubTypeOfUseDescription via SQL JOINs against TypeOfUseMaster/SubTypeOfUseMaster -
    /// AssetUseFactorCVMasterEntity stays a pure POCO with only the FK ids (no navigation
    /// properties), so ProjectTo can't reach either description on its own. Preserves the base
    /// pipeline order: ApplyFilters -> ApplySearch -> ApplySort -> Count -> Skip/Take -> project.
    /// </summary>
    public override async Task<PagedResult<AssetUseFactorCVMasterDto>> GetAllAsync(
        AssetUseFactorCVMasterQueryParameters queryParameters, CancellationToken cancellationToken = default)
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
            from u in pagedQuery
            join tou in _typeOfUseRepository.GetQueryable() on u.TypeOfUseId equals tou.Id into touJoin
            from tou in touJoin.DefaultIfEmpty()
            join stou in _subTypeOfUseRepository.GetQueryable() on u.SubTypeOfUseId equals stou.Id into stouJoin
            from stou in stouJoin.DefaultIfEmpty()
            select new AssetUseFactorCVMasterDto
            {
                Id = u.Id,
                TypeOfUseId = u.TypeOfUseId,
                TypeOfUseDescription = tou != null ? (tou.Description ?? string.Empty) : string.Empty,
                SubTypeOfUseId = u.SubTypeOfUseId,
                SubTypeOfUseDescription = stou != null ? stou.Description : string.Empty,
                Factor = u.Factor,
                YearRangeCVId = u.YearRangeCVId,
                IsActive = u.IsActive,
                CreatedDate = u.CreatedDate,
                UpdatedDate = u.UpdatedDate
            }
        ).ToListAsync(cancellationToken);

        var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

        return new PagedResult<AssetUseFactorCVMasterDto>(items, totalCount, pageNumber, pageSize);
    }

    public override async Task<AssetUseFactorCVMasterDto> CreateAsync(
        CreateAssetUseFactorCVMasterDto createDto, CancellationToken cancellationToken = default)
    {
        // DTO properties are validated as [Required] by model binding before the service is reached,
        // so !.Value is safe here — see CreateAssetUseFactorCVMasterDto.
        await EnsureTypeOfUseExistsAsync(createDto.TypeOfUseId!.Value, OperationType.Create, cancellationToken);
        await EnsureSubTypeOfUseExistsAsync(createDto.TypeOfUseId!.Value, createDto.SubTypeOfUseId!.Value, OperationType.Create, cancellationToken);
        await EnsureYearRangeExistsAsync(createDto.YearRangeCVId!.Value, OperationType.Create, cancellationToken);
        return await base.CreateAsync(createDto, cancellationToken);
    }

    public override async Task<AssetUseFactorCVMasterDto?> UpdateAsync(
        int id, UpdateAssetUseFactorCVMasterDto updateDto, CancellationToken cancellationToken = default)
    {
        await EnsureTypeOfUseExistsAsync(updateDto.TypeOfUseId!.Value, OperationType.Update, cancellationToken);
        await EnsureSubTypeOfUseExistsAsync(updateDto.TypeOfUseId!.Value, updateDto.SubTypeOfUseId!.Value, OperationType.Update, cancellationToken);
        await EnsureYearRangeExistsAsync(updateDto.YearRangeCVId!.Value, OperationType.Update, cancellationToken);
        return await base.UpdateAsync(id, updateDto, cancellationToken);
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetUseFactorCVMasterEntity entity, CancellationToken cancellationToken = default)
    {
        var duplicate = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.TypeOfUseId == entity.TypeOfUseId
                        && x.SubTypeOfUseId == entity.SubTypeOfUseId
                        && x.YearRangeCVId == entity.YearRangeCVId, cancellationToken);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.YearRangeCVId), "UseFactorCV_Combination_Duplicate")
            : ValidationResult.Success();
    }

    // Note: the base service only invokes this hook (not ValidateForCreateAsync) on Update/BulkUpdate,
    // so the duplicate-combination check is duplicated here — matching AssetNatureFactorCVService's
    // "validate on any update" convention — with the duplicate check excluding the record being updated.
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        AssetUseFactorCVMasterEntity currentEntity,
        AssetUseFactorCVMasterEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        var duplicate = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id != id
                        && x.TypeOfUseId == updatedEntity.TypeOfUseId
                        && x.SubTypeOfUseId == updatedEntity.SubTypeOfUseId
                        && x.YearRangeCVId == updatedEntity.YearRangeCVId, cancellationToken);

        if (duplicate)
            return ValidationResult.Failure(nameof(updatedEntity.YearRangeCVId), "UseFactorCV_Combination_Duplicate");

        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssetUseFactorCVMasterEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, AssetUseFactorCVMasterEntity entity, CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<AssetUseFactorCVMasterEntity>(id, cancellationToken);
    }

    private async Task EnsureTypeOfUseExistsAsync(int typeOfUseId, OperationType operationType, CancellationToken cancellationToken)
    {
        var exists = await _typeOfUseRepository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id == typeOfUseId && x.IsActive, cancellationToken);

        if (!exists)
            throw new ValidationException(
                nameof(CreateAssetUseFactorCVMasterDto.TypeOfUseId),
                $"Type of use with ID {typeOfUseId} not found.",
                operationType);
    }

    // Validates both that the sub-type-of-use exists AND that it belongs to the given TypeOfUseId —
    // the two FKs are independent columns (no composite FK in the DB), so checking existence alone
    // would let a caller pair a valid SubTypeOfUseId with an unrelated TypeOfUseId.
    private async Task EnsureSubTypeOfUseExistsAsync(int typeOfUseId, int subTypeOfUseId, OperationType operationType, CancellationToken cancellationToken)
    {
        var belongsToTypeOfUse = await _subTypeOfUseRepository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id == subTypeOfUseId && x.IsActive && x.TypeOfUseId == typeOfUseId, cancellationToken);

        if (!belongsToTypeOfUse)
            throw new ValidationException(
                nameof(CreateAssetUseFactorCVMasterDto.SubTypeOfUseId),
                $"Sub type of use with ID {subTypeOfUseId} not found for type of use {typeOfUseId}.",
                operationType);
    }

    private async Task EnsureYearRangeExistsAsync(int yearRangeCVId, OperationType operationType, CancellationToken cancellationToken)
    {
        var exists = await _yearRangeRepository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id == yearRangeCVId && x.IsActive, cancellationToken);

        if (!exists)
            throw new ValidationException(
                nameof(CreateAssetUseFactorCVMasterDto.YearRangeCVId),
                $"Assessment year range with ID {yearRangeCVId} not found.",
                operationType);
    }
}
