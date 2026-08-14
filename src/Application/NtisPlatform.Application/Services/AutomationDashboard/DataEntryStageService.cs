using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Application.Helpers.AutomationDashboard;
using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services.AutomationDashboard;

/// <summary>
/// Service for Data Entry dashboard grid assembly and summary rules.
/// Implements optimized data aggregation with proper exception handling and logging.
/// </summary>
public class DataEntryStageService : IDataEntryStageService
{
    private readonly IDataEntryStageRepository _repository;
    private readonly ILogger<DataEntryStageService> _logger;

    public DataEntryStageService(
        IDataEntryStageRepository repository,
        ILogger<DataEntryStageService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Builds the Data Entry grid while keeping aggregation rules in the service.
    /// Implements sequential data fetching and proper exception handling to avoid DbContext concurrency issues.
    /// </summary>
    public async Task<DataEntryGridResponseDto> GetDataEntryGridDataAsync(
        DashboardGridQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dataEntryStageId = queryParameters.WorkflowStageId!.Value;

            // Fetch zones
            var zones = await _repository.ReadZonesAsync(null, cancellationToken);
            if (!zones.Any())
            {
                _logger.LogInformation("No active zones found for request");
                return new DataEntryGridResponseDto
                {
                    DivisionData = new List<DataEntryDivisionDataDto>(),
                    TotalRow = new DataEntryDivisionDataDto { DivisionName = "TOTAL" }
                };
            }

            var zoneIds = zones.Select(z => z.ZoneId).ToList();

            // Fetch all required data sequentially to avoid DbContext concurrency issues
            var (dataEntryProperties, internalSurveyProperties, assessmentProperties, zoneTotals,
                 photoCompleteIds, planCompleteIds, propertyTypes, propertyUses, assessmentStatusIdsByName,
                 assessmentStatusCounts, internalSurveyStageId, assessmentStageId, propertyPhotoTypeId,
                 planPhotoTypeId) = await FetchGridDataAsync(queryParameters, zoneIds, cancellationToken);

            // Build property type breakdown using common helper
            var propertyTypeBreakdown = BuildPropertyTypeBreakdown(
                zoneIds,
                propertyTypes,
                propertyUses);

            // Build assessment status breakdown using common helper
            var assessmentStatusBreakdown = BuildAssessmentStatusBreakdown(
                zoneIds,
                assessmentStatusIdsByName,
                assessmentStatusCounts);

            // Build grid
            var result = BuildGrid(
                zones,
                dataEntryProperties,
                internalSurveyProperties,
                assessmentProperties,
                zoneTotals,
                photoCompleteIds,
                planCompleteIds,
                propertyTypeBreakdown,
                assessmentStatusBreakdown,
                assessmentStageId,
                propertyPhotoTypeId,
                planPhotoTypeId);

            // Calculate totals
            result.TotalRow = CalculateTotals(
                result.DivisionData,
                result.DivisionData.Sum(d => d.Structure),
                result.DivisionData.Sum(d => d.Unit));

            _logger.LogInformation(
                "Successfully retrieved Data Entry grid data for stage {WorkflowStageId} with {ZoneCount} zones",
                dataEntryStageId, zones.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Data Entry grid data");
            throw new ApplicationException("An error occurred while retrieving Data Entry grid data", ex);
        }
    }

    /// <summary>
    /// Builds Data Entry ward-wise summary from a repository data snapshot.
    /// </summary>
    public async Task<DataEntryWardWiseSummaryResponseDto> GetDataEntryWardWiseSummaryAsync(
        WardWiseSummaryQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await _repository.ReadDataEntryWardWiseSummaryAsync(
                queryParameters,
                cancellationToken);

            var allWardData = BuildDataEntryWardRows(snapshot);
            var orderedWardData = allWardData
                .OrderByDescending(GetWardSummaryScore)
                .ThenBy(w => w.WardNo)
                .ToList();

            return new DataEntryWardWiseSummaryResponseDto
            {
                ZoneId = snapshot.ZoneId,
                ZoneName = snapshot.ZoneName,
                PageNumber = snapshot.PageNumber,
                PageSize = snapshot.PageSize,
                TotalCount = snapshot.TotalCount,
                TotalRow = CalculateWardTotals(allWardData),
                WardData = WorkflowStagePagingHelper.PageWardData(orderedWardData, snapshot.PageNumber, snapshot.PageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving Data Entry ward-wise summary for zone {ZoneId}, stage {WorkflowStageId}",
                queryParameters.ZoneId,
                queryParameters.WorkflowStageId);
            throw new ApplicationException("An error occurred while retrieving Data Entry ward-wise summary", ex);
        }
    }

    /// <summary>
    /// Fetches all required data for grid display sequentially to avoid DbContext concurrency issues
    /// </summary>
    private async Task<(
        List<DataEntryStagePropertyProjection> DataEntryProperties,
        List<DataEntryStagePropertyProjection> InternalSurveyProperties,
        List<DataEntryStagePropertyProjection> AssessmentProperties,
        Dictionary<int, (int StructureCount, int UnitCount)> ZoneTotals,
        HashSet<int> PhotoCompleteIds,
        HashSet<int> PlanCompleteIds,
        List<DataEntryPropertyTypeSourceProjection> PropertyTypes,
        List<DataEntryPropertyUseSourceProjection> PropertyUses,
        Dictionary<string, int> AssessmentStatusIdsByName,
        List<DataEntryAssessmentStatusCountProjection> AssessmentStatusCounts,
        int InternalSurveyStageId,
        int AssessmentStageId,
        int PropertyPhotoTypeId,
        int PlanPhotoTypeId)> FetchGridDataAsync(
        DashboardGridQueryParameters queryParameters,
        List<int> zoneIds,
        CancellationToken cancellationToken)
    {
        var dataEntryStageId = queryParameters.WorkflowStageId!.Value;

        // Execute sequentially to avoid DbContext concurrency issues
        var internalSurveyStageId = await _repository.ReadInternalSurveyStageIdAsync(cancellationToken);
        var assessmentStageId = await _repository.ReadAssessmentStageIdAsync(cancellationToken);
        var propertyPhotoTypeId = await _repository.ReadPropertyPhotoTypeIdAsync(cancellationToken);
        var planPhotoTypeId = await _repository.ReadPlanPhotoTypeIdAsync(cancellationToken);

        var dataEntryProperties = await _repository.ReadStagePropertiesForZonesAsync(
            dataEntryStageId, zoneIds, cancellationToken, queryParameters);
        var internalSurveyProperties = await _repository.ReadStagePropertiesForZonesAsync(
            internalSurveyStageId, zoneIds, cancellationToken, queryParameters);
        var assessmentProperties = await _repository.ReadStagePropertiesForZonesAsync(
            assessmentStageId, zoneIds, cancellationToken, queryParameters);

        var zoneTotals = await _repository.ReadZoneTotalsAsync(zoneIds, cancellationToken, queryParameters);
        var completedPhotos = await _repository.ReadCompletedPhotosAsync(
            dataEntryStageId, zoneIds, propertyPhotoTypeId, planPhotoTypeId, cancellationToken, queryParameters);

        var photoCompleteIds = completedPhotos
            .Where(p => p.PhotoTypeId == propertyPhotoTypeId)
            .Select(p => p.PropertyId)
            .ToHashSet();

        var planCompleteIds = completedPhotos
            .Where(p => p.PhotoTypeId == planPhotoTypeId)
            .Select(p => p.PropertyId)
            .ToHashSet();

        var propertyTypes = await _repository.ReadPropertyTypesAsync(
            dataEntryStageId, zoneIds, cancellationToken, queryParameters);
        var propertyUses = await _repository.ReadPropertyUsesAsync(
            dataEntryStageId, zoneIds, cancellationToken, queryParameters);

        var assessmentStatusIdsByName = await _repository.ReadAssessmentStatusIdsAsync(cancellationToken);
        var assessmentStatusCounts = await _repository.ReadAssessmentStatusCountsAsync(
            dataEntryStageId, zoneIds, cancellationToken, queryParameters);

        return (dataEntryProperties, internalSurveyProperties, assessmentProperties, zoneTotals,
                photoCompleteIds, planCompleteIds, propertyTypes, propertyUses, assessmentStatusIdsByName,
                assessmentStatusCounts, internalSurveyStageId, assessmentStageId, propertyPhotoTypeId,
                planPhotoTypeId);
    }

    /// Assembles zone rows from preloaded batch data.
    /// </summary>
    private static DataEntryGridResponseDto BuildGrid(
        List<(int ZoneId, string ZoneName, string ZoneNo)> zones,
        List<DataEntryStagePropertyProjection> dataEntryProperties,
        List<DataEntryStagePropertyProjection> internalSurveyProperties,
        List<DataEntryStagePropertyProjection> assessmentProperties,
        Dictionary<int, (int StructureCount, int UnitCount)> zoneTotals,
        HashSet<int> photoCompleteIds,
        HashSet<int> planCompleteIds,
        Dictionary<int, DataEntryPropertyTypeBreakdownDto> propertyTypes,
        Dictionary<int, AssessmentStatusBreakdownDto> assessmentStatuses,
        int assessmentStageId,
        int propertyPhotoTypeId,
        int planPhotoTypeId)
    {
        var assessmentPropertyIds = assessmentProperties.Select(p => p.PropertyId).ToHashSet();
        var dataEntryCountsByZone = CountByZone(dataEntryProperties);
        var internalCountsByZone = CountByZone(internalSurveyProperties);
        var qaCompletedCountsByZone = assessmentStageId == 0
            ? new Dictionary<int, (int StructureCount, int UnitCount)>()
            : CountByZone(dataEntryProperties.Where(p => assessmentPropertyIds.Contains(p.PropertyId)));
        var qaPendingCountsByZone = assessmentStageId == 0
            ? dataEntryCountsByZone
            : CountByZone(dataEntryProperties.Where(p => !assessmentPropertyIds.Contains(p.PropertyId)));
        var photoCompleteByZone = propertyPhotoTypeId == 0
            ? new Dictionary<int, int>()
            : dataEntryProperties
                .Where(p => photoCompleteIds.Contains(p.PropertyId))
                .GroupBy(p => p.ZoneId)
                .ToDictionary(g => g.Key, g => g.Count());
        var planCompleteByZone = planPhotoTypeId == 0
            ? new Dictionary<int, int>()
            : dataEntryProperties
                .Where(p => planCompleteIds.Contains(p.PropertyId))
                .GroupBy(p => p.ZoneId)
                .ToDictionary(g => g.Key, g => g.Count());
        var response = new DataEntryGridResponseDto();

        foreach (var (zoneId, zoneName, zoneNo) in zones)
        {
            var dataEntryCounts = dataEntryCountsByZone.GetValueOrDefault(zoneId);
            var internalCounts = internalCountsByZone.GetValueOrDefault(zoneId);
            var totalCounts = zoneTotals.GetValueOrDefault(zoneId);
            var qaCompletedCounts = qaCompletedCountsByZone.GetValueOrDefault(zoneId);
            var qaPendingCounts = qaPendingCountsByZone.GetValueOrDefault(zoneId);
            var photoComplete = propertyPhotoTypeId == 0 ? 0 : photoCompleteByZone.GetValueOrDefault(zoneId);
            var planComplete = planPhotoTypeId == 0 ? 0 : planCompleteByZone.GetValueOrDefault(zoneId);

            response.DivisionData.Add(new DataEntryDivisionDataDto
            {
                DivisionId = zoneId,
                DivisionName = zoneName,
                ZoneNo = zoneNo,
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
                    Pending = propertyPhotoTypeId == 0 ? dataEntryCounts.UnitCount : dataEntryCounts.UnitCount - photoComplete
                },
                Plan = new PlanBreakdownDto
                {
                    Complete = planComplete,
                    Pending = planPhotoTypeId == 0 ? dataEntryCounts.UnitCount : dataEntryCounts.UnitCount - planComplete
                },
                QualityAnalyst = new QualityAnalystBreakdownDto
                {
                    CompletedStructure = assessmentStageId == 0 ? 0 : qaCompletedCounts.StructureCount,
                    CompletedUnit = assessmentStageId == 0 ? 0 : qaCompletedCounts.UnitCount,
                    PendingStructure = assessmentStageId == 0 ? dataEntryCounts.StructureCount : qaPendingCounts.StructureCount,
                    PendingUnit = assessmentStageId == 0 ? dataEntryCounts.UnitCount : qaPendingCounts.UnitCount
                },
                PropertyType = propertyTypes.GetValueOrDefault(zoneId) ?? new DataEntryPropertyTypeBreakdownDto(),
                AssessmentStatusBreakdown = assessmentStatuses.GetValueOrDefault(zoneId) ?? new AssessmentStatusBreakdownDto()
            });
        }

        return response;
    }

