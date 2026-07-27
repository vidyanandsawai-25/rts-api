namespace NtisPlatform.Application.Services.TaxEngine.OccupationTax;

/// <summary>
/// Which certificate condition governs the Occupation Tax application for a property,
/// per business rule BR2.
/// </summary>
public enum OccupationCondition
{
    /// <summary>
    /// Neither an Occupation Certificate (OC) nor a Completion Certificate (CC) date is present.
    /// Tax is applied from the Electricity Bill date (normalized to the finance-year start).
    /// </summary>
    ElectricityBill = 0,

    /// <summary>
    /// Only a Completion Certificate (CC) date is present. The CC baseline applies throughout.
    /// </summary>
    CompletionCertificate = 1,

    /// <summary>
    /// Only an Occupation Certificate (OC) date is present. The OC condition applies throughout.
    /// </summary>
    OccupationCertificate = 2,
}

/// <summary>
/// Immutable configuration for the Occupation Tax engine. All figures are annual and driven
/// by the refreshed PropertyTaxDetails NETTAX. Kept as an options object so retro cut-off and
/// component splits are config-driven (BR4).
/// </summary>
public sealed class OccupationTaxOptions
{
    /// <summary>
    /// Approved annual NETTAX for the property (e.g. 36,500). This is split into a single
    /// General-tax portion plus a fixed number of equal components.
    /// </summary>
    public decimal AnnualNetTax { get; init; }

    /// <summary>
    /// The General-tax portion of the annual NETTAX (e.g. 21,900). The remaining NETTAX is
    /// divided equally across <see cref="ComponentCount"/> components.
    /// </summary>
    public decimal GeneralTaxPortion { get; init; }

    /// <summary>
    /// Number of equal secondary tax components the non-general portion is split into
    /// (e.g. 4 components of 3,650 each).
    /// </summary>
    public int ComponentCount { get; init; } = 4;

    /// <summary>
    /// The Completion Certificate baseline multiplier applied to the annual NETTAX. Defaults to
    /// 1.0 (no-op) -- the live path always overrides this from
    /// CertificateTaxGuideline.CC_PERIOD_MULTIPLIER, which defaults to 1 for Thane; do not assume
    /// a non-1.0 value here without explicit business sign-off.
    /// </summary>
    public decimal CompletionCertificateMultiplier { get; init; } = 1.0m;

    /// <summary>
    /// Divisor used to derive the annual floor from the annual NETTAX (e.g. 2 => half NETTAX).
    /// </summary>
    public int FloorDivisor { get; init; } = 2;

    /// <summary>
    /// Number of finance years to look back when no explicit retro cut-off date is configured
    /// (BR4 default cut-off). With CurrentFY 2026 and a value of 6, retro spans FY2020..FY2025.
    /// </summary>
    public int DefaultRetroLookbackYears { get; init; } = 6;

    /// <summary>
    /// Optional configured retro cut-off date. When present, retro application starts at the
    /// finance year containing this date and overrides <see cref="DefaultRetroLookbackYears"/>
    /// (BR4 config-driven cut-off).
    /// </summary>
    public DateTime? RetroCutoffDate { get; init; }

    /// <summary>
    /// The CC baseline value (annual NETTAX x multiplier). Convenience accessor.
    /// </summary>
    public decimal CompletionCertificateBaseline => AnnualNetTax * CompletionCertificateMultiplier;

    /// <summary>
    /// The annual floor value (annual NETTAX / divisor). Convenience accessor.
    /// </summary>
    public decimal AnnualFloor => FloorDivisor == 0 ? 0m : AnnualNetTax / FloorDivisor;
}

/// <summary>
/// Everything the engine needs to know about a single property to compute Occupation Tax.
/// Certificate dates drive BR2 condition selection; NETTAX drives the amounts.
/// </summary>
public sealed class OccupationTaxInput
{
    /// <summary>Property identifier the taxes are computed for.</summary>
    public int PropertyId { get; init; }

    /// <summary>Occupation Certificate issue date, if any.</summary>
    public DateTime? OccupationCertificateDate { get; init; }

    /// <summary>Completion Certificate issue date, if any.</summary>
    public DateTime? CompletionCertificateDate { get; init; }

    /// <summary>Electricity Bill date, used only when neither OC nor CC is present (BR2).</summary>
    public DateTime? ElectricityBillDate { get; init; }

    /// <summary>Configuration and approved NETTAX figures for this property.</summary>
    public OccupationTaxOptions Options { get; init; } = new();
}

/// <summary>
/// The Occupation Tax amount computed for one finance year.
/// </summary>
public sealed class OccupationTaxYearResult
{
    /// <summary>Finance year (start year), e.g. 2026 for 01-Apr-2026..31-Mar-2027.</summary>
    public int FinanceYear { get; init; }

    /// <summary>Start date of the finance year.</summary>
    public DateTime FinanceYearStart { get; init; }

    /// <summary>Inclusive end date of the finance year.</summary>
    public DateTime FinanceYearEnd { get; init; }

    /// <summary>General-tax portion for this year (may be prorated).</summary>
    public decimal GeneralTax { get; init; }

    /// <summary>Per-component amount for this year (may be prorated).</summary>
    public decimal ComponentTax { get; init; }

    /// <summary>Number of secondary components.</summary>
    public int ComponentCount { get; init; }

    /// <summary>True when this year was prorated by day count rather than applied in full.</summary>
    public bool IsProrated { get; init; }

    /// <summary>Number of chargeable days used for proration (equals FY length when full).</summary>
    public int ChargeableDays { get; init; }

    /// <summary>True when a leap-year add-back was applied to a full year (BR7).</summary>
    public bool LeapAddbackApplied { get; init; }

    /// <summary>The total NETTAX for this finance year (General + ComponentCount x Component).</summary>
    public decimal NetTax => GeneralTax + (ComponentCount * ComponentTax);
}

/// <summary>
/// Outcome of an Occupation Tax computation for a property, spanning the current finance year
/// plus any retrospective years (BR4/BR5).
/// </summary>
public sealed class OccupationTaxResult
{
    /// <summary>Property the result applies to.</summary>
    public int PropertyId { get; init; }

    /// <summary>False when a precondition failed (BR6); no writes should occur.</summary>
    public bool IsValid { get; init; }

    /// <summary>Human-readable reason when <see cref="IsValid"/> is false.</summary>
    public string? RejectionReason { get; init; }

    /// <summary>The certificate condition that governed the computation (BR2).</summary>
    public OccupationCondition Condition { get; init; }

    /// <summary>The current finance-year result (full annual TransMast amount).</summary>
    public OccupationTaxYearResult? CurrentYear { get; init; }

    /// <summary>Retrospective finance-year results, oldest first (BR4/BR5). Empty when none.</summary>
    public IReadOnlyList<OccupationTaxYearResult> RetroYears { get; init; }
        = Array.Empty<OccupationTaxYearResult>();

    /// <summary>Sum of NETTAX across all retro years (the roll-up figure, BR5).</summary>
    public decimal RetroRollUp => RetroYears.Sum(y => y.NetTax);

    /// <summary>Helper to build a rejected result carrying a reason (BR6).</summary>
    public static OccupationTaxResult Rejected(int propertyId, string reason) => new()
    {
        PropertyId = propertyId,
        IsValid = false,
        RejectionReason = reason,
    };
}
