using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxCalculationDetail;

/// <summary>
/// Custom DTOs (not <see cref="BaseDtos"/>): RetrospectiveTaxCalculationDetailEntity uses a
/// BIGINT key and has no CreatedBy/IsActive/UpdatedBy/UpdatedDate columns.
/// </summary>
public class RetrospectiveTaxCalculationDetailDto
{
    public long Id { get; set; }
    public long CalculationId { get; set; }
    public int PropertyId { get; set; }
    public int FloorId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? RateMode { get; set; }
    public string? PercentageMode { get; set; }
    public decimal BaseTaxAmount { get; set; }
    public decimal TaxMultiplier { get; set; }
    public decimal RetrospectiveTaxAmount { get; set; }
    public decimal? PenaltyPercent { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateRetrospectiveTaxCalculationDetailDto
{
    [Range(1, long.MaxValue, ErrorMessage = "RetrospectiveTaxCalculationDetail_CalculationId_Invalid")]
    public long CalculationId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveTaxCalculationDetail_PropertyId_Invalid")]
    public int PropertyId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveTaxCalculationDetail_FloorId_Invalid")]
    public int FloorId { get; set; }

    [Required(ErrorMessage = "RetrospectiveTaxCalculationDetail_FinancialYear_Required")]
    [StringLength(20, ErrorMessage = "RetrospectiveTaxCalculationDetail_FinancialYear_MaxLen_20")]
    public string FinancialYear { get; set; } = string.Empty;

    [Required(ErrorMessage = "RetrospectiveTaxCalculationDetail_FromDate_Required")]
    public DateTime FromDate { get; set; }

    [Required(ErrorMessage = "RetrospectiveTaxCalculationDetail_ToDate_Required")]
    public DateTime ToDate { get; set; }

    [StringLength(50, ErrorMessage = "RetrospectiveTaxCalculationDetail_RateMode_MaxLen_50")]
    public string? RateMode { get; set; }

    [StringLength(50, ErrorMessage = "RetrospectiveTaxCalculationDetail_PercentageMode_MaxLen_50")]
    public string? PercentageMode { get; set; }

    public decimal BaseTaxAmount { get; set; }
    public decimal TaxMultiplier { get; set; } = 1.00m;
    public decimal RetrospectiveTaxAmount { get; set; }
    public decimal? PenaltyPercent { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class UpdateRetrospectiveTaxCalculationDetailDto
{
    [Range(1, long.MaxValue, ErrorMessage = "RetrospectiveTaxCalculationDetail_CalculationId_Invalid")]
    public long CalculationId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveTaxCalculationDetail_PropertyId_Invalid")]
    public int PropertyId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveTaxCalculationDetail_FloorId_Invalid")]
    public int FloorId { get; set; }

    [Required(ErrorMessage = "RetrospectiveTaxCalculationDetail_FinancialYear_Required")]
    [StringLength(20, ErrorMessage = "RetrospectiveTaxCalculationDetail_FinancialYear_MaxLen_20")]
    public string FinancialYear { get; set; } = string.Empty;

    [Required(ErrorMessage = "RetrospectiveTaxCalculationDetail_FromDate_Required")]
    public DateTime FromDate { get; set; }

    [Required(ErrorMessage = "RetrospectiveTaxCalculationDetail_ToDate_Required")]
    public DateTime ToDate { get; set; }

    [StringLength(50, ErrorMessage = "RetrospectiveTaxCalculationDetail_RateMode_MaxLen_50")]
    public string? RateMode { get; set; }

    [StringLength(50, ErrorMessage = "RetrospectiveTaxCalculationDetail_PercentageMode_MaxLen_50")]
    public string? PercentageMode { get; set; }

    public decimal BaseTaxAmount { get; set; }
    public decimal TaxMultiplier { get; set; }
    public decimal RetrospectiveTaxAmount { get; set; }
    public decimal? PenaltyPercent { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal TotalAmount { get; set; }
}
