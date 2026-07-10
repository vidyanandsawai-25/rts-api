using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NtisPlatform.Application.DTOs.PropertyPhoto;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Exceptions;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Controller for PTIS.PropertyPhoto operations, aligned with the Property Photo UI
/// (Additional Images gallery, floor-plan viewer, Add Photo Plan Slot, Replace Image).
/// Handles: listing photos / photo-type slots, uploading, replacing and deleting photos.
/// The image bytes themselves are served by the existing DocumentController using the
/// returned DocumentGuid: GET /api/documents/{documentGuid}/view (inline / thumbnail) and
/// GET /api/documents/{documentGuid}/download (the "Download" button).
/// </summary>
[ApiController]
[Route("api/property-photos")]
[Authorize]
public class PropertyPhotoController : ControllerBase
{
    private readonly IPropertyPhotoApplicationService _service;
    private readonly ILogger<PropertyPhotoController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly FileValidationHelper _fileValidationHelper;

    public PropertyPhotoController(
        IPropertyPhotoApplicationService service,
        ILogger<PropertyPhotoController> logger,
        IWebHostEnvironment environment,
        FileValidationHelper fileValidationHelper)
    {
        _service = service;
        _logger = logger;
        _environment = environment;
        _fileValidationHelper = fileValidationHelper;
    }

    /// <summary>
    /// GET - All current photos for a property (the "Additional Images" gallery).
    /// Each item includes the DocumentGuid; clients build the image URLs via the existing
    /// DocumentController: GET /api/documents/{documentGuid}/view (inline / thumbnail) and
    /// GET /api/documents/{documentGuid}/download.
    /// </summary>
    [HttpGet("property/{propertyId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertyPhotoDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPhotosByProperty(int propertyId, CancellationToken cancellationToken)
    {
        try
        {
            if (propertyId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid PropertyId" });

            var result = await _service.GetPhotosByPropertyAsync(propertyId, cancellationToken);

            return Ok(new ApiResponse<List<PropertyPhotoDto>>
            {
                Success = true,
                Message = "Property photos retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting photos for PropertyId={PropertyId}. CorrelationId: {CorrelationId}",
                propertyId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving property photos",
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// GET - The full photo gallery for a property as ONE grouped JSON: every active photo
    /// type with its current photos nested inside (left panel = types + counts, main panel =
    /// the selected type's photos). Each photo carries its DocumentGuid for view/download.
    /// </summary>
    [HttpGet("property/{propertyId}/grouped")]
    [ProducesResponseType(typeof(ApiResponse<PropertyPhotoGalleryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetGroupedPhotosByProperty(int propertyId, CancellationToken cancellationToken)
    {
        try
        {
            if (propertyId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid PropertyId" });

            var result = await _service.GetGroupedPhotosByPropertyAsync(propertyId, cancellationToken);

            return Ok(new ApiResponse<PropertyPhotoGalleryDto>
            {
                Success = true,
                Message = "Property photos retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting grouped photos for PropertyId={PropertyId}. CorrelationId: {CorrelationId}",
                propertyId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving property photos",
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// GET - All active photo types with their current status for a property.
    /// Drives the photo-slot picker / "Add Photo Plan Slot": shows which slots are filled
    /// (HasPhoto = true, with DocumentGuid) and which are still empty.
    /// </summary>
    [HttpGet("types-with-status/{propertyId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertyPhotoTypeWithStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPhotoTypesWithStatus(int propertyId, CancellationToken cancellationToken)
    {
        try
        {
            if (propertyId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid PropertyId" });

            var result = await _service.GetPhotoTypesWithStatusAsync(propertyId, cancellationToken);

            return Ok(new ApiResponse<List<PropertyPhotoTypeWithStatusDto>>
            {
                Success = true,
                Message = "Photo types retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting photo types with status for PropertyId={PropertyId}. CorrelationId: {CorrelationId}",
                propertyId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving photo types",
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
