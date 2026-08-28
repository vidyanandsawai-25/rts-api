using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSSmsNotificationService
{
    /// <summary>
    /// Sends SMS when citizen requests OTP for login or authentication
    /// </summary>
    Task SendCitizenOtpAsync(string mobileNo, string otp, CancellationToken ct = default);

    /// <summary>
    /// Single unified dynamic SMS template for all application status updates
    /// Template: "Dear {CitizenName}, your RTS Application No: {ApplicationNo} for {ServiceName} is currently {Status}. Track status: {TrackingUrl} - {CorporationName}"
    /// </summary>
    Task SendApplicationStatusUpdateAsync(int applicationId, string applicationNo, string citizenName, string mobileNo, string serviceName, string status, string? customUrl = null, CancellationToken ct = default);

    /// <summary>
    /// Sends SMS when citizen submits an RTS application
    /// </summary>
    Task SendApplicationSubmittedAsync(int applicationId, string applicationNo, string citizenName, string mobileNo, string serviceName, decimal fees = 0, CancellationToken ct = default);

    /// <summary>
    /// Sends SMS when online/counter payment is completed and verified (includes receipt link)
    /// </summary>
    Task SendPaymentSuccessAsync(int applicationId, string applicationNo, string citizenName, string mobileNo, decimal amount, string receiptNo, CancellationToken ct = default);

    /// <summary>
    /// Sends SMS when application advances to a new approval stage or changes status
    /// </summary>
    Task SendApplicationStageAdvancedAsync(int applicationId, string applicationNo, string citizenName, string mobileNo, string serviceName, string stageName, string status, string? remark = null, CancellationToken ct = default);

    /// <summary>
    /// Sends SMS when application is approved at final stage
    /// </summary>
    Task SendApplicationApprovedAsync(int applicationId, string applicationNo, string citizenName, string mobileNo, string serviceName, CancellationToken ct = default);

    /// <summary>
    /// Sends SMS when certificate is generated & digitally signed after Certificate Decision & Inputs
    /// </summary>
    Task SendApplicationCertificateIssuedAsync(int applicationId, string applicationNo, string citizenName, string mobileNo, string serviceName, CancellationToken ct = default);

    /// <summary>
    /// Sends SMS when application is rejected
    /// </summary>
    Task SendApplicationRejectedAsync(int applicationId, string applicationNo, string citizenName, string mobileNo, string serviceName, string? remark = null, CancellationToken ct = default);

    /// <summary>
    /// Sends SMS when application is reverted for correction
    /// </summary>
    Task SendApplicationRevertedAsync(int applicationId, string applicationNo, string citizenName, string mobileNo, string serviceName, string? remark = null, CancellationToken ct = default);
}
