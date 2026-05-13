namespace NtisPlatform.Application.DTOs.Email;

/// <summary>
/// Email SMTP configuration settings retrieved from config tables
/// </summary>
public class EmailSettingsDto
{
    /// <summary>
    /// SMTP server hostname (e.g., smtp.gmail.com)
    /// </summary>
    public string SmtpHost { get; set; } = null!;

    /// <summary>
    /// SMTP server port (e.g., 587 for TLS)
    /// </summary>
    public int SmtpPort { get; set; }

    /// <summary>
    /// SMTP authentication username
    /// </summary>
    public string SmtpUserName { get; set; } = null!;

    /// <summary>
    /// SMTP authentication password
    /// </summary>
    public string SmtpPassword { get; set; } = null!;

    /// <summary>
    /// Sender email address
    /// </summary>
    public string FromEmail { get; set; } = null!;

    /// <summary>
    /// Sender display name
    /// </summary>
    public string FromName { get; set; } = null!;

    /// <summary>
    /// SMTP secure socket options: None, Auto, SslOnConnect, StartTls, StartTlsWhenAvailable
    /// Default: Auto
    /// </summary>
    public string SecureSocketOptions { get; set; } = "Auto";

    /// <summary>
    /// Application login URL for email templates
    /// </summary>
    public string? LoginUrl { get; set; }
}
