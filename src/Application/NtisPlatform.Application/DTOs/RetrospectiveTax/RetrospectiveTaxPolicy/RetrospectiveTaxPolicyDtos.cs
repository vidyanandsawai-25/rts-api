using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxPolicy;

public class RetrospectiveTaxPolicyDto : BaseDtos
{
    public string TaxPolicyCode { get; set; } = string.Empty;
    public string TaxPolicyName { get; set; } = string.Empty;
    public string RateMode { get; set; } = string.Empty;
    public string PercentageMode { get; set; } = string.Empty;
    public decimal? FixedPercentage { get; set; }
    public byte FinancialYearStartMonth { get; set; }
    public byte FinancialYearStartDay { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class CreateRetrospectiveTaxPolicyDto : CreateBaseDtos
{
    [Required(ErrorMessage = "RetrospectiveTaxPolicy_TaxPolicyCode_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveTaxPolicy_TaxPolicyCode_MaxLen_50")]
    public string TaxPolicyCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "RetrospectiveTaxPolicy_TaxPolicyName_Required")]
    [StringLength(200, ErrorMessage = "RetrospectiveTaxPolicy_TaxPolicyName_MaxLen_200")]
    public string TaxPolicyName { get; set; } = string.Empty;

    /// <summary>
    /// HISTORIC_YEAR_WISE / CURRENT_YEAR_FOR_ALL_YEARS.
    /// Get valid choices (with display labels) from GET api/RetrospectiveTaxPolicy/rate-modes.
    /// </summary>
    [Required(ErrorMessage = "RetrospectiveTaxPolicy_RateMode_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveTaxPolicy_RateMode_MaxLen_50")]
    public string RateMode { get; set; } = string.Empty;

    /// <summary>
    /// HISTORIC_YEAR_WISE / CURRENT_YEAR_FOR_ALL_YEARS / FIXED_PERCENTAGE.
    /// Get valid choices (with display labels) from GET api/RetrospectiveTaxPolicy/percentage-modes.
    /// When FIXED_PERCENTAGE, FixedPercentage is also required.
    /// </summary>
    [Required(ErrorMessage = "RetrospectiveTaxPolicy_PercentageMode_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveTaxPolicy_PercentageMode_MaxLen_50")]
    public string PercentageMode { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "RetrospectiveTaxPolicy_FixedPercentage_Invalid")]
    public decimal? FixedPercentage { get; set; }

    [Range(1, 12, ErrorMessage = "RetrospectiveTaxPolicy_FinancialYearStartMonth_Invalid")]
    public byte FinancialYearStartMonth { get; set; } = 4;

    [Range(1, 31, ErrorMessage = "RetrospectiveTaxPolicy_FinancialYearStartDay_Invalid")]
    public byte FinancialYearStartDay { get; set; } = 1;

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class UpdateRetrospectiveTaxPolicyDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "RetrospectiveTaxPolicy_TaxPolicyCode_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveTaxPolicy_TaxPolicyCode_MaxLen_50")]
    public string TaxPolicyCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "RetrospectiveTaxPolicy_TaxPolicyName_Required")]
    [StringLength(200, ErrorMessage = "RetrospectiveTaxPolicy_TaxPolicyName_MaxLen_200")]
    public string TaxPolicyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "RetrospectiveTaxPolicy_RateMode_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveTaxPolicy_RateMode_MaxLen_50")]
    public string RateMode { get; set; } = string.Empty;

    [Required(ErrorMessage = "RetrospectiveTaxPolicy_PercentageMode_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveTaxPolicy_PercentageMode_MaxLen_50")]
    public string PercentageMode { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "RetrospectiveTaxPolicy_FixedPercentage_Invalid")]
    public decimal? FixedPercentage { get; set; }

    [Range(1, 12, ErrorMessage = "RetrospectiveTaxPolicy_FinancialYearStartMonth_Invalid")]
    public byte FinancialYearStartMonth { get; set; }

    [Range(1, 31, ErrorMessage = "RetrospectiveTaxPolicy_FinancialYearStartDay_Invalid")]
    public byte FinancialYearStartDay { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
