using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management.LeaseRentBillTransaction;

/// <summary>
/// DTO for LeaseRentBillTransactionEntity - Payment header for lease-rent collection.
/// </summary>
public class LeaseRentBillTransactionDto : BaseDtos
{
    public string TransactionNo { get; set; } = string.Empty;
    public string? ReceiptNo { get; set; }
    public int AssetId { get; set; }
    public int LeaseRegistrationId { get; set; }
    public int FinanceYear { get; set; }
    public decimal TotalMonthlyRentAmount { get; set; }
    public decimal TotalPenaltyAmount { get; set; }
    public decimal TotalGSTAmount { get; set; }
    public decimal TotalDemandAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public decimal NetPayableAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public string? ChequeOrTransactionNo { get; set; }
    public DateTime? ChequeDate { get; set; }
    public string? OnlineTransactionId { get; set; }
    public string? PaymentGatewayName { get; set; }
    public string? PayerMobile { get; set; }
    public string? PayerEmail { get; set; }
    public string PaymentStatus { get; set; } = "Success";
    public int? CancelledBy { get; set; }
    public DateTime? CancelledDate { get; set; }
    public string? CancellationReason { get; set; }
    public string? Remark { get; set; }

    // Navigation property names
    public string? AssetName { get; set; }
    public string? AssetNo { get; set; }
}

public class CreateLeaseRentBillTransactionDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AMS_LeaseRentBillTransaction_TransactionNo_Required")]
    [StringLength(50, ErrorMessage = "AMS_LeaseRentBillTransaction_TransactionNo_MaxLengthExceeded_50")]
    public string TransactionNo { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "AMS_LeaseRentBillTransaction_ReceiptNo_MaxLengthExceeded_50")]
    public string? ReceiptNo { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransaction_AssetId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransaction_AssetId_InvalidRange")]
    public int AssetId { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransaction_LeaseRegistrationId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransaction_LeaseRegistrationId_InvalidRange")]
    public int LeaseRegistrationId { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransaction_FinanceYear_Required")]
    [Range(2000, 2100, ErrorMessage = "AMS_LeaseRentBillTransaction_FinanceYear_InvalidRange")]
    public int FinanceYear { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransaction_TotalMonthlyRentAmount_InvalidRange")]
    public decimal TotalMonthlyRentAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransaction_TotalPenaltyAmount_InvalidRange")]
    public decimal TotalPenaltyAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransaction_TotalGSTAmount_InvalidRange")]
    public decimal TotalGSTAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransaction_DiscountAmount_InvalidRange")]
    public decimal DiscountAmount { get; set; }

    public decimal AdjustmentAmount { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransaction_PaidAmount_Required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransaction_PaidAmount_InvalidRange")]
    public decimal PaidAmount { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransaction_PaymentMode_Required")]
    [StringLength(50, ErrorMessage = "AMS_LeaseRentBillTransaction_PaymentMode_MaxLengthExceeded_50")]
    [RegularExpression("^(Cash|Cheque|DD|UPI|Online|NEFT|RTGS)$", ErrorMessage = "AMS_LeaseRentBillTransaction_PaymentMode_Invalid")]
    public string PaymentMode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AMS_LeaseRentBillTransaction_PaymentDate_Required")]
    public DateTime PaymentDate { get; set; }

    [StringLength(200, ErrorMessage = "AMS_LeaseRentBillTransaction_BankName_MaxLengthExceeded_200")]
    public string? BankName { get; set; }

    [StringLength(200, ErrorMessage = "AMS_LeaseRentBillTransaction_BranchName_MaxLengthExceeded_200")]
    public string? BranchName { get; set; }

    [StringLength(100, ErrorMessage = "AMS_LeaseRentBillTransaction_ChequeOrTransactionNo_MaxLengthExceeded_100")]
    public string? ChequeOrTransactionNo { get; set; }

    public DateTime? ChequeDate { get; set; }

    [StringLength(100, ErrorMessage = "AMS_LeaseRentBillTransaction_OnlineTransactionId_MaxLengthExceeded_100")]
    public string? OnlineTransactionId { get; set; }

    [StringLength(100, ErrorMessage = "AMS_LeaseRentBillTransaction_PaymentGatewayName_MaxLengthExceeded_100")]
    public string? PaymentGatewayName { get; set; }

    [StringLength(20, ErrorMessage = "AMS_LeaseRentBillTransaction_PayerMobile_MaxLengthExceeded_20")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "AMS_LeaseRentBillTransaction_PayerMobile_Invalid")]
    public string? PayerMobile { get; set; }

    [StringLength(100, ErrorMessage = "AMS_LeaseRentBillTransaction_PayerEmail_MaxLengthExceeded_100")]
    [EmailAddress(ErrorMessage = "AMS_LeaseRentBillTransaction_PayerEmail_Invalid")]
    public string? PayerEmail { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransaction_PaymentStatus_Required")]
    [StringLength(50, ErrorMessage = "AMS_LeaseRentBillTransaction_PaymentStatus_MaxLengthExceeded_50")]
    [RegularExpression("^(Success|Pending|Failed|Cancelled)$", ErrorMessage = "AMS_LeaseRentBillTransaction_PaymentStatus_Invalid")]
    public string PaymentStatus { get; set; } = "Success";

    [StringLength(500, ErrorMessage = "AMS_LeaseRentBillTransaction_Remark_MaxLengthExceeded_500")]
    public string? Remark { get; set; }
}

public class UpdateLeaseRentBillTransactionDto : UpdateBaseDtos
{
    [StringLength(50, ErrorMessage = "AMS_LeaseRentBillTransaction_ReceiptNo_MaxLengthExceeded_50")]
    public string? ReceiptNo { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransaction_DiscountAmount_InvalidRange")]
    public decimal DiscountAmount { get; set; }

    public decimal AdjustmentAmount { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransaction_PaymentStatus_Required")]
    [StringLength(50, ErrorMessage = "AMS_LeaseRentBillTransaction_PaymentStatus_MaxLengthExceeded_50")]
    [RegularExpression("^(Success|Pending|Failed|Cancelled)$", ErrorMessage = "AMS_LeaseRentBillTransaction_PaymentStatus_Invalid")]
    public string PaymentStatus { get; set; } = "Success";

    [StringLength(500, ErrorMessage = "AMS_LeaseRentBillTransaction_CancellationReason_MaxLengthExceeded_500")]
    public string? CancellationReason { get; set; }

    [StringLength(500, ErrorMessage = "AMS_LeaseRentBillTransaction_Remark_MaxLengthExceeded_500")]
    public string? Remark { get; set; }
}
