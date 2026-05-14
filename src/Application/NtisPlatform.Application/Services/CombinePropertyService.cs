using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class CombinePropertyService : BaseCommonCrudService<PropertyEntity, CombinePropertyDto, CreateCombinePropertyDto, UpdateCombinePropertyDto, CombinePropertyQueryParameters, int>, ICombinePropertyService
{
    private readonly IRepository<WardEntity, int> _wardRepository;
    private readonly IRepository<TransMastEntity> _transMastRepository;
    private readonly IRepository<TaxPendingDetailsEntity> _taxPendingRepository;
    private readonly IRepository<CombinePropertyHistoryEntity> _combineHistoryRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyMastOldRepository;
    private readonly ICombinePropertyValidator _validator;
    private readonly IPropertyDataCopier _dataCopier;
    private readonly IPropertyDeactivator _deactivator;
    private readonly ILogger<CombinePropertyService> _logger;

    public CombinePropertyService(
         IRepository<PropertyEntity, int> repository,
        IRepository<WardEntity, int> wardRepository,
         IRepository<TransMastEntity> transMastRepository,
        IRepository<TaxPendingDetailsEntity> taxPendingRepository,
        IRepository<CombinePropertyHistoryEntity> combineHistoryRepository,
         IRepository<PropertyMastOldEntity, int> propertyMastOldRepository,
        ICombinePropertyValidator validator,
        IPropertyDataCopier dataCopier,
        IPropertyDeactivator deactivator,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CombinePropertyService> logger) : base(repository, unitOfWork, mapper)
    {
        _wardRepository = wardRepository;
        _transMastRepository = transMastRepository;
        _taxPendingRepository = taxPendingRepository;
        _combineHistoryRepository = combineHistoryRepository;
        _propertyMastOldRepository = propertyMastOldRepository;
        _validator = validator;
        _dataCopier = dataCopier;
        _deactivator = deactivator;
        _logger = logger;
    }
    
    public async Task<List<PropertyCombineDetailsDto>> GetPropertyCombineDetailsAsync(
        PropertyCombineDetailsQueryParameters queryParams,
        CancellationToken cancellationToken = default)
    {
        if (!queryParams.WardId.HasValue ||
            string.IsNullOrWhiteSpace(queryParams.PropertyNo) ||
            string.IsNullOrWhiteSpace(queryParams.PartitionNo))
        {
            return [];
        }

        var partitionNumbers = FilterExpressionBuilder.Csv(queryParams.PartitionNo);
        if (partitionNumbers.Count == 0)
        {
            return [];
        }

        _logger.LogDebug("Fetching property details for WardId={WardId}, PropertyNo={PropertyNo}, Partitions={Partitions}",
       queryParams.WardId, queryParams.PropertyNo, string.Join(",", partitionNumbers));

        var ward = await _wardRepository.GetByIdAsync(queryParams.WardId.Value, cancellationToken);
        var wardNo = ward?.WardNo ?? string.Empty;

        var query = from pm in _repository.GetQueryable()
                    join pmo in _propertyMastOldRepository.GetQueryable()
                        on pm.PropertyMastOldId equals pmo.Id into pmoJoin
                    from pmo in pmoJoin.DefaultIfEmpty()
                    where pm.WardId == queryParams.WardId.Value &&
                          pm.PropertyNo == queryParams.PropertyNo &&
                          pm.PartitionNo != null &&
                          partitionNumbers.Contains(pm.PartitionNo) &&
                           pm.IsActive == true
                    select new
                    {
                        Property = pm,
                        OldPropertyNo = pmo != null && pmo.IsActive == true && pmo.MarkedForDeletion != true ? pmo.OldPropertyNo : null
                    };

        var propertiesData = await query.ToListAsync(cancellationToken);

        if (propertiesData.Count == 0)
        {
            return [];
        }

        var propertyIds = propertiesData.Select(p => p.Property.Id).Distinct().ToList();
        var taxData = await GetTaxDataAsync(propertyIds, cancellationToken);

        return propertiesData.Select(x =>
        {
            var taxInfo = taxData.TryGetValue(x.Property.Id, out var info) ? info : (TaxAmount: 0m, PendingAmount: 0m);

            return new PropertyCombineDetailsDto
            {
                PropertyId = x.Property.Id,
                WardId = x.Property.WardId,
                WardNo = wardNo,
                PropertyNo = x.Property.PropertyNo,
                PartitionNo = x.Property.PartitionNo,
                OldPropertyNo = x.OldPropertyNo ?? string.Empty,
                OwnerName = x.Property.OwnerName ?? string.Empty,
                OccupierName = x.Property.OccupierName ?? string.Empty,
                TaxAmount = taxInfo.TaxAmount,
                PendingAmount = taxInfo.PendingAmount
            };
        }).ToList();
    }

    /// <summary>
    ///  /// Get tax and pending amounts for properties using existing generic repositories.
    /// Aggregates data from TransMast and TaxPendingDetails tables.
    /// </summary>
    private async Task<Dictionary<int, (decimal TaxAmount, decimal PendingAmount)>> GetTaxDataAsync(
       List<int> propertyIds,
       CancellationToken cancellationToken)
    {
        if (propertyIds.Count == 0)
        {
            return [];
        }

        var taxAmounts = await _transMastRepository.GetQueryable()
             .Where(tm => propertyIds.Contains(tm.PropertyId) && tm.IsActive == true)
            .GroupBy(tm => tm.PropertyId)
            .Select(g => new { PropertyId = g.Key, TaxAmount = g.Sum(tm => tm.TaxAmount) })
            .ToListAsync(cancellationToken);

        var pendingAmounts = await _taxPendingRepository.GetQueryable()
            .Where(tpd => propertyIds.Contains(tpd.PropertyId) && tpd.IsActive == true)
            .GroupBy(tpd => tpd.PropertyId)
            .Select(g => new { PropertyId = g.Key, PendingAmount = g.Sum(tpd => tpd.PendingAmount ?? 0) })
            .ToListAsync(cancellationToken);

        var taxLookup = taxAmounts.ToDictionary(x => x.PropertyId, x => x.TaxAmount);
        var pendingLookup = pendingAmounts.ToDictionary(x => x.PropertyId, x => x.PendingAmount);

        return propertyIds.ToDictionary(
            id => id,
            id => (
                TaxAmount: taxLookup.TryGetValue(id, out var tax) ? tax : 0m,
                PendingAmount: pendingLookup.TryGetValue(id, out var pending) ? pending : 0m
            ));
    }


    public override async Task<PagedResult<CombinePropertyDto>> GetAllAsync(
     CombinePropertyQueryParameters queryParams,
     CancellationToken cancellationToken = default)
    {
        // Start with IQueryable - no materialization yet
        var query = _repository.GetQueryable()
            .Where(x => x.PropertyNo != null && x.IsActive == true);

        // Apply filters in SQL
        query = ApplyFilters(query, queryParams);

        // Filter out null/empty PartitionNo before grouping
        query = query.Where(x => !string.IsNullOrWhiteSpace(x.PartitionNo));

        // Group by the de-duplication key (remove Id so grouping actually deduplicates rows)
        // Surface a representative PropertyId using Max(Id) (assumes higher Id is the preferred record)
        var groupedQuery = query
            .GroupBy(x => new { x.WardId, x.PropertyNo, x.PartitionNo })
            .Select(g => new
            {
                PropertyId = g.Max(p => p.Id),
                g.Key.WardId,
                g.Key.PropertyNo,
                g.Key.PartitionNo
            });

        // Get total count before paging (executes COUNT query in SQL)
        var totalCount = await groupedQuery.CountAsync(cancellationToken);

        // Apply sorting in SQL
        var sortedQuery = ApplySorting(groupedQuery, queryParams);

        // Handle unpaged (PageSize == -1) vs paged results
        var isUnpaged = queryParams.PageSize == -1;
        var effectivePageSize = isUnpaged ? (totalCount > 0 ? totalCount : 1) : queryParams.PageSize;
        var effectivePageNumber = isUnpaged ? 1 : queryParams.PageNumber;

        // Apply paging in SQL (OFFSET/FETCH) only if paged
        var pagedData = isUnpaged
            ? await sortedQuery.ToListAsync(cancellationToken)
            : await sortedQuery
                .Skip((effectivePageNumber - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToListAsync(cancellationToken);

        // Load ward lookup data
        var wardIds = pagedData.Select(x => x.WardId).Distinct().ToList();
        var wards = await _wardRepository.GetQueryable()
            .Where(w => wardIds.Contains(w.Id) && w.IsActive)
            .Select(w => new { w.Id, w.WardNo })
            .ToListAsync(cancellationToken);

        var wardLookup = wards.ToDictionary(w => w.Id, w => w.WardNo);

        // Map to DTOs
        var dtos = pagedData.Select(x => new CombinePropertyDto
        {
            Id = x.PropertyId,
            WardId = x.WardId,
            WardNo = wardLookup.TryGetValue(x.WardId, out var wn) ? wn : null,
            PropertyNo = x.PropertyNo,
            FromProperty = x.PartitionNo ?? string.Empty,
            ToProperty = x.PartitionNo ?? string.Empty
        });

        return new PagedResult<CombinePropertyDto>(dtos, totalCount, effectivePageNumber, effectivePageSize);
    }

    /// <summary>
    /// Apply sorting to the grouped query
    /// </summary>
    private static IQueryable<T> ApplySorting<T>(IQueryable<T> query, CombinePropertyQueryParameters queryParams)
        where T : class
    {
        var desc = string.Equals(queryParams.SortOrder, "DESC", StringComparison.OrdinalIgnoreCase);

        return (queryParams.SortBy?.ToLowerInvariant()) switch
        {
            "wardid" => desc
                ? query.OrderByDescending(x => EF.Property<int>(x, "WardId"))
                : query.OrderBy(x => EF.Property<int>(x, "WardId")),
            "propertyno" => desc
                ? query.OrderByDescending(x => EF.Property<string>(x, "PropertyNo"))
                : query.OrderBy(x => EF.Property<string>(x, "PropertyNo")),
            "partitionno" => desc
                ? query.OrderByDescending(x => EF.Property<string>(x, "PartitionNo"))
                : query.OrderBy(x => EF.Property<string>(x, "PartitionNo")),
            _ => desc
                ? query.OrderByDescending(x => EF.Property<int>(x, "WardId"))
                       .ThenByDescending(x => EF.Property<string>(x, "PropertyNo"))
                       .ThenByDescending(x => EF.Property<string>(x, "PartitionNo"))
                : query.OrderBy(x => EF.Property<int>(x, "WardId"))
                       .ThenBy(x => EF.Property<string>(x, "PropertyNo"))
                       .ThenBy(x => EF.Property<string>(x, "PartitionNo"))
        };
    }
    /// <inheritdoc />
    public async Task<CombinePropertiesResponseDto> CombinePropertiesAsync(
        CombinePropertiesRequestDto request,
        CancellationToken cancellationToken = default)
    {

        var combinePropertyIds = ParsePropertyIds(request.CombinePropertyIds);
        // Remove MainPropertyId from the list to prevent self-combination
        combinePropertyIds = combinePropertyIds
            .Where(id => id != request.MainPropertyId)
            .Distinct()
            .ToList();

        if (combinePropertyIds.Count == 0)
        {
            _logger.LogWarning("No valid property IDs provided for combination");
            return new CombinePropertiesResponseDto
            {
                Success = false,
                Message = "No valid property IDs provided to combine",
                MainPropertyId = request.MainPropertyId
            };
        }

        // Defensive check: Verify main property exists before validation
        var mainProperty = await _repository.GetByIdAsync(request.MainPropertyId, cancellationToken);
        if (mainProperty == null)
        {
            _logger.LogWarning("MainPropertyId {MainPropertyId} not found", request.MainPropertyId);
            return new CombinePropertiesResponseDto
            {
                Success = false,
                Message = "MainPropertyId not found.",
                MainPropertyId = request.MainPropertyId
            };
        }

        // Validate using validator service
        var (isValid, errorMessage, _) = await _validator.ValidatePropertiesForCombinationAsync(
            request.MainPropertyId,
            combinePropertyIds,
            cancellationToken);

        if (!isValid)
        {
            return new CombinePropertiesResponseDto
            {
                Success = false,
                Message = errorMessage,
                MainPropertyId = request.MainPropertyId
            };
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Copy property data using data copier service
            await _dataCopier.CopyPropertyDataAsync(
                request.MainPropertyId,
                combinePropertyIds,
                request.CreatedBy,
                cancellationToken);

            // Deactivate combined properties using deactivator service
            await _deactivator.DeactivateCombinedPropertiesAsync(combinePropertyIds, cancellationToken);

            // Ensure main property records are active
            await _deactivator.EnsureMainPropertyRecordsActiveAsync(request.MainPropertyId, cancellationToken);

            // Set IsCombineProperty flags
            await UpdateMainPropertyCombineFlagAsync(request.MainPropertyId, combinePropertyIds, cancellationToken);

            // Insert history records
            await InsertCombineHistoryAsync(request.MainPropertyId, combinePropertyIds, request.Remark, request.CreatedBy, cancellationToken);

            // Single consolidated SaveChanges - persists all pending changes before commit
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);


            return new CombinePropertiesResponseDto
            {
                Success = true,
                MainPropertyId = request.MainPropertyId,
                CombinedPropertyIds = combinePropertyIds,
                Message = "Properties combined successfully."
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to combine properties for MainPropertyId={MainPropertyId}", request.MainPropertyId);
            throw;
        }
    }

    private async Task UpdateMainPropertyCombineFlagAsync(int mainPropertyId, List<int> combinePropertyIds, CancellationToken cancellationToken)
    {
        var mainPropertyCount = await _repository.GetQueryable()
          .Where(p => p.Id == mainPropertyId)
          .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsCombineProperty, true), cancellationToken);


        var combinedPropertiesCount = await _repository.GetQueryable()
            .Where(p => combinePropertyIds.Contains(p.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsCombineProperty, true), cancellationToken);


        if (combinedPropertiesCount != combinePropertyIds.Count)
        {
            _logger.LogWarning("Mismatch in IsCombineProperty flag update: expected {ExpectedCount}, affected {ActualCount}",
                combinePropertyIds.Count, combinedPropertiesCount);
        }
    }

    private async Task InsertCombineHistoryAsync(
       int mainPropertyId,
       List<int> combinePropertyIds,
       string? remark,
       int? createdBy,
       CancellationToken cancellationToken)
    {
        var historyRecords = new List<CombinePropertyHistoryEntity>();

        foreach (var targetPropertyId in combinePropertyIds)
        {
            var historyRecord = new CombinePropertyHistoryEntity
            {
                MainPropertyId = mainPropertyId,
                TargetPropertyId = targetPropertyId,
                Remark = remark,
                IsActive = true,
                CreatedBy = createdBy
            };

            historyRecords.Add(historyRecord);
        }

        if (historyRecords.Count > 0)
        {
            await _combineHistoryRepository.AddRangeAsync(historyRecords, cancellationToken);
            // Note: SaveChanges removed - will be called once before transaction commit
        }
    }

    private static List<int> ParsePropertyIds(string propertyIds)
    {
        return propertyIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => int.TryParse(id.Trim(), out var result) ? result : 0)
            .Where(id => id > 0)
            .ToList();
    }

    private static IQueryable<PropertyEntity> ApplyFilters(IQueryable<PropertyEntity> query, CombinePropertyQueryParameters queryParams)
    {
        if (queryParams.WardId.HasValue)
            query = query.Where(x => x.WardId == queryParams.WardId);

        if (!string.IsNullOrWhiteSpace(queryParams.PropertyNo))
            query = query.Where(x => x.PropertyNo != null && x.PropertyNo.Contains(queryParams.PropertyNo));

        if (!string.IsNullOrWhiteSpace(queryParams.PartitionNo))
            query = query.Where(x => x.PartitionNo != null && x.PartitionNo.Contains(queryParams.PartitionNo));

        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
        {
            var term = queryParams.SearchTerm.Trim();
            query = query.Where(x =>
                (x.PropertyNo != null && x.PropertyNo.Contains(term)) ||
                (x.PartitionNo != null && x.PartitionNo.Contains(term)));
        }

        return query;
    }
}


