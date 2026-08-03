using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
public class InventoryItemModelController : ControllerBase
{
    private readonly ILogger<InventoryItemModelController> _logger;
    private readonly IInventoryItemModelService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;

    public InventoryItemModelController(
        ILogger<InventoryItemModelController> logger,
        IInventoryItemModelService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService)
    {
        _logger = logger;
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InventoryItemModelDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] InventoryItemModelQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(InventoryItemModelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemModelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemModelDto>), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateInventoryItemModelDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemModelDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> Update(int id, [FromBody] UpdateInventoryItemModelDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemModelDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<InventoryItemModelEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);
}
