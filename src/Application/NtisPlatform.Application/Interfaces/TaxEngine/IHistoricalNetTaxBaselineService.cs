namespace NtisPlatform.Application.Interfaces.TaxEngine;

/// <summary>
/// Recomputes what a property's NETTAX baseline would have been for a specific finance year,
/// using the RV engine's year-scoped rate/tax-percentage master data (<c>RateEntity</c> /
/// <c>TaxPercentageMasterRVEntity</c>, both keyed by <c>AssessmentYearRangeEntity</c>), instead of
/// reading the one current <c>PolicyTaxDetails</c> snapshot every other finance year reuses.
/// Entirely read-only -- never writes to <c>PolicyTaxDetails</c>/<c>RVCalculationResults</c>/etc.
/// Backs the TAXATION_RATE_MODE / TAX_PERCENTAGE_MODE guideline settings' "historical year-wise"
/// and "fixed" options; callers pass concrete finance years, never a "current" sentinel.
/// </summary>
public interface IHistoricalNetTaxBaselineService
{
    /// <summary>
    /// Returns the recomputed (AnnualNetTax, GeneralTaxPortion) for <paramref name="propertyId"/>
    /// using <paramref name="rateFinanceYear"/>'s rate and either <paramref name="fixedTaxPercentage"/>
    /// (when set) or <paramref name="percentageFinanceYear"/>'s tax percentage. Returns null when the
    /// property has no active <c>PropertyDetails</c> rows or no rate/year-range can be resolved for
    /// <paramref name="rateFinanceYear"/> -- callers should fail open (leave that year unscaled)
    /// rather than treat null as zero.
    /// </summary>
    Task<(decimal AnnualNetTax, decimal GeneralTaxPortion)?> ComputeBaselineAsync(
        int propertyId,
        int rateFinanceYear,
        int percentageFinanceYear,
        decimal? fixedTaxPercentage,
        CancellationToken cancellationToken = default);
}
