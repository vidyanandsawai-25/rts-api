using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.AutomationDashboard;

/// <summary>
/// Base repository containing common logic for all workflow stage repositories.
/// Provides reusable methods for zone-wise data calculation, structure/unit counting, and assessment status breakdown.
/// </summary>
public abstract class WorkflowStageBaseRepository
{
    protected const string ApartmentCategoryName = "Apartment";
    protected const string GeoSequencingStageName = "GeoSequencing";
    protected const string InternalSurveyStageName = "InternalSurvey";
    protected const string AssessmentStageName = "Assessment";
    protected const string PropertyPhotoTypeCode = "PROPERTY_PHOTO";
    protected const string PlanPhotoTypeCode = "PLAN_PHOTO";
    private static readonly string[] MixedPropertyTypes = { "R-C", "C-R", "C-I", "I-C", "I-R", "R-I" };
    protected readonly ApplicationDbContext _context;

    protected WorkflowStageBaseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Normalizes nullable paging input and caps page size for stable API performance.
    /// </summary>
    protected static (int PageNumber, int PageSize) NormalizePaging(int? pageNumber, int? pageSize)
        => WorkflowStagePagingHelper.NormalizePaging(pageNumber, pageSize);

    /// <summary>
    /// Loads and validates common zone, stage, ward, and paging context for ward-wise APIs.
    /// </summary>
    protected async Task<WardWiseSummaryContext> GetWardWiseSummaryContextAsync(
        int zoneId,
        int workflowStageId,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var (normalizedPageNumber, normalizedPageSize) = NormalizePaging(pageNumber, pageSize);

        if (zoneId <= 0 || workflowStageId <= 0)
            return WardWiseSummaryContext.Invalid(normalizedPageNumber, normalizedPageSize);

        var stageExists = await _context.PropertyWorkflowStageMaster
            .AsNoTracking()
            .AnyAsync(s => s.IsActive && s.Id == workflowStageId, cancellationToken);

        if (!stageExists)
            return WardWiseSummaryContext.Invalid(normalizedPageNumber, normalizedPageSize);

        var zone = await GetZoneAsync(zoneId, cancellationToken);
        if (zone.ZoneId == 0)
            return WardWiseSummaryContext.Invalid(normalizedPageNumber, normalizedPageSize);

        var wards = await GetWardsInZoneAsync(zoneId, cancellationToken);
        return new WardWiseSummaryContext(
            IsValid: true,
            ZoneId: zone.ZoneId,
            ZoneName: zone.ZoneName,
            PageNumber: normalizedPageNumber,
            PageSize: normalizedPageSize,
            Wards: wards);
    }

    /// <summary>
    /// Applies paging after totals are calculated from the complete ward result set.
    /// </summary>
    protected static List<T> PageWardData<T>(IEnumerable<T> wardData, int pageNumber, int pageSize)
        => WorkflowStagePagingHelper.PageWardData(wardData, pageNumber, pageSize);

