using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.RTSServiceMaster;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Api.Controllers.Master;

[Route("api/[controller]")]
[ApiController]
public class RTSServiceController : ControllerBase
{
    private readonly IRTSServiceService _service;
    private readonly ILogger<RTSServiceController> _logger;
    public RTSServiceController(IRTSServiceService service, ILogger<RTSServiceController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RTSServiceQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [AllowAnonymous]
    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

}