    /// <summary>
    /// Groups raw property type/use rows into property-type columns using common helper.
    /// </summary>
    private static Dictionary<int, DataEntryPropertyTypeBreakdownDto> BuildPropertyTypeBreakdown(
        List<int> zoneIds,
        List<DataEntryPropertyTypeSourceProjection> properties,
        List<DataEntryPropertyUseSourceProjection> details)
    {
        var result = zoneIds.ToDictionary(z => z, _ => new DataEntryPropertyTypeBreakdownDto());

        // Build property use groups using common helper
        var propertyUseGroups = WorkflowStagePropertyTypeBuilder.BuildPropertyUseGroups(
            details,
            d => d.PropertyId,
            d => d.Type,
            d => d.TypeOfUseCode);

        // Group properties by zone
        foreach (var zoneGroup in properties.GroupBy(p => p.ZoneId))
        {
            var zoneBreakdown = WorkflowStagePropertyTypeBuilder.Build(
                zoneGroup.ToList(),
                propertyUseGroups,
                p => p.PropertyId,
                p => p.PropertyType);

            result[zoneGroup.Key].Residential = zoneBreakdown.Residential;
            result[zoneGroup.Key].NonResidential = zoneBreakdown.NonResidential;
            result[zoneGroup.Key].Mixed = zoneBreakdown.Mixed;
            result[zoneGroup.Key].PublicUtility = zoneBreakdown.PublicUtility;
        }

        return result;
    }

