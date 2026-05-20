using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
 
public class RetentionYearWiseController : ControllerBase
{
    private readonly IRetentionYearWiseService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly ILogger<RetentionYearWiseController> _logger;
    private readonly IReferenceValidationService _referenceValidationService;
    public RetentionYearWiseController(IRetentionYearWiseService service, IHardDeleteCleanupService cleanupService, IReferenceValidationService referenceValidationService, ILogger<RetentionYearWiseController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _logger = logger;
        _referenceValidationService = referenceValidationService;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RetentionYearWiseQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRetentionYearWiseDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRetentionYearWiseDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
    => this.ExecuteForceDelete<RetentionYearWiseEntity, int>(_cleanupService, _referenceValidationService,id, _logger, ct);
}
