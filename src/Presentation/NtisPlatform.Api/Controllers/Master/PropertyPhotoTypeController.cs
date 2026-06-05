using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.PropertyPhotoType;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
public class PropertyPhotoTypeController : ControllerBase
{
    private readonly IPropertyPhotoTypeService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly ILogger<PropertyPhotoTypeController> _logger;
    private readonly IReferenceValidationService _referenceValidationService;

    public PropertyPhotoTypeController(
        IPropertyPhotoTypeService service, 
        IHardDeleteCleanupService cleanupService, 
        IReferenceValidationService referenceValidationService,
        ILogger<PropertyPhotoTypeController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _logger = logger;
        _referenceValidationService = referenceValidationService;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertyPhotoTypeQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreatePropertyPhotoTypeDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePropertyPhotoTypeDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<PropertyPhotoTypeEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);
}