    /// <summary>
    /// Groups raw status count rows into assessed/unassessed/newly-assessed/in-process columns.
    /// </summary>
    private static Dictionary<int, AssessmentStatusBreakdownDto> BuildAssessmentStatusBreakdown(
        List<int> zoneIds,
        Dictionary<string, int> statusIdsByName,
        List<DataEntryAssessmentStatusCountProjection> counts)
    {
        var result = zoneIds.ToDictionary(z => z, _ => new AssessmentStatusBreakdownDto());

        var countsByZoneAndStatus = counts
            .GroupBy(x => x.ZoneId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(
                    x => x.StatusId,
                    x => (StructureCount: x.PropertyCount - x.UnitsOnlyCount, UnitCount: x.UnitsOnlyCount)));

        foreach (var zoneId in zoneIds)
        {
            if (!countsByZoneAndStatus.TryGetValue(zoneId, out var zoneCounts))
                zoneCounts = new Dictionary<int, (int StructureCount, int UnitCount)>();

            result[zoneId].Assessed = GetStatusCounts(statusIdsByName, zoneCounts, "ASSESSED");
            result[zoneId].Unassessed = GetStatusCounts(statusIdsByName, zoneCounts, "UNASSESSED", "UN ASSESSED");
            result[zoneId].NewlyAssessedFound = GetStatusCounts(statusIdsByName, zoneCounts, "PARTIALLY_ASSESSED", "PARTIALLY ASSESSED", "NEWLY_ASSESSED_FOUND", "NEWLY ASSESSED FOUND");
            result[zoneId].AssessmentInProcess = GetStatusCounts(statusIdsByName, zoneCounts, "UNDER_UNASSESSED", "UNDER UNASSESSED", "ASSESSMENT_IN_PROCESS", "ASSESSMENT IN PROCESS");
        }

        return result;
    }

