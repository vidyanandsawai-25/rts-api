using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.TaxCalculationGuideline;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
public class TaxCalculationGuidelineController : ControllerBase
{
    private readonly ITaxCalculationGuidelineService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<TaxCalculationGuidelineController> _logger;

    public TaxCalculationGuidelineController(
        ITaxCalculationGuidelineService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<TaxCalculationGuidelineController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] TaxCalculationGuidelineQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateTaxCalculationGuidelineDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateTaxCalculationGuidelineDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<TaxCalculationGuidelineEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);
}
