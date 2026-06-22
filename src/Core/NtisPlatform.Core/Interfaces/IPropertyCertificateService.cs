using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Enums;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Service for PTIS.PropertyCertificates operations
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

    /// <summary>
    /// Gets all property certificates by property ID including inactive ones (IsActive = false).
    /// This is used for bulk operations where we need to check all certificates regardless of status.
    /// Still excludes certificates marked for deletion.
    /// </summary>
    /// <param name="propertyId">The property ID</param>
    /// <param name="includeOptions">Flags indicating which related entities to load</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all property certificates including inactive ones</returns>
    Task<List<PropertyCertificateEntity>> GetByPropertyIdIncludingInactiveAsync(
        int propertyId,
        PropertyCertificateIncludeOptions includeOptions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates property certificate metadata (number and date)
    /// </summary>
    Task UpdateAsync(
        int id,
        string? certificateNo,
        DateTime? issueDate,
        int updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles the enabled status of a property certificate
    /// </summary>
    Task ToggleEnabledAsync(
        int id,
        bool isEnabled,
        int updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a property certificate
    /// </summary>
    Task DeleteAsync(
        int id,
        int deletedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlinks the document binding from a property certificate.
    /// </summary>
    Task UnlinkDocumentBindingAsync(
        int propertyCertificateId,
        int updatedBy,
        CancellationToken cancellationToken = default);
}
