using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.RTSCertificate;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers;

[Route("api/rts-certificate")]
[ApiController]
public class RTSCertificateController : ControllerBase
{
    private readonly IRTSCertificateService _service;
    private readonly ILogger<RTSCertificateController> _logger;

    public RTSCertificateController(IRTSCertificateService service, ILogger<RTSCertificateController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("templates")]
    [HttpGet("service-configurations")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<RTSCertificateTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTemplates(CancellationToken ct)
    {
        var result = await _service.GetAllTemplatesAsync(ct);
        return Ok(new ApiResponse<List<RTSCertificateTemplateDto>>
        {
            Success = true,
            Message = "Templates retrieved successfully",
            Items = result
        });
    }

    [HttpGet("templates/{id}")]
    [HttpGet("service-configurations/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RTSCertificateTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplateById(int id, CancellationToken ct)
    {
        var result = await _service.GetTemplateByIdAsync(id, ct);
        if (result == null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Template not found" });

        return Ok(new ApiResponse<RTSCertificateTemplateDto>
        {
            Success = true,
            Message = "Template retrieved successfully",
            Items = result
        });
    }

    [HttpGet("templates/by-service/{serviceId}")]
    [HttpGet("service-configurations/by-service/{serviceId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RTSCertificateTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplateByServiceId(int serviceId, CancellationToken ct)
    {
        var result = await _service.GetTemplateByServiceIdAsync(serviceId, ct);
        return Ok(new ApiResponse<RTSCertificateTemplateDto?>
        {
            Success = true,
            Message = "Template retrieved successfully",
            Items = result
        });
    }

    [HttpGet("templates/available-tags/{serviceId}")]
    [HttpGet("service-configurations/available-tags/{serviceId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<CertificateAvailableTagDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableTags(int serviceId, CancellationToken ct)
    {
        var result = await _service.GetAvailableTagsForServiceAsync(serviceId, ct);
        return Ok(new ApiResponse<List<CertificateAvailableTagDto>>
        {
            Success = true,
            Message = "Available tags retrieved successfully",
            Items = result
        });
    }

    [HttpPost("templates")]
    [HttpPost("service-configurations")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RTSCertificateTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateRTSCertificateTemplateDto dto, CancellationToken ct)
    {
        int userId = GetUserId();
        var result = await _service.CreateTemplateAsync(dto, userId, ct);
        return Ok(new ApiResponse<RTSCertificateTemplateDto>
        {
            Success = true,
            Message = "Template created successfully",
            Items = result
        });
    }

    [HttpPut("templates/{id}")]
    [HttpPut("service-configurations/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RTSCertificateTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] UpdateRTSCertificateTemplateDto dto, CancellationToken ct)
    {
        dto.Id = id;
        int userId = GetUserId();
        var result = await _service.UpdateTemplateAsync(dto, userId, ct);
        return Ok(new ApiResponse<RTSCertificateTemplateDto>
        {
            Success = true,
            Message = "Template updated successfully",
            Items = result
        });
    }

    [HttpDelete("templates/{id}")]
    [HttpDelete("service-configurations/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteTemplate(int id, CancellationToken ct)
    {
        int userId = GetUserId();
        var result = await _service.DeleteTemplateAsync(id, userId, ct);
        return Ok(new ApiResponse<bool>
        {
            Success = result,
            Message = result ? "Template deleted successfully" : "Template not found",
            Items = result
        });
    }

    [HttpPost("preview")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CertificatePreviewResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewCertificate([FromBody] CertificatePreviewRequestDto request, CancellationToken ct)
    {
        var result = await _service.PreviewCertificateAsync(request, ct);
        return Ok(new ApiResponse<CertificatePreviewResponseDto>
        {
            Success = true,
            Message = "Certificate preview generated successfully",
            Items = result
        });
    }

    [HttpPost("issue")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RTSIssuedCertificateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> IssueCertificate([FromBody] IssueCertificateRequestDto request, CancellationToken ct)
    {
        int userId = GetUserId();
        var result = await _service.IssueCertificateAsync(request, userId, ct);
        return Ok(new ApiResponse<RTSIssuedCertificateDto>
        {
            Success = true,
            Message = "Certificate issued and digitally signed successfully",
            Items = result
        });
    }

    [HttpGet("by-application/{applicationNo}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RTSIssuedCertificateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByApplicationNo(string applicationNo, CancellationToken ct)
    {
        var result = await _service.GetIssuedCertificateByApplicationNoAsync(applicationNo, ct);
        if (result == null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Certificate not yet issued for this application" });

        return Ok(new ApiResponse<RTSIssuedCertificateDto>
        {
            Success = true,
            Message = "Certificate retrieved successfully",
            Items = result
        });
    }

    [HttpGet("by-guid/{certificateGuid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RTSIssuedCertificateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByGuid(Guid certificateGuid, CancellationToken ct)
    {
        var result = await _service.GetIssuedCertificateByGuidAsync(certificateGuid, ct);
        if (result == null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Certificate not found" });

        return Ok(new ApiResponse<RTSIssuedCertificateDto>
        {
            Success = true,
            Message = "Certificate retrieved successfully",
            Items = result
        });
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("id") ?? User.FindFirst("UserId");
        if (claim != null && int.TryParse(claim.Value, out int id))
            return id;
        return 1; // Default System Admin
    }
}
