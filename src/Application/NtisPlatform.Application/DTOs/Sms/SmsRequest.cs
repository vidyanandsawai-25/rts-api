namespace NtisPlatform.Application.DTOs.Sms;

/// <summary>
/// Request model for sending an SMS.
/// </summary>
public class SmsRequest
{
    /// <summary>
    /// Recipient phone number (required), in whatever format the gateway expects.
    /// </summary>
    public string PhoneNumber { get; set; } = null!;

    /// <summary>
    /// Message text (required).
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// Optional DLT Template ID
    /// </summary>
    public string? TemplateId { get; set; }

    /// <summary>
    /// Optional Template Name (e.g. RTS_APP_SUBMITTED, RTS_FEE_PAID)
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Optional SMS Type ID (1: OTP, 4: RTS Application Submitted, etc.)
    /// </summary>
    public int? SMSTypeID { get; set; }

    /// <summary>
    /// Optional associated RTS Application ID
    /// </summary>
    public int? ApplicationId { get; set; }
}
