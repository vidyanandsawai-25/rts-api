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

    /// <summary>
    /// POST - Upload a new photo for a property + photo type ("Add Photo Plan Slot").
    /// Max file size is configured in appsettings.json under FileStorage:MaxFileSizeBytes (default: 100MB).
    /// Rate limited - configured in appsettings.json under RateLimiting:FileUpload (default: 10 uploads per 5 minutes).
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("fileupload")]
    [ProducesResponseType(typeof(ApiResponse<PropertyPhotoUploadResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Upload(
        [FromForm] PropertyPhotoUploadFormDto formDto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (formDto.File == null || formDto.File.Length == 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "File is required" });

            if (!_fileValidationHelper.IsValidFileType(formDto.File.ContentType, formDto.File.FileName))
                return BadRequest(new ApiResponse<object> { Success = false, Message = _fileValidationHelper.GetInvalidFileTypeMessage() });

            if (formDto.PropertyId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "PropertyId is required" });

            if (formDto.PhotoTypeId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "PhotoTypeId is required" });

            using var stream = formDto.File.OpenReadStream();
            var result = await _service.UploadPhotoAsync(
                stream,
                formDto.File.FileName,
                formDto.File.ContentType,
                formDto.File.Length,
                formDto.PropertyId,
                formDto.PhotoTypeId,
                formDto.DisplayOrder,
                formDto.Remarks,
                GetUserId(),
                cancellationToken);

            return Ok(new ApiResponse<PropertyPhotoUploadResponseDto>
            {
                Success = true,
                Message = "Property photo uploaded successfully",
                Items = result
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
        catch (ArgumentException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Validation error uploading PropertyPhoto. CorrelationId: {CorrelationId}, PropertyId: {PropertyId}, PhotoTypeId: {PhotoTypeId}",
                correlationId, formDto.PropertyId, formDto.PhotoTypeId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                CorrelationId = correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error uploading PropertyPhoto. CorrelationId: {CorrelationId}, PropertyId: {PropertyId}, PhotoTypeId: {PhotoTypeId}, FileName: {FileName}",
                correlationId, formDto.PropertyId, formDto.PhotoTypeId, formDto.File?.FileName ?? "unknown");

            var errorMessage = _environment.IsDevelopment()
                ? $"An error occurred: {ex.Message}"
                : "An error occurred";

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = errorMessage,
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// POST - Replace an existing photo ("Replace Image"). The current photo is superseded
    /// (retained for audit) and a new latest version is stored.
    /// Rate limited - configured in appsettings.json under RateLimiting:FileUpload.
    /// </summary>
    [HttpPost("{propertyPhotoId}/replace")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("fileupload")]
    [ProducesResponseType(typeof(ApiResponse<PropertyPhotoUploadResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Replace(
        int propertyPhotoId,
        [FromForm] ReplacePropertyPhotoFormDto formDto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (propertyPhotoId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid PropertyPhotoId" });

            if (formDto.File == null || formDto.File.Length == 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "File is required" });

            if (!_fileValidationHelper.IsValidFileType(formDto.File.ContentType, formDto.File.FileName))
                return BadRequest(new ApiResponse<object> { Success = false, Message = _fileValidationHelper.GetInvalidFileTypeMessage() });

            await using var stream = formDto.File.OpenReadStream();

            var result = await _service.ReplacePhotoAsync(
                propertyPhotoId,
                stream,
                formDto.File.FileName,
                formDto.File.ContentType,
                formDto.File.Length,
                formDto.Remarks,
                GetUserId(),
                cancellationToken);

            return Ok(new ApiResponse<PropertyPhotoUploadResponseDto>
            {
                Success = true,
                Message = "Property photo replaced successfully",
                Items = result
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
        catch (PropertyPhotoNotFoundException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "PropertyPhoto not found: {Id}. CorrelationId: {CorrelationId}",
                propertyPhotoId, correlationId);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                CorrelationId = correlationId
            });
        }
        catch (ArgumentException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "Validation error replacing PropertyPhoto: {Id}. CorrelationId: {CorrelationId}",
                propertyPhotoId, correlationId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                CorrelationId = correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error replacing PropertyPhoto: {Id}. CorrelationId: {CorrelationId}",
                propertyPhotoId, correlationId);

            var errorMessage = _environment.IsDevelopment()
                ? $"An error occurred: {ex.Message}"
                : "An error occurred";

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = errorMessage,
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// DELETE - Soft delete a photo (two-phase delete). Frees the photo-type slot so a new
    /// photo can be uploaded for the same type.
    /// </summary>
    [HttpDelete("{propertyPhotoId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int propertyPhotoId, CancellationToken cancellationToken)
    {
        try
        {
            if (propertyPhotoId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid PropertyPhotoId" });

            var deleted = await _service.DeletePhotoAsync(propertyPhotoId, GetUserId(), cancellationToken);
            if (!deleted)
                return NotFound(new ApiResponse<object> { Success = false, Message = "Property photo not found" });

            return Ok(new ApiResponse<object> { Success = true, Message = "Property photo deleted" });
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
            _logger.LogError(ex, "Error deleting PropertyPhoto: {Id}. CorrelationId: {CorrelationId}",
                propertyPhotoId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while deleting the property photo",
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
