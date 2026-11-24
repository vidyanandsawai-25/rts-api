using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service for dispatching official WhatsApp Business notifications via DigitalYug / Meta WABA Gateway.
/// </summary>
public interface IRTSWhatsAppNotificationService
{
    /// <summary>
    /// Dispatches dynamic application status update (Submitted, In Progress, Reverted, Rejected, Approved).
    /// Template: rts_application_status (Utility)
    /// </summary>
    Task SendApplicationStatusAsync(
        string mobileNo,
        string citizenName,
        string applicationNo,
        string serviceName,
        string status,
        string trackingUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Dispatches payment confirmation and e-receipt download link.
    /// Template: rts_payment_receipt (Utility)
    /// </summary>
    Task SendPaymentReceiptAsync(
        string mobileNo,
        string citizenName,
        decimal amount,
        string applicationNo,
        string receiptNo,
        string receiptUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Dispatches certificate issuance notice and verifiable download link.
    /// Template: rts_certificate_issued (Utility)
    /// </summary>
    Task SendCertificateIssuedAsync(
        string mobileNo,
        string citizenName,
        string applicationNo,
        string serviceName,
        string certificateUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Dispatches citizen portal login OTP with interactive copy-code button.
    /// Template: otp (Authentication)
    /// </summary>
    Task SendCitizenOtpAsync(
        string mobileNo,
        string otp,
        CancellationToken ct = default);
}
