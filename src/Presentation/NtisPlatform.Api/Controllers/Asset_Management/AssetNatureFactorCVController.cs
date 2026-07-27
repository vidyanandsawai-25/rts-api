using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Asset_Management.AssetNatureFactorCVMaster;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers.Asset_Management;

/// <summary>CRUD endpoints for [AMS].[NatureFactorCVMaster].</summary>
[ApiController]
[Route("api/asset-management/nature-factor-cv")]
public class AssetNatureFactorCVController : ControllerBase
{
    private readonly IAssetNatureFactorCVService _service;
    private readonly ILogger<AssetNatureFactorCVController> _logger;

    public AssetNatureFactorCVController(IAssetNatureFactorCVService service, ILogger<AssetNatureFactorCVController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssetNatureFactorCVMasterDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] AssetNatureFactorCVMasterQueryParameters qp, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, qp, _logger, ct);

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AssetNatureFactorCVMasterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AssetNatureFactorCVMasterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssetNatureFactorCVMasterDto>), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateAssetNatureFactorCVMasterDto dto, CancellationToken ct)
        => this.ExecuteCreate(_service, dto, _logger, ct);

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssetNatureFactorCVMasterDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> Update(int id, [FromBody] UpdateAssetNatureFactorCVMasterDto dto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, dto, _logger, ct);

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssetNatureFactorCVMasterDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    [HttpPost("Bulk")]
    public Task<IActionResult> BulkCreate([FromBody] CreateAssetNatureFactorCVMasterDto[] items, CancellationToken ct)
        => this.ExecuteBulkCreate(_service, items, _logger, ct);

    [HttpPut("Bulk")]
    public Task<IActionResult> BulkUpdate([FromBody] BulkUpdateItem<int, UpdateAssetNatureFactorCVMasterDto>[] items, CancellationToken ct)
        => this.ExecuteBulkUpdate(_service, items, _logger, ct);

    [HttpDelete("Bulk")]
    public Task<IActionResult> BulkDelete([FromBody] int[] ids, CancellationToken ct)
        => this.ExecuteBulkDelete(_service, ids, _logger, ct);
}
