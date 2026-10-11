using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.RTSApprovalFlowStageUserAllocationMaster;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// CRUD API for candidate user allocations assigned to RTS approval-flow stages.
/// </summary>
[ApiController]
[Route("api/RTSApprovalFlowStageUserAllocationMaster")]
public class RTSApprovalFlowStageUserAllocationMasterController : ControllerBase
{
    private readonly IRTSApprovalFlowStageUserAllocationMasterService _service;
    private readonly ILogger<RTSApprovalFlowStageUserAllocationMasterController> _logger;

    public RTSApprovalFlowStageUserAllocationMasterController(
        IRTSApprovalFlowStageUserAllocationMasterService service,
        ILogger<RTSApprovalFlowStageUserAllocationMasterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public Task<IActionResult> GetAll(
        [FromQuery] RTSApprovalFlowStageUserAllocationMasterQueryParameters queryParameters,
        CancellationToken ct) => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [AllowAnonymous]
    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct) =>
        this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create(
        [FromBody] CreateRTSApprovalFlowStageUserAllocationMasterDto createDto,
        CancellationToken ct) => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(
        int id,
        [FromBody] UpdateRTSApprovalFlowStageUserAllocationMasterDto updateDto,
        CancellationToken ct) => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    /// <summary>Remove a candidate officer allocation row from the stage.</summary>
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct) =>
        this.ExecuteDelete(_service, id, _logger, ct);
}
