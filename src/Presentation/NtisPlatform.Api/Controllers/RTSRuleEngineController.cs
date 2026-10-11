using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
namespace NtisPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RTSRuleEngineController : ControllerBase
{
    private readonly IRTSRuleMasterService _service;
    private readonly ILogger<RTSRuleEngineController> _logger;
    public RTSRuleEngineController(IRTSRuleMasterService service, ILogger<RTSRuleEngineController> logger)
    {
        _service = service;
        _logger = logger;
    }
    
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RTSRuleMasterDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] RTSRuleMasterQueryParameters queryParameters, CancellationToken ct)
       => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);
    
    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RTSRuleMasterDto), StatusCodes.Status200OK)]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [AllowAnonymous]
    [HttpPost("bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateRTSRuleMasterRequestDto[] items,CancellationToken ct)
     => this.ExecuteBulkCreate(_service, items, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRTSRuleMasterRequestDto dto, CancellationToken ct)
     => this.ExecuteUpdate(_service, id, dto, _logger, ct);

    [AllowAnonymous]
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
}
