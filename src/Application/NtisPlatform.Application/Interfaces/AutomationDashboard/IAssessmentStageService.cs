using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Application.Interfaces.AutomationDashboard;

/// <summary>
/// Service for Assessment dashboard business logic.
/// </summary>
public interface IAssessmentStageService
{
    /// <summary>
    /// Builds Assessment dashboard grid data for the requested tab type.
    /// </summary>
    Task<AssessmentGridResponseDto> GetAssessmentGridDataAsync(
        PropertySearchRequestDto? searchRequest,
        string type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends one Assessment property to Clerk approval.
    /// </summary>
    Task<SendToApproveResponseDto> SendToApproveAsync(
        SendToApproveRequestDto request,
        CancellationToken cancellationToken = default);
}
