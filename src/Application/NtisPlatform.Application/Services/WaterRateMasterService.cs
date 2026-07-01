using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class WaterRateMasterService
    : BaseCommonCrudService<WaterRateMasterEntity, WaterRateMasterDto, CreateWaterRateMasterDto, UpdateWaterRateMasterDto, WaterRateMasterQueryParameters, int>,
      IWaterRateMasterService
{
    private readonly IReferenceValidationService _referenceValidator;

    public WaterRateMasterService(
        IRepository<WaterRateMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override IQueryable<WaterRateMasterEntity> ApplyIncludes(IQueryable<WaterRateMasterEntity> query)
    {
        return query
            .Include(x => x.WaterConnectionType)
            .Include(x => x.WaterConnectionSize)
            .Include(x => x.FinanceYear);
    }

    public override async Task<PagedResult<WaterRateMasterDto>> GetAllAsync(
        WaterRateMasterQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        // Extract the search term from any of the potential search/filter parameters sent by the frontend search input
        var term = queryParameters.SearchTerm;
        if (string.IsNullOrWhiteSpace(term))
        {
            if (!string.IsNullOrWhiteSpace(queryParameters.ConnectionTypeName))
            {
                term = queryParameters.ConnectionTypeName;
                queryParameters.ConnectionTypeName = null;
            }
            else if (!string.IsNullOrWhiteSpace(queryParameters.ConnectionSizeUnit))
            {
                term = queryParameters.ConnectionSizeUnit;
                queryParameters.ConnectionSizeUnit = null;
            }
            else if (!string.IsNullOrWhiteSpace(queryParameters.ConnectionSize))
            {
                term = queryParameters.ConnectionSize;
                queryParameters.ConnectionSize = null;
            }
            else if (!string.IsNullOrWhiteSpace(queryParameters.YearCode))
            {
                term = queryParameters.YearCode;
                queryParameters.YearCode = null;
            }
        }
        else
        {
            // If SearchTerm was explicitly provided, clear search-only helper string parameters
            queryParameters.ConnectionTypeName = null;
            queryParameters.ConnectionSizeUnit = null;
            queryParameters.ConnectionSize = null;
            queryParameters.YearCode = null;
            // YearlyRate is left intact so standard ApplyFilters can filter by it
        }

        IQueryable<WaterRateMasterEntity> query = _repository.GetQueryable()
            .Include(x => x.WaterConnectionType)
            .Include(x => x.WaterConnectionSize)
            .Include(x => x.FinanceYear);

        // Apply standard filters (WaterConnectionTypeId, WaterConnectionSizeId, FinanceYearId, IsActive, YearlyRate)
        query = query.ApplyFilters(queryParameters);

        // Apply sorting (defaults to Id when SortBy is not provided)
        query = query.ApplySort(queryParameters);

        // Apply custom search across navigation property strings, sizes, and rates
        if (!string.IsNullOrWhiteSpace(term))
        {
            var searchPattern = term.Trim().ToLowerInvariant();
            decimal? decimalSearchVal = null;
            if (decimal.TryParse(term, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedDecimal))
            {
                decimalSearchVal = parsedDecimal;
            }

            query = query.Where(x =>
                (x.WaterConnectionType.ConnectionTypeName != null && x.WaterConnectionType.ConnectionTypeName.ToLower().Contains(searchPattern)) ||
                (x.WaterConnectionSize.ConnectionSizeUnit != null && x.WaterConnectionSize.ConnectionSizeUnit.ToLower().Contains(searchPattern)) ||
                (x.FinanceYear.YearCode != null && x.FinanceYear.YearCode.ToLower().Contains(searchPattern)) ||
                x.WaterConnectionSize.ConnectionSize.ToString().Contains(searchPattern) ||
                (x.WaterConnectionSize.ConnectionSize.ToString() + " " + x.WaterConnectionSize.ConnectionSizeUnit).ToLower().Contains(searchPattern) ||
                (x.WaterConnectionSize.ConnectionSize.ToString() + x.WaterConnectionSize.ConnectionSizeUnit).ToLower().Contains(searchPattern) ||
                (decimalSearchVal.HasValue && x.YearlyRate == decimalSearchVal.Value)
            );
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedQuery = query
            .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize);

        var entities = await pagedQuery.ToListAsync(cancellationToken);
        var items = _mapper.Map<List<WaterRateMasterDto>>(entities);

        var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

        return new PagedResult<WaterRateMasterDto>(items, totalCount, pageNumber, pageSize);
    }

    public override async Task<WaterRateMasterDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable()
            .Include(x => x.WaterConnectionType)
            .Include(x => x.WaterConnectionSize)
            .Include(x => x.FinanceYear)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
            return default;

        var dto = _mapper.Map<WaterRateMasterDto>(entity);

        return dto;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        WaterRateMasterEntity currentEntity,
        WaterRateMasterEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<WaterRateMasterEntity>(id, cancellationToken);
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        WaterRateMasterEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<WaterRateMasterEntity>(id, cancellationToken);
    }
}
