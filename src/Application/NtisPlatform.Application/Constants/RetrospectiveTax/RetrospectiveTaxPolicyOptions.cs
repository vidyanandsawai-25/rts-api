using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxPolicy;

namespace NtisPlatform.Application.Constants.RetrospectiveTax;

/// <summary>
/// Static option lists for the "Taxation Rate &amp; Percentage" screen's RateMode/PercentageMode
/// dropdowns. Kept in one place so the API contract for these two fields (code sent to the
/// server + label shown to the user) can't drift out of sync between backend and frontend.
/// </summary>
public static class RetrospectiveTaxPolicyOptions
{
    /// <summary>Options for RetrospectiveTaxPolicy.RateMode ("Taxation rate" dropdown).</summary>
    public static IReadOnlyList<RetrospectiveTaxPolicyOptionDto> RateModes { get; } = new[]
    {
        new RetrospectiveTaxPolicyOptionDto { Code = "HISTORIC_YEAR_WISE", Label = "Historical year-wise rate" },
        new RetrospectiveTaxPolicyOptionDto { Code = "CURRENT_YEAR_FOR_ALL_YEARS", Label = "Current-year rate for all years" },
    };

    /// <summary>Options for RetrospectiveTaxPolicy.PercentageMode ("Taxation percentage" dropdown).</summary>
    public static IReadOnlyList<RetrospectiveTaxPolicyOptionDto> PercentageModes { get; } = new[]
    {
        new RetrospectiveTaxPolicyOptionDto { Code = "HISTORIC_YEAR_WISE", Label = "Historical year-wise percentage" },
        new RetrospectiveTaxPolicyOptionDto { Code = "CURRENT_YEAR_FOR_ALL_YEARS", Label = "Current-year percentage for all years" },
        new RetrospectiveTaxPolicyOptionDto { Code = "FIXED_PERCENTAGE", Label = "Fixed percentage for all years" },
    };
}
