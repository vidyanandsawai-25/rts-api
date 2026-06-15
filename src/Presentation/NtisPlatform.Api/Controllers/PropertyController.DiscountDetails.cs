using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NtisPlatform.Application.DTOs.PropertyDiscount;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Discount Information Tab API - Partial controller for segregated property endpoints
/// Handles the `{propertyId}/discount-details` API endpoint which loads discount-applicable social attributes
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves discount information for a specific property including all social attributes where IsDiscountApplicable=1.
    /// This endpoint is used to populate the Discount Information tab in the property form.
    /// Returns all discount-applicable attributes with their current toggle states, values, and associated documents.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Property discount details including all applicable discount attributes</returns>
    /// <response code="200">Returns the property discount details</response>
    /// <response code="404">Property not found</response>
    [HttpGet("{propertyId}/discount-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDiscountInfoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDiscountDetails(int propertyId, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.GetDiscountDetailsAsync(propertyId, ct);

            if (result == null)
            {
                _logger.LogWarning("Property with ID {PropertyId} not found", propertyId);
                return NotFound(new ApiResponse<PropertyDiscountInfoResponseDto>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} not found"
                });
            }

            return Ok(new ApiResponse<PropertyDiscountInfoResponseDto>
            {
                Success = true,
                Message = "Discount information retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving discount details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyDiscountInfoResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property discount information"
                });
        }
    }

    /// <summary>
    /// Updates discount information for a specific property by upserting PropertySocialDetails records.
    /// Handles toggle states (BitValue), numeric values, text values, dates, and document uploads.
    /// Only processes social attributes where IsDiscountApplicable=1.
    /// </summary>
    /// <param name="propertyId">The unique identifier of the property</param>
    /// <param name="dto">The discount information update payload containing attribute values and documents</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success response with updated discount data</returns>
    /// <response code="200">Discount information updated successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="400">Invalid data - Validation error</response>
    [HttpPut("{propertyId}/discount-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDiscountInfoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDiscountDetails(int propertyId, [FromBody] UpsertPropertyDiscountInfoDto dto, CancellationToken ct)
    {
        try
        {
            // Validate propertyId matches the DTO
            if (dto.PropertyId != propertyId)
            {
                return BadRequest(new ApiResponse<PropertyDiscountInfoResponseDto>
                {
                    Success = false,
                    Message = "PropertyId in URL does not match PropertyId in request body"
                });
            }

            var result = await _propertyService.UpdateDiscountDetailsAsync(propertyId, dto, ct);

            if (result == null)
            {
                _logger.LogWarning("Property with ID {PropertyId} not found for update", propertyId);
                return NotFound(new ApiResponse<PropertyDiscountInfoResponseDto>
                {
                    Success = false,
                    Message = $"Property with ID {propertyId} not found"
                });
            }

            return Ok(new ApiResponse<PropertyDiscountInfoResponseDto>
            {
                Success = true,
                Message = "Discount information updated successfully",
                Items = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating discount details for property {PropertyId}", propertyId);
            return BadRequest(new ApiResponse<PropertyDiscountInfoResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating discount details for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyDiscountInfoResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while updating property discount information"
                });
        }
    }

    /// <summary>
    /// Upload a document for a discount attribute.
    /// Creates or updates PropertySocialDetails record with the uploaded document.
    /// Rate limited to prevent abuse.
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("discount-details/upload")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("fileupload")]
    [ProducesResponseType(typeof(ApiResponse<DiscountDocumentUploadResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UploadDiscountDocument(
        [FromForm] DiscountDocumentUploadFormDto formDto,
        CancellationToken ct)
    {
        try
        {
            if (formDto.File == null || formDto.File.Length == 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "File is required" });

            if (!_fileValidationHelper.IsValidFileType(formDto.File.ContentType, formDto.File.FileName))
                return BadRequest(new ApiResponse<object> { Success = false, Message = _fileValidationHelper.GetInvalidFileTypeMessage() });

            if (formDto.PropertyId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "PropertyId is required" });

            if (formDto.SocialAttributeId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "SocialAttributeId is required" });

            using var stream = formDto.File.OpenReadStream();
            var result = await _discountDocumentService.UploadDiscountDocumentAsync(
                stream,
                formDto.File.FileName,
                formDto.File.ContentType,
                formDto.File.Length,
                formDto.PropertyId,
                formDto.SocialAttributeId,
                formDto.Remark,
                GetUserId(),
                formDto.IsPhoto,
                ct);

            return Ok(new ApiResponse<DiscountDocumentUploadResponseDto>
            {
                Success = true,
                Message = "Discount document uploaded successfully",
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
            _logger.LogWarning(ex, "Validation error uploading discount document. CorrelationId: {CorrelationId}", correlationId);
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
            _logger.LogError(ex, "Error uploading discount document. CorrelationId: {CorrelationId}, FileName: {FileName}",
                correlationId, formDto.File?.FileName ?? "unknown");

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
    /// Replace an existing discount document.
    /// Updates the PropertySocialDetails record with a new document.
    /// Rate limited to prevent abuse.
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("discount-details/{propertySocialDetailId}/replace-document")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("fileupload")]
    [ProducesResponseType(typeof(ApiResponse<DiscountDocumentUploadResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ReplaceDiscountDocument(
        int propertySocialDetailId,
        [FromForm] ReplaceDiscountDocumentFormDto formDto,
        CancellationToken ct)
    {
        try
        {
            if (propertySocialDetailId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid PropertySocialDetailId" });

            if (formDto.File == null || formDto.File.Length == 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "File is required" });

            if (!_fileValidationHelper.IsValidFileType(formDto.File.ContentType, formDto.File.FileName))
                return BadRequest(new ApiResponse<object> { Success = false, Message = _fileValidationHelper.GetInvalidFileTypeMessage() });

            await using var stream = formDto.File.OpenReadStream();

            var result = await _discountDocumentService.ReplaceDiscountDocumentAsync(
                propertySocialDetailId,
                stream,
                formDto.File.FileName,
                formDto.File.ContentType,
                formDto.File.Length,
                formDto.Remark,
                GetUserId(),
                formDto.IsPhoto,
                ct);

            return Ok(new ApiResponse<DiscountDocumentUploadResponseDto>
            {
                Success = true,
                Message = "Discount document replaced successfully",
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
        catch (InvalidOperationException ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogWarning(ex, "PropertySocialDetail not found: {Id}. CorrelationId: {CorrelationId}",
                propertySocialDetailId, correlationId);
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
            _logger.LogWarning(ex, "Validation error replacing discount document: {Id}. CorrelationId: {CorrelationId}",
                propertySocialDetailId, correlationId);
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
            _logger.LogError(ex, "Error replacing discount document: {Id}. CorrelationId: {CorrelationId}",
                propertySocialDetailId, correlationId);

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
