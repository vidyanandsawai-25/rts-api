using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly IReportDefinitionService _service;
    private readonly IReportService _reportService;
    private readonly ReportDefinitionCacheService _cache;
    private readonly ReportDefinitionCacheWarmupService _cacheWarmup;
    private readonly ITokenService _tokenService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(
        IReportDefinitionService service,
        IReportService reportService,
        ReportDefinitionCacheService cache,
        ReportDefinitionCacheWarmupService cacheWarmup,
        ITokenService tokenService,
        ILogger<ReportController> logger)
    {
        _service = service;
        _reportService = reportService;
        _cache = cache;
        _cacheWarmup = cacheWarmup;
        _tokenService = tokenService;
        _logger = logger;
    }

    // ─── Report Definition CRUD ──────────────────────────────────────────────────

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] ReportDefinitionQueryParameters qp, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, qp, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    // ─── SignalR Hub Token ───────────────────────────────────────────────────────

    /// <summary>
    /// Issues a short-lived (5 min) hub-only JWT (scope="report-hub") for the SignalR WebSocket handshake.
    /// Same signing key/issuer/audience as every token; the scope claim is what the ReportHub policy checks.
    /// </summary>
    [HttpGet("hub-token")]
    public IActionResult HubToken()
    {
        var hubToken = _tokenService.GenerateHubToken(GetUserId(), expiresInMinutes: 5);
        return Ok(new { hubToken });
    }

    // ─── Cache Management ────────────────────────────────────────────────────────

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    [HttpPost("cache/invalidate")]
    public async Task<IActionResult> InvalidateCache(CancellationToken ct)
    {
        _cache.Invalidate();
        await _cacheWarmup.WarmUpAsync(ct);
        return Ok(new { message = "Report definition cache reloaded." });
    }

    // ─── Async Report Generation (submit → poll status → download) ────────────────

    /// <summary>
    /// Queues a report for async generation. Returns a request id the UI polls for status.
    /// </summary>
    [HttpPost("request")]
    public async Task<IActionResult> Request([FromBody] ReportRequestSubmitDto request, CancellationToken ct)
    {
        var result = await _reportService.SubmitAsync(request, GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>Returns the status of a previously submitted report request.</summary>
    [HttpGet("status/{requestId:guid}")]
    public async Task<IActionResult> Status(Guid requestId, CancellationToken ct)
    {
        var status = await _reportService.GetStatusAsync(requestId, GetUserId(), ct);
        return status is null ? NotFound() : Ok(status);
    }

    /// <summary>Lists the caller's most recent report requests (newest first) for the "My Reports" list.</summary>
    [HttpGet("requests")]
    public async Task<IActionResult> MyRequests([FromQuery] int take = 25, CancellationToken ct = default)
    {
        var items = await _reportService.GetMyRequestsAsync(GetUserId(), take, ct);
        return Ok(items);
    }

    /// <summary>Streams the finished PDF for a completed request (per-resource authorized).</summary>
    [HttpGet("download/{requestId:guid}")]
    public async Task<IActionResult> Download(Guid requestId, CancellationToken ct)
    {
        var download = await _reportService.GetDownloadAsync(requestId, GetUserId(), ct);
        if (download is null)
            return NotFound();

        return File(download.Content, download.ContentType, download.FileName);
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
        {
            throw new UnauthorizedAccessException("Valid user identification is required.");
        }
        return id;
    }
}