    /// <summary>
    /// Gets active workflow stage id by stage name.
    /// </summary>
    protected async Task<int> GetStageIdByNameAsync(string stageName, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyWorkflowStageMaster
            .AsNoTracking()
            .Where(s => s.IsActive && s.StageName == stageName)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Gets active property photo type id by photo type code.
    /// </summary>
    protected async Task<int> GetPhotoTypeIdAsync(string photoTypeCode, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyPhotoTypes.AsNoTracking()
            .Where(pt => pt.IsActive && pt.PhotoTypeCode == photoTypeCode)
            .Select(pt => pt.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Gets assessed and unassessed status ids in one query.
    /// </summary>
    protected async Task<(int AssessedId, int UnassessedId)> GetAssessedAndUnassessedStatusIdsAsync(CancellationToken cancellationToken)
    {
        var statuses = await _context.PropertyAssessmentStatuses
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new { s.Id, s.StatusName })
            .ToListAsync(cancellationToken);

        var assessedId = statuses.FirstOrDefault(s => NormalizeAssessmentStatusName(s.StatusName) == "ASSESSED")?.Id ?? 0;
        var unassessedId = statuses.FirstOrDefault(s => NormalizeAssessmentStatusName(s.StatusName) == "UNASSESSED")?.Id ?? 0;

        return (assessedId, unassessedId);
    }

    protected sealed record WardWiseSummaryContext(
        bool IsValid,int ZoneId,string ZoneName,
        int PageNumber,int PageSize,List<(int WardId, string WardNo)> Wards)
    {
        public int TotalCount => Wards.Count;

        public static WardWiseSummaryContext Invalid(int pageNumber, int pageSize)
            => new(false, 0, string.Empty, pageNumber, pageSize, new List<(int WardId, string WardNo)>());
    }

    /// <summary>
    /// Common flow: Get properties by WorkflowStageId, then map to zones
    /// Step 1: PropertyWorkflowDetails -> PropertyIds
    /// Step 2: PropertyMast -> WardIds
    /// Step 3: WardMaster -> ZoneIds
    /// Step 4: ZoneMaster -> Zone Names
    /// </summary>
    protected async Task<Dictionary<int, List<int>>> GetZoneToPropertyMappingAsync(
        int workflowStageId,
        PropertySearchRequestDto? queryParameters,
        CancellationToken cancellationToken)
    {
        // Step 1: Get PropertyIds from PropertyWorkflowDetails by WorkflowStageId
        // No IsActive filter on PropertyWorkflowDetails to match SQL query logic
        var propertyIdsQuery = _context.PropertyWorkflowDetails
            .AsNoTracking()
            .Where(pwd => pwd.WorkflowStageId == workflowStageId  // Only filter by WorkflowStageId
                         && pwd.Property.IsActive && !pwd.Property.MarkedForDeletion )
            .Select(pwd => pwd.PropertyId)
            .Distinct();

        var propertyIds = await propertyIdsQuery.ToListAsync(cancellationToken);

        if (!propertyIds.Any())
            return new Dictionary<int, List<int>>();

        // Step 2: Get WardIds from PropertyMast by PropertyIds
        var propertyToWardMapping = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => propertyIds.Contains(p.Id))
            .Select(p => new { p.Id, p.WardId })
            .ToListAsync(cancellationToken);

        var wardIds = propertyToWardMapping.Select(p => p.WardId).Distinct().ToList();

        // Step 3: Get ZoneIds from WardMaster by WardIds
        var wardToZoneMapping = await _context.WardMaster
            .AsNoTracking()
            .Where(w => w.IsActive && wardIds.Contains(w.Id))
            .Select(w => new { w.Id, w.ZoneId })
            .ToListAsync(cancellationToken);

        // Apply zone filter if provided
        var filterZoneId = queryParameters?.ZoneId;
        if (filterZoneId.HasValue)
        {
            wardToZoneMapping = wardToZoneMapping
                .Where(w => w.ZoneId == filterZoneId.Value)
                .ToList();
        }

        // Create ZoneId -> PropertyIds mapping
        var wardToZoneByWardId = wardToZoneMapping.ToDictionary(w => w.Id, w => w.ZoneId);
        var zoneToPropertyMapping = new Dictionary<int, List<int>>();

        foreach (var propertyWard in propertyToWardMapping)
        {
            if (wardToZoneByWardId.TryGetValue(propertyWard.WardId, out var zoneId))
            {
                if (!zoneToPropertyMapping.ContainsKey(zoneId))
                    zoneToPropertyMapping[zoneId] = new List<int>();

                zoneToPropertyMapping[zoneId].Add(propertyWard.Id);
            }
        }

        return zoneToPropertyMapping;
    }

    /// <summary>
    /// Get all zones (optionally filtered)
    /// </summary>
    protected async Task<List<(int ZoneId, string ZoneName, string ZoneNo)>> GetZonesAsync(int? zoneId, CancellationToken cancellationToken)
    {
        var zonesQuery = _context.ZoneMaster
            .AsNoTracking()
            .Where(z => z.IsActive);

        if (zoneId.HasValue)
            zonesQuery = zonesQuery.Where(z => z.Id == zoneId.Value);

        return await zonesQuery
            .OrderBy(z => z.SequenceNo ?? 0)
            .ThenBy(z => z.ZoneNo)
            .Select(z => new ValueTuple<int, string, string>(z.Id, z.Description ?? z.ZoneNo, z.ZoneNo))
            .ToListAsync(cancellationToken);
    }

    protected async Task<(int ZoneId, string ZoneName, string ZoneNo)> GetZoneAsync(int zoneId, CancellationToken cancellationToken)
    {
        var zone = await _context.ZoneMaster
            .AsNoTracking()
            .Where(z => z.IsActive && z.Id == zoneId)
            .Select(z => new { z.Id, ZoneName = z.Description ?? z.ZoneNo, z.ZoneNo })
            .FirstOrDefaultAsync(cancellationToken);

        return zone == null ? (0, string.Empty, string.Empty) : (zone.Id, zone.ZoneName, zone.ZoneNo);
    }

    protected async Task<List<(int WardId, string WardNo)>> GetWardsInZoneAsync(
        int zoneId,
        CancellationToken cancellationToken)
    {
        return await _context.WardMaster
            .AsNoTracking()
            .Where(w => w.IsActive && w.ZoneId == zoneId)
            .OrderBy(w => w.SequenceNo ?? 0)
            .ThenBy(w => w.WardNo)
            .Select(w => new ValueTuple<int, string>(w.Id, w.WardNo ?? string.Empty))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Common: Apply filters to property query
    /// </summary>
    protected static IQueryable<PropertyEntity> ApplyFilters(IQueryable<PropertyEntity> query, PropertySearchRequestDto? request)
        => request is null
            ? query
            : ApplyFiltersCore(query, request.PropertyTypeId, request.WardId, request.CategoryId, request.PropertyNo, request.OwnerName);

    protected static IQueryable<PropertyEntity> ApplyFilters(IQueryable<PropertyEntity> query, AssessmentGridQueryParameters? request)
        => request is null
            ? query
            : ApplyFiltersCore(query, request.PropertyTypeId, wardId: null, request.CategoryId, request.PropertyNo, request.OwnerName);

    protected static IQueryable<PropertyEntity> ApplyFilters(IQueryable<PropertyEntity> query, SubGridQueryParameters? request)
        => request is null
            ? query
            : ApplyFiltersCore(query, request.PropertyTypeId, request.WardId, categoryId: null, request.PropertyNo, request.OwnerName);

    protected static IQueryable<PropertyEntity> ApplyFilters(IQueryable<PropertyEntity> query, WardSubGridQueryParameters? request)
        => request is null
            ? query
            : ApplyFiltersCore(query, request.PropertyTypeId, request.WardId, categoryId: null, request.PropertyNo, request.OwnerName);

    private static IQueryable<PropertyEntity> ApplyFiltersCore(
        IQueryable<PropertyEntity> query,
        int? propertyTypeId,
        int? wardId,
        int? categoryId,
        string? propertyNo,
        string? ownerName)
    {
        if (propertyTypeId.HasValue)
            query = query.Where(p => p.PropertyTypeId == propertyTypeId.Value);

        if (wardId.HasValue)
            query = query.Where(p => p.WardId == wardId.Value);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(propertyNo))
            query = query.Where(p => p.PropertyNo != null && p.PropertyNo.Contains(propertyNo));

        if (!string.IsNullOrWhiteSpace(ownerName))
            query = query.Where(p => p.OwnerName != null && p.OwnerName.Contains(ownerName));

        return query;
    }

    /// <summary>
    /// Applies the main-grid property type and property description bucket filters.
    /// </summary>
    protected IQueryable<PropertyEntity> ApplyMainGridPropertyTypeFilters(
        IQueryable<PropertyEntity> query,
        PropertySearchRequestDto? request)
        => request is null
            ? query
            : ApplyMainGridPropertyTypeFiltersCore(query, request.PropertyTypeId, request.PropertyTypeCategoryId);

    protected IQueryable<PropertyEntity> ApplyMainGridPropertyTypeFilters(
        IQueryable<PropertyEntity> query,
        DashboardGridQueryParameters? request)
        => request is null
            ? query
            : ApplyMainGridPropertyTypeFiltersCore(query, request.PropertyTypeId, request.PropertyTypeCategoryId);

    protected IQueryable<PropertyEntity> ApplyMainGridPropertyTypeFilters(
        IQueryable<PropertyEntity> query,
        AssessmentGridQueryParameters? request)
        => request is null
            ? query
            : ApplyMainGridPropertyTypeFiltersCore(query, request.PropertyTypeId, request.PropertyTypeCategoryId);

    protected IQueryable<PropertyEntity> ApplyMainGridPropertyTypeFilters(
        IQueryable<PropertyEntity> query,
        WardWiseSummaryQueryParameters? request)
        => request is null
            ? query
            : ApplyMainGridPropertyTypeFiltersCore(query, request.PropertyTypeId, request.PropertyTypeCategoryId);

    protected IQueryable<PropertyEntity> ApplyMainGridPropertyTypeFilters(
        IQueryable<PropertyEntity> query,
        SubGridQueryParameters? request)
        => request is null
            ? query
            : ApplyMainGridPropertyTypeFiltersCore(query, request.PropertyTypeId, request.PropertyTypeCategoryId);

    protected IQueryable<PropertyEntity> ApplyMainGridPropertyTypeFilters(
        IQueryable<PropertyEntity> query,
        WardSubGridQueryParameters? request)
        => request is null
            ? query
            : ApplyMainGridPropertyTypeFiltersCore(query, request.PropertyTypeId, request.PropertyTypeCategoryId);

    protected IQueryable<PropertyEntity> ApplyMainGridPropertyTypeFilters(
        IQueryable<PropertyEntity> query,
        PendingAssessmentQueryParameters? request)
        => request is null
            ? query
            : ApplyMainGridPropertyTypeFiltersCore(query, request.PropertyTypeId, propertyTypeCategoryId: null);

    private IQueryable<PropertyEntity> ApplyMainGridPropertyTypeFiltersCore(
        IQueryable<PropertyEntity> query,
        int? propertyTypeId,
        int? propertyTypeCategoryId)
    {
        if (propertyTypeId is > 0)
            query = query.Where(p => p.PropertyTypeId == propertyTypeId.Value);

        if (propertyTypeCategoryId is > 0)
            query = ApplyPropertyTypeCategoryFilter(query, propertyTypeCategoryId.Value);

        return query;
    }

    /// <summary>
    /// Filters properties into the same Residential/Non-Residential/Mixed/Public Utility/Under Construction buckets used by main-grid breakdowns.
    /// </summary>
    protected IQueryable<PropertyEntity> ApplyPropertyTypeCategoryFilter(
        IQueryable<PropertyEntity> query,
        int propertyTypeCategoryId)
    {
        const int residential = 1;
        const int nonResidential = 2;
        const int mixed = 3;
        const int openPlots = 4;
        const int publicUtility = 5;
        const int underConstruction = 6;

        if (propertyTypeCategoryId == mixed)
        {
            return query.Where(p =>
                _context.PropertyTypeMasters.AsNoTracking()
                    .Where(x => x.IsActive && x.Id == p.PropertyTypeId)
                    .Select(pt => pt.Type == null ? string.Empty : pt.Type.Trim().ToUpper())
                    .Any(type => MixedPropertyTypes.Contains(type)));
        }

        if (propertyTypeCategoryId == openPlots)
        {
            return query.Where(p =>
                !_context.PropertyTypeMasters.AsNoTracking()
                    .Where(x => x.IsActive && x.Id == p.PropertyTypeId)
                    .Select(pt => pt.Type == null ? string.Empty : pt.Type.Trim().ToUpper())
                    .Any(type => MixedPropertyTypes.Contains(type))
                && (p.OpenPlot == true
                    || _context.PropertyDetails.AsNoTracking()
                        .Where(pd => pd.IsActive && !pd.MarkedForDeletion && pd.PropertyId == p.Id)
                        .Any(pd => pd.IsOpenPlot == true
                            || _context.TypeOfUse.AsNoTracking()
                                .Where(tou => tou.IsActive && tou.Id == pd.TypeOfUseId)
                                .Select(tou => tou.Description ?? "")
                                .Any(desc => desc.ToUpper().Contains("OPEN")))));
        }

        if (propertyTypeCategoryId == underConstruction)
        {
            return query.Where(p =>
                !_context.PropertyTypeMasters.AsNoTracking()
                    .Where(x => x.IsActive && x.Id == p.PropertyTypeId)
                    .Select(pt => pt.Type == null ? string.Empty : pt.Type.Trim().ToUpper())
                    .Any(type => MixedPropertyTypes.Contains(type))
                && _context.PropertyDetails.AsNoTracking()
                    .Where(pd => pd.IsActive && !pd.MarkedForDeletion && pd.PropertyId == p.Id)
                    .Any(pd => _context.TypeOfUse.AsNoTracking()
                        .Where(tou => tou.IsActive && tou.Id == pd.TypeOfUseId)
                        .Any(tou => (tou.TypeOfUseCode ?? "").ToUpper() == "UC")));
        }

        if (propertyTypeCategoryId == publicUtility)
        {
            return query.Where(p =>
                !_context.PropertyTypeMasters.AsNoTracking()
                    .Where(x => x.IsActive && x.Id == p.PropertyTypeId)
                    .Select(pt => pt.Type == null ? string.Empty : pt.Type.Trim().ToUpper())
                    .Any(type => MixedPropertyTypes.Contains(type))
                && !_context.PropertyDetails.AsNoTracking()
                    .Where(pd => pd.IsActive && !pd.MarkedForDeletion && pd.PropertyId == p.Id)
                    .Any(pd => _context.TypeOfUse.AsNoTracking()
                        .Where(tou => tou.IsActive && tou.Id == pd.TypeOfUseId)
                        .Any(tou => (tou.TypeOfUseCode ?? "").ToUpper() == "UC"))
                && _context.PropertyDetails.AsNoTracking()
                    .Where(pd => pd.IsActive && !pd.MarkedForDeletion && pd.PropertyId == p.Id)
                    .Any(pd => _context.TypeOfUse.AsNoTracking()
                        .Where(tou => tou.IsActive && tou.Id == pd.TypeOfUseId)
                        .Any(tou => (tou.Type ?? "").ToUpper() == "N" || (tou.Type ?? "").ToUpper() == "I")));
        }

        if (propertyTypeCategoryId == residential)
        {
            return query.Where(p =>
                !_context.PropertyTypeMasters.AsNoTracking()
                    .Where(x => x.IsActive && x.Id == p.PropertyTypeId)
                    .Select(pt => pt.Type == null ? string.Empty : pt.Type.Trim().ToUpper())
                    .Any(type => MixedPropertyTypes.Contains(type))
                && !_context.PropertyDetails.AsNoTracking()
                    .Where(pd => pd.IsActive && !pd.MarkedForDeletion && pd.PropertyId == p.Id)
                    .Any(pd => _context.TypeOfUse.AsNoTracking()
                        .Where(tou => tou.IsActive && tou.Id == pd.TypeOfUseId)
                        .Any(tou => (tou.TypeOfUseCode ?? "").ToUpper() == "UC"))
                && !_context.PropertyDetails.AsNoTracking()
                    .Where(pd => pd.IsActive && !pd.MarkedForDeletion && pd.PropertyId == p.Id)
                    .Any(pd => _context.TypeOfUse.AsNoTracking()
                        .Where(tou => tou.IsActive && tou.Id == pd.TypeOfUseId)
                        .Any(tou => (tou.Type ?? "").ToUpper() == "N" || (tou.Type ?? "").ToUpper() == "I"))
                && (_context.PropertyDetails.AsNoTracking()
                    .Where(pd => pd.IsActive && !pd.MarkedForDeletion && pd.PropertyId == p.Id)
                    .Any(pd => _context.TypeOfUse.AsNoTracking()
                        .Where(tou => tou.IsActive && tou.Id == pd.TypeOfUseId)
                        .Any(tou => (tou.Type ?? "").ToUpper() == "R"))
                    || !_context.PropertyDetails.AsNoTracking()
                        .Where(pd => pd.IsActive && !pd.MarkedForDeletion && pd.PropertyId == p.Id)
                        .Any(pd => _context.TypeOfUse.AsNoTracking()
                            .Any(tou => tou.IsActive && tou.Id == pd.TypeOfUseId))));
        }

        if (propertyTypeCategoryId == nonResidential)
        {
            return query.Where(p =>
                !_context.PropertyTypeMasters.AsNoTracking()
                    .Where(x => x.IsActive && x.Id == p.PropertyTypeId)
                    .Select(pt => pt.Type == null ? string.Empty : pt.Type.Trim().ToUpper())
                    .Any(type => MixedPropertyTypes.Contains(type))
                && !_context.PropertyDetails.AsNoTracking()
                    .Where(pd => pd.IsActive && !pd.MarkedForDeletion && pd.PropertyId == p.Id)
                    .Any(pd => _context.TypeOfUse.AsNoTracking()
                        .Where(tou => tou.IsActive && tou.Id == pd.TypeOfUseId)
                        .Any(tou => (tou.TypeOfUseCode ?? "").ToUpper() == "UC"))
                && !_context.PropertyDetails.AsNoTracking()
                    .Where(pd => pd.IsActive && !pd.MarkedForDeletion && pd.PropertyId == p.Id)
                    .Any(pd => _context.TypeOfUse.AsNoTracking()
                        .Where(tou => tou.IsActive && tou.Id == pd.TypeOfUseId)
                        .Any(tou => (tou.Type ?? "").ToUpper() == "N" || (tou.Type ?? "").ToUpper() == "I"))
                && !_context.PropertyDetails.AsNoTracking()
                    .Where(pd => pd.IsActive && !pd.MarkedForDeletion && pd.PropertyId == p.Id)
                    .Any(pd => _context.TypeOfUse.AsNoTracking()
                        .Where(tou => tou.IsActive && tou.Id == pd.TypeOfUseId)
                        .Any(tou => (tou.Type ?? "").ToUpper() == "R"))
                && _context.PropertyDetails.AsNoTracking()
                    .Where(pd => pd.IsActive && !pd.MarkedForDeletion && pd.PropertyId == p.Id)
                    .Any(pd => _context.TypeOfUse.AsNoTracking()
                        .Where(tou => tou.IsActive && tou.Id == pd.TypeOfUseId)
                        .Any(tou => (tou.Type ?? "").ToUpper() == "C")));
        }

        return query;
    }

    /// <summary>
    /// DEPRECATED: Old counting logic using Apartment category (incorrect for most use cases)
    /// Use GetStructureAndUnitCountsForStageAsync or GetTotalStructureAndUnitInZoneAsync instead
    /// Structure = Properties without PartitionNo
    /// Unit = Properties with PartitionNo (Apartment category)
    /// </summary>
    [Obsolete("Use GetStructureAndUnitCountsForStageAsync or GetTotalStructureAndUnitInZoneAsync for correct SQL-matching logic")]
    protected async Task<(int PropertyCount, int StructureCount, int UnitCount)> CountPropertiesAsync(
        IQueryable<PropertyEntity> query,
        CancellationToken cancellationToken)
    {
        var propertyCount = await query.CountAsync(cancellationToken);

        var unitsOnlyCount = await (
            from p in query
            join pc in _context.PropertyCategoryMaster on p.CategoryId equals pc.Id into categoryJoin
            from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()
            where pc != null
                  && pc.PropertyCategoryName == ApartmentCategoryName
                  && p.PartitionNo != null
                  && p.PartitionNo.Trim() != ""
            select p
        ).CountAsync(cancellationToken);

        var structureCount = propertyCount - unitsOnlyCount;

        return (propertyCount, structureCount, unitsOnlyCount);
    }

    /// <summary>
    /// DEPRECATED: Old counting logic using Apartment category (incorrect for most use cases)
    /// Use GetStructureAndUnitCountsForStageAsync or GetTotalStructureAndUnitInZoneAsync instead
    /// </summary>
    [Obsolete("Use GetStructureAndUnitCountsForStageAsync or GetTotalStructureAndUnitInZoneAsync for correct SQL-matching logic")]
    protected async Task<(int PropertyCount, int StructureCount, int UnitCount)> CountPropertiesByIdsAsync(
        List<int> propertyIds,
        CancellationToken cancellationToken)
    {
        if (!propertyIds.Any())
            return (0, 0, 0);

        var query = _context.PropertyMast
            .AsNoTracking()
            .Where(p => propertyIds.Contains(p.Id));

        return await CountPropertiesAsync(query, cancellationToken);
    }

   
    protected async Task<(int StructureCount, int UnitCount)> GetStructureAndUnitCountsForStageAsync(
        List<int> propertyIds,
        List<int> wardIds,
        CancellationToken cancellationToken)
    {
        if (!propertyIds.Any())
            return (0, 0);

        var counts = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => propertyIds.Contains(p.Id))
            .GroupBy(p => 1)
            .Select(g => new
            {
                StructureCount = g.Count(p => p.PartitionNo == null || p.PartitionNo == ""),
                UnitCount = g.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return (counts?.StructureCount ?? 0, counts?.UnitCount ?? 0);
    }

 
    protected async Task<(int StructureCount, int UnitCount)> GetTotalStructureAndUnitInZoneAsync(List<int> wardIds, CancellationToken cancellationToken)
    {
        if (!wardIds.Any())
            return (0, 0);

        var counts = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive
                     && !p.MarkedForDeletion
                     && wardIds.Contains(p.WardId))
            .GroupBy(p => 1)
            .Select(g => new
            {
                StructureCount = g.Count(p => p.PartitionNo == null || p.PartitionNo == ""),
                UnitCount = g.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return (counts?.StructureCount ?? 0, counts?.UnitCount ?? 0);
    }

    protected async Task<int> GetRegisteredPropertyCountForWardsAsync(
        List<int> wardIds,
        CancellationToken cancellationToken)
    {
        if (!wardIds.Any())
            return 0;

        return await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive
                     && !p.MarkedForDeletion
                     && p.PropertyNo != null
                     && p.PropertyNo != ""
                     && wardIds.Contains(p.WardId))
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Common: Get Property Type Breakdown
    /// </summary>
    protected async Task<PropertyTypesBreakdownDto> GetPropertyTypeBreakdownAsync(List<int> propertyIds,CancellationToken cancellationToken)
    {
        var breakdown = new PropertyTypesBreakdownDto();

        if (!propertyIds.Any())
            return breakdown;

        // Step 1: Check for Mixed properties
        var mixedTypes = new[] { "R-C", "C-R", "C-I", "I-C", "I-R", "R-I" };
        var mixedPropertyIds = await (
            from p in _context.PropertyMast.AsNoTracking()
            join pt in _context.PropertyTypeMasters.AsNoTracking() on p.PropertyTypeId equals pt.Id
            where propertyIds.Contains(p.Id)
                  && pt.IsActive
                  && pt.Type != null
                  && mixedTypes.Contains(pt.Type.ToUpper())
            select p.Id
        ).Distinct().ToListAsync(cancellationToken);

        breakdown.Mixed = mixedPropertyIds.Count;

        // Step 2: Get TypeOfUse information for remaining properties
        var remainingPropertyIds = propertyIds.Except(mixedPropertyIds).ToList();
        if (!remainingPropertyIds.Any())
            return breakdown;

        var propertyTypeOfUseData = await (
            from pd in _context.PropertyDetails.AsNoTracking()
            join tou in _context.TypeOfUse.AsNoTracking() on pd.TypeOfUseId equals tou.Id
            where remainingPropertyIds.Contains(pd.PropertyId)
                  && pd.IsActive
                  && !pd.MarkedForDeletion
                  && tou.IsActive
            select new
            {
                PropertyId = pd.PropertyId,
                Type = tou.Type,
                TypeOfUseCode = tou.TypeOfUseCode
            }
        ).ToListAsync(cancellationToken);

        var propertyTypeGroups = propertyTypeOfUseData
            .GroupBy(x => x.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                Types = g.Select(x => x.Type).Distinct().ToList(),
                Codes = g.Select(x => x.TypeOfUseCode).Distinct().ToList()
            })
            .ToList();

        foreach (var propertyGroup in propertyTypeGroups)
        {
            if (propertyGroup.Codes.Any(code => code != null && code.ToUpper() == "UC"))
                breakdown.UnderConstruction++;
            else if (propertyGroup.Types.Any(type => type != null && (type.ToUpper() == "N" || type.ToUpper() == "I")))
                breakdown.PublicUtility++;
            else if (propertyGroup.Types.Any(type => type != null && type.ToUpper() == "R"))
                breakdown.Residential++;
            else if (propertyGroup.Types.Any(type => type != null && type.ToUpper() == "C"))
                breakdown.NonResidential++;
        }

        var propertiesWithoutDetails = remainingPropertyIds.Count - propertyTypeGroups.Count;
        if (propertiesWithoutDetails > 0)
            breakdown.Residential += propertiesWithoutDetails;

        return breakdown;
    }

    /// <summary>
    /// Common: Get Assessment Status Breakdown
    /// </summary>
    protected async Task<AssessmentStatusBreakdownDto> GetAssessmentStatusBreakdownAsync(List<int> propertyIds,CancellationToken cancellationToken)
    {
        if (!propertyIds.Any())
            return new AssessmentStatusBreakdownDto();

        var statuses = await _context.PropertyAssessmentStatuses
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new { s.Id, s.StatusName })
            .ToListAsync(cancellationToken);

        var statusIdsByName = statuses
            .Where(s => IsTrackedAssessmentStatus(s.StatusName))
            .GroupBy(s => NormalizeAssessmentStatusName(s.StatusName))
            .ToDictionary(g => g.Key, g => g.First().Id);

        var statusIds = statusIdsByName.Values.ToList();
        var countsByStatusId = await (
            from p in _context.PropertyMast.AsNoTracking()
            join pc in _context.PropertyCategoryMaster.AsNoTracking() on p.CategoryId equals pc.Id into categoryJoin
            from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()
            where propertyIds.Contains(p.Id)
                  && p.PropertyAssessmentStatusId.HasValue
                  && statusIds.Contains(p.PropertyAssessmentStatusId.Value)
            group new { p, pc } by p.PropertyAssessmentStatusId!.Value into g
            select new
            {
                StatusId = g.Key,
                PropertyCount = g.Count(),
                UnitsOnlyCount = g.Count(x => x.pc != null
                                               && x.pc.PropertyCategoryName == ApartmentCategoryName
                                               && x.p.PartitionNo != null
                                               && x.p.PartitionNo.Trim() != "")
            })
            .ToDictionaryAsync(
                x => x.StatusId,
                x => (StructureCount: x.PropertyCount - x.UnitsOnlyCount, UnitCount: x.UnitsOnlyCount),
                cancellationToken);

        var assessed = GetStatusCounts(statusIdsByName, countsByStatusId, "ASSESSED");
        var unassessed = GetStatusCounts(statusIdsByName, countsByStatusId, "UNASSESSED", "UN ASSESSED");
        var newlyAssessed = GetStatusCounts(statusIdsByName, countsByStatusId, "PARTIALLY_ASSESSED", "PARTIALLY ASSESSED", "NEWLY_ASSESSED_FOUND", "NEWLY ASSESSED FOUND");
        var assessmentInProcess = GetStatusCounts(statusIdsByName, countsByStatusId, "UNDER_UNASSESSED", "UNDER UNASSESSED", "ASSESSMENT_IN_PROCESS", "ASSESSMENT IN PROCESS");

        return new AssessmentStatusBreakdownDto
        {
            Assessed = assessed,
            Unassessed = unassessed,
            NewlyAssessedFound = newlyAssessed,
            AssessmentInProcess = assessmentInProcess
        };
    }

    private static StructureUnitCountDto GetStatusCounts(
        Dictionary<string, int> statusIdsByName,
        Dictionary<int, (int StructureCount, int UnitCount)> countsByStatusId,
        params string[] statusNames)
    {
        var statusId = ResolveAssessmentStatusId(statusIdsByName, statusNames);
        if (!countsByStatusId.TryGetValue(statusId, out var counts))
        {
            return new StructureUnitCountDto { StatusId = statusId };
        }

        return new StructureUnitCountDto
        {
            StatusId = statusId,
            StructureCount = counts.StructureCount,
            UnitCount = counts.UnitCount
        };
    }

    private static int ResolveAssessmentStatusId(Dictionary<string, int> statusIdsByName, params string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (statusIdsByName.TryGetValue(NormalizeAssessmentStatusName(alias), out var statusId))
                return statusId;
        }

        return 0;
    }

