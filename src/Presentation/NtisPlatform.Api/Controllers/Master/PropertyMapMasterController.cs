using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.PropertyMapMaster;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
public class PropertyMapMasterController : ControllerBase
{
    private readonly IPropertyMapMasterService _service;
    private readonly ILogger<PropertyMapMasterController> _logger;

    public PropertyMapMasterController(IPropertyMapMasterService service, ILogger<PropertyMapMasterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertyMapQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreatePropertyMapMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePropertyMapMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
}