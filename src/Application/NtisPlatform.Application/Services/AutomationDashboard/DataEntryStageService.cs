using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for Data Entry dashboard grid assembly and summary rules.
/// </summary>
public class DataEntryStageService : IDataEntryStageService
{
    private static readonly HashSet<string> MixedPropertyTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "R-C", "C-R", "C-I", "I-C", "I-R", "R-I"
    };

    private readonly IDataEntryStageRepository _repository;

    public DataEntryStageService(IDataEntryStageRepository repository)
    {
        _repository = repository;
    }

    // Builds the Data Entry grid while batching DB reads and keeping aggregation rules in the service.
    public async Task<DataEntryGridResponseDto> GetDataEntryGridDataAsync(PropertySearchRequestDto? searchRequest = null,CancellationToken cancellationToken = default)
    {
        if (searchRequest?.WorkflowStageId == null)
            return new DataEntryGridResponseDto();

        var dataEntryStageId = searchRequest.WorkflowStageId.Value;
        var snapshot = await _repository.GetDataEntryGridSnapshotAsync(
            dataEntryStageId,
            searchRequest.ZoneId,
            cancellationToken,
            searchRequest.PropertyTypeId,
            searchRequest.PropertyTypeCategoryId);

        if (!snapshot.WorkflowStageExists)
            return new DataEntryGridResponseDto();

        if (!snapshot.Zones.Any())
        {
            return new DataEntryGridResponseDto
            {
                DivisionData = new List<DataEntryDivisionDataDto>(),
                TotalRow = new DataEntryDivisionDataDto { DivisionName = "TOTAL" }
            };
        }

        var zoneIds = snapshot.Zones.Select(z => z.ZoneId).ToList();
        var stageProperties = snapshot.StageProperties;

        var result = BuildGrid(
            snapshot.Zones,
            stageProperties.Where(p => p.WorkflowStageId == dataEntryStageId).ToList(),
            stageProperties.Where(p => p.WorkflowStageId == snapshot.InternalSurveyStageId).ToList(),
            stageProperties.Where(p => p.WorkflowStageId == snapshot.AssessmentStageId).ToList(),
            snapshot.ZoneTotals.ToDictionary(z => z.ZoneId, z => (z.StructureCount, z.UnitCount)),
            snapshot.CompletedPhotos.Where(p => p.PhotoTypeId == snapshot.PropertyPhotoTypeId).Select(p => p.PropertyId).ToHashSet(),
            snapshot.CompletedPhotos.Where(p => p.PhotoTypeId == snapshot.PlanPhotoTypeId).Select(p => p.PropertyId).ToHashSet(),
            BuildPropertyTypeBreakdown(zoneIds, snapshot.PropertyTypeSources, snapshot.PropertyUseSources),
            BuildAssessmentStatusBreakdown(zoneIds, snapshot.AssessmentStatusIdsByName, snapshot.AssessmentStatusCounts),
            snapshot.AssessmentStageId,
            snapshot.PropertyPhotoTypeId,
            snapshot.PlanPhotoTypeId);

        result.TotalRow = CalculateTotals(
            result.DivisionData,
            result.DivisionData.Sum(d => d.Structure),
            result.DivisionData.Sum(d => d.Unit));

        return result;
    }

    // Assembles zone rows from preloaded batch data.
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

    // Groups raw property type/use rows into the same four Data Entry property-type columns.
    private static Dictionary<int, DataEntryPropertyTypeBreakdownDto> BuildPropertyTypeBreakdown(
        List<int> zoneIds,
        List<DataEntryPropertyTypeSourceProjection> properties,
        List<DataEntryPropertyUseSourceProjection> details)
    {
        var result = zoneIds.ToDictionary(z => z, _ => new DataEntryPropertyTypeBreakdownDto());
        var mixedPropertyIds = properties
            .Where(p => p.PropertyType != null && MixedPropertyTypes.Contains(p.PropertyType))
            .Select(p => p.PropertyId)
            .ToHashSet();

        foreach (var zoneGroup in properties.Where(p => mixedPropertyIds.Contains(p.PropertyId)).GroupBy(p => p.ZoneId))
            result[zoneGroup.Key].Mixed = zoneGroup.Select(p => p.PropertyId).Distinct().Count();

        var remainingPropertiesByZone = properties
            .Where(p => !mixedPropertyIds.Contains(p.PropertyId))
            .GroupBy(p => p.ZoneId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.PropertyId).Distinct().ToHashSet());

        var detailGroups = details
            .Where(d => !mixedPropertyIds.Contains(d.PropertyId))
            .GroupBy(d => new { d.ZoneId, d.PropertyId })
            .ToList();
        var propertiesWithDetailsByZone = detailGroups
            .GroupBy(g => g.Key.ZoneId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Key.PropertyId).Distinct().Count());

        foreach (var group in detailGroups)
        {
            var types = group.Select(x => x.Type?.Trim().ToUpperInvariant()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            var codes = group.Select(x => x.TypeOfUseCode?.Trim().ToUpperInvariant()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            var breakdown = result[group.Key.ZoneId];

            if (codes.Any(code => code == "UC"))
                continue;
            if (types.Any(type => type == "N" || type == "I"))
                breakdown.PublicUtility++;
            else if (types.Any(type => type == "R"))
                breakdown.Residential++;
            else if (types.Any(type => type == "C"))
                breakdown.NonResidential++;
        }

        foreach (var (zoneId, propertyIds) in remainingPropertiesByZone)
        {
            var propertiesWithDetails = propertiesWithDetailsByZone.GetValueOrDefault(zoneId);
            result[zoneId].Residential += Math.Max(0, propertyIds.Count - propertiesWithDetails);
        }

        return result;
    }

    // Groups raw status count rows into assessed/unassessed/newly-assessed/in-process columns.
    private static Dictionary<int, AssessmentStatusBreakdownDto> BuildAssessmentStatusBreakdown(
        List<int> zoneIds,
        Dictionary<string, int> statusIdsByName,
        List<DataEntryAssessmentStatusCountProjection> counts)
    {
        var countsByZoneAndStatus = counts
            .GroupBy(x => x.ZoneId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(
                    x => x.StatusId,
                    x => (StructureCount: x.PropertyCount - x.UnitsOnlyCount, UnitCount: x.UnitsOnlyCount)));

        return zoneIds.ToDictionary(
            zoneId => zoneId,
            zoneId =>
            {
                countsByZoneAndStatus.TryGetValue(zoneId, out var zoneCounts);
                zoneCounts ??= new Dictionary<int, (int StructureCount, int UnitCount)>();
                return new AssessmentStatusBreakdownDto
                {
                    Assessed = GetStatusCounts(statusIdsByName, zoneCounts, "ASSESSED"),
                    Unassessed = GetStatusCounts(statusIdsByName, zoneCounts, "UNASSESSED"),
                    NewlyAssessedFound = GetStatusCounts(statusIdsByName, zoneCounts, "PARTIALLY_ASSESSED"),
                    AssessmentInProcess = GetStatusCounts(statusIdsByName, zoneCounts, "UNDER_UNASSESSED")
                };
            });
    }

    private static Dictionary<int, (int StructureCount, int UnitCount)> CountByZone(
        IEnumerable<DataEntryStagePropertyProjection> properties)
        => properties
            .GroupBy(p => p.ZoneId)
            .ToDictionary(
                g => g.Key,
                g => (g.Count(p => string.IsNullOrWhiteSpace(p.PartitionNo)), g.Count()));

    private static StructureUnitCountDto GetStatusCounts(Dictionary<string, int> statusIdsByName,Dictionary<int, (int StructureCount, int UnitCount)> countsByStatusId,string statusName)
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
                    StructureCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.Assessed.StructureCount),
                    UnitCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.Assessed.UnitCount)
                },
                Unassessed = new StructureUnitCountDto
                {
                    StructureCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.Unassessed.StructureCount),
                    UnitCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.Unassessed.UnitCount)
                },
                NewlyAssessedFound = new StructureUnitCountDto
                {
                    StructureCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.NewlyAssessedFound.StructureCount),
                    UnitCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.NewlyAssessedFound.UnitCount)
                },
                AssessmentInProcess = new StructureUnitCountDto
                {
                    StructureCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.AssessmentInProcess.StructureCount),
                    UnitCount = divisionData.Sum(d => d.AssessmentStatusBreakdown.AssessmentInProcess.UnitCount)
                }
            }
        };
}
