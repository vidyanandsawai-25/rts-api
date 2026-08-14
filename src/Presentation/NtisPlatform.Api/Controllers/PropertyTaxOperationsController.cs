using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.PropertyTaxOperations;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Tax Operations console: initialise the screen, evaluate scope/eligibility,
/// execute "Add Tax" (synchronous for now), and read job status / audit. Bespoke (non-CRUD)
/// controller in the style of <c>RateableValueController</c>; the global exception middleware
/// maps thrown exceptions to status codes.
/// </summary>
[ApiController]
[Route("api/property-tax/operations")]
[Authorize]
public class PropertyTaxOperationsController : ControllerBase
{
    private readonly IPropertyTaxOperationsService _service;
    private readonly ILogger<PropertyTaxOperationsController> _logger;

    private static readonly HashSet<string> PermittedOperations =
        new(StringComparer.OrdinalIgnoreCase) { "AddTax" };

    public PropertyTaxOperationsController(
        IPropertyTaxOperationsService service,
        ILogger<PropertyTaxOperationsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // GET api/property-tax/operations/init?financeYearId=3002
    [HttpGet("init")]
    [ProducesResponseType(typeof(OperationsInitDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Init([FromQuery] int? financeYearId, CancellationToken ct)
        => Ok(await _service.GetInitAsync(GetUserId(), financeYearId, ct));

    // GET api/property-tax/operations/export-properties?status=eligible&financeYearId=3002
    [HttpGet("export-properties")]
    public async Task ExportProperties(
        [FromQuery] string status = "all",
        [FromQuery] int? financeYearId = null,
        CancellationToken ct = default)
    {
        var allowed = new[] { "all", "eligible", "skipped" };
        if (!allowed.Contains(status, StringComparer.OrdinalIgnoreCase))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Microsoft.AspNetCore.Http.HttpResponseWritingExtensions.WriteAsync(Response, $"Invalid status '{status}'. Allowed values: all, eligible, skipped.", ct);
            return;
        }

        var normalizedStatus = status.ToLowerInvariant();
        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"property_tax_properties_{normalizedStatus}.csv\"");
        await _service.WritePropertiesCsvToStreamAsync(Response.Body, normalizedStatus, financeYearId, ct);
    }

    // GET api/property-tax/operations/import-template
    [HttpGet("import-template")]
    [ProducesResponseType(typeof(ImportTemplateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportTemplate(CancellationToken ct)
        => Ok(await _service.GetImportTemplateAsync(ct));

    // POST api/property-tax/operations/eligible-count
    [HttpPost("eligible-count")]
    [ProducesResponseType(typeof(EligibleCountResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EligibleCount(
        [FromBody] EligibleCountRequestDto request, CancellationToken ct)
        => Ok(await _service.GetEligibleCountAsync(request, GetUserId(), ct));

    // POST api/property-tax/operations/preview
    [HttpPost("preview")]
    [ProducesResponseType(typeof(OperationPreviewResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Preview(
        [FromBody] OperationPreviewRequestDto request, CancellationToken ct)
        => Ok(await _service.GetPreviewAsync(request, GetUserId(), ct));

    // POST api/property-tax/operations/preview-export?downloadType=all|eligible|skipped
    [HttpPost("preview-export")]
    public async Task PreviewExport(
        [FromBody] OperationPreviewRequestDto request,
        [FromQuery] string downloadType = "all",
        CancellationToken ct = default)
    {
        var allowed = new[] { "all", "eligible", "skipped" };
        if (!allowed.Contains(downloadType, StringComparer.OrdinalIgnoreCase))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Microsoft.AspNetCore.Http.HttpResponseWritingExtensions.WriteAsync(Response, $"Invalid downloadType '{downloadType}'. Allowed values: all, eligible, skipped.", ct);
            return;
        }

        Response.ContentType = "text/csv; charset=utf-8";
        var fileName = $"preview_{downloadType.ToLowerInvariant()}_{DateTime.Now:yyyyMMddHHmmss}.csv";
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");

        await _service.WritePreviewExportCsvToStreamAsync(Response.Body, request, downloadType, ct);
    }

    // POST api/property-tax/operations/execute
    [HttpPost("execute")]
    [ProducesResponseType(typeof(ApiResponse<ExecuteOperationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Execute(
        [FromBody] ExecuteOperationRequestDto request, CancellationToken ct)
    {
        if (!PermittedOperations.Contains(request.Operation ?? string.Empty))
            return Forbid();

        var result = await _service.ExecuteAsync(request, BuildContext(), ct);
        return Ok(new ApiResponse<ExecuteOperationResponseDto>
        {
            Success = true,
            Message = "Operation completed",
            Items = result
        });
    }

    // GET api/property-tax/operations/jobs/{jobId}/status
    [HttpGet("jobs/{jobId:int}/status")]
    [ProducesResponseType(typeof(JobStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JobStatus(int jobId, CancellationToken ct)
        => Ok(await _service.GetJobStatusAsync(jobId, GetUserId(), ct));

    // GET api/property-tax/operations/jobs/{jobId}/properties
    [HttpGet("jobs/{jobId:int}/properties")]
    [ProducesResponseType(typeof(PagedResult<JobPropertyResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JobProperties(
        int jobId, [FromQuery] JobPropertiesQueryParameters query, CancellationToken ct)
        => Ok(await _service.GetJobPropertiesAsync(jobId, query, GetUserId(), ct));

    // GET api/property-tax/operations/audit
    [HttpGet("audit")]
    [ProducesResponseType(typeof(PagedResult<JobAuditDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Audit(
        [FromQuery] OperationAuditQueryParameters query, CancellationToken ct)
        => Ok(await _service.GetAuditListAsync(query, GetUserId(), ct));

    // GET api/property-tax/operations/audit/{jobId}
    [HttpGet("audit/{jobId:int}")]
    [ProducesResponseType(typeof(JobAuditDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AuditDetail(int jobId, [FromQuery] JobPropertiesQueryParameters query, CancellationToken ct)
        => Ok(await _service.GetAuditDetailAsync(jobId, query, GetUserId(), ct));

    private OperationContext BuildContext()
    {
        var userName = User.FindFirst("name")?.Value ?? User.Identity?.Name;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        return new OperationContext(GetUserId(), userName, role, ip, userAgent);
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
            throw new UnauthorizedAccessException("Valid user identification is required.");
        return id;
    }
}
