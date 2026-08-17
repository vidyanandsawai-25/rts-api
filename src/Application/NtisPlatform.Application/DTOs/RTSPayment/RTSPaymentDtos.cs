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
    public string Currency { get; set; } = "INR";
    public string PaymentGateway { get; set; } = string.Empty;
    public string GatewayPaymentId { get; set; } = string.Empty;
    public string ReceiptNo { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PaymentMode { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerMobile { get; set; }
}
