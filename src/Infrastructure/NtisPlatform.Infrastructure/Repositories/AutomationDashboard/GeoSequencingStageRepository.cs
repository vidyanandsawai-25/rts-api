using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.AutomationDashboard;

/// <summary>
/// Repository for Geo-Sequencing stage database reads.
/// Optimized for performance with reusable query builders and efficient data fetching.
/// </summary>
public class GeoSequencingStageRepository : WorkflowStageBaseRepository, IGeoSequencingStageRepository
{
    private static readonly string[] AssessmentStatusNames =
    [
        "ASSESSED",
        "UNASSESSED",
        "PARTIALLYASSESSED",
        "NEWLYASSESSEDFOUND",
        "UNDERUNASSESSED",
        "ASSESSMENTINPROCESS"
    ];

    public GeoSequencingStageRepository(ApplicationDbContext context) : base(context)
    {
    }

    #region Public Interface Methods

    /// <summary>
    /// Reads active zones, optionally filtered by zone id.
    /// Delegates to base repository for consistency.
    /// </summary>
    public Task<List<(int ZoneId, string ZoneName, string ZoneNo)>> ReadZonesAsync(
        int? zoneId,
        CancellationToken cancellationToken = default)
        => GetZonesAsync(zoneId, cancellationToken);

    /// <summary>
    /// Reads one active zone by id.
    /// Delegates to base repository for consistency.
    /// </summary>
    public Task<(int ZoneId, string ZoneName, string ZoneNo)> ReadZoneAsync(
        int zoneId,
        CancellationToken cancellationToken = default)
        => GetZoneAsync(zoneId, cancellationToken);

    /// <summary>
    /// Reads active wards for one zone.
    /// Delegates to base repository for consistency.
    /// </summary>
    public Task<List<(int WardId, string WardNo)>> ReadWardsInZoneAsync(
        int zoneId,
        CancellationToken cancellationToken = default)
        => GetWardsInZoneAsync(zoneId, cancellationToken);

