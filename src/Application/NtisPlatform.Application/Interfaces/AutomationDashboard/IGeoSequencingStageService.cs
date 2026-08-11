using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Application.Interfaces.AutomationDashboard;

/// <summary>
/// Service interface for Geo-Sequencing dashboard grid assembly.
/// </summary>
public interface IGeoSequencingStageService
{
    /// <summary>
    /// Builds zone-wise Geo-Sequencing grid data.
    /// </summary>
    Task<GeoSequencingGridResponseDto> GetGeoSequencingGridDataAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds ward-wise Geo-Sequencing summary data.
    /// </summary>
    Task<GeoSequencingWardWiseSummaryResponseDto> GetGeoSequencingWardWiseSummaryAsync(
        int zoneId,
        int workflowStageId,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken = default);
}
