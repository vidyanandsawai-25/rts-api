using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
 
public class TaxZoneController : ControllerBase
{
    private readonly ITaxZoneService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly ILogger<TaxZoneController> _logger;
    private readonly IReferenceValidationService _referenceValidationService;
    public TaxZoneController(ITaxZoneService service, IHardDeleteCleanupService cleanupService, IReferenceValidationService referenceValidationService, ILogger<TaxZoneController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _logger = logger;
        _referenceValidationService = referenceValidationService;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] TaxZoneQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateTaxZoneDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateTaxZoneDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
    => this.ExecuteForceDelete<TaxZoneEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);
}
