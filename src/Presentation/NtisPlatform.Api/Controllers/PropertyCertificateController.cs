using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NtisPlatform.Api.Constants;
using NtisPlatform.Application.DTOs.PropertyCertificate;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Controller for Building Permissions and Certificates.
/// Aligned with the Property Quick Data Entry UI.
/// Handles: Loading certificate types with status, uploading documents, replacing documents, and bulk saving all changes.
/// </summary>
[ApiController]
[Route("api/property-certificates")]
[Authorize]
public class PropertyCertificateController : ControllerBase
{
    private readonly IPropertyCertificateApplicationService _service;
    private readonly ILogger<PropertyCertificateController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly FileValidationHelper _fileValidationHelper;

    public PropertyCertificateController(
        IPropertyCertificateApplicationService service,
        ILogger<PropertyCertificateController> logger,
        IWebHostEnvironment environment,
        FileValidationHelper fileValidationHelper)
    {
        _service = service;
        _logger = logger;
        _environment = environment;
        _fileValidationHelper = fileValidationHelper;
    }

    /// <summary>
    /// 1. GET - Load all certificate types with their current status for a property.
    /// Called when the page opens to populate all certificate cards.
    /// Shows which certificates exist (with data) and which are empty.
    /// </summary>
    [HttpGet("types-with-status/{propertyId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertyCertificateWithStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCertificateTypesWithStatus(
        int propertyId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (propertyId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid PropertyId" });

            var result = await _service.GetCertificateTypesWithStatusAsync(propertyId, cancellationToken);

            return Ok(new ApiResponse<List<PropertyCertificateWithStatusDto>>
            {
                Success = true,
                Message = "Certificate types retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting certificate types with status for PropertyId={PropertyId}. CorrelationId: {CorrelationId}",
                propertyId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving certificate types",
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// 2. POST - Upload a new certificate document.
    /// Called when user clicks "Upload Doc" button on a certificate card.
    /// Creates a new certificate with document.
    /// Max file size is configured in appsettings.json under FileStorage:MaxFileSizeBytes (default: 100MB).
    /// Rate limited to prevent abuse - configured in appsettings.json under RateLimiting:FileUpload (default: 10 uploads per 5 minutes)
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("fileupload")]
    [ProducesResponseType(typeof(ApiResponse<PropertyCertificateUploadResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Upload(
        [FromForm] PropertyCertificateUploadFormDto formDto,
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

            if (formDto.CertificateTypeId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "CertificateTypeId is required" });

            using var stream = formDto.File.OpenReadStream();
            var result = await _service.UploadWithDocumentAsync(
                stream,
                formDto.File.FileName,
                formDto.File.ContentType,
                formDto.File.Length,
                formDto.PropertyId,
                formDto.CertificateTypeId,
                formDto.CertificateNo,
                formDto.IssueDate,
                GetUserId(),
                cancellationToken);

            return Ok(new ApiResponse<PropertyCertificateUploadResponseDto>
            {
                Success = true,
                Message = "PropertyCertificate uploaded successfully",
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
            _logger.LogWarning(ex, "Validation error uploading PropertyCertificate. CorrelationId: {CorrelationId}, PropertyId: {PropertyId}, CertificateTypeId: {CertificateTypeId}", 
                correlationId, formDto.PropertyId, formDto.CertificateTypeId);
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
            _logger.LogError(ex, "Error uploading PropertyCertificate. CorrelationId: {CorrelationId}, PropertyId: {PropertyId}, CertificateTypeId: {CertificateTypeId}, FileName: {FileName}", 
                correlationId, formDto.PropertyId, formDto.CertificateTypeId, formDto.File?.FileName ?? "unknown");

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
    /// 3. POST - Replace an existing certificate document.
    /// Called when user clicks "Upload Doc" button on a certificate card that already has a document.
    /// Replaces the old document with a new one.
    /// Rate limited to prevent abuse - configured in appsettings.json under RateLimiting:FileUpload (default: 10 uploads per 5 minutes)
    /// </summary>
    [HttpPost("{propertyCertificateId}/replace-document")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("fileupload")]
    [ProducesResponseType(typeof(ApiResponse<PropertyCertificateUploadResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ReplaceDocument(
        int propertyCertificateId,
        [FromForm] ReplacePropertyCertificateDocumentFormDto formDto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (propertyCertificateId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid PropertyCertificateId" });

            if (formDto.File == null || formDto.File.Length == 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "File is required" });

            // Validate file
            if (!_fileValidationHelper.IsValidFileType(formDto.File.ContentType, formDto.File.FileName))
                return BadRequest(new ApiResponse<object> { Success = false, Message = _fileValidationHelper.GetInvalidFileTypeMessage() });

            await using var stream = formDto.File.OpenReadStream();

            var result = await _service.ReplaceDocumentAsync(
                propertyCertificateId,
                stream,
                formDto.File.FileName,
                formDto.File.ContentType,
                formDto.File.Length,
                GetUserId(),
                cancellationToken);

            return Ok(new ApiResponse<PropertyCertificateUploadResponseDto>
            {
                Success = true,
                Message = "Certificate document replaced successfully",
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
            _logger.LogWarning(ex, "Certificate not found: {Id}. CorrelationId: {CorrelationId}",
                propertyCertificateId, correlationId);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                CorrelationId = correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error replacing document for certificate: {Id}. CorrelationId: {CorrelationId}",
                propertyCertificateId, correlationId);

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
    /// 4. POST - Save all certificate changes with a single button.
    /// Called when user clicks the "Save Changes" button at the bottom of the page.
    /// Saves all metadata for all certificates at once:
    /// - Certificate numbers
    /// - Certificate dates
    /// - Enable/disable status (toggle switches)
    /// Note: Documents are uploaded separately via the upload/replace-document endpoints.
    /// </summary>
    [HttpPost("bulk-save")]
    [ProducesResponseType(typeof(ApiResponse<PropertyCertificateBulkSaveResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkSaveAll(
        [FromBody] PropertyCertificateBulkSaveDto bulkDto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid request data" });

            var result = await _service.BulkSaveAllAsync(bulkDto, GetUserId(), cancellationToken);

            var hasErrors = result.Errors.Any();

            return Ok(new ApiResponse<PropertyCertificateBulkSaveResponseDto>
            {
                Success = !hasErrors,
                Message = hasErrors 
                    ? $"Saved with {result.Errors.Count} error(s). Enabled: {result.EnabledCount}, Disabled: {result.DisabledCount}"
                    : $"All certificates saved successfully. Enabled: {result.EnabledCount}, Disabled: {result.DisabledCount}",
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
            _logger.LogError(ex, "Error in bulk save. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while saving certificates",
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
