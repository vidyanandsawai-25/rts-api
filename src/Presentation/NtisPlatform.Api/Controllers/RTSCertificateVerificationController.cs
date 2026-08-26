using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.RTSCertificate;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

[Route("api/rts-certificate-verification")]
[ApiController]
[AllowAnonymous]
public class RTSCertificateVerificationController : ControllerBase
{
    private readonly IRTSCertificateService _service;
    private readonly ILogger<RTSCertificateVerificationController> _logger;

    public RTSCertificateVerificationController(IRTSCertificateService service, ILogger<RTSCertificateVerificationController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("verify/{certificateGuid}")]
    [ProducesResponseType(typeof(ApiResponse<CertificateVerificationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyCertificate(Guid certificateGuid, CancellationToken ct)
    {
        var result = await _service.VerifyCertificatePublicAsync(certificateGuid, ct);
        return Ok(new ApiResponse<CertificateVerificationResponseDto>
        {
            Success = result.IsValid,
            Message = result.Message ?? (result.IsValid ? "Certificate verified" : "Certificate invalid"),
            Items = result
        });
    }
}
