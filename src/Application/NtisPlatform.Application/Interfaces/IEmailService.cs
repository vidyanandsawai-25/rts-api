using NtisPlatform.Application.DTOs.Email;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously
    /// </summary>
    /// <param name="request">Email request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SendEmailAsync(EmailRequest request, CancellationToken cancellationToken = default);
}
