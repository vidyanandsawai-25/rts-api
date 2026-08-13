using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Core.Interfaces.IAutomationDashboard;

/// <summary>
/// Repository interface for Data Entry and Quality Analyst stage operations.
/// Handles division-wise grid data for Data Entry and Quality Analyst workflow stage.
/// Optimized with granular methods to avoid DbContext concurrency issues.
/// </summary>
public interface IDataEntryStageRepository
{
    /// <summary>
    /// Reads active zones, optionally filtered by zone id.
    /// </summary>
    Task<List<(int ZoneId, string ZoneName, string ZoneNo)>> ReadZonesAsync(
        int? zoneId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the Internal Survey stage id.
    /// </summary>
    Task<int> ReadInternalSurveyStageIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the Assessment stage id.
    /// </summary>
    Task<int> ReadAssessmentStageIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the Property photo type id.
    /// </summary>
    Task<int> ReadPropertyPhotoTypeIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the Plan photo type id.
    /// </summary>
    Task<int> ReadPlanPhotoTypeIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads stage properties for selected zones.
    /// </summary>
    Task<List<DataEntryStagePropertyProjection>> ReadStagePropertiesForZonesAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null);

    /// <summary>
    /// Reads zone totals (structure and unit counts).
    /// </summary>
    Task<Dictionary<int, (int StructureCount, int UnitCount)>> ReadZoneTotalsAsync(
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null);

    /// <summary>
    /// Reads completed photos for selected zones.
    /// </summary>
    Task<List<DataEntryCompletedPhotoProjection>> ReadCompletedPhotosAsync(
        int workflowStageId,
        List<int> zoneIds,
        int propertyPhotoTypeId,
        int planPhotoTypeId,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null);

    /// <summary>
    /// Reads property types for selected zones.
    /// </summary>
    Task<List<DataEntryPropertyTypeSourceProjection>> ReadPropertyTypesAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null);

    /// <summary>
    /// Reads property uses for selected zones.
    /// </summary>
    Task<List<DataEntryPropertyUseSourceProjection>> ReadPropertyUsesAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null);

    /// <summary>
    /// Reads assessment status IDs by name.
    /// </summary>
    Task<Dictionary<string, int>> ReadAssessmentStatusIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads assessment status counts grouped by zone.
    /// </summary>
    Task<List<DataEntryAssessmentStatusCountProjection>> ReadAssessmentStatusCountsAsync(
        int workflowStageId,
        List<int> zoneIds,
        CancellationToken cancellationToken = default,
        DashboardGridQueryParameters? queryParameters = null);

    Task<DataEntryWardWiseSummaryProjection> ReadDataEntryWardWiseSummaryAsync(
        WardWiseSummaryQueryParameters queryParameters,
        CancellationToken cancellationToken = default);
}