    private static bool IsTrackedAssessmentStatus(string statusName)
    {
        var normalized = NormalizeAssessmentStatusName(statusName);
        return normalized is "ASSESSED"
            or "UNASSESSED"
            or "PARTIALLYASSESSED"
            or "NEWLYASSESSEDFOUND"
            or "UNDERUNASSESSED"
            or "ASSESSMENTINPROCESS";
    }

    private static string NormalizeAssessmentStatusName(string value)
        => new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    /// <summary>
    /// Get properties in a zone for a specific workflow stage
    /// </summary>
    protected async Task<List<int>> GetPropertiesInZoneForStageAsync(
        int zoneId,
        int workflowStageId,
        CancellationToken cancellationToken)
    {
        // Get ward IDs in this zone
        var wardIds = await _context.WardMaster
            .AsNoTracking()
            .Where(w => w.IsActive && w.ZoneId == zoneId)
            .Select(w => w.Id)
            .ToListAsync(cancellationToken);

        // Get property IDs from PropertyWorkflowDetails for this stage
        // No IsActive filter on PropertyWorkflowDetails to match SQL query logic
        var propertyIdsQuery = _context.PropertyWorkflowDetails
            .AsNoTracking()
            .Where(pwd => pwd.WorkflowStageId == workflowStageId  // Only filter by WorkflowStageId
                         && pwd.Property.IsActive
                         && !pwd.Property.MarkedForDeletion
                         && wardIds.Contains(pwd.Property.WardId))
            .Select(pwd => pwd.PropertyId)
            .Distinct();

        return await propertyIdsQuery.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets active property ids for a workflow stage across pre-loaded ward ids.
    /// </summary>
    protected async Task<List<int>> GetPropertiesForStageInZoneAsync(
        int zoneId,
        int stageId,
        List<int> wardIds,
        CancellationToken cancellationToken,
        bool requirePropertyNo = false)
    {
        if (stageId == 0 || !wardIds.Any())
            return new List<int>();

        var query = _context.PropertyWorkflowDetails
            .AsNoTracking()
            .Where(pwd => pwd.WorkflowStageId == stageId
                       && pwd.Property.IsActive
                       && !pwd.Property.MarkedForDeletion
                       && wardIds.Contains(pwd.Property.WardId));

        if (requirePropertyNo)
        {
            query = query.Where(pwd => pwd.Property.PropertyNo != null
                                    && pwd.Property.PropertyNo != "");
        }

        return await query
            .Select(pwd => pwd.PropertyId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    protected async Task<List<int>> GetPropertiesForStageInWardsAsync( int workflowStageId,List<int> wardIds,CancellationToken cancellationToken,bool requirePropertyNo = false)
    {
        if (workflowStageId == 0 || !wardIds.Any())
            return new List<int>();

        var query = _context.PropertyWorkflowDetails
            .AsNoTracking()
            .Where(pwd => pwd.WorkflowStageId == workflowStageId
                       && pwd.Property.IsActive
                       && !pwd.Property.MarkedForDeletion
                       && wardIds.Contains(pwd.Property.WardId));

        if (requirePropertyNo)
        {
            query = query.Where(pwd => pwd.Property.PropertyNo != null
                                    && pwd.Property.PropertyNo != "");
        }

        return await query
            .Select(pwd => pwd.PropertyId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    protected static int? GetTypeOfUseId(PropertySearchRequestDto? request)
        => request?.TypeOfUseId;

    protected static int? GetTypeOfUseId(AssessmentGridQueryParameters? request)
        => request?.TypeOfUseId;
}

