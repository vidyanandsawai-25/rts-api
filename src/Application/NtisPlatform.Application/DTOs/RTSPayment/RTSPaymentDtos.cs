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

    public string? BankName { get; set; }

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
    public string? AmountInWords { get; set; }
    public string? AmountInWordsLocal { get; set; }
    public string Currency { get; set; } = "INR";
    public string PaymentGateway { get; set; } = string.Empty;
    public string GatewayPaymentId { get; set; } = string.Empty;
    public string? TransactionNo { get; set; }
    public string? BankRefNo { get; set; }
    public string? PayerVpaOrAccount { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PaymentMode { get; set; }
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
