using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Application.Interfaces.AutomationDashboard;

/// <summary>
/// Service for Data Entry dashboard grid business logic.
/// </summary>
public interface IDataEntryStageService
{
    /// <summary>
    /// Builds Data Entry dashboard grid data from batched repository reads.
    /// </summary>
    Task<DataEntryGridResponseDto> GetDataEntryGridDataAsync(
        DashboardGridQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds Data Entry ward-wise summary data from repository snapshots.
    /// </summary>
    Task<DataEntryWardWiseSummaryResponseDto> GetDataEntryWardWiseSummaryAsync(
        WardWiseSummaryQueryParameters queryParameters,
        CancellationToken cancellationToken = default);
}

