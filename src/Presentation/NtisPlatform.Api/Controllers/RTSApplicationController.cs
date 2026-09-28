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
    private readonly IRepository<SMSMasterEntity, int> _smsMasterRepository;
    private readonly ILogger<RTSApplicationController> _logger;

    public RTSApplicationController(
        IRTSApplicationService service,
        IRTSSmsNotificationService smsNotificationService,
        IRepository<SMSGatewayMasterEntity, int> gatewayRepository,
        IRepository<SMSMasterEntity, int> smsMasterRepository,
        ILogger<RTSApplicationController> logger)
    {
        _service = service;
        _smsNotificationService = smsNotificationService;
        _gatewayRepository = gatewayRepository;
        _smsMasterRepository = smsMasterRepository;
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

        var txnId = $"txn_{sanitizedMobile}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        // 1. Check if live SMS Gateway is enabled in database
        var isGatewayActive = await _gatewayRepository.GetQueryable().AnyAsync(g => g.IsActive, ct);

        // 2. Check if OTP template is active in database (CORE.SMSMaster)
        var isOtpTemplateActive = await _smsMasterRepository.GetQueryable().AnyAsync(t => t.IsActive && t.TemplateName == "RTS_CITIZEN_LOGIN_OTP", ct);

        // If SMS Gateway or OTP template is inactive, enable smooth direct login (bypass OTP step)
        if (!isGatewayActive || !isOtpTemplateActive)
        {
            _logger.LogInformation("SMS Gateway or RTS_CITIZEN_LOGIN_OTP template is inactive in database. Enabling direct login for {Mobile}", sanitizedMobile);
            return Ok(new
            {
                success = true,
                directLogin = true,
                isLive = false,
                message = "SMS service is disabled in database. Direct login enabled.",
                txnId,
                otp = "123456",
                expiresInSeconds = 120
            });
        }

        var otp = new Random().Next(100000, 999999).ToString();

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
            directLogin = false,
            isLive = true,
            message = "OTP dispatched successfully via official SMS gateway.",
            txnId,
            otp,
            expiresInSeconds = 120
        });
    }
}
