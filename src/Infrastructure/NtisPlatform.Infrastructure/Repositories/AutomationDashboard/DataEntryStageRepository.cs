using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Infrastructure.Data;
using System.Data;
using System.Data.Common;

namespace NtisPlatform.Infrastructure.Repositories.AutomationDashboard;

/// <summary>
/// Repository for Data Entry and Quality Analyst stage operations.
/// Optimized for performance with reusable query builders and efficient data fetching.
/// Implements granular methods to avoid DbContext concurrency issues.
/// </summary>
public class DataEntryStageRepository : WorkflowStageBaseRepository, IDataEntryStageRepository
{
    public DataEntryStageRepository(ApplicationDbContext context) : base(context)
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
    /// Reads the Internal Survey stage id.
    /// Delegates to base repository for consistency.
    /// </summary>
    public Task<int> ReadInternalSurveyStageIdAsync(CancellationToken cancellationToken = default)
        => GetStageIdByNameAsync(InternalSurveyStageName, cancellationToken);

    /// <summary>
    /// Reads the Assessment stage id.
    /// Delegates to base repository for consistency.
    /// </summary>
    public Task<int> ReadAssessmentStageIdAsync(CancellationToken cancellationToken = default)
        => GetStageIdByNameAsync(AssessmentStageName, cancellationToken);

    /// <summary>
    /// Reads the Property photo type id.
    /// Delegates to base repository for consistency.
    /// </summary>
    public Task<int> ReadPropertyPhotoTypeIdAsync(CancellationToken cancellationToken = default)
        => GetPhotoTypeIdAsync(PropertyPhotoTypeCode, cancellationToken);

    /// <summary>
    /// Reads the Plan photo type id.
    /// Delegates to base repository for consistency.
    /// </summary>
    public Task<int> ReadPlanPhotoTypeIdAsync(CancellationToken cancellationToken = default)
        => GetPhotoTypeIdAsync(PlanPhotoTypeCode, cancellationToken);

