using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.AapleSarkar;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

[AllowAnonymous]
[Route("api/[controller]")]
[ApiController]
public class AapleSarkarController : ControllerBase
{
    private readonly IAapleSarkarIntegrationService _service;
    private readonly IMahaITDashboardService _dashboardService;
    private readonly ILogger<AapleSarkarController> _logger;

    public AapleSarkarController(
        IAapleSarkarIntegrationService service,
        IMahaITDashboardService dashboardService,
        ILogger<AapleSarkarController> logger)
    {
        _service = service;
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// MahaIT Aaple Sarkar Portal Service Landing Callback.
    /// MahaIT redirects citizen to this URL upon clicking 'Apply':
    /// /api/AapleSarkar/callback?str={EncryptedData}&amp;ns={ServiceId}&amp;ULBID={UlbId}&amp;ULBDistrict={DistrictId}
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string str,
        [FromQuery] string ns,
        [FromQuery] int? ULBID,
        [FromQuery] int? ULBDistrict,
        CancellationToken ct)
    {
        _logger.LogInformation("Received Aaple Sarkar landing callback for ServiceId={Ns}, ULBID={UlbId}, ULBDistrict={District}",
            ns, ULBID, ULBDistrict);

        if (string.IsNullOrWhiteSpace(str))
        {
            _logger.LogWarning("Callback str parameter is missing.");
            return BadRequest(new { status = "FAILED", error = "Encrypted parameter 'str' is required." });
        }

        var (success, redirectUrl, error) = await _service.ProcessCallbackAsync(
            str,
            ns,
            ULBID,
            ULBDistrict,
            ct);

        if (!success || string.IsNullOrWhiteSpace(redirectUrl))
        {
            _logger.LogError("Failed to process Aaple Sarkar callback: {Error}", error);
            return StatusCode(500, new { status = "FAILED", error = error ?? "Internal error processing callback." });
        }

        // Set TD cookie for browser session persistence
        var uri = new Uri(redirectUrl);
        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var tdToken = queryParams["TD"];

        if (!string.IsNullOrWhiteSpace(tdToken))
        {
            Response.Cookies.Append("TD", tdToken, new CookieOptions
            {
                HttpOnly = false,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });
        }

        _logger.LogInformation("Redirecting citizen to RTS UI: {Url}", redirectUrl);
        return Redirect(redirectUrl);
    }

    /// <summary>
    /// Aaple Sarkar Citizen Dashboard Redirect.
    /// Aaple Sarkar portal redirects citizen to this URL to view their applications:
    /// GET /api/AapleSarkar/GotoApplicationDashboard?Appid={AapleSarkarTrackId}
    /// </summary>
    [HttpGet("GotoApplicationDashboard")]
    public async Task<IActionResult> GotoApplicationDashboard(
        [FromQuery] string Appid,
        CancellationToken ct)
    {
        _logger.LogInformation("Received GotoApplicationDashboard request for Appid={Appid}", Appid);

        if (string.IsNullOrWhiteSpace(Appid))
        {
            return BadRequest(new { status = false, message = "Parameter 'Appid' is required." });
        }

        var (success, redirectUrl, error, citizenUserId) = await _service.ProcessDashboardRedirectAsync(Appid, ct);

        if (!success || string.IsNullOrWhiteSpace(redirectUrl))
        {
            return StatusCode(500, new { status = false, message = error ?? "Failed to resolve citizen dashboard." });
        }

        // Append CUID cookie so UI can automatically load the citizen's applications
        if (!string.IsNullOrWhiteSpace(citizenUserId))
        {
            Response.Cookies.Append("CUID", citizenUserId, new CookieOptions
            {
                HttpOnly = false,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });
        }

        return Redirect(redirectUrl);
    }

    /// <summary>
    /// Retrieves all applications submitted by an Aaple Sarkar citizen.
    /// Called by the RTS Citizen Dashboard UI (/service/dashboard).
    /// </summary>
    [HttpPost("GetAapleSarkarApplications")]
    public async Task<IActionResult> GetAapleSarkarApplications(
        [FromBody] AapleSarkarCitizenApplicationsRequestDto request,
        CancellationToken ct)
    {
        if (request == null)
            request = new AapleSarkarCitizenApplicationsRequestDto();

        // Fallback to cookie if CitizenUserId was not passed in body
        if (string.IsNullOrWhiteSpace(request.CitizenUserId) && Request.Cookies.TryGetValue("CUID", out var cookieCuid))
        {
            request.CitizenUserId = cookieCuid;
        }

        if (string.IsNullOrWhiteSpace(request.CitizenUserId))
        {
            return BadRequest(new { status = false, message = "CitizenUserId or CUID cookie is required." });
        }

        var result = await _service.GetAapleSarkarApplicationsAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Pushes monthly department application statistics to MahaIT RTS Central Dashboard.
    /// Document Reference: DashboardAPI_clientDoc_New.pdf
    /// POST /api/AapleSarkar/push-department-dashboard?year=2026&amp;month=10
    /// </summary>
    [HttpPost("push-department-dashboard")]
    public async Task<IActionResult> PushDepartmentDashboard(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken ct)
    {
        int targetYear = year ?? DateTime.Now.Year;
        int targetMonth = month ?? DateTime.Now.Month;

        _logger.LogInformation("Pushing department dashboard summary to MahaIT for Year={Year}, Month={Month}",
            targetYear, targetMonth);

        var result = await _dashboardService.PushToMahaITDashboardAsync(targetYear, targetMonth, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets the generated monthly summary for the department RTS dashboard.
    /// GET /api/AapleSarkar/get-department-dashboard-summary?year=2026&amp;month=10
    /// </summary>
    [HttpGet("get-department-dashboard-summary")]
    public async Task<IActionResult> GetDepartmentDashboardSummary(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken ct)
    {
        int targetYear = year ?? DateTime.Now.Year;
        int targetMonth = month ?? DateTime.Now.Month;

        var reports = await _dashboardService.GetDashboardSummaryAsync(targetYear, targetMonth, ct);
        return Ok(new { status = true, year = targetYear, month = targetMonth, data = reports });
    }

    /// <summary>
    /// Gets recent push logs to MahaIT Central Dashboard.
    /// GET /api/AapleSarkar/get-department-push-logs?limit=50
    /// </summary>
    [HttpGet("get-department-push-logs")]
    public async Task<IActionResult> GetDepartmentPushLogs(
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var logs = await _dashboardService.GetPushLogsAsync(limit, ct);
        return Ok(new { status = true, data = logs });
    }
}
