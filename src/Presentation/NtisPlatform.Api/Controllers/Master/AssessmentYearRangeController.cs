using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.AssessmentYearRange;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]

public class AssessmentYearRangeController : ControllerBase
{
    private readonly IAssessmentYearRangeService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly ILogger<AssessmentYearRangeController> _logger;
    private readonly IReferenceValidationService _referenceValidationService;
    public AssessmentYearRangeController(IAssessmentYearRangeService service, IHardDeleteCleanupService cleanupService, IReferenceValidationService referenceValidationService, ILogger<AssessmentYearRangeController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] AssessmentYearRangeQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateAssessmentYearRangeDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateAssessmentYearRangeDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<AssessmentYearRangeEntity, int>(_cleanupService, _referenceValidationService,id, _logger, ct);
}
