using System;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RTSPayment;

public class CreatePaymentOrderRequestDto
{
    [Required]
    public int ApplicationId { get; set; }

    public string? PaymentGateway { get; set; }

    public string? CustomerName { get; set; }

    public string? Email { get; set; }

    public string? MobileNo { get; set; }

}

public class PaymentStatusDto
{
    public long? TransactionId { get; set; }
    public int ApplicationId { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal RequiredFee { get; set; }
    public bool IsFeeRequired { get; set; }
    public bool HasPaymentTransaction { get; set; }
    public string PaymentStatus { get; set; } = "NOT_REQUIRED";
    public string? StatusNameEn { get; set; }
    public string? StatusNameMr { get; set; }
    public string? BadgeColor { get; set; }
    public string? ReceiptNo { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? GatewayPaymentId { get; set; }
}

public class BulkPaymentStatusRequestDto
{
    [Required]
    [MinLength(1)]
    public List<string> ApplicationNos { get; set; } = new();
}

public class PaymentOrderResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int TransactionId { get; set; }
    public int ApplicationId { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public long AmountInPaise { get; set; }
    public string Currency { get; set; } = "INR";
    public string Gateway { get; set; } = string.Empty;
    public string? GatewayOrderId { get; set; }
    public string? KeyId { get; set; }
    public string? Description { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerMobile { get; set; }
}

public class VerifyPaymentRequestDto
{
    [Required]
    public int ApplicationId { get; set; }

    [Required]
    public string GatewayOrderId { get; set; } = string.Empty;

    [Required]
    public string GatewayPaymentId { get; set; } = string.Empty;

    [Required]
    public string GatewaySignature { get; set; } = string.Empty;

    public string? PaymentMode { get; set; }
}

public class RecordOfflinePaymentRequestDto
{
    [Required]
    public int ApplicationId { get; set; }

    [Required]
    public string PaymentMode { get; set; } = "Cash";

    public decimal? Amount { get; set; }

    public string? InstrumentNo { get; set; }

    public DateTime? InstrumentDate { get; set; }

    public int? BankId { get; set; }

    public string? BankName { get; set; }

    public string? BranchName { get; set; }

    public string? Remarks { get; set; }
}

public class VerifyPaymentResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ReceiptNo { get; set; }
    public string? TransactionId { get; set; }
    public string? ApplicationNo { get; set; }
    public decimal Amount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentStatus { get; set; }
}

public class PaymentReceiptDto
{
    public int TransactionId { get; set; }
    public int ApplicationId { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceNameLocal { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string DepartmentNameLocal { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? BaseAmount { get; set; }
    public decimal? LateFeeAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public string? AmountInWords { get; set; }
    public string? AmountInWordsLocal { get; set; }
    public string Currency { get; set; } = "INR";
    public string PaymentGateway { get; set; } = string.Empty;
    public string GatewayPaymentId { get; set; } = string.Empty;
    public string? TransactionNo { get; set; }
    public string? BankRefNo { get; set; }
    public string? BankName { get; set; }
    public string? InstrumentNo { get; set; }
    public string? PayerVpaOrAccount { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PaymentMode { get; set; }
    public string? Channel { get; set; }
    public string? CounterOfficerName { get; set; }
    public string? Remarks { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerMobile { get; set; }
    public string? CustomerEmail { get; set; }
    public string? UlbLogo { get; set; }
    public string UlbName { get; set; } = string.Empty;
    public string UlbNameLocal { get; set; } = string.Empty;
}

public class PaymentTransactionQueryDto
{
    public int? DepartmentId { get; set; }
    public int? ServiceId { get; set; }
    public int? PaymentStatusId { get; set; }
    public string? StatusCode { get; set; }
    public int? PaymentModeId { get; set; }
    public string? PaymentModeCode { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchText { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PaymentTransactionListItemDto
{
    public long Id { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public int ApplicationId { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public string PaymentStatus { get; set; } = string.Empty;
    public string? StatusBadgeColor { get; set; }
    public string? PaymentMode { get; set; }
    public string? GatewayPaymentId { get; set; }
    public string? ReceiptNo { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CitizenName { get; set; }
    public string? CitizenMobile { get; set; }
}

public class ApprovePaymentRequestDto
{
    [Required]
    public long TransactionId { get; set; }

    public DateTime? ClearanceDate { get; set; }

    public string? BankRefNo { get; set; }

    public string? Remarks { get; set; }
}

public class DisapprovePaymentRequestDto
{
    [Required]
    public long TransactionId { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;

    public string? Remarks { get; set; }
}

public class CancelReceiptRequestDto
{
    [Required]
    public long TransactionId { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;

    public string? Remarks { get; set; }
}

public class PendingClearanceQueryDto
{
    public string? PaymentMode { get; set; }
    public string? StatusCode { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchText { get; set; }
    public int? DepartmentId { get; set; }
    public int? ServiceId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PendingClearanceItemDto
{
    public long TransactionId { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public int ApplicationId { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public string? CitizenName { get; set; }
    public string? CitizenMobile { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? ServiceNameLocal { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? DepartmentNameLocal { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public int? BankId { get; set; }
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public string? InstrumentNo { get; set; }
    public DateTime? InstrumentDate { get; set; }
    public DateTime? ClearanceDate { get; set; }
    public string? BankRefNo { get; set; }
    public string? GatewayPaymentId { get; set; }
    public string? ReceiptNo { get; set; }
    public DateTime? ReceiptDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? StatusBadgeColor { get; set; }
    public string? ApprovalStatus { get; set; }
    public bool IsReceiptCancelled { get; set; }
    public string? CancellationReason { get; set; }
    public string? FailureReason { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class PendingClearanceStatsDto
{
    public int TotalPendingDdCheque { get; set; }
    public int TotalPendingOnline { get; set; }
    public int TotalClearedToday { get; set; }
    public int TotalBounced { get; set; }
    public int TotalCancelled { get; set; }
    public decimal TotalClearedAmountToday { get; set; }
}

public class BulkClearanceRowResultDto
{
    public int RowIndex { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public string? InstrumentNo { get; set; }
    public string Action { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BulkClearanceResponseDto
{
    public int TotalRows { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BulkClearanceRowResultDto> Results { get; set; } = new();
}

public class RTSBankLookupDto
{
    public int Id { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public string? IFSCCode { get; set; }
}

public class ReceiptCancellationLogDto
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
    public DateTime CancelledDate { get; set; }
    public string CancellationReason { get; set; } = string.Empty;
    public int? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovalRemarks { get; set; }
    public DateTime CreatedDate { get; set; }
}


