using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportParameterDefinitionController : ControllerBase
{
    private readonly IReportParameterDefinitionService _service;
    private readonly ILogger<ReportParameterDefinitionController> _logger;

    public ReportParameterDefinitionController(
        IReportParameterDefinitionService service,
        ILogger<ReportParameterDefinitionController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] ReportParameterDefinitionQueryParameters qp, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, qp, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);
}
