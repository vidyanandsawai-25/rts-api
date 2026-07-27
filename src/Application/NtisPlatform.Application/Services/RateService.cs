using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Utilities;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Constants;

namespace NtisPlatform.Application.Services;

public class RateService : BaseCommonCrudService<RateEntity, RateDto, CreateRateDto, UpdateRateDto, RateQueryParameters, int>, IRateService
{
    private readonly IRepository<TaxZoneEntity> _taxZoneRepository;
    private readonly IRepository<FloorEntity> _floorRepository;
    private readonly IRepository<ConstructionTypeEntity> _constructionTypeRepository;
    private readonly IRepository<TypeOfUseGroupEntity> _typeOfUseGroupRepository;
    private readonly IRepository<AssessmentYearRangeEntity> _assessmentYearRangeRepository;
    private readonly IRepository<RateSectionEntity> _rateSectionRepository;
    private readonly IRepository<TypeOfUseEntity> _typeOfUseRepository;
    private readonly IRepository<TypeOfUseCategoryEntity> _typeOfUseCategoryRepository;

    public RateService(
        IRepository<RateEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRepository<TaxZoneEntity> taxZoneRepository,
        IRepository<FloorEntity> floorRepository,
        IRepository<ConstructionTypeEntity> constructionTypeRepository,
        IRepository<TypeOfUseGroupEntity> typeOfUseGroupRepository,
        IRepository<AssessmentYearRangeEntity> assessmentYearRangeRepository,
        IRepository<RateSectionEntity> rateSectionRepository,
        IRepository<TypeOfUseEntity> typeOfUseRepository,
        IRepository<TypeOfUseCategoryEntity> typeOfUseCategoryRepository)
        : base(repository, unitOfWork, mapper)
    {
        _taxZoneRepository = taxZoneRepository;
        _floorRepository = floorRepository;
        _constructionTypeRepository = constructionTypeRepository;
        _typeOfUseGroupRepository = typeOfUseGroupRepository;
        _assessmentYearRangeRepository = assessmentYearRangeRepository;
        _rateSectionRepository = rateSectionRepository;
        _typeOfUseRepository = typeOfUseRepository;
        _typeOfUseCategoryRepository = typeOfUseCategoryRepository;
    }

    public async Task<PagedResult<DetailedRateDto>> GetDetailedAllAsync(RateQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable();

        // Apply filters/search/sort to the base query
        query = query.ApplyFilters(queryParameters);
        query = query.ApplySearch(queryParameters);
        query = query.ApplySort(queryParameters);

        var totalCount = await query.CountAsync(cancellationToken);

        var detailedQuery = from r in query
                            join tz in _taxZoneRepository.GetQueryable() on r.TaxZoneId equals tz.Id into tzJoined
                            from tz in tzJoined.DefaultIfEmpty()
                            join f in _floorRepository.GetQueryable() on r.FloorId equals f.Id into fJoined
                            from f in fJoined.DefaultIfEmpty()
                            join ct in _constructionTypeRepository.GetQueryable() on r.ConstructionTypeId equals ct.Id into ctJoined
                            from ct in ctJoined.DefaultIfEmpty()
                            join toug in _typeOfUseGroupRepository.GetQueryable() on r.TypeOfUseGroupId equals toug.Id into tougJoined
                            from toug in tougJoined.DefaultIfEmpty()
                            join ayr in _assessmentYearRangeRepository.GetQueryable() on r.YearRangeRVId equals ayr.Id into ayrJoined
                            from ayr in ayrJoined.DefaultIfEmpty()
                            join rs in _rateSectionRepository.GetQueryable() on r.RateSectionId equals rs.Id into rsJoined
                            from rs in rsJoined.DefaultIfEmpty()
                            select new DetailedRateDto
                            {
                                Id = r.Id,
                                TaxZoneId = r.TaxZoneId,
                                TaxZone = tz != null ? tz.TaxZoneNo : string.Empty,
                                FloorId = r.FloorId,
                                Floor = f != null ? f.Description : string.Empty,
                                ConstructionTypeId = r.ConstructionTypeId,
                                ConstructionType = ct != null ? ct.Description : string.Empty,
                                TypeOfUseGroupId = r.TypeOfUseGroupId,
                                TypeOfUseGroup = toug != null ? toug.GroupName : string.Empty,
                                YearRangeRVId = r.YearRangeRVId,
                                YearRangeRV = ayr != null ? ayr.FromYear + "-" + ayr.ToYear : string.Empty,
                                RateSectionId = r.RateSectionId,
                                RateSection = rs != null ? rs.Description : string.Empty,
                                RateRemark = r.RateRemark,
                                RateSquareFeet = r.RateSquareFeet,
                                RateSquareMeter = r.RateSquareMeter,
                                IsActive = r.IsActive,
                                CreatedDate = r.CreatedDate,
                                UpdatedDate = r.UpdatedDate,
                                CreatedBy = r.CreatedBy,
                                UpdatedBy = r.UpdatedBy
                            };


        var items = await detailedQuery
            .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize)
            .ToListAsync(cancellationToken);

