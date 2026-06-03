using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class CombinePropertyService : BaseCommonCrudService<PropertyEntity, CombinePropertyDto, CreateCombinePropertyDto, UpdateCombinePropertyDto, CombinePropertyQueryParameters, int>, ICombinePropertyService
{
    private readonly IRepository<WardEntity, int> _wardRepository;
    private readonly IRepository<TransMastEntity> _transMastRepository;
    private readonly IRepository<TaxPendingDetailsEntity> _taxPendingRepository;
    private readonly IRepository<CombinePropertyHistoryEntity> _combineHistoryRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyMastOldRepository;
    private readonly IRepository<PropertyTypeMasterEntity, int> _propertyTypeMasterRepository;
    private readonly IRepository<PropertyCategoryEntity, int> _categoryRepository;
    private readonly ICombinePropertyValidator _validator;
    private readonly IPropertyDataCopier _dataCopier;
    private readonly IPropertyDeactivator _deactivator;
    private readonly ICombinePropertyTaxService _taxService;
    private readonly ILogger<CombinePropertyService> _logger;

    public CombinePropertyService(
         IRepository<PropertyEntity, int> repository,
        IRepository<WardEntity, int> wardRepository,
         IRepository<TransMastEntity> transMastRepository,
        IRepository<TaxPendingDetailsEntity> taxPendingRepository,
        IRepository<CombinePropertyHistoryEntity> combineHistoryRepository,
         IRepository<PropertyMastOldEntity, int> propertyMastOldRepository,
         IRepository<PropertyTypeMasterEntity, int> propertyTypeMasterRepository,
        IRepository<PropertyCategoryEntity, int> categoryRepository,
        ICombinePropertyValidator validator,
        IPropertyDataCopier dataCopier,
        IPropertyDeactivator deactivator,
        ICombinePropertyTaxService taxService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CombinePropertyService> logger) : base(repository, unitOfWork, mapper)
    {
        _wardRepository = wardRepository;
        _transMastRepository = transMastRepository;
        _taxPendingRepository = taxPendingRepository;
        _combineHistoryRepository = combineHistoryRepository;
        _propertyMastOldRepository = propertyMastOldRepository;
        _propertyTypeMasterRepository = propertyTypeMasterRepository;
        _categoryRepository = categoryRepository;
        _validator = validator;
        _dataCopier = dataCopier;
        _deactivator = deactivator;
        _taxService = taxService;
        _logger = logger;
    }

    public async Task<List<PropertyCombineDetailsDto>> GetPropertyCombineDetailsAsync(
        PropertyCombineDetailsQueryParameters queryParams,
        CancellationToken cancellationToken = default)
    {
        if (!queryParams.WardId.HasValue)
        {
            return [];
        }

        // Parse comma-separated PropertyNo values
        var propertyNumbers = string.IsNullOrWhiteSpace(queryParams.PropertyNo)
            ? []
            : FilterExpressionBuilder.Csv(queryParams.PropertyNo);

        // Parse comma-separated PartitionNo values
        // Special handling: "0" represents empty/blank partition numbers (main property with no partition)
        // Note: Empty string "" is different from null - empty string means explicit empty filter
        var partitionNumbers = queryParams.PartitionNo == null
            ? []
            : FilterExpressionBuilder.Csv(queryParams.PartitionNo);

        // Check if "0" is present in the partition filter (represents empty partitions / main property)
        var includeEmptyPartitions = partitionNumbers.Contains("0");

        // Remove "0" from the list as it's a special placeholder, not an actual partition value
        if (includeEmptyPartitions)
        {
            partitionNumbers = partitionNumbers.Where(p => p != "0").ToList();
        }

        // If partition filter was provided (not null) but results in empty list and no "0" flag, return empty
        // This handles cases like PartitionNo="" or PartitionNo="   ,  ,  " (whitespace only)
        if (queryParams.PartitionNo != null && partitionNumbers.Count == 0 && !includeEmptyPartitions)
        {
            return [];
        }

        _logger.LogDebug("Fetching property details for WardId={WardId}, PropertyNo={PropertyNo}, Partitions={Partitions}, IncludeEmpty={IncludeEmpty}",
            queryParams.WardId,
            propertyNumbers.Count > 0 ? string.Join(",", propertyNumbers) : "ALL",
            partitionNumbers.Count > 0 ? string.Join(",", partitionNumbers) : (includeEmptyPartitions ? "EMPTY_ONLY" : "ALL"),
            includeEmptyPartitions);

        var ward = await _wardRepository.GetByIdAsync(queryParams.WardId.Value, cancellationToken);
        var wardNo = ward?.WardNo ?? string.Empty;

        // Determine if partition filter is active
        var hasPartitionFilter = partitionNumbers.Count > 0 || includeEmptyPartitions;

        var query = from pm in _repository.GetQueryable()
                    join pmo in _propertyMastOldRepository.GetQueryable()
                        on pm.PropertyMastOldId equals pmo.Id into pmoJoin
                    from pmo in pmoJoin.DefaultIfEmpty()
                    join ptm in _propertyTypeMasterRepository.GetQueryable()
                        on pm.PropertyTypeId equals (int?)ptm.Id into ptmJoin
                    from ptm in ptmJoin.DefaultIfEmpty()
                    where pm.WardId == queryParams.WardId.Value &&
                          pm.IsActive == true &&
                          // PropertyNo filter is optional, supports comma-separated values
                          (propertyNumbers.Count == 0 || (pm.PropertyNo != null && propertyNumbers.Contains(pm.PropertyNo!))) &&
                          // PartitionNo filter logic - EXACT MATCH on specified partitions:
                          // - If no partition filter: return all properties
                          // - If partitions specified (e.g., "A1,A2,A3"): return ONLY those exact partitions
                          // - If "0" included (e.g., "0,A1,A2"): also include properties with empty/null partition (main property)
                          (
                              // No partition filter - return all properties
                              !hasPartitionFilter ||
                              // Exact match on specified partition numbers (e.g., A1, A2, A3)
                              (partitionNumbers.Count > 0 && pm.PartitionNo != null && partitionNumbers.Contains(pm.PartitionNo!)) ||
                              // Include empty/null partitions when "0" is specified (main property)
                              (includeEmptyPartitions && string.IsNullOrWhiteSpace(pm.PartitionNo))
                          )
                    select new
                    {
                        Property = pm,
                        OldPropertyNo = pmo != null && pmo.IsActive == true && pmo.MarkedForDeletion != true ? pmo.OldPropertyNo : null,
                        PropertyTypeId = ptm != null && ptm.IsActive == true ? (int?)ptm.Id : null,
                        PropertyDescription = ptm != null && ptm.IsActive == true ? ptm.PropertyDescription : null
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
                CategoryId = x.Property.CategoryId,
                PropertyTypeId = x.PropertyTypeId,
                PropertyDescription = x.PropertyDescription ?? string.Empty,
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
        query = await ApplyFiltersAsync(query, queryParams, cancellationToken);

        // Group by the de-duplication key (remove Id so grouping actually deduplicates rows)
        // Surface a representative PropertyId using Max(Id) (assumes higher Id is the preferred record)
        var groupedQuery = query
            .GroupBy(x => new { x.WardId, x.PropertyNo, x.PartitionNo })
            .Select(g => new
            {
                PropertyId = g.Max(p => p.Id),
                g.Key.WardId,
                g.Key.PropertyNo,
                g.Key.PartitionNo,
                CategoryId = g.FirstOrDefault()!.CategoryId,
                SocietyDetailId = g.FirstOrDefault()!.SocietyDetailId
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
            ToProperty = x.PartitionNo ?? string.Empty,
            CategoryId = x.CategoryId,
            SocietyDetailId = x.SocietyDetailId
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

        var combinePropertyIds = ParsePropertyIds(request.CombinedPropertyIds);
        // Remove SourcePropertyId from the list to prevent self-combination
        combinePropertyIds = combinePropertyIds
            .Where(id => id != request.SourcePropertyId)
            .Distinct()
            .ToList();

        if (combinePropertyIds.Count == 0)
        {
            _logger.LogWarning("No valid property IDs provided for combination");
            return new CombinePropertiesResponseDto
            {
                Success = false,
                Message = "No valid property IDs provided to combine",
                SourcePropertyId = request.SourcePropertyId
            };
        }

        // Prevent duplicate combine entries: CombinedPropertyId should not already exist in active history
        var duplicateCombinedPropertyIds = await _combineHistoryRepository.GetQueryable()
            .Where(h => h.IsActive && combinePropertyIds.Contains(h.CombinedPropertyId))
            .Select(h => h.CombinedPropertyId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (duplicateCombinedPropertyIds.Count > 0)
        {
            _logger.LogWarning("Duplicate combine request detected for active CombinedPropertyIds: {CombinedPropertyIds}",
                string.Join(",", duplicateCombinedPropertyIds));

            return new CombinePropertiesResponseDto
            {
                Success = false,
                Message = $"Properties already combined: {string.Join(", ", duplicateCombinedPropertyIds)}",
                SourcePropertyId = request.SourcePropertyId
            };
        }

        // Defensive check: Verify source property exists before validation
        var sourceProperty = await _repository.GetByIdAsync(request.SourcePropertyId, cancellationToken);
        if (sourceProperty == null)
        {
            _logger.LogWarning("SourcePropertyId {SourcePropertyId} not found", request.SourcePropertyId);
            return new CombinePropertiesResponseDto
            {
                Success = false,
                Message = "SourcePropertyId not found.",
                SourcePropertyId = request.SourcePropertyId
            };
        }

        // Validate using validator service
        var (isValid, errorMessage, _) = await _validator.ValidatePropertiesForCombinationAsync(
            request.SourcePropertyId,
            combinePropertyIds,
            request.OverrideOwnerNameMismatch,
            cancellationToken);

        if (!isValid)
        {
            return new CombinePropertiesResponseDto
            {
                Success = false,
                Message = errorMessage ?? string.Empty,
                SourcePropertyId = request.SourcePropertyId
            };
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Step 1: Copy property data using data copier service
            // Pass OverrideOwnerNameMismatch flag to merge owner names when they are different
            // Pass PropertyTypeId to update on main property
            await _dataCopier.CopyPropertyDataAsync(
                request.SourcePropertyId,
                combinePropertyIds,
                request.CreatedBy,
                request.OverrideOwnerNameMismatch,
                request.PropertyTypeId,
                cancellationToken);

            // Step 2: Deactivate combined properties using deactivator service
            await _deactivator.DeactivateCombinedPropertiesAsync(combinePropertyIds, cancellationToken);

            // Step 3: Ensure source property records are active
            await _deactivator.EnsureMainPropertyRecordsActiveAsync(request.SourcePropertyId, cancellationToken);

            // Step 4: Insert history records
            await InsertCombineHistoryAsync(request.SourcePropertyId, combinePropertyIds, request.CombineReason, request.CreatedBy, cancellationToken);

            // Step 5: Process tax handling for combine property
            // - Aggregate pending taxes from combined properties (year-wise, tax-wise)
            // - Recalculate current year RV tax
            // - Check if bill is distributed (placeholder for future TransMast sync)
            var taxProcessingSucceeded = await _taxService.ProcessCombinePropertyTaxesAsync(
                request.SourcePropertyId,
                combinePropertyIds,
                request.CreatedBy,
                cancellationToken);
            if (!taxProcessingSucceeded)
            {
                _logger.LogWarning("Combine-property tax processing reported failure for SourcePropertyId={SourcePropertyId}", request.SourcePropertyId);
            }

            // Single consolidated SaveChanges - persists all pending changes before commit
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);


            return new CombinePropertiesResponseDto
            {
                Success = true,
                SourcePropertyId = request.SourcePropertyId,
                CombinedPropertyIds = combinePropertyIds,
                Message = "Properties combined successfully."
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to combine properties for SourcePropertyId={SourcePropertyId}", request.SourcePropertyId);
            throw;
        }
    }

    private async Task InsertCombineHistoryAsync(
       int sourcePropertyId,
       List<int> combinedPropertyIds,
       string combineReason,
       int? createdBy,
       CancellationToken cancellationToken)
    {
        var historyRecords = new List<CombinePropertyHistoryEntity>();
        foreach (var combinedPropertyId in combinedPropertyIds)
        {
            var historyRecord = new CombinePropertyHistoryEntity
            {
                SourcePropertyId = sourcePropertyId,
                CombinedPropertyId = combinedPropertyId,
                CombineReason = combineReason,
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

    /// <summary>
    /// Get combined property history.
    /// - If SourcePropertyId is NOT provided: Returns list of distinct source properties that have combined history (with CombineReason from first history record).
    /// - If SourcePropertyId IS provided: Returns ONLY the combined properties for that source (excludes the source property itself).
    /// </summary>
    public async Task<List<CombinePropertyHistoryDto>> GetCombinePropertyHistoryAsync(
        int? sourcePropertyId,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Get history data from combine history table
        var historyQuery = _combineHistoryRepository.GetQueryable()
            .Where(h => h.IsActive);

        // Apply SourcePropertyId filter only if provided
        if (sourcePropertyId.HasValue)
        {
            historyQuery = historyQuery.Where(h => h.SourcePropertyId == sourcePropertyId.Value);
        }

        var historyData = await historyQuery
            .Select(h => new { h.SourcePropertyId, h.CombinedPropertyId, h.CombineReason })
            .ToListAsync(cancellationToken);

        // If no history data found, return empty list
        if (historyData.Count == 0)
        {
            return [];
        }

        // Step 2: Determine which property IDs to fetch based on the scenario
        List<int> propertyIdsToFetch;
        Dictionary<int, string?> combineReasonLookup;

        if (sourcePropertyId.HasValue)
        {
            // Scenario: SourcePropertyId IS provided
            // Return ONLY the CombinedPropertyIds (exclude the source property)
            propertyIdsToFetch = historyData
                .Select(h => h.CombinedPropertyId)
                .Distinct()
                .ToList();

            // Create lookup for CombineReason by CombinedPropertyId
            combineReasonLookup = historyData
                .GroupBy(h => h.CombinedPropertyId)
                .ToDictionary(g => g.Key, g => g.First().CombineReason);
        }
        else
        {
            // Scenario: SourcePropertyId is NOT provided
            // Return ONLY the distinct SourcePropertyIds (list of properties that have combined history)
            propertyIdsToFetch = historyData
                .Select(h => h.SourcePropertyId)
                .Distinct()
                .ToList();

            // For source properties, get the CombineReason from the first history record for each source
            // This shows why properties were combined into this source
            combineReasonLookup = historyData
                .GroupBy(h => h.SourcePropertyId)
                .ToDictionary(g => g.Key, g => g.First().CombineReason);
        }

        // If no property IDs to fetch, return empty list
        if (propertyIdsToFetch.Count == 0)
        {
            return [];
        }

        // Step 3: Get property details for the selected properties
        var propertiesQuery = from pm in _repository.GetQueryable()
                              join pmo in _propertyMastOldRepository.GetQueryable()
                                  on pm.PropertyMastOldId equals pmo.Id into pmoJoin
                              from pmo in pmoJoin.DefaultIfEmpty()
                              join ptm in _propertyTypeMasterRepository.GetQueryable()
                                  on pm.PropertyTypeId equals (int?)ptm.Id into ptmJoin
                              from ptm in ptmJoin.DefaultIfEmpty()
                              where propertyIdsToFetch.Contains(pm.Id)
                              select new
                              {
                                  Property = pm,
                                  OldPropertyNo = pmo != null && pmo.IsActive == true && pmo.MarkedForDeletion != true ? pmo.OldPropertyNo : null,
                                  PropertyTypeId = ptm != null && ptm.IsActive == true ? (int?)ptm.Id : null,
                                  PropertyDescription = ptm != null && ptm.IsActive == true ? ptm.PropertyDescription : null
                              };

        var propertiesData = await propertiesQuery.ToListAsync(cancellationToken);

        // Step 4: Get ward information
        var wardIds = propertiesData.Select(p => p.Property.WardId).Distinct().ToList();
        var wards = await _wardRepository.GetQueryable()
            .Where(w => wardIds.Contains(w.Id) && w.IsActive)
            .Select(w => new { w.Id, w.WardNo })
            .ToListAsync(cancellationToken);

        var wardLookup = wards.ToDictionary(w => w.Id, w => w.WardNo);

        // Step 5: Get tax data for all properties
        var propertyIds = propertiesData.Select(p => p.Property.Id).Distinct().ToList();
        var taxData = await GetTaxDataAsync(propertyIds, cancellationToken);

        // Step 6: Map to DTOs
        var result = propertiesData
            .OrderBy(x => x.Property.Id)
            .Select(x =>
            {
                var taxInfo = taxData.TryGetValue(x.Property.Id, out var info) ? info : (TaxAmount: 0m, PendingAmount: 0m);
                
                // Get CombineReason - now available for both source and combined properties
                var combineReason = combineReasonLookup.TryGetValue(x.Property.Id, out var reason) ? reason : null;

                return new CombinePropertyHistoryDto
                {
                    PropertyId = x.Property.Id,
                    WardId = x.Property.WardId,
                    WardNo = wardLookup.TryGetValue(x.Property.WardId, out var wardNo) ? wardNo : null,
                    PropertyNo = x.Property.PropertyNo,
                    PartitionNo = x.Property.PartitionNo,
                    OldPropertyNo = x.OldPropertyNo ?? string.Empty,
                    OwnerName = x.Property.OwnerName ?? string.Empty,
                    OccupierName = x.Property.OccupierName ?? string.Empty,
                    CategoryId = x.Property.CategoryId,
                    PropertyTypeId = x.PropertyTypeId,
                    PropertyDescription = x.PropertyDescription ?? string.Empty,
                    TaxAmount = taxInfo.TaxAmount,
                    PendingAmount = taxInfo.PendingAmount,
                    CombineReason = combineReason
                };
            }).ToList();

        return result;
    }

    private async Task<IQueryable<PropertyEntity>> ApplyFiltersAsync(
        IQueryable<PropertyEntity> query, 
        CombinePropertyQueryParameters queryParams,
        CancellationToken cancellationToken)
    {
        // Track if we're dealing with standalone properties (standalone apartments or non-apartments)
        // For these, we only filter by WardId and skip additional filters
        bool isStandaloneProperty = false;

        // Track if we're dealing with multi-unit apartments
        // For multi-unit apartments, we filter by WardId, PropertyNo, SocietyDetailId, CategoryId
        // but do NOT filter by PartitionNo so all properties from that wing are returned
        bool isMultiUnitApartment = false;

        // Check if category is apartment-related (Apartment or Multi Commercial Apartment)
        bool isApartmentCategory = false;
        if (queryParams.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(queryParams.CategoryId.Value, cancellationToken);
            if (category != null && !string.IsNullOrEmpty(category.PropertyCategoryName))
            {
                // Check if category name contains "Apartment" (case-insensitive)
                isApartmentCategory = category.PropertyCategoryName.Contains(
                    CapitalValueConstants.PropertyCategory.ApartmentKeyword, 
                    StringComparison.OrdinalIgnoreCase);
            }

            // Filter by CategoryId - only return properties matching the selected category
            query = query.Where(x => x.CategoryId == queryParams.CategoryId);
        }

        // Conditional filtering based on category
        if (queryParams.CategoryId.HasValue)
        {
            if (isApartmentCategory)
            {
                // For apartments: Check if this is a multi-unit apartment with wings
                // Multi-unit apartments have multiple properties with:
                // - Same PropertyNo
                // - Same SocietyDetailId (indicates they belong to the same wing/building)
                // - Different PartitionNo values
                //
                // Standalone apartments may have PartitionNo but either:
                // - No SocietyDetailId (NULL)
                // - Different SocietyDetailIds (independent units)
                bool hasWingsForProperty = false;

                if (queryParams.WardId.HasValue && !string.IsNullOrWhiteSpace(queryParams.PropertyNo))
                {
                    // Check if there are OTHER properties with same PropertyNo AND same SocietyDetailId
                    // This indicates a true multi-unit apartment building with wings
                    hasWingsForProperty = await _repository.GetQueryable()
                        .Where(x => x.CategoryId == queryParams.CategoryId &&
                                    x.WardId == queryParams.WardId &&
                                    x.PropertyNo != null && x.PropertyNo.Contains(queryParams.PropertyNo!) &&
                                    x.IsActive == true &&
                                    !string.IsNullOrWhiteSpace(x.PartitionNo) &&
                                    x.SocietyDetailId.HasValue) // Must have SocietyDetailId to be considered a wing
                        .GroupBy(x => new { x.PropertyNo, x.SocietyDetailId })
                        .AnyAsync(g => g.Count() > 1, cancellationToken); // Multiple properties with same PropertyNo+SocietyDetailId
                }

                if (hasWingsForProperty)
                {
                    // Multi-unit apartment: Filter by WardId AND PropertyNo, exclude main property
                    query = query.Where(x => x.WardId == queryParams.WardId);
                    query = query.Where(x => x.PropertyNo != null && x.PropertyNo.Contains(queryParams.PropertyNo!));

                    // Exclude main property (no partition) - only show wing properties
                    query = query.Where(x => !string.IsNullOrWhiteSpace(x.PartitionNo));

                    // Filter by SocietyDetailId (wing) for apartments to show only combinable properties
                    if (queryParams.SocietyDetailId.HasValue)
                        query = query.Where(x => x.SocietyDetailId == queryParams.SocietyDetailId);

                    // Mark as multi-unit apartment - skip PartitionNo filter to return all properties from the wing
                    isMultiUnitApartment = true;
                    isStandaloneProperty = false;
                }
                else
                {
                    // Standalone apartment (no partitions): Filter by WardId only
                    // PropertyNo is NOT used for standalone apartments - user selects from all properties in ward
                    if (queryParams.WardId.HasValue)
                        query = query.Where(x => x.WardId == queryParams.WardId);

                    // Mark as standalone - skip additional filters
                    isStandaloneProperty = true;
                }
            }
            else
            {
                // For Non-Apartment (Individual, Plot, etc.): Filter by WardId only
                // PropertyNo is NOT used for non-apartments - user selects from all properties in ward
                if (queryParams.WardId.HasValue)
                    query = query.Where(x => x.WardId == queryParams.WardId);

                // Mark as standalone - skip additional filters
                isStandaloneProperty = true;
            }
        }
        else
        {
            // No CategoryId provided: Apply standard filters
            if (queryParams.WardId.HasValue)
                query = query.Where(x => x.WardId == queryParams.WardId);

            if (!string.IsNullOrWhiteSpace(queryParams.PropertyNo))
                query = query.Where(x => x.PropertyNo != null && x.PropertyNo.Contains(queryParams.PropertyNo));

            // When no category is specified, allow additional filters
            isStandaloneProperty = false;
        }

        // Only apply additional filters when no category is specified
        // For standalone apartments and non-apartments, skip these filters (only WardId should apply)
        // For multi-unit apartments, skip PartitionNo filter to return all properties from the wing
        if (!isStandaloneProperty && !isMultiUnitApartment)
        {
            // Apply PartitionNo filter (only when no category is specified)
            if (!string.IsNullOrWhiteSpace(queryParams.PartitionNo))
                query = query.Where(x => string.IsNullOrWhiteSpace(x.PartitionNo) || (x.PartitionNo != null && x.PartitionNo.Contains(queryParams.PartitionNo)));

            // Apply SearchTerm filter (only when no category is specified)
            if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
            {
                var term = queryParams.SearchTerm.Trim();
                query = query.Where(x =>
                    (x.PropertyNo != null && x.PropertyNo.Contains(term)) ||
                    (x.PartitionNo != null && x.PartitionNo.Contains(term)));
            }
        }

        return query;
    }
}

















































