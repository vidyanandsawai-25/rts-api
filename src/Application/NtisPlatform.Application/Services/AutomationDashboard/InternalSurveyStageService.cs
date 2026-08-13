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

            result.TotalRow = WorkflowStageDataBuilder.CalculateInternalSurveyDivisionTotals(result.DivisionData);

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
                .OrderByDescending(WorkflowStageDataBuilder.GetInternalSurveyWardSummaryScore)
                .ThenBy(w => w.WardNo)
                .ToList();

            result.TotalRow = WorkflowStageDataBuilder.CalculateInternalSurveyWardTotals(allWardData);
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