        // Normalize pagination metadata for unpaged results (PageSize = -1)
        var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

        return new PagedResult<DetailedRateDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<TypeOfUseDetailsDto>> GetTypeOfUseDetailsAsync(TypeOfUseQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var query = from tou in _typeOfUseRepository.GetQueryable()
                    join touc in _typeOfUseCategoryRepository.GetQueryable() on tou.TypeOfUseCategoryId equals touc.Id into toucJoined
                    from touc in toucJoined.DefaultIfEmpty()
                    join toug in _typeOfUseGroupRepository.GetQueryable() on tou.TypeOfUseGroupId equals toug.Id into tougJoined
                    from toug in tougJoined.DefaultIfEmpty()
                    where tou.IsActive && (touc == null || touc.IsActive)
                       && (touc != null && (touc.TypeOfUseCategoryCode == TypeOfUseConstants.Parking || 
                                            touc.TypeOfUseCategoryCode == TypeOfUseConstants.OpenSpace))
                    select new TypeOfUseDetailsDto
                    {
                        Id = tou.Id,
                        Description = tou.Description,
                        TypeOfUseCode = tou.TypeOfUseCode,
                        TypeOfUseGroupId = tou.TypeOfUseGroupId,
                        TypeOfUseCategoryId = tou.TypeOfUseCategoryId,
                        TypeOfUseCategoryName = touc != null ? touc.TypeOfUseCategoryName : null,
                        TypeOfUseCategoryCode = touc != null ? touc.TypeOfUseCategoryCode : null,
                        TypeOfUseGroupCode = toug != null ? toug.TypeOfUseGroupCode : null,
                        GroupName = toug != null ? toug.GroupName : null
                    };

        return await ExecuteTypeOfUseQueryAsync(query, queryParameters, cancellationToken);
    }

    public async Task<PagedResult<TypeOfUseDetailsDto>> GetOpenPlotTypeOfUseDetailsAsync(TypeOfUseQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var query = from tou in _typeOfUseRepository.GetQueryable()
                    join touc in _typeOfUseCategoryRepository.GetQueryable() on tou.TypeOfUseCategoryId equals touc.Id into toucJoined
                    from touc in toucJoined.DefaultIfEmpty()
                    join toug in _typeOfUseGroupRepository.GetQueryable() on tou.TypeOfUseGroupId equals toug.Id into tougJoined
                    from toug in tougJoined.DefaultIfEmpty()
                    where tou.IsActive && (touc == null || touc.IsActive)
                       && (touc != null && (touc.TypeOfUseCategoryCode == TypeOfUseConstants.Parking || 
                                            touc.TypeOfUseCategoryCode == TypeOfUseConstants.Op || 
                                            touc.TypeOfUseCategoryCode == TypeOfUseConstants.OpenSpace))
                       && toug != null && toug.IsOpenPlot && toug.IsActive
                    select new TypeOfUseDetailsDto
                    {
                        Id = tou.Id,
                        Description = tou.Description,
                        TypeOfUseCode = tou.TypeOfUseCode,
                        TypeOfUseGroupId = tou.TypeOfUseGroupId,
                        TypeOfUseCategoryId = tou.TypeOfUseCategoryId,
                        TypeOfUseCategoryName = touc != null ? touc.TypeOfUseCategoryName : null,
                        TypeOfUseCategoryCode = touc != null ? touc.TypeOfUseCategoryCode : null,
                        TypeOfUseGroupCode = toug.TypeOfUseGroupCode,
                        GroupName = toug.GroupName
                    };

        return await ExecuteTypeOfUseQueryAsync(query, queryParameters, cancellationToken);
    }

