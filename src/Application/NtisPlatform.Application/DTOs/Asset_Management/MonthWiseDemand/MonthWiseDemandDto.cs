using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management.MonthWiseDemand;

/// <summary>
/// DTO for MonthWiseDemandEntity - One month's rent demand for a lease.
/// </summary>
public class MonthWiseDemandDto : BaseDtos
{
    public int AssetId { get; set; }
    public int LeaseRegistrationId { get; set; }
    public int FinanceYear { get; set; }
    public int DemandYear { get; set; }
    public byte QuarterNo { get; set; }
    public byte DemandMonth { get; set; }
    public decimal MonthlyRentAmount { get; set; }
    public int? PenaltyRuleMasterId { get; set; }
    public decimal PenaltyAmount { get; set; }
    public int? GSTMasterId { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal TotalDemandAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public string DemandStatus { get; set; } = "Pending";
    public DateTime? LastPaymentDate { get; set; }
    public DateTime? DueDate { get; set; }

    // Navigation property names
    public string? AssetName { get; set; }
    public string? AssetNo { get; set; }
    public string? PenaltyRuleName { get; set; }
    public string? GSTName { get; set; }
}

public class CreateMonthWiseDemandDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AMS_MonthWiseDemand_AssetId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_MonthWiseDemand_AssetId_InvalidRange")]
    public int AssetId { get; set; }

    [Required(ErrorMessage = "AMS_MonthWiseDemand_LeaseRegistrationId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_MonthWiseDemand_LeaseRegistrationId_InvalidRange")]
    public int LeaseRegistrationId { get; set; }

    [Required(ErrorMessage = "AMS_MonthWiseDemand_FinanceYear_Required")]
    [Range(2000, 2100, ErrorMessage = "AMS_MonthWiseDemand_FinanceYear_InvalidRange")]
    public int FinanceYear { get; set; }

    [Required(ErrorMessage = "AMS_MonthWiseDemand_DemandYear_Required")]
    [Range(2000, 2100, ErrorMessage = "AMS_MonthWiseDemand_DemandYear_InvalidRange")]
    public int DemandYear { get; set; }

    [Required(ErrorMessage = "AMS_MonthWiseDemand_QuarterNo_Required")]
    [Range(1, 4, ErrorMessage = "AMS_MonthWiseDemand_QuarterNo_InvalidRange")]
    public byte QuarterNo { get; set; }

    [Required(ErrorMessage = "AMS_MonthWiseDemand_DemandMonth_Required")]
    [Range(1, 12, ErrorMessage = "AMS_MonthWiseDemand_DemandMonth_InvalidRange")]
    public byte DemandMonth { get; set; }

    [Required(ErrorMessage = "AMS_MonthWiseDemand_MonthlyRentAmount_Required")]
    [Range(0, double.MaxValue, ErrorMessage = "AMS_MonthWiseDemand_MonthlyRentAmount_InvalidRange")]
    public decimal MonthlyRentAmount { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_MonthWiseDemand_PenaltyRuleMasterId_InvalidRange")]
    public int? PenaltyRuleMasterId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_MonthWiseDemand_PenaltyAmount_InvalidRange")]
    public decimal PenaltyAmount { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_MonthWiseDemand_GSTMasterId_InvalidRange")]
    public int? GSTMasterId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_MonthWiseDemand_GSTAmount_InvalidRange")]
    public decimal GSTAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_MonthWiseDemand_PaidAmount_InvalidRange")]
    public decimal PaidAmount { get; set; }

    [Required(ErrorMessage = "AMS_MonthWiseDemand_DemandStatus_Required")]
    [StringLength(50, ErrorMessage = "AMS_MonthWiseDemand_DemandStatus_MaxLengthExceeded_50")]
    [RegularExpression("^(Pending|Partial|Paid|Cancelled)$", ErrorMessage = "AMS_MonthWiseDemand_DemandStatus_Invalid")]
    public string DemandStatus { get; set; } = "Pending";

    public DateTime? LastPaymentDate { get; set; }

    public DateTime? DueDate { get; set; }
}

public class UpdateMonthWiseDemandDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AMS_MonthWiseDemand_MonthlyRentAmount_Required")]
    [Range(0, double.MaxValue, ErrorMessage = "AMS_MonthWiseDemand_MonthlyRentAmount_InvalidRange")]
    public decimal MonthlyRentAmount { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_MonthWiseDemand_PenaltyRuleMasterId_InvalidRange")]
    public int? PenaltyRuleMasterId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_MonthWiseDemand_PenaltyAmount_InvalidRange")]
    public decimal PenaltyAmount { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_MonthWiseDemand_GSTMasterId_InvalidRange")]
    public int? GSTMasterId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_MonthWiseDemand_GSTAmount_InvalidRange")]
    public decimal GSTAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_MonthWiseDemand_PaidAmount_InvalidRange")]
    public decimal PaidAmount { get; set; }

    [Required(ErrorMessage = "AMS_MonthWiseDemand_DemandStatus_Required")]
    [StringLength(50, ErrorMessage = "AMS_MonthWiseDemand_DemandStatus_MaxLengthExceeded_50")]
    [RegularExpression("^(Pending|Partial|Paid|Cancelled)$", ErrorMessage = "AMS_MonthWiseDemand_DemandStatus_Invalid")]
    public string DemandStatus { get; set; } = "Pending";

    public DateTime? LastPaymentDate { get; set; }

    public DateTime? DueDate { get; set; }
}