    /// <summary>
    /// Reads stage properties for selected zones.
    /// Optimized with efficient query composition using query builder pattern.
    /// </summary>
    public async Task<List<DataEntryStagePropertyProjection>> ReadStagePropertiesForZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null)
    {
        if (workflowStageId <= 0 || !zoneIds.Any())
            return new List<DataEntryStagePropertyProjection>();

        var query = BuildStagePropertiesQuery(workflowStageId, zoneIds, queryParameters);
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reads zone totals (structure and unit counts).
    /// Optimized with efficient query composition using query builder pattern.
    /// </summary>
    public async Task<Dictionary<int, (int StructureCount, int UnitCount)>> ReadZoneTotalsAsync(
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null)
    {
        if (!zoneIds.Any())
            return new Dictionary<int, (int StructureCount, int UnitCount)>();

        var query = BuildZoneTotalsQuery(zoneIds, queryParameters);
        var results = await query.ToListAsync(cancellationToken);

        return results.ToDictionary(
            r => r.ZoneId,
            r => (r.StructureCount, r.UnitCount));
    }

    /// <summary>
    /// Reads completed photos for selected zones.
    /// Optimized with efficient query composition using query builder pattern.
    /// </summary>
    public async Task<List<DataEntryCompletedPhotoProjection>> ReadCompletedPhotosAsync(
        int workflowStageId,
        List<int> zoneIds,
        int propertyPhotoTypeId,
        int planPhotoTypeId,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null)
    {
        if (workflowStageId <= 0 || !zoneIds.Any())
            return new List<DataEntryCompletedPhotoProjection>();

        var photoTypeIds = new List<int>();
        if (propertyPhotoTypeId > 0) photoTypeIds.Add(propertyPhotoTypeId);
        if (planPhotoTypeId > 0) photoTypeIds.Add(planPhotoTypeId);

        if (!photoTypeIds.Any())
            return new List<DataEntryCompletedPhotoProjection>();

        var query = BuildCompletedPhotosQuery(workflowStageId, zoneIds, photoTypeIds, queryParameters);
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reads property types for selected zones.
    /// Optimized with efficient query composition using query builder pattern.
    /// </summary>
    public async Task<List<DataEntryPropertyTypeSourceProjection>> ReadPropertyTypesAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null)
    {
        if (workflowStageId <= 0 || !zoneIds.Any())
            return new List<DataEntryPropertyTypeSourceProjection>();

        var query = BuildPropertyTypesQuery(workflowStageId, zoneIds, queryParameters);
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reads property uses for selected zones.
    /// Optimized with efficient query composition using query builder pattern.
    /// </summary>
    public async Task<List<DataEntryPropertyUseSourceProjection>> ReadPropertyUsesAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null)
    {
        if (workflowStageId <= 0 || !zoneIds.Any())
            return new List<DataEntryPropertyUseSourceProjection>();

        var query = BuildPropertyUsesQuery(workflowStageId, zoneIds, queryParameters);
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reads assessment status IDs by name.
    /// </summary>
    public async Task<Dictionary<string, int>> ReadAssessmentStatusIdsAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new[] { "ASSESSED", "UNASSESSED", "PARTIALLY_ASSESSED", "UNDER_UNASSESSED" };

        var results = await _context.PropertyAssessmentStatuses
            .AsNoTracking()
            .Where(s => s.IsActive && statuses.Contains(s.StatusName.ToUpper()))
            .Select(s => new { s.Id, StatusName = s.StatusName.ToUpper() })
            .ToListAsync(cancellationToken);

        return results.ToDictionary(s => s.StatusName, s => s.Id);
    }

    /// <summary>
    /// Reads assessment status counts grouped by zone.
    /// Optimized with efficient query composition using query builder pattern.
    /// </summary>
    public async Task<List<DataEntryAssessmentStatusCountProjection>> ReadAssessmentStatusCountsAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null)
    {
        if (workflowStageId <= 0 || !zoneIds.Any())
            return new List<DataEntryAssessmentStatusCountProjection>();

        var query = BuildAssessmentStatusCountsQuery(workflowStageId, zoneIds, queryParameters);
        return await query.ToListAsync(cancellationToken);
    }

    #endregion

    #region Query Builders

    /// <summary>
    /// Builds query for stage properties.
    /// Reusable and composable for different workflow stages.
    /// </summary>
    private IQueryable<DataEntryStagePropertyProjection> BuildStagePropertiesQuery(
        int workflowStageId,
        List<int> zoneIds,
        DashboardGridQueryParameters? queryParameters)
    {
        var properties = BuildFilteredPropertyQuery(queryParameters);

        var query = from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
                    join p in properties on pwd.PropertyId equals p.Id
                    join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
                    where pwd.WorkflowStageId == workflowStageId
                          && zoneIds.Contains(w.ZoneId)
                          && w.IsActive
                    select new DataEntryStagePropertyProjection
                    {
                        PropertyId = p.Id,
                        WorkflowStageId = pwd.WorkflowStageId,
                        ZoneId = w.ZoneId,
                        PartitionNo = p.PartitionNo
                    };

        return query.Distinct();
    }

    /// <summary>
    /// Builds query for zone totals.
    /// </summary>
    private IQueryable<DataEntryZoneCountProjection> BuildZoneTotalsQuery(
        List<int> zoneIds,
        DashboardGridQueryParameters? queryParameters)
    {
        var properties = BuildFilteredPropertyQuery(queryParameters);

        var baseQuery = from p in properties
                        join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
                        where zoneIds.Contains(w.ZoneId)
                              && w.IsActive
                        select new { p, w.ZoneId };

        return baseQuery
            .GroupBy(x => x.ZoneId)
            .Select(g => new DataEntryZoneCountProjection
            {
                ZoneId = g.Key,
                StructureCount = g.Count(x => string.IsNullOrEmpty(x.p.PartitionNo)),
                UnitCount = g.Count()
            });
    }

    /// <summary>
    /// Builds query for completed photos.
    /// </summary>
    private IQueryable<DataEntryCompletedPhotoProjection> BuildCompletedPhotosQuery(
        int workflowStageId,
        List<int> zoneIds,
        List<int> photoTypeIds,
        DashboardGridQueryParameters? queryParameters)
    {
        var properties = BuildFilteredPropertyQuery(queryParameters);

        var query = from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
                    join p in properties on pwd.PropertyId equals p.Id
                    join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
                    join pp in _context.PropertyPhotos.AsNoTracking() on p.Id equals pp.PropertyId
                    where pwd.WorkflowStageId == workflowStageId
                          && zoneIds.Contains(w.ZoneId)
                          && photoTypeIds.Contains(pp.PhotoTypeId)
                          && w.IsActive
                          && pp.IsActive
                          && !pp.MarkedForDeletion
                    select new DataEntryCompletedPhotoProjection
                    {
                        PropertyId = p.Id,
                        PhotoTypeId = pp.PhotoTypeId
                    };

        return query.Distinct();
    }

    /// <summary>
    /// Builds query for property types.
    /// </summary>
    private IQueryable<DataEntryPropertyTypeSourceProjection> BuildPropertyTypesQuery(
        int workflowStageId,
        List<int> zoneIds,
        DashboardGridQueryParameters? queryParameters)
    {
        var properties = BuildFilteredPropertyQuery(queryParameters);

        var query = from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
                    join p in properties on pwd.PropertyId equals p.Id
                    join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
                    join pt in _context.PropertyTypeMasters.AsNoTracking() on p.PropertyTypeId equals pt.Id into ptJoin
                    from pt in ptJoin.DefaultIfEmpty()
                    where pwd.WorkflowStageId == workflowStageId
                          && zoneIds.Contains(w.ZoneId)
                          && w.IsActive
                          && (pt == null || pt.IsActive)
                    select new DataEntryPropertyTypeSourceProjection
                    {
                        PropertyId = p.Id,
                        ZoneId = w.ZoneId,
                        PropertyType = pt != null ? pt.Type : null
                    };

        return query.Distinct();
    }

    /// <summary>
    /// Builds query for property uses.
    /// </summary>
    private IQueryable<DataEntryPropertyUseSourceProjection> BuildPropertyUsesQuery(
        int workflowStageId,
        List<int> zoneIds,
        DashboardGridQueryParameters? queryParameters)
    {
        var properties = BuildFilteredPropertyQuery(queryParameters);

        var query = from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
                    join p in properties on pwd.PropertyId equals p.Id
                    join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
                    join pd in _context.PropertyDetails.AsNoTracking() on p.Id equals pd.PropertyId
                    join tou in _context.TypeOfUse.AsNoTracking() on pd.TypeOfUseId equals tou.Id
                    where pwd.WorkflowStageId == workflowStageId
                          && zoneIds.Contains(w.ZoneId)
                          && w.IsActive
                          && pd.IsActive
                          && !pd.MarkedForDeletion
                          && tou.IsActive
                    select new DataEntryPropertyUseSourceProjection
                    {
                        PropertyId = p.Id,
                        ZoneId = w.ZoneId,
                        Type = tou.Type,
                        TypeOfUseCode = tou.TypeOfUseCode
                    };

        return query;
    }

    /// <summary>
    /// Builds query for assessment status counts.
    /// </summary>
    private IQueryable<DataEntryAssessmentStatusCountProjection> BuildAssessmentStatusCountsQuery(
        int workflowStageId,
        List<int> zoneIds,
        DashboardGridQueryParameters? queryParameters)
    {
        var statuses = new[] { "ASSESSED", "UNASSESSED", "PARTIALLY_ASSESSED", "UNDER_UNASSESSED" };
        var properties = BuildFilteredPropertyQuery(queryParameters);

        var baseQuery = from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
                        join p in properties on pwd.PropertyId equals p.Id
                        join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
                        join pas in _context.PropertyAssessmentStatuses.AsNoTracking() on p.PropertyAssessmentStatusId equals pas.Id
                        join pc in _context.PropertyCategoryMaster.AsNoTracking() on p.CategoryId equals pc.Id into pcJoin
                        from pc in pcJoin.DefaultIfEmpty()
                        where pwd.WorkflowStageId == workflowStageId
                              && zoneIds.Contains(w.ZoneId)
                              && p.PropertyAssessmentStatusId != null
                              && w.IsActive
                              && pas.IsActive
                              && statuses.Contains(pas.StatusName.ToUpper())
                              && (pc == null || pc.IsActive)
                        select new
                        {
                            p,
                            w.ZoneId,
                            StatusId = p.PropertyAssessmentStatusId!.Value,
                            IsUnitOnly = pc != null && pc.PropertyCategoryName == ApartmentCategoryName
                                        && !string.IsNullOrEmpty(p.PartitionNo)
                        };

        return baseQuery
            .GroupBy(x => new { x.ZoneId, x.StatusId })
            .Select(g => new DataEntryAssessmentStatusCountProjection
            {
                ZoneId = g.Key.ZoneId,
                StatusId = g.Key.StatusId,
                PropertyCount = g.Count(),
                UnitsOnlyCount = g.Count(x => x.IsUnitOnly)
            });
    }

    #endregion

    #region Filter Helpers

    private IQueryable<PropertyEntity> BuildFilteredPropertyQuery(DashboardGridQueryParameters? queryParameters)
        => ApplyMainGridPropertyTypeFilters(
            _context.PropertyMast
                .AsNoTracking()
                .Where(p => p.IsActive && !p.MarkedForDeletion),
            queryParameters);

    #endregion

    #region Legacy Ward-Wise Summary Method

    public async Task<DataEntryGridSnapshotProjection> GetDataEntryGridSnapshotAsync(
        int dataEntryStageId,
        int? zoneId,
        CancellationToken cancellationToken = default,
        int? propertyTypeId = null,
        int? propertyTypeCategoryId = null)
    {
        const string sql = """
            SET NOCOUNT ON;

            DECLARE @InternalSurveyStageId INT =
                ISNULL((SELECT TOP (1) Id FROM PTIS.PropertyWorkflowStageMaster WHERE IsActive = 1 AND StageName = 'InternalSurvey'), 0);
            DECLARE @AssessmentStageId INT =
                ISNULL((SELECT TOP (1) Id FROM PTIS.PropertyWorkflowStageMaster WHERE IsActive = 1 AND StageName = 'Assessment'), 0);
            DECLARE @PropertyPhotoTypeId INT =
                ISNULL((SELECT TOP (1) Id FROM PTIS.PropertyPhotoType WHERE IsActive = 1 AND PhotoTypeCode = 'PROPERTY_PHOTO'), 0);
            DECLARE @PlanPhotoTypeId INT =
                ISNULL((SELECT TOP (1) Id FROM PTIS.PropertyPhotoType WHERE IsActive = 1 AND PhotoTypeCode = 'PLAN_PHOTO'), 0);

            SELECT
                z.Id AS ZoneId,
                COALESCE(z.Description, z.ZoneNo, '') AS ZoneName,
                ISNULL(z.SequenceNo, 0) AS SequenceNo,
                z.ZoneNo
            INTO #SelectedZones
            FROM PTIS.ZoneMaster z
            WHERE z.IsActive = 1
              AND (@ZoneId IS NULL OR z.Id = @ZoneId);

            CREATE UNIQUE CLUSTERED INDEX IX_SelectedZones_ZoneId ON #SelectedZones(ZoneId);

            SELECT DISTINCT
                p.Id AS PropertyId,
                w.ZoneId,
                p.PartitionNo
            INTO #FilteredProperties
            FROM PTIS.PropertyMast p
            INNER JOIN PTIS.WardMaster w ON p.WardId = w.Id
            INNER JOIN #SelectedZones z ON w.ZoneId = z.ZoneId
            LEFT JOIN PTIS.PropertyTypeMaster ptm ON p.PropertyTypeId = ptm.Id AND ptm.IsActive = 1
            WHERE p.IsActive = 1
              AND p.MarkedForDeletion = 0
              AND w.IsActive = 1
              AND (@PropertyTypeId IS NULL OR p.PropertyTypeId = @PropertyTypeId)
              AND
              (
                  @PropertyTypeCategoryId IS NULL
                  OR (@PropertyTypeCategoryId = 3 AND UPPER(ISNULL(ptm.Type, '')) IN ('R-C', 'C-R', 'C-I', 'I-C', 'I-R', 'R-I'))
                  OR (@PropertyTypeCategoryId = 4
                      AND UPPER(ISNULL(ptm.Type, '')) NOT IN ('R-C', 'C-R', 'C-I', 'I-C', 'I-R', 'R-I')
                      AND (p.OpenPlot = 1 OR EXISTS
                      (
                          SELECT 1
                          FROM PTIS.PropertyDetails pd
                          LEFT JOIN PTIS.TypeOfUseMaster tou ON pd.TypeOfUseId = tou.Id
                          WHERE pd.PropertyId = p.Id
                            AND pd.IsActive = 1
                            AND pd.MarkedForDeletion = 0
                            AND (pd.IsOpenPlot = 1 OR UPPER(ISNULL(tou.Description, '')) LIKE '%OPEN%')
                      )))
                  OR (@PropertyTypeCategoryId = 6
                      AND UPPER(ISNULL(ptm.Type, '')) NOT IN ('R-C', 'C-R', 'C-I', 'I-C', 'I-R', 'R-I')
                      AND NOT (p.OpenPlot = 1 OR EXISTS
                      (
                          SELECT 1
                          FROM PTIS.PropertyDetails pd
                          LEFT JOIN PTIS.TypeOfUseMaster tou ON pd.TypeOfUseId = tou.Id
                          WHERE pd.PropertyId = p.Id
                            AND pd.IsActive = 1
                            AND pd.MarkedForDeletion = 0
                            AND (pd.IsOpenPlot = 1 OR UPPER(ISNULL(tou.Description, '')) LIKE '%OPEN%')
                      ))
                      AND EXISTS
                      (
                          SELECT 1
                          FROM PTIS.PropertyDetails pd
                          LEFT JOIN PTIS.TypeOfUseMaster tou ON pd.TypeOfUseId = tou.Id
                          WHERE pd.PropertyId = p.Id
                            AND pd.IsActive = 1
                            AND pd.MarkedForDeletion = 0
                            AND UPPER(ISNULL(tou.TypeOfUseCode, '')) = 'UC'
                      ))
                  OR (@PropertyTypeCategoryId = 5
                      AND UPPER(ISNULL(ptm.Type, '')) NOT IN ('R-C', 'C-R', 'C-I', 'I-C', 'I-R', 'R-I')
                      AND NOT (p.OpenPlot = 1 OR EXISTS
                      (
                          SELECT 1
                          FROM PTIS.PropertyDetails pd
                          LEFT JOIN PTIS.TypeOfUseMaster tou ON pd.TypeOfUseId = tou.Id
                          WHERE pd.PropertyId = p.Id
                            AND pd.IsActive = 1
                            AND pd.MarkedForDeletion = 0
                            AND (pd.IsOpenPlot = 1 OR UPPER(ISNULL(tou.Description, '')) LIKE '%OPEN%')
                      ))
                      AND NOT EXISTS
                      (
                          SELECT 1
                          FROM PTIS.PropertyDetails pd
                          LEFT JOIN PTIS.TypeOfUseMaster tou ON pd.TypeOfUseId = tou.Id
                          WHERE pd.PropertyId = p.Id
                            AND pd.IsActive = 1
                            AND pd.MarkedForDeletion = 0
                            AND UPPER(ISNULL(tou.TypeOfUseCode, '')) = 'UC'
                      )
                      AND EXISTS
                      (
                          SELECT 1
                          FROM PTIS.PropertyDetails pd
                          LEFT JOIN PTIS.TypeOfUseMaster tou ON pd.TypeOfUseId = tou.Id
                          WHERE pd.PropertyId = p.Id
                            AND pd.IsActive = 1
                            AND pd.MarkedForDeletion = 0
                            AND (UPPER(ISNULL(tou.Type, '')) IN ('N', 'I')
                                 OR UPPER(ISNULL(tou.TypeOfUseCode, '')) = 'PU'
                                 OR UPPER(ISNULL(tou.Description, '')) LIKE '%PUBLIC%'
                                 OR UPPER(ISNULL(tou.Description, '')) LIKE '%INDUSTRIAL%')
                      ))
                  OR (@PropertyTypeCategoryId = 1
                      AND UPPER(ISNULL(ptm.Type, '')) NOT IN ('R-C', 'C-R', 'C-I', 'I-C', 'I-R', 'R-I')
                      AND NOT (p.OpenPlot = 1 OR EXISTS
                      (
                          SELECT 1
                          FROM PTIS.PropertyDetails pd
                          LEFT JOIN PTIS.TypeOfUseMaster tou ON pd.TypeOfUseId = tou.Id
                          WHERE pd.PropertyId = p.Id
                            AND pd.IsActive = 1
                            AND pd.MarkedForDeletion = 0
                            AND (pd.IsOpenPlot = 1 OR UPPER(ISNULL(tou.Description, '')) LIKE '%OPEN%')
                      ))
                      AND NOT EXISTS
                      (
                          SELECT 1
                          FROM PTIS.PropertyDetails pd
                          LEFT JOIN PTIS.TypeOfUseMaster tou ON pd.TypeOfUseId = tou.Id
                          WHERE pd.PropertyId = p.Id
                            AND pd.IsActive = 1
                            AND pd.MarkedForDeletion = 0
                            AND (UPPER(ISNULL(tou.TypeOfUseCode, '')) = 'UC'
                                 OR UPPER(ISNULL(tou.Type, '')) IN ('N', 'I')
                                 OR UPPER(ISNULL(tou.TypeOfUseCode, '')) = 'PU'
                                 OR UPPER(ISNULL(tou.Description, '')) LIKE '%PUBLIC%'
                                 OR UPPER(ISNULL(tou.Description, '')) LIKE '%INDUSTRIAL%')
                      )
                      AND (EXISTS
                      (
                          SELECT 1
                          FROM PTIS.PropertyDetails pd
                          LEFT JOIN PTIS.TypeOfUseMaster tou ON pd.TypeOfUseId = tou.Id
                          WHERE pd.PropertyId = p.Id
                            AND pd.IsActive = 1
                            AND pd.MarkedForDeletion = 0
                            AND (UPPER(ISNULL(tou.Type, '')) = 'R' OR UPPER(ISNULL(tou.Description, '')) LIKE '%RESIDENTIAL%')
                      )
                      OR NOT EXISTS
                      (
                          SELECT 1
                          FROM PTIS.PropertyDetails pd
                          WHERE pd.PropertyId = p.Id
                            AND pd.IsActive = 1
                            AND pd.MarkedForDeletion = 0
                      )))
                  OR (@PropertyTypeCategoryId = 2
                      AND UPPER(ISNULL(ptm.Type, '')) NOT IN ('R-C', 'C-R', 'C-I', 'I-C', 'I-R', 'R-I')
                      AND NOT (p.OpenPlot = 1 OR EXISTS
                      (
                          SELECT 1
                          FROM PTIS.PropertyDetails pd
                          LEFT JOIN PTIS.TypeOfUseMaster tou ON pd.TypeOfUseId = tou.Id
                          WHERE pd.PropertyId = p.Id
                            AND pd.IsActive = 1
                            AND pd.MarkedForDeletion = 0
                            AND (pd.IsOpenPlot = 1 OR UPPER(ISNULL(tou.Description, '')) LIKE '%OPEN%')
                      ))
                      AND NOT EXISTS
                      (
                          SELECT 1
                          FROM PTIS.PropertyDetails pd
                          LEFT JOIN PTIS.TypeOfUseMaster tou ON pd.TypeOfUseId = tou.Id
                          WHERE pd.PropertyId = p.Id
                            AND pd.IsActive = 1
                            AND pd.MarkedForDeletion = 0
                            AND (UPPER(ISNULL(tou.TypeOfUseCode, '')) = 'UC'
                                 OR UPPER(ISNULL(tou.Type, '')) IN ('N', 'I')
                                 OR UPPER(ISNULL(tou.TypeOfUseCode, '')) = 'PU'
                                 OR UPPER(ISNULL(tou.Description, '')) LIKE '%PUBLIC%'
                                 OR UPPER(ISNULL(tou.Description, '')) LIKE '%INDUSTRIAL%'
                                 OR UPPER(ISNULL(tou.Type, '')) = 'R'
                                 OR UPPER(ISNULL(tou.Description, '')) LIKE '%RESIDENTIAL%')
                      )
                      AND EXISTS
                      (
                          SELECT 1
                          FROM PTIS.PropertyDetails pd
                          LEFT JOIN PTIS.TypeOfUseMaster tou ON pd.TypeOfUseId = tou.Id
                          WHERE pd.PropertyId = p.Id
                            AND pd.IsActive = 1
                            AND pd.MarkedForDeletion = 0
                            AND (UPPER(ISNULL(tou.Type, '')) = 'C' OR UPPER(ISNULL(tou.Description, '')) LIKE '%COMMERCIAL%')
                      ))
              );

            CREATE UNIQUE CLUSTERED INDEX IX_FilteredProperties_Property ON #FilteredProperties(PropertyId);
            CREATE INDEX IX_FilteredProperties_Zone ON #FilteredProperties(ZoneId);

            SELECT DISTINCT
                pwd.WorkflowStageId,
                p.Id AS PropertyId,
                w.ZoneId,
                p.PartitionNo
            INTO #StageProperties
            FROM PTIS.PropertyWorkflowDetails pwd
            INNER JOIN PTIS.PropertyMast p ON pwd.PropertyId = p.Id
            INNER JOIN PTIS.WardMaster w ON p.WardId = w.Id
            INNER JOIN #FilteredProperties fp ON p.Id = fp.PropertyId
            WHERE pwd.WorkflowStageId IN (@DataEntryStageId, @InternalSurveyStageId, @AssessmentStageId)
              AND w.IsActive = 1;

            CREATE CLUSTERED INDEX IX_StageProperties_StageZone ON #StageProperties(WorkflowStageId, ZoneId, PropertyId);
            CREATE INDEX IX_StageProperties_Property ON #StageProperties(PropertyId, WorkflowStageId);

            SELECT
                sp.PropertyId,
                sp.ZoneId,
                sp.PartitionNo,
                p.PropertyTypeId,
                p.CategoryId,
                p.PropertyAssessmentStatusId
            INTO #DataEntryProperties
            FROM #StageProperties sp
            INNER JOIN PTIS.PropertyMast p ON sp.PropertyId = p.Id
            WHERE sp.WorkflowStageId = @DataEntryStageId;

            CREATE UNIQUE CLUSTERED INDEX IX_DataEntryProperties_Property ON #DataEntryProperties(PropertyId);
            CREATE INDEX IX_DataEntryProperties_Zone ON #DataEntryProperties(ZoneId);

            SELECT
                CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM PTIS.PropertyWorkflowStageMaster
                    WHERE IsActive = 1 AND Id = @DataEntryStageId
                )
                THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS WorkflowStageExists,
                @InternalSurveyStageId AS InternalSurveyStageId,
                @AssessmentStageId AS AssessmentStageId,
                @PropertyPhotoTypeId AS PropertyPhotoTypeId,
                @PlanPhotoTypeId AS PlanPhotoTypeId;

            SELECT ZoneId, ZoneName, ZoneNo
            FROM #SelectedZones
            ORDER BY SequenceNo, ZoneNo;

            SELECT WorkflowStageId, PropertyId, ZoneId, PartitionNo
            FROM #StageProperties;

            SELECT
                w.ZoneId,
                SUM(CASE WHEN p.PartitionNo IS NULL OR LTRIM(RTRIM(p.PartitionNo)) = '' THEN 1 ELSE 0 END) AS StructureCount,
                COUNT(1) AS UnitCount
            FROM PTIS.PropertyMast p
            INNER JOIN PTIS.WardMaster w ON p.WardId = w.Id
            INNER JOIN #FilteredProperties fp ON p.Id = fp.PropertyId
            WHERE w.IsActive = 1
            GROUP BY w.ZoneId;

            SELECT DISTINCT
                dep.PropertyId,
                pp.PhotoTypeId
            FROM #DataEntryProperties dep
            INNER JOIN PTIS.PropertyPhoto pp ON dep.PropertyId = pp.PropertyId
            WHERE pp.PhotoTypeId IN (@PropertyPhotoTypeId, @PlanPhotoTypeId)
              AND pp.IsActive = 1
              AND pp.MarkedForDeletion = 0;

            SELECT DISTINCT
                dep.PropertyId,
                dep.ZoneId,
                pt.Type AS PropertyType
            FROM #DataEntryProperties dep
            LEFT JOIN PTIS.PropertyTypeMaster pt ON dep.PropertyTypeId = pt.Id AND pt.IsActive = 1;

            SELECT
                dep.PropertyId,
                dep.ZoneId,
                tou.Type,
                tou.TypeOfUseCode
            FROM #DataEntryProperties dep
            INNER JOIN PTIS.PropertyDetails pd ON dep.PropertyId = pd.PropertyId
            INNER JOIN PTIS.TypeOfUseMaster tou ON pd.TypeOfUseId = tou.Id
            WHERE pd.IsActive = 1
              AND pd.MarkedForDeletion = 0
              AND tou.IsActive = 1;

            SELECT
                pas.Id,
                UPPER(pas.StatusName) AS StatusName
            FROM PTIS.PropertyAssessmentStatusMaster pas
            WHERE pas.IsActive = 1
              AND UPPER(pas.StatusName) IN ('ASSESSED', 'UNASSESSED', 'PARTIALLY_ASSESSED', 'UNDER_UNASSESSED');

            SELECT
                dep.ZoneId,
                dep.PropertyAssessmentStatusId AS StatusId,
                COUNT(1) AS PropertyCount,
                SUM(CASE
                    WHEN pc.Id IS NOT NULL
                     AND pc.PropertyCategoryName = 'Apartment'
                     AND dep.PartitionNo IS NOT NULL
                     AND LTRIM(RTRIM(dep.PartitionNo)) <> ''
                    THEN 1 ELSE 0 END) AS UnitsOnlyCount
            FROM #DataEntryProperties dep
            INNER JOIN PTIS.PropertyAssessmentStatusMaster pas
                ON dep.PropertyAssessmentStatusId = pas.Id
               AND pas.IsActive = 1
               AND UPPER(pas.StatusName) IN ('ASSESSED', 'UNASSESSED', 'PARTIALLY_ASSESSED', 'UNDER_UNASSESSED')
            LEFT JOIN PTIS.PropertyCategoryMaster pc ON dep.CategoryId = pc.Id AND pc.IsActive = 1
            WHERE dep.PropertyAssessmentStatusId IS NOT NULL
            GROUP BY dep.ZoneId, dep.PropertyAssessmentStatusId;
            """;

        var snapshot = new DataEntryGridSnapshotProjection();
        var connection = _context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 120;

            AddParameter(command, "@DataEntryStageId", dataEntryStageId);
            AddParameter(command, "@ZoneId", zoneId);
            AddParameter(command, "@PropertyTypeId", propertyTypeId);
            AddParameter(command, "@PropertyTypeCategoryId", propertyTypeCategoryId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await ReadSnapshotHeaderAsync(reader, snapshot, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            snapshot.Zones = await ReadZonesAsync(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            snapshot.StageProperties = await ReadStagePropertiesAsync(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            snapshot.ZoneTotals = await ReadZoneTotalsAsync(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            snapshot.CompletedPhotos = await ReadCompletedPhotosAsync(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            snapshot.PropertyTypeSources = await ReadPropertyTypeSourcesAsync(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            snapshot.PropertyUseSources = await ReadPropertyUseSourcesAsync(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            snapshot.AssessmentStatusIdsByName = await ReadAssessmentStatusesAsync(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            snapshot.AssessmentStatusCounts = await ReadAssessmentStatusCountsAsync(reader, cancellationToken);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }

        return snapshot;
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static async Task<List<(int ZoneId, string ZoneName, string ZoneNo)>> ReadZonesAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var rows = new List<(int ZoneId, string ZoneName, string ZoneNo)>();
        while (await reader.ReadAsync(cancellationToken))
            rows.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        return rows;
    }

    private static async Task ReadSnapshotHeaderAsync(
        DbDataReader reader,
        DataEntryGridSnapshotProjection snapshot,
        CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
            return;

        snapshot.WorkflowStageExists = reader.GetBoolean(0);
        snapshot.InternalSurveyStageId = reader.GetInt32(1);
        snapshot.AssessmentStageId = reader.GetInt32(2);
        snapshot.PropertyPhotoTypeId = reader.GetInt32(3);
        snapshot.PlanPhotoTypeId = reader.GetInt32(4);
    }

    private static async Task<List<DataEntryStagePropertyProjection>> ReadStagePropertiesAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var rows = new List<DataEntryStagePropertyProjection>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new DataEntryStagePropertyProjection
            {
                WorkflowStageId = reader.GetInt32(0),
                PropertyId = reader.GetInt32(1),
                ZoneId = reader.GetInt32(2),
                PartitionNo = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }

        return rows;
    }

    private static async Task<List<DataEntryZoneCountProjection>> ReadZoneTotalsAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var rows = new List<DataEntryZoneCountProjection>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new DataEntryZoneCountProjection
            {
                ZoneId = reader.GetInt32(0),
                StructureCount = reader.GetInt32(1),
                UnitCount = reader.GetInt32(2)
            });
        }

        return rows;
    }

    private static async Task<List<DataEntryCompletedPhotoProjection>> ReadCompletedPhotosAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var rows = new List<DataEntryCompletedPhotoProjection>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new DataEntryCompletedPhotoProjection
            {
                PropertyId = reader.GetInt32(0),
                PhotoTypeId = reader.GetInt32(1)
            });
        }

        return rows;
    }

    private static async Task<List<DataEntryPropertyTypeSourceProjection>> ReadPropertyTypeSourcesAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var rows = new List<DataEntryPropertyTypeSourceProjection>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new DataEntryPropertyTypeSourceProjection
            {
                PropertyId = reader.GetInt32(0),
                ZoneId = reader.GetInt32(1),
                PropertyType = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }

        return rows;
    }

    private static async Task<List<DataEntryPropertyUseSourceProjection>> ReadPropertyUseSourcesAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var rows = new List<DataEntryPropertyUseSourceProjection>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new DataEntryPropertyUseSourceProjection
            {
                PropertyId = reader.GetInt32(0),
                ZoneId = reader.GetInt32(1),
                Type = reader.IsDBNull(2) ? null : reader.GetString(2),
                TypeOfUseCode = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }

        return rows;
    }

    private static async Task<Dictionary<string, int>> ReadAssessmentStatusesAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var rows = new Dictionary<string, int>();
        while (await reader.ReadAsync(cancellationToken))
            rows[reader.GetString(1)] = reader.GetInt32(0);
        return rows;
    }

    private static async Task<List<DataEntryAssessmentStatusCountProjection>> ReadAssessmentStatusCountsAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var rows = new List<DataEntryAssessmentStatusCountProjection>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new DataEntryAssessmentStatusCountProjection
            {
                ZoneId = reader.GetInt32(0),
                StatusId = reader.GetInt32(1),
                PropertyCount = reader.GetInt32(2),
                UnitsOnlyCount = reader.GetInt32(3)
            });
        }

        return rows;
    }

    public async Task<DataEntryWardWiseSummaryProjection> ReadDataEntryWardWiseSummaryAsync(
        WardWiseSummaryQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var zoneId = queryParameters.ZoneId;
        var workflowStageId = queryParameters.WorkflowStageId;
        var context = await GetWardWiseSummaryContextAsync(
            zoneId,
            workflowStageId,
            queryParameters.PageNumber,
            queryParameters.PageSize,
            cancellationToken);
        if (!context.IsValid)
            return new DataEntryWardWiseSummaryProjection
            {
                PageNumber = context.PageNumber,
                PageSize = context.PageSize
            };

        var internalSurveyStageId = await GetInternalSurveyStageIdAsync(cancellationToken);
        var assessmentStageId = await GetAssessmentStageIdAsync(cancellationToken);
        var propertyPhotoTypeId = await GetPhotoTypeIdAsync(PropertyPhotoTypeCode, cancellationToken);
        var planPhotoTypeId = await GetPhotoTypeIdAsync(PlanPhotoTypeCode, cancellationToken);

        return await ReadDataEntryWardDataBatchAsync(
            context.Wards,
            context.ZoneId,
            context.ZoneName,
            context.PageNumber,
            context.PageSize,
            context.TotalCount,
            workflowStageId,
            internalSurveyStageId,
            assessmentStageId,
            propertyPhotoTypeId,
            planPhotoTypeId,
            cancellationToken);
    }

    private async Task<DataEntryWardWiseSummaryProjection> ReadDataEntryWardDataBatchAsync(
        List<(int WardId, string WardNo)> wards,
        int zoneId,
        string zoneName,
        int pageNumber,
        int pageSize,
        int totalCount,
        int dataEntryStageId,
        int internalSurveyStageId,
        int assessmentStageId,
        int propertyPhotoTypeId,
        int planPhotoTypeId,
        CancellationToken cancellationToken)
    {
        var wardIds = wards.Select(w => w.WardId).ToList();
        if (!wardIds.Any())
            return new DataEntryWardWiseSummaryProjection
            {
                ZoneId = zoneId,
                ZoneName = zoneName,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                DataEntryStageId = dataEntryStageId,
                InternalSurveyStageId = internalSurveyStageId,
                AssessmentStageId = assessmentStageId,
                PropertyPhotoTypeId = propertyPhotoTypeId,
                PlanPhotoTypeId = planPhotoTypeId,
                Wards = wards
            };

        var stageIds = new[] { dataEntryStageId, internalSurveyStageId, assessmentStageId }
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var stageRows = (await (
            from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
            join p in _context.PropertyMast.AsNoTracking() on pwd.PropertyId equals p.Id
            join pc in _context.PropertyCategoryMaster.AsNoTracking() on p.CategoryId equals pc.Id into categoryJoin
            from pc in categoryJoin.Where(x => x.IsActive).DefaultIfEmpty()
            where stageIds.Contains(pwd.WorkflowStageId)
                  && p.IsActive
                  && !p.MarkedForDeletion
                  && wardIds.Contains(p.WardId)
            select new DataEntryWardStageProjection
            {
                PropertyId = p.Id,
                WorkflowStageId = pwd.WorkflowStageId,
                WardId = p.WardId,
                PartitionNo = p.PartitionNo,
                PropertyTypeId = p.PropertyTypeId,
                CategoryId = p.CategoryId,
                CategoryName = pc == null ? null : pc.PropertyCategoryName,
                PropertyAssessmentStatusId = p.PropertyAssessmentStatusId
            })
            .ToListAsync(cancellationToken))
            .GroupBy(row => new { row.PropertyId, row.WorkflowStageId })
            .Select(group => group.First())
            .ToList();

        var wardTotalRows = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive
                     && !p.MarkedForDeletion
                     && wardIds.Contains(p.WardId))
            .GroupBy(p => p.WardId)
            .Select(g => new DataEntryWardCountProjection
            {
                WardId = g.Key,
                StructureCount = g.Count(p => p.PartitionNo == null || p.PartitionNo == ""),
                UnitCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        var dataEntryRows = stageRows
            .Where(r => r.WorkflowStageId == dataEntryStageId)
            .ToList();
        var dataEntryIds = dataEntryRows.Select(r => r.PropertyId).Distinct().ToList();

        var completedPhotoRows = dataEntryIds.Any()
            ? await (
                from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
                join p in _context.PropertyMast.AsNoTracking() on pwd.PropertyId equals p.Id
                join pp in _context.PropertyPhotos.AsNoTracking() on p.Id equals pp.PropertyId
                where pwd.WorkflowStageId == dataEntryStageId
                      && p.IsActive
                      && !p.MarkedForDeletion
                      && wardIds.Contains(p.WardId)
                      && (pp.PhotoTypeId == propertyPhotoTypeId || pp.PhotoTypeId == planPhotoTypeId)
                      && pp.IsActive
                      && !pp.MarkedForDeletion
                select new DataEntryCompletedPhotoProjection
                {
                    PropertyId = pp.PropertyId,
                    PhotoTypeId = pp.PhotoTypeId
                })
                .Distinct()
                .ToListAsync(cancellationToken)
            : new List<DataEntryCompletedPhotoProjection>();

        var propertyTypeRows = dataEntryIds.Any()
            ? await (
                from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
                join p in _context.PropertyMast.AsNoTracking() on pwd.PropertyId equals p.Id
                join pt in _context.PropertyTypeMasters.AsNoTracking() on p.PropertyTypeId equals pt.Id into propertyTypeJoin
                from pt in propertyTypeJoin.Where(x => x.IsActive).DefaultIfEmpty()
                where pwd.WorkflowStageId == dataEntryStageId
                      && p.IsActive
                      && !p.MarkedForDeletion
                      && wardIds.Contains(p.WardId)
                select new DataEntryPropertyTypeSourceProjection
                {
                    PropertyId = p.Id,
                    ZoneId = p.WardId,
                    PropertyType = pt == null ? null : pt.Type
                })
                .ToListAsync(cancellationToken)
            : new List<DataEntryPropertyTypeSourceProjection>();

        var propertyUseRows = dataEntryIds.Any()
            ? await (
                from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
                join p in _context.PropertyMast.AsNoTracking() on pwd.PropertyId equals p.Id
                join pd in _context.PropertyDetails.AsNoTracking() on p.Id equals pd.PropertyId
                join tou in _context.TypeOfUse.AsNoTracking() on pd.TypeOfUseId equals tou.Id
                where pwd.WorkflowStageId == dataEntryStageId
                      && p.IsActive
                      && !p.MarkedForDeletion
                      && wardIds.Contains(p.WardId)
                      && pd.IsActive
                      && !pd.MarkedForDeletion
                      && tou.IsActive
                select new DataEntryPropertyUseSourceProjection
                {
                    PropertyId = pd.PropertyId,
                    ZoneId = p.WardId,
                    Type = tou.Type,
                    TypeOfUseCode = tou.TypeOfUseCode
                })
                .ToListAsync(cancellationToken)
            : new List<DataEntryPropertyUseSourceProjection>();

        var statusIdsByName = await ReadAssessmentStatusIdsByNameAsync(cancellationToken);

        return new DataEntryWardWiseSummaryProjection
        {
            ZoneId = zoneId,
            ZoneName = zoneName,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            DataEntryStageId = dataEntryStageId,
            InternalSurveyStageId = internalSurveyStageId,
            AssessmentStageId = assessmentStageId,
            PropertyPhotoTypeId = propertyPhotoTypeId,
            PlanPhotoTypeId = planPhotoTypeId,
            Wards = wards,
            StageRows = stageRows,
            WardTotalRows = wardTotalRows,
            CompletedPhotoRows = completedPhotoRows,
            PropertyTypeRows = propertyTypeRows,
            PropertyUseRows = propertyUseRows,
            AssessmentStatusIdsByName = statusIdsByName
        };
    }

    private async Task<Dictionary<string, int>> ReadAssessmentStatusIdsByNameAsync(CancellationToken cancellationToken)
    {
        var statusNames = new[] { "ASSESSED", "UNASSESSED", "PARTIALLY_ASSESSED", "UNDER_UNASSESSED" };
        return await _context.PropertyAssessmentStatuses
            .AsNoTracking()
            .Where(s => s.IsActive && statusNames.Contains(s.StatusName.ToUpper()))
            .Select(s => new { s.Id, StatusName = s.StatusName.ToUpper() })
            .ToDictionaryAsync(s => s.StatusName, s => s.Id, cancellationToken);
    }

    private Task<int> GetInternalSurveyStageIdAsync(CancellationToken cancellationToken)
        => GetStageIdByNameAsync(InternalSurveyStageName, cancellationToken);

    private Task<int> GetAssessmentStageIdAsync(CancellationToken cancellationToken)
        => GetStageIdByNameAsync(AssessmentStageName, cancellationToken);

    #endregion
}


