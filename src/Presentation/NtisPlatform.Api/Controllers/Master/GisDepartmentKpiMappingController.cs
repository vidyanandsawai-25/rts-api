using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// Controller for Department KPI Mapping CRUD operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GisDepartmentKpiMappingController : ControllerBase
{
    private readonly IGisDepartmentKpiMappingService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<GisDepartmentKpiMappingController> _logger;

    public GisDepartmentKpiMappingController(
        IGisDepartmentKpiMappingService service, 
        IHardDeleteCleanupService cleanupService, 
        IReferenceValidationService referenceValidationService, 
        ILogger<GisDepartmentKpiMappingController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] GisDepartmentKpiMappingQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateGisDepartmentKpiMappingDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateGisDepartmentKpiMappingDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<GisDepartmentKpiMappingEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);
}
