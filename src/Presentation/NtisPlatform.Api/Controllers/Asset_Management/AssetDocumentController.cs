using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDocument;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers.Asset_Management;

/// <summary>
/// Controller for AMS.AssetDocument operations.
/// </summary>
[ApiController]
[Route("api/asset-documents")]
[Authorize]
public class AssetDocumentController : ControllerBase
{
    private readonly IAssetDocumentApplicationService _service;
    private readonly ILogger<AssetDocumentController> _logger;

    public AssetDocumentController(
        IAssetDocumentApplicationService service,
        ILogger<AssetDocumentController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("asset/{assetId}")]
    [ProducesResponseType(typeof(ApiResponse<List<AssetDocumentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDocumentsByAsset(int assetId, CancellationToken cancellationToken)
    {
        try
        {
            if (assetId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid AssetId" });

            var result = await _service.GetDocumentsByAssetAsync(assetId, cancellationToken);

            return Ok(new ApiResponse<List<AssetDocumentDto>>
            {
                Success = true,
                Message = "Asset documents retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting documents for AssetId={AssetId}. CorrelationId: {CorrelationId}",
                assetId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving asset documents",
                CorrelationId = correlationId
            });
        }
    }

    [HttpGet("asset/{assetId}/grouped")]
    [ProducesResponseType(typeof(ApiResponse<AssetDocumentGalleryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetGroupedDocumentsByAsset(int assetId, CancellationToken cancellationToken)
    {
        try
        {
            if (assetId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid AssetId" });

            var result = await _service.GetGroupedDocumentsByAssetAsync(assetId, cancellationToken);

            return Ok(new ApiResponse<AssetDocumentGalleryDto>
            {
                Success = true,
                Message = "Asset documents retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting grouped documents for AssetId={AssetId}. CorrelationId: {CorrelationId}",
                assetId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving asset documents",
                CorrelationId = correlationId
            });
        }
    }

    [HttpGet("types-with-status/{assetId}")]
    [ProducesResponseType(typeof(ApiResponse<List<AssetDocumentTypeWithStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDocumentTypesWithStatus(int assetId, CancellationToken cancellationToken)
    {
        try
        {
            if (assetId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid AssetId" });

            var result = await _service.GetDocumentTypesWithStatusAsync(assetId, cancellationToken);

            return Ok(new ApiResponse<List<AssetDocumentTypeWithStatusDto>>
            {
                Success = true,
                Message = "Document types retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting document types with status for AssetId={AssetId}. CorrelationId: {CorrelationId}",
                assetId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving document types",
                CorrelationId = correlationId
            });
        }
    }

    [HttpPost("bulk-save")]
    [ProducesResponseType(typeof(ApiResponse<AssetDocumentBulkSaveResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkSaveAll(
        [FromBody] AssetDocumentBulkSaveDto bulkDto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid request data" });

            var result = await _service.BulkSaveAllAsync(bulkDto, GetUserId(), cancellationToken);
            var hasErrors = result.Errors.Any();

            return Ok(new ApiResponse<AssetDocumentBulkSaveResponseDto>
            {
                Success = !hasErrors,
                Message = hasErrors 
                    ? $"Saved with {result.Errors.Count} error(s). Enabled: {result.EnabledCount}, Disabled: {result.DisabledCount}"
                    : $"All asset documents saved successfully. Enabled: {result.EnabledCount}, Disabled: {result.DisabledCount}",
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
            _logger.LogError(ex, "Error in asset document bulk save. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while saving asset documents",
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// POST - Single multipart endpoint: registers an asset document slot and uploads the file in one call.
    /// All fields bind directly from the form.
    /// The service handles all validation, file-type checking, slot registration, and upload.
    /// Exceptions propagate to <c>GlobalExceptionHandlerMiddleware</c>.
    /// </summary>
    [HttpPost("save-with-upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<AssetDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveWithUpload(
        [FromForm] AssetDocumentSaveWithUploadDto request,
        CancellationToken cancellationToken)
    {
        var result = await _service.SaveWithUploadAsync(request, GetUserId(), cancellationToken);
        return Ok(new ApiResponse<AssetDocumentDto>
        {
            Success = true,
            Message = "Document saved and uploaded successfully.",
            Items = result
        });
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
