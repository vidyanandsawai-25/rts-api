using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Enterprise WhatsApp Business API (WABA) notification service following the standard NtisPlatform architecture pattern.
/// Uses strongly-typed WhatsAppOptions, resilient IHttpClientFactory calls, and database audit logging via CORE.SMSSendDetails.
/// </summary>
public class RTSWhatsAppNotificationService : IRTSWhatsAppNotificationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<WhatsAppOptions> _options;
    private readonly ILogger<RTSWhatsAppNotificationService> _logger;

    public RTSWhatsAppNotificationService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IOptions<WhatsAppOptions> options,
        ILogger<RTSWhatsAppNotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    private static string ResolveTemplateLanguage(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName)) return "en_US";

        // rmndr_rts and legacy amc templates are registered with 'en' language code in Meta WACRM
        if (string.Equals(templateName, "rmndr_rts", StringComparison.OrdinalIgnoreCase) ||
            templateName.StartsWith("amc_", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        // All official RTS utility templates (rts_*) are registered as en_US in Meta WABA
        return "en_US";
    }

    /// <summary>
    /// Normalizes mobile number to Indian 91XXXXXXXXXX format.
    /// </summary>
    private static string? SanitizeMobileNumber(string? mobileNo)
    {
        if (string.IsNullOrWhiteSpace(mobileNo)) return null;

        var digits = new string(mobileNo.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits)) return null;

        if (digits.Length == 10)
        {
            return $"91{digits}";
        }

        if (digits.Length == 11 && digits.StartsWith("0"))
        {
            return $"91{digits.Substring(1)}";
        }

        if (digits.Length == 12 && digits.StartsWith("91"))
        {
            return digits;
        }

        return digits.Length >= 10 ? digits : null;
    }

    private async Task<string> GetPortalBaseUrlAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ulb = await dbContext.ULBMasters.AsNoTracking().FirstOrDefaultAsync(u => u.IsActive, ct);
            if (ulb != null && !string.IsNullOrWhiteSpace(ulb.WebsiteUrl))
            {
                var cleanUrl = ulb.WebsiteUrl.Trim().TrimEnd('/');
                if (!cleanUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !cleanUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    cleanUrl = $"https://{cleanUrl}";
                }
                return cleanUrl;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve portal base URL from database ULBMaster.");
        }

        return "https://rts.amcakola.in";
    }

    /// <summary>
    /// Records outgoing WhatsApp notification in the central message audit trail (CORE.SMSSendDetails).
    /// Follows the exact audit pattern established in SmsService.LogSmsOutboxAsync.
    /// </summary>
    private async Task LogWhatsAppOutboxAsync(
        string mobileNo,
        string templateName,
        string messagePreview,
        string status,
        string? gatewayResponse,
        int? applicationId,
        int? smstypeId,
        int? createdBy,
        CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var logEntry = new SMSSendDetailsEntity
            {
                ReceiverMobileNo = mobileNo,
                SenderName = "WHATSAPP",
                TemplateID = templateName,
                SMSTypeID = smstypeId,
                Message = messagePreview,
                SmsUrl = _options.Value.WabaUrl,
                SMSStatus = status,
                GatewayResponse = gatewayResponse,
                ApplicationId = applicationId,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow
            };

            await dbContext.SMSSendDetails.AddAsync(logEntry, ct);
            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist WhatsApp audit log in SMSSendDetails for recipient {Mobile}", mobileNo);
        }
    }

    private async Task<bool> SendWabaPayloadAsync(
        string recipientMobile,
        object payload,
        string templateName,
        string messagePreview,
        int? applicationId,
        CancellationToken ct,
        int? smstypeId = null,
        int? createdBy = null)
    {
        var config = _options.Value;
        if (!config.IsActive)
        {
            _logger.LogInformation("WhatsApp notifications are disabled (WhatsApp:IsActive=false). Skipping dispatch.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.WabaUrl) ||
            string.IsNullOrWhiteSpace(config.ApiKey) ||
            string.IsNullOrWhiteSpace(config.WabaNumber))
        {
            _logger.LogWarning("WhatsApp configuration is incomplete (WabaUrl, ApiKey, or WabaNumber is missing in configuration). Skipping dispatch.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(templateName))
        {
            _logger.LogWarning("WhatsApp template name is not configured for the requested notification type. Skipping dispatch.");
            return false;
        }

        var sanitizedMobile = SanitizeMobileNumber(recipientMobile);
        if (string.IsNullOrWhiteSpace(sanitizedMobile))
        {
            _logger.LogWarning("Invalid mobile number '{Mobile}' for WhatsApp dispatch. Skipping.", recipientMobile);
            return false;
        }

        try
        {
            var jsonBody = JsonSerializer.Serialize(payload);
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, config.WabaUrl)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(config.ApiKey))
            {
                var token = config.ApiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? config.ApiKey.Substring(7).Trim()
                    : config.ApiKey.Trim();

                requestMessage.Headers.Add("Authorization", $"Bearer {token}");
                requestMessage.Headers.Add("key", token);
            }

            if (!string.IsNullOrWhiteSpace(config.WabaNumber))
            {
                requestMessage.Headers.Add("wabaNumber", config.WabaNumber);
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            _logger.LogInformation("Dispatching WhatsApp notification via template '{Template}' to {Mobile}...", templateName, sanitizedMobile);

            var response = await client.SendAsync(requestMessage, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);

            // Fallback: If template dispatch fails (e.g. Meta error #132001 template does not exist), automatically fall back to direct WhatsApp text
            if (!response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(messagePreview))
            {
                _logger.LogWarning("WhatsApp template '{Template}' dispatch failed (HTTP {Status}: {Error}). Falling back to direct WhatsApp text message for {Mobile}...",
                    templateName, response.StatusCode, responseContent, sanitizedMobile);

                try
                {
                    var fallbackPayload = new
                    {
                        number = sanitizedMobile,
                        type = "text",
                        message = messagePreview,
                        content_text = messagePreview
                    };

                    using var fallbackRequest = new HttpRequestMessage(HttpMethod.Post, config.WabaUrl)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(fallbackPayload), Encoding.UTF8, "application/json")
                    };

                    if (!string.IsNullOrWhiteSpace(config.ApiKey))
                    {
                        var token = config.ApiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                            ? config.ApiKey.Substring(7).Trim()
                            : config.ApiKey.Trim();

                        fallbackRequest.Headers.Add("Authorization", $"Bearer {token}");
                        fallbackRequest.Headers.Add("key", token);
                    }

                    if (!string.IsNullOrWhiteSpace(config.WabaNumber))
                    {
                        fallbackRequest.Headers.Add("wabaNumber", config.WabaNumber);
                    }

                    var fallbackResponse = await client.SendAsync(fallbackRequest, ct);
                    var fallbackContent = await fallbackResponse.Content.ReadAsStringAsync(ct);

                    if (fallbackResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("WhatsApp direct text fallback successfully delivered to {Mobile}. Response: {Response}", sanitizedMobile, fallbackContent);
                        await LogWhatsAppOutboxAsync(sanitizedMobile, templateName, messagePreview, "WHATSAPP_SENT", fallbackContent, applicationId, smstypeId, createdBy, ct);
                        return true;
                    }
                    else
                    {
                        _logger.LogError("WhatsApp text fallback also returned HTTP {StatusCode} for {Mobile}. Response: {Response}", fallbackResponse.StatusCode, sanitizedMobile, fallbackContent);
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogWarning(fallbackEx, "WhatsApp text fallback exception for {Mobile}", sanitizedMobile);
                }
            }

            var status = response.IsSuccessStatusCode ? "WHATSAPP_SENT" : "WHATSAPP_FAILED";

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp message successfully dispatched to {Mobile}. Response: {Response}", sanitizedMobile, responseContent);
            }
            else
            {
                _logger.LogError("WhatsApp gateway returned HTTP {StatusCode} for {Mobile}. Response: {Response}", response.StatusCode, sanitizedMobile, responseContent);
            }

            await LogWhatsAppOutboxAsync(sanitizedMobile, templateName, messagePreview, status, responseContent, applicationId, smstypeId, createdBy, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch WhatsApp message via template '{Template}' to {Mobile}", templateName, recipientMobile);
            await LogWhatsAppOutboxAsync(sanitizedMobile, templateName, messagePreview, "WHATSAPP_ERROR", ex.Message, applicationId, smstypeId, createdBy, ct);
            return false;
        }
    }

    public async Task SendApplicationStatusAsync(
        string mobileNo,
        string citizenName,
        string applicationNo,
        string serviceName,
        string status,
        string trackingUrl,
        CancellationToken ct = default,
        int? applicationId = null)
    {
        if (!_options.Value.SendStatusOnWhatsApp) return;

        var templateName = _options.Value.Templates.ApplicationStatus;
        var cleanCitizenName = string.IsNullOrWhiteSpace(citizenName) ? "Citizen" : citizenName.Trim();
        var cleanServiceName = string.IsNullOrWhiteSpace(serviceName) ? "RTS Service" : serviceName.Trim();
        var cleanStatus = string.IsNullOrWhiteSpace(status) ? "SUBMITTED" : status.Trim();

        var payload = new
        {
            to = SanitizeMobileNumber(mobileNo),
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = ResolveTemplateLanguage(templateName) },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[]
                        {
                            new { type = "text", text = cleanCitizenName },
                            new { type = "text", text = applicationNo },
                            new { type = "text", text = cleanServiceName },
                            new { type = "text", text = cleanStatus },
                            new { type = "text", text = trackingUrl }
                        }
                    }
                }
            }
        };

        var preview = $"🏛️ *अकोला महानगरपालिका (RTS)*\n\nप्रिय *{cleanCitizenName}*,\nआपल्या अर्ज क्र. *{applicationNo}* ({cleanServiceName}) ची सद्यस्थिती: *{cleanStatus}*.\n\n🔍 अर्जाची सद्यस्थिती तपासण्यासाठी खालील लिंकवर क्लिक करा:\n{trackingUrl}";
        await SendWabaPayloadAsync(mobileNo, payload, templateName, preview, applicationId, ct);
    }

    public async Task SendPaymentReceiptAsync(
        string mobileNo,
        string citizenName,
        decimal amount,
        string applicationNo,
        string receiptNo,
        string receiptUrl,
        CancellationToken ct = default,
        int? applicationId = null)
    {
        var templateName = _options.Value.Templates.PaymentReceipt;
        var cleanCitizenName = string.IsNullOrWhiteSpace(citizenName) ? "Citizen" : citizenName.Trim();
        var formattedAmount = amount.ToString("F2");

        var payload = new
        {
            to = SanitizeMobileNumber(mobileNo),
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = ResolveTemplateLanguage(templateName) },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[]
                        {
                            new { type = "text", text = cleanCitizenName },
                            new { type = "text", text = formattedAmount },
                            new { type = "text", text = applicationNo },
                            new { type = "text", text = receiptNo },
                            new { type = "text", text = receiptUrl }
                        }
                    }
                }
            }
        };

        var preview = $"🏛️ *अकोला महानगरपालिका (RTS)*\n\nप्रिय *{cleanCitizenName}*,\nआपल्या अर्ज क्र. *{applicationNo}* साठी रु. *{formattedAmount}* चे शुल्क यशस्वीरीत्या जमा झाले आहे.\nपावती क्र: *{receiptNo}*\n\n🔗 अधिकृत पावती पाहण्यासाठी:\n{receiptUrl}";
        await SendWabaPayloadAsync(mobileNo, payload, templateName, preview, applicationId, ct);
    }

    public async Task SendCertificateIssuedAsync(
        string mobileNo,
        string citizenName,
        string applicationNo,
        string serviceName,
        string certificateUrl,
        CancellationToken ct = default,
        int? applicationId = null)
    {
        var templateName = _options.Value.Templates.CertificateIssued;
        var cleanCitizenName = string.IsNullOrWhiteSpace(citizenName) ? "Citizen" : citizenName.Trim();
        var cleanServiceName = string.IsNullOrWhiteSpace(serviceName) ? "RTS Service" : serviceName.Trim();

        var payload = new
        {
            to = SanitizeMobileNumber(mobileNo),
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = ResolveTemplateLanguage(templateName) },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[]
                        {
                            new { type = "text", text = cleanCitizenName },
                            new { type = "text", text = applicationNo },
                            new { type = "text", text = cleanServiceName },
                            new { type = "text", text = certificateUrl }
                        }
                    }
                }
            }
        };

        var preview = $"🏛️ *अकोला महानगरपालिका (RTS)*\n\nअभिनंदन *{cleanCitizenName}*!\nआपल्या अर्ज क्र. *{applicationNo}* ({cleanServiceName}) चा डिजिटल स्वाक्षरी असलेला अधिकृत दाखला मंजूर झाला आहे.\n\n📜 दाखला डाऊनलोड करण्यासाठी खालील लिंकवर क्लिक करा:\n{certificateUrl}";
        await SendWabaPayloadAsync(mobileNo, payload, templateName, preview, applicationId, ct);
    }

    public async Task SendCitizenOtpAsync(
        string mobileNo,
        string otp,
        CancellationToken ct = default)
    {
        if (!_options.Value.SendOtpOnWhatsApp) return;

        var templateName = _options.Value.Templates.CitizenOtp;

        var payload = new
        {
            to = SanitizeMobileNumber(mobileNo),
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = ResolveTemplateLanguage(templateName) },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[]
                        {
                            new { type = "text", text = otp }
                        }
                    },
                    new
                    {
                        type = "button",
                        sub_type = "url",
                        index = 0,
                        parameters = new object[]
                        {
                            new { type = "text", text = otp }
                        }
                    }
                }
            }
        };

        var preview = $"Login OTP Verification Code";
        await SendWabaPayloadAsync(mobileNo, payload, templateName, preview, null, ct);
    }

    public async Task SendOfficerTaskAssignedAsync(
        string mobileNo,
        string officerName,
        string applicationNo,
        string serviceName,
        string stageName,
        string dueDate,
        string portalUrl,
        int? applicationId = null,
        int? stageId = null,
        int? officerUserId = null,
        CancellationToken ct = default)
    {
        if (!_options.Value.SendOfficerAlertsOnWhatsApp) return;

        var templateName = _options.Value.Templates.OfficerAssigned;
        var cleanOfficerName = string.IsNullOrWhiteSpace(officerName) ? "Officer" : officerName.Trim();
        var cleanServiceName = string.IsNullOrWhiteSpace(serviceName) ? "RTS Service" : serviceName.Trim();
        var cleanStageName = string.IsNullOrWhiteSpace(stageName) ? "Review / Verification" : stageName.Trim();
        var cleanDueDate = string.IsNullOrWhiteSpace(dueDate) ? DateTime.UtcNow.AddDays(7).ToString("dd-MM-yyyy") : dueDate.Trim();
        var cleanPortalUrl = string.IsNullOrWhiteSpace(portalUrl) ? _options.Value.Worker.OfficerPortalBaseUrl : portalUrl.Trim();

        var payload = new
        {
            to = SanitizeMobileNumber(mobileNo),
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = ResolveTemplateLanguage(templateName) },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[]
                        {
                            new { type = "text", text = cleanOfficerName },
                            new { type = "text", text = applicationNo },
                            new { type = "text", text = cleanServiceName },
                            new { type = "text", text = cleanStageName },
                            new { type = "text", text = cleanDueDate },
                            new { type = "text", text = cleanPortalUrl }
                        }
                    }
                }
            }
        };

        var preview = $"🏛️ *अकोला महानगरपालिका (RTS)*\n\nप्रिय अधिकारी *{cleanOfficerName}*,\nअर्ज क्र. *{applicationNo}* ({cleanServiceName}) आपल्याकडे *{cleanStageName}* टप्प्यासाठी वर्ग करण्यात आला आहे.\n⏱️ विहित मुदत: *{cleanDueDate}*\n\n🔗 पोर्टल: {cleanPortalUrl}";
        await SendWabaPayloadAsync(mobileNo, payload, templateName, preview, applicationId, ct, smstypeId: stageId, createdBy: officerUserId);
    }

    public async Task SendOfficerSlaWarningAsync(
        string mobileNo,
        string officerName,
        string applicationNo,
        string serviceName,
        int daysRemaining,
        string dueDate,
        string actionUrl,
        int? applicationId = null,
        int? officerUserId = null,
        CancellationToken ct = default)
    {
        if (!_options.Value.SendOfficerAlertsOnWhatsApp) return;

        var templateName = _options.Value.Templates.OfficerSlaWarning;
        var cleanOfficerName = string.IsNullOrWhiteSpace(officerName) ? "Officer" : officerName.Trim();
        var cleanServiceName = string.IsNullOrWhiteSpace(serviceName) ? "RTS Service" : serviceName.Trim();
        var cleanDueDate = string.IsNullOrWhiteSpace(dueDate) ? DateTime.UtcNow.AddDays(daysRemaining).ToString("dd-MM-yyyy") : dueDate.Trim();
        var cleanActionUrl = string.IsNullOrWhiteSpace(actionUrl) ? _options.Value.Worker.OfficerPortalBaseUrl : actionUrl.Trim();

        var payload = new
        {
            to = SanitizeMobileNumber(mobileNo),
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = ResolveTemplateLanguage(templateName) },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[]
                        {
                            new { type = "text", text = cleanOfficerName },
                            new { type = "text", text = applicationNo },
                            new { type = "text", text = cleanServiceName },
                            new { type = "text", text = Math.Max(1, daysRemaining).ToString() },
                            new { type = "text", text = cleanDueDate },
                            new { type = "text", text = cleanActionUrl }
                        }
                    }
                }
            }
        };

        var preview = $"⚠️ *अकोला महानगरपालिका (RTS) - SLA इशारा*\n\nप्रिय अधिकारी *{cleanOfficerName}*,\nअर्ज क्र. *{applicationNo}* ({cleanServiceName}) ची मुदत संपण्यास *{daysRemaining}* दिवस शिल्लक आहेत.\n⏱️ अंतिम मुदत: *{cleanDueDate}*\n\n🔗 पोर्टल: {cleanActionUrl}";
        await SendWabaPayloadAsync(mobileNo, payload, templateName, preview, applicationId, ct, smstypeId: null, createdBy: officerUserId);
    }

    public async Task SendOfficerDailyDigestAsync(
        string mobileNo,
        string officerName,
        int totalPending,
        int nearSlaCount,
        string dashboardUrl,
        int? officerUserId = null,
        CancellationToken ct = default)
    {
        if (!_options.Value.SendOfficerAlertsOnWhatsApp) return;

        var templateName = _options.Value.Templates.OfficerDailyDigest;
        var cleanOfficerName = string.IsNullOrWhiteSpace(officerName) ? "Officer" : officerName.Trim();
        var cleanDashboardUrl = string.IsNullOrWhiteSpace(dashboardUrl) ? _options.Value.Worker.OfficerPortalBaseUrl : dashboardUrl.Trim();

        var payload = new
        {
            to = SanitizeMobileNumber(mobileNo),
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = ResolveTemplateLanguage(templateName) },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[]
                        {
                            new { type = "text", text = cleanOfficerName },
                            new { type = "text", text = totalPending.ToString() },
                            new { type = "text", text = nearSlaCount.ToString() },
                            new { type = "text", text = cleanDashboardUrl }
                        }
                    }
                }
            }
        };

        var preview = $"🏛️ *अकोला महानगरपालिका (RTS) - दैनिक प्रलंबित आढावा*\n\nप्रिय अधिकारी *{cleanOfficerName}*,\nआपल्याकडे सद्यस्थितीत एकूण *{totalPending}* अर्ज प्रलंबित आहेत.\n⚠️ SLA मुदत संपत आलेले अर्ज: *{nearSlaCount}*\n\n🔗 डॅशबोर्ड: {cleanDashboardUrl}";
        await SendWabaPayloadAsync(mobileNo, payload, templateName, preview, null, ct, smstypeId: null, createdBy: officerUserId);
    }

    public async Task SendCitizenActionReminderAsync(
        string mobileNo,
        string citizenName,
        string applicationNo,
        string serviceName,
        string pendingAction,
        string dueDate,
        string serviceUrl,
        int? applicationId = null,
        CancellationToken ct = default)
    {
        var templateName = _options.Value.Templates.CitizenReminder;
        var cleanCitizenName = string.IsNullOrWhiteSpace(citizenName) ? "Citizen" : citizenName.Trim();
        var cleanServiceName = string.IsNullOrWhiteSpace(serviceName) ? "RTS Service" : serviceName.Trim();
        var cleanPendingAction = string.IsNullOrWhiteSpace(pendingAction) ? "Fee Payment / Document Submission" : pendingAction.Trim();
        var cleanDueDate = string.IsNullOrWhiteSpace(dueDate) ? DateTime.UtcNow.AddDays(7).ToString("dd-MM-yyyy") : dueDate.Trim();
        var portalBaseUrl = await GetPortalBaseUrlAsync(ct);
        var cleanServiceUrl = string.IsNullOrWhiteSpace(serviceUrl) ? $"{portalBaseUrl}/service" : serviceUrl.Trim();

        var payload = new
        {
            to = SanitizeMobileNumber(mobileNo),
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = ResolveTemplateLanguage(templateName) },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[]
                        {
                            new { type = "text", text = cleanCitizenName },
                            new { type = "text", text = applicationNo },
                            new { type = "text", text = cleanServiceName },
                            new { type = "text", text = cleanPendingAction },
                            new { type = "text", text = cleanDueDate },
                            new { type = "text", text = cleanServiceUrl }
                        }
                    }
                }
            }
        };

        var preview = $"Action Reminder: {applicationNo} ({cleanServiceName}) needs {cleanPendingAction}. Due: {cleanDueDate}";
        await SendWabaPayloadAsync(mobileNo, payload, templateName, preview, applicationId, ct);
    }
}
