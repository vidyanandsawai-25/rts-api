using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSSmsNotificationService
{
    /// <summary>
    /// Sends SMS when citizen submits an RTS application
    /// </summary>
    Task SendApplicationSubmittedAsync(int applicationId, string applicationNo, string citizenName, string mobileNo, string serviceName, CancellationToken ct = default);

    /// <summary>
    /// Sends SMS when application requires payment
    /// </summary>
    Task SendPaymentPendingAsync(int applicationId, string applicationNo, string citizenName, string mobileNo, string serviceName, decimal amount, CancellationToken ct = default);

    /// <summary>
    /// Sends SMS when online payment is completed and verified
    /// </summary>
    Task SendPaymentSuccessAsync(int applicationId, string applicationNo, string citizenName, string mobileNo, decimal amount, string receiptNo, CancellationToken ct = default);

    /// <summary>
    /// Sends SMS when certificate/license is issued
    /// </summary>
    Task SendApplicationApprovedAsync(int applicationId, string applicationNo, string citizenName, string mobileNo, string serviceName, CancellationToken ct = default);
}
