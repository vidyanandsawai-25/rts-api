namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxPolicy;

/// <summary>
/// One dropdown choice for a CHECK-constraint-backed enum field on RetrospectiveTaxPolicy
/// (RateMode / PercentageMode). Not a DB-backed lookup table — the values mirror the
/// CK_RetrospectiveTaxPolicy_RateMode / CK_RetrospectiveTaxPolicy_PercentageMode constraints
/// in the PTIS schema script, so the UI has a single source of truth for what's valid and the
/// API developer has a single place to update if the constraint ever changes.
/// </summary>
public class RetrospectiveTaxPolicyOptionDto
{
    /// <summary>The value to send back in RateMode/PercentageMode on Create/Update, e.g. "HISTORIC_YEAR_WISE".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display text for the dropdown, e.g. "Historical year-wise rate".</summary>
    public string Label { get; set; } = string.Empty;
}
