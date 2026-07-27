using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Api.Controllers.Asset_Management;

/// <summary>CRUD endpoints for the Asset Mouja master.</summary>
[ApiController]
[Route("api/asset-management/mouja")]
public class AssetMoujaController : ControllerBase
{
    private readonly IAssetMoujaService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<AssetMoujaController> _logger;

    public AssetMoujaController(
        IAssetMoujaService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<AssetMoujaController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssetMoujaDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] AssetMoujaQueryParameters qp, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, qp, _logger, ct);

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AssetMoujaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AssetMoujaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssetMoujaDto>), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateAssetMoujaDto dto, CancellationToken ct)
        => this.ExecuteCreate(_service, dto, _logger, ct);

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssetMoujaDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> Update(int id, [FromBody] UpdateAssetMoujaDto dto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, dto, _logger, ct);

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssetMoujaDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<AssetMoujaMasterEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);
}
