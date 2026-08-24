using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.RTSApplication;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

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
    private readonly IRepository<SMSGatewayMasterEntity, int> _gatewayRepository;
    private readonly ILogger<RTSApplicationController> _logger;

    public RTSApplicationController(
        IRTSApplicationService service,
        IRTSSmsNotificationService smsNotificationService,
        IRepository<SMSGatewayMasterEntity, int> gatewayRepository,
        ILogger<RTSApplicationController> logger)
    {
        _service = service;
        _smsNotificationService = smsNotificationService;
        _gatewayRepository = gatewayRepository;
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

        // Check if live SMS Gateway is enabled in database
        var isGatewayActive = await _gatewayRepository.GetQueryable().AnyAsync(g => g.IsActive, ct);
        if (!isGatewayActive)
        {
            return BadRequest(new
            {
                success = false,
                message = "SMS Gateway is not active in database.",
            });
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
            return StatusCode(500, new { success = false, message = "Failed to dispatch OTP SMS via gateway." });
        }

        return Ok(new
        {
            success = true,
            isLive = true,
            message = "OTP dispatched successfully via official SMS gateway.",
            txnId,
            otp,
            expiresInSeconds = 120
        });
    }
}
