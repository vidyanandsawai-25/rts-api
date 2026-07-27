namespace NtisPlatform.Application.Interfaces.TaxEngine;

/// <summary>
/// Client for the Rateable Value (RV) recalculation API. Refreshing RV updates the property's
/// PropertyTaxDetails NETTAX and PropertyTaxCalculationRVResults, which the Occupation Tax engine
/// then consumes. This is step 1 of the certificate-change pipeline and must complete before the
/// Occupation Tax engine runs.
/// </summary>
public interface IRateableValueApiClient
{
    /// <summary>
    /// Recalculates and persists the Rateable Value (and therefore NETTAX) for the given property.
    /// </summary>
    /// <param name="propertyId">Property whose RV should be refreshed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecalculateAsync(int propertyId, CancellationToken cancellationToken = default);
}
