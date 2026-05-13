using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using NtisPlatform.Application.DTOs.Email;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Email service implementation using MailKit
/// </summary>
public class EmailService : IEmailService
{
    private readonly IEmailSettingsProvider _settingsProvider;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IEmailSettingsProvider settingsProvider,
        ILogger<EmailService> logger)
    {
        _settingsProvider = settingsProvider;
        _logger = logger;
    }

    public async Task SendEmailAsync(EmailRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.ToEmail))
            throw new ArgumentException("ToEmail is required", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new ArgumentException("Subject is required", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Body))
            throw new ArgumentException("Body is required", nameof(request));

        // Retrieve email settings from config tables
        var settings = await _settingsProvider.GetEmailSettingsAsync(cancellationToken);

        // Build MIME message
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
        message.To.Add(new MailboxAddress(request.ToName ?? request.ToEmail, request.ToEmail));

        // Add CC recipients
        if (request.Cc != null && request.Cc.Any())
        {
            foreach (var cc in request.Cc.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                message.Cc.Add(MailboxAddress.Parse(cc));
            }
        }

        // Add BCC recipients
        if (request.Bcc != null && request.Bcc.Any())
        {
            foreach (var bcc in request.Bcc.Where(b => !string.IsNullOrWhiteSpace(b)))
            {
                message.Bcc.Add(MailboxAddress.Parse(bcc));
            }
        }

        message.Subject = request.Subject;

        // Build body
        var bodyBuilder = new BodyBuilder();
        if (request.IsHtml)
        {
            bodyBuilder.HtmlBody = request.Body;
        }
        else
        {
            bodyBuilder.TextBody = request.Body;
        }

        message.Body = bodyBuilder.ToMessageBody();

        // Send email
        using var smtpClient = new SmtpClient();
        try
        {
            _logger.LogInformation("Connecting to SMTP server {SmtpHost}:{SmtpPort}", settings.SmtpHost, settings.SmtpPort);

            // Parse secure socket options from settings
            var secureSocketOptions = ParseSecureSocketOptions(settings.SecureSocketOptions);
            _logger.LogDebug("Using secure socket option: {SecureSocketOptions}", secureSocketOptions);

            // Connect to SMTP server
            await smtpClient.ConnectAsync(
                settings.SmtpHost,
                settings.SmtpPort,
                secureSocketOptions,
                cancellationToken);

            // Authenticate
            await smtpClient.AuthenticateAsync(settings.SmtpUserName, settings.SmtpPassword, cancellationToken);

            // Send the email
            await smtpClient.SendAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {ToEmail}", request.ToEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", request.ToEmail);
            throw new InvalidOperationException($"Failed to send email to {request.ToEmail}", ex);
        }
        finally
        {
            // Disconnect from SMTP server
            if (smtpClient.IsConnected)
            {
                await smtpClient.DisconnectAsync(true, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Parse secure socket options from config string value
    /// </summary>
    private SecureSocketOptions ParseSecureSocketOptions(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _logger.LogWarning("SecureSocketOptions not specified, using Auto");
            return SecureSocketOptions.Auto;
        }

        // Try to parse the enum value (case-insensitive)
        if (Enum.TryParse<SecureSocketOptions>(value, ignoreCase: true, out var result))
        {
            return result;
        }

        // Fallback for legacy boolean values
        if (bool.TryParse(value, out var boolValue))
        {
            _logger.LogWarning("Using legacy boolean UseSsl value, consider migrating to explicit SecureSocketOptions");
            return boolValue ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        }

        _logger.LogWarning("Invalid SecureSocketOptions value '{Value}', defaulting to Auto", value);
        return SecureSocketOptions.Auto;
    }
}
