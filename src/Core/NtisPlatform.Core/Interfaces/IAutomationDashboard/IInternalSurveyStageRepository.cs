using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Core.Interfaces.IAutomationDashboard;

/// <summary>
/// Repository interface for Internal Survey stage database reads.
/// </summary>
public interface IInternalSurveyStageRepository
{
    /// <summary>
    /// Checks whether the workflow stage exists and is active.
    /// </summary>
    Task<bool> StageExistsAsync(int workflowStageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active zones, optionally filtered by zone id.
    /// </summary>
    Task<List<(int ZoneId, string ZoneName, string ZoneNo)>> ReadZonesAsync(int? zoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one active zone by id.
    /// </summary>
    Task<(int ZoneId, string ZoneName, string ZoneNo)> ReadZoneAsync(int zoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active wards for one zone.
    /// </summary>
    Task<List<(int WardId, string WardNo)>> ReadWardsInZoneAsync(int zoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active Geo-Sequencing workflow stage id.
    /// </summary>
    Task<int> ReadGeoSequencingStageIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assessed and unassessed assessment status ids.
    /// </summary>
    Task<(int AssessedId, int UnassessedId)> ReadAssessedAndUnassessedStatusIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active property photo type id used by Internal Survey.
    /// </summary>
    Task<int> ReadPropertyPhotoTypeIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads stage properties for selected zones.
    /// </summary>
    Task<List<InternalSurveyStagePropertyProjection>> ReadStagePropertiesForZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        bool requirePropertyNo,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null);

    /// <summary>
    /// Reads property use rows for selected stage properties in zones.
    /// </summary>
    Task<List<InternalSurveyPropertyUseSourceProjection>> ReadPropertyUsesForStageInZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        bool requirePropertyNo,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null);

    /// <summary>
    /// Reads property photo counts grouped by zone.
    /// </summary>
    Task<List<InternalSurveyPhotoCountProjection>> ReadPhotoCountsByZoneAsync(
        int workflowStageId,
        List<int> zoneIds,
        int propertyPhotoTypeId,
        CancellationToken cancellationToken = default,
        PropertySearchRequestDto? searchRequest = null);

    /// <summary>
    /// Reads property photo counts grouped by ward.
    /// </summary>
    Task<List<InternalSurveyPhotoCountProjection>> ReadPhotoCountsByWardAsync(
        int workflowStageId,
        List<int> wardIds,
        int propertyPhotoTypeId,
        CancellationToken cancellationToken = default);
}
