using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Core.Interfaces.Property;

/// <summary>
/// Persistence port for the "Residential Society and Wing Registration" use-case on the Property aggregate
/// (society name, address, manager/secretary contacts, and wing assignment).
/// <para>
/// Persistence only; business rules live in <c>IPropertySocietyService</c> and saving is
/// delegated to <c>IUnitOfWork</c>. Extends <see cref="IPropertyAggregateRepository"/> — the
/// shared aggregate-root load is inherited, not repeated.
/// </para>
/// </summary>
public interface IPropertySocietyRepository : IPropertyAggregateRepository
{
    /// <summary>
    /// Reads the society projection for a property, or null when the property is not found
    /// OR when the property has no linked society row. Callers that need to distinguish those
    /// two null cases should call <see cref="PropertyExistsAsync"/> separately.
    /// </summary>
    Task<PropertySocietyDetailsDto?> GetSocietyDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when an active, non-deleted property with the given id exists.
    /// Used by the service to distinguish "property not found" (→ 404) from
    /// "property found but no society yet" (→ empty DTO).
    /// </summary>
    Task<bool> PropertyExistsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Loads the active, non-deleted society row by its own id as a tracked entity, or null.</summary>
    Task<SocietyDetailsEntity?> GetSocietyByIdAsync(int societyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the active, non-deleted society row whose <c>PropertyId</c> matches the given property
    /// as a tracked entity. Used as the fallback lookup when the parent's <c>SocietyDetailId</c> FK
    /// is null or stale, preventing duplicate child rows in legacy data scenarios.
    /// </summary>
    Task<SocietyDetailsEntity?> GetSocietyByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Stages a new society row for insertion (persisted later via the unit of work).</summary>
    void AddSociety(SocietyDetailsEntity society);
}
