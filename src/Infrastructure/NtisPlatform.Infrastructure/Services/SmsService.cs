using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Sms;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Placeholder SMS service — no SMS gateway is configured yet. Logs the message instead of
/// sending it so the OTP flows can be fully wired and tested end-to-end before a real provider
/// (e.g. Twilio, MSG91) is selected and plugged in here.
/// </summary>
public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;

    public SmsService(ILogger<SmsService> logger)
    {
        _logger = logger;
    }

    public Task SendSmsAsync(SmsRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new ArgumentException("PhoneNumber is required", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Message is required", nameof(request));

        _logger.LogWarning(
            "SMS gateway not configured — logging instead of sending. To: {PhoneNumber}, Message: {Message}",
            request.PhoneNumber, request.Message);

        return Task.CompletedTask;
    }
}
