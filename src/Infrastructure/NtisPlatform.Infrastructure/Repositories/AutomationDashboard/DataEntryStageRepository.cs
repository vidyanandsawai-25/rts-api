using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Infrastructure.Data;
using System.Data;
using System.Data.Common;

namespace NtisPlatform.Infrastructure.Repositories.AutomationDashboard;

/// <summary>
/// Repository for Data Entry and Quality Analyst stage operations.
/// Handles division-wise grid data for Data Entry and Quality Analyst workflow stage ONLY.
/// </summary>
public class DataEntryStageRepository : WorkflowStageBaseRepository, IDataEntryStageRepository
{
    public DataEntryStageRepository(ApplicationDbContext context) : base(context)
    {
    }

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

            SELECT ZoneId, ZoneName
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

    private static async Task<List<(int ZoneId, string ZoneName)>> ReadZonesAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var rows = new List<(int ZoneId, string ZoneName)>();
        while (await reader.ReadAsync(cancellationToken))
            rows.Add((reader.GetInt32(0), reader.GetString(1)));
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

    public async Task<DataEntryWardWiseSummaryResponseDto> GetDataEntryWardWiseSummaryAsync(
        int zoneId,
        int workflowStageId,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var context = await GetWardWiseSummaryContextAsync(zoneId, workflowStageId, pageNumber, pageSize, cancellationToken);
        if (!context.IsValid)
            return new DataEntryWardWiseSummaryResponseDto();

        var internalSurveyStageId = await GetInternalSurveyStageIdAsync(cancellationToken);
        var assessmentStageId = await GetAssessmentStageIdAsync(cancellationToken);
        var propertyPhotoTypeId = await GetPhotoTypeIdAsync(PropertyPhotoTypeCode, cancellationToken);
        var planPhotoTypeId = await GetPhotoTypeIdAsync(PlanPhotoTypeCode, cancellationToken);

        var result = new DataEntryWardWiseSummaryResponseDto
        {
            ZoneId = context.ZoneId,
            ZoneName = context.ZoneName,
            PageNumber = context.PageNumber,
            PageSize = context.PageSize,
            TotalCount = context.TotalCount
        };

        var allWardData = await GetDataEntryWardDataBatchAsync(
            context.Wards,
            workflowStageId,
            internalSurveyStageId,
            assessmentStageId,
            propertyPhotoTypeId,
            planPhotoTypeId,
            cancellationToken);

        var orderedWardData = allWardData
            .OrderByDescending(HasWardSummaryData)
            .ToList();

        result.TotalRow = CalculateWardTotals(allWardData);
        result.WardData = PageWardData(orderedWardData, context.PageNumber, context.PageSize);
        return result;
    }

    private async Task<List<DataEntryWardDataDto>> GetDataEntryWardDataBatchAsync(
        List<(int WardId, string WardNo)> wards,
        int dataEntryStageId,
        int internalSurveyStageId,
        int assessmentStageId,
        int propertyPhotoTypeId,
        int planPhotoTypeId,
        CancellationToken cancellationToken)
    {
        var wardIds = wards.Select(w => w.WardId).ToList();
        if (!wardIds.Any())
            return new List<DataEntryWardDataDto>();

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
            select new DataEntryWardStageRow
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
            .Select(g => new DataEntryWardCountRow
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
                select new DataEntryWardPhotoRow
                {
                    PropertyId = pp.PropertyId,
                    PhotoTypeId = pp.PhotoTypeId
                })
                .Distinct()
                .ToListAsync(cancellationToken)
            : new List<DataEntryWardPhotoRow>();

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

        return BuildDataEntryWardRows(
            wards,
            dataEntryStageId,
            internalSurveyStageId,
            assessmentStageId,
            propertyPhotoTypeId,
            planPhotoTypeId,
            stageRows,
            wardTotalRows,
            completedPhotoRows,
            propertyTypeRows,
            propertyUseRows,
            statusIdsByName);
    }

