using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.RTSApprovalFlowMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Exceptions;

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
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id, ct);
            return Ok(new ApiResponse<RTSApprovalFlowStageMasterDto>
            {
                Success = deleted,
                Message = deleted ? "Stage and its allocations deleted" : "Record not found"
            });
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            _logger.LogError(ex, "Delete operation failed for stage id: {Id}", id);
            return StatusCode(500, new ApiResponse<RTSApprovalFlowStageMasterDto>
            {
                Success = false,
                Message = "An error occurred while deleting the record"
            });
        }
    }
}
