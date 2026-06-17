using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Interfaces.Property;

/// <summary>
/// Use-case boundary for the "Record Identification and Classification" capability of the
/// Property aggregate — the information that uniquely identifies a property and places it
/// within the municipality's administrative geography (ward, zone, tax-zone, category, type,
/// mouja) together with its physical description (plot dimensions, assessment sanitation
/// counts, society/wing assignment).
/// <para>
/// Each method is an explicit use-case operation (query or command), not a generic CRUD
/// delegate. Business rules — foreign-key validation, the upsert decisions for assessment /
/// plot / society child rows, transaction boundaries — are enforced by the implementation and
/// do not leak into the controller. The controller remains a thin HTTP adapter.
/// </para>
/// <para>
/// Tab naming ("Basic Details") is a Presentation-layer concern; inner layers refer to this
/// capability by its domain intent.
/// </para>
/// </summary>
public interface IPropertyBasicDetailsService
{
    /// <summary>
    /// <b>Query</b> — Returns the record-identification projection for a property,
    /// or <see langword="null"/> when the property does not exist. Controller maps null to 404.
    /// </summary>
    Task<PropertyBasicDetailsDto?> GetBasicDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <b>Command</b> — Updates the property's administrative geography and physical description
    /// across PropertyMast, assessment, plot and society rows atomically.
    /// Returns the refreshed projection, or <see langword="null"/> when the property is not found.
    /// </summary>
    /// <exception cref="NtisPlatform.Application.Exceptions.PropertyValidationException">
    /// Thrown when a referenced TaxZone, Ward or Mouja does not exist or is inactive.
    /// </exception>
    Task<PropertyBasicDetailsDto?> UpdateBasicDetailsAsync(int propertyId, UpdatePropertyBasicDetailsDto dto, CancellationToken cancellationToken = default);
}
