using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Reporting;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Implements the worker-facing handshake/data/upload operations. The worker owns queue status
/// writes (directly in the report DB); this service only validates the SLT, mints the LLT, serves
/// paginated data from the read-only replica via the report data providers, and stores the PDF.
/// </summary>
public class ReportWorkerService : IReportWorkerService
{
    // Preserve exact provider key casing (e.g. "main", "CollectionReport", "Transmast_GEN").
    private static readonly JsonSerializerOptions _exactJson = new();

    private readonly IReportingRepository<ReportRequestEntity, Guid> _reportRequestRepository;
    private readonly IReportingUnitOfWork _reportingUnitOfWork;
    private readonly ReportDefinitionCacheService _cache;
    private readonly IRepository<ReportDefinitionEntity, int> _reportDefinitionRepository;
    private readonly IEnumerable<IReportDataProvider> _dataProviders;
    private readonly ITokenService _tokenService;
    private readonly IDocumentApplicationService _documentApplicationService;
    private readonly ReportingOptions _options;
    private readonly ILogger<ReportWorkerService> _logger;

    public ReportWorkerService(
        IReportingRepository<ReportRequestEntity, Guid> reportRequestRepository,
        IReportingUnitOfWork reportingUnitOfWork,
        ReportDefinitionCacheService cache,
        IRepository<ReportDefinitionEntity, int> reportDefinitionRepository,
        IEnumerable<IReportDataProvider> dataProviders,
        ITokenService tokenService,
        IDocumentApplicationService documentApplicationService,
        IOptions<ReportingOptions> options,
        ILogger<ReportWorkerService> logger)
    {
        _reportRequestRepository = reportRequestRepository;
        _reportingUnitOfWork = reportingUnitOfWork;
        _cache = cache;
        _reportDefinitionRepository = reportDefinitionRepository;
        _dataProviders = dataProviders;
        _tokenService = tokenService;
        _documentApplicationService = documentApplicationService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<WorkerAuthenticateResultDto?> AuthenticateAsync(
        WorkerAuthenticateRequestDto request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.ShortLivedToken))
            return null;

        // Validate the SLT as a JWT (signature, expiry, scope=report-slt) and extract the
        // reportRequestId claim. This replaces the opaque-string DB lookup — the token is
        // self-describing so we look up the row by its ID, not by the token value.
        var sltClaims = _tokenService.ValidateShortLivedToken(request.ShortLivedToken);
        if (sltClaims is null)
        {
            _logger.LogWarning("Worker SLT JWT validation failed for body request {RequestId}.", request.ReportRequestId);
            return null;
        }
        var (claimedRequestId, claimedUserId) = sltClaims.Value;

        if (request.ReportRequestId != claimedRequestId)
        {
            _logger.LogWarning(
                "Worker authentication request id {RequestId} does not match SLT claim {ClaimedRequestId}.",
                request.ReportRequestId, claimedRequestId);
            return null;
        }

        var entity = await _reportRequestRepository.GetQueryable()
            .FirstOrDefaultAsync(r => r.ReportRequestId == claimedRequestId, ct);

        if (entity is null
            || entity.RequestedByUserId != claimedUserId
            || entity.SltConsumed
            || entity.SltExpiresAt is null
            || entity.SltExpiresAt.Value <= DateTime.Now
            // Defense-in-depth: ensure this platform instance only services its own tenant's rows.
            || (_options.OrganizationId > 0 && entity.OrganizationId != _options.OrganizationId))
        {
            _logger.LogWarning("Worker authentication rejected for request {RequestId}.", claimedRequestId);
            return null;
        }

        entity.SltConsumed = true;
        await _reportRequestRepository.UpdateAsync(entity, ct);
        try
        {
            // RowVersion guards against two workers consuming the same SLT concurrently.
            await _reportingUnitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            _reportingUnitOfWork.DiscardChanges();
            return null;
        }

        var definition = await ResolveDefinitionAsync(entity.ReportCode, ct);
        var provider = ResolveProvider(definition.DataProviderCode);
        var parameters = DeserializeParameters(entity.ParametersJson);

        var sections = await GetSectionsAsync(provider, parameters, ct);

        var llt = _tokenService.GenerateReportWorkerToken(
            entity.ReportRequestId, entity.RequestedByUserId, _options.LltMinutes);

