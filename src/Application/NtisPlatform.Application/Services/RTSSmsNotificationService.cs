using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Sms;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSSmsNotificationService : IRTSSmsNotificationService
{
    private readonly ISmsService _smsService;
    private readonly IRepository<SMSMasterEntity, int> _smsMasterRepository;
    private readonly IRepository<SMSTypeEntity, int> _smsTypeRepository;
    private readonly IRepository<ULBMasterEntity, int> _ulbRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RTSSmsNotificationService> _logger;

    public RTSSmsNotificationService(
        ISmsService smsService,
        IRepository<SMSMasterEntity, int> smsMasterRepository,
        IRepository<SMSTypeEntity, int> smsTypeRepository,
        IRepository<ULBMasterEntity, int> ulbRepository,
        IConfiguration configuration,
        ILogger<RTSSmsNotificationService> logger)
    {
        _smsService = smsService;
        _smsMasterRepository = smsMasterRepository;
        _smsTypeRepository = smsTypeRepository;
        _ulbRepository = ulbRepository;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetPortalBaseUrl()
    {
        var configuredUrl = _configuration["AppSettings:PortalUrl"]
                         ?? _configuration["RTS:PortalUrl"]
                         ?? _configuration["RTS:TrackingUrl"];

        if (!string.IsNullOrWhiteSpace(configuredUrl))
        {
            return configuredUrl.TrimEnd('/');
        }

        return "https://akolamc.org/service";
    }

    private async Task<string> GetCorporationNameAsync(CancellationToken ct)
    {
        try
        {
            var ulb = await _ulbRepository.GetQueryable().FirstOrDefaultAsync(u => u.IsActive, ct);
            return ulb?.UlbName ?? "Akola Municipal Corporation";
        }
        catch
        {
            return "Akola Municipal Corporation";
        }
    }

    private async Task SendDynamicSmsAsync(
        string templateName,
        string typeName,
        string defaultFallbackMessage,
        string defaultTemplateId,
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

            // 1. Check if custom template exists in database (SMSMaster)
            var template = await _smsMasterRepository.GetQueryable()
                .Include(t => t.SmsType)
                .Where(t => t.IsActive && t.TemplateName == templateName)
                .OrderByDescending(t => t.SmsID)
                .FirstOrDefaultAsync(ct);

            if (template == null)
            {
                template = await _smsMasterRepository.GetQueryable()
                    .Include(t => t.SmsType)
                    .Where(t => t.IsActive && t.SmsType != null && t.SmsType.TypeName == typeName)
                    .OrderByDescending(t => t.SmsID)
                    .FirstOrDefaultAsync(ct);
            }

            string rawText = template != null && !string.IsNullOrWhiteSpace(template.SmsText)
                ? template.SmsText
                : defaultFallbackMessage;

            string templateId = template?.TemplateID ?? defaultTemplateId;

            // 2. Perform Dynamic Replacements
            var message = rawText;
            foreach (var kv in placeholders)
            {
                message = message.Replace($"{{{kv.Key}}}", kv.Value, StringComparison.OrdinalIgnoreCase);
            }

            // 3. Dispatch via database-backed SmsService
            await _smsService.SendSmsAsync(new SmsRequest
            {
                PhoneNumber = mobileNo,
                Message = message,
                TemplateId = templateId,
                TemplateName = template?.TemplateName ?? templateName,
                SMSTypeID = template?.SMSTypeID,
                ApplicationId = applicationId
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS for template '{TemplateName}' to {Mobile}", templateName, mobileNo);
        }
    }

    /// <summary>
    /// 1. Citizen Portal OTP Login Template
    /// </summary>
    public async Task SendCitizenOtpAsync(string mobileNo, string otp, CancellationToken ct = default)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "Otp", otp },
            { "OTP", otp }
        };

        var fallbackMsg = "Your RTS Citizen Portal login OTP is {Otp}. Please do not share this OTP with anyone. - {CorporationName}";
        await SendDynamicSmsAsync("RTS_CITIZEN_LOGIN_OTP", "Citizen Login OTP", fallbackMsg, "1707175319753583565", mobileNo, null, placeholders, ct);
    }

    /// <summary>
    /// 2. Single Unified Dynamic Application Status Template (Triggers on all lifecycle actions)
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
        var trackingUrl = customUrl ?? $"{GetPortalBaseUrl()}?track={Uri.EscapeDataString(applicationNo)}";

        var placeholders = new Dictionary<string, string>
        {
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "UserName", citizenName },
            { "ApplicationNo", applicationNo },
            { "ServiceName", serviceName },
            { "Status", status },
            { "TrackingUrl", trackingUrl }
        };

        var fallbackMsg = "Dear {CitizenName}, your RTS Application No: {ApplicationNo} for {ServiceName} is currently {Status}. Track status: {TrackingUrl} - {CorporationName}";
        await SendDynamicSmsAsync("RTS_APP_STATUS_UPDATE", "RTS Application Status Update", fallbackMsg, "1707175319753583566", mobileNo, applicationId, placeholders, ct);
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
        var status = fees > 0
            ? $"SUBMITTED (Fee of Rs.{fees:F2} Pending)"
            : "SUBMITTED";

        var url = fees > 0
            ? $"{GetPortalBaseUrl()}?pay={Uri.EscapeDataString(applicationNo)}"
            : $"{GetPortalBaseUrl()}?track={Uri.EscapeDataString(applicationNo)}";

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
        var displayStatus = $"IN PROGRESS (At Stage: {stageName})";
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
        var certUrl = $"{GetPortalBaseUrl()}?cert={Uri.EscapeDataString(applicationNo)}";
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
            : $"REJECTED (Reason: {remark})";

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
            ? "REVERTED for correction"
            : $"REVERTED for correction (Remark: {remark})";

        await SendApplicationStatusUpdateAsync(applicationId, applicationNo, citizenName, mobileNo, serviceName, displayStatus, null, ct);
    }

    /// <summary>
    /// 3. Payment Success e-Receipt Template
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
        var receiptUrl = $"{GetPortalBaseUrl()}?receipt={Uri.EscapeDataString(receiptNo)}";
        var placeholders = new Dictionary<string, string>
        {
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "UserName", citizenName },
            { "ApplicationNo", applicationNo },
            { "Amount", amount.ToString("F2") },
            { "ReceiptNo", receiptNo },
            { "ReceiptUrl", receiptUrl },
            { "TrackingUrl", receiptUrl }
        };

        var fallbackMsg = "Dear {CitizenName}, payment of Rs.{Amount} for RTS Application No: {ApplicationNo} is successful. Receipt No: {ReceiptNo}. Download receipt: {ReceiptUrl} - {CorporationName}";
        await SendDynamicSmsAsync("RTS_FEE_PAID", "Online Fee Paid", fallbackMsg, "1707175319753583568", mobileNo, applicationId, placeholders, ct);
    }
}
