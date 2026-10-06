using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.RTSApprovalFlowMaster;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// Controller for ApprovalFlowStageMaster CRUD operations
/// </summary>
[ApiController]
[Route("api/RTSApprovalFlowStageMaster")]
public class RTSApprovalFlowStageMasterController : ControllerBase
{
    private readonly IRTSApprovalFlowStageMasterService _service;
    private readonly ILogger<RTSApprovalFlowStageMasterController> _logger;

    public RTSApprovalFlowStageMasterController(IRTSApprovalFlowStageMasterService service, ILogger<RTSApprovalFlowStageMasterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all ApprovalFlowStage Masters with filtering, sorting, and pagination
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RTSApprovalFlowStageMasterQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Get ApprovalFlowStage Master by ID
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    /// <summary>
    /// Create new ApprovalFlowStage Master
    /// </summary>
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRTSApprovalFlowStageMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    /// <summary>
    /// Update existing ApprovalFlowStage Master
    /// </summary>
    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRTSApprovalFlowStageMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    /// <summary>
    /// Delete ApprovalFlowStage Master
    /// </summary>
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
}
