using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Api.Controllers.Master;

/// <summary>
/// Rule Master registry — admin/developer CRUD for the rules surfaced to end users
/// (by Display Name) on the Dynamic Tax Register.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DynamicTaxRuleController : ControllerBase
{
    private readonly IDynamicTaxRuleService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<DynamicTaxRuleController> _logger;

    public DynamicTaxRuleController(
        IDynamicTaxRuleService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<DynamicTaxRuleController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] DynamicTaxRuleQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateDynamicTaxRuleDto dto, CancellationToken ct)
        => this.ExecuteCreate(_service, dto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateDynamicTaxRuleDto dto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, dto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    /// <summary>
    /// Permanently removes a rule from PTIS.DynamicTaxRuleMaster — a real hard delete, not the
    /// soft-delete <see cref="Delete"/> above. Blocked when referenced by TaxMaster, TaxMasterMapping,
    /// or TaxConditionRule via RuleDefinitionId (restrictive FKs and ReferenceValidationService),
    /// turning reference violations into a 409 Conflict.
    /// </summary>
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<DynamicTaxRuleEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);
}
