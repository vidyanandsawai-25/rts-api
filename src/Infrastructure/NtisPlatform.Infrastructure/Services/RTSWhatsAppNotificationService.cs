using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
                SMSTypeID = null,
                Message = messagePreview,
                SmsUrl = _options.Value.WabaUrl,
                SMSStatus = status,
                GatewayResponse = gatewayResponse,
                ApplicationId = applicationId,
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
        CancellationToken ct)
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

            requestMessage.Headers.Add("key", config.ApiKey);
            requestMessage.Headers.Add("wabaNumber", config.WabaNumber);

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            _logger.LogInformation("Dispatching WhatsApp notification via template '{Template}' to {Mobile}...", templateName, sanitizedMobile);

            var response = await client.SendAsync(requestMessage, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);

            var status = response.IsSuccessStatusCode ? "WHATSAPP_SENT" : "WHATSAPP_FAILED";

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp message successfully dispatched to {Mobile}. Response: {Response}", sanitizedMobile, responseContent);
            }
            else
            {
                _logger.LogError("WhatsApp gateway returned HTTP {StatusCode} for {Mobile}. Response: {Response}", response.StatusCode, sanitizedMobile, responseContent);
            }

            await LogWhatsAppOutboxAsync(sanitizedMobile, templateName, messagePreview, status, responseContent, applicationId, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch WhatsApp message via template '{Template}' to {Mobile}", templateName, recipientMobile);
            await LogWhatsAppOutboxAsync(sanitizedMobile, templateName, messagePreview, "WHATSAPP_ERROR", ex.Message, applicationId, ct);
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
        CancellationToken ct = default)
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
                language = new { code = "en" },
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

        var preview = $"Application No: {applicationNo}, Service: {cleanServiceName}, Status: {cleanStatus}";
        await SendWabaPayloadAsync(mobileNo, payload, templateName, preview, null, ct);
    }

    public async Task SendPaymentReceiptAsync(
        string mobileNo,
        string citizenName,
        decimal amount,
        string applicationNo,
        string receiptNo,
        string receiptUrl,
        CancellationToken ct = default)
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
                language = new { code = "en" },
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

        var preview = $"Payment of Rs. {formattedAmount} for {applicationNo}. Receipt: {receiptNo}";
        await SendWabaPayloadAsync(mobileNo, payload, templateName, preview, null, ct);
    }

    public async Task SendCertificateIssuedAsync(
        string mobileNo,
        string citizenName,
        string applicationNo,
        string serviceName,
        string certificateUrl,
        CancellationToken ct = default)
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
                language = new { code = "en" },
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

        var preview = $"Certificate issued for {applicationNo} ({cleanServiceName})";
        await SendWabaPayloadAsync(mobileNo, payload, templateName, preview, null, ct);
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
                language = new { code = "en" },
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
}
