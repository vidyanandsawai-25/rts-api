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

/// <summary>CRUD endpoints for the Asset Grievance Remark master.</summary>
[ApiController]
[Route("api/asset-management/asset-grievance-remark")]
public class AssetGrievanceRemarkController : ControllerBase
{
    private readonly IAssetGrievanceRemarkService _service;
    private readonly IHardDeleteCleanupService _cleanupService;
    private readonly IReferenceValidationService _referenceValidationService;
    private readonly ILogger<AssetGrievanceRemarkController> _logger;

    public AssetGrievanceRemarkController(
        IAssetGrievanceRemarkService service,
        IHardDeleteCleanupService cleanupService,
        IReferenceValidationService referenceValidationService,
        ILogger<AssetGrievanceRemarkController> logger)
    {
        _service = service;
        _cleanupService = cleanupService;
        _referenceValidationService = referenceValidationService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssetGrievanceRemarkDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] AssetGrievanceRemarkQueryParameters queryParameters, CancellationToken cancellationToken)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, cancellationToken);

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AssetGrievanceRemarkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => this.ExecuteGetById(_service, id, _logger, cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AssetGrievanceRemarkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssetGrievanceRemarkDto>), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateAssetGrievanceRemarkDto createDto, CancellationToken cancellationToken)
        => this.ExecuteCreate(_service, createDto, _logger, cancellationToken);

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssetGrievanceRemarkDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> Update(int id, [FromBody] UpdateAssetGrievanceRemarkDto updateDto, CancellationToken cancellationToken)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, cancellationToken);

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssetGrievanceRemarkDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => this.ExecuteDelete(_service, id, _logger, cancellationToken);

    [Authorize]
    [HttpDelete("{id}/purge")]
    public Task<IActionResult> Purge(int id, CancellationToken cancellationToken)
        => this.ExecuteForceDelete<AssetGrievanceRemarkMasterEntity, int>(_cleanupService, _referenceValidationService, id, _logger, cancellationToken);
}
