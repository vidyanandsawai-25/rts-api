using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>CRUD endpoints for the penalty rule master.</summary>
[ApiController]
[Route("api/[controller]")]
public class PenaltyRuleController : ControllerBase
{
    private readonly IPenaltyRuleService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<PenaltyRuleController> _logger;

    public PenaltyRuleController(
        IPenaltyRuleService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<PenaltyRuleController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PenaltyRuleDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] PenaltyRuleQueryParameters qp, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, qp, _logger, ct);

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PenaltyRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PenaltyRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PenaltyRuleDto>), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreatePenaltyRuleDto dto, CancellationToken ct)
        => this.ExecuteCreate(_service, dto, _logger, ct);

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PenaltyRuleDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePenaltyRuleDto dto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, dto, _logger, ct);

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PenaltyRuleDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<PenaltyRuleMasterEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);
}
