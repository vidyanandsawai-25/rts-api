using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.ApprovalFlowMaster;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// Controller for ApprovalFlowMaster CRUD operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ApprovalFlowMasterController : ControllerBase
{
    private readonly IApprovalFlowMasterService _service;
    private readonly ILogger<ApprovalFlowMasterController> _logger;

    public ApprovalFlowMasterController(IApprovalFlowMasterService service, ILogger<ApprovalFlowMasterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all ApprovalFlow Masters with filtering, sorting, and pagination
    /// </summary>
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] ApprovalFlowMasterQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Get ApprovalFlow Master by ID
    /// </summary>
    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    /// <summary>
    /// Create new ApprovalFlow Master
    /// </summary>
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateApprovalFlowMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    /// <summary>
    /// Update existing ApprovalFlow Master
    /// </summary>
    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateApprovalFlowMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    /// <summary>
    /// Get configured workflow stages by RTS Service ID
    /// </summary>
    [AllowAnonymous]
    [HttpGet("stages/{serviceId}")]
    public async Task<IActionResult> GetStagesByServiceId(int serviceId, CancellationToken ct)
    {
        var result = await _service.GetWorkflowStagesByServiceIdAsync(serviceId, ct);
        if (result == null)
        {
            return NotFound(new { status = false, message = "No active workflow configuration found for this service." });
        }
        return Ok(new { status = true, data = result });
    }

    /// <summary>
    /// Delete ApprovalFlow Master
    /// </summary>
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
}
