using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Core.Interfaces.Property;

/// <summary>
/// Persistence port for the "Social Discount Attribute Management" use-case on the Property aggregate
/// (discount-applicable social attributes and their current flag / numeric / document values).
/// <para>
/// Persistence only; business rules live in <c>IPropertyDiscountService</c> and saving is delegated
/// to <c>IUnitOfWork</c>. Extends <see cref="IPropertyAggregateRepository"/> — the shared
/// aggregate-root load is inherited, not repeated.
/// </para>
/// </summary>
public interface IPropertyDiscountRepository : IPropertyAggregateRepository
{
    /// <summary>Reads the discount projection: every discount-applicable attribute with its current value and document, or null when the property is not found.</summary>
    Task<PropertyDiscountInfoResponseDto?> GetDiscountDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Loads the property's active social-detail rows as tracked entities.</summary>
    Task<List<PropertySocialDetailsEntity>> GetActiveSocialDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Loads all social-detail rows for the property, including soft-deleted ones, as tracked entities.</summary>
    Task<List<PropertySocialDetailsEntity>> GetSocialDetailsIncludingDeletedAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Returns the set of social-attribute ids that are active and discount-applicable (used to authorize updates).</summary>
    Task<HashSet<int>> GetDiscountApplicableAttributeIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>Stages a new social-detail row for insertion (persisted later via the unit of work).</summary>
    void AddSocialDetail(PropertySocialDetailsEntity socialDetail);
}
