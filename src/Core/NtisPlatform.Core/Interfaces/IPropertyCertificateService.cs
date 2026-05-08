using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Enums;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Service for PTIS.PropertyCertificate operations
/// </summary>
public interface IPropertyCertificateService
{
    /// <summary>
    /// Creates a property certificate without document binding.
    /// Use this when you need to create the certificate before the DocumentBinding exists.
    /// </summary>
    Task<int> CreateAsync(
        int propertyId,
        int certificateTypeId,
        string? certificateNo,
        DateTime? issueDate,
        int createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a property certificate with document binding in a single operation.
    /// Optimized to eliminate separate update call, reducing database roundtrips.
    /// </summary>
    Task<int> CreateWithDocumentAsync(
        int propertyId,
        int certificateTypeId,
        int documentBindingId,
        string? certificateNo,
        DateTime? issueDate,
        int createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the document binding ID for an existing property certificate.
    /// </summary>
    Task UpdateDocumentBindingAsync(
        int propertyCertificateId,
        int documentBindingId,
        int updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a property certificate by ID without any related data.
    /// Use this for best performance when related entities are not needed.
    /// </summary>
    Task<PropertyCertificateEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a property certificate by ID with specified related entities.
    /// Provides flexible loading strategy for optimal performance.
    /// </summary>
    /// <param name="id">The property certificate ID</param>
    /// <param name="includeOptions">Flags indicating which related entities to load</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property certificate with requested related data, or null if not found</returns>
    Task<PropertyCertificateEntity?> GetByIdAsync(
        int id,
        PropertyCertificateIncludeOptions includeOptions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets property certificates by property ID with full related data (legacy method).
    /// Note: Always loads CertificateType, DocumentBinding, and Document.
    /// Consider using overload with includeOptions for better performance.
    /// </summary>
    Task<List<PropertyCertificateEntity>> GetByPropertyIdAsync(
        int propertyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets property certificates by property ID with specified related entities.
    /// Provides flexible loading strategy for optimal performance.
    /// </summary>
    /// <param name="propertyId">The property ID</param>
    /// <param name="includeOptions">Flags indicating which related entities to load</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of property certificates with requested related data</returns>
    Task<List<PropertyCertificateEntity>> GetByPropertyIdAsync(
        int propertyId,
        PropertyCertificateIncludeOptions includeOptions,
        CancellationToken cancellationToken = default);
}
