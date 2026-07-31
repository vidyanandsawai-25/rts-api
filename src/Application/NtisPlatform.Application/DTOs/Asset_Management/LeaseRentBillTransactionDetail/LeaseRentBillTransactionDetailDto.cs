using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management.LeaseRentBillTransactionDetail;

/// <summary>
/// DTO for LeaseRentBillTransactionDetailEntity - Month-wise payment detail line.
/// </summary>
public class LeaseRentBillTransactionDetailDto : BaseDtos
{
    public int LeaseRentBillTransactionId { get; set; }
    public int MonthWiseDemandId { get; set; }
    public int AssetId { get; set; }
    public int LeaseRegistrationId { get; set; }
    public int FinanceYear { get; set; }
    public int DemandYear { get; set; }
    public byte QuarterNo { get; set; }
    public byte DemandMonth { get; set; }
    public decimal MonthlyRentAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal TotalDemandAmount { get; set; }
    public decimal PreviousPaidAmount { get; set; }
    public decimal CurrentPaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string PaymentStatus { get; set; } = "Paid";

    // Navigation property names
    public string? AssetName { get; set; }
    public string? AssetNo { get; set; }
}

public class CreateLeaseRentBillTransactionDetailDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AMS_LeaseRentBillTransactionDetail_LeaseRentBillTransactionId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_LeaseRentBillTransactionId_InvalidRange")]
    public int LeaseRentBillTransactionId { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransactionDetail_MonthWiseDemandId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_MonthWiseDemandId_InvalidRange")]
    public int MonthWiseDemandId { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransactionDetail_AssetId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_AssetId_InvalidRange")]
    public int AssetId { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransactionDetail_LeaseRegistrationId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_LeaseRegistrationId_InvalidRange")]
    public int LeaseRegistrationId { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransactionDetail_FinanceYear_Required")]
    [Range(2000, 2100, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_FinanceYear_InvalidRange")]
    public int FinanceYear { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransactionDetail_DemandYear_Required")]
    [Range(2000, 2100, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_DemandYear_InvalidRange")]
    public int DemandYear { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransactionDetail_QuarterNo_Required")]
    [Range(1, 4, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_QuarterNo_InvalidRange")]
    public byte QuarterNo { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransactionDetail_DemandMonth_Required")]
    [Range(1, 12, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_DemandMonth_InvalidRange")]
    public byte DemandMonth { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransactionDetail_MonthlyRentAmount_Required")]
    [Range(0, double.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_MonthlyRentAmount_InvalidRange")]
    public decimal MonthlyRentAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_PenaltyAmount_InvalidRange")]
    public decimal PenaltyAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_GSTAmount_InvalidRange")]
    public decimal GSTAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_PreviousPaidAmount_InvalidRange")]
    public decimal PreviousPaidAmount { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransactionDetail_CurrentPaidAmount_Required")]
    [Range(0, double.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_CurrentPaidAmount_InvalidRange")]
    public decimal CurrentPaidAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_BalanceAmount_InvalidRange")]
    public decimal BalanceAmount { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransactionDetail_PaymentStatus_Required")]
    [StringLength(50, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_PaymentStatus_MaxLengthExceeded_50")]
    [RegularExpression("^(Partial|Paid|Cancelled)$", ErrorMessage = "AMS_LeaseRentBillTransactionDetail_PaymentStatus_Invalid")]
    public string PaymentStatus { get; set; } = "Paid";
}

public class UpdateLeaseRentBillTransactionDetailDto : UpdateBaseDtos
{
    [Range(0, double.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_PreviousPaidAmount_InvalidRange")]
    public decimal PreviousPaidAmount { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransactionDetail_CurrentPaidAmount_Required")]
    [Range(0, double.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_CurrentPaidAmount_InvalidRange")]
    public decimal CurrentPaidAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_BalanceAmount_InvalidRange")]
    public decimal BalanceAmount { get; set; }

    [Required(ErrorMessage = "AMS_LeaseRentBillTransactionDetail_PaymentStatus_Required")]
    [StringLength(50, ErrorMessage = "AMS_LeaseRentBillTransactionDetail_PaymentStatus_MaxLengthExceeded_50")]
    [RegularExpression("^(Partial|Paid|Cancelled)$", ErrorMessage = "AMS_LeaseRentBillTransactionDetail_PaymentStatus_Invalid")]
    public string PaymentStatus { get; set; } = "Paid";
}
