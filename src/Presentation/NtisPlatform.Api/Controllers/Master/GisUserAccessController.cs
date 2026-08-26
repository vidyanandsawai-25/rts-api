using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// Controller for Multi-Department GIS User Access Matrix CRUD operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GisUserAccessController : ControllerBase
{
    private readonly IGisDepartmentUserAccessService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<GisUserAccessController> _logger;

    public GisUserAccessController(
        IGisDepartmentUserAccessService service, 
        IHardDeleteCleanupService cleanupService, 
        IReferenceValidationService referenceValidationService, 
        ILogger<GisUserAccessController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] GisDepartmentUserAccessQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateGisDepartmentUserAccessDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateGisDepartmentUserAccessDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<GisDepartmentUserAccessEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);
}
