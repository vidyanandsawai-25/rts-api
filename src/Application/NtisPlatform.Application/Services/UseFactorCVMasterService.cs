using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master.UseFactorCVMaster;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class UseFactorCVMasterService : BaseCommonCrudService<UseFactorCVMasterEntity, UseFactorCVMasterDto, CreateUseFactorCVMasterDto, UpdateUseFactorCVMasterDto, UseFactorCVMasterQueryParameters, int>, IUseFactorCVMasterService
{
    private readonly IRepository<TypeOfUseEntity, int> _typeOfUseRepository;
    private readonly IRepository<SubTypeOfUseEntity, int> _subTypeOfUseRepository;
    private readonly IRepository<AssessmentYearRangeCVEntity, int> _yearRangeCVRepository;

    public UseFactorCVMasterService(
        IRepository<UseFactorCVMasterEntity, int> repository,
        IRepository<TypeOfUseEntity, int> typeOfUseRepository,
        IRepository<SubTypeOfUseEntity, int> subTypeOfUseRepository,
        IRepository<AssessmentYearRangeCVEntity, int> yearRangeCVRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _typeOfUseRepository = typeOfUseRepository;
        _subTypeOfUseRepository = subTypeOfUseRepository;
        _yearRangeCVRepository = yearRangeCVRepository;
    }

    public override async Task<PagedResult<UseFactorCVMasterDto>> GetAllAsync(
    UseFactorCVMasterQueryParameters queryParameters,
    CancellationToken cancellationToken = default)
    {
        // Filter SubTypeOfUse records to include only IsActive = 1
        var subTypeOfUseQuery = _subTypeOfUseRepository.GetQueryable().Where(s => s.IsActive);
        if (queryParameters.TypeOfUseId.HasValue)
            subTypeOfUseQuery = subTypeOfUseQuery.Where(s => s.TypeOfUseId == queryParameters.TypeOfUseId.Value);
        if (queryParameters.SubTypeOfUseId.HasValue)
            subTypeOfUseQuery = subTypeOfUseQuery.Where(s => s.Id == queryParameters.SubTypeOfUseId.Value);
        if (queryParameters.IsActive.HasValue)
            subTypeOfUseQuery = subTypeOfUseQuery.Where(s => s.IsActive == queryParameters.IsActive.Value);

        // Filter AssessmentYearRangeCV records to include only IsActive = 1
        var yearRangeQuery = _yearRangeCVRepository.GetQueryable().Where(yr => yr.IsActive);
        if (queryParameters.YearRangeCVId.HasValue)
            yearRangeQuery = yearRangeQuery.Where(yr => yr.Id == queryParameters.YearRangeCVId.Value);

        var useFactorQuery = _repository.GetQueryable();
        if (queryParameters.IsActive.HasValue)
            useFactorQuery = useFactorQuery.Where(uf => uf.IsActive == queryParameters.IsActive.Value);

        var yearRangesWithData = useFactorQuery.Select(uf => uf.YearRangeCVId).Distinct();

        var yearRangesWithNoData = GetYearRangesWithNoData(yearRangeQuery, yearRangesWithData, subTypeOfUseQuery);
        var combinationsWithDataPerYear = GetCombinationsWithDataPerYear(yearRangeQuery, yearRangesWithData, useFactorQuery, subTypeOfUseQuery);
        var combinationsWithoutDataInActiveYears = GetCombinationsWithoutDataInActiveYears(subTypeOfUseQuery, yearRangeQuery, yearRangesWithData, useFactorQuery);

        var query = yearRangesWithNoData
            .Concat(combinationsWithDataPerYear)
            .Concat(combinationsWithoutDataInActiveYears);

        if (queryParameters.IsActive.HasValue)
            query = query.Where(x => x.IsActive == queryParameters.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(queryParameters.SortBy))
            query = query.ApplySort(queryParameters);
        else
            query = query.OrderBy(x => x.TypeOfUseId).ThenBy(x => x.SubTypeOfUseId).ThenBy(x => x.YearRangeCVId);

        var totalCount = await query.CountAsync(cancellationToken);
        List<UseFactorCVMasterDto> items;
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
        return new PagedResult<UseFactorCVMasterDto>(items, totalCount, pageNumber, pageSize);
    }


    /// <summary>
    /// Returns placeholder UseFactorCVMasterDto rows for all (TypeOfUse, SubTypeOfUse, YearRange) combinations
    /// where no factor data exists in the database.
    /// </summary>
    private IQueryable<UseFactorCVMasterDto> GetYearRangesWithNoData(
      IQueryable<AssessmentYearRangeCVEntity> yearRangeQuery,
      IQueryable<int> yearRangesWithData,
      IQueryable<SubTypeOfUseEntity> subTypeOfUseQuery)
    {
        return from yearRange in yearRangeQuery
               where !yearRangesWithData.Contains(yearRange.Id)
               from subTypeOfUse in subTypeOfUseQuery
               join typeOfUse in _typeOfUseRepository.GetQueryable().Where(t => t.IsActive)
                   on subTypeOfUse.TypeOfUseId equals typeOfUse.Id
               select new UseFactorCVMasterDto
               {
                   Id = 0,
                   TypeOfUseId = typeOfUse.Id,
                   TypeOfUseCode = typeOfUse.TypeOfUseCode,
                   TypeOfUseDescription = typeOfUse.Description,
                   Type = typeOfUse.Type,
                   TypeOfUseGroupId = typeOfUse.TypeOfUseGroupId,
                   SubTypeOfUseId = subTypeOfUse.Id,
                   SubTypeOfUseDescription = subTypeOfUse.Description,
                   Factor = 1,
                   YearRangeCVId = yearRange.Id,
                   FromYear = yearRange.FromYear,
                   ToYear = yearRange.ToYear,
                   IsActive = subTypeOfUse.IsActive,
                   CreatedDate = null,
                   UpdatedDate = null
               };
    }

    /// <summary>
    /// Returns UseFactorCVMasterDto rows for (TypeOfUse, SubTypeOfUse, YearRange) combinations
    /// where factor data exists in the database.
    /// </summary>
    private IQueryable<UseFactorCVMasterDto> GetCombinationsWithDataPerYear(
    IQueryable<AssessmentYearRangeCVEntity> yearRangeQuery,
    IQueryable<int> yearRangesWithData,
    IQueryable<UseFactorCVMasterEntity> useFactorQuery,
    IQueryable<SubTypeOfUseEntity> subTypeOfUseQuery)
    {
        return from yearRange in yearRangeQuery
               where yearRangesWithData.Contains(yearRange.Id)
               join factor in useFactorQuery
                   on yearRange.Id equals factor.YearRangeCVId
               join subTypeOfUse in subTypeOfUseQuery on factor.SubTypeOfUseId equals subTypeOfUse.Id
               join typeOfUse in _typeOfUseRepository.GetQueryable().Where(t => t.IsActive) // Add IsActive filter
                   on factor.TypeOfUseId equals typeOfUse.Id
               select new UseFactorCVMasterDto
               {
                   Id = factor.Id,
                   TypeOfUseId = typeOfUse.Id,
                   TypeOfUseCode = typeOfUse.TypeOfUseCode,
                   TypeOfUseDescription = typeOfUse.Description,
                   Type = typeOfUse.Type,
                   TypeOfUseGroupId = typeOfUse.TypeOfUseGroupId,
                   SubTypeOfUseId = subTypeOfUse.Id,
                   SubTypeOfUseDescription = subTypeOfUse.Description,
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
    /// Returns placeholder UseFactorCVMasterDto rows for (TypeOfUse, SubTypeOfUse, YearRange) combinations
    /// that are missing factor data, but where the year range has at least some data for other combinations.
    /// </summary>
    private IQueryable<UseFactorCVMasterDto> GetCombinationsWithoutDataInActiveYears(
    IQueryable<SubTypeOfUseEntity> subTypeOfUseQuery,
    IQueryable<AssessmentYearRangeCVEntity> yearRangeQuery,
    IQueryable<int> yearRangesWithData,
    IQueryable<UseFactorCVMasterEntity> useFactorQuery)
    {
        return from subTypeOfUse in subTypeOfUseQuery
               join typeOfUse in _typeOfUseRepository.GetQueryable().Where(t => t.IsActive) // Add IsActive filter
                   on subTypeOfUse.TypeOfUseId equals typeOfUse.Id
               from yearRange in yearRangeQuery
               where yearRangesWithData.Contains(yearRange.Id)
               join factor in useFactorQuery
                   on new { TypeOfUseId = typeOfUse.Id, SubTypeOfUseId = subTypeOfUse.Id, YearRangeCVId = yearRange.Id }
                   equals new { factor.TypeOfUseId, factor.SubTypeOfUseId, factor.YearRangeCVId } into factorGroup
               from factor in factorGroup.DefaultIfEmpty()
               where factor == null
               select new UseFactorCVMasterDto
               {
                   Id = 0,
                   TypeOfUseId = typeOfUse.Id,
                   TypeOfUseCode = typeOfUse.TypeOfUseCode,
                   TypeOfUseDescription = typeOfUse.Description,
                   Type = typeOfUse.Type,
                   TypeOfUseGroupId = typeOfUse.TypeOfUseGroupId,
                   SubTypeOfUseId = subTypeOfUse.Id,
                   SubTypeOfUseDescription = subTypeOfUse.Description,
                   Factor = 1,
                   YearRangeCVId = yearRange.Id,
                   FromYear = yearRange.FromYear,
                   ToYear = yearRange.ToYear,
                   IsActive = subTypeOfUse.IsActive,
                   CreatedDate = null,
                   UpdatedDate = null
               };
    }

    public override async Task<UseFactorCVMasterDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var query =
            from useFactor in _repository.GetQueryable()
            where useFactor.Id == id
            join subTypeOfUse in _subTypeOfUseRepository.GetQueryable() on useFactor.SubTypeOfUseId equals subTypeOfUse.Id into subTypeOfUseGroup
            from subTypeOfUse in subTypeOfUseGroup.DefaultIfEmpty()
            join typeOfUse in _typeOfUseRepository.GetQueryable() on useFactor.TypeOfUseId equals typeOfUse.Id into typeOfUseGroup
            from typeOfUse in typeOfUseGroup.DefaultIfEmpty()
            join yearRange in _yearRangeCVRepository.GetQueryable() on useFactor.YearRangeCVId equals yearRange.Id into yearRangeGroup
            from yearRange in yearRangeGroup.DefaultIfEmpty()
            select new UseFactorCVMasterDto
            {
                Id = useFactor.Id,
                TypeOfUseId = useFactor.TypeOfUseId,
                TypeOfUseCode = typeOfUse != null ? typeOfUse.TypeOfUseCode : null,
                TypeOfUseDescription = typeOfUse != null ? typeOfUse.Description : null,
                Type = typeOfUse != null ? typeOfUse.Type : null,
                TypeOfUseGroupId = typeOfUse != null ? typeOfUse.TypeOfUseGroupId : (int?)null,
                SubTypeOfUseId = useFactor.SubTypeOfUseId,
                SubTypeOfUseDescription = subTypeOfUse != null ? subTypeOfUse.Description : null,
                Factor = useFactor.Factor,
                YearRangeCVId = useFactor.YearRangeCVId,
                FromYear = yearRange != null ? yearRange.FromYear : (int?)null,
                ToYear = yearRange != null ? yearRange.ToYear : (int?)null,
                IsActive = useFactor.IsActive,
                CreatedDate = useFactor.CreatedDate,
                UpdatedDate = useFactor.UpdatedDate
            };

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

   
}
