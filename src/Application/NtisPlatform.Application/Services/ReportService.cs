using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Reporting;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// UI-facing async reporting service: queues report requests, reports their status, and serves
/// the finished PDF. Generation itself is performed off-server by the ntis-report worker, which
/// claims the queued request, pulls data, renders, and stores the PDF via the document service.
/// </summary>
public class ReportService : IReportService
{
    private readonly ReportDefinitionCacheService _cache;
    private readonly IRepository<ReportDefinitionEntity, int> _reportDefinitionRepository;
    private readonly IReportingRepository<ReportRequestEntity, Guid> _reportRequestRepository;
    private readonly IReportingUnitOfWork _reportingUnitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IDocumentService _documentService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ReportingOptions _options;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        ReportDefinitionCacheService cache,
        IRepository<ReportDefinitionEntity, int> reportDefinitionRepository,
        IReportingRepository<ReportRequestEntity, Guid> reportRequestRepository,
        IReportingUnitOfWork reportingUnitOfWork,
        ITokenService tokenService,
        IDocumentService documentService,
        IFileStorageService fileStorageService,
        IOptions<ReportingOptions> options,
        ILogger<ReportService> logger)
    {
        _cache = cache;
        _reportDefinitionRepository = reportDefinitionRepository;
        _reportRequestRepository = reportRequestRepository;
        _reportingUnitOfWork = reportingUnitOfWork;
        _tokenService = tokenService;
        _documentService = documentService;
        _fileStorageService = fileStorageService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ReportRequestSubmitResultDto> SubmitAsync(
        ReportRequestSubmitDto request,
        int requestedByUserId,
        CancellationToken ct = default)
    {
        // Validate the report exists & is active — cache first, DB fallback on miss.
        var definition = _cache.TryGetDefinition(request.ReportCode);
        if (definition is null)
        {
            _logger.LogWarning(
                "Report '{ReportCode}' not in cache, falling back to DB lookup.", request.ReportCode);

            definition = await _reportDefinitionRepository.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ReportCode == request.ReportCode && r.IsActive, ct)
                ?? throw new KeyNotFoundException(
                    $"Report '{request.ReportCode}' not found or inactive.");
        }

        if (string.IsNullOrWhiteSpace(_options.PlatformBaseUrl))
            throw new InvalidOperationException(
                "Reporting:PlatformBaseUrl is not configured. The worker cannot call back this " +
                "platform instance without knowing its URL. Set it in appsettings.");

        var now = DateTime.UtcNow;
        var reportRequestId = Guid.NewGuid();
        var entity = new ReportRequestEntity
        {
            ReportRequestId = reportRequestId,
            ReportCode = definition.ReportCode,
            ParametersJson = JsonSerializer.Serialize(request.Parameters),
            Status = ReportRequestStatus.Pending,
            RequestedByUserId = requestedByUserId,
            // Optional tenant tag — metadata / dashboard / queue routing key.
            OrganizationId = _options.OrganizationId > 0 ? _options.OrganizationId : (int?)null,
            // Stamped here so the shared worker knows which tenant's platform to call back.
            PlatformBaseUrl = _options.PlatformBaseUrl,
            CreatedDate = now,
            // JWT credential the worker presents to /authenticate in exchange for an LLT.
            // Carries scope="report-slt" + reportRequestId so the platform can validate it without
            // a DB lookup by value. SltMinutes must cover max queue wait + retry cycle (≥ 120 min).
            ShortLivedToken = _tokenService.GenerateShortLivedToken(reportRequestId, requestedByUserId, _options.SltMinutes),
            SltExpiresAt = now.AddMinutes(_options.SltMinutes),
            SltConsumed = false,
        };

        await _reportRequestRepository.AddAsync(entity, ct);
        await _reportingUnitOfWork.SaveChangesAsync(ct);

        // That's it — the row (Status = Pending) IS the hand-off. The ntis-report worker polls
        // dbo.ReportRequest for Pending rows and enqueues/renders them itself, so the two repos share
        // only this database (no cross-repo job-contract assembly).
        _logger.LogInformation(
            "Queued report request {RequestId} for {ReportCode} by user {UserId}.",
            entity.ReportRequestId, entity.ReportCode, requestedByUserId);

        return new ReportRequestSubmitResultDto
        {
            ReportRequestId = entity.ReportRequestId,
            Status = entity.Status.ToString(),
        };
    }

    public async Task<ReportRequestStatusDto?> GetStatusAsync(
        Guid reportRequestId,
        int requestedByUserId,
        CancellationToken ct = default)
    {
        var entity = await _reportRequestRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReportRequestId == reportRequestId, ct);

        // Null or not-owned → caller maps to 404 (don't leak existence of others' requests).
        if (entity is null || entity.RequestedByUserId != requestedByUserId)
            return null;

        return MapToStatusDto(entity);
    }

    public async Task<ReportRequestStatusDto?> GetStatusInternalAsync(
        Guid reportRequestId,
        CancellationToken ct = default)
    {
        var entity = await _reportRequestRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReportRequestId == reportRequestId, ct);

        return entity is null ? null : MapToStatusDto(entity);
    }

    private static ReportRequestStatusDto MapToStatusDto(ReportRequestEntity entity) =>
        new()
        {
            ReportRequestId = entity.ReportRequestId,
            ReportCode = entity.ReportCode,
            Status = entity.Status.ToString(),
            CreatedDate = entity.CreatedDate,
            StartedDate = entity.StartedDate,
            CompletedDate = entity.CompletedDate,
            ErrorMessage = entity.ErrorMessage,
            RequestedByUserId = entity.RequestedByUserId,
            DownloadAvailable =
                entity.Status == ReportRequestStatus.Completed && entity.OutputDocumentGuid.HasValue,
        };

    public async Task<IReadOnlyList<ReportRequestStatusDto>> GetMyRequestsAsync(
        int requestedByUserId,
        int take,
        CancellationToken ct = default)
    {
        var clampedTake = take < 1 ? 25 : Math.Min(take, 100);

        // Materialize entities first (the Status enum uses a value converter, so ToString() can't
        // be translated inside a projection), then map in memory — same approach as GetStatusAsync.
        var entities = await _reportRequestRepository.GetQueryable()
            .AsNoTracking()
            .Where(r => r.RequestedByUserId == requestedByUserId)
            .OrderByDescending(r => r.CreatedDate)
            .Take(clampedTake)
            .ToListAsync(ct);

        return entities.Select(MapToStatusDto).ToList();
    }

    public async Task<ReportDownloadResult?> GetDownloadAsync(
        Guid reportRequestId,
        int requestedByUserId,
        CancellationToken ct = default)
    {
        var entity = await _reportRequestRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReportRequestId == reportRequestId, ct);

        if (entity is null
            || entity.RequestedByUserId != requestedByUserId
            || entity.Status != ReportRequestStatus.Completed
            || !entity.OutputDocumentGuid.HasValue)
        {
            return null;
        }

        var document = await _documentService.GetDocumentByGuidAsync(entity.OutputDocumentGuid.Value, ct);
        if (document is null)
        {
            _logger.LogWarning(
                "Report request {RequestId} is Completed but its document {DocumentGuid} was not found.",
                reportRequestId, entity.OutputDocumentGuid);
            return null;
        }

        var stream = await _fileStorageService.ReadFileAsync(document.StoragePath, ct);
        if (stream is null)
            return null;

        var fileName = string.IsNullOrWhiteSpace(document.OriginalFileName)
            ? $"{entity.ReportCode}.pdf"
            : document.OriginalFileName;

        return new ReportDownloadResult(stream, fileName, document.MimeType);
    }
}
