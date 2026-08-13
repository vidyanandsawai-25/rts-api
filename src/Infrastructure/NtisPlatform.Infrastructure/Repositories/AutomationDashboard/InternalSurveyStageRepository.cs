using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.AutomationDashboard;

/// <summary>
/// Repository for Internal Survey stage database reads.
/// Optimized for performance with reusable query builders and efficient data fetching.
/// </summary>
public class InternalSurveyStageRepository : WorkflowStageBaseRepository, IInternalSurveyStageRepository
{
    public InternalSurveyStageRepository(ApplicationDbContext context) : base(context)
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
    /// Reads the active Geo-Sequencing stage id.
    /// Delegates to base repository for consistency.
    /// </summary>
    public Task<int> ReadGeoSequencingStageIdAsync(CancellationToken cancellationToken = default)
        => GetStageIdByNameAsync(GeoSequencingStageName, cancellationToken);

    /// <summary>
    /// Reads assessed and unassessed assessment status ids.
    /// Delegates to base repository for consistency.
    /// </summary>
    public Task<(int AssessedId, int UnassessedId)> ReadAssessedAndUnassessedStatusIdsAsync(
        CancellationToken cancellationToken = default)
        => GetAssessedAndUnassessedStatusIdsAsync(cancellationToken);

    /// <summary>
    /// Reads the Internal Survey property photo type id.
    /// Delegates to base repository for consistency.
    /// </summary>
    public Task<int> ReadPropertyPhotoTypeIdAsync(CancellationToken cancellationToken = default)
        => GetPhotoTypeIdAsync(PropertyPhotoTypeCode, cancellationToken);

