using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.RTSCertificate;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System.Security.Claims;

namespace NtisPlatform.Api.Controllers;

[Route("api/rts-certificate-templates")]
[ApiController]
[AllowAnonymous]
public class RTSCertificateTemplatesController : ControllerBase
{
    private readonly IRTSCertificateTemplateLibraryService _service;

    public RTSCertificateTemplatesController(IRTSCertificateTemplateLibraryService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RTSCertificateLibraryTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _service.GetAllAsync(ct);
        return Ok(new ApiResponse<List<RTSCertificateLibraryTemplateDto>>
        {
            Success = true,
            Message = "Certificate templates retrieved successfully",
            Items = result
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<RTSCertificateLibraryTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (result == null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Certificate template not found" });

        return Ok(new ApiResponse<RTSCertificateLibraryTemplateDto>
        {
            Success = true,
            Message = "Certificate template retrieved successfully",
            Items = result
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RTSCertificateLibraryTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateRTSCertificateLibraryTemplateDto dto, CancellationToken ct)
    {
        var result = await _service.CreateAsync(dto, GetUserId(), ct);
        return Ok(new ApiResponse<RTSCertificateLibraryTemplateDto>
        {
            Success = true,
            Message = "Certificate template created successfully",
            Items = result
        });
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<RTSCertificateLibraryTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateRTSCertificateLibraryTemplateDto dto,
        CancellationToken ct)
    {
        dto.Id = id;
        var result = await _service.UpdateAsync(dto, GetUserId(), ct);
        return Ok(new ApiResponse<RTSCertificateLibraryTemplateDto>
        {
            Success = true,
            Message = "Certificate template updated successfully",
            Items = result
        });
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, GetUserId(), ct);
        return Ok(new ApiResponse<bool>
        {
            Success = result,
            Message = result ? "Certificate template deleted successfully" : "Certificate template not found",
            Items = result
        });
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("id") ?? User.FindFirst("UserId");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : 1;
    }
}
