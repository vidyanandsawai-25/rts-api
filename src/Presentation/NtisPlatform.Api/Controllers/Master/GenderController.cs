using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.GenderMaster;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
public class GenderController : ControllerBase
{
    private readonly IGenderMasterService _service;
    private readonly ILogger<GenderController> _logger;

    public GenderController(IGenderMasterService service, ILogger<GenderController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] GenderQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateGenderMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateGenderMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
}