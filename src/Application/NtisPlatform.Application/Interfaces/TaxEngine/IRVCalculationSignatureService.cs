using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces.TaxEngine;

/// <summary>
/// Stores and retrieves the per-property RV calculation input signature used by
/// <see cref="RateableValueService"/> to skip recalculation when nothing relevant has changed.
/// </summary>
public interface IRVCalculationSignatureService
{
    /// <summary>Returns the stored signature row for a property, or null if none exists yet.</summary>
    Task<RVCalculationSignatureEntity?> GetAsync(int propertyId);

    /// <summary>
    /// Inserts or updates the stored signature for a property. Does NOT call SaveChanges.
    /// The caller is responsible for saving changes (typically as a best-effort follow-up save
    /// after the RV results themselves have already committed) -- signature bookkeeping is
    /// intentionally kept outside the main RV calculation transaction so that a failure here
    /// (e.g. a race on the unique index for concurrent recalculations of the same property)
    /// cannot roll back RV results that were already computed and persisted correctly.
    /// </summary>
    Task UpsertAsync(int propertyId, string signatureHash, DateTime calculatedAt);
}
