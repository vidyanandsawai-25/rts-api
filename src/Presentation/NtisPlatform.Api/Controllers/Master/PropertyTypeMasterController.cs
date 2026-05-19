using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.PropertyTypeMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
 
public class PropertyTypeMasterController : ControllerBase
{
    private readonly IPropertyTypeMasterService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly ILogger<PropertyTypeMasterController> _logger;

    public PropertyTypeMasterController(IPropertyTypeMasterService service, IHardDeleteCleanupService cleanupService, ILogger<PropertyTypeMasterController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertyTypeMasterQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreatePropertyTypeMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePropertyTypeMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
    => this.ExecuteForceDelete<PropertyTypeMasterEntity, int>(_cleanupService, id, _logger, ct);
}
