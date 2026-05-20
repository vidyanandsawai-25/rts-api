using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
public class FloorController : ControllerBase
{
    private readonly IFloorService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly ILogger<FloorController> _logger;
    private readonly IReferenceValidationService _referenceValidationService;

    public FloorController(IFloorService service, IHardDeleteCleanupService cleanupService, IReferenceValidationService referenceValidationService,ILogger<FloorController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _logger = logger;
        _referenceValidationService = referenceValidationService;
    }

    // read
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] FloorQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    // create
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateFloorDto dto, CancellationToken ct)
        => this.ExecuteCreate(_service, dto, _logger, ct);

    [HttpPost("Range")]
    public Task<IActionResult> CreateFromRange([FromBody] RangeCreateRequest<CreateFloorDto> request, CancellationToken ct)
        => this.ExecuteCreateFromRange(_service, request, _logger, ct);

    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateFloorDto[] items, CancellationToken ct)
    => this.ExecuteBulkCreate(_service, items, _logger, ct);


    // update
    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateFloorDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateFloorDto>[] items, CancellationToken ct)
    => this.ExecuteBulkUpdate(_service, items, _logger, ct);

    // delete
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [HttpDelete("Bulk")]
    public Task<IActionResult> BulkDelete([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkDelete(_service, ids, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
    => this.ExecuteForceDelete<FloorEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);

    [Authorize]
    [HttpDelete("Bulk/purge")]
    public Task<IActionResult> BulkPurge([FromBody] int[] ids, CancellationToken ct)
    => this.ExecuteBulkForceDelete<FloorEntity, int>(_cleanupService, _referenceValidationService, ids, _logger, ct);
}
