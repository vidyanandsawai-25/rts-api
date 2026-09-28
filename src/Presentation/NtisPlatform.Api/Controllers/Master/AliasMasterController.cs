using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// CRUD for the <c>CORE.AliasMaster</c> master — per-language display aliases for software field
/// names, shown on the Alias Master screen. Every write is immediate and live; there is no
/// draft/approval step.
/// </summary>
[ApiController]
[Route("api/alias-master")]
[Authorize]
public class AliasMasterController : ControllerBase
{
    private readonly IAliasMasterService _service;
    private readonly ILogger<AliasMasterController> _logger;

    public AliasMasterController(IAliasMasterService service, ILogger<AliasMasterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] AliasMasterQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// All active aliases, unpaged, for the frontend to use as a live override map on top of its
    /// static JSON translations. Public within the authenticated app — any screen can consume it.
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var result = await _service.GetActiveAliasesAsync(ct);
        return Ok(new ApiResponse<List<AliasLabelDto>> { Success = true, Items = result });
    }

    /// <summary>
    /// Aggregate counts: total fields, active fields, and inactive fields in Alias Master.
    /// </summary>
    [HttpGet("counts")]
    public async Task<IActionResult> GetCounts(CancellationToken ct)
    {
        var result = await _service.GetCountsAsync(ct);
        return Ok(new ApiResponse<AliasMasterCountDto> { Success = true, Items = result });
    }

    [HttpGet("{id:int}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateAliasMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id:int}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateAliasMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromQuery] bool isActive, CancellationToken ct)
    {
        var updated = await _service.SetActiveStatusAsync(id, isActive, ct);
        return Ok(new ApiResponse<object>
        {
            Success = updated,
            Message = updated
                ? (isActive ? "Alias activated successfully" : "Alias deactivated successfully")
                : "Record not found"
        });
    }
}
