using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Application.Interfaces.AutomationDashboard;

/// <summary>
/// Service for Internal Survey dashboard business logic.
/// </summary>
public interface IInternalSurveyStageService
{
    /// <summary>
    /// Builds Internal Survey dashboard grid data from batched repository reads.
    /// </summary>
    Task<InternalSurveyGridResponseDto> GetInternalSurveyGridDataAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds Internal Survey ward-wise summary data from batched repository reads.
    /// </summary>
    Task<InternalSurveyWardWiseSummaryResponseDto> GetInternalSurveyWardWiseSummaryAsync(
        int zoneId,
        int workflowStageId,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken = default);
}
