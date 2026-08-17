using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Sms;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Enterprise DLT-compliant SMS Service backed dynamically by CORE.SMSGatewayMaster, CORE.SmsGatewayDetails, and appsettings/environment overrides.
/// </summary>
public class SmsService : ISmsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsService> _logger;

    public SmsService(
        ApplicationDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SmsService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendSmsAsync(SmsRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            _logger.LogWarning("SMS dispatch skipped: PhoneNumber is empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            _logger.LogWarning("SMS dispatch skipped: Message is empty for recipient {PhoneNumber}.", request.PhoneNumber);
            return;
        }

        // 1. Sanitize phone number (strip +91, non-digits)
        var sanitizedMobile = new string(request.PhoneNumber.Where(char.IsDigit).ToArray());
        if (sanitizedMobile.Length > 10 && sanitizedMobile.StartsWith("91"))
        {
            sanitizedMobile = sanitizedMobile.Substring(2);
        }

        // 2. Fetch Active Gateway Configuration from Database
        var activeGateway = await _dbContext.SMSGatewayMasters
            .Include(g => g.GatewayDetails)
            .Where(g => g.IsActive)
            .OrderByDescending(g => g.SMSGatewayMasterID)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeGateway == null || !activeGateway.GatewayDetails.Any())
        {
            _logger.LogWarning("No active SMS Gateway configured in CORE.SMSGatewayMaster. Mock logging SMS to: {Mobile}, Message: {Message}",
                sanitizedMobile, request.Message);

            await LogSmsOutboxAsync(sanitizedMobile, request, "SMS gateway not configured in DB — logged only", "LOGGED", null, cancellationToken);
            return;
        }

        // 3. Build Dynamic Gateway URL & Parameters
        var details = activeGateway.GatewayDetails
            .Where(d => d.IsActive)
            .OrderBy(d => d.SequenceNo ?? d.SMSGatewayDetailsID)
            .ToList();

        var baseUrlItem = details.FirstOrDefault(d => d.IsURL || d.PropertyName.Equals("BaseURL", StringComparison.OrdinalIgnoreCase));
        var baseUrl = _configuration["AppSettings:SmsGateway:BaseUrl"]
                   ?? _configuration["SMS_BASE_URL"]
                   ?? baseUrlItem?.Value?.Trim();

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("SMS Gateway BaseURL is not configured. Skipping live dispatch.");
            await LogSmsOutboxAsync(sanitizedMobile, request, "Missing BaseURL in Gateway config", "FAILED", null, cancellationToken);
            return;
        }

        var queryParams = new List<string>();
        string? senderName = _configuration["AppSettings:SmsGateway:SenderId"] ?? _configuration["SMS_SENDER_ID"];

        foreach (var prop in details)
        {
            if (prop.IsURL || prop.PropertyName.Equals("BaseURL", StringComparison.OrdinalIgnoreCase))
                continue;

            var propName = prop.PropertyName.Trim().TrimStart('&');

            // Dynamic environment / config overrides if provided
            var configuredPropValue = _configuration[$"AppSettings:SmsGateway:{propName}"]
                                   ?? _configuration[$"SMS_{propName.ToUpperInvariant()}"]
                                   ?? prop.Value;

            if (prop.IsMobile || propName.Equals("mobiles", StringComparison.OrdinalIgnoreCase) || propName.Equals("mobile", StringComparison.OrdinalIgnoreCase))
            {
                queryParams.Add($"{propName}={HttpUtility.UrlEncode(sanitizedMobile)}");
            }
            else if (prop.IsMessage || propName.Equals("sms", StringComparison.OrdinalIgnoreCase) || propName.Equals("message", StringComparison.OrdinalIgnoreCase))
            {
                queryParams.Add($"{propName}={HttpUtility.UrlEncode(request.Message)}");
            }
            else if (prop.IsTemplateID || propName.Equals("templateid", StringComparison.OrdinalIgnoreCase) || propName.Equals("tempid", StringComparison.OrdinalIgnoreCase))
            {
                var templateId = !string.IsNullOrWhiteSpace(request.TemplateId) ? request.TemplateId : configuredPropValue;
                if (!string.IsNullOrWhiteSpace(templateId))
                {
                    queryParams.Add($"{propName}={HttpUtility.UrlEncode(templateId)}");
                }
            }
            else
            {
                if (propName.Equals("senderid", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(senderName))
                {
                    senderName = configuredPropValue;
                }
                queryParams.Add($"{propName}={HttpUtility.UrlEncode(configuredPropValue ?? string.Empty)}");
            }
        }

        var fullUrl = baseUrl.Contains('?')
            ? $"{baseUrl}&{string.Join("&", queryParams)}"
            : $"{baseUrl}?{string.Join("&", queryParams)}";

        // 4. Send HTTP Request
        string? gatewayResponse = null;
        string smsStatus = "PENDING";

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            _logger.LogInformation("Dispatching live SMS via {Provider} to {Mobile}...", activeGateway.ProviderName, sanitizedMobile);

            var response = await httpClient.GetAsync(fullUrl, cancellationToken);
            gatewayResponse = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                smsStatus = "SENT";
                _logger.LogInformation("SMS dispatched successfully to {Mobile}. Response: {Response}", sanitizedMobile, gatewayResponse);
            }
            else
            {
                smsStatus = "FAILED";
                _logger.LogWarning("SMS gateway returned non-success code {StatusCode} for {Mobile}. Response: {Response}",
                    response.StatusCode, sanitizedMobile, gatewayResponse);
            }
        }
        catch (Exception ex)
        {
            smsStatus = "FAILED";
            gatewayResponse = $"Exception: {ex.Message}";
            _logger.LogError(ex, "Failed to send SMS to {Mobile} via gateway URL: {Url}", sanitizedMobile, fullUrl);
        }

        // 5. Persist Outbox Audit Log
        await LogSmsOutboxAsync(sanitizedMobile, request, gatewayResponse, smsStatus, senderName, cancellationToken);
    }

    private async Task LogSmsOutboxAsync(
        string mobileNo,
        SmsRequest request,
        string? gatewayResponse,
        string status,
        string? senderName,
        CancellationToken cancellationToken)
    {
        try
        {
            var logEntry = new SMSSendDetailsEntity
            {
                ReceiverMobileNo = mobileNo,
                SenderName = senderName ?? "DMCDTX",
                TemplateID = request.TemplateId,
                SMSTypeID = request.SMSTypeID,
                Message = request.Message,
                SmsUrl = null,
                SMSStatus = status,
                GatewayResponse = gatewayResponse,
                ApplicationId = request.ApplicationId,
                CreatedBy = 1,
                CreatedDate = DateTime.UtcNow
            };

            await _dbContext.SMSSendDetails.AddAsync(logEntry, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save SMSSendDetails log for recipient {Mobile}", mobileNo);
        }
    }
}
