namespace NtisPlatform.Application.DTOs.Email;

/// <summary>
/// Request model for sending emails
/// </summary>
public class EmailRequest
{
    /// <summary>
    /// Recipient email address (required)
    /// </summary>
    public string ToEmail { get; set; } = null!;

    /// <summary>
    /// Recipient display name (optional)
    /// </summary>
    public string? ToName { get; set; }

    /// <summary>
    /// Email subject line (required)
    /// </summary>
    public string Subject { get; set; } = null!;

    /// <summary>
    /// Email body content (required)
    /// </summary>
    public string Body { get; set; } = null!;

    /// <summary>
    /// Whether the body contains HTML markup
    /// </summary>
    public bool IsHtml { get; set; } = true;

    /// <summary>
    /// CC (carbon copy) recipients
    /// </summary>
    public List<string> Cc { get; set; } = new();

    /// <summary>
    /// BCC (blind carbon copy) recipients
    /// </summary>
    public List<string> Bcc { get; set; } = new();
}
