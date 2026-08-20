using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxCalculationDetail;

/// <summary>
/// MarkedForDeletion/MarkedForDeletionDate are system-managed via the Purge endpoints, not user
/// input, so they're declared directly here rather than in <see cref="BaseDtos"/>.
/// </summary>
public class RetrospectiveTaxCalculationDetailDto : BaseDtos
{
    public int CalculationId { get; set; }
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
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreateRetrospectiveTaxCalculationDetailDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveTaxCalculationDetail_CalculationId_Invalid")]
    public int CalculationId { get; set; }

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

public class UpdateRetrospectiveTaxCalculationDetailDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveTaxCalculationDetail_CalculationId_Invalid")]
    public int CalculationId { get; set; }

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
