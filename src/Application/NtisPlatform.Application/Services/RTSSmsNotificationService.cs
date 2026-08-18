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

            string rawText = template != null && !string.IsNullOrWhiteSpace(template.SmsText)
                ? template.SmsText
                : defaultFallbackMessage;

            string templateId = template?.TemplateID ?? defaultTemplateId;

            // 2. Perform Dynamic Placeholder Replacements
            var message = rawText;
            foreach (var kv in placeholders)
            {
                message = message.Replace($"{{{kv.Key}}}", kv.Value, StringComparison.OrdinalIgnoreCase);
            }

            // 3. Dispatch via dynamic database-backed SmsService
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
            _logger.LogError(ex, "Error processing dynamic SMS for template '{TemplateName}' to {Mobile}", templateName, mobileNo);
        }
    }

    public async Task SendCitizenOtpAsync(string mobileNo, string otp, CancellationToken ct = default)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "Otp", otp },
            { "OTP", otp }
        };

        var fallbackMsg = "Your RTS Citizen Portal login OTP is {Otp}. Please do not share this OTP with anyone. - {CorporationName}";
        var fallbackTempId = "1707175319753583565";

        await SendDynamicSmsAsync("RTS_CITIZEN_LOGIN_OTP", "Citizen Login OTP", fallbackMsg, fallbackTempId, mobileNo, null, placeholders, ct);
    }

    public async Task SendApplicationSubmittedAsync(
        int applicationId,
        string applicationNo,
        string citizenName,
        string mobileNo,
        string serviceName,
        decimal fees = 0,
        CancellationToken ct = default)
    {
        var trackingUrl = $"{GetPortalBaseUrl()}?track={Uri.EscapeDataString(applicationNo)}";
        var paymentUrl = $"{GetPortalBaseUrl()}?pay={Uri.EscapeDataString(applicationNo)}";

        var placeholders = new Dictionary<string, string>
        {
            { "UserName", citizenName },
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "ApplicationNo", applicationNo },
            { "ServiceName", serviceName },
            { "Amount", fees.ToString("F2") },
            { "TrackingUrl", trackingUrl },
            { "PaymentUrl", paymentUrl }
        };

        string fallbackMsg;
        if (fees > 0)
        {
            fallbackMsg = "Dear {ApplicantName}, your application for {ServiceName} is submitted (App No: {ApplicationNo}). Statutory fee of Rs.{Amount} is pending. Pay fee: {PaymentUrl} or Track: {TrackingUrl} - {CorporationName}";
        }
        else
        {
            fallbackMsg = "Dear {ApplicantName}, your application for {ServiceName} has been received (App No: {ApplicationNo}). Track status: {TrackingUrl} - {CorporationName}";
        }

        await SendDynamicSmsAsync("RTS_APP_SUBMITTED", "RTS Application Submitted", fallbackMsg, "1707175319753583566", mobileNo, applicationId, placeholders, ct);
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
        var paymentUrl = $"{GetPortalBaseUrl()}?pay={Uri.EscapeDataString(applicationNo)}";
        var placeholders = new Dictionary<string, string>
        {
            { "UserName", citizenName },
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "ApplicationNo", applicationNo },
            { "ServiceName", serviceName },
            { "Amount", amount.ToString("F2") },
            { "PaymentUrl", paymentUrl },
            { "TrackingUrl", paymentUrl }
        };

        var fallbackMsg = "Dear {ApplicantName}, government statutory fee of Rs.{Amount} is pending for RTS Application No: {ApplicationNo} ({ServiceName}). Pay now: {PaymentUrl} - {CorporationName}";
        await SendDynamicSmsAsync("RTS_PAYMENT_PENDING", "RTS Payment Pending", fallbackMsg, "1707175319753583567", mobileNo, applicationId, placeholders, ct);
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
        var receiptUrl = $"{GetPortalBaseUrl()}?receipt={Uri.EscapeDataString(receiptNo)}";
        var placeholders = new Dictionary<string, string>
        {
            { "UserName", citizenName },
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "ApplicationNo", applicationNo },
            { "Amount", amount.ToString("F2") },
            { "ReceiptNo", receiptNo },
            { "ReceiptUrl", receiptUrl },
            { "TrackingUrl", receiptUrl }
        };

        var fallbackMsg = "Dear {ApplicantName}, payment of Rs.{Amount} for RTS Application No: {ApplicationNo} is successful. Official e-Receipt No: {ReceiptNo}. Download receipt: {ReceiptUrl} - {CorporationName}";
        await SendDynamicSmsAsync("RTS_FEE_PAID", "Online Fee Paid", fallbackMsg, "1707175319753583568", mobileNo, applicationId, placeholders, ct);
    }

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
        var trackingUrl = $"{GetPortalBaseUrl()}?track={Uri.EscapeDataString(applicationNo)}";
        var placeholders = new Dictionary<string, string>
        {
            { "UserName", citizenName },
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "ApplicationNo", applicationNo },
            { "ServiceName", serviceName },
            { "StageName", stageName },
            { "Status", status },
            { "Remark", remark ?? "In Progress" },
            { "TrackingUrl", trackingUrl }
        };

        var fallbackMsg = "Dear {ApplicantName}, your RTS Application No: {ApplicationNo} for {ServiceName} is currently at stage '{StageName}'. Status: {Status}. Track: {TrackingUrl} - {CorporationName}";
        await SendDynamicSmsAsync("RTS_STAGE_ADVANCED", "RTS Stage Advanced", fallbackMsg, "1707175319753583569", mobileNo, applicationId, placeholders, ct);
    }

    public async Task SendApplicationApprovedAsync(
        int applicationId,
        string applicationNo,
        string citizenName,
        string mobileNo,
        string serviceName,
        CancellationToken ct = default)
    {
        var certUrl = $"{GetPortalBaseUrl()}?cert={Uri.EscapeDataString(applicationNo)}";
        var placeholders = new Dictionary<string, string>
        {
            { "UserName", citizenName },
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "ApplicationNo", applicationNo },
            { "ServiceName", serviceName },
            { "CertificateUrl", certUrl },
            { "TrackingUrl", certUrl }
        };

        var fallbackMsg = "Dear {ApplicantName}, your RTS Application No: {ApplicationNo} for {ServiceName} has been APPROVED. Download your official certificate/order at: {CertificateUrl} - {CorporationName}";
        await SendDynamicSmsAsync("RTS_APP_APPROVED", "RTS Application Approved", fallbackMsg, "1707175319753583570", mobileNo, applicationId, placeholders, ct);
    }

    public async Task SendApplicationRejectedAsync(
        int applicationId,
        string applicationNo,
        string citizenName,
        string mobileNo,
        string serviceName,
        string? remark = null,
        CancellationToken ct = default)
    {
        var trackingUrl = $"{GetPortalBaseUrl()}?track={Uri.EscapeDataString(applicationNo)}";
        var placeholders = new Dictionary<string, string>
        {
            { "UserName", citizenName },
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "ApplicationNo", applicationNo },
            { "ServiceName", serviceName },
            { "Remark", string.IsNullOrWhiteSpace(remark) ? "Criteria not fulfilled" : remark },
            { "TrackingUrl", trackingUrl }
        };

        var fallbackMsg = "Dear {ApplicantName}, your RTS Application No: {ApplicationNo} for {ServiceName} could not be approved. Reason: {Remark}. View details: {TrackingUrl} - {CorporationName}";
        await SendDynamicSmsAsync("RTS_APP_REJECTED", "RTS Application Rejected", fallbackMsg, "1707175319753583571", mobileNo, applicationId, placeholders, ct);
    }

    public async Task SendApplicationRevertedAsync(
        int applicationId,
        string applicationNo,
        string citizenName,
        string mobileNo,
        string serviceName,
        string? remark = null,
        CancellationToken ct = default)
    {
        var trackingUrl = $"{GetPortalBaseUrl()}?track={Uri.EscapeDataString(applicationNo)}";
        var placeholders = new Dictionary<string, string>
        {
            { "UserName", citizenName },
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "ApplicationNo", applicationNo },
            { "ServiceName", serviceName },
            { "Remark", string.IsNullOrWhiteSpace(remark) ? "Correction required" : remark },
            { "TrackingUrl", trackingUrl }
        };

        var fallbackMsg = "Dear {ApplicantName}, correction/additional document is required for RTS Application No: {ApplicationNo} ({ServiceName}). Officer Remark: {Remark}. Update at: {TrackingUrl} - {CorporationName}";
        await SendDynamicSmsAsync("RTS_APP_REVERTED", "RTS Application Reverted", fallbackMsg, "1707175319753583572", mobileNo, applicationId, placeholders, ct);
    }

    public async Task SendGrievanceRegisteredAsync(
        int applicationId,
        string applicationNo,
        string grievanceNo,
        string citizenName,
        string mobileNo,
        string serviceName,
        CancellationToken ct = default)
    {
        var trackingUrl = $"{GetPortalBaseUrl()}?appeal={Uri.EscapeDataString(grievanceNo)}";
        var placeholders = new Dictionary<string, string>
        {
            { "UserName", citizenName },
            { "CitizenName", citizenName },
            { "ApplicantName", citizenName },
            { "ApplicationNo", applicationNo },
            { "GrievanceNo", grievanceNo },
            { "ServiceName", serviceName },
            { "TrackingUrl", trackingUrl }
        };

        var fallbackMsg = "Dear {ApplicantName}, your RTS Appeal/Grievance for Application No: {ApplicationNo} ({ServiceName}) has been registered with Token No: {GrievanceNo}. Track at: {TrackingUrl} - {CorporationName}";
        await SendDynamicSmsAsync("RTS_GRIEVANCE_REGISTERED", "RTS Grievance Registered", fallbackMsg, "1707175319753583573", mobileNo, applicationId, placeholders, ct);
    }
}
