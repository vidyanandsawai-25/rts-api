using NtisPlatform.Application.DTOs.RTSPayment;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSPaymentService
{
    Task<PaymentDemandDto> CreateOrUpdatePaymentDemandAsync(CreatePaymentDemandRequestDto request, CancellationToken ct = default);
    Task<PaymentDemandDto?> GetPaymentDemandAsync(int applicationId, CancellationToken ct = default);
    Task<PaymentOrderResponseDto> CreatePaymentOrderAsync(CreatePaymentOrderRequestDto request, CancellationToken ct = default);
    Task<PaymentStatusDto> InitializePendingPaymentAsync(int applicationId, CancellationToken ct = default);
    Task<VerifyPaymentResponseDto> VerifyPaymentAsync(VerifyPaymentRequestDto request, CancellationToken ct = default);
    Task<PaymentReceiptDto> RecordOfflinePaymentAsync(RecordOfflinePaymentRequestDto request, int officerUserId, CancellationToken ct = default);
    Task<PaymentReceiptDto?> GetPaymentReceiptByApplicationIdAsync(int applicationId, CancellationToken ct = default);
    Task<PaymentReceiptDto?> GetPaymentReceiptByReceiptNoAsync(string receiptNo, CancellationToken ct = default);
    Task<PaymentStatusDto?> GetPaymentStatusAsync(int applicationId, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentStatusDto>> GetPaymentStatusesByApplicationNosAsync( IEnumerable<string> applicationNos, CancellationToken ct = default);
    Task<bool> ProcessWebhookAsync(string webhookPayload, string? signatureHeader, CancellationToken ct = default);
    Task<PagedResult<PaymentTransactionListItemDto>> GetTransactionsAsync(PaymentTransactionQueryDto query, CancellationToken ct = default);
    Task<PagedResult<PendingClearanceItemDto>> GetPendingClearancePaymentsAsync(PendingClearanceQueryDto query, CancellationToken ct = default);
    Task<PendingClearanceStatsDto> GetPendingClearanceStatsAsync(int UserId,CancellationToken ct = default);
    Task<VerifyPaymentResponseDto> ApproveTransactionAsync(ApprovePaymentRequestDto request, int officerUserId, CancellationToken ct = default);
    Task<VerifyPaymentResponseDto> DisapproveTransactionAsync(DisapprovePaymentRequestDto request, int officerUserId, CancellationToken ct = default);
    Task<VerifyPaymentResponseDto> CancelReceiptAsync(CancelReceiptRequestDto request, int officerUserId, CancellationToken ct = default);
    Task<BulkClearanceResponseDto> BulkClearanceUploadAsync(System.IO.Stream stream, int officerUserId, CancellationToken ct = default);
    Task<byte[]> DownloadClearanceTemplateAsync(CancellationToken ct = default);
    Task<List<RTSBankLookupDto>> GetBanksAsync(CancellationToken ct = default);
    Task<List<RTSPaymentModeLookupDto>> GetPaymentModesAsync(CancellationToken ct = default);
    Task<List<RTSPaymentStatusLookupDto>> GetPaymentStatusesAsync(CancellationToken ct = default);
    Task<List<ReceiptCancellationLogDto>> GetCancellationLogsAsync(int? applicationId = null, long? transactionId = null, CancellationToken ct = default);
}

