using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master.AgeFactorCVMaster;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class AgeFactorCVMasterService : BaseCommonCrudService<AgeFactorCVMasterEntity, AgeFactorCVMasterDto, CreateAgeFactorCVMasterDto, UpdateAgeFactorCVMasterDto, AgeFactorCVMasterQueryParameters, int>, IAgeFactorCVMasterService
{
    private readonly IRepository<ConstructionTypeEntity, int> _constructionTypeRepository;
    private readonly IRepository<AssessmentYearRangeCVEntity, int> _yearRangeCVRepository;

    public AgeFactorCVMasterService(
        IRepository<AgeFactorCVMasterEntity, int> repository,
        IRepository<ConstructionTypeEntity, int> constructionTypeRepository,
        IRepository<AssessmentYearRangeCVEntity, int> yearRangeCVRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _constructionTypeRepository = constructionTypeRepository;
        _yearRangeCVRepository = yearRangeCVRepository;
    }

    public override async Task<PagedResult<AgeFactorCVMasterDto>> GetAllAsync(
    AgeFactorCVMasterQueryParameters queryParameters,
    CancellationToken cancellationToken = default)
    {
        // Filter ConstructionType records to include only IsActive = 1
        var constructionTypeQuery = _constructionTypeRepository.GetQueryable().Where(c => c.IsActive);
        if (queryParameters.ConstructionTypeId.HasValue)
            constructionTypeQuery = constructionTypeQuery.Where(c => c.Id == queryParameters.ConstructionTypeId.Value);

        // Filter AssessmentYearRangeCV records to include only IsActive = 1
        var yearRangeQuery = _yearRangeCVRepository.GetQueryable().Where(yr => yr.IsActive);
        if (queryParameters.YearRangeCVId.HasValue)
            yearRangeQuery = yearRangeQuery.Where(yr => yr.Id == queryParameters.YearRangeCVId.Value);

        var ageFactorQuery = _repository.GetQueryable();
        if (queryParameters.IsActive.HasValue)
            ageFactorQuery = ageFactorQuery.Where(af => af.IsActive == queryParameters.IsActive.Value);
        if (queryParameters.AgeFrom.HasValue)
            ageFactorQuery = ageFactorQuery.Where(af => af.AgeFrom >= queryParameters.AgeFrom.Value);
        if (queryParameters.AgeTo.HasValue)
            ageFactorQuery = ageFactorQuery.Where(af => af.AgeTo <= queryParameters.AgeTo.Value);

        var yearRangesWithData = ageFactorQuery.Select(af => af.YearRangeCVId).Distinct();

        var yearRangesWithNoData = GetYearRangesWithNoData(yearRangeQuery, yearRangesWithData, constructionTypeQuery);
        var constructionTypesWithDataPerYear = GetConstructionTypesWithDataPerYear(yearRangeQuery, yearRangesWithData, ageFactorQuery, constructionTypeQuery);
        var constructionTypesWithoutDataInActiveYears = GetConstructionTypesWithoutDataInActiveYears(constructionTypeQuery, yearRangeQuery, yearRangesWithData, ageFactorQuery);

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
        List<AgeFactorCVMasterDto> items;
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
        return new PagedResult<AgeFactorCVMasterDto>(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Returns placeholder AgeFactorCVMasterDto rows for all (ConstructionType, YearRange) combinations
    /// where no factor data exists in the database.
    /// </summary>
    private IQueryable<AgeFactorCVMasterDto> GetYearRangesWithNoData(
        IQueryable<AssessmentYearRangeCVEntity> yearRangeQuery,
        IQueryable<int> yearRangesWithData,
        IQueryable<ConstructionTypeEntity> constructionTypeQuery)
    {
        return from yearRange in yearRangeQuery
               where !yearRangesWithData.Contains(yearRange.Id)
               from constructionType in constructionTypeQuery
               select new AgeFactorCVMasterDto
               {
                   Id = 0,
                   ConstructionTypeId = constructionType.Id,
                   ConstructionCode = constructionType.ConstructionCode,
                   ConstructionDescription = constructionType.Description,
                   AgeFrom = 0,
                   AgeTo = 5,
                   Factor = 1,
                   YearRangeCVId = yearRange.Id,
                   FromYear = yearRange.FromYear,
                   ToYear = yearRange.ToYear,
                   IsActive = constructionType.IsActive,
                   CreatedDate = null,
                   UpdatedDate = null
               };
    }

    /// <summary>
    /// Returns AgeFactorCVMasterDto rows for (ConstructionType, YearRange) combinations
    /// where factor data exists in the database.
    /// </summary>
    private IQueryable<AgeFactorCVMasterDto> GetConstructionTypesWithDataPerYear(
        IQueryable<AssessmentYearRangeCVEntity> yearRangeQuery,
        IQueryable<int> yearRangesWithData,
        IQueryable<AgeFactorCVMasterEntity> ageFactorQuery,
        IQueryable<ConstructionTypeEntity> constructionTypeQuery)
    {
        return from yearRange in yearRangeQuery
               where yearRangesWithData.Contains(yearRange.Id)
               join factor in ageFactorQuery
                   on yearRange.Id equals factor.YearRangeCVId
               join constructionType in constructionTypeQuery on factor.ConstructionTypeId equals constructionType.Id
               select new AgeFactorCVMasterDto
               {
                   Id = factor.Id,
                   ConstructionTypeId = constructionType.Id,
                   ConstructionCode = constructionType.ConstructionCode,
                   ConstructionDescription = constructionType.Description,
                   AgeFrom = factor.AgeFrom,
                   AgeTo = factor.AgeTo,
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
    /// Returns placeholder AgeFactorCVMasterDto rows for (ConstructionType, YearRange) combinations
    /// that are missing factor data, but where the year range has at least some data for other construction types.
    /// </summary>
    private IQueryable<AgeFactorCVMasterDto> GetConstructionTypesWithoutDataInActiveYears(
        IQueryable<ConstructionTypeEntity> constructionTypeQuery,
        IQueryable<AssessmentYearRangeCVEntity> yearRangeQuery,
        IQueryable<int> yearRangesWithData,
        IQueryable<AgeFactorCVMasterEntity> ageFactorQuery)
    {
        return from constructionType in constructionTypeQuery
               from yearRange in yearRangeQuery
               where yearRangesWithData.Contains(yearRange.Id)
               join factor in ageFactorQuery
                   on new { ConstructionTypeId = constructionType.Id, YearRangeCVId = yearRange.Id }
                   equals new { factor.ConstructionTypeId, factor.YearRangeCVId } into factorGroup
               from factor in factorGroup.DefaultIfEmpty()
               where factor == null
               select new AgeFactorCVMasterDto
               {
                   Id = 0,
                   ConstructionTypeId = constructionType.Id,
                   ConstructionCode = constructionType.ConstructionCode,
                   ConstructionDescription = constructionType.Description,
                   AgeFrom = 0,
                   AgeTo = 5,
                   Factor = 1,
                   YearRangeCVId = yearRange.Id,
                   FromYear = yearRange.FromYear,
                   ToYear = yearRange.ToYear,
                   IsActive = constructionType.IsActive,
                   CreatedDate = null,
                   UpdatedDate = null
               };
    }

    public override async Task<AgeFactorCVMasterDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var query =
            from ageFactor in _repository.GetQueryable()
            where ageFactor.Id == id
            join constructionType in _constructionTypeRepository.GetQueryable().Where(c => c.IsActive)
                on ageFactor.ConstructionTypeId equals constructionType.Id into constructionTypeGroup
            from constructionType in constructionTypeGroup.DefaultIfEmpty()
            join yearRange in _yearRangeCVRepository.GetQueryable().Where(yr => yr.IsActive)
                on ageFactor.YearRangeCVId equals yearRange.Id into yearRangeGroup
            from yearRange in yearRangeGroup.DefaultIfEmpty()
            select new AgeFactorCVMasterDto
            {
                Id = ageFactor.Id,
                ConstructionTypeId = ageFactor.ConstructionTypeId,
                ConstructionCode = constructionType != null ? constructionType.ConstructionCode : null,
                ConstructionDescription = constructionType != null ? constructionType.Description : null,
                AgeFrom = ageFactor.AgeFrom,
                AgeTo = ageFactor.AgeTo,
                Factor = ageFactor.Factor,
                YearRangeCVId = ageFactor.YearRangeCVId,
                FromYear = yearRange != null ? yearRange.FromYear : (int?)null,
                ToYear = yearRange != null ? yearRange.ToYear : (int?)null,
                IsActive = ageFactor.IsActive,
                CreatedDate = ageFactor.CreatedDate,
                UpdatedDate = ageFactor.UpdatedDate
            };

        return await query.FirstOrDefaultAsync(cancellationToken);
    }
}