    private static StructureUnitCountDto GetStatusCounts(
        Dictionary<string, int> statusIdsByName,
        Dictionary<int, (int StructureCount, int UnitCount)> countsByStatusId,
        params string[] statusNames)
    {
        var statusId = ResolveStatusId(statusIdsByName, statusNames);
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

    private static int ResolveStatusId(Dictionary<string, int> statusIdsByName, params string[] aliases)
    {
        var normalizedLookup = statusIdsByName
            .GroupBy(kvp => NormalizeStatusName(kvp.Key))
            .ToDictionary(g => g.Key, g => g.First().Value);

        foreach (var alias in aliases)
        {
            if (normalizedLookup.TryGetValue(NormalizeStatusName(alias), out var statusId))
                return statusId;
        }

        return 0;
    }

    private static string NormalizeStatusName(string value)
        => new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static List<DataEntryWardDataDto> BuildDataEntryWardRows(DataEntryWardWiseSummaryProjection snapshot)
    {
        var dataEntryRows = snapshot.StageRows.Where(r => r.WorkflowStageId == snapshot.DataEntryStageId).ToList();
        var internalSurveyRows = snapshot.StageRows.Where(r => r.WorkflowStageId == snapshot.InternalSurveyStageId).ToList();
        var assessmentPropertyIds = snapshot.AssessmentStageId == 0
            ? new HashSet<int>()
            : snapshot.StageRows
                .Where(r => r.WorkflowStageId == snapshot.AssessmentStageId)
                .Select(r => r.PropertyId)
                .ToHashSet();

        var dataEntryCountsByWard = CountByWard(dataEntryRows);
        var internalCountsByWard = CountByWard(internalSurveyRows);
        var wardTotalsByWard = snapshot.WardTotalRows.ToDictionary(r => r.WardId, r => (r.StructureCount, r.UnitCount));
        var dataEntryPropertyIdsByWard = dataEntryRows
            .GroupBy(r => r.WardId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.PropertyId).Distinct().ToHashSet());
        var dataEntryRowsByPropertyId = dataEntryRows
            .GroupBy(r => r.PropertyId)
            .ToDictionary(g => g.Key, g => g.First());
        var photoCompleteByWard = CountCompletedPhotosByWard(
            snapshot.CompletedPhotoRows,
            dataEntryRowsByPropertyId,
            snapshot.PropertyPhotoTypeId);
        var planCompleteByWard = CountCompletedPhotosByWard(
            snapshot.CompletedPhotoRows,
            dataEntryRowsByPropertyId,
            snapshot.PlanPhotoTypeId);
        var qaCompletedCountsByWard = snapshot.AssessmentStageId == 0
            ? new Dictionary<int, (int StructureCount, int UnitCount)>()
            : CountByWard(dataEntryRows.Where(r => assessmentPropertyIds.Contains(r.PropertyId)));
        var qaPendingCountsByWard = snapshot.AssessmentStageId == 0
            ? dataEntryCountsByWard
            : CountByWard(dataEntryRows.Where(r => !assessmentPropertyIds.Contains(r.PropertyId)));
        var propertyTypeByWard = BuildPropertyTypeBreakdownByWard(
            snapshot.Wards.Select(w => w.WardId).ToList(),
            snapshot.PropertyTypeRows,
            snapshot.PropertyUseRows);
        var assessmentStatusByWard = BuildAssessmentStatusBreakdownByWard(
            snapshot.Wards.Select(w => w.WardId).ToList(),
            dataEntryRows,
            snapshot.AssessmentStatusIdsByName);

        return snapshot.Wards
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
                var photoComplete = snapshot.PropertyPhotoTypeId == 0 ? 0 : photoCompleteByWard.GetValueOrDefault(ward.WardId);
                var planComplete = snapshot.PlanPhotoTypeId == 0 ? 0 : planCompleteByWard.GetValueOrDefault(ward.WardId);

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
                        Pending = snapshot.PropertyPhotoTypeId == 0 ? dataEntryPropertyCount : dataEntryPropertyCount - photoComplete
                    },
                    Plan = new PlanBreakdownDto
                    {
                        Complete = planComplete,
                        Pending = snapshot.PlanPhotoTypeId == 0 ? dataEntryPropertyCount : dataEntryPropertyCount - planComplete
                    },
                    QualityAnalyst = new QualityAnalystBreakdownDto
                    {
                        CompletedStructure = snapshot.AssessmentStageId == 0 ? 0 : qaCompletedCounts.StructureCount,
                        CompletedUnit = snapshot.AssessmentStageId == 0 ? 0 : qaCompletedCounts.UnitCount,
                        PendingStructure = snapshot.AssessmentStageId == 0 ? dataEntryCounts.StructureCount : qaPendingCounts.StructureCount,
                        PendingUnit = snapshot.AssessmentStageId == 0 ? dataEntryCounts.UnitCount : qaPendingCounts.UnitCount
                    },
                    PropertyType = propertyTypeByWard.GetValueOrDefault(ward.WardId) ?? new DataEntryPropertyTypeBreakdownDto(),
                    AssessmentStatusBreakdown = assessmentStatusByWard.GetValueOrDefault(ward.WardId) ?? new AssessmentStatusBreakdownDto()
                };
            })
            .ToList();
    }

    private static Dictionary<int, (int StructureCount, int UnitCount)> CountByWard(IEnumerable<DataEntryWardStageProjection> rows)
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
        List<DataEntryCompletedPhotoProjection> completedPhotoRows,
        Dictionary<int, DataEntryWardStageProjection> dataEntryRowsByPropertyId,
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
        var propertyUseGroups = WorkflowStagePropertyTypeBuilder.BuildPropertyUseGroups(
            details,
            d => d.PropertyId,
            d => d.Type,
            d => d.TypeOfUseCode);

        foreach (var wardGroup in properties.GroupBy(p => p.ZoneId))
        {
            var breakdown = WorkflowStagePropertyTypeBuilder.Build(
                wardGroup.ToList(),
                propertyUseGroups,
                p => p.PropertyId,
                p => p.PropertyType);

            result[wardGroup.Key].Residential = breakdown.Residential;
            result[wardGroup.Key].NonResidential = breakdown.NonResidential;
            result[wardGroup.Key].Mixed = breakdown.Mixed;
            result[wardGroup.Key].PublicUtility = breakdown.PublicUtility;
        }

        return result;
    }

    private static Dictionary<int, AssessmentStatusBreakdownDto> BuildAssessmentStatusBreakdownByWard(
        List<int> wardIds,
        List<DataEntryWardStageProjection> dataEntryRows,
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
                    Unassessed = GetStatusCounts(statusIdsByName, wardCounts, "UNASSESSED", "UN ASSESSED"),
                    NewlyAssessedFound = GetStatusCounts(statusIdsByName, wardCounts, "PARTIALLY_ASSESSED", "PARTIALLY ASSESSED", "NEWLY_ASSESSED_FOUND", "NEWLY ASSESSED FOUND"),
                    AssessmentInProcess = GetStatusCounts(statusIdsByName, wardCounts, "UNDER_UNASSESSED", "UNDER UNASSESSED", "ASSESSMENT_IN_PROCESS", "ASSESSMENT IN PROCESS")
                };
            });
    }

    private static bool IsApartmentUnit(DataEntryWardStageProjection row)
        => row.CategoryName == "Apartment"
           && !string.IsNullOrWhiteSpace(row.PartitionNo);

    private static int GetWardSummaryScore(DataEntryWardDataDto ward)
        => ward.Structure
           + ward.Unit
           + ward.InternalSurvey.Structure
           + ward.InternalSurvey.Unit
           + ward.DataEntry.CompletedStructure
           + ward.DataEntry.CompletedUnit
           + ward.Photo.Complete
           + ward.Plan.Complete
           + ward.QualityAnalyst.CompletedStructure
           + ward.QualityAnalyst.CompletedUnit
           + ward.PropertyType.Residential
           + ward.PropertyType.NonResidential
           + ward.PropertyType.Mixed
           + ward.PropertyType.PublicUtility
           + ward.AssessmentStatusBreakdown.Assessed.StructureCount
           + ward.AssessmentStatusBreakdown.Assessed.UnitCount
           + ward.AssessmentStatusBreakdown.Unassessed.StructureCount
           + ward.AssessmentStatusBreakdown.Unassessed.UnitCount
           + ward.AssessmentStatusBreakdown.NewlyAssessedFound.StructureCount
           + ward.AssessmentStatusBreakdown.NewlyAssessedFound.UnitCount
           + ward.AssessmentStatusBreakdown.AssessmentInProcess.StructureCount
           + ward.AssessmentStatusBreakdown.AssessmentInProcess.UnitCount;

    private static DataEntryWardDataDto CalculateWardTotals(List<DataEntryWardDataDto> wardData)
        => new()
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
                    StatusId = wardData.Select(w => w.AssessmentStatusBreakdown.Assessed.StatusId).FirstOrDefault(id => id > 0),
                    StructureCount = wardData.Sum(w => w.AssessmentStatusBreakdown.Assessed.StructureCount),
                    UnitCount = wardData.Sum(w => w.AssessmentStatusBreakdown.Assessed.UnitCount)
                },
                Unassessed = new StructureUnitCountDto
                {
                    StatusId = wardData.Select(w => w.AssessmentStatusBreakdown.Unassessed.StatusId).FirstOrDefault(id => id > 0),
                    StructureCount = wardData.Sum(w => w.AssessmentStatusBreakdown.Unassessed.StructureCount),
                    UnitCount = wardData.Sum(w => w.AssessmentStatusBreakdown.Unassessed.UnitCount)
                },
                NewlyAssessedFound = new StructureUnitCountDto
                {
                    StatusId = wardData.Select(w => w.AssessmentStatusBreakdown.NewlyAssessedFound.StatusId).FirstOrDefault(id => id > 0),
                    StructureCount = wardData.Sum(w => w.AssessmentStatusBreakdown.NewlyAssessedFound.StructureCount),
                    UnitCount = wardData.Sum(w => w.AssessmentStatusBreakdown.NewlyAssessedFound.UnitCount)
                },
                AssessmentInProcess = new StructureUnitCountDto
                {
                    StatusId = wardData.Select(w => w.AssessmentStatusBreakdown.AssessmentInProcess.StatusId).FirstOrDefault(id => id > 0),
                    StructureCount = wardData.Sum(w => w.AssessmentStatusBreakdown.AssessmentInProcess.StructureCount),
                    UnitCount = wardData.Sum(w => w.AssessmentStatusBreakdown.AssessmentInProcess.UnitCount)
                }
            }
        };

    /// <summary>
    /// Counts properties by zone, distinguishing structures (no partition) from units.
    /// </summary>
    private static Dictionary<int, (int StructureCount, int UnitCount)> CountByZone(
        IEnumerable<DataEntryStagePropertyProjection> properties)
        => properties
            .GroupBy(p => p.ZoneId)
            .ToDictionary(
                g => g.Key,
                g => (g.Count(p => string.IsNullOrWhiteSpace(p.PartitionNo)), g.Count()));

    /// <summary>
    /// Calculates total row by summing all division data.
    /// </summary>

    private static DataEntryDivisionDataDto CalculateTotals(List<DataEntryDivisionDataDto> divisionData, int totalStructure, int totalUnit)
        => new()
        {
            DivisionName = "TOTAL",
            Structure = totalStructure,
            Unit = totalUnit,
            InternalSurvey = new InternalSurveyBreakdownDto
            {
                Structure = divisionData.Sum(d => d.InternalSurvey.Structure),
                Unit = divisionData.Sum(d => d.InternalSurvey.Unit)
            },
            DataEntry = new DataEntryBreakdownDto
            {
                CompletedStructure = divisionData.Sum(d => d.DataEntry.CompletedStructure),
                CompletedUnit = divisionData.Sum(d => d.DataEntry.CompletedUnit),
                PendingStructure = divisionData.Sum(d => d.DataEntry.PendingStructure),
                PendingUnit = divisionData.Sum(d => d.DataEntry.PendingUnit)
            },
            Photo = new PhotoBreakdownDto
            {
                Complete = divisionData.Sum(d => d.Photo.Complete),
                Pending = divisionData.Sum(d => d.Photo.Pending)
            },
            Plan = new PlanBreakdownDto
            {
                Complete = divisionData.Sum(d => d.Plan.Complete),
                Pending = divisionData.Sum(d => d.Plan.Pending)
            },
            QualityAnalyst = new QualityAnalystBreakdownDto
            {
                CompletedStructure = divisionData.Sum(d => d.QualityAnalyst.CompletedStructure),
                CompletedUnit = divisionData.Sum(d => d.QualityAnalyst.CompletedUnit),
                PendingStructure = divisionData.Sum(d => d.QualityAnalyst.PendingStructure),
                PendingUnit = divisionData.Sum(d => d.QualityAnalyst.PendingUnit)
            },
            PropertyType = new DataEntryPropertyTypeBreakdownDto
            {
                Residential = divisionData.Sum(d => d.PropertyType.Residential),
                NonResidential = divisionData.Sum(d => d.PropertyType.NonResidential),
                Mixed = divisionData.Sum(d => d.PropertyType.Mixed),
                PublicUtility = divisionData.Sum(d => d.PropertyType.PublicUtility)
            },
            AssessmentStatusBreakdown = new AssessmentStatusBreakdownDto
            {
                Assessed = new StructureUnitCountDto
                {
                    StatusId = divisionData.Select(d => d.AssessmentStatusBreakdown.Assessed.StatusId).FirstOrDefault(id => id > 0),
                    StructureCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.Assessed.StructureCount),
                    UnitCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.Assessed.UnitCount)
                },
                Unassessed = new StructureUnitCountDto
                {
                    StatusId = divisionData.Select(d => d.AssessmentStatusBreakdown.Unassessed.StatusId).FirstOrDefault(id => id > 0),
                    StructureCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.Unassessed.StructureCount),
                    UnitCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.Unassessed.UnitCount)
                },
                NewlyAssessedFound = new StructureUnitCountDto
                {
                    StatusId = divisionData.Select(d => d.AssessmentStatusBreakdown.NewlyAssessedFound.StatusId).FirstOrDefault(id => id > 0),
                    StructureCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.NewlyAssessedFound.StructureCount),
                    UnitCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.NewlyAssessedFound.UnitCount)
                },
                AssessmentInProcess = new StructureUnitCountDto
                {
                    StatusId = divisionData.Select(d => d.AssessmentStatusBreakdown.AssessmentInProcess.StatusId).FirstOrDefault(id => id > 0),
                    StructureCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.AssessmentInProcess.StructureCount),
                    UnitCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.AssessmentInProcess.UnitCount)
                }
            }
        };
}

