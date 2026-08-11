using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for Internal Survey dashboard grid assembly and summary rules.
/// </summary>
public class InternalSurveyStageService : IInternalSurveyStageService
{
    private static readonly HashSet<string> MixedPropertyTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "R-C", "C-R", "C-I", "I-C", "I-R", "R-I"
    };

    private readonly IInternalSurveyStageRepository _repository;

    public InternalSurveyStageService(IInternalSurveyStageRepository repository)
    {
        _repository = repository;
    }

    // Builds the Internal Survey grid while keeping aggregation rules in the service.
    public async Task<InternalSurveyGridResponseDto> GetInternalSurveyGridDataAsync(PropertySearchRequestDto? searchRequest = null,CancellationToken cancellationToken = default)
    {
        if (searchRequest?.WorkflowStageId == null)
            return new InternalSurveyGridResponseDto();

        var snapshot = await BuildGridSnapshotAsync(searchRequest.WorkflowStageId.Value,searchRequest,cancellationToken);

        if (!snapshot.WorkflowStageExists)
            return new InternalSurveyGridResponseDto();

        var response = new InternalSurveyGridResponseDto();
        if (!snapshot.Zones.Any())
        {
            response.TotalRow = new InternalSurveyDivisionDataDto { DivisionName = "TOTAL" };
            return response;
        }

        var geoPropertiesByZone = snapshot.GeoSequencingProperties
            .GroupBy(p => p.ZoneId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var internalPropertiesByZone = snapshot.InternalSurveyProperties
            .GroupBy(p => p.ZoneId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var propertyUseGroups = BuildPropertyUseGroups(snapshot.InternalSurveyPropertyUses);
        var photoCountsByZone = snapshot.PhotoCountsByZone
            .Where(p => p.ZoneId.HasValue)
            .ToDictionary(p => p.ZoneId!.Value, p => p.Count);

        foreach (var (zoneId, zoneName, zoneNo) in snapshot.Zones)
        {
            geoPropertiesByZone.TryGetValue(zoneId, out var geoProperties);
            internalPropertiesByZone.TryGetValue(zoneId, out var internalProperties);
            photoCountsByZone.TryGetValue(zoneId, out var photoCount);

            response.DivisionData.Add(BuildDivisionData(
                zoneId,
                zoneName,
                zoneNo,
                geoProperties ?? new List<InternalSurveyStagePropertyProjection>(),
                internalProperties ?? new List<InternalSurveyStagePropertyProjection>(),
                propertyUseGroups,
                snapshot.AssessedStatusId,
                snapshot.UnassessedStatusId,
                photoCount));
        }

        response.TotalRow = CalculateTotals(response.DivisionData);
        return response;
    }

    // Builds the Internal Survey ward-wise summary from one repository snapshot.
    public async Task<InternalSurveyWardWiseSummaryResponseDto> GetInternalSurveyWardWiseSummaryAsync(
        int zoneId,int workflowStageId,int? pageNumber,int? pageSize,CancellationToken cancellationToken = default)
    {
        var (normalizedPageNumber, normalizedPageSize) = WorkflowStagePagingHelper.NormalizePaging(pageNumber, pageSize);
        var snapshot = await BuildWardWiseSnapshotAsync(zoneId,workflowStageId,cancellationToken);

        if (!snapshot.IsValid)
            return new InternalSurveyWardWiseSummaryResponseDto();

        var response = new InternalSurveyWardWiseSummaryResponseDto
        {
            ZoneId = snapshot.ZoneId,
            ZoneName = snapshot.ZoneName,
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize,
            TotalCount = snapshot.Wards.Count
        };

        var geoPropertiesByWard = snapshot.GeoSequencingProperties
            .GroupBy(p => p.WardId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var internalPropertiesByWard = snapshot.InternalSurveyProperties
            .GroupBy(p => p.WardId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var propertyUseGroups = BuildPropertyUseGroups(snapshot.InternalSurveyPropertyUses);
        var photoCountsByWard = snapshot.PhotoCountsByWard
            .Where(p => p.WardId.HasValue)
            .ToDictionary(p => p.WardId!.Value, p => p.Count);
        var allWardData = new List<InternalSurveyWardDataDto>(snapshot.Wards.Count);

        foreach (var (wardId, wardNo) in snapshot.Wards)
        {
            geoPropertiesByWard.TryGetValue(wardId, out var geoProperties);
            internalPropertiesByWard.TryGetValue(wardId, out var internalProperties);
            photoCountsByWard.TryGetValue(wardId, out var photoCount);

            allWardData.Add(BuildWardData(
                wardId,
                wardNo,
                geoProperties ?? new List<InternalSurveyStagePropertyProjection>(),
                internalProperties ?? new List<InternalSurveyStagePropertyProjection>(),
                propertyUseGroups,
                snapshot.AssessedStatusId,
                snapshot.UnassessedStatusId,
                photoCount));
        }

        var orderedWardData = allWardData
            .OrderByDescending(HasWardSummaryData)
            .ToList();

        response.TotalRow = CalculateWardTotals(allWardData);
        response.WardData = WorkflowStagePagingHelper.PageWardData(orderedWardData, normalizedPageNumber, normalizedPageSize);
        return response;
    }

    // Loads and groups the raw repository reads required by the zone grid.
    private async Task<InternalSurveyGridSnapshotProjection> BuildGridSnapshotAsync(
        int internalSurveyStageId,
        PropertySearchRequestDto searchRequest,
        CancellationToken cancellationToken)
    {
        var stageExists = await _repository.StageExistsAsync(internalSurveyStageId, cancellationToken);
        if (!stageExists)
            return new InternalSurveyGridSnapshotProjection();

        var zones = await _repository.ReadZonesAsync(searchRequest.ZoneId, cancellationToken);
        var snapshot = new InternalSurveyGridSnapshotProjection
        {
            WorkflowStageExists = true,
            Zones = zones
        };

        if (!zones.Any())
            return snapshot;

        var zoneIds = zones.Select(z => z.ZoneId).ToList();
        snapshot.GeoSequencingStageId = await _repository.ReadGeoSequencingStageIdAsync(cancellationToken);
        (snapshot.AssessedStatusId, snapshot.UnassessedStatusId) = await _repository.ReadAssessedAndUnassessedStatusIdsAsync(cancellationToken);
        snapshot.PropertyPhotoTypeId = await _repository.ReadPropertyPhotoTypeIdAsync(cancellationToken);
        snapshot.GeoSequencingProperties = await _repository.ReadStagePropertiesForZonesAsync(
            snapshot.GeoSequencingStageId,
            zoneIds,
            requirePropertyNo: true,
            cancellationToken,
            searchRequest);
        snapshot.InternalSurveyProperties = await _repository.ReadStagePropertiesForZonesAsync(
            internalSurveyStageId,
            zoneIds,
            requirePropertyNo: true,
            cancellationToken,
            searchRequest);
        snapshot.InternalSurveyPropertyUses = await _repository.ReadPropertyUsesForStageInZonesAsync(
            internalSurveyStageId,
            zoneIds,
            requirePropertyNo: true,
            cancellationToken,
            searchRequest);
        snapshot.PhotoCountsByZone = await _repository.ReadPhotoCountsByZoneAsync(
            internalSurveyStageId,
            zoneIds,
            snapshot.PropertyPhotoTypeId,
            cancellationToken,
            searchRequest);

        return snapshot;
    }

    // Loads and groups the raw repository reads required by the ward-wise summary.
    private async Task<InternalSurveyWardWiseSnapshotProjection> BuildWardWiseSnapshotAsync(int zoneId,int workflowStageId,CancellationToken cancellationToken)
    {
        if (zoneId <= 0 || workflowStageId <= 0)
            return new InternalSurveyWardWiseSnapshotProjection();

        var stageExists = await _repository.StageExistsAsync(workflowStageId, cancellationToken);
        if (!stageExists)
            return new InternalSurveyWardWiseSnapshotProjection();

        var zone = await _repository.ReadZoneAsync(zoneId, cancellationToken);
        if (zone.ZoneId == 0)
            return new InternalSurveyWardWiseSnapshotProjection();

        var wards = await _repository.ReadWardsInZoneAsync(zoneId, cancellationToken);
        var snapshot = new InternalSurveyWardWiseSnapshotProjection
        {
            IsValid = true,
            ZoneId = zone.ZoneId,
            ZoneName = zone.ZoneName,
            ZoneNo = zone.ZoneNo,
            Wards = wards
        };

        if (!wards.Any())
            return snapshot;

        var zoneIds = new List<int> { zoneId };
        var wardIds = wards.Select(w => w.WardId).ToList();
        snapshot.GeoSequencingStageId = await _repository.ReadGeoSequencingStageIdAsync(cancellationToken);
        (snapshot.AssessedStatusId, snapshot.UnassessedStatusId) = await _repository.ReadAssessedAndUnassessedStatusIdsAsync(cancellationToken);
        snapshot.PropertyPhotoTypeId = await _repository.ReadPropertyPhotoTypeIdAsync(cancellationToken);
        snapshot.GeoSequencingProperties = await _repository.ReadStagePropertiesForZonesAsync(
            snapshot.GeoSequencingStageId,
            zoneIds,
            requirePropertyNo: true,
            cancellationToken);
        snapshot.InternalSurveyProperties = await _repository.ReadStagePropertiesForZonesAsync(
            workflowStageId,
            zoneIds,
            requirePropertyNo: true,
            cancellationToken);
        snapshot.InternalSurveyPropertyUses = await _repository.ReadPropertyUsesForStageInZonesAsync(
            workflowStageId,
            zoneIds,
            requirePropertyNo: true,
            cancellationToken);
        snapshot.PhotoCountsByWard = await _repository.ReadPhotoCountsByWardAsync(
            workflowStageId,
            wardIds,
            snapshot.PropertyPhotoTypeId,
            cancellationToken);

        return snapshot;
    }

    private static InternalSurveyWardDataDto BuildWardData(
        int wardId,
        string wardNo,
        List<InternalSurveyStagePropertyProjection> geoSequencingProperties,
        List<InternalSurveyStagePropertyProjection> internalSurveyProperties,
        Dictionary<int, PropertyUseGroup> propertyUseGroups,
        int assessedStatusId,
        int unassessedStatusId,
        int photoCount)
    {
        var wardData = new InternalSurveyWardDataDto
        {
            WardId = wardId,
            WardNo = wardNo,
            GeoSequencingProperties = new GeoSequencingPropertiesDto
            {
                Structure = CountStructures(geoSequencingProperties),
                Unit = geoSequencingProperties.Count
            }
        };

        if (!internalSurveyProperties.Any())
            return wardData;

        wardData.SurveyProperties = new SurveyPropertiesDto
        {
            Structure = CountStructures(internalSurveyProperties),
            Unit = internalSurveyProperties.Count
        };
        wardData.PropertyType = BuildPropertyTypeBreakdown(internalSurveyProperties, propertyUseGroups);
        wardData.AssessedProperties = BuildAssessedProperties(internalSurveyProperties, assessedStatusId);
        wardData.UnassessedProperties = BuildUnassessedProperties(internalSurveyProperties, unassessedStatusId);
        wardData.NewlyAssessedFound = new NewlyAssessedFoundDto();
        wardData.AssessmentInprocess = new AssessmentInprocessDto();
        wardData.PhotoCount = photoCount;

        return wardData;
    }

    private static InternalSurveyDivisionDataDto BuildDivisionData(
        int divisionId,
        string divisionName,
        string zoneNo,
        List<InternalSurveyStagePropertyProjection> geoSequencingProperties,
        List<InternalSurveyStagePropertyProjection> internalSurveyProperties,
        Dictionary<int, PropertyUseGroup> propertyUseGroups,
        int assessedStatusId,
        int unassessedStatusId,
        int photoCount)
    {
        if (!internalSurveyProperties.Any())
        {
            return new InternalSurveyDivisionDataDto
            {
                DivisionId = divisionId,
                DivisionName = divisionName,
                ZoneNo = zoneNo,
                GeoSequencingProperties = new GeoSequencingPropertiesDto
                {
                    Structure = CountStructures(geoSequencingProperties),
                    Unit = geoSequencingProperties.Count
                }
            };
        }

        return new InternalSurveyDivisionDataDto
        {
            DivisionId = divisionId,
            DivisionName = divisionName,
            ZoneNo = zoneNo,
            GeoSequencingProperties = new GeoSequencingPropertiesDto
            {
                Structure = CountStructures(geoSequencingProperties),
                Unit = geoSequencingProperties.Count
            },
            SurveyProperties = new SurveyPropertiesDto
            {
                Structure = CountStructures(internalSurveyProperties),
                Unit = internalSurveyProperties.Count
            },
            PropertyType = BuildPropertyTypeBreakdown(internalSurveyProperties, propertyUseGroups),
            AssessedProperties = BuildAssessedProperties(internalSurveyProperties, assessedStatusId),
            UnassessedProperties = BuildUnassessedProperties(internalSurveyProperties, unassessedStatusId),
            NewlyAssessedFound = new NewlyAssessedFoundDto(),
            AssessmentInprocess = new AssessmentInprocessDto(),
            PhotoCount = photoCount
        };
    }

    private static Dictionary<int, PropertyUseGroup> BuildPropertyUseGroups(List<InternalSurveyPropertyUseSourceProjection> propertyUses)
        => propertyUses
            .GroupBy(x => x.PropertyId)
            .ToDictionary(
                g => g.Key,
                g => new PropertyUseGroup(
                    g.Where(x => x.Type != null).Select(x => x.Type!).Distinct().ToList(),
                    g.Where(x => x.TypeOfUseCode != null).Select(x => x.TypeOfUseCode!).Distinct().ToList()));

    private static PropertyTypesBreakdownDto BuildPropertyTypeBreakdown(
        List<InternalSurveyStagePropertyProjection> properties, Dictionary<int, PropertyUseGroup> propertyUseGroups)
    {
        var breakdown = new PropertyTypesBreakdownDto();
        if (!properties.Any())
            return breakdown;

        var nonMixedProperties = new List<InternalSurveyStagePropertyProjection>();
        foreach (var property in properties)
        {
            if (IsMixedProperty(property.PropertyTypeCode))
                breakdown.Mixed++;
            else
                nonMixedProperties.Add(property);
        }

        var propertiesWithDetails = 0;
        foreach (var property in nonMixedProperties)
        {
            if (!propertyUseGroups.TryGetValue(property.PropertyId, out var useGroup))
                continue;

            propertiesWithDetails++;
            if (useGroup.Codes.Any(code => code.Equals("UC", StringComparison.OrdinalIgnoreCase)))
                breakdown.UnderConstruction++;
            else if (useGroup.Types.Any(type => type.Equals("N", StringComparison.OrdinalIgnoreCase) || type.Equals("I", StringComparison.OrdinalIgnoreCase)))
                breakdown.PublicUtility++;
            else if (useGroup.Types.Any(type => type.Equals("R", StringComparison.OrdinalIgnoreCase)))
                breakdown.Residential++;
            else if (useGroup.Types.Any(type => type.Equals("C", StringComparison.OrdinalIgnoreCase)))
                breakdown.NonResidential++;
        }

        breakdown.Residential += nonMixedProperties.Count - propertiesWithDetails;
        return breakdown;
    }

    private static AssessedPropertiesSimpleDto BuildAssessedProperties(List<InternalSurveyStagePropertyProjection> properties,int assessedStatusId)
    {
        if (assessedStatusId == 0)
            return new AssessedPropertiesSimpleDto();

        var statusProperties = properties
            .Where(p => p.AssessmentStatusId == assessedStatusId)
            .ToList();

        return new AssessedPropertiesSimpleDto
        {
            Structure = CountStructures(statusProperties),
            Units = statusProperties.Count
        };
    }

    private static UnassessedPropertiesDto BuildUnassessedProperties(List<InternalSurveyStagePropertyProjection> properties,int unassessedStatusId)
    {
        if (unassessedStatusId == 0)
            return new UnassessedPropertiesDto();

        var statusProperties = properties
            .Where(p => p.AssessmentStatusId == unassessedStatusId)
            .ToList();

        return new UnassessedPropertiesDto
        {
            Structure = CountStructures(statusProperties),
            Units = statusProperties.Count
        };
    }

    private static InternalSurveyDivisionDataDto CalculateTotals(List<InternalSurveyDivisionDataDto> divisionData)
    {
        return new InternalSurveyDivisionDataDto
        {
            DivisionName = "TOTAL",
            GeoSequencingProperties = new GeoSequencingPropertiesDto
            {
                Structure = divisionData.Sum(d => d.GeoSequencingProperties.Structure),
                Unit = divisionData.Sum(d => d.GeoSequencingProperties.Unit)
            },
            SurveyProperties = new SurveyPropertiesDto
            {
                Structure = divisionData.Sum(d => d.SurveyProperties.Structure),
                Unit = divisionData.Sum(d => d.SurveyProperties.Unit)
            },
            PropertyType = new PropertyTypesBreakdownDto
            {
                Residential = divisionData.Sum(d => d.PropertyType.Residential),
                NonResidential = divisionData.Sum(d => d.PropertyType.NonResidential),
                Mixed = divisionData.Sum(d => d.PropertyType.Mixed),
                PublicUtility = divisionData.Sum(d => d.PropertyType.PublicUtility),
                UnderConstruction = divisionData.Sum(d => d.PropertyType.UnderConstruction)
            },
            AssessedProperties = new AssessedPropertiesSimpleDto
            {
                Structure = divisionData.Sum(d => d.AssessedProperties.Structure),
                Units = divisionData.Sum(d => d.AssessedProperties.Units)
            },
            UnassessedProperties = new UnassessedPropertiesDto
            {
                Structure = divisionData.Sum(d => d.UnassessedProperties.Structure),
                Units = divisionData.Sum(d => d.UnassessedProperties.Units)
            },
            NewlyAssessedFound = new NewlyAssessedFoundDto
            {
                Structure = divisionData.Sum(d => d.NewlyAssessedFound.Structure),
                Unit = divisionData.Sum(d => d.NewlyAssessedFound.Unit)
            },
            AssessmentInprocess = new AssessmentInprocessDto
            {
                Structure = divisionData.Sum(d => d.AssessmentInprocess.Structure),
                Unit = divisionData.Sum(d => d.AssessmentInprocess.Unit)
            },
            PhotoCount = divisionData.Sum(d => d.PhotoCount)
        };
    }

    private static bool HasWardSummaryData(InternalSurveyWardDataDto ward)
        => ward.GeoSequencingProperties.Structure > 0
           || ward.GeoSequencingProperties.Unit > 0
           || ward.SurveyProperties.Structure > 0
           || ward.SurveyProperties.Unit > 0
           || ward.PropertyType.Residential > 0
           || ward.PropertyType.NonResidential > 0
           || ward.PropertyType.Mixed > 0
           || ward.PropertyType.PublicUtility > 0
           || ward.PropertyType.UnderConstruction > 0
           || ward.AssessedProperties.Structure > 0
           || ward.AssessedProperties.Units > 0
           || ward.UnassessedProperties.Structure > 0
           || ward.UnassessedProperties.Units > 0
           || ward.NewlyAssessedFound.Structure > 0
           || ward.NewlyAssessedFound.Unit > 0
           || ward.AssessmentInprocess.Structure > 0
           || ward.AssessmentInprocess.Unit > 0
           || ward.PhotoCount > 0;

    private static InternalSurveyWardDataDto CalculateWardTotals(List<InternalSurveyWardDataDto> wardData)
    {
        return new InternalSurveyWardDataDto
        {
            WardNo = "TOTAL",
            GeoSequencingProperties = new GeoSequencingPropertiesDto
            {
                Structure = wardData.Sum(w => w.GeoSequencingProperties.Structure),
                Unit = wardData.Sum(w => w.GeoSequencingProperties.Unit)
            },
            SurveyProperties = new SurveyPropertiesDto
            {
                Structure = wardData.Sum(w => w.SurveyProperties.Structure),
                Unit = wardData.Sum(w => w.SurveyProperties.Unit)
            },
            PropertyType = new PropertyTypesBreakdownDto
            {
                Residential = wardData.Sum(w => w.PropertyType.Residential),
                NonResidential = wardData.Sum(w => w.PropertyType.NonResidential),
                Mixed = wardData.Sum(w => w.PropertyType.Mixed),
                PublicUtility = wardData.Sum(w => w.PropertyType.PublicUtility),
                UnderConstruction = wardData.Sum(w => w.PropertyType.UnderConstruction)
            },
            AssessedProperties = new AssessedPropertiesSimpleDto
            {
                Structure = wardData.Sum(w => w.AssessedProperties.Structure),
                Units = wardData.Sum(w => w.AssessedProperties.Units)
            },
            UnassessedProperties = new UnassessedPropertiesDto
            {
                Structure = wardData.Sum(w => w.UnassessedProperties.Structure),
                Units = wardData.Sum(w => w.UnassessedProperties.Units)
            },
            NewlyAssessedFound = new NewlyAssessedFoundDto
            {
                Structure = wardData.Sum(w => w.NewlyAssessedFound.Structure),
                Unit = wardData.Sum(w => w.NewlyAssessedFound.Unit)
            },
            AssessmentInprocess = new AssessmentInprocessDto
            {
                Structure = wardData.Sum(w => w.AssessmentInprocess.Structure),
                Unit = wardData.Sum(w => w.AssessmentInprocess.Unit)
            },
            PhotoCount = wardData.Sum(w => w.PhotoCount)
        };
    }

    private static int CountStructures(List<InternalSurveyStagePropertyProjection> properties)
        => properties.Count(p => string.IsNullOrWhiteSpace(p.PartitionNo));

    private static bool IsMixedProperty(string? propertyTypeCode)
        => !string.IsNullOrWhiteSpace(propertyTypeCode)
           && MixedPropertyTypes.Contains(propertyTypeCode);

    private sealed record PropertyUseGroup(List<string> Types, List<string> Codes);
}
