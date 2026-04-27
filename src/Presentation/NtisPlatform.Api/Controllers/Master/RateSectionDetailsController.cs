using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
 
public class RateSectionDetailsController : ControllerBase
{
    private readonly IRateSectionDetailsService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly ILogger<RateSectionDetailsController> _logger;

    public RateSectionDetailsController(
        IRateSectionDetailsService service, 
        IHardDeleteCleanupService cleanupService,
        ILogger<RateSectionDetailsController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RateSectionDetailsQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRateSectionDetailsDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRateSectionDetailsDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<RateSectionDetailsEntity, int>(_cleanupService, id, _logger, ct);

    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateRateSectionDetailsDto[] items, CancellationToken ct)
        => this.ExecuteBulkCreate(_service, items, _logger, ct);

    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateRateSectionDetailsDto>[] items, CancellationToken ct)
        => this.ExecuteBulkUpdate(_service, items, _logger, ct);

    [HttpDelete("Bulk")]
    public Task<IActionResult> BulkDelete([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkDelete(_service, ids, _logger, ct);

}

