using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.PropertyDiscount;
using NtisPlatform.Application.DTOs.PropertySocialDetails;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NtisPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PropertySocialDetailsController : ControllerBase
{
    private readonly IPropertySocialDetailsService _service;
    private readonly ILogger<PropertySocialDetailsController> _logger;
    private readonly IPropertySocialDetailsDocumentService _socialDetailsDocumentService;
    private readonly IWebHostEnvironment _environment;
    private readonly FileValidationHelper _fileValidationHelper;

    public PropertySocialDetailsController(
        ILogger<PropertySocialDetailsController> logger,
        IPropertySocialDetailsService service,
        IPropertySocialDetailsDocumentService socialDetailsDocumentService,
        IWebHostEnvironment environment,
        FileValidationHelper fileValidationHelper)
    {
        _service = service;
        _logger = logger;
        _socialDetailsDocumentService = socialDetailsDocumentService;
        _environment = environment;
        _fileValidationHelper = fileValidationHelper;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertySocialDetailsQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreatePropertySocialDetailsDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePropertySocialDetailsDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    /// <summary>
    /// Gets comprehensive social information for a property including ALL social attributes 
    /// in parent-child hierarchy with existing values and empty attributes.
    /// </summary>
    /// <param name="propertyId">The property ID to get social information for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Complete social attribute hierarchy with current values</returns>
    /// <response code="200">Returns all social attributes with values</response>
    /// <response code="500">If an error occurs</response>
    /// <remarks>
    /// This endpoint returns:
    /// - ALL active social attributes from SocialAttributeMaster
    /// - Parent-child hierarchy (e.g., HAS_SOLAR ? NO_OF_SOLAR)
    /// - Current values from PropertySocialDetails if they exist
    /// - Empty/null values for attributes not yet saved
    /// 
    /// Sample response structure:
    /// 
    ///     {
    ///       "propertyId": 123,
    ///       "socialAttributes": [
    ///         {
    ///           "id": 5,
    ///           "socialAttributeCode": "HAS_SOLAR",
    ///           "socialAttributeName": "Solar Installed",
    ///           "dataType": "BIT",
    ///           "bitValue": true,
    ///           "propertySocialDetailId": 100,
    ///           "children": [
    ///             {
    ///               "id": 6,
    ///               "socialAttributeCode": "NO_OF_SOLAR",
    ///               "socialAttributeName": "Number Of Solar Units",
    ///               "dataType": "INT",
    ///               "intValue": 10,
    ///               "propertySocialDetailId": 101,
    ///               "isRequiredWhenParentTrue": true,
    ///               "children": []
    ///             }
    ///           ]
    ///         }
    ///       ]
    ///     }
    /// </remarks>
    [HttpGet("property/{propertyId}/social-info")]
    [ProducesResponseType(typeof(ApiResponse<PropertySocialInfoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPropertySocialInfo(int propertyId, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetPropertySocialInfoAsync(propertyId, ct);

            return Ok(new ApiResponse<PropertySocialInfoResponseDto>
            {
                Success = true,
                Message = "Property social information retrieved successfully",
                Items = result
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NtisPlatform.Application.Exceptions.ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving social information for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertySocialInfoResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property social information"
                });
        }
    }

    /// <summary>
    /// Upsert (Add/Update/Remove) property social information in a single operation.
    /// This endpoint allows you to add new social attributes, update existing ones, and remove unwanted ones.
    /// </summary>
    /// <param name="dto">Contains the property ID, social attributes to add/update, and IDs of attributes to remove</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated list of all active social attributes for the property</returns>
    /// <response code="200">Returns the updated list of property social attributes</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="500">If an error occurs during the operation</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/PropertySocialDetails/upsert
    ///     {
    ///         "propertyId": 123,
    ///         "updatedBy": 1,
    ///         "socialAttributes": [
    ///             {
    ///                 "id": null,
    ///                 "socialAttributeId": 5,
    ///                 "bitValue": true,
    ///                 "remark": "Solar installed"
    ///             },
    ///             {
    ///                 "id": null,
    ///                 "socialAttributeId": 6,
    ///                 "intValue": 10,
    ///                 "remark": "10 solar units"
    ///             },
    ///             {
    ///                 "id": 10,
    ///                 "socialAttributeId": 3,
    ///                 "decimalValue": 100.5,
    ///                 "remark": "Updated road width"
    ///             }
    ///         ],
    ///         "socialAttributeIdsToRemove": [8, 9]
    ///     }
    ///     
    /// **How it works:**
    /// - **Add**: Set `id` to null or omit it, and provide the social attribute details
    /// - **Update**: Provide existing `id` with updated values
    /// - **Remove**: Add the `socialAttributeId` to `socialAttributeIdsToRemove` array (soft delete - sets IsActive = false)
    /// 
    /// **For parent-child relationships:**
    /// - When adding HAS_SOLAR = true, also add NO_OF_SOLAR with value
    /// - When removing parent, child values are preserved unless explicitly removed
    /// 
    /// All operations are performed in a single transaction for data consistency.
    /// </remarks>
    [HttpPut("upsert")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertySocialDetailsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpsertPropertySocialInfo([FromBody] UpsertPropertySocialInfoDto dto, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _service.UpsertPropertySocialInfoAsync(dto, ct);

            return Ok(new ApiResponse<List<PropertySocialDetailsDto>>
            {
                Success = true,
                Message = "Property social information updated successfully",
                Items = result
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NtisPlatform.Application.Exceptions.ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting property social information for property {PropertyId}", dto.PropertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<List<PropertySocialDetailsDto>>
                {
                    Success = false,
                    Message = "An error occurred while updating property social information"
                });
        }
    }

    /// <summary>
    /// Upload a document for a social details attribute.
    /// Creates or updates PropertySocialDetails record with the uploaded document.
    /// Rate limited to prevent abuse.
    /// </summary>
    [Authorize]
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("fileupload")]
    [ProducesResponseType(typeof(ApiResponse<PropertySocialDetailsDocumentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UploadSocialDocument(
        [FromForm] SocialDetailsDocumentUploadFormDto formDto,
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
            var result = await _socialDetailsDocumentService.UploadSocialDetailsDocumentAsync(
                stream,
                formDto.File.FileName,
                formDto.File.ContentType,
                formDto.File.Length,
                formDto.PropertyId,
                formDto.SocialAttributeId,
                formDto.Remark,
                GetUserId(),
                formDto.IsPhoto,
                ct,
                restrictToDiscount: false,
                restrictToSocial: true);

            return Ok(new ApiResponse<PropertySocialDetailsDocumentResponseDto>
            {
                Success = true,
                Message = "Document uploaded successfully",
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
            _logger.LogWarning(ex, "Validation error uploading document. CorrelationId: {CorrelationId}", correlationId);
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
            _logger.LogError(ex, "Error uploading document. CorrelationId: {CorrelationId}, FileName: {FileName}",
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
    /// Replace an existing social details document.
    /// Updates the PropertySocialDetails record with a new document.
    /// Rate limited to prevent abuse.
    /// </summary>
    [Authorize]
    [HttpPost("{propertySocialDetailId}/replace-document")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("fileupload")]
    [ProducesResponseType(typeof(ApiResponse<PropertySocialDetailsDocumentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ReplaceSocialDocument(
        int propertySocialDetailId,
        [FromForm] ReplaceSocialDetailsDocumentFormDto formDto,
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

            var result = await _socialDetailsDocumentService.ReplaceSocialDetailsDocumentAsync(
                propertySocialDetailId,
                stream,
                formDto.File.FileName,
                formDto.File.ContentType,
                formDto.File.Length,
                formDto.Remark,
                GetUserId(),
                formDto.IsPhoto,
                ct,
                restrictToDiscount: false,
                restrictToSocial: true);

            return Ok(new ApiResponse<PropertySocialDetailsDocumentResponseDto>
            {
                Success = true,
                Message = "Document replaced successfully",
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
            _logger.LogWarning(ex, "Validation error replacing document: {Id}. CorrelationId: {CorrelationId}",
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
            _logger.LogError(ex, "Error replacing document: {Id}. CorrelationId: {CorrelationId}",
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
