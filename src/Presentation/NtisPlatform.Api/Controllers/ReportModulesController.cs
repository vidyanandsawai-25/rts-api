using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Read-only lookup for report modules (name + logo, base64-encoded). Modules themselves are
/// managed exclusively through the separate report-admin tool, so only GetAll/GetById are exposed
/// here — same generic GetAll/GetById plumbing as ReportController, just without the rest of the
/// CRUD surface.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportModulesController : ControllerBase
{
    private readonly IReportModuleService _service;
    private readonly ILogger<ReportModulesController> _logger;

    public ReportModulesController(IReportModuleService service, ILogger<ReportModulesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] ReportModuleQueryParameters qp, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, qp, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);
}
