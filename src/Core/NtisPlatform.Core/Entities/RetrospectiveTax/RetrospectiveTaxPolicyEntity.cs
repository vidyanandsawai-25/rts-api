using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Common taxation rate/percentage policy for the current ULB.
/// Maps to the "Taxation Rate &amp; Percentage" screen section. Only one policy should be active
/// at a time (enforced via a filtered unique index on IsActive).
/// </summary>
[Table("RetrospectiveTaxPolicy", Schema = "PTIS")]
public class RetrospectiveTaxPolicyEntity : BaseEntity
{
    public string TaxPolicyCode { get; set; } = string.Empty;

    public string TaxPolicyName { get; set; } = string.Empty;

    /// <summary>HISTORIC_YEAR_WISE / CURRENT_YEAR_FOR_ALL_YEARS</summary>
    public string RateMode { get; set; } = string.Empty;

    /// <summary>HISTORIC_YEAR_WISE / CURRENT_YEAR_FOR_ALL_YEARS / FIXED_PERCENTAGE</summary>
    public string PercentageMode { get; set; } = string.Empty;

    public decimal? FixedPercentage { get; set; }

    public byte FinancialYearStartMonth { get; set; } = 4;

    public byte FinancialYearStartDay { get; set; } = 1;

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }
}