    /// <summary>
    /// Reads workflow stage properties for selected zones.
    /// Optimized with efficient query composition.
    /// </summary>
    public async Task<List<GeoSequencingStagePropertyProjection>> ReadStagePropertiesForZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null)
    {
        if (workflowStageId <= 0 || !zoneIds.Any())
            return new List<GeoSequencingStagePropertyProjection>();

        var query = BuildStagePropertiesQuery(workflowStageId, zoneIds, queryParameters);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<GeoSequencingStagePropertyProjection>> ReadStagePropertiesForZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        WardWiseSummaryQueryParameters? queryParameters = null)
    {
        if (workflowStageId <= 0 || !zoneIds.Any())
            return new List<GeoSequencingStagePropertyProjection>();

        var query = BuildStagePropertiesQuery(workflowStageId, zoneIds, queryParameters);
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reads registered property counts grouped by zone.
    /// Optimized with single query execution.
    /// </summary>
    public async Task<Dictionary<int, int>> ReadRegisteredCountsByZoneAsync(
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null)
    {
        if (!zoneIds.Any())
            return new Dictionary<int, int>();

        var query = BuildRegisteredPropertiesByZoneQuery(zoneIds, queryParameters);
        return await query.ToDictionaryAsync(x => x.ZoneId, x => x.Count, cancellationToken);
    }

    /// <summary>
    /// Reads registered property counts grouped by ward.
    /// Optimized with single query execution.
    /// </summary>
    public async Task<Dictionary<int, int>> ReadRegisteredCountsByWardAsync(
        List<int> wardIds,
        CancellationToken cancellationToken = default)
    {
        if (!wardIds.Any())
            return new Dictionary<int, int>();

        var query = BuildRegisteredPropertiesByWardQuery(wardIds);
        return await query.ToDictionaryAsync(x => x.WardId, x => x.Count, cancellationToken);
    }

    /// <summary>
    /// Reads property use rows for selected stage properties in zones.
    /// Optimized with efficient query composition.
    /// </summary>
    public async Task<List<GeoSequencingPropertyUseProjection>> ReadPropertyUsesForZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null)
    {
        if (workflowStageId <= 0 || !zoneIds.Any())
            return new List<GeoSequencingPropertyUseProjection>();

        var query = BuildPropertyUsesQuery(workflowStageId, zoneIds, queryParameters);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<GeoSequencingPropertyUseProjection>> ReadPropertyUsesForZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        WardWiseSummaryQueryParameters? queryParameters = null)
    {
        if (workflowStageId <= 0 || !zoneIds.Any())
            return new List<GeoSequencingPropertyUseProjection>();

        var query = BuildPropertyUsesQuery(workflowStageId, zoneIds, queryParameters);
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reads assessment status ids by status name.
    /// Optimized with single query execution.
    /// </summary>
    public Task<Dictionary<string, int>> ReadAssessmentStatusIdsByNameAsync(
        CancellationToken cancellationToken = default)
        => ReadAssessmentStatusIdsByNameCoreAsync(cancellationToken);

    private async Task<Dictionary<string, int>> ReadAssessmentStatusIdsByNameCoreAsync(CancellationToken cancellationToken)
    {
        var statuses = await _context.PropertyAssessmentStatuses
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new { s.Id, s.StatusName })
            .ToListAsync(cancellationToken);

        return statuses
            .Where(s => AssessmentStatusNames.Contains(NormalizeStatusName(s.StatusName)))
            .GroupBy(s => NormalizeStatusName(s.StatusName))
            .ToDictionary(g => g.Key, g => g.First().Id);
    }

    private static string NormalizeStatusName(string value)
        => new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    #endregion

    #region Private Query Builders

    /// <summary>
    /// Builds optimized query for stage properties with proper filtering and projections.
    /// Separates query building from execution for better testability and reusability.
    /// </summary>
    private IQueryable<GeoSequencingStagePropertyProjection> BuildStagePropertiesQuery(
        int workflowStageId,
        List<int> zoneIds,
        DashboardGridQueryParameters? queryParameters)
        => BuildStagePropertiesQueryCore(workflowStageId, zoneIds, BuildFilteredPropertiesQuery(queryParameters));

    private IQueryable<GeoSequencingStagePropertyProjection> BuildStagePropertiesQuery(
        int workflowStageId,
        List<int> zoneIds,
        WardWiseSummaryQueryParameters? queryParameters)
        => BuildStagePropertiesQueryCore(workflowStageId, zoneIds, BuildFilteredPropertiesQuery(queryParameters));

    private IQueryable<GeoSequencingStagePropertyProjection> BuildStagePropertiesQueryCore(
        int workflowStageId,
        List<int> zoneIds,
        IQueryable<PropertyEntity> propertiesQuery)
    {
        // Compose main query with all necessary joins
        return from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
               where pwd.WorkflowStageId == workflowStageId
               join p in propertiesQuery on pwd.PropertyId equals p.Id
               join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
               where w.IsActive && zoneIds.Contains(w.ZoneId)
               join pc in _context.PropertyCategoryMaster.AsNoTracking()
                   on p.CategoryId equals pc.Id into categoryJoin
               from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()
               join pt in _context.PropertyTypeMasters.AsNoTracking()
                   on p.PropertyTypeId equals pt.Id into propertyTypeJoin
               from pt in propertyTypeJoin.Where(x => x.IsActive).DefaultIfEmpty()
               select new GeoSequencingStagePropertyProjection
               {
                   PropertyId = p.Id,
                   WardId = p.WardId,
                   ZoneId = w.ZoneId,
                   PartitionNo = p.PartitionNo,
                   PropertyTypeCode = pt != null ? pt.Type : null,
                   PropertyCategoryName = pc != null ? pc.PropertyCategoryName : null,
                   AssessmentStatusId = p.PropertyAssessmentStatusId
               };
    }

    /// <summary>
    /// Builds query for registered properties grouped by zone.
    /// Reusable query builder for counting operations.
    /// </summary>
    private IQueryable<(int ZoneId, int Count)> BuildRegisteredPropertiesByZoneQuery(
        List<int> zoneIds,
        DashboardGridQueryParameters? queryParameters)
    {
        var propertiesQuery = BuildFilteredPropertiesQuery(queryParameters, includeRegisteredOnly: true);

        return from p in propertiesQuery
               join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
               where w.IsActive && zoneIds.Contains(w.ZoneId)
               group p by w.ZoneId into g
               select ValueTuple.Create(g.Key, g.Count());
    }

    /// <summary>
    /// Builds query for registered properties grouped by ward.
    /// Reusable query builder for counting operations.
    /// </summary>
    private IQueryable<(int WardId, int Count)> BuildRegisteredPropertiesByWardQuery(List<int> wardIds)
    {
        return from p in _context.PropertyMast.AsNoTracking()
               where p.IsActive
                     && !p.MarkedForDeletion
                     && !string.IsNullOrEmpty(p.PropertyNo)
                     && wardIds.Contains(p.WardId)
               group p by p.WardId into g
               select ValueTuple.Create(g.Key, g.Count());
    }

    /// <summary>
    /// Builds query for property uses (type of use data).
    /// Separates query composition for better maintainability.
    /// </summary>
    private IQueryable<GeoSequencingPropertyUseProjection> BuildPropertyUsesQuery(
        int workflowStageId,
        List<int> zoneIds,
        DashboardGridQueryParameters? queryParameters)
        => BuildPropertyUsesQueryCore(workflowStageId, zoneIds, BuildFilteredPropertiesQuery(queryParameters));

    private IQueryable<GeoSequencingPropertyUseProjection> BuildPropertyUsesQuery(
        int workflowStageId,
        List<int> zoneIds,
        WardWiseSummaryQueryParameters? queryParameters)
        => BuildPropertyUsesQueryCore(workflowStageId, zoneIds, BuildFilteredPropertiesQuery(queryParameters));

    private IQueryable<GeoSequencingPropertyUseProjection> BuildPropertyUsesQueryCore(
        int workflowStageId,
        List<int> zoneIds,
        IQueryable<PropertyEntity> propertiesQuery)
    {
        return from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
               where pwd.WorkflowStageId == workflowStageId
               join p in propertiesQuery on pwd.PropertyId equals p.Id
               join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
               where w.IsActive && zoneIds.Contains(w.ZoneId)
               join pd in _context.PropertyDetails.AsNoTracking() on p.Id equals pd.PropertyId
               where pd.IsActive && !pd.MarkedForDeletion
               join tou in _context.TypeOfUse.AsNoTracking() on pd.TypeOfUseId equals tou.Id
               where tou.IsActive
               select new GeoSequencingPropertyUseProjection
               {
                   PropertyId = p.Id,
                   Type = tou.Type,
                   TypeOfUseCode = tou.TypeOfUseCode
               };
    }

    /// <summary>
    /// Builds filtered properties base query with optional registered-only filter.
    /// Centralizes property filtering logic to ensure consistency across all queries.
    /// </summary>
    private IQueryable<PropertyEntity> BuildFilteredPropertiesQuery(
        DashboardGridQueryParameters? queryParameters,
        bool includeRegisteredOnly = false)
    {
        var query = _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion);

        if (includeRegisteredOnly)
        {
            query = query.Where(p => !string.IsNullOrEmpty(p.PropertyNo));
        }

        query = ApplyMainGridPropertyTypeFilters(query, queryParameters);

        return query;
    }

    private IQueryable<PropertyEntity> BuildFilteredPropertiesQuery(
        WardWiseSummaryQueryParameters? queryParameters,
        bool includeRegisteredOnly = false)
    {
        var query = _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion);

        if (includeRegisteredOnly)
        {
            query = query.Where(p => !string.IsNullOrEmpty(p.PropertyNo));
        }

        query = ApplyMainGridPropertyTypeFilters(query, queryParameters);

        return query;
    }

    #endregion
}
