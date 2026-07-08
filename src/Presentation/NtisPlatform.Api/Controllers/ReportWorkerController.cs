using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Service-to-service endpoints called by the ntis-report worker during async report generation.
/// Authenticate is anonymous (the SLT is the credential); data/upload/image require a worker LLT whose
/// reportRequestId claim must match the body (prevents a token issued for one request acting on another).
/// Terminal-status notifications live in <see cref="ReportNotifyController"/>.
/// </summary>
[ApiController]
[Route("api/Report/worker")]
public class ReportWorkerController : ControllerBase
{
    private const string ReportWorkerPolicy = "ReportWorker";

    private readonly IReportWorkerService _workerService;
    private readonly ILogger<ReportWorkerController> _logger;

    public ReportWorkerController(
        IReportWorkerService workerService,
        ILogger<ReportWorkerController> logger)
    {
        _workerService = workerService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] WorkerAuthenticateRequestDto request, CancellationToken ct)
    {
        var result = await _workerService.AuthenticateAsync(request, ct);
        return result is null ? Unauthorized() : Ok(result);
    }

    [Authorize(Policy = ReportWorkerPolicy)]
    [HttpPost("data")]
    public async Task<IActionResult> Data([FromBody] WorkerDataRequestDto request, CancellationToken ct)
    {
        if (!TokenMatchesRequest(request.ReportRequestId))
            return Forbid();

        var page = await _workerService.GetDataPageAsync(
            request.ReportRequestId, request.Section, request.Page, request.PageSize, ct);
        return Ok(page);
    }

    [Authorize(Policy = ReportWorkerPolicy)]
    [HttpPost("upload")]
    [RequestSizeLimit(Constants.FileUploadConstants.MaxFileSizeBytes)]
    public async Task<IActionResult> Upload(
        [FromForm] Guid reportRequestId,
        IFormFile pdf,
        CancellationToken ct)
    {
        if (!TokenMatchesRequest(reportRequestId))
            return Forbid();

        if (pdf is null || pdf.Length == 0)
            return BadRequest("A non-empty PDF file is required.");

        await using (var probe = pdf.OpenReadStream())
        {
            var header = new byte[5];
            var read = await probe.ReadAsync(header.AsMemory(0, 5), ct);
            // "%PDF-"
            if (read < 5 || header[0] != 0x25 || header[1] != 0x50 || header[2] != 0x44 || header[3] != 0x46 || header[4] != 0x2D)
                return BadRequest("Uploaded file is not a valid PDF.");
        }

        await using var stream = pdf.OpenReadStream();
        var result = await _workerService.UploadAsync(reportRequestId, stream, pdf.FileName, pdf.Length, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns the raw image bytes for the given DocumentGuid so the worker can embed
    /// secure platform images in Crystal Reports templates.
    /// The LLT already proves the caller is an authenticated worker; image guids come
    /// from the platform's own data provider, not user input.
    /// </summary>
    [Authorize(Policy = ReportWorkerPolicy)]
    [HttpGet("image/{imageGuid:guid}")]
    public async Task<IActionResult> GetImage(Guid imageGuid, CancellationToken ct)
    {
        var result = await _workerService.GetImageAsync(imageGuid, ct);
        if (result is null)
            return NotFound();

        return File(result.Content, result.MimeType);
    }

    private bool TokenMatchesRequest(Guid reportRequestId)
    {
        var claim = User.FindFirst("reportRequestId")?.Value;
        if (!Guid.TryParse(claim, out var tokenRequestId) || tokenRequestId != reportRequestId)
        {
            _logger.LogWarning(
                "Worker token reportRequestId claim '{Claim}' does not match body {RequestId}.",
                claim, reportRequestId);
            return false;
        }
        return true;
    }
}
