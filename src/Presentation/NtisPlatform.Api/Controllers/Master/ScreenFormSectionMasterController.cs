using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;


namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
public class ScreenFormSectionMasterController : ControllerBase
{
    private readonly IScreenFormSectionMasterService _service;
    private readonly ILogger<ScreenFormSectionMasterController> _logger;

    public ScreenFormSectionMasterController(
        IScreenFormSectionMasterService service,
        ILogger<ScreenFormSectionMasterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] ScreenFormSectionMasterQueryParameters queryParams, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParams, _logger, ct);

    [HttpGet("{id:int}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateScreenFormSectionMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id:int}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateScreenFormSectionMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id:int}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
}