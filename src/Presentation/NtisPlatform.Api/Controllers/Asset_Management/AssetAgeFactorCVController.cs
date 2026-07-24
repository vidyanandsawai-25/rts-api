using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Asset_Management.AssetAgeFactorCVMaster;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers.Asset_Management;

/// <summary>CRUD endpoints for [AMS].[AgeFactorCVMaster].</summary>
[ApiController]
[Route("api/asset-management/age-factor-cv")]
public class AssetAgeFactorCVController : ControllerBase
{
    private readonly IAssetAgeFactorCVService _service;
    private readonly ILogger<AssetAgeFactorCVController> _logger;

    public AssetAgeFactorCVController(IAssetAgeFactorCVService service, ILogger<AssetAgeFactorCVController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssetAgeFactorCVMasterDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] AssetAgeFactorCVMasterQueryParameters qp, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, qp, _logger, ct);

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AssetAgeFactorCVMasterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AssetAgeFactorCVMasterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssetAgeFactorCVMasterDto>), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateAssetAgeFactorCVMasterDto dto, CancellationToken ct)
        => this.ExecuteCreate(_service, dto, _logger, ct);

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssetAgeFactorCVMasterDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> Update(int id, [FromBody] UpdateAssetAgeFactorCVMasterDto dto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, dto, _logger, ct);

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssetAgeFactorCVMasterDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateAssetAgeFactorCVMasterDto[] items, CancellationToken ct)
        => this.ExecuteBulkCreate(_service, items, _logger, ct);

    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateAssetAgeFactorCVMasterDto>[] items, CancellationToken ct)
        => this.ExecuteBulkUpdate(_service, items, _logger, ct);

    [HttpDelete("Bulk")]
    public Task<IActionResult> BulkDelete([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkDelete(_service, ids, _logger, ct);
}
