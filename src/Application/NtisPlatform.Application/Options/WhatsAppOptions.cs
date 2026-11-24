namespace NtisPlatform.Application.Options;

/// <summary>
/// Configuration options for WhatsApp Business API (WABA) notifications following standard platform pattern.
/// Bound from the "WhatsApp" or "WhatsAppSettings" section of configuration.
/// </summary>
public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    public bool IsActive { get; set; } = true;

    /// <summary>Alias for IsActive to maintain compatibility.</summary>
    public bool IsEnabled
    {
        get => IsActive;
        set => IsActive = value;
    }

    public bool SendOtpOnWhatsApp { get; set; } = true;
    public bool SendStatusOnWhatsApp { get; set; } = true;
    public string WabaUrl { get; set; } = string.Empty;
    public string WabaNumber { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    public WhatsAppTemplatesOptions Templates { get; set; } = new();
}

public sealed class WhatsAppTemplatesOptions
{
    public string ApplicationStatus { get; set; } = string.Empty;
    public string PaymentReceipt { get; set; } = string.Empty;
    public string CertificateIssued { get; set; } = string.Empty;
    public string CitizenOtp { get; set; } = string.Empty;
}
