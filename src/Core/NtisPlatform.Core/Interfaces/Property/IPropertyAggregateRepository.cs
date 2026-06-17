using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Interfaces.Property;

/// <summary>
/// Root persistence port for the Property aggregate: resolves the active aggregate root
/// that every mutation use-case must load before applying any state changes.
/// <para>
/// All per-tab repository ports extend this interface so that the single
/// <see cref="GetActivePropertyAsync"/> signature is declared once and inherited — not
/// copy-pasted across every feature port. Concrete implementations inherit the shared
/// implementation from <c>PropertyRepositoryBase</c>.
/// </para>
/// </summary>
public interface IPropertyAggregateRepository
{
    /// <summary>
    /// Returns the tracked <c>PropertyMast</c> row when it is active and not soft-deleted,
    /// or <see langword="null"/> when the property does not exist, is inactive, or is marked
    /// for deletion. Callers map <see langword="null"/> to a 404 response; no exception is thrown.
    /// </summary>
    Task<PropertyEntity?> GetActivePropertyAsync(int propertyId, CancellationToken cancellationToken = default);
}
