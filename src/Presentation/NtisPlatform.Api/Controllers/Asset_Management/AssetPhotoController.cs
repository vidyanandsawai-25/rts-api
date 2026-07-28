using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers.Asset_Management;

/// <summary>
/// Controller for AMS.AssetPhoto operations.
/// </summary>
[ApiController]
[Route("api/asset-photos")]
[Authorize]
public class AssetPhotoController : ControllerBase
{
    private readonly IAssetPhotoApplicationService _service;
    private readonly ILogger<AssetPhotoController> _logger;

    public AssetPhotoController(
        IAssetPhotoApplicationService service,
        ILogger<AssetPhotoController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("asset/{assetId}")]
    [ProducesResponseType(typeof(ApiResponse<List<AssetPhotoDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPhotosByAsset(int assetId, CancellationToken cancellationToken)
    {
        try
        {
            if (assetId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid AssetId" });

            var result = await _service.GetPhotosByAssetAsync(assetId, cancellationToken);

            return Ok(new ApiResponse<List<AssetPhotoDto>>
            {
                Success = true,
                Message = "Asset photos retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting photos for AssetId={AssetId}. CorrelationId: {CorrelationId}",
                assetId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving asset photos",
                CorrelationId = correlationId
            });
        }
    }

    [HttpGet("asset/{assetId}/grouped")]
    [ProducesResponseType(typeof(ApiResponse<AssetPhotoGalleryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetGroupedPhotosByAsset(int assetId, CancellationToken cancellationToken)
    {
        try
        {
            if (assetId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid AssetId" });

            var result = await _service.GetGroupedPhotosByAssetAsync(assetId, cancellationToken);

            return Ok(new ApiResponse<AssetPhotoGalleryDto>
            {
                Success = true,
                Message = "Asset photos retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting grouped photos for AssetId={AssetId}. CorrelationId: {CorrelationId}",
                assetId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving asset photos",
                CorrelationId = correlationId
            });
        }
    }

    [HttpGet("types-with-status/{assetId}")]
    [ProducesResponseType(typeof(ApiResponse<List<AssetPhotoTypeWithStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPhotoTypesWithStatus(int assetId, CancellationToken cancellationToken)
    {
        try
        {
            if (assetId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid AssetId" });

            var result = await _service.GetPhotoTypesWithStatusAsync(assetId, cancellationToken);

            return Ok(new ApiResponse<List<AssetPhotoTypeWithStatusDto>>
            {
                Success = true,
                Message = "Photo types retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting photo types with status for AssetId={AssetId}. CorrelationId: {CorrelationId}",
                assetId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving photo types",
                CorrelationId = correlationId
            });
        }
    }

    // ── Sub-Unit scoped endpoints ──────────────────────────────────────────────

    // ── Bulk Save Endpoint ───────────────────────────────────────────────────

    [HttpPost("bulk-save")]
    [ProducesResponseType(typeof(ApiResponse<AssetPhotoBulkSaveResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkSaveAll(
        [FromBody] AssetPhotoBulkSaveDto bulkDto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid request data" });

            var result = await _service.BulkSaveAllAsync(bulkDto, GetUserId(), cancellationToken);
            var hasErrors = result.Errors.Any();

            return Ok(new ApiResponse<AssetPhotoBulkSaveResponseDto>
            {
                Success = !hasErrors,
                Message = hasErrors 
                    ? $"Saved with {result.Errors.Count} error(s). Enabled: {result.EnabledCount}, Disabled: {result.DisabledCount}"
                    : $"All asset photos saved successfully. Enabled: {result.EnabledCount}, Disabled: {result.DisabledCount}",
                Items = result,
                Errors = hasErrors ? result.Errors : null
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Unauthorized access attempt. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Valid user identification is required.",
                CorrelationId = correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error in asset photo bulk save. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while saving asset photos",
                CorrelationId = correlationId
            });
        }
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
        {
            throw new UnauthorizedAccessException("Valid user identification is required.");
        }
        return id;
    }
}
