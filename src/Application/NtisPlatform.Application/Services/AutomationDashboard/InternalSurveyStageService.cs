using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Application.Helpers.AutomationDashboard;
using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services.AutomationDashboard;

/// <summary>
/// Service for Internal Survey dashboard grid assembly and summary rules.
/// Implements optimized data aggregation with proper exception handling and logging.
/// </summary>
public class InternalSurveyStageService : IInternalSurveyStageService
{
    private readonly IInternalSurveyStageRepository _repository;
    private readonly ILogger<InternalSurveyStageService> _logger;

    public InternalSurveyStageService(
        IInternalSurveyStageRepository repository,
        ILogger<InternalSurveyStageService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Builds the Internal Survey grid while keeping aggregation rules in the service.
    /// Implements parallel data fetching and proper exception handling.
    /// </summary>
    public async Task<InternalSurveyGridResponseDto> GetInternalSurveyGridDataAsync(
        DashboardGridQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Fetch zones
            var zones = await _repository.ReadZonesAsync(null, cancellationToken);
            if (!zones.Any())
            {
                _logger.LogInformation("No active zones found for request");
                return new InternalSurveyGridResponseDto
                {
                    TotalRow = new InternalSurveyDivisionDataDto { DivisionName = "TOTAL" }
                };
            }

            var zoneIds = zones.Select(z => z.ZoneId).ToList();

            // Fetch all required data in parallel for better performance
            var (geoProperties, internalProperties, propertyUses, photoCounts, geoStageId, assessedId, unassessedId) =
                await FetchGridDataAsync(queryParameters, zoneIds, cancellationToken);

            // Build property use groups using common helper
            var propertyUseGroups = WorkflowStagePropertyTypeBuilder.BuildPropertyUseGroups(
                propertyUses,
                p => p.PropertyId,
                p => p.Type,
                p => p.TypeOfUseCode);

            // Group properties by zone for efficient lookup
            var geoPropertiesByZone = geoProperties
                .GroupBy(p => p.ZoneId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var internalPropertiesByZone = internalProperties
                .GroupBy(p => p.ZoneId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var photoCountsByZone = photoCounts
                .Where(p => p.ZoneId.HasValue)
                .ToDictionary(p => p.ZoneId!.Value, p => p.Count);

            // Build division data
            var result = new InternalSurveyGridResponseDto();
            foreach (var (zoneId, zoneName, zoneNo) in zones)
            {
                geoPropertiesByZone.TryGetValue(zoneId, out var geoProps);
                internalPropertiesByZone.TryGetValue(zoneId, out var internalProps);
                photoCountsByZone.TryGetValue(zoneId, out var photoCount);

                result.DivisionData.Add(WorkflowStageDataBuilder.BuildInternalSurveyDivisionData(
                    zoneId,
                    zoneName,
                    zoneNo,
                    geoProps ?? new List<InternalSurveyStagePropertyProjection>(),
                    internalProps ?? new List<InternalSurveyStagePropertyProjection>(),
                    propertyUseGroups,
                    assessedId,
                    unassessedId,
                    photoCount));
            }

            result.TotalRow = CalculateDivisionTotals(result.DivisionData);

            _logger.LogInformation(
                "Successfully retrieved Internal Survey grid data for stage {WorkflowStageId} with {DivisionCount} divisions",
                queryParameters.WorkflowStageId,
                result.DivisionData.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Internal Survey grid data");
            throw;
        }
    }

    /// <summary>
    /// Builds the Internal Survey ward-wise summary with proper validation and exception handling.
    /// Implements parallel data fetching for better performance.
    /// </summary>
    public async Task<InternalSurveyWardWiseSummaryResponseDto> GetInternalSurveyWardWiseSummaryAsync(
        WardWiseSummaryQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build context
            var (normalizedPageNumber, normalizedPageSize) = WorkflowStagePagingHelper.NormalizePaging(queryParameters.PageNumber, queryParameters.PageSize);
            var context = await BuildWardWiseContextAsync(
                queryParameters.ZoneId,
                normalizedPageNumber,
                normalizedPageSize,
                cancellationToken);

            var zoneIds = new List<int> { queryParameters.ZoneId };

            // Fetch all required data in parallel
            var (geoProperties, internalProperties, propertyUses, photoCounts, assessedId, unassessedId) =
                await FetchWardDataAsync(queryParameters, zoneIds, context.Wards, cancellationToken);

            // Build property use groups using common helper
            var propertyUseGroups = WorkflowStagePropertyTypeBuilder.BuildPropertyUseGroups(
                propertyUses,
                p => p.PropertyId,
                p => p.Type,
                p => p.TypeOfUseCode);

            // Group properties by ward for efficient lookup
            var geoPropertiesByWard = geoProperties
                .GroupBy(p => p.WardId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var internalPropertiesByWard = internalProperties
                .GroupBy(p => p.WardId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var photoCountsByWard = photoCounts
                .Where(p => p.WardId.HasValue)
                .ToDictionary(p => p.WardId!.Value, p => p.Count);

            // Build result
            var result = new InternalSurveyWardWiseSummaryResponseDto
            {
                ZoneId = context.ZoneId,
                ZoneName = context.ZoneName,
                PageNumber = context.PageNumber,
                PageSize = context.PageSize,
                TotalCount = context.TotalCount
            };

            // Build all ward data
            var allWardData = new List<InternalSurveyWardDataDto>(context.Wards.Count);
            foreach (var (wardId, wardNo) in context.Wards)
            {
                geoPropertiesByWard.TryGetValue(wardId, out var geoProps);
                internalPropertiesByWard.TryGetValue(wardId, out var internalProps);
                photoCountsByWard.TryGetValue(wardId, out var photoCount);

                allWardData.Add(WorkflowStageDataBuilder.BuildInternalSurveyWardData(
                    wardId,
                    wardNo,
                    geoProps ?? new List<InternalSurveyStagePropertyProjection>(),
                    internalProps ?? new List<InternalSurveyStagePropertyProjection>(),
                    propertyUseGroups,
                    assessedId,
                    unassessedId,
                    photoCount));
            }

            // Order by data presence and apply pagination
            var orderedWardData = allWardData
                .OrderByDescending(GetWardSummaryScore)
                .ThenBy(w => w.WardNo)
                .ToList();

            result.TotalRow = CalculateWardTotals(allWardData);
            result.WardData = WorkflowStagePagingHelper.PageWardData(orderedWardData, context.PageNumber, context.PageSize);

            _logger.LogInformation(
                "Successfully retrieved ward-wise summary for zone {ZoneId}, stage {WorkflowStageId}, page {PageNumber}",
                queryParameters.ZoneId,
                queryParameters.WorkflowStageId,
                context.PageNumber);

            return result;
        }
        catch (ZoneNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving ward-wise summary for zone {ZoneId}, stage {WorkflowStageId}",
                queryParameters.ZoneId,
                queryParameters.WorkflowStageId);
            throw;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Fetches all required data for grid display sequentially to avoid DbContext concurrency issues
    /// </summary>
    private async Task<(
        List<InternalSurveyStagePropertyProjection> GeoProperties,
        List<InternalSurveyStagePropertyProjection> InternalProperties,
        List<InternalSurveyPropertyUseSourceProjection> PropertyUses,
        List<InternalSurveyPhotoCountProjection> PhotoCounts,
        int GeoSequencingStageId,
        int AssessedStatusId,
        int UnassessedStatusId)> FetchGridDataAsync(
        DashboardGridQueryParameters queryParameters,
        List<int> zoneIds,
        CancellationToken cancellationToken)
    {
        var workflowStageId = queryParameters.WorkflowStageId!.Value;

        // Execute sequentially to avoid DbContext concurrency issues
        var geoStageId = await _repository.ReadGeoSequencingStageIdAsync(cancellationToken);
        var (assessedId, unassessedId) = await _repository.ReadAssessedAndUnassessedStatusIdsAsync(cancellationToken);
        var photoTypeId = await _repository.ReadPropertyPhotoTypeIdAsync(cancellationToken);

        var geoProperties = await _repository.ReadStagePropertiesForZonesAsync(
            geoStageId, zoneIds, requirePropertyNo: true, cancellationToken, queryParameters);
        var internalProperties = await _repository.ReadStagePropertiesForZonesAsync(
            workflowStageId, zoneIds, requirePropertyNo: true, cancellationToken, queryParameters);
        var propertyUses = await _repository.ReadPropertyUsesForStageInZonesAsync(
            workflowStageId, zoneIds, requirePropertyNo: true, cancellationToken, queryParameters);
        var photoCounts = await _repository.ReadPhotoCountsByZoneAsync(
            workflowStageId, zoneIds, photoTypeId, cancellationToken, queryParameters);

        return (geoProperties, internalProperties, propertyUses, photoCounts, geoStageId, assessedId, unassessedId);
    }

    /// <summary>
    /// Fetches all required data for ward-wise summary sequentially to avoid DbContext concurrency issues
    /// </summary>
    private async Task<(
        List<InternalSurveyStagePropertyProjection> GeoProperties,
        List<InternalSurveyStagePropertyProjection> InternalProperties,
        List<InternalSurveyPropertyUseSourceProjection> PropertyUses,
        List<InternalSurveyPhotoCountProjection> PhotoCounts,
        int AssessedStatusId,
        int UnassessedStatusId)> FetchWardDataAsync(
        WardWiseSummaryQueryParameters queryParameters,
        List<int> zoneIds,
        List<(int WardId, string WardNo)> wards,
        CancellationToken cancellationToken)
    {
        var workflowStageId = queryParameters.WorkflowStageId;
        var wardIds = wards.Select(w => w.WardId).ToList();

        // Execute sequentially to avoid DbContext concurrency issues
        var geoStageId = await _repository.ReadGeoSequencingStageIdAsync(cancellationToken);
        var (assessedId, unassessedId) = await _repository.ReadAssessedAndUnassessedStatusIdsAsync(cancellationToken);
        var photoTypeId = await _repository.ReadPropertyPhotoTypeIdAsync(cancellationToken);

        var geoProperties = await _repository.ReadStagePropertiesForZonesAsync(
            geoStageId, zoneIds, requirePropertyNo: true, cancellationToken, queryParameters);
        var internalProperties = await _repository.ReadStagePropertiesForZonesAsync(
            workflowStageId, zoneIds, requirePropertyNo: true, cancellationToken, queryParameters);
        var propertyUses = await _repository.ReadPropertyUsesForStageInZonesAsync(
            workflowStageId, zoneIds, requirePropertyNo: true, cancellationToken, queryParameters);
        var photoCounts = await _repository.ReadPhotoCountsByWardAsync(
            workflowStageId, wardIds, photoTypeId, cancellationToken);

        return (geoProperties, internalProperties, propertyUses, photoCounts, assessedId, unassessedId);
    }

    /// <summary>
    /// Loads zone and ward context required for ward-wise summary.
    /// </summary>
    private async Task<WardWiseSummaryContext> BuildWardWiseContextAsync(
        int zoneId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var zone = await _repository.ReadZoneAsync(zoneId, cancellationToken);
        if (zone.ZoneId == 0)
        {
            throw new ZoneNotFoundException(zoneId);
        }

        var wards = await _repository.ReadWardsInZoneAsync(zoneId, cancellationToken);

        return new WardWiseSummaryContext(
            ZoneId: zone.ZoneId,
            ZoneName: zone.ZoneName,
            PageNumber: pageNumber,
            PageSize: pageSize,
            Wards: wards);
    }

    /// <summary>
    /// Calculates division totals from all division data
    /// </summary>
    private static InternalSurveyDivisionDataDto CalculateDivisionTotals(List<InternalSurveyDivisionDataDto> divisionData)
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

    /// <summary>
    /// Calculates ward totals from all ward data
    /// </summary>
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

    /// <summary>
    /// Checks if ward has any summary data
    /// </summary>
    private static bool HasWardSummaryData(InternalSurveyWardDataDto ward)
        => GetWardSummaryScore(ward) > 0;

    private static int GetWardSummaryScore(InternalSurveyWardDataDto ward)
        => ward.GeoSequencingProperties.Structure
           + ward.GeoSequencingProperties.Unit
           + ward.SurveyProperties.Structure
           + ward.SurveyProperties.Unit
           + ward.PropertyType.Residential
           + ward.PropertyType.NonResidential
           + ward.PropertyType.Mixed
           + ward.PropertyType.PublicUtility
           + ward.PropertyType.UnderConstruction
           + ward.AssessedProperties.Structure
           + ward.AssessedProperties.Units
           + ward.UnassessedProperties.Structure
           + ward.UnassessedProperties.Units
           + ward.NewlyAssessedFound.Structure
           + ward.NewlyAssessedFound.Unit
           + ward.AssessmentInprocess.Structure
           + ward.AssessmentInprocess.Unit
           + ward.PhotoCount;

    #endregion

    #region Context Records

    private sealed record WardWiseSummaryContext(
        int ZoneId,
        string ZoneName,
        int PageNumber,
        int PageSize,
        List<(int WardId, string WardNo)> Wards)
    {
        public int TotalCount => Wards.Count;
    }

    #endregion
}