        return new WorkerAuthenticateResultDto
        {
            LongLivedToken = llt,
            ReportName = definition.TemplateFile,
            DataProviderCode = definition.DataProviderCode,
            ParametersJson = entity.ParametersJson,
            Sections = sections,
            OutputFormat = "pdf",
        };
    }

    public async Task<ReportDataPage> GetDataPageAsync(
        Guid reportRequestId,
        string section,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (pageSize <= 0)
            pageSize = _options.DefaultPageSize;

        var entity = await _reportRequestRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReportRequestId == reportRequestId, ct)
            ?? throw new KeyNotFoundException($"Report request '{reportRequestId}' not found.");

        var definition = await ResolveDefinitionAsync(entity.ReportCode, ct);
        var provider = ResolveProvider(definition.DataProviderCode);
        var parameters = DeserializeParameters(entity.ParametersJson);

        if (provider is IPagedReportDataProvider paged)
            return await paged.GetDataPageAsync(parameters, section, page, pageSize, ct);

        // Fallback: non-paged provider — fetch the whole dataset, return the requested section once.
        var rows = await GetSectionRowsFallbackAsync(provider, parameters, section, ct);
        return new ReportDataPage
        {
            Section = section,
            Page = page < 1 ? 1 : page,
            PageSize = rows.Count,
            TotalCount = rows.Count,
            HasMore = false,
            Rows = rows,
        };
    }

    public async Task<ImageResult?> GetImageAsync(Guid imageGuid, CancellationToken ct = default)
    {
        // Reuse the same document-view path DocumentController uses. ViewDocumentAsync performs no
        // per-user ACL check (authorization is the controller's concern); here the worker LLT is the
        // trust boundary, and report images are platform-owned system documents.
        var (stream, _, mimeType) = await _documentApplicationService.ViewDocumentAsync(imageGuid, ct);
        if (stream is null)
            return null;

        var mime = string.IsNullOrWhiteSpace(mimeType) ? "image/jpeg" : mimeType;
        return new ImageResult(stream, mime);
    }

    public async Task<WorkerUploadResultDto> UploadAsync(
        Guid reportRequestId,
        Stream pdf,
        string fileName,
        long fileSizeBytes,
        CancellationToken ct = default)
    {
        // Tracked load — we record OutputDocumentGuid on the row as part of upload (below).
        var entity = await _reportRequestRepository.GetQueryable()
            .FirstOrDefaultAsync(r => r.ReportRequestId == reportRequestId, ct)
            ?? throw new KeyNotFoundException($"Report request '{reportRequestId}' not found.");

        // Idempotency: a reclaim/retry after a prior successful upload must NOT create a second
        // document + orphan file. If this request already has a stored PDF, return it unchanged.
        if (entity.OutputDocumentGuid.HasValue)
        {
            _logger.LogInformation(
                "Upload for request {RequestId} is a no-op; document {DocumentGuid} already stored.",
                reportRequestId, entity.OutputDocumentGuid);
            return new WorkerUploadResultDto { DocumentGuid = entity.OutputDocumentGuid.Value };
        }

        var safeName = string.IsNullOrWhiteSpace(fileName) ? $"{entity.ReportCode}.pdf" : fileName;

        // Delegate file-save + CORE.Document creation to the shared application service — the same
        // path DocumentController.Upload uses. It owns checksum, the transaction, and orphan-file
        // cleanup on failure, so the worker doesn't re-implement any of that.
        var uploaded = await _documentApplicationService.UploadDocumentAsync(
            pdf,
            safeName,
            "application/pdf",
            fileSizeBytes,
            new DocumentUploadDto
            {
                OwnerUserId = entity.RequestedByUserId,
                DocumentType = "Report",
            },
            uploadedBy: entity.RequestedByUserId,
            cancellationToken: ct);

        // Record the guid on the request so a subsequent retry short-circuits above. The document is
        // already committed by the call above; this only stamps the reporting row.
        entity.OutputDocumentGuid = uploaded.DocumentGuid;
        await _reportRequestRepository.UpdateAsync(entity, ct);
        await _reportingUnitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Stored report PDF for request {RequestId} as document {DocumentGuid}.",
            reportRequestId, uploaded.DocumentGuid);

        return new WorkerUploadResultDto { DocumentGuid = uploaded.DocumentGuid };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<ReportDefinitionEntity> ResolveDefinitionAsync(string reportCode, CancellationToken ct)
    {
        var definition = _cache.TryGetDefinition(reportCode);
        if (definition is not null)
            return definition;

        return await _reportDefinitionRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReportCode == reportCode && r.IsActive, ct)
            ?? throw new KeyNotFoundException($"Report '{reportCode}' not found or inactive.");
    }

    private IReportDataProvider ResolveProvider(string dataProviderCode) =>
        _dataProviders.FirstOrDefault(p =>
            p.ProviderCode.Equals(dataProviderCode, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException(
            $"No IReportDataProvider registered for ProviderCode '{dataProviderCode}'.");

    private static Dictionary<string, string> DeserializeParameters(string? parametersJson)
    {
        if (string.IsNullOrWhiteSpace(parametersJson))
            return new Dictionary<string, string>();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(parametersJson)
            ?? new Dictionary<string, string>();
    }

    /// <summary>Derives the section list for a non-paged provider by inspecting the dataset shape.</summary>
    private async Task<List<ReportSectionDescriptor>> GetSectionsAsync(
        IReportDataProvider provider, Dictionary<string, string> parameters, CancellationToken ct)
    {
        if (provider is IPagedReportDataProvider paged)
            return paged.GetSections().ToList();

        var data = await provider.GetDataAsync(parameters, ct);
        using var doc = JsonSerializer.SerializeToDocument(data, _exactJson);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
            return new List<ReportSectionDescriptor> { new("main", false) };

        if (root.ValueKind == JsonValueKind.Object)
            return root.EnumerateObject()
                       .Select(p => new ReportSectionDescriptor(p.Name, false))
                       .ToList();

        return new List<ReportSectionDescriptor> { new("main", false) };
    }

    private async Task<List<object>> GetSectionRowsFallbackAsync(
        IReportDataProvider provider, Dictionary<string, string> parameters, string section, CancellationToken ct)
    {
        var data = await provider.GetDataAsync(parameters, ct);
        using var doc = JsonSerializer.SerializeToDocument(data, _exactJson);
        var root = doc.RootElement;

        JsonElement array;
        if (root.ValueKind == JsonValueKind.Array)
        {
            array = root; // flat report; section is "main"
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            var match = root.EnumerateObject()
                .FirstOrDefault(p => p.Name.Equals(section, StringComparison.OrdinalIgnoreCase));
            if (match.Value.ValueKind != JsonValueKind.Array)
                return new List<object>();
            array = match.Value;
        }
        else
        {
            return new List<object>();
        }

        // Clone each element so it survives disposal of the JsonDocument; JsonElement serializes verbatim.
        return array.EnumerateArray().Select(e => (object)e.Clone()).ToList();
    }
}