    private static async Task<PagedResult<TypeOfUseDetailsDto>> ExecuteTypeOfUseQueryAsync(
        IQueryable<TypeOfUseDetailsDto> query,
        TypeOfUseQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        // Handle SearchTerm if provided
        if (!string.IsNullOrWhiteSpace(queryParameters.SearchTerm))
        {
            var searchTerm = queryParameters.SearchTerm.Trim().ToLower();
            query = query.Where(x => (x.TypeOfUseCode != null && x.TypeOfUseCode.ToLower().Contains(searchTerm)) ||
                                     (x.Description != null && x.Description.ToLower().Contains(searchTerm)) ||
                                     (x.GroupName != null && x.GroupName.ToLower().Contains(searchTerm)));
        }

        // Apply Sorting (default to Id or SortBy)
        if (!string.IsNullOrWhiteSpace(queryParameters.SortBy))
        {
            var allowedSortFields = new[] { "Id", "TypeOfUseCode", "Description", "GroupName" };
            if (!allowedSortFields.Contains(queryParameters.SortBy, StringComparer.OrdinalIgnoreCase))
            {
                throw new FilterValidationException("SortBy", $"Field '{queryParameters.SortBy}' is not sortable. Allowed fields: {string.Join(", ", allowedSortFields)}");
            }

            var isDesc = string.Equals(queryParameters.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(queryParameters.SortBy, "TypeOfUseCode", StringComparison.OrdinalIgnoreCase))
            {
                query = isDesc ? query.OrderByDescending(x => x.TypeOfUseCode) : query.OrderBy(x => x.TypeOfUseCode);
            }
            else if (string.Equals(queryParameters.SortBy, "Description", StringComparison.OrdinalIgnoreCase))
            {
                query = isDesc ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description);
            }
            else if (string.Equals(queryParameters.SortBy, "GroupName", StringComparison.OrdinalIgnoreCase))
            {
                query = isDesc ? query.OrderByDescending(x => x.GroupName) : query.OrderBy(x => x.GroupName);
            }
            else
            {
                query = isDesc ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id);
            }
        }
        else
        {
            query = query.OrderBy(x => x.Id);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagination = PaginationHelper.Calculate(
            queryParameters.PageNumber,
            queryParameters.PageSize,
            totalCount);

        var items = await query
            .Skip(pagination.skip)
            .Take(pagination.take)
            .ToListAsync(cancellationToken);

        return new PagedResult<TypeOfUseDetailsDto>(items, totalCount, pagination.pageNumber, pagination.pageSize);
    }

    public override async Task<RateDto> CreateAsync(CreateRateDto createDto, CancellationToken cancellationToken = default)
    {
        if (createDto.FloorId == 0)
        {
            var floorId = await ResolveGroundFloorIdAsync(cancellationToken);
            if (floorId > 0)
            {
                createDto.FloorId = floorId;
            }
        }
        return await base.CreateAsync(createDto, cancellationToken);
    }

    public override async Task<BulkResult<RateDto>> BulkCreateAsync(CreateRateDto[] items, CancellationToken cancellationToken = default)
    {
        var groundFloorId = 0;
        foreach (var item in items)
        {
            if (item.FloorId == 0)
            {
                if (groundFloorId == 0)
                {
                    groundFloorId = await ResolveGroundFloorIdAsync(cancellationToken);
                }
                if (groundFloorId > 0)
                {
                    item.FloorId = groundFloorId;
                }
            }
        }
        return await base.BulkCreateAsync(items, cancellationToken);
    }

    public async Task<RateDto> CreateOpenPlotAsync(CreateOpenPlotRateDto createDto, CancellationToken cancellationToken = default)
    {
        var constructionTypeId = await ResolveOpenPlotConstructionTypeIdAsync(cancellationToken);

        var rateCreateDto = MapToCreateRateDto(createDto, constructionTypeId);

        return await CreateAsync(rateCreateDto, cancellationToken);
    }

    public async Task<BulkResult<RateDto>> BulkCreateOpenPlotAsync(CreateOpenPlotRateDto[] items, CancellationToken cancellationToken = default)
    {
        if (items.Length == 0)
            return new BulkResult<RateDto>(0, 0, []);

        var constructionTypeId = await ResolveOpenPlotConstructionTypeIdAsync(cancellationToken);

        var rateCreateDtos = items
            .Select(item => MapToCreateRateDto(item, constructionTypeId))
            .ToArray();

        return await BulkCreateAsync(rateCreateDtos, cancellationToken);
    }

    private async Task<int> ResolveGroundFloorIdAsync(CancellationToken cancellationToken)
    {
        var floorId = await _floorRepository.GetQueryable()
            .Where(f => f.FloorCode == "G" && f.IsActive)
            .OrderBy(f => f.Id)
            .Select(f => f.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (floorId == 0)
        {
            throw new ValidationException(
                "Ground floor (code 'G') is not configured. Please add an active floor with code 'G' before creating rates with FloorId = 0.",
                OperationType.Create);
        }

        return floorId;
    }

    private async Task<int> ResolveOpenPlotConstructionTypeIdAsync(CancellationToken cancellationToken)
    {
        var constructionTypeId = await _constructionTypeRepository.GetQueryable()
            .Where(c => c.ConstructionCode == ConstructionTypeConstants.OpenPlot && c.IsActive)
            .OrderBy(c => c.Id)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (constructionTypeId == 0)
        {
            throw new ValidationException(
                $"Construction type for open plot (code '{ConstructionTypeConstants.OpenPlot}') is not configured. Please add an active construction type with code '{ConstructionTypeConstants.OpenPlot}'.",
                OperationType.Create);
        }

        return constructionTypeId;
    }

    private static CreateRateDto MapToCreateRateDto(CreateOpenPlotRateDto source, int constructionTypeId)
    {
        return new CreateRateDto
        {
            TaxZoneId = source.TaxZoneId,
            FloorId = source.FloorId,
            ConstructionTypeId = constructionTypeId,
            TypeOfUseGroupId = source.TypeOfUseGroupId,
            YearRangeRVId = source.YearRangeRVId,
            RateSquareMeter = source.RateSquareMeter,
            RateSquareFeet = source.RateSquareFeet,
            RateSectionId = source.RateSectionId,
            RateRemark = source.RateRemark,
            IsActive = source.IsActive,
            CreatedBy = source.CreatedBy
        };
    }
}
