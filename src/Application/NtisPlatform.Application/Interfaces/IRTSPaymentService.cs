using System.Threading;
using System.Threading.Tasks;
using NtisPlatform.Application.DTOs.RTSPayment;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSPaymentService
{
    Task<PaymentOrderResponseDto> CreatePaymentOrderAsync(CreatePaymentOrderRequestDto request, CancellationToken ct = default);
    Task<VerifyPaymentResponseDto> VerifyPaymentAsync(VerifyPaymentRequestDto request, CancellationToken ct = default);
    Task<PaymentReceiptDto?> GetPaymentReceiptByApplicationIdAsync(int applicationId, CancellationToken ct = default);
    Task<PaymentReceiptDto?> GetPaymentReceiptByReceiptNoAsync(string receiptNo, CancellationToken ct = default);
    Task<object?> GetPaymentStatusAsync(int applicationId, CancellationToken ct = default);
    Task<bool> ProcessWebhookAsync(string webhookPayload, string? signatureHeader, CancellationToken ct = default);
}
