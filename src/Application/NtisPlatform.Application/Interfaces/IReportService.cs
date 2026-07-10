using NtisPlatform.Application.DTOs.Report;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Carries a stored report PDF stream back to the controller for download.
/// The caller is responsible for disposing <see cref="Content"/>.
/// </summary>
public sealed record ReportDownloadResult(Stream Content, string FileName, string ContentType);

public interface IReportService
{
    /// <summary>
    /// Queues an async report request: validates the report code, persists a Pending
    /// ReportRequest with a short-lived worker token, and returns the request id to poll.
    /// </summary>
    Task<ReportRequestSubmitResultDto> SubmitAsync(
        ReportRequestSubmitDto request,
        int requestedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the status of a report request, or null if it does not exist or does not
    /// belong to <paramref name="requestedByUserId"/> (caller maps null to 404).
    /// </summary>
    Task<ReportRequestStatusDto?> GetStatusAsync(
        Guid reportRequestId,
        int requestedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the caller's most recent report requests (newest first), capped at
    /// <paramref name="take"/> (clamped 1..100). Used by the UI's "My Reports" list.
    /// </summary>
    Task<IReadOnlyList<ReportRequestStatusDto>> GetMyRequestsAsync(
        int requestedByUserId,
        int take,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the stored PDF for a completed request, or null if it does not exist,
    /// is not yet completed, or does not belong to <paramref name="requestedByUserId"/>.
    /// </summary>
    Task<ReportDownloadResult?> GetDownloadAsync(
        Guid reportRequestId,
        int requestedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Internal lookup without ownership check — used by the worker notify endpoint to
    /// resolve the report owner's userId so the hub can broadcast to the right group.
    /// </summary>
    Task<ReportRequestStatusDto?> GetStatusInternalAsync(
        Guid reportRequestId,
        CancellationToken ct = default);
}
