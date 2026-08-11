using NtisPlatform.Core.Models;
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
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default);
}
