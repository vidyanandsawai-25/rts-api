using NtisPlatform.Application.DTOs.Report;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service-to-service operations the ntis-report worker calls during async generation:
/// authenticate (SLT → LLT), pull report data page-by-page, upload the finished PDF,
/// and download secure images embedded in report data.
/// </summary>
public interface IReportWorkerService
{
    /// <summary>
    /// Validates and single-use-consumes the short-lived token, then issues a long-lived worker
    /// token plus the template name, data provider, parameters, and section list. Returns null
    /// when the SLT is unknown, already consumed, expired, or does not match the request id.
    /// </summary>
    Task<WorkerAuthenticateResultDto?> AuthenticateAsync(
        WorkerAuthenticateRequestDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Returns one page of data for the given section of the report request's dataset.
    /// </summary>
    Task<ReportDataPage> GetDataPageAsync(
        Guid reportRequestId,
        string section,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Stores the rendered PDF via the document service and returns its DocumentGuid. The worker
    /// then records the guid and Completed status on the queue row.
    /// </summary>
    Task<WorkerUploadResultDto> UploadAsync(
        Guid reportRequestId,
        Stream pdf,
        string fileName,
        long fileSizeBytes,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads the raw bytes and MIME type of a secure document by its guid.
    /// Used by the worker to resolve image columns before Crystal render.
    /// Returns null when the document does not exist or is inactive.
    /// </summary>
    Task<ImageResult?> GetImageAsync(Guid imageGuid, CancellationToken ct = default);
}

/// <summary>Raw image bytes + MIME type returned by <see cref="IReportWorkerService.GetImageAsync"/>.</summary>
public sealed record ImageResult(Stream Content, string MimeType);
