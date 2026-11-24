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
    public bool SendOfficerAlertsOnWhatsApp { get; set; } = true;
    public string WabaUrl { get; set; } = string.Empty;
    public string WabaNumber { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    public WhatsAppTemplatesOptions Templates { get; set; } = new();
    public WhatsAppOfficerWorkerOptions Worker { get; set; } = new();
}

public sealed class WhatsAppTemplatesOptions
{
    public string ApplicationStatus { get; set; } = "rts_application_status";
    public string PaymentReceipt { get; set; } = "rts_payment_receipt";
    public string CertificateIssued { get; set; } = "rts_certificate_issued";
    public string CitizenOtp { get; set; } = "amc_user_otp";
    public string OfficerAssigned { get; set; } = "rts_officer_assignment";
    public string OfficerSlaWarning { get; set; } = "rts_officer_sla_warning";
    public string OfficerDailyDigest { get; set; } = "rts_officer_daily_digest";
    public string CitizenReminder { get; set; } = "rts_citizen_reminder";
}

public sealed class WhatsAppOfficerWorkerOptions
{
    public bool IsEnabled { get; set; } = true;
    public int PollIntervalMinutes { get; set; } = 2;
    public int MorningDigestHour { get; set; } = 9;
    public int MorningDigestMinute { get; set; } = 30;
    public int SlaWarningThresholdDays { get; set; } = 2;
    public string OfficerPortalBaseUrl { get; set; } = "https://rts.amcakola.in/en/login";
}
