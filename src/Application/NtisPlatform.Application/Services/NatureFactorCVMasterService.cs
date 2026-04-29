using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master.NatureFactorCVMaster;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class NatureFactorCVMasterService : BaseCommonCrudService<NatureFactorCVMasterEntity, NatureFactorCVMasterDto, CreateNatureFactorCVMasterDto, UpdateNatureFactorCVMasterDto, NatureFactorCVMasterQueryParameters, int>, INatureFactorCVMasterService
{
    private readonly IRepository<ConstructionTypeEntity, int> _constructionTypeRepository;
    private readonly IRepository<AssessmentYearRangeCVEntity, int> _yearRangeCVRepository;

    public NatureFactorCVMasterService(
        IRepository<NatureFactorCVMasterEntity, int> repository,
        IRepository<ConstructionTypeEntity, int> constructionTypeRepository,
        IRepository<AssessmentYearRangeCVEntity, int> yearRangeCVRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _constructionTypeRepository = constructionTypeRepository;
        _yearRangeCVRepository = yearRangeCVRepository;
    }

    public override async Task<PagedResult<NatureFactorCVMasterDto>> GetAllAsync(
        NatureFactorCVMasterQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var constructionTypeQuery = _constructionTypeRepository.GetQueryable();
        if (queryParameters.ConstructionTypeId.HasValue)
            constructionTypeQuery = constructionTypeQuery.Where(c => c.Id == queryParameters.ConstructionTypeId.Value);

        var yearRangeQuery = _yearRangeCVRepository.GetQueryable().Where(yr => yr.IsActive);
        if (queryParameters.YearRangeCVId.HasValue)
            yearRangeQuery = yearRangeQuery.Where(yr => yr.Id == queryParameters.YearRangeCVId.Value);

        var natureFactorQuery = _repository.GetQueryable();
        if (queryParameters.IsActive.HasValue)
            natureFactorQuery = natureFactorQuery.Where(nf => nf.IsActive == queryParameters.IsActive.Value);

        var yearRangesWithData = natureFactorQuery.Select(nf => nf.YearRangeCVId).Distinct();

        var yearRangesWithNoData = GetYearRangesWithNoData(yearRangeQuery, yearRangesWithData, constructionTypeQuery);
        var constructionTypesWithDataPerYear = GetConstructionTypesWithDataPerYear(yearRangeQuery, yearRangesWithData, natureFactorQuery, constructionTypeQuery);
        var constructionTypesWithoutDataInActiveYears = GetConstructionTypesWithoutDataInActiveYears(constructionTypeQuery, yearRangeQuery, yearRangesWithData, natureFactorQuery);

        var query = yearRangesWithNoData
            .Concat(constructionTypesWithDataPerYear)
            .Concat(constructionTypesWithoutDataInActiveYears);

        if (queryParameters.IsActive.HasValue)
            query = query.Where(x => x.IsActive == queryParameters.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(queryParameters.SortBy))
            query = query.ApplySort(queryParameters);
        else
            query = query.OrderBy(x => x.YearRangeCVId).ThenBy(x => x.ConstructionTypeId);

        var totalCount = await query.CountAsync(cancellationToken);
        List<NatureFactorCVMasterDto> items;
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
        return new PagedResult<NatureFactorCVMasterDto>(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Returns placeholder NatureFactorCVMasterDto rows for all (ConstructionType, YearRange) combinations
    /// where no factor data exists in the database.
    /// </summary>
    private IQueryable<NatureFactorCVMasterDto> GetYearRangesWithNoData(
        IQueryable<AssessmentYearRangeCVEntity> yearRangeQuery,
        IQueryable<int> yearRangesWithData,
        IQueryable<ConstructionTypeEntity> constructionTypeQuery)
    {
        return from yearRange in yearRangeQuery
               where !yearRangesWithData.Contains(yearRange.Id)
               from constructionType in constructionTypeQuery
               select new NatureFactorCVMasterDto
               {
                   Id = 0,
                   ConstructionTypeId = constructionType.Id,
                   ConstructionCode = constructionType.ConstructionCode,
                   ConstructionDescription = constructionType.Description,
                   Factor = 0,
                   YearRangeCVId = yearRange.Id,
                   FromYear = yearRange.FromYear,
                   ToYear = yearRange.ToYear,
                   IsActive = constructionType.IsActive,
                   CreatedDate = null,
                   UpdatedDate = null
               };
    }

    /// <summary>
    /// Returns NatureFactorCVMasterDto rows for (ConstructionType, YearRange) combinations
    /// where factor data exists in the database.
    /// </summary>
    private IQueryable<NatureFactorCVMasterDto> GetConstructionTypesWithDataPerYear(
        IQueryable<AssessmentYearRangeCVEntity> yearRangeQuery,
        IQueryable<int> yearRangesWithData,
        IQueryable<NatureFactorCVMasterEntity> natureFactorQuery,
        IQueryable<ConstructionTypeEntity> constructionTypeQuery)
    {
        return from yearRange in yearRangeQuery
               where yearRangesWithData.Contains(yearRange.Id)
               join factor in natureFactorQuery
                   on yearRange.Id equals factor.YearRangeCVId
               join constructionType in constructionTypeQuery on factor.ConstructionTypeId equals constructionType.Id
               select new NatureFactorCVMasterDto
               {
                   Id = factor.Id,
                   ConstructionTypeId = constructionType.Id,
                   ConstructionCode = constructionType.ConstructionCode,
                   ConstructionDescription = constructionType.Description,
                   Factor = factor.Factor,
                   YearRangeCVId = factor.YearRangeCVId,
                   FromYear = yearRange.FromYear,
                   ToYear = yearRange.ToYear,
                   IsActive = factor.IsActive,
                   CreatedDate = factor.CreatedDate,
                   UpdatedDate = factor.UpdatedDate
               };
    }

    /// <summary>
    /// Returns placeholder NatureFactorCVMasterDto rows for (ConstructionType, YearRange) combinations
    /// that are missing factor data, but where the year range has at least some data for other construction types.
    /// </summary>
    private IQueryable<NatureFactorCVMasterDto> GetConstructionTypesWithoutDataInActiveYears(
        IQueryable<ConstructionTypeEntity> constructionTypeQuery,
        IQueryable<AssessmentYearRangeCVEntity> yearRangeQuery,
        IQueryable<int> yearRangesWithData,
        IQueryable<NatureFactorCVMasterEntity> natureFactorQuery)
    {
        return from constructionType in constructionTypeQuery
               from yearRange in yearRangeQuery
               where yearRangesWithData.Contains(yearRange.Id)
               join factor in natureFactorQuery
                   on new { ConstructionTypeId = constructionType.Id, YearRangeCVId = yearRange.Id }
                   equals new { factor.ConstructionTypeId, factor.YearRangeCVId } into factorGroup
               from factor in factorGroup.DefaultIfEmpty()
               where factor == null
               select new NatureFactorCVMasterDto
               {
                   Id = 0,
                   ConstructionTypeId = constructionType.Id,
                   ConstructionCode = constructionType.ConstructionCode,
                   ConstructionDescription = constructionType.Description,
                   Factor = 0,
                   YearRangeCVId = yearRange.Id,
                   FromYear = yearRange.FromYear,
                   ToYear = yearRange.ToYear,
                   IsActive = constructionType.IsActive,
                   CreatedDate = null,
                   UpdatedDate = null
               };
    }

    public override async Task<NatureFactorCVMasterDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var query =
            from natureFactor in _repository.GetQueryable()
            where natureFactor.Id == id
            join constructionType in _constructionTypeRepository.GetQueryable() on natureFactor.ConstructionTypeId equals constructionType.Id into constructionTypeGroup
            from constructionType in constructionTypeGroup.DefaultIfEmpty()
            join yearRange in _yearRangeCVRepository.GetQueryable() on natureFactor.YearRangeCVId equals yearRange.Id into yearRangeGroup
            from yearRange in yearRangeGroup.DefaultIfEmpty()
            select new NatureFactorCVMasterDto
            {
                Id = natureFactor.Id,
                ConstructionTypeId = natureFactor.ConstructionTypeId,
                ConstructionCode = constructionType != null ? constructionType.ConstructionCode : null,
                ConstructionDescription = constructionType != null ? constructionType.Description : null,
                Factor = natureFactor.Factor,
                YearRangeCVId = natureFactor.YearRangeCVId,
                FromYear = yearRange != null ? yearRange.FromYear : (int?)null,
                ToYear = yearRange != null ? yearRange.ToYear : (int?)null,
                IsActive = natureFactor.IsActive,
                CreatedDate = natureFactor.CreatedDate,
                UpdatedDate = natureFactor.UpdatedDate
            };

        return await query.FirstOrDefaultAsync(cancellationToken);
    }
   
}