    private static List<DataEntryWardDataDto> BuildDataEntryWardRows(
        List<(int WardId, string WardNo)> wards,
        int dataEntryStageId,
        int internalSurveyStageId,
        int assessmentStageId,
        int propertyPhotoTypeId,
        int planPhotoTypeId,
        List<DataEntryWardStageRow> stageRows,
        List<DataEntryWardCountRow> wardTotalRows,
        List<DataEntryWardPhotoRow> completedPhotoRows,
        List<DataEntryPropertyTypeSourceProjection> propertyTypeRows,
        List<DataEntryPropertyUseSourceProjection> propertyUseRows,
        Dictionary<string, int> statusIdsByName)
    {
        var dataEntryRows = stageRows.Where(r => r.WorkflowStageId == dataEntryStageId).ToList();
        var internalSurveyRows = stageRows.Where(r => r.WorkflowStageId == internalSurveyStageId).ToList();
        var assessmentPropertyIds = assessmentStageId == 0
            ? new HashSet<int>()
            : stageRows
                .Where(r => r.WorkflowStageId == assessmentStageId)
                .Select(r => r.PropertyId)
                .ToHashSet();
        var dataEntryCountsByWard = CountByWard(dataEntryRows);
        var internalCountsByWard = CountByWard(internalSurveyRows);
        var wardTotalsByWard = wardTotalRows.ToDictionary(r => r.WardId, r => (r.StructureCount, r.UnitCount));
        var dataEntryPropertyIdsByWard = dataEntryRows
            .GroupBy(r => r.WardId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.PropertyId).Distinct().ToHashSet());
        var dataEntryRowsByPropertyId = dataEntryRows
            .GroupBy(r => r.PropertyId)
            .ToDictionary(g => g.Key, g => g.First());
        var photoCompleteByWard = CountCompletedPhotosByWard(
            completedPhotoRows,
            dataEntryRowsByPropertyId,
            propertyPhotoTypeId);
        var planCompleteByWard = CountCompletedPhotosByWard(
            completedPhotoRows,
            dataEntryRowsByPropertyId,
            planPhotoTypeId);
        var qaCompletedCountsByWard = assessmentStageId == 0
            ? new Dictionary<int, (int StructureCount, int UnitCount)>()
            : CountByWard(dataEntryRows.Where(r => assessmentPropertyIds.Contains(r.PropertyId)));
        var qaPendingCountsByWard = assessmentStageId == 0
            ? dataEntryCountsByWard
            : CountByWard(dataEntryRows.Where(r => !assessmentPropertyIds.Contains(r.PropertyId)));
        var propertyTypeByWard = BuildPropertyTypeBreakdownByWard(
            wards.Select(w => w.WardId).ToList(),
            propertyTypeRows,
            propertyUseRows);
        var assessmentStatusByWard = BuildAssessmentStatusBreakdownByWard(
            wards.Select(w => w.WardId).ToList(),
            dataEntryRows,
            statusIdsByName);

