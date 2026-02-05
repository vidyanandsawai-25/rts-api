using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers.Master;
[ApiController]
[Route("api/[controller]")]

public class MoujaController : ControllerBase
{
    private readonly IMoujaService _service;
    private readonly ILogger<MoujaController> _logger;
    
    public MoujaController(IMoujaService service, ILogger<MoujaController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] MoujaQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);
    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateMoujaDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);
    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateMoujaDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
}
