using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveCalculationEvidence;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Api.Controllers.RetrospectiveTax;

[ApiController]
[Route("api/[controller]")]
public class RetrospectiveCalculationEvidenceController : ControllerBase
{
    private readonly IRetrospectiveCalculationEvidenceService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<RetrospectiveCalculationEvidenceController> _logger;

    public RetrospectiveCalculationEvidenceController(
        IRetrospectiveCalculationEvidenceService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<RetrospectiveCalculationEvidenceController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RetrospectiveCalculationEvidenceQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(long id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRetrospectiveCalculationEvidenceDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateRetrospectiveCalculationEvidenceDto[] items, CancellationToken ct)
        => this.ExecuteBulkCreate(_service, items, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(long id, [FromBody] UpdateRetrospectiveCalculationEvidenceDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<long, UpdateRetrospectiveCalculationEvidenceDto>[] items, CancellationToken ct)
        => this.ExecuteBulkUpdate(_service, items, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(long id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [HttpDelete("Bulk")]
    public Task<IActionResult> BulkDelete([FromBody] long[] ids, CancellationToken ct)
        => this.ExecuteBulkDelete(_service, ids, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(long id, CancellationToken ct)
        => this.ExecuteForceDelete<RetrospectiveCalculationEvidenceEntity, long>(_cleanupService, _referenceValidationService, id, _logger, ct);

    [Authorize]
    [HttpDelete("Bulk/purge")]
    public Task<IActionResult> BulkPurge([FromBody] long[] ids, CancellationToken ct)
        => this.ExecuteBulkForceDelete<RetrospectiveCalculationEvidenceEntity, long>(_cleanupService, _referenceValidationService, ids, _logger, ct);
}
