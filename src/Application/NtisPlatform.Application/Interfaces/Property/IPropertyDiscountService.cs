using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Interfaces.Property;

/// <summary>
/// Use-case boundary for the "Social Discount Attribute Management" capability of the
/// Property aggregate — recording which government-recognised social attributes (solar panel,
/// disability, senior citizen, etc.) apply to this property, along with their flag / numeric /
/// document values, so that the tax engine can apply the corresponding discount rates.
/// <para>
/// Each method is an explicit use-case operation (query or command). Business rules —
/// the restriction to discount-applicable attributes only, the per-item upsert decision,
/// and the attribute-id consistency check — are enforced by the implementation.
/// </para>
/// <para>
/// Tab naming ("Discount Details") is a Presentation-layer concern; inner layers refer to
/// this capability by its domain intent.
/// </para>
/// </summary>
public interface IPropertyDiscountService
{
    /// <summary>
    /// <b>Query</b> — Returns every discount-applicable social attribute with its current value
    /// and supporting document for a property, or <see langword="null"/> when the property does
    /// not exist. Controller maps null to 404.
    /// </summary>
    Task<PropertyDiscountInfoResponseDto?> GetDiscountDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <b>Command</b> — Upserts discount-applicable social-detail values for a property.
    /// Returns the refreshed projection, or <see langword="null"/> when the property is not found.
    /// </summary>
    /// <exception cref="NtisPlatform.Application.Exceptions.PropertyValidationException">
    /// Thrown when an attribute is not discount-applicable, a referenced detail record is not
    /// found for the property, or the detail record's attribute id does not match the supplied id.
    /// </exception>
    Task<PropertyDiscountInfoResponseDto?> UpdateDiscountDetailsAsync(int propertyId, UpsertPropertyDiscountInfoDto dto, CancellationToken cancellationToken = default);
}
