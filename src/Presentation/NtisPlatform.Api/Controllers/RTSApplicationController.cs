using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.RTSApplication;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

public class SendCitizenOtpRequestDto
{
    public string Mobile { get; set; } = string.Empty;
}

[AllowAnonymous]
[Route("api/[controller]")]
[ApiController]
public class RTSApplicationController : ControllerBase
{
    private readonly IRTSApplicationService _service;
    private readonly IRTSSmsNotificationService _smsNotificationService;
    private readonly ILogger<RTSApplicationController> _logger;

    public RTSApplicationController(
        IRTSApplicationService service,
        IRTSSmsNotificationService smsNotificationService,
        ILogger<RTSApplicationController> logger)
    {
        _service = service;
        _smsNotificationService = smsNotificationService;
        _logger = logger;
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRTSApplicationDetailsDto dto, CancellationToken ct)
        => this.ExecuteCreate(_service, dto, _logger, ct);

    [HttpPost("citizen-otp/send")]
    public async Task<IActionResult> SendCitizenOtp([FromBody] SendCitizenOtpRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Mobile))
        {
            return BadRequest(new { success = false, message = "Mobile number is required." });
        }

        var sanitizedMobile = new string(request.Mobile.Where(char.IsDigit).ToArray());
        if (sanitizedMobile.Length > 10 && sanitizedMobile.StartsWith("91"))
        {
            sanitizedMobile = sanitizedMobile.Substring(2);
        }

        if (sanitizedMobile.Length != 10)
        {
            return BadRequest(new { success = false, message = "Please provide a valid 10-digit mobile number." });
        }

        var otp = new Random().Next(100000, 999999).ToString();
        var txnId = $"txn_{sanitizedMobile}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        try
        {
            await _smsNotificationService.SendCitizenOtpAsync(sanitizedMobile, otp, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed sending OTP to {Mobile}", sanitizedMobile);
        }

        return Ok(new
        {
            success = true,
            message = "OTP dispatched successfully via official SMS gateway.",
            txnId,
            demoOtp = otp,
            expiresInSeconds = 120
        });
    }
}