        return wards
            .Select(ward =>
            {
                var dataEntryCounts = dataEntryCountsByWard.GetValueOrDefault(ward.WardId);
                var internalCounts = internalCountsByWard.GetValueOrDefault(ward.WardId);
                var totalCounts = wardTotalsByWard.GetValueOrDefault(ward.WardId);
                var qaCompletedCounts = qaCompletedCountsByWard.GetValueOrDefault(ward.WardId);
                var qaPendingCounts = qaPendingCountsByWard.GetValueOrDefault(ward.WardId);
                var dataEntryPropertyCount = dataEntryPropertyIdsByWard.TryGetValue(ward.WardId, out var propertyIds)
                    ? propertyIds.Count
                    : 0;
                var photoComplete = propertyPhotoTypeId == 0 ? 0 : photoCompleteByWard.GetValueOrDefault(ward.WardId);
                var planComplete = planPhotoTypeId == 0 ? 0 : planCompleteByWard.GetValueOrDefault(ward.WardId);

                return new DataEntryWardDataDto
                {
                    WardId = ward.WardId,
                    WardNo = ward.WardNo,
                    Structure = dataEntryCounts.StructureCount,
                    Unit = dataEntryCounts.UnitCount,
                    InternalSurvey = new InternalSurveyBreakdownDto
                    {
                        Structure = internalCounts.StructureCount,
                        Unit = internalCounts.UnitCount
                    },
                    DataEntry = new DataEntryBreakdownDto
                    {
                        CompletedStructure = dataEntryCounts.StructureCount,
                        CompletedUnit = dataEntryCounts.UnitCount,
                        PendingStructure = Math.Max(0, totalCounts.StructureCount - dataEntryCounts.StructureCount),
                        PendingUnit = Math.Max(0, totalCounts.UnitCount - dataEntryCounts.UnitCount)
                    },
                    Photo = new PhotoBreakdownDto
                    {
                        Complete = photoComplete,
                        Pending = propertyPhotoTypeId == 0 ? dataEntryPropertyCount : dataEntryPropertyCount - photoComplete
                    },
                    Plan = new PlanBreakdownDto
                    {
                        Complete = planComplete,
                        Pending = planPhotoTypeId == 0 ? dataEntryPropertyCount : dataEntryPropertyCount - planComplete
                    },
                    QualityAnalyst = new QualityAnalystBreakdownDto
                    {
                        CompletedStructure = assessmentStageId == 0 ? 0 : qaCompletedCounts.StructureCount,
                        CompletedUnit = assessmentStageId == 0 ? 0 : qaCompletedCounts.UnitCount,
                        PendingStructure = assessmentStageId == 0 ? dataEntryCounts.StructureCount : qaPendingCounts.StructureCount,
                        PendingUnit = assessmentStageId == 0 ? dataEntryCounts.UnitCount : qaPendingCounts.UnitCount
                    },
                    PropertyType = propertyTypeByWard.GetValueOrDefault(ward.WardId) ?? new DataEntryPropertyTypeBreakdownDto(),
                    AssessmentStatusBreakdown = assessmentStatusByWard.GetValueOrDefault(ward.WardId) ?? new AssessmentStatusBreakdownDto()
                };
            })
            .ToList();
    }

    private static Dictionary<int, (int StructureCount, int UnitCount)> CountByWard(IEnumerable<DataEntryWardStageRow> rows)
        => rows
            .GroupBy(r => r.WardId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var distinctRows = g
                        .GroupBy(r => r.PropertyId)
                        .Select(group => group.First())
                        .ToList();

                    return (
                        distinctRows.Count(r => string.IsNullOrWhiteSpace(r.PartitionNo)),
                        distinctRows.Count);
                });

    private static Dictionary<int, int> CountCompletedPhotosByWard(
        List<DataEntryWardPhotoRow> completedPhotoRows,
        Dictionary<int, DataEntryWardStageRow> dataEntryRowsByPropertyId,
        int photoTypeId)
    {
        if (photoTypeId == 0)
            return new Dictionary<int, int>();

        return completedPhotoRows
            .Where(row => row.PhotoTypeId == photoTypeId
                          && dataEntryRowsByPropertyId.ContainsKey(row.PropertyId))
            .GroupBy(row => dataEntryRowsByPropertyId[row.PropertyId].WardId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(row => row.PropertyId).Distinct().Count());
    }

    private static Dictionary<int, DataEntryPropertyTypeBreakdownDto> BuildPropertyTypeBreakdownByWard(
        List<int> wardIds,
        List<DataEntryPropertyTypeSourceProjection> properties,
        List<DataEntryPropertyUseSourceProjection> details)
    {
        var result = wardIds.ToDictionary(id => id, _ => new DataEntryPropertyTypeBreakdownDto());
        var mixedTypes = new HashSet<string>(new[] { "R-C", "C-R", "C-I", "I-C", "I-R", "R-I" }, StringComparer.OrdinalIgnoreCase);
        var mixedPropertyIds = properties
            .Where(p => p.PropertyType != null && mixedTypes.Contains(p.PropertyType))
            .Select(p => p.PropertyId)
            .ToHashSet();

        foreach (var wardGroup in properties.Where(p => mixedPropertyIds.Contains(p.PropertyId)).GroupBy(p => p.ZoneId))
            result[wardGroup.Key].Mixed = wardGroup.Select(p => p.PropertyId).Distinct().Count();

        var remainingPropertiesByWard = properties
            .Where(p => !mixedPropertyIds.Contains(p.PropertyId))
            .GroupBy(p => p.ZoneId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.PropertyId).Distinct().ToHashSet());
        var detailGroups = details
            .Where(d => !mixedPropertyIds.Contains(d.PropertyId))
            .GroupBy(d => new { WardId = d.ZoneId, d.PropertyId })
            .ToList();
        var propertiesWithDetailsByWard = detailGroups
            .GroupBy(g => g.Key.WardId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Key.PropertyId).Distinct().Count());

        foreach (var group in detailGroups)
        {
            var types = group.Select(x => x.Type?.Trim().ToUpperInvariant()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            var codes = group.Select(x => x.TypeOfUseCode?.Trim().ToUpperInvariant()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            var breakdown = result[group.Key.WardId];

            if (codes.Any(code => code == "UC"))
                continue;
            if (types.Any(type => type == "N" || type == "I"))
                breakdown.PublicUtility++;
            else if (types.Any(type => type == "R"))
                breakdown.Residential++;
            else if (types.Any(type => type == "C"))
                breakdown.NonResidential++;
        }

        foreach (var (wardId, propertyIds) in remainingPropertiesByWard)
        {
            var propertiesWithDetails = propertiesWithDetailsByWard.GetValueOrDefault(wardId);
            result[wardId].Residential += Math.Max(0, propertyIds.Count - propertiesWithDetails);
        }

        return result;
    }

    private static Dictionary<int, AssessmentStatusBreakdownDto> BuildAssessmentStatusBreakdownByWard(
        List<int> wardIds,
        List<DataEntryWardStageRow> dataEntryRows,
        Dictionary<string, int> statusIdsByName)
    {
        var statusIds = statusIdsByName.Values.ToHashSet();
        var countsByWardAndStatus = dataEntryRows
            .Where(row => row.PropertyAssessmentStatusId.HasValue && statusIds.Contains(row.PropertyAssessmentStatusId.Value))
            .GroupBy(row => row.WardId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .GroupBy(row => row.PropertyAssessmentStatusId!.Value)
                    .ToDictionary(
                        sg => sg.Key,
                        sg =>
                        {
                            var rows = sg.ToList();
                            var unitCount = rows.Count(IsApartmentUnit);
                            return (StructureCount: rows.Count - unitCount, UnitCount: unitCount);
                        }));

        return wardIds.ToDictionary(
            wardId => wardId,
            wardId =>
            {
                countsByWardAndStatus.TryGetValue(wardId, out var wardCounts);
                wardCounts ??= new Dictionary<int, (int StructureCount, int UnitCount)>();
                return new AssessmentStatusBreakdownDto
                {
                    Assessed = GetStatusCounts(statusIdsByName, wardCounts, "ASSESSED"),
                    Unassessed = GetStatusCounts(statusIdsByName, wardCounts, "UNASSESSED"),
                    NewlyAssessedFound = GetStatusCounts(statusIdsByName, wardCounts, "PARTIALLY_ASSESSED"),
                    AssessmentInProcess = GetStatusCounts(statusIdsByName, wardCounts, "UNDER_UNASSESSED")
                };
            });
    }

    private static bool IsApartmentUnit(DataEntryWardStageRow row)
        => row.CategoryName == ApartmentCategoryName
           && !string.IsNullOrWhiteSpace(row.PartitionNo);

    private static StructureUnitCountDto GetStatusCounts(
        Dictionary<string, int> statusIdsByName,
        Dictionary<int, (int StructureCount, int UnitCount)> countsByStatusId,
        string statusName)
    {
        if (!statusIdsByName.TryGetValue(statusName, out var statusId) ||
            !countsByStatusId.TryGetValue(statusId, out var counts))
        {
            return new StructureUnitCountDto();
        }

        return new StructureUnitCountDto
        {
            StructureCount = counts.StructureCount,
            UnitCount = counts.UnitCount
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

    private static bool HasWardSummaryData(DataEntryWardDataDto ward)
        => ward.Structure > 0
           || ward.Unit > 0
           || ward.InternalSurvey.Structure > 0
           || ward.InternalSurvey.Unit > 0
           || ward.DataEntry.CompletedStructure > 0
           || ward.DataEntry.CompletedUnit > 0
           || ward.DataEntry.PendingStructure > 0
           || ward.DataEntry.PendingUnit > 0
           || ward.Photo.Complete > 0
           || ward.Photo.Pending > 0
           || ward.Plan.Complete > 0
           || ward.Plan.Pending > 0
           || ward.QualityAnalyst.CompletedStructure > 0
           || ward.QualityAnalyst.CompletedUnit > 0
           || ward.QualityAnalyst.PendingStructure > 0
           || ward.QualityAnalyst.PendingUnit > 0
           || ward.PropertyType.Residential > 0
           || ward.PropertyType.NonResidential > 0
           || ward.PropertyType.Mixed > 0
           || ward.PropertyType.PublicUtility > 0
           || ward.AssessmentStatusBreakdown.Assessed.StructureCount > 0
           || ward.AssessmentStatusBreakdown.Assessed.UnitCount > 0
           || ward.AssessmentStatusBreakdown.Unassessed.StructureCount > 0
           || ward.AssessmentStatusBreakdown.Unassessed.UnitCount > 0
           || ward.AssessmentStatusBreakdown.NewlyAssessedFound.StructureCount > 0
           || ward.AssessmentStatusBreakdown.NewlyAssessedFound.UnitCount > 0
           || ward.AssessmentStatusBreakdown.AssessmentInProcess.StructureCount > 0
           || ward.AssessmentStatusBreakdown.AssessmentInProcess.UnitCount > 0;

    private DataEntryWardDataDto CalculateWardTotals(List<DataEntryWardDataDto> wardData)
    {
        return new DataEntryWardDataDto
        {
            WardNo = "TOTAL",
            Structure = wardData.Sum(w => w.Structure),
            Unit = wardData.Sum(w => w.Unit),
            InternalSurvey = new InternalSurveyBreakdownDto
            {
                Structure = wardData.Sum(w => w.InternalSurvey.Structure),
                Unit = wardData.Sum(w => w.InternalSurvey.Unit)
            },
            DataEntry = new DataEntryBreakdownDto
            {
                CompletedStructure = wardData.Sum(w => w.DataEntry.CompletedStructure),
                CompletedUnit = wardData.Sum(w => w.DataEntry.CompletedUnit),
                PendingStructure = wardData.Sum(w => w.DataEntry.PendingStructure),
                PendingUnit = wardData.Sum(w => w.DataEntry.PendingUnit)
            },
            Photo = new PhotoBreakdownDto
            {
                Complete = wardData.Sum(w => w.Photo.Complete),
                Pending = wardData.Sum(w => w.Photo.Pending)
            },
            Plan = new PlanBreakdownDto
            {
                Complete = wardData.Sum(w => w.Plan.Complete),
                Pending = wardData.Sum(w => w.Plan.Pending)
            },
            QualityAnalyst = new QualityAnalystBreakdownDto
            {
                CompletedStructure = wardData.Sum(w => w.QualityAnalyst.CompletedStructure),
                CompletedUnit = wardData.Sum(w => w.QualityAnalyst.CompletedUnit),
                PendingStructure = wardData.Sum(w => w.QualityAnalyst.PendingStructure),
                PendingUnit = wardData.Sum(w => w.QualityAnalyst.PendingUnit)
            },
            PropertyType = new DataEntryPropertyTypeBreakdownDto
            {
                Residential = wardData.Sum(w => w.PropertyType.Residential),
                NonResidential = wardData.Sum(w => w.PropertyType.NonResidential),
                Mixed = wardData.Sum(w => w.PropertyType.Mixed),
                PublicUtility = wardData.Sum(w => w.PropertyType.PublicUtility)
            },
            AssessmentStatusBreakdown = new AssessmentStatusBreakdownDto
            {
                Assessed = new StructureUnitCountDto
                {
                    StructureCount = wardData.Sum(w => w.AssessmentStatusBreakdown.Assessed.StructureCount),
                    UnitCount = wardData.Sum(w => w.AssessmentStatusBreakdown.Assessed.UnitCount)
                },
                Unassessed = new StructureUnitCountDto
                {
                    StructureCount = wardData.Sum(w => w.AssessmentStatusBreakdown.Unassessed.StructureCount),
                    UnitCount = wardData.Sum(w => w.AssessmentStatusBreakdown.Unassessed.UnitCount)
                },
                NewlyAssessedFound = new StructureUnitCountDto
                {
                    StructureCount = wardData.Sum(w => w.AssessmentStatusBreakdown.NewlyAssessedFound.StructureCount),
                    UnitCount = wardData.Sum(w => w.AssessmentStatusBreakdown.NewlyAssessedFound.UnitCount)
                },
                AssessmentInProcess = new StructureUnitCountDto
                {
                    StructureCount = wardData.Sum(w => w.AssessmentStatusBreakdown.AssessmentInProcess.StructureCount),
                    UnitCount = wardData.Sum(w => w.AssessmentStatusBreakdown.AssessmentInProcess.UnitCount)
                }
            }
        };
    }

    private sealed class DataEntryWardStageRow
    {
        public int PropertyId { get; set; }
        public int WorkflowStageId { get; set; }
        public int WardId { get; set; }
        public string? PartitionNo { get; set; }
        public int? PropertyTypeId { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? PropertyAssessmentStatusId { get; set; }
    }

    private sealed class DataEntryWardCountRow
    {
        public int WardId { get; set; }
        public int StructureCount { get; set; }
        public int UnitCount { get; set; }
    }

    private sealed class DataEntryWardPhotoRow
    {
        public int PropertyId { get; set; }
        public int PhotoTypeId { get; set; }
    }
}
