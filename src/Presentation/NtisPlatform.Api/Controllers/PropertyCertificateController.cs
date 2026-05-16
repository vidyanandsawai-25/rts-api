using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NtisPlatform.Api.Constants;
using NtisPlatform.Application.DTOs.PropertyCertificate;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Controller for PTIS.PropertyCertificate operations
/// SEPARATE from Document controller
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
    /// Upload PropertyCertificate with document.
    /// Creates: PTIS.PropertyCertificate + CORE.Document + CORE.DocumentBinding.
    /// Max file size is configured in appsettings.json under FileStorage:MaxFileSizeBytes (default: 100MB).
    /// The file size limit is enforced at runtime through FormOptions configuration.
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
    /// Get PropertyCertificates by PropertyId
    /// </summary>
    [HttpGet("by-property/{propertyId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertyCertificateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPropertyId(int propertyId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByPropertyIdAsync(propertyId, cancellationToken);
        return Ok(new ApiResponse<List<PropertyCertificateDto>> { Success = true, Items = result });
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
