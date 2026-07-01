using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class WaterConnectionSizeService
    : BaseCommonCrudService<WaterConnectionSizeEntity, WaterConnectionSizeDto, CreateWaterConnectionSizeDto, UpdateWaterConnectionSizeDto, WaterConnectionSizeQueryParameters, int>,
      IWaterConnectionSizeService
{
    private readonly IReferenceValidationService _referenceValidator;

    public WaterConnectionSizeService(
        IRepository<WaterConnectionSizeEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    public override async Task<PagedResult<WaterConnectionSizeDto>> GetAllAsync(
        WaterConnectionSizeQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        // Extract the search term from any of the potential search/filter parameters sent by the frontend
        var term = queryParameters.SearchTerm;
        if (string.IsNullOrWhiteSpace(term))
        {
            if (!string.IsNullOrWhiteSpace(queryParameters.ConnectionSizeUnit))
            {
                term = queryParameters.ConnectionSizeUnit;
                queryParameters.ConnectionSizeUnit = null;
            }
            else if (!string.IsNullOrWhiteSpace(queryParameters.ConnectionSize))
            {
                term = queryParameters.ConnectionSize;
                queryParameters.ConnectionSize = null;
            }
            else if (!string.IsNullOrWhiteSpace(queryParameters.DisplayLabel))
            {
                term = queryParameters.DisplayLabel;
                queryParameters.DisplayLabel = null;
            }
        }
        else
        {
            // If SearchTerm was explicitly provided, only clear parameters that don't belong to entity filters
            queryParameters.ConnectionSize = null;
            queryParameters.DisplayLabel = null;
            // ConnectionSizeUnit is left intact so standard ApplyFilters can filter by it
        }

        IQueryable<WaterConnectionSizeEntity> query = _repository.GetQueryable();

        // Apply standard filters
        query = query.ApplyFilters(queryParameters);

        // Apply sorting (defaults to Id when SortBy is not provided)
        query = query.ApplySort(queryParameters);

        // Apply custom search across ConnectionSizeUnit, ConnectionSize, and the combined display format
        if (!string.IsNullOrWhiteSpace(term))
        {
            var searchPattern = term.Trim().ToLowerInvariant();
            query = query.Where(x =>
                (x.ConnectionSizeUnit != null && x.ConnectionSizeUnit.ToLower().Contains(searchPattern)) ||
                x.ConnectionSize.ToString().Contains(searchPattern) ||
                (x.ConnectionSize.ToString() + " " + x.ConnectionSizeUnit).ToLower().Contains(searchPattern) ||
                (x.ConnectionSize.ToString() + x.ConnectionSizeUnit).ToLower().Contains(searchPattern)
            );
        }



        var totalCount = await query.CountAsync(cancellationToken);

        var pagedQuery = query
            .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize);

        var queryWithIncludes = ApplyIncludes(pagedQuery);

        var entities = await queryWithIncludes.ToListAsync(cancellationToken);
        var items = _mapper.Map<List<WaterConnectionSizeDto>>(entities);

        var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

        return new PagedResult<WaterConnectionSizeDto>(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Override to force in-memory mapping instead of ProjectTo.
    /// The DTO uses ToString("G29") for DisplayLabel which EF Core cannot translate to SQL.
    /// </summary>
    protected override IQueryable<WaterConnectionSizeEntity> ApplyIncludes(IQueryable<WaterConnectionSizeEntity> query)
    {
        return query.AsNoTracking();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        WaterConnectionSizeEntity currentEntity,
        WaterConnectionSizeEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<WaterConnectionSizeEntity>(id, cancellationToken);
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        WaterConnectionSizeEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<WaterConnectionSizeEntity>(id, cancellationToken);
    }
}
