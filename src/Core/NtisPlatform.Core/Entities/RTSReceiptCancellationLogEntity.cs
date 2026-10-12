using System;

namespace NtisPlatform.Core.Entities;

public class RTSReceiptCancellationLogEntity
{
    public int Id { get; set; }
    public long TransactionId { get; set; }
    public int ApplicationId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string? InstrumentNo { get; set; }
    public int? BankId { get; set; }
    public string? BankName { get; set; }
    public int? CancelledBy { get; set; }
    public string? CancelledByName { get; set; }
    public DateTime CancelledDate { get; set; } = DateTime.UtcNow;
    public string CancellationReason { get; set; } = string.Empty;
    public int? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovalRemarks { get; set; }
    public string? IPAddress { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public virtual RTSPaymentTransactionEntity? PaymentTransaction { get; set; }
    public virtual RTSApplicationDetailsEntity? Application { get; set; }
}
