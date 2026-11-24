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
        CancellationToken ct = default,
        int? applicationId = null);

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
        CancellationToken ct = default,
        int? applicationId = null);

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
        CancellationToken ct = default,
        int? applicationId = null);

    /// <summary>
    /// Dispatches citizen portal login OTP with interactive copy-code button.
    /// Template: amc_user_otp (Authentication)
    /// </summary>
    Task SendCitizenOtpAsync(
        string mobileNo,
        string otp,
        CancellationToken ct = default);

    /// <summary>
    /// Dispatches task assignment notice to the allocated officer when an application enters their stage.
    /// Template: rts_officer_assignment (Utility)
    /// </summary>
    Task SendOfficerTaskAssignedAsync(
        string mobileNo,
        string officerName,
        string applicationNo,
        string serviceName,
        string stageName,
        string dueDate,
        string portalUrl,
        int? applicationId = null,
        int? stageId = null,
        int? officerUserId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Dispatches urgent SLA warning to the assigned officer when SLA is expiring in 2 days or less.
    /// Template: rts_officer_sla_warning (Utility)
    /// </summary>
    Task SendOfficerSlaWarningAsync(
        string mobileNo,
        string officerName,
        string applicationNo,
        string serviceName,
        int daysRemaining,
        string dueDate,
        string actionUrl,
        int? applicationId = null,
        int? officerUserId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Dispatches morning digest summary to each officer with their pending applications count.
    /// Template: rts_officer_daily_digest (Utility)
    /// </summary>
    Task SendOfficerDailyDigestAsync(
        string mobileNo,
        string officerName,
        int totalPending,
        int nearSlaCount,
        string dashboardUrl,
        int? officerUserId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Dispatches pending fee payment or document re-upload reminder to citizen.
    /// Template: rts_citizen_reminder (Utility)
    /// </summary>
    Task SendCitizenActionReminderAsync(
        string mobileNo,
        string citizenName,
        string applicationNo,
        string serviceName,
        string pendingAction,
        string dueDate,
        string serviceUrl,
        int? applicationId = null,
        CancellationToken ct = default);
}
