using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master.FloorFactorCVMaster;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class FloorFactorCVMasterService : BaseCommonCrudService<FloorFactorCVMasterEntity, FloorFactorCVMasterDto, CreateFloorFactorCVMasterDto, UpdateFloorFactorCVMasterDto, FloorFactorCVMasterQueryParameters, int>, IFloorFactorCVMasterService
{
    private readonly IRepository<FloorEntity, int> _floorRepository;
    private readonly IRepository<AssessmentYearRangeCVEntity, int> _yearRangeCVRepository;

    public FloorFactorCVMasterService(
        IRepository<FloorFactorCVMasterEntity, int> repository,
        IRepository<FloorEntity, int> floorRepository,
        IRepository<AssessmentYearRangeCVEntity, int> yearRangeCVRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _floorRepository = floorRepository;
        _yearRangeCVRepository = yearRangeCVRepository;
    }

    /// <summary>
    /// Returns a paged list of FloorFactorCVMasterDto, including placeholder rows for missing (Floor, YearRange) combinations.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting, and paging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged result of FloorFactorCVMasterDto.</returns>
    public override async Task<PagedResult<FloorFactorCVMasterDto>> GetAllAsync(
     FloorFactorCVMasterQueryParameters queryParameters,
     CancellationToken cancellationToken = default)
    {
        // Filter FloorMaster records to include only IsActive = 1
        var floorQuery = _floorRepository.GetQueryable().Where(f => f.IsActive);
        if (queryParameters.FloorId.HasValue)
            floorQuery = floorQuery.Where(f => f.Id == queryParameters.FloorId.Value);

        // Filter AssessmentYearRangeCV records to include only IsActive = 1
        var yearRangeQuery = _yearRangeCVRepository.GetQueryable().Where(yr => yr.IsActive);
        if (queryParameters.YearRangeCVId.HasValue)
            yearRangeQuery = yearRangeQuery.Where(yr => yr.Id == queryParameters.YearRangeCVId.Value);

        var floorFactorQuery = _repository.GetQueryable();
        if (queryParameters.IsActive.HasValue)
            floorFactorQuery = floorFactorQuery.Where(ff => ff.IsActive == queryParameters.IsActive.Value);

        var yearRangesWithData = floorFactorQuery.Select(ff => ff.YearRangeCVId).Distinct();

        var yearRangesWithNoData = GetYearRangesWithNoData(yearRangeQuery, yearRangesWithData, floorQuery);
        var floorsWithDataPerYear = GetFloorsWithDataPerYear(yearRangeQuery, yearRangesWithData, floorFactorQuery, floorQuery);
        var floorsWithoutDataInActiveYears = GetFloorsWithoutDataInActiveYears(floorQuery, yearRangeQuery, yearRangesWithData, floorFactorQuery);

        var query = yearRangesWithNoData
            .Concat(floorsWithDataPerYear)
            .Concat(floorsWithoutDataInActiveYears);

        if (queryParameters.IsActive.HasValue)
            query = query.Where(x => x.IsActive == queryParameters.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(queryParameters.SortBy))
            query = query.ApplySort(queryParameters);
        else
            query = query.OrderBy(x => x.YearRangeCVId).ThenBy(x => x.FloorId);

        var totalCount = await query.CountAsync(cancellationToken);
        List<FloorFactorCVMasterDto> items;
        var pageNumber = queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize;
        if (queryParameters.PageSize == -1)
        {
            items = await query.ToListAsync(cancellationToken);
            pageNumber = 1;
            pageSize = totalCount;
        }
        else
        {
            items = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToListAsync(cancellationToken);
        }
        return new PagedResult<FloorFactorCVMasterDto>(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Returns placeholder FloorFactorCVMasterDto rows for all (Floor, YearRange) combinations
    /// where no factor data exists in the database.
    /// </summary>
    private IQueryable<FloorFactorCVMasterDto> GetYearRangesWithNoData(
        IQueryable<AssessmentYearRangeCVEntity> yearRangeQuery,
        IQueryable<int> yearRangesWithData,
        IQueryable<FloorEntity> floorQuery)
    {
        return from yearRange in yearRangeQuery
               where !yearRangesWithData.Contains(yearRange.Id)
               from floor in floorQuery
               select new FloorFactorCVMasterDto
               {
                   Id = 0,
                   FloorId = floor.Id,
                   FloorCode = floor.FloorCode,
                   FloorDescription = floor.Description,
                   FactorWithLift = 0,
                   FactorWithoutLift = 0,
                   YearRangeCVId = yearRange.Id,
                   FromYear = yearRange.FromYear,
                   ToYear = yearRange.ToYear,
                   IsActive = floor.IsActive,
                   CreatedDate = null,
                   UpdatedDate = null
               };
    }

    /// <summary>
    /// Returns FloorFactorCVMasterDto rows for (Floor, YearRange) combinations
    /// where factor data exists in the database.
    /// </summary>
    private IQueryable<FloorFactorCVMasterDto> GetFloorsWithDataPerYear(
        IQueryable<AssessmentYearRangeCVEntity> yearRangeQuery,
        IQueryable<int> yearRangesWithData,
        IQueryable<FloorFactorCVMasterEntity> floorFactorQuery,
        IQueryable<FloorEntity> floorQuery)
    {
        return from yearRange in yearRangeQuery
               where yearRangesWithData.Contains(yearRange.Id)
               join factor in floorFactorQuery
                   on yearRange.Id equals factor.YearRangeCVId
               join floor in floorQuery on factor.FloorId equals floor.Id
               select new FloorFactorCVMasterDto
               {
                   Id = factor.Id,
                   FloorId = floor.Id,
                   FloorCode = floor.FloorCode,
                   FloorDescription = floor.Description,
                  FactorWithLift = factor.FactorWithLift,
                  FactorWithoutLift = factor.FactorWithoutLift,
                   YearRangeCVId = factor.YearRangeCVId,
                   FromYear = yearRange.FromYear,
                   ToYear = yearRange.ToYear,
                   IsActive = factor.IsActive,
                   CreatedDate = factor.CreatedDate,
                   UpdatedDate = factor.UpdatedDate
               };
    }

    /// <summary>
    /// Returns placeholder FloorFactorCVMasterDto rows for (Floor, YearRange) combinations
    /// that are missing factor data, but where the year range has at least some data for other floors.
    /// </summary>
    private IQueryable<FloorFactorCVMasterDto> GetFloorsWithoutDataInActiveYears(
        IQueryable<FloorEntity> floorQuery,
        IQueryable<AssessmentYearRangeCVEntity> yearRangeQuery,
        IQueryable<int> yearRangesWithData,
        IQueryable<FloorFactorCVMasterEntity> floorFactorQuery)
    {
        return from floor in floorQuery
               from yearRange in yearRangeQuery
               where yearRangesWithData.Contains(yearRange.Id)
               join factor in floorFactorQuery
                   on new { FloorId = floor.Id, YearRangeCVId = yearRange.Id }
                   equals new { factor.FloorId, factor.YearRangeCVId } into factorGroup
               from factor in factorGroup.DefaultIfEmpty()
               where factor == null
               select new FloorFactorCVMasterDto
               {
                   Id = 0,
                   FloorId = floor.Id,
                   FloorCode = floor.FloorCode,
                   FloorDescription = floor.Description,
                   FactorWithLift = 0,
                   FactorWithoutLift = 0,
                   YearRangeCVId = yearRange.Id,
                   FromYear = yearRange.FromYear,
                   ToYear = yearRange.ToYear,
                   IsActive = floor.IsActive,
                   CreatedDate = null,
                   UpdatedDate = null
               };
    }
    public override async Task<FloorFactorCVMasterDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var query =
            from floorFactor in _repository.GetQueryable()
            where floorFactor.Id == id
            join floor in _floorRepository.GetQueryable().Where(f => f.IsActive) on floorFactor.FloorId equals floor.Id into floorGroup
            from floor in floorGroup.DefaultIfEmpty()
            join yearRange in _yearRangeCVRepository.GetQueryable().Where(yr => yr.IsActive) on floorFactor.YearRangeCVId equals yearRange.Id into yearRangeGroup
            from yearRange in yearRangeGroup.DefaultIfEmpty()
            select new FloorFactorCVMasterDto
            {
                Id = floorFactor.Id,
                FloorId = floorFactor.FloorId,
                FloorCode = floor != null ? floor.FloorCode : null,
                FloorDescription = floor != null ? floor.Description : null,
                FactorWithLift = floorFactor.FactorWithLift,
                FactorWithoutLift = floorFactor.FactorWithoutLift,
                YearRangeCVId = floorFactor.YearRangeCVId,
                FromYear = yearRange != null ? yearRange.FromYear : (int?)null,
                ToYear = yearRange != null ? yearRange.ToYear : (int?)null,
                IsActive = floorFactor.IsActive,
                CreatedDate = floorFactor.CreatedDate,
                UpdatedDate = floorFactor.UpdatedDate
            };

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

}
