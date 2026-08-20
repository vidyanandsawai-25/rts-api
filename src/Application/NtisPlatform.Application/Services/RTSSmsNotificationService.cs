using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Sms;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// 100% Database-driven SMS Notification Service.
/// All SMS templates, DLT Template IDs, Gateway configurations, URLs, and Corporation details
/// are resolved dynamically from database tables CORE.SMSMaster, CORE.SMSGatewayMaster, and CORE.ULBMaster.
/// </summary>
public class RTSSmsNotificationService : IRTSSmsNotificationService
{
    private readonly ISmsService _smsService;
    private readonly IRepository<SMSMasterEntity, int> _smsMasterRepository;
    private readonly IRepository<SMSTypeEntity, int> _smsTypeRepository;
    private readonly IRepository<ULBMasterEntity, int> _ulbRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RTSSmsNotificationService> _logger;

    public RTSSmsNotificationService(
        ISmsService smsService,
        IRepository<SMSMasterEntity, int> smsMasterRepository,
        IRepository<SMSTypeEntity, int> smsTypeRepository,
        IRepository<ULBMasterEntity, int> ulbRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RTSSmsNotificationService> logger)
    {
        _smsService = smsService;
        _smsMasterRepository = smsMasterRepository;
        _smsTypeRepository = smsTypeRepository;
        _ulbRepository = ulbRepository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Dynamically resolves the portal base URL from the database ULBMaster.WebsiteUrl or current HTTP request host
    /// </summary>
    private async Task<string> GetPortalBaseUrlAsync(CancellationToken ct)
    {
        try
        {
            var ulb = await _ulbRepository.GetQueryable().FirstOrDefaultAsync(u => u.IsActive, ct);
            if (!string.IsNullOrWhiteSpace(ulb?.WebsiteUrl))
            {
                var cleanUrl = ulb.WebsiteUrl.Trim().TrimEnd('/');
                if (!cleanUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !cleanUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    cleanUrl = $"https://{cleanUrl}";
                }
                return cleanUrl.EndsWith("/service", StringComparison.OrdinalIgnoreCase) ? cleanUrl : $"{cleanUrl}/service";
            }
        }
        catch { }

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var origin = httpContext.Request.Headers["Origin"].FirstOrDefault()
                          ?? httpContext.Request.Headers["Referer"].FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(origin) && Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return $"{uri.Scheme}://{uri.Authority}/service";
                }

                if (httpContext.Request.Host.HasValue)
                {
                    return $"{httpContext.Request.Scheme}://{httpContext.Request.Host.Value}/service";
                }
            }
        }
        catch { }

        return "/service";
    }

    /// <summary>
    /// Dynamically resolves the corporation name from the database ULBMaster table
    /// </summary>
    private async Task<string> GetCorporationNameAsync(CancellationToken ct)
    {
        try
        {
            var ulb = await _ulbRepository.GetQueryable().FirstOrDefaultAsync(u => u.IsActive, ct);
            if (ulb != null)
            {
                if (!string.IsNullOrWhiteSpace(ulb.UlbName)) return ulb.UlbName.Trim();
                if (!string.IsNullOrWhiteSpace(ulb.UlbNameLocal)) return ulb.UlbNameLocal.Trim();
            }
        }
        catch { }

        return string.Empty;
    }

    /// <summary>
    /// 100% Dynamic SMS Dispatch: Fetches Template and DLT ID directly from DB table CORE.SMSMaster
    /// </summary>
    private async Task SendDynamicSmsAsync(
        string templateName,
        string mobileNo,
        int? applicationId,
        Dictionary<string, string> placeholders,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(mobileNo)) return;

        try
        {
            var corporationName = await GetCorporationNameAsync(ct);
            placeholders["CorporationName"] = corporationName;
            placeholders["UlbName"] = corporationName;

            // 1. Fetch template dynamically from DB table CORE.SMSMaster
            var template = await _smsMasterRepository.GetQueryable()
                .Include(t => t.SmsType)
                .Where(t => t.IsActive && t.TemplateName == templateName)
                .OrderByDescending(t => t.SmsID)
                .FirstOrDefaultAsync(ct);

            if (template == null || string.IsNullOrWhiteSpace(template.SmsText))
            {
                _logger.LogWarning("SMS template '{TemplateName}' not found or inactive in CORE.SMSMaster database. Skipping dispatch.", templateName);
                return;
            }

            // 2. Perform Dynamic Placeholder Replacements on database template text
            var message = template.SmsText;
            foreach (var kv in placeholders)
            {
                message = message.Replace($"{{{kv.Key}}}", kv.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            // 3. Dispatch via database-backed SmsService with DB TemplateID
            await _smsService.SendSmsAsync(new SmsRequest
            {
                PhoneNumber = mobileNo,
                Message = message,
                TemplateId = template.TemplateID ?? string.Empty,
                TemplateName = template.TemplateName,
                SMSTypeID = template.SMSTypeID,
                ApplicationId = applicationId
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending dynamic SMS for template '{TemplateName}' to {Mobile}", templateName, mobileNo);
        }
    }

    /// <summary>
    /// 1. Citizen Portal OTP Login Template (TemplateName: RTS_CITIZEN_LOGIN_OTP)
    /// </summary>
    public async Task SendCitizenOtpAsync(string mobileNo, string otp, CancellationToken ct = default)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "Otp", otp },
            { "OTP", otp }
        };

        await SendDynamicSmsAsync("RTS_CITIZEN_LOGIN_OTP", mobileNo, null, placeholders, ct);
    }

    /// <summary>
    /// 2. Unified Dynamic Application Status Template (TemplateName: RTS_APP_STATUS_UPDATE)
    /// </summary>
    public async Task SendApplicationStatusUpdateAsync(
        int applicationId,
        string applicationNo,
        string citizenName,
        string mobileNo,
        string serviceName,
        string status,
        string? customUrl = null,
        CancellationToken ct = default)
    {
        var baseUrl = await GetPortalBaseUrlAsync(ct);
        var trackingUrl = customUrl ?? $"{baseUrl}?track={Uri.EscapeDataString(applicationNo)}";

        var placeholders = new Dictionary<string, string>
        {
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "UserName", citizenName },
            { "ApplicationNo", applicationNo },
            { "ServiceName", serviceName },
            { "Status", status },
            { "TrackingUrl", trackingUrl },
            { "TrackingParam", applicationNo }
        };

        await SendDynamicSmsAsync("RTS_APP_STATUS_UPDATE", mobileNo, applicationId, placeholders, ct);
    }

    /// <summary>
    /// Form submission action
    /// </summary>
    public async Task SendApplicationSubmittedAsync(
        int applicationId,
        string applicationNo,
        string citizenName,
        string mobileNo,
        string serviceName,
        decimal fees = 0,
        CancellationToken ct = default)
    {
        var baseUrl = await GetPortalBaseUrlAsync(ct);
        var status = fees > 0
            ? "SUBMITTED (Fee Pending)"
            : "SUBMITTED";

        var url = fees > 0
            ? $"{baseUrl}?pay={Uri.EscapeDataString(applicationNo)}"
            : $"{baseUrl}?track={Uri.EscapeDataString(applicationNo)}";

        await SendApplicationStatusUpdateAsync(applicationId, applicationNo, citizenName, mobileNo, serviceName, status, url, ct);
    }

    /// <summary>
    /// Officer advances stage action
    /// </summary>
    public async Task SendApplicationStageAdvancedAsync(
        int applicationId,
        string applicationNo,
        string citizenName,
        string mobileNo,
        string serviceName,
        string stageName,
        string status,
        string? remark = null,
        CancellationToken ct = default)
    {
        var displayStatus = $"IN PROGRESS ({stageName})";
        await SendApplicationStatusUpdateAsync(applicationId, applicationNo, citizenName, mobileNo, serviceName, displayStatus, null, ct);
    }

    /// <summary>
    /// Final approval action
    /// </summary>
    public async Task SendApplicationApprovedAsync(
        int applicationId,
        string applicationNo,
        string citizenName,
        string mobileNo,
        string serviceName,
        CancellationToken ct = default)
    {
        var baseUrl = await GetPortalBaseUrlAsync(ct);
        var certUrl = $"{baseUrl}?cert={Uri.EscapeDataString(applicationNo)}";
        await SendApplicationStatusUpdateAsync(applicationId, applicationNo, citizenName, mobileNo, serviceName, "APPROVED", certUrl, ct);
    }

    /// <summary>
    /// Rejection action
    /// </summary>
    public async Task SendApplicationRejectedAsync(
        int applicationId,
        string applicationNo,
        string citizenName,
        string mobileNo,
        string serviceName,
        string? remark = null,
        CancellationToken ct = default)
    {
        var displayStatus = string.IsNullOrWhiteSpace(remark)
            ? "REJECTED"
            : $"REJECTED ({remark})";

        await SendApplicationStatusUpdateAsync(applicationId, applicationNo, citizenName, mobileNo, serviceName, displayStatus, null, ct);
    }

    /// <summary>
    /// Revert action
    /// </summary>
    public async Task SendApplicationRevertedAsync(
        int applicationId,
        string applicationNo,
        string citizenName,
        string mobileNo,
        string serviceName,
        string? remark = null,
        CancellationToken ct = default)
    {
        var displayStatus = string.IsNullOrWhiteSpace(remark)
            ? "REVERTED"
            : $"REVERTED ({remark})";

        await SendApplicationStatusUpdateAsync(applicationId, applicationNo, citizenName, mobileNo, serviceName, displayStatus, null, ct);
    }

    /// <summary>
    /// 3. Payment Success e-Receipt Template (TemplateName: RTS_FEE_PAID)
    /// </summary>
    public async Task SendPaymentSuccessAsync(
        int applicationId,
        string applicationNo,
        string citizenName,
        string mobileNo,
        decimal amount,
        string receiptNo,
        CancellationToken ct = default)
    {
        var baseUrl = await GetPortalBaseUrlAsync(ct);
        var receiptUrl = $"{baseUrl}?receipt={Uri.EscapeDataString(receiptNo)}";
        var placeholders = new Dictionary<string, string>
        {
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "UserName", citizenName },
            { "ApplicationNo", applicationNo },
            { "Amount", amount.ToString("F2") },
            { "ReceiptNo", receiptNo },
            { "ReceiptUrl", receiptUrl },
            { "TrackingUrl", receiptUrl },
            { "ReceiptParam", receiptNo }
        };

        await SendDynamicSmsAsync("RTS_FEE_PAID", mobileNo, applicationId, placeholders, ct);
    }
}
