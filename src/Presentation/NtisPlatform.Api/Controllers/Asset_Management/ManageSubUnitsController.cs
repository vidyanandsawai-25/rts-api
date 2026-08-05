using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Asset_Management.ManageSubUnits;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers.Asset_Management;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ManageSubUnitsController : ControllerBase
{
    private readonly ILogger<ManageSubUnitsController> _logger;
    private readonly IManageSubUnitsService _service;

    public ManageSubUnitsController(
        ILogger<ManageSubUnitsController> logger,
        IManageSubUnitsService service)
    {
        _logger = logger;
        _service = service;
    }

    /// <summary>
    /// Bulk generates child assets (rooms/shops) under a parent asset with optional room and lease/rent details.
    /// </summary>
    /// <param name="dto">DTO containing parent asset ID, generation parameters, and optional room/lease-rent details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response with generated asset IDs and any errors</returns>
    [HttpPost("bulk-generate")]
    [ProducesResponseType(typeof(ApiResponse<BulkGenerateChildAssetsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BulkGenerateChildAssetsResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkGenerateChildAssets([FromBody] BulkGenerateChildAssetsDto dto, CancellationToken ct)
    {
        _logger.LogInformation("Starting bulk generation of {Count} child assets for parent {ParentAssetId}",
            dto.Count, dto.ParentAssetId);

        var result = await _service.BulkGenerateChildAssetsAsync(dto, ct);

        if (result.Errors.Any())
        {
            _logger.LogWarning("Bulk generation completed with {ErrorCount} errors. Generated {Total} assets",
                result.Errors.Count, result.TotalGenerated);

            return Ok(new ApiResponse<BulkGenerateChildAssetsResponseDto>
            {
                Success = result.TotalGenerated > 0,
                Message = result.TotalGenerated > 0
                    ? $"Partially completed. Generated {result.TotalGenerated} assets with {result.Errors.Count} errors."
                    : "Bulk generation failed. See errors for details.",
                Items = result
            });
        }

        _logger.LogInformation("Successfully generated {Total} child assets", result.TotalGenerated);

        return Ok(new ApiResponse<BulkGenerateChildAssetsResponseDto>
        {
            Success = true,
            Message = $"Successfully generated {result.TotalGenerated} child assets",
            Items = result
        });
    }

    /// <summary>
    /// Bulk generates child assets (rooms/shops) across multiple floors in a single transaction.
    /// Creates one AssetMaster entry + one SubUnitsDetails row per unit per floor.
    /// Called by the "Generate N Units" button on the Floor Details step.
    /// </summary>
    [HttpPost("bulk-generate-across-floors")]
    [ProducesResponseType(typeof(ApiResponse<BulkGenerateAcrossFloorsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BulkGenerateAcrossFloorsResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkGenerateAcrossFloors([FromBody] BulkGenerateAcrossFloorsDto dto, CancellationToken ct)
    {
        _logger.LogInformation(
            "Starting bulk-generate-across-floors: {TotalUnits} units for parent {ParentAssetId} across {FloorCount} floors",
            dto.FloorIds?.Count * dto.UnitsPerFloor, dto.ParentAssetId, dto.FloorIds?.Count);

        var result = await _service.BulkGenerateAcrossFloorsAsync(dto, ct);

        if (result.Errors.Any())
        {
            _logger.LogWarning("bulk-generate-across-floors completed with {ErrorCount} errors. Generated {Total} assets",
                result.Errors.Count, result.TotalGenerated);

            return Ok(new ApiResponse<BulkGenerateAcrossFloorsResponseDto>
            {
                Success = result.TotalGenerated > 0,
                Message = result.TotalGenerated > 0
                    ? $"Partially completed. Generated {result.TotalGenerated} assets with {result.Errors.Count} errors."
                    : "Generation failed. See errors for details.",
                Items = result
            });
        }

        _logger.LogInformation("Successfully generated {Total} child assets across floors", result.TotalGenerated);

        return Ok(new ApiResponse<BulkGenerateAcrossFloorsResponseDto>
        {
            Success = true,
            Message = $"Successfully generated {result.TotalGenerated} child assets",
            Items = result
        });
    }

    /// <summary>
    /// Creates a single child asset (room/shop) under a parent asset with complete form details.
    ///
    /// This endpoint handles the complete flow:
    /// 1. Creates the child asset under the parent
    /// 2. Creates room-wise submission details if provided
    /// 3. Creates lease/rent details if rent information is provided
    ///
    /// All operations are wrapped in a transaction.
    /// </summary>
    /// <param name="dto">DTO containing all form data including basic info, rent info, floor config, and room details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response with created asset ID and related record IDs</returns>
    [HttpPost("create")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<CreateChildAssetResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CreateChildAssetResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateChildAsset([FromForm] CreateChildAssetDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.AssessmentYear))
        {
            dto.AssessmentYear = DateTime.UtcNow.Year.ToString();
        }

        _logger.LogInformation("Creating child asset for parent {ParentAssetId}", dto.ParentAssetId);

        var result = await _service.CreateChildAssetAsync(dto, ct);

        if (!result.Success)
        {
            _logger.LogWarning("Failed to create child asset: {Message}", result.Message);

            return BadRequest(new ApiResponse<CreateChildAssetResponseDto>
            {
                Success = false,
                Message = result.Message,
                Items = result
            });
        }

        _logger.LogInformation("Successfully created child asset {AssetNo} with ID {AssetId}",
            result.AssetNo, result.AssetId);

        return Ok(new ApiResponse<CreateChildAssetResponseDto>
        {
            Success = true,
            Message = result.Message,
            Items = result
        });
    }

    /// <summary>
    /// Get all sub-units by parent asset ID.
    /// Used for Sub Unit Details grid.
    /// </summary>
    [HttpGet("by-asset/{assetId:int}")]
    [ProducesResponseType(typeof(ApiResponse<List<SubUnitListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSubUnitsByParentId(
        int assetId,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.GetAllSubUnitsByParentIdAsync(assetId, ct);

            return Ok(new ApiResponse<List<SubUnitListDto>>
            {
                Success = true,
                Message = $"Retrieved {result.Count} sub-units successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving sub-units for ParentAssetId: {ParentAssetId}",
                assetId);

            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<List<SubUnitListDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving sub-units",
                    Items = new List<SubUnitListDto>()
                });
        }
    }

    /// <summary>
    /// Get complete sub-unit details by sub-unit asset ID.
    /// Used when clicking the eye button.
    /// </summary>
    [HttpGet("{assetId:int}")]
    [ProducesResponseType(typeof(ApiResponse<SubAssetDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubUnitDetailsById(
        int assetId,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.GetSubUnitDetailsByIdAsync(assetId, ct);

            return Ok(new ApiResponse<SubAssetDetailDto>
            {
                Success = true,
                Message = "Sub-unit details retrieved successfully",
                Items = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Items = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving sub-unit details for AssetId: {AssetId}",
                assetId);

            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving sub-unit details",
                    Items = null
                });
        }
    }

    /// <summary>
    /// Get complete subunit details (including floors, rooms, and minus shapes) by parent building asset ID.
    /// </summary>
    [HttpGet("parent/{parentAssetId:int}/complete-details")]
    [ProducesResponseType(typeof(ApiResponse<List<SubUnitCompleteDetailDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubUnitsCompleteDetailsByParentId(
        int parentAssetId,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.GetSubUnitsCompleteDetailsByParentIdAsync(parentAssetId, ct);

            return Ok(new ApiResponse<List<SubUnitCompleteDetailDto>>
            {
                Success = true,
                Message = "Complete sub-unit details retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving complete sub-unit details for ParentAssetId: {ParentAssetId}",
                parentAssetId);

            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<List<SubUnitCompleteDetailDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving complete sub-unit details",
                    Items = new List<SubUnitCompleteDetailDto>()
            });
        }
    }

    /// <summary>
    /// Get the combined floor-details + lease/rent payload by asset id.
    /// Used when the caller already knows the sub-unit asset id.
    /// </summary>
    [HttpGet("by-asset/{assetId:int}/lease-rent-details")]
    [ProducesResponseType(typeof(ApiResponse<SubUnitLeaseRentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubUnitLeaseRentBySubUnitDetailsId(
        int assetId,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.GetSubUnitLeaseRentBySubUnitDetailsIdAsync(assetId, ct);

            return Ok(new ApiResponse<SubUnitLeaseRentDetailDto>
            {
                Success = true,
                Message = "Sub-unit lease/rent details retrieved successfully",
                Items = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<SubUnitLeaseRentDetailDto>
            {
                Success = false,
                Message = ex.Message,
                Items = null
            });
        }
    }
}
