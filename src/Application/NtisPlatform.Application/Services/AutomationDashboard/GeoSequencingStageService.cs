using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Application.Helpers.AutomationDashboard;
using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services.AutomationDashboard;

/// <summary>
/// Service for Geo-Sequencing dashboard grid assembly and summary rules.
/// Implements optimized data aggregation with proper exception handling.
/// </summary>
public class GeoSequencingStageService : IGeoSequencingStageService
{
    private readonly IGeoSequencingStageRepository _repository;
    private readonly ILogger<GeoSequencingStageService> _logger;

    public GeoSequencingStageService(
        IGeoSequencingStageRepository repository,
        ILogger<GeoSequencingStageService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Builds the Geo-Sequencing grid while keeping aggregation rules in the service.
    /// </summary>
    public async Task<GeoSequencingGridResponseDto> GetGeoSequencingGridDataAsync(
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
                return new GeoSequencingGridResponseDto();
            }

            var zoneIds = zones.Select(z => z.ZoneId).ToList();

            // Fetch all required data in parallel for better performance
            var (stageProperties, propertyUses, registeredCounts, statusIdsByName) = 
                await FetchGridDataAsync(queryParameters, zoneIds, cancellationToken);

            // Build property use groups
            var propertyUseGroups = BuildPropertyUseGroups(propertyUses);

            // Group properties by zone for efficient lookup
            var propertiesByZone = stageProperties.GroupBy(p => p.ZoneId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Build zone data
            var result = new GeoSequencingGridResponseDto();
            foreach (var (zoneId, zoneName, zoneNo) in zones)
            {
                propertiesByZone.TryGetValue(zoneId, out var zoneProperties);
                registeredCounts.TryGetValue(zoneId, out var registeredCount);

                result.Zones.Add(WorkflowStageDataBuilder.BuildGeoSequencingZoneData(
                    zoneId,
                    zoneName,
                    zoneNo,
                    registeredCount,
                    zoneProperties ?? new List<GeoSequencingStagePropertyProjection>(),
                    propertyUseGroups,
                    statusIdsByName));
            }

            result.TotalRow = WorkflowStageDataBuilder.CalculateGeoSequencingZoneTotals(result.Zones);

            _logger.LogInformation(
                "Successfully retrieved Geo-Sequencing grid data for stage {WorkflowStageId} with {ZoneCount} zones", 
                queryParameters.WorkflowStageId, 
                result.Zones.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Geo-Sequencing grid data");
            throw;
        }
    }

    /// <summary>
    /// Builds the Geo-Sequencing ward-wise summary from raw repository reads.
    /// </summary>
    public async Task<GeoSequencingWardWiseSummaryResponseDto> GetGeoSequencingWardWiseSummaryAsync(
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
            var (stageProperties, propertyUses, registeredCounts, statusIdsByName) = 
                await FetchWardDataAsync(queryParameters, zoneIds, context.Wards, cancellationToken);

            // Build property use groups
            var propertyUseGroups = BuildPropertyUseGroups(propertyUses);

            // Group properties by ward for efficient lookup
            var propertiesByWard = stageProperties
                .GroupBy(p => p.WardId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Build result
            var result = new GeoSequencingWardWiseSummaryResponseDto
            {
                ZoneId = context.ZoneId,
                ZoneName = context.ZoneName,
                PageNumber = context.PageNumber,
                PageSize = context.PageSize,
                TotalCount = context.TotalCount
            };

            // Build all ward data
            var allWardData = new List<GeoSequencingWardDataDto>(context.Wards.Count);
            foreach (var (wardId, wardNo) in context.Wards)
            {
                propertiesByWard.TryGetValue(wardId, out var wardProperties);
                registeredCounts.TryGetValue(wardId, out var registeredCount);

                allWardData.Add(WorkflowStageDataBuilder.BuildGeoSequencingWardData(
                    wardId,
                    wardNo,
                    registeredCount,
                    wardProperties ?? new List<GeoSequencingStagePropertyProjection>(),
                    propertyUseGroups,
                    statusIdsByName));
            }

            // Order by data presence and apply pagination
            var orderedWardData = allWardData
                .OrderByDescending(WorkflowStageDataBuilder.GetGeoSequencingWardSummaryScore)
                .ThenBy(w => w.WardNo)
                .ToList();

            result.TotalRow = WorkflowStageDataBuilder.CalculateGeoSequencingWardTotals(allWardData);
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
        List<GeoSequencingStagePropertyProjection> StageProperties,
        List<GeoSequencingPropertyUseProjection> PropertyUses,
        Dictionary<int, int> RegisteredCounts,
        Dictionary<string, int> StatusIdsByName)> FetchGridDataAsync(
        DashboardGridQueryParameters queryParameters,
        List<int> zoneIds,
        CancellationToken cancellationToken)
    {
        var workflowStageId = queryParameters.WorkflowStageId!.Value;

        // Execute sequentially to avoid DbContext concurrency issues
        var stageProperties = await _repository.ReadStagePropertiesForZonesAsync(
            workflowStageId, zoneIds, cancellationToken, queryParameters);
        var propertyUses = await _repository.ReadPropertyUsesForZonesAsync(
            workflowStageId, zoneIds, cancellationToken, queryParameters);
        var registeredCounts = await _repository.ReadRegisteredCountsByZoneAsync(
            zoneIds, cancellationToken, queryParameters);
        var statusIdsByName = await _repository.ReadAssessmentStatusIdsByNameAsync(cancellationToken);

        return (stageProperties, propertyUses, registeredCounts, statusIdsByName);
    }

    /// <summary>
    /// Fetches all required data for ward-wise summary sequentially to avoid DbContext concurrency issues
    /// </summary>
    private async Task<(
        List<GeoSequencingStagePropertyProjection> StageProperties,
        List<GeoSequencingPropertyUseProjection> PropertyUses,
        Dictionary<int, int> RegisteredCounts,
        Dictionary<string, int> StatusIdsByName)> FetchWardDataAsync(
        WardWiseSummaryQueryParameters queryParameters,
        List<int> zoneIds,
        List<(int WardId, string WardNo)> wards,
        CancellationToken cancellationToken)
    {
        var workflowStageId = queryParameters.WorkflowStageId;
        var wardIds = wards.Select(w => w.WardId).ToList();

        // Execute sequentially to avoid DbContext concurrency issues
        var stageProperties = await _repository.ReadStagePropertiesForZonesAsync(
            workflowStageId, zoneIds, cancellationToken, queryParameters);
        var propertyUses = await _repository.ReadPropertyUsesForZonesAsync(
            workflowStageId, zoneIds, cancellationToken, queryParameters);
        var registeredCounts = await _repository.ReadRegisteredCountsByWardAsync(
            wardIds, cancellationToken);
        var statusIdsByName = await _repository.ReadAssessmentStatusIdsByNameAsync(cancellationToken);

        return (stageProperties, propertyUses, registeredCounts, statusIdsByName);
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
    /// Groups property uses by property ID for efficient lookup using common helper
    /// </summary>
    private static Dictionary<int, PropertyUseGroup> BuildPropertyUseGroups(
        List<GeoSequencingPropertyUseProjection> propertyUses)
        => WorkflowStagePropertyTypeBuilder.BuildPropertyUseGroups(
            propertyUses,
            p => p.PropertyId,
            p => p.Type,
            p => p.TypeOfUseCode);

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

