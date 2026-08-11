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
        DashboardGridQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds ward-wise Geo-Sequencing summary data.
    /// </summary>
    Task<GeoSequencingWardWiseSummaryResponseDto> GetGeoSequencingWardWiseSummaryAsync(
        WardWiseSummaryQueryParameters queryParameters,
        CancellationToken cancellationToken = default);
}

