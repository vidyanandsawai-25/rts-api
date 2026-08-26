using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// Controller for Dynamic GIS Layer Catalog Master CRUD operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GisLayerMasterController : ControllerBase
{
    private readonly IGisLayerMasterService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<GisLayerMasterController> _logger;

    public GisLayerMasterController(
        IGisLayerMasterService service, 
        IHardDeleteCleanupService cleanupService, 
        IReferenceValidationService referenceValidationService, 
        ILogger<GisLayerMasterController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] GisLayerMasterQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateGisLayerMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateGisLayerMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<GisLayerMasterEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);
}
