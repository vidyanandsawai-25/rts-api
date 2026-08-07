using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Master.NatureFactorCVMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Api.Controllers.Master;

[ApiController]
[Route("api/[controller]")]
public class NatureFactorCVMasterController : ControllerBase
{
    private readonly INatureFactorCVMasterService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<NatureFactorCVMasterController> _logger;

    public NatureFactorCVMasterController(
        INatureFactorCVMasterService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<NatureFactorCVMasterController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] NatureFactorCVMasterQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateNatureFactorCVMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateNatureFactorCVMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateNatureFactorCVMasterDto[] items, CancellationToken ct)
         => this.ExecuteBulkCreate(_service, items, _logger, ct);

    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateNatureFactorCVMasterDto>[] items, CancellationToken ct)
        => this.ExecuteBulkUpdate(_service, items, _logger, ct);

    [HttpDelete("Bulk")]
    public Task<IActionResult> BulkDelete([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkDelete(_service, ids, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<NatureFactorCVMasterEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);

    /// <summary>
    /// Permanently deletes multiple records by IDs. This is an irreversible operation.
    /// </summary>
    [Authorize]
    [HttpDelete("Bulk/purge")]
    public Task<IActionResult> BulkPurge([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkForceDelete<NatureFactorCVMasterEntity, int>(_cleanupService, _referenceValidationService, ids, _logger, ct);
}
