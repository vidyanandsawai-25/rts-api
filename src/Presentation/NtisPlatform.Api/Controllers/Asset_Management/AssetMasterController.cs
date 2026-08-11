using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.DTOs.Asset_Management.AssetFieldValue;

namespace NtisPlatform.Api.Controllers.Asset_Management;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AssetMasterController : ControllerBase
{
    private readonly ILogger<AssetMasterController> _logger;
    private readonly IAssetMasterService _service;
   
    #region Constructor

    public AssetMasterController(
        ILogger<AssetMasterController> logger,
        IAssetMasterService service)
    {
        _logger = logger;
        _service = service;        
    }

    #endregion

    #region Main CRUD Operations

    /// <summary>
    /// Gets all asset masters with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssetMasterDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] AssetMasterQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    /// <summary>
    /// Gets asset master by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AssetMasterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    /// <summary>
    /// Creates a new asset master with optional field values and photos.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<AssetMasterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssetMasterDto>), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromForm] CreateAssetMasterDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    /// <summary>
    /// Updates an existing asset master with optional field values and photos.
    /// </summary>
    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<AssetMasterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(int id, [FromForm] UpdateAssetMasterDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    /// <summary>
    /// Soft delete: Marks the record as deleted (IsActive=false).
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    #endregion

    #region Custom Endpoints

    /// <summary>
    /// Exports filtered asset master records to an Excel spreadsheet.
    /// </summary>
    [HttpGet("export-excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportExcel([FromQuery] AssetMasterQueryParameters queryParameters, CancellationToken ct)
    {
        var bytes = await _service.ExportToExcelAsync(queryParameters, ct);
        var fileName = $"AssetMaster_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Gets the combined payload for the floor and other details tab in a single call.
    /// </summary>
    [HttpGet("parent/{parentAssetId:int}/floor-and-other-details")]
    [ProducesResponseType(typeof(ApiResponse<AssetFloorAndOtherDetailsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFloorAndOtherDetails(int parentAssetId, CancellationToken ct)
    {
        if (parentAssetId <= 0)
        {
            return BadRequest(new ApiResponse<AssetFloorAndOtherDetailsResponseDto>
            {
                Success = false,
                Message = "Parent asset ID must be greater than zero."
            });
        }

        var asset = await _service.GetAssetFloorAndOtherDetailsAsync(parentAssetId, ct);
        if (asset == null)
        {
            return NotFound();
        }

        return Ok(new ApiResponse<AssetFloorAndOtherDetailsResponseDto>
        {
            Success = true,
            Items = asset,
            Message = "Asset floor and other details retrieved successfully."
        });
    }

    /// <summary>
    /// Gets all sub-assets grouped by parent asset ID with their related floor details,
    /// room-wise submissions, and renter details.
    /// </summary>
    /// <param name="parentAssetId">The parent asset ID to get sub-assets for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A grouped response containing parent asset info and all sub-assets with related details.</returns>
    [HttpGet("parent/{parentAssetId}/sub-assets")]
    [ProducesResponseType(typeof(SubAssetGroupedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSubAssetsGroupedByParent(int parentAssetId, CancellationToken ct)
    {
        if (parentAssetId <= 0)
        {
            return BadRequest(new ApiResponse<SubAssetGroupedResponseDto>
            {
                Success = false,
                Message = "Parent asset ID must be greater than zero."
            });
        }

        var result = await _service.GetSubAssetsGroupedByParentAsync(parentAssetId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Activates asset master record, all field values for the asset, and child assets where ParentAssetId equals the asset id.
    /// </summary>
    [HttpPut("{assetId:int}/activate")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateAsset(int assetId, CancellationToken ct)
    {
        var isUpdated = await _service.ActivateAssetAndFieldValuesAsync(assetId, ct);
        if (!isUpdated)
        {
            return NotFound();
        }

        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Message = "Asset activated successfully.",
            Items = true
        });
    }

    /// <summary>
    /// Bulk save dynamic field values for an asset.
    /// </summary>
    [HttpPost("{assetId:int}/field-values/bulk")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkSaveFieldValues(int assetId, [FromBody] List<CreateAssetFieldValueDto> fieldValues, CancellationToken ct)
    {
        var success = await _service.BulkSaveFieldValuesAsync(assetId, fieldValues, ct);
        if (!success)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<bool>
            {
                Success = false,
                Message = "Failed to bulk save field values."
            });
        }

        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Message = "Field values bulk saved successfully.",
            Items = true
        });
    }

    #endregion
}
