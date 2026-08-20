using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxPolicy;

/// <summary>
/// Request body for the "Taxation Rate &amp; Percentage" screen's "Save Taxation" button. This
/// section is common for every rule under the current ULB, so there is only ever one active
/// policy row (enforced by UX_RetrospectiveTaxPolicy_OneActive) — the caller doesn't need to know
/// its Id. Saving updates that single active row in place, or creates it on first use.
/// TaxPolicyCode/TaxPolicyName aren't shown on this screen; when omitted on first creation they
/// default to "DEFAULT" / "Default Taxation Policy", and existing values are left unchanged on
/// every later save.
/// </summary>
public class SaveRetrospectiveTaxPolicyDto
{
    [StringLength(50, ErrorMessage = "RetrospectiveTaxPolicy_TaxPolicyCode_MaxLen_50")]
    public string? TaxPolicyCode { get; set; }

    [StringLength(200, ErrorMessage = "RetrospectiveTaxPolicy_TaxPolicyName_MaxLen_200")]
    public string? TaxPolicyName { get; set; }

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

    /// <summary>Id of the user performing this save. Stamped as CreatedBy the first time this policy is created, and as UpdatedBy on every later save.</summary>
    public int? UpdatedBy { get; set; }
}
