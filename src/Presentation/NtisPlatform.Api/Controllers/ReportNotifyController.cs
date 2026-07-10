using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NtisPlatform.Api.Hubs;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Worker-facing status-notification endpoint. Called by the ntis-report worker after it writes a
/// terminal status (Completed/Failed) to the queue DB; broadcasts a SignalR push to the report
/// owner's browser so the UI updates immediately without waiting for the fallback poll interval.
/// Separated from <see cref="ReportWorkerController"/> because this is a SignalR/notification
/// concern, not part of the SLT->LLT data/upload handshake.
/// </summary>
[ApiController]
[Route("api/Report/worker")]
public class ReportNotifyController : ControllerBase
{
    private const string ReportWorkerPolicy = "ReportWorker";

    private readonly IReportService _reportService;
    private readonly IHubContext<ReportStatusHub> _hub;
    private readonly ILogger<ReportNotifyController> _logger;

    public ReportNotifyController(
        IReportService reportService,
        IHubContext<ReportStatusHub> hub,
        ILogger<ReportNotifyController> logger)
    {
        _reportService = reportService;
        _hub = hub;
        _logger = logger;
    }

    [Authorize(Policy = ReportWorkerPolicy)]
    [HttpPost("notify")]
    public async Task<IActionResult> Notify([FromBody] WorkerNotifyDto dto, CancellationToken ct)
    {
        // The worker LLT's reportRequestId claim must match the body (a token issued for one request
        // must not be able to push notifications for another).
        var claim = User.FindFirst("reportRequestId")?.Value;
        if (!Guid.TryParse(claim, out var tokenRequestId) || tokenRequestId != dto.ReportRequestId)
        {
            _logger.LogWarning(
                "Worker token reportRequestId claim '{Claim}' does not match body {RequestId}.",
                claim, dto.ReportRequestId);
            return Forbid();
        }

        var status = await _reportService.GetStatusInternalAsync(dto.ReportRequestId, ct);
        if (status is null)
            return NotFound();

        await _hub.Clients
            .Group($"user:{status.RequestedByUserId}")
            .SendAsync("ReportStatusChanged", dto.ReportRequestId, status.Status, ct);

        _logger.LogInformation(
            "Broadcast ReportStatusChanged {RequestId} → {Status} to user {UserId}.",
            dto.ReportRequestId, status.Status, status.RequestedByUserId);

        return Ok();
    }
}
