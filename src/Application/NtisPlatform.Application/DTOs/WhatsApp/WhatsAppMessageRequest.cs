namespace NtisPlatform.Application.DTOs.WhatsApp;

/// <summary>
/// Request DTO for dispatching a WhatsApp Business notification.
/// Mirrors the enterprise DTO pattern established in NtisPlatform.Application.DTOs.Sms.SmsRequest.
/// </summary>
public sealed class WhatsAppMessageRequest
{
    public string RecipientMobile { get; set; } = null!;
    public string TemplateName { get; set; } = null!;
    public string LanguageCode { get; set; } = "en";
    public string[] Parameters { get; set; } = System.Array.Empty<string>();
    public int? ApplicationId { get; set; }
    public int? SMSTypeID { get; set; }
}
