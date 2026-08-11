using NtisPlatform.Application.DTOs.PropertyKyc;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Interfaces.Property;

/// <summary>
/// Use-case boundary for the "Owner and Occupier Registration" capability of the Property aggregate
/// — the KYC (Know Your Customer) information that records who owns and who occupies the property:
/// owner / occupier names (vernacular + English), address, contact details, owner-type
/// classification and Aadhar reference.
/// <para>
/// Each method is an explicit use-case operation (query or command). Business rules — the upsert
/// decision for the assessment row that stores owner-type and Aadhar, the transaction boundary
/// protecting the two-entity save — are enforced by the implementation.
/// </para>
/// <para>
/// Tab naming ("KYC Details") is a Presentation-layer concern; inner layers refer to this
/// capability by its domain intent.
/// </para>
/// </summary>
public interface IPropertyKycService
{
    /// <summary>
    /// <b>Query</b> — Returns the owner/occupier registration projection for a property,
    /// or <see langword="null"/> when the property does not exist. Controller maps null to 404.
    /// </summary>
    Task<PropertyKycDetailsDto?> GetKycDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <b>Command</b> — Updates owner/occupier details on PropertyMast and the associated
    /// assessment row atomically.
    /// Returns the refreshed projection, or <see langword="null"/> when the property is not found.
    /// </summary>
    Task<PropertyKycDetailsDto?> UpdateKycDetailsAsync(int propertyId, UpdatePropertyKycDetailsDto dto, CancellationToken cancellationToken = default);

    Task<PropertyKycDetailsCommonDto?> GetKycDetailsCommon(
        PropertyKycDetailsQueryParameters queryParameters,
        CancellationToken cancellationToken = default);
}
