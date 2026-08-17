using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Sms;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSSmsNotificationService : IRTSSmsNotificationService
{
    private readonly ISmsService _smsService;
    private readonly IRepository<SMSMasterEntity, int> _smsMasterRepository;
    private readonly IRepository<SMSTypeEntity, int> _smsTypeRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RTSSmsNotificationService> _logger;

    public RTSSmsNotificationService(
        ISmsService smsService,
        IRepository<SMSMasterEntity, int> smsMasterRepository,
        IRepository<SMSTypeEntity, int> smsTypeRepository,
        IConfiguration configuration,
        ILogger<RTSSmsNotificationService> logger)
    {
        _smsService = smsService;
        _smsMasterRepository = smsMasterRepository;
        _smsTypeRepository = smsTypeRepository;
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

    private async Task SendDynamicSmsAsync(
        string templateName,
        string typeName,
        string mobileNo,
        int? applicationId,
        Dictionary<string, string> placeholders,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(mobileNo)) return;

        try
        {
            // 1. Fetch template from DB by TemplateName
            var template = await _smsMasterRepository.GetQueryable()
                .Include(t => t.SmsType)
                .Where(t => t.IsActive && t.TemplateName == templateName)
                .OrderByDescending(t => t.SmsID)
                .FirstOrDefaultAsync(ct);

            // Fallback: Fetch by TypeName
            if (template == null)
            {
                template = await _smsMasterRepository.GetQueryable()
                    .Include(t => t.SmsType)
                    .Where(t => t.IsActive && t.SmsType != null && t.SmsType.TypeName == typeName)
                    .OrderByDescending(t => t.SmsID)
                    .FirstOrDefaultAsync(ct);
            }

            if (template == null || string.IsNullOrWhiteSpace(template.SmsText))
            {
                _logger.LogWarning("SMS Template '{TemplateName}' / '{TypeName}' not found or empty in CORE.SMSMaster. Skipping SMS.", templateName, typeName);
                return;
            }

            // 2. Perform Dynamic Placeholder Replacements from DB template
            var message = template.SmsText;
            foreach (var kv in placeholders)
            {
                message = message.Replace($"{{{kv.Key}}}", kv.Value, StringComparison.OrdinalIgnoreCase);
            }

            // 3. Dispatch via dynamic SmsService
            await _smsService.SendSmsAsync(new SmsRequest
            {
                PhoneNumber = mobileNo,
                Message = message,
                TemplateId = template.TemplateID,
                TemplateName = template.TemplateName,
                SMSTypeID = template.SMSTypeID,
                ApplicationId = applicationId
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing dynamic SMS for template '{TemplateName}' to {Mobile}", templateName, mobileNo);
        }
    }

    public async Task SendApplicationSubmittedAsync(
        int applicationId,
        string applicationNo,
        string citizenName,
        string mobileNo,
        string serviceName,
        CancellationToken ct = default)
    {
        var portalUrl = $"{GetPortalBaseUrl()}?track={Uri.EscapeDataString(applicationNo)}";
        var placeholders = new Dictionary<string, string>
        {
            { "UserName", citizenName },
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "ApplicationNo", applicationNo },
            { "ServiceName", serviceName },
            { "TrackingUrl", portalUrl }
        };

        await SendDynamicSmsAsync("RTS_APP_SUBMITTED", "RTS Application Submitted", mobileNo, applicationId, placeholders, ct);
    }

    public async Task SendPaymentPendingAsync(
        int applicationId,
        string applicationNo,
        string citizenName,
        string mobileNo,
        string serviceName,
        decimal amount,
        CancellationToken ct = default)
    {
        var portalUrl = $"{GetPortalBaseUrl()}?pay={Uri.EscapeDataString(applicationNo)}";
        var placeholders = new Dictionary<string, string>
        {
            { "UserName", citizenName },
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "ApplicationNo", applicationNo },
            { "ServiceName", serviceName },
            { "Amount", amount.ToString("F2") },
            { "TrackingUrl", portalUrl }
        };

        await SendDynamicSmsAsync("RTS_PAYMENT_PENDING", "RTS Payment Pending", mobileNo, applicationId, placeholders, ct);
    }

    public async Task SendPaymentSuccessAsync(
        int applicationId,
        string applicationNo,
        string citizenName,
        string mobileNo,
        decimal amount,
        string receiptNo,
        CancellationToken ct = default)
    {
        var portalUrl = $"{GetPortalBaseUrl()}?receipt={Uri.EscapeDataString(receiptNo)}";
        var placeholders = new Dictionary<string, string>
        {
            { "UserName", citizenName },
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "ApplicationNo", applicationNo },
            { "Amount", amount.ToString("F2") },
            { "ReceiptNo", receiptNo },
            { "TrackingUrl", portalUrl }
        };

        await SendDynamicSmsAsync("RTS_FEE_PAID", "Online Fee Paid", mobileNo, applicationId, placeholders, ct);
    }

    public async Task SendApplicationApprovedAsync(
        int applicationId,
        string applicationNo,
        string citizenName,
        string mobileNo,
        string serviceName,
        CancellationToken ct = default)
    {
        var portalUrl = $"{GetPortalBaseUrl()}?cert={Uri.EscapeDataString(applicationNo)}";
        var placeholders = new Dictionary<string, string>
        {
            { "UserName", citizenName },
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "ApplicationNo", applicationNo },
            { "ServiceName", serviceName },
            { "TrackingUrl", portalUrl }
        };

        await SendDynamicSmsAsync("RTS_APP_APPROVED", "RTS Application Approved", mobileNo, applicationId, placeholders, ct);
    }
}
