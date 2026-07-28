using NtisPlatform.Application.Services.TaxEngine.OccupationTax;

namespace NtisPlatform.Application.Interfaces.TaxEngine;

/// <summary>
/// Applies Occupation Tax for a property. This is step 2 of the certificate-change pipeline and
/// must run only after the Rateable Value (and NETTAX) have been refreshed.
/// </summary>
public interface IOccupationTaxService
{
    /// <summary>
    /// Loads the property's refreshed NETTAX and certificate dates, runs the Occupation Tax engine,
    /// and persists the resulting TransMast rows (current finance year plus any retrospective years).
    /// </summary>
    /// <param name="propertyId">Property to apply Occupation Tax for.</param>
    /// <param name="userId">User attributed to the resulting writes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ApplyAsync(int propertyId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the Occupation Tax result WITHOUT persisting to database. Used for preview/validation.
    /// Throws <see cref="InvalidOperationException"/> if the computation is rejected (see
    /// <see cref="OccupationTaxResult.RejectionReason"/>).
    /// </summary>
    /// <param name="propertyId">Property to preview.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current year and retrospective-year calculation result (no database write).</returns>
    Task<OccupationTaxResult> PreviewAsync(int propertyId, CancellationToken cancellationToken = default);
}
