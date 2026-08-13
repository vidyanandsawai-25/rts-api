using NtisPlatform.Application.DTOs.Sms;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service for sending SMS messages.
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends an SMS asynchronously
    /// </summary>
    /// <param name="request">SMS request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SendSmsAsync(SmsRequest request, CancellationToken cancellationToken = default);
}
