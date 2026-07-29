using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Core.Interfaces.IAutomationDashboard;

/// <summary>
/// Repository interface for Geo-Sequencing stage database reads.
/// </summary>
public interface IGeoSequencingStageRepository
{
    /// <summary>
    /// Checks whether the workflow stage exists and is active.
    /// </summary>
    Task<bool> StageExistsAsync(int workflowStageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active zones, optionally filtered by zone id.
    /// </summary>
    Task<List<(int ZoneId, string ZoneName)>> ReadZonesAsync(int? zoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one active zone by id.
    /// </summary>
    Task<(int ZoneId, string ZoneName)> ReadZoneAsync(int zoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active wards for one zone.
    /// </summary>
    Task<List<(int WardId, string WardNo)>> ReadWardsInZoneAsync(int zoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads workflow stage properties for selected zones.
    /// </summary>
    Task<List<GeoSequencingStagePropertyProjection>> ReadStagePropertiesForZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null);

    /// <summary>
    /// Reads registered property counts grouped by zone.
    /// </summary>
    Task<Dictionary<int, int>> ReadRegisteredCountsByZoneAsync(
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null);

    /// <summary>
    /// Reads registered property counts grouped by ward.
    /// </summary>
    Task<Dictionary<int, int>> ReadRegisteredCountsByWardAsync(
        List<int> wardIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads property use rows for selected stage properties in zones.
    /// </summary>
    Task<List<GeoSequencingPropertyUseProjection>> ReadPropertyUsesForZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null);

    /// <summary>
    /// Reads assessment status ids by status name.
    /// </summary>
    Task<Dictionary<string, int>> ReadAssessmentStatusIdsByNameAsync(CancellationToken cancellationToken = default);


}
