using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.RTSDepartmentMaster;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Api.Controllers.Master;

[AllowAnonymous]
[Route("api/[controller]")]
[ApiController]
public class RTSDepartmentController : ControllerBase
{
    private readonly IRTSDepartmentService _service;
    private readonly ILogger<RTSDepartmentController> _logger;
    public RTSDepartmentController(IRTSDepartmentService service, ILogger<RTSDepartmentController> logger)
    {
        _service = service;
        _logger = logger;
    }
    /// <summary>
    /// Get all ApprovalFlow Masters with filtering, sorting, and pagination
    /// </summary>
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RTSDepartmentQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

}
