using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for Geo-Sequencing dashboard grid assembly and summary rules.
/// </summary>
public class GeoSequencingStageService : IGeoSequencingStageService
{
    private const string ApartmentCategoryName = "Apartment";

    private static readonly HashSet<string> MixedPropertyTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "R-C", "C-R", "C-I", "I-C", "I-R", "R-I"
    };

    private readonly IGeoSequencingStageRepository _repository;

    public GeoSequencingStageService(IGeoSequencingStageRepository repository)
    {
        _repository = repository;
    }

    // Builds the Geo-Sequencing grid while keeping aggregation rules in the service.
    public async Task<GeoSequencingGridResponseDto> GetGeoSequencingGridDataAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default)
    {
        if (searchRequest?.WorkflowStageId == null)
            return new GeoSequencingGridResponseDto();

        var workflowStageId = searchRequest.WorkflowStageId.Value;
        if (!await _repository.StageExistsAsync(workflowStageId, cancellationToken))
            return new GeoSequencingGridResponseDto();

        var zones = await _repository.ReadZonesAsync(searchRequest.ZoneId, cancellationToken);
        if (!zones.Any())
            return new GeoSequencingGridResponseDto();

        var zoneIds = zones.Select(z => z.ZoneId).ToList();
        var stageProperties = await _repository.ReadStagePropertiesForZonesAsync(
            workflowStageId,
            zoneIds,
            cancellationToken,
            searchRequest);
        var propertyUseGroups = BuildPropertyUseGroups(await _repository.ReadPropertyUsesForZonesAsync(
            workflowStageId,
            zoneIds,
            cancellationToken,
            searchRequest));
        var registeredCounts = await _repository.ReadRegisteredCountsByZoneAsync(zoneIds, cancellationToken, searchRequest);
        var statusIdsByName = await _repository.ReadAssessmentStatusIdsByNameAsync(cancellationToken);
        var propertiesByZone = stageProperties
            .GroupBy(p => p.ZoneId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new GeoSequencingGridResponseDto();
        foreach (var (zoneId, zoneName) in zones)
        {
            propertiesByZone.TryGetValue(zoneId, out var zoneProperties);
            registeredCounts.TryGetValue(zoneId, out var registeredCount);
            result.Zones.Add(BuildZoneData(
                zoneId,
                zoneName,
                registeredCount,
                zoneProperties ?? new List<GeoSequencingStagePropertyProjection>(),
                propertyUseGroups,
                statusIdsByName));
        }

        result.TotalRow = CalculateTotals(result.Zones);
        return result;
    }

    // Builds the Geo-Sequencing ward-wise summary from raw repository reads.
    public async Task<GeoSequencingWardWiseSummaryResponseDto> GetGeoSequencingWardWiseSummaryAsync(
        int zoneId,
        int workflowStageId,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPageNumber, normalizedPageSize) = WorkflowStagePagingHelper.NormalizePaging(pageNumber, pageSize);
        var context = await BuildWardWiseContextAsync(
            zoneId,
            workflowStageId,
            normalizedPageNumber,
            normalizedPageSize,
            cancellationToken);

        if (!context.IsValid)
            return new GeoSequencingWardWiseSummaryResponseDto();

        var zoneIds = new List<int> { zoneId };
        var stageProperties = await _repository.ReadStagePropertiesForZonesAsync(
            workflowStageId,
            zoneIds,
            cancellationToken);
        var propertyUseGroups = BuildPropertyUseGroups(await _repository.ReadPropertyUsesForZonesAsync(
            workflowStageId,
            zoneIds,
            cancellationToken));
        var registeredCounts = await _repository.ReadRegisteredCountsByWardAsync(
            context.Wards.Select(w => w.WardId).ToList(),
            cancellationToken);
        var statusIdsByName = await _repository.ReadAssessmentStatusIdsByNameAsync(cancellationToken);
        var propertiesByWard = stageProperties
            .GroupBy(p => p.WardId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new GeoSequencingWardWiseSummaryResponseDto
        {
            ZoneId = context.ZoneId,
            ZoneName = context.ZoneName,
            PageNumber = context.PageNumber,
            PageSize = context.PageSize,
            TotalCount = context.TotalCount
        };

        var allWardData = new List<GeoSequencingWardDataDto>(context.Wards.Count);
        foreach (var (wardId, wardNo) in context.Wards)
        {
            propertiesByWard.TryGetValue(wardId, out var wardProperties);
            registeredCounts.TryGetValue(wardId, out var registeredCount);
            allWardData.Add(BuildWardData(
                wardId,
                wardNo,
                registeredCount,
                wardProperties ?? new List<GeoSequencingStagePropertyProjection>(),
                propertyUseGroups,
                statusIdsByName));
        }

        var orderedWardData = allWardData
            .OrderByDescending(HasWardSummaryData)
            .ToList();

        result.TotalRow = CalculateWardTotals(allWardData);
        result.WardData = WorkflowStagePagingHelper.PageWardData(orderedWardData, context.PageNumber, context.PageSize);
        return result;
    }

    // Validates stage, zone, and wards required for ward-wise summary.
    private async Task<WardWiseSummaryContext> BuildWardWiseContextAsync(
        int zoneId,
        int workflowStageId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (zoneId <= 0 || workflowStageId <= 0)
            return WardWiseSummaryContext.Invalid(pageNumber, pageSize);

        if (!await _repository.StageExistsAsync(workflowStageId, cancellationToken))
            return WardWiseSummaryContext.Invalid(pageNumber, pageSize);

        var zone = await _repository.ReadZoneAsync(zoneId, cancellationToken);
        if (zone.ZoneId == 0)
            return WardWiseSummaryContext.Invalid(pageNumber, pageSize);

        return new WardWiseSummaryContext(
            IsValid: true,
            ZoneId: zone.ZoneId,
            ZoneName: zone.ZoneName,
            PageNumber: pageNumber,
            PageSize: pageSize,
            Wards: await _repository.ReadWardsInZoneAsync(zoneId, cancellationToken));
    }

    private static GeoSequencingWardDataDto BuildWardData(
        int wardId,string wardNo,int registeredCount,List<GeoSequencingStagePropertyProjection> properties,
        Dictionary<int, PropertyUseGroup> propertyUseGroups,Dictionary<string, int> statusIdsByName)
    {
        return new GeoSequencingWardDataDto
        {
            WardId = wardId,
            WardNo = wardNo,
            RegisteredProperties = registeredCount,
            GeoSequencedProperties = new GeoSequencedPropertiesDto
            {
                StructureCount = properties.Count(p => string.IsNullOrWhiteSpace(p.PartitionNo)),
                UnitCount = properties.Count
            },
            PropertyTypeBreakdown = BuildPropertyTypeBreakdown(properties, propertyUseGroups),
            AssessmentStatusBreakdown = BuildAssessmentStatusBreakdown(properties, statusIdsByName)
        };
    }

    private static GeoSequencingZoneDataDto BuildZoneData(
        int zoneId,string zoneName,int registeredCount,List<GeoSequencingStagePropertyProjection> properties,
        Dictionary<int, PropertyUseGroup> propertyUseGroups,Dictionary<string, int> statusIdsByName)
    {
        return new GeoSequencingZoneDataDto
        {
            ZoneId = zoneId,
            ZoneName = zoneName,
            RegisteredProperties = registeredCount,
            GeoSequencedProperties = new GeoSequencedPropertiesDto
            {
                StructureCount = properties.Count(p => string.IsNullOrWhiteSpace(p.PartitionNo)),
                UnitCount = properties.Count
            },
            PropertyTypeBreakdown = BuildPropertyTypeBreakdown(properties, propertyUseGroups),
            AssessmentStatusBreakdown = BuildAssessmentStatusBreakdown(properties, statusIdsByName)
        };
    }

    private static Dictionary<int, PropertyUseGroup> BuildPropertyUseGroups(List<GeoSequencingPropertyUseProjection> propertyUses)
        => propertyUses
            .GroupBy(x => x.PropertyId)
            .ToDictionary(
                g => g.Key,
                g => new PropertyUseGroup(
                    g.Where(x => x.Type != null).Select(x => x.Type!).Distinct().ToList(),
                    g.Where(x => x.TypeOfUseCode != null).Select(x => x.TypeOfUseCode!).Distinct().ToList()));

    private static PropertyTypesBreakdownDto BuildPropertyTypeBreakdown(List<GeoSequencingStagePropertyProjection> properties,Dictionary<int, PropertyUseGroup> propertyUseGroups)
    {
        var breakdown = new PropertyTypesBreakdownDto();
        if (!properties.Any())
            return breakdown;

        var nonMixedProperties = new List<GeoSequencingStagePropertyProjection>();
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

    private static AssessmentStatusBreakdownDto BuildAssessmentStatusBreakdown(List<GeoSequencingStagePropertyProjection> properties,Dictionary<string, int> statusIdsByName)
    {
        return new AssessmentStatusBreakdownDto
        {
            Assessed = GetStatusCounts(properties, statusIdsByName, "ASSESSED"),
            Unassessed = GetStatusCounts(properties, statusIdsByName, "UNASSESSED"),
            NewlyAssessedFound = GetStatusCounts(properties, statusIdsByName, "PARTIALLY_ASSESSED"),
            AssessmentInProcess = GetStatusCounts(properties, statusIdsByName, "UNDER_UNASSESSED")
        };
    }

    private static StructureUnitCountDto GetStatusCounts(List<GeoSequencingStagePropertyProjection> properties,Dictionary<string, int> statusIdsByName,string statusName)
    {
        if (!statusIdsByName.TryGetValue(statusName, out var statusId))
            return new StructureUnitCountDto();

        var statusProperties = properties
            .Where(p => p.AssessmentStatusId == statusId)
            .ToList();
        var unitCount = statusProperties.Count(IsApartmentUnit);

        return new StructureUnitCountDto
        {
            StructureCount = statusProperties.Count - unitCount,
            UnitCount = unitCount
        };
    }

    private static GeoSequencingZoneDataDto CalculateTotals(List<GeoSequencingZoneDataDto> zones)
    {
        return new GeoSequencingZoneDataDto
        {
            ZoneName = "TOTAL",
            RegisteredProperties = zones.Sum(z => z.RegisteredProperties),
            GeoSequencedProperties = new GeoSequencedPropertiesDto
            {
                StructureCount = zones.Sum(z => z.GeoSequencedProperties.StructureCount),
                UnitCount = zones.Sum(z => z.GeoSequencedProperties.UnitCount)
            },
            PropertyTypeBreakdown = new PropertyTypesBreakdownDto
            {
                Residential = zones.Sum(z => z.PropertyTypeBreakdown.Residential),
                NonResidential = zones.Sum(z => z.PropertyTypeBreakdown.NonResidential),
                Mixed = zones.Sum(z => z.PropertyTypeBreakdown.Mixed),
                PublicUtility = zones.Sum(z => z.PropertyTypeBreakdown.PublicUtility),
                UnderConstruction = zones.Sum(z => z.PropertyTypeBreakdown.UnderConstruction)
            },
            AssessmentStatusBreakdown = new AssessmentStatusBreakdownDto
            {
                Assessed = new StructureUnitCountDto
                {
                    StructureCount = zones.Sum(z => z.AssessmentStatusBreakdown.Assessed.StructureCount),
                    UnitCount = zones.Sum(z => z.AssessmentStatusBreakdown.Assessed.UnitCount)
                },
                Unassessed = new StructureUnitCountDto
                {
                    StructureCount = zones.Sum(z => z.AssessmentStatusBreakdown.Unassessed.StructureCount),
                    UnitCount = zones.Sum(z => z.AssessmentStatusBreakdown.Unassessed.UnitCount)
                },
                NewlyAssessedFound = new StructureUnitCountDto
                {
                    StructureCount = zones.Sum(z => z.AssessmentStatusBreakdown.NewlyAssessedFound.StructureCount),
                    UnitCount = zones.Sum(z => z.AssessmentStatusBreakdown.NewlyAssessedFound.UnitCount)
                },
                AssessmentInProcess = new StructureUnitCountDto
                {
                    StructureCount = zones.Sum(z => z.AssessmentStatusBreakdown.AssessmentInProcess.StructureCount),
                    UnitCount = zones.Sum(z => z.AssessmentStatusBreakdown.AssessmentInProcess.UnitCount)
                }
            }
        };
    }

    private static GeoSequencingWardDataDto CalculateWardTotals(List<GeoSequencingWardDataDto> wards)
    {
        return new GeoSequencingWardDataDto
        {
            WardNo = "TOTAL",
            RegisteredProperties = wards.Sum(w => w.RegisteredProperties),
            GeoSequencedProperties = new GeoSequencedPropertiesDto
            {
                StructureCount = wards.Sum(w => w.GeoSequencedProperties.StructureCount),
                UnitCount = wards.Sum(w => w.GeoSequencedProperties.UnitCount)
            },
            PropertyTypeBreakdown = new PropertyTypesBreakdownDto
            {
                Residential = wards.Sum(w => w.PropertyTypeBreakdown.Residential),
                NonResidential = wards.Sum(w => w.PropertyTypeBreakdown.NonResidential),
                Mixed = wards.Sum(w => w.PropertyTypeBreakdown.Mixed),
                PublicUtility = wards.Sum(w => w.PropertyTypeBreakdown.PublicUtility),
                UnderConstruction = wards.Sum(w => w.PropertyTypeBreakdown.UnderConstruction)
            },
            AssessmentStatusBreakdown = new AssessmentStatusBreakdownDto
            {
                Assessed = new StructureUnitCountDto
                {
                    StructureCount = wards.Sum(w => w.AssessmentStatusBreakdown.Assessed.StructureCount),
                    UnitCount = wards.Sum(w => w.AssessmentStatusBreakdown.Assessed.UnitCount)
                },
                Unassessed = new StructureUnitCountDto
                {
                    StructureCount = wards.Sum(w => w.AssessmentStatusBreakdown.Unassessed.StructureCount),
                    UnitCount = wards.Sum(w => w.AssessmentStatusBreakdown.Unassessed.UnitCount)
                },
                NewlyAssessedFound = new StructureUnitCountDto
                {
                    StructureCount = wards.Sum(w => w.AssessmentStatusBreakdown.NewlyAssessedFound.StructureCount),
                    UnitCount = wards.Sum(w => w.AssessmentStatusBreakdown.NewlyAssessedFound.UnitCount)
                },
                AssessmentInProcess = new StructureUnitCountDto
                {
                    StructureCount = wards.Sum(w => w.AssessmentStatusBreakdown.AssessmentInProcess.StructureCount),
                    UnitCount = wards.Sum(w => w.AssessmentStatusBreakdown.AssessmentInProcess.UnitCount)
                }
            }
        };
    }

    private static bool HasWardSummaryData(GeoSequencingWardDataDto ward)
        => ward.RegisteredProperties > 0
           || ward.GeoSequencedProperties.StructureCount > 0
           || ward.GeoSequencedProperties.UnitCount > 0
           || ward.PropertyTypeBreakdown.Residential > 0
           || ward.PropertyTypeBreakdown.NonResidential > 0
           || ward.PropertyTypeBreakdown.Mixed > 0
           || ward.PropertyTypeBreakdown.PublicUtility > 0
           || ward.PropertyTypeBreakdown.UnderConstruction > 0
           || ward.AssessmentStatusBreakdown.Assessed.StructureCount > 0
           || ward.AssessmentStatusBreakdown.Assessed.UnitCount > 0
           || ward.AssessmentStatusBreakdown.Unassessed.StructureCount > 0
           || ward.AssessmentStatusBreakdown.Unassessed.UnitCount > 0
           || ward.AssessmentStatusBreakdown.NewlyAssessedFound.StructureCount > 0
           || ward.AssessmentStatusBreakdown.NewlyAssessedFound.UnitCount > 0
           || ward.AssessmentStatusBreakdown.AssessmentInProcess.StructureCount > 0
           || ward.AssessmentStatusBreakdown.AssessmentInProcess.UnitCount > 0;

    private static bool IsApartmentUnit(GeoSequencingStagePropertyProjection property)
        => property.PropertyCategoryName == ApartmentCategoryName
           && !string.IsNullOrWhiteSpace(property.PartitionNo);

    private static bool IsMixedProperty(string? propertyTypeCode)
        => !string.IsNullOrWhiteSpace(propertyTypeCode)
           && MixedPropertyTypes.Contains(propertyTypeCode);

    private sealed record PropertyUseGroup(List<string> Types, List<string> Codes);

    private sealed record WardWiseSummaryContext(
        bool IsValid,
        int ZoneId,
        string ZoneName,
        int PageNumber,
        int PageSize,
        List<(int WardId, string WardNo)> Wards)
    {
        public int TotalCount => Wards.Count;

        public static WardWiseSummaryContext Invalid(int pageNumber, int pageSize)
            => new(false, 0, string.Empty, pageNumber, pageSize, new List<(int WardId, string WardNo)>());
    }


}
