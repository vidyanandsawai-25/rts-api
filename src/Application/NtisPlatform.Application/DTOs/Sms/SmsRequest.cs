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
}
