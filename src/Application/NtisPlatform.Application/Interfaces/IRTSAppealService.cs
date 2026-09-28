using NtisPlatform.Application.DTOs.RTSAppeal;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface defining domain and appellate workflow operations for Right to Services (RTS) Appeals
/// under the Maharashtra Right to Public Services Act (MRTSA 2015).
/// </summary>
public interface IRTSAppealService
{
    /// <summary>
    /// Retrieves all active statutory grounds / categories for filing an appeal.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of appeal type master DTOs.</returns>
    Task<List<RTSAppealTypeDto>> GetAppealTypesAsync(CancellationToken ct = default);

    /// <summary>
    /// Evaluates whether an application is eligible for First or Second Appeal under MRTSA 2015 statutory rules.
    /// Analyzes rejection date, deemed delay (45 days), SLA breach, and filing window constraints (30-day limit or 90-day condonation).
    /// </summary>
    /// <param name="applicationNo">Original application reference number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Statutory eligibility evaluation result.</returns>
    Task<RTSAppealLevelCheckResultDto> CheckAppealLevelAsync(string applicationNo, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a comprehensive read-only summary of the original RTS application, including applicant details,
    /// dynamic form answers, uploaded documents, and complete application history timeline.
    /// </summary>
    /// <param name="applicationNo">Original application reference number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Application summary DTO or null if not found.</returns>
    Task<RTSAppealApplicationSummaryDto?> GetApplicationSummaryForAppealAsync(string applicationNo, CancellationToken ct = default);

    /// <summary>
    /// Submits a citizen appeal (First or Second Appeal) with statutory validation, generates an official appeal tracking number,
    /// and logs an immutable timeline entry wrapped in a transaction.
    /// </summary>
    /// <param name="dto">Appeal creation payload containing grievance grounds and delay justification.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Submission result DTO containing generated appeal number.</returns>
    Task<RTSAppealApplicationResultDto> SubmitAppealAsync(CreateRTSAppealApplicationDto dto, CancellationToken ct = default);

    /// <summary>
    /// Computes aggregated metrics for the appellate officer dashboard cards via an optimized single database query.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dashboard metrics summary DTO.</returns>
    Task<RTSAppealDashboardCardsDto> GetAppealDashboardCardsAsync(CancellationToken ct = default);

    /// <summary>
    /// Alias matching RTSApplicationApprovalService.GetDashboardCardsDataAsync.
    /// </summary>
    Task<RTSAppealDashboardCardsCountDto> GetDashboardCardsDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a paginated and filtered list of appeal applications for the appellate officer desk.
    /// </summary>
    /// <param name="queryParams">Filtering and pagination parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated appeal grid result.</returns>
    Task<PagedResult<RTSAppealGridItemDto>> GetAppealDashboardGridAsync(RTSAppealQueryParameters queryParams, CancellationToken ct = default);

    /// <summary>
    /// Alias matching RTSApplicationApprovalService.GetAllDashboardApplicationAsync.
    /// </summary>
    Task<PagedResult<RTSAppealDashboardDetailsDto>> GetAllDashboardAppealsAsync(RTSAppealQueryParameters queryParams, CancellationToken ct = default);

    /// <summary>
    /// Retrieves complete appellate dossier for an appeal including original application snapshot, citizen grounds,
    /// dynamic form answers, documents, timeline history, and authorized action permissions.
    /// </summary>
    /// <param name="appealId">Primary key of the appeal record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Appellate processing details DTO or null if not found.</returns>
    Task<RTSAppealProcessDetailsDto?> GetAppealProcessDetailsAsync(int appealId, CancellationToken ct = default);

    /// <summary>
    /// Alias matching ViewApplicationApprovalSummaryAsync naming pattern.
    /// </summary>
    Task<RTSAppealProcessDetailsDto?> ViewAppealSummaryAsync(int appealId, CancellationToken ct = default);

    /// <summary>
    /// Adjudicates an appeal by recording the officer's decision (Approve, Reject, or Return with directions),
    /// appending an immutable history record wrapped in a transaction.
    /// </summary>
    /// <param name="dto">Action payload with official remarks and decision.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the action was processed successfully.</returns>
    Task<bool> ProcessAppealOfficerActionAsync(ProcessRTSAppealActionDto dto, CancellationToken ct = default);

    /// <summary>
    /// Alias matching ProcessAppealOfficerActionAsync.
    /// </summary>
    Task<bool> ProcessAppealActionAsync(ProcessRTSAppealActionDto dto, CancellationToken ct = default);
}

/// <summary>
/// Backward-compatibility alias for previous casing.
/// </summary>
public interface IRtsAppealService : IRTSAppealService
{
}
