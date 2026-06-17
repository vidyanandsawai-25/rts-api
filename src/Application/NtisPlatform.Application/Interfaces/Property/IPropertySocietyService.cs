using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Interfaces.Property;

/// <summary>
/// Use-case boundary for the "Residential Society and Wing Registration" capability of the
/// Property aggregate — the cooperative housing society that manages the building in which
/// the property resides, together with the wing assignment within that society.
/// <para>
/// Each method is an explicit use-case operation (query or command). Business rules —
/// Wing FK validation, the get-or-create society decision, the parent-id fallback lookup
/// to prevent duplicate rows in legacy data, and the two-save transaction boundary — are
/// enforced by the implementation.
/// </para>
/// <para>
/// Tab naming ("Society Details") is a Presentation-layer concern; inner layers refer to
/// this capability by its domain intent.
/// </para>
/// </summary>
public interface IPropertySocietyService
{
    /// <summary>
    /// <b>Query</b> — Returns the society projection for a property,
    /// or <see langword="null"/> when the property does not exist.
    /// Returns an empty DTO (not null) when the property exists but has no society row yet.
    /// Controller maps null to 404.
    /// </summary>
    Task<PropertySocietyDetailsDto?> GetSocietyDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <b>Command</b> — Upserts the society row for a property (creating a new row and
    /// linking the parent FK when none exists).
    /// Returns the refreshed projection, or <see langword="null"/> when the property is not found.
    /// </summary>
    /// <exception cref="NtisPlatform.Application.Exceptions.PropertyValidationException">
    /// Thrown when the referenced Wing does not exist or is inactive.
    /// </exception>
    Task<PropertySocietyDetailsDto?> UpdateSocietyDetailsAsync(int propertyId, UpdatePropertySocietyDetailsDto dto, CancellationToken cancellationToken = default);
}