    /// <summary>
    /// Reads stage properties for selected zones.
    /// Optimized with efficient query composition using query builder pattern.
    /// </summary>
    public async Task<List<InternalSurveyStagePropertyProjection>> ReadStagePropertiesForZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        bool requirePropertyNo,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null)
    {
        if (workflowStageId <= 0 || !zoneIds.Any())
            return new List<InternalSurveyStagePropertyProjection>();

        var query = BuildStagePropertiesQuery(workflowStageId, zoneIds, requirePropertyNo, queryParameters);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<InternalSurveyStagePropertyProjection>> ReadStagePropertiesForZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        bool requirePropertyNo,
        CancellationToken cancellationToken = default,
        WardWiseSummaryQueryParameters? queryParameters = null)
    {
        if (workflowStageId <= 0 || !zoneIds.Any())
            return new List<InternalSurveyStagePropertyProjection>();

        var query = BuildStagePropertiesQuery(workflowStageId, zoneIds, requirePropertyNo, queryParameters);
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reads property use rows for selected stage properties in zones.
    /// Optimized with efficient query composition using query builder pattern.
    /// </summary>
    public async Task<List<InternalSurveyPropertyUseSourceProjection>> ReadPropertyUsesForStageInZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        bool requirePropertyNo,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null)
    {
        if (workflowStageId <= 0 || !zoneIds.Any())
            return new List<InternalSurveyPropertyUseSourceProjection>();

        var query = BuildPropertyUsesQuery(workflowStageId, zoneIds, requirePropertyNo, queryParameters);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<InternalSurveyPropertyUseSourceProjection>> ReadPropertyUsesForStageInZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        bool requirePropertyNo,
        CancellationToken cancellationToken = default,
        WardWiseSummaryQueryParameters? queryParameters = null)
    {
        if (workflowStageId <= 0 || !zoneIds.Any())
            return new List<InternalSurveyPropertyUseSourceProjection>();

        var query = BuildPropertyUsesQuery(workflowStageId, zoneIds, requirePropertyNo, queryParameters);
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reads property photo counts grouped by zone.
    /// Optimized with efficient query composition using query builder pattern.
    /// </summary>
    public async Task<List<InternalSurveyPhotoCountProjection>> ReadPhotoCountsByZoneAsync(
        int workflowStageId,
        List<int> zoneIds,
        int propertyPhotoTypeId,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null)
    {
        if (workflowStageId <= 0 || propertyPhotoTypeId <= 0 || !zoneIds.Any())
            return new List<InternalSurveyPhotoCountProjection>();

        var query = BuildPhotoCountsByZoneQuery(workflowStageId, zoneIds, propertyPhotoTypeId, queryParameters);
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reads property photo counts grouped by ward.
    /// Optimized with efficient query composition using query builder pattern.
    /// </summary>
    public async Task<List<InternalSurveyPhotoCountProjection>> ReadPhotoCountsByWardAsync(
        int workflowStageId,
        List<int> wardIds,
        int propertyPhotoTypeId,
        CancellationToken cancellationToken = default)
    {
        if (workflowStageId <= 0 || propertyPhotoTypeId <= 0 || !wardIds.Any())
            return new List<InternalSurveyPhotoCountProjection>();

        var query = BuildPhotoCountsByWardQuery(workflowStageId, wardIds, propertyPhotoTypeId);
        return await query.ToListAsync(cancellationToken);
    }

    #endregion

    #region Private Query Builders

    /// <summary>
    /// Builds optimized query for stage properties with proper filtering and projections.
    /// Separates query building from execution for better testability and reusability.
    /// </summary>
    private IQueryable<InternalSurveyStagePropertyProjection> BuildStagePropertiesQuery(
        int workflowStageId,
        List<int> zoneIds,
        bool requirePropertyNo,
        DashboardGridQueryParameters? queryParameters)
        => BuildStagePropertiesQueryCore(workflowStageId, zoneIds, BuildFilteredPropertiesQuery(queryParameters, requirePropertyNo));

    private IQueryable<InternalSurveyStagePropertyProjection> BuildStagePropertiesQuery(
        int workflowStageId,
        List<int> zoneIds,
        bool requirePropertyNo,
        WardWiseSummaryQueryParameters? queryParameters)
        => BuildStagePropertiesQueryCore(workflowStageId, zoneIds, BuildFilteredPropertiesQuery(queryParameters, requirePropertyNo));

    private IQueryable<InternalSurveyStagePropertyProjection> BuildStagePropertiesQueryCore(
        int workflowStageId,
        List<int> zoneIds,
        IQueryable<PropertyEntity> propertiesQuery)
    {
        return from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
               where pwd.WorkflowStageId == workflowStageId
               join p in propertiesQuery on pwd.PropertyId equals p.Id
               join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
               where w.IsActive && zoneIds.Contains(w.ZoneId)
               join pt in _context.PropertyTypeMasters.AsNoTracking()
                   on p.PropertyTypeId equals pt.Id into propertyTypeJoin
               from pt in propertyTypeJoin.Where(x => x.IsActive).DefaultIfEmpty()
               select new InternalSurveyStagePropertyProjection
               {
                   PropertyId = p.Id,
                   WardId = p.WardId,
                   ZoneId = w.ZoneId,
                   PartitionNo = p.PartitionNo,
                   PropertyTypeCode = pt != null ? pt.Type : null,
                   AssessmentStatusId = p.PropertyAssessmentStatusId
               };
    }

    /// <summary>
    /// Builds query for property uses (type of use data).
    /// Separates query composition for better maintainability.
    /// </summary>
    private IQueryable<InternalSurveyPropertyUseSourceProjection> BuildPropertyUsesQuery(
        int workflowStageId,
        List<int> zoneIds,
        bool requirePropertyNo,
        DashboardGridQueryParameters? queryParameters)
        => BuildPropertyUsesQueryCore(workflowStageId, zoneIds, BuildFilteredPropertiesQuery(queryParameters, requirePropertyNo));

    private IQueryable<InternalSurveyPropertyUseSourceProjection> BuildPropertyUsesQuery(
        int workflowStageId,
        List<int> zoneIds,
        bool requirePropertyNo,
        WardWiseSummaryQueryParameters? queryParameters)
        => BuildPropertyUsesQueryCore(workflowStageId, zoneIds, BuildFilteredPropertiesQuery(queryParameters, requirePropertyNo));

    private IQueryable<InternalSurveyPropertyUseSourceProjection> BuildPropertyUsesQueryCore(
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
               select new InternalSurveyPropertyUseSourceProjection
               {
                   PropertyId = p.Id,
                   WardId = p.WardId,
                   ZoneId = w.ZoneId,
                   Type = tou.Type,
                   TypeOfUseCode = tou.TypeOfUseCode
               };
    }

    /// <summary>
    /// Builds query for property photo counts grouped by zone.
    /// Separates query composition for better maintainability.
    /// </summary>
    private IQueryable<InternalSurveyPhotoCountProjection> BuildPhotoCountsByZoneQuery(
        int workflowStageId,
        List<int> zoneIds,
        int propertyPhotoTypeId,
        DashboardGridQueryParameters? queryParameters)
    {
        var propertiesQuery = BuildFilteredPropertiesQuery(queryParameters, requirePropertyNo: true);

        // Build stage properties subquery
        var stageProperties =
            from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
            where pwd.WorkflowStageId == workflowStageId
            join p in propertiesQuery on pwd.PropertyId equals p.Id
            join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
            where w.IsActive && zoneIds.Contains(w.ZoneId)
            select new { PropertyId = p.Id, w.ZoneId };

        // Join with photos and group by zone
        return from sp in stageProperties
               join pp in _context.PropertyPhotos.AsNoTracking() on sp.PropertyId equals pp.PropertyId
               where pp.PhotoTypeId == propertyPhotoTypeId
                     && pp.IsActive
                     && !pp.MarkedForDeletion
               group pp by sp.ZoneId into g
               select new InternalSurveyPhotoCountProjection
               {
                   ZoneId = g.Key,
                   Count = g.Count()
               };
    }

    /// <summary>
    /// Builds query for property photo counts grouped by ward.
    /// Separates query composition for better maintainability.
    /// </summary>
    private IQueryable<InternalSurveyPhotoCountProjection> BuildPhotoCountsByWardQuery(
        int workflowStageId,
        List<int> wardIds,
        int propertyPhotoTypeId)
    {
        // Build stage properties subquery with registered properties only
        var stageProperties =
            from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
            join p in _context.PropertyMast.AsNoTracking() on pwd.PropertyId equals p.Id
            where pwd.WorkflowStageId == workflowStageId
                  && p.IsActive
                  && !p.MarkedForDeletion
                  && !string.IsNullOrEmpty(p.PropertyNo)
                  && wardIds.Contains(p.WardId)
            select new { PropertyId = p.Id, p.WardId };

        // Join with photos and group by ward
        return from sp in stageProperties
               join pp in _context.PropertyPhotos.AsNoTracking() on sp.PropertyId equals pp.PropertyId
               where pp.PhotoTypeId == propertyPhotoTypeId
                     && pp.IsActive
                     && !pp.MarkedForDeletion
               group pp by sp.WardId into g
               select new InternalSurveyPhotoCountProjection
               {
                   WardId = g.Key,
                   Count = g.Count()
               };
    }

    /// <summary>
    /// Builds filtered properties base query with optional property number filter.
    /// Centralizes property filtering logic to ensure consistency across all queries.
    /// </summary>
    private IQueryable<PropertyEntity> BuildFilteredPropertiesQuery(
        DashboardGridQueryParameters? queryParameters,
        bool requirePropertyNo = false)
    {
        var query = _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion);

        if (requirePropertyNo)
        {
            query = query.Where(p => !string.IsNullOrEmpty(p.PropertyNo));
        }

        query = ApplyMainGridPropertyTypeFilters(query, queryParameters);

        return query;
    }

    private IQueryable<PropertyEntity> BuildFilteredPropertiesQuery(
        WardWiseSummaryQueryParameters? queryParameters,
        bool requirePropertyNo = false)
    {
        var query = _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion);

        if (requirePropertyNo)
        {
            query = query.Where(p => !string.IsNullOrEmpty(p.PropertyNo));
        }

        query = ApplyMainGridPropertyTypeFilters(query, queryParameters);

        return query;
    }

    #endregion
}

