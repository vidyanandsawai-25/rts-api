using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Api.Controllers.Asset_Management;

[ApiController]
[Route("api/asset-management/document-definition")]
public class AssetDocumentDefinitionController : ControllerBase
{
    private readonly IAssetDocumentDefinitionService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly ILogger<AssetDocumentDefinitionController> _logger;
    private readonly IReferenceValidationService _referenceValidationService;

    public AssetDocumentDefinitionController(
        IAssetDocumentDefinitionService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<AssetDocumentDefinitionController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _logger = logger;
        _referenceValidationService = referenceValidationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssetDocumentDefinitionDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] AssetDocumentDefinitionQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AssetDocumentDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AssetDocumentDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssetDocumentDefinitionDto>), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateAssetDocumentDefinitionDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssetDocumentDefinitionDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> Update(int id, [FromBody] UpdateAssetDocumentDefinitionDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssetDocumentDefinitionDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken ct)
        => this.ExecuteForceDelete<AssetDocumentDefinitionEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);
}
