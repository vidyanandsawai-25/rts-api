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
    /// <param name="propertyId">The property ID</param>
    /// <param name="certificateTypeId">The certificate type ID</param>
    /// <param name="certificateNo">The certificate number</param>
    /// <param name="issueDate">The certificate issue date</param>
    /// <param name="createdBy">User ID creating this certificate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="propertyDetailsId">Null for the property-wise certificate; the floor's PropertyDetailsId for a floor-wise one.</param>
    /// <param name="suppressRecalculation">
    /// When true, skips publishing the Application-layer PropertyCertificateChangedEvent (not
    /// cref-linkable from Core, which Application depends on, not the reverse) for this call even if
    /// the certificate type is taxable and the guideline allows it. Used by
    /// bulk-save callers that process several certificates for the same property in one request and
    /// need to trigger the RV-refresh-then-Occupation-Tax pipeline exactly once, after every
    /// certificate in the batch has been saved, instead of once per certificate.
    /// </param>
    Task<int> CreateAsync(
        int propertyId,
        int certificateTypeId,
        string? certificateNo,
        DateTime? issueDate,
        int createdBy,
        CancellationToken cancellationToken = default,
        int? propertyDetailsId = null,
        bool suppressRecalculation = false);

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
        CancellationToken cancellationToken = default,
        int? propertyDetailsId = null);

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
    /// <param name="id">The property certificate ID</param>
    /// <param name="certificateNo">The certificate number</param>
    /// <param name="issueDate">The certificate issue date</param>
    /// <param name="updatedBy">User ID updating this certificate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="suppressRecalculation">See <see cref="CreateAsync"/>.</param>
    Task UpdateAsync(
        int id,
        string? certificateNo,
        DateTime? issueDate,
        int updatedBy,
        CancellationToken cancellationToken = default,
        bool suppressRecalculation = false);

    /// <summary>
    /// Toggles the enabled status of a property certificate
    /// </summary>
    /// <param name="id">The property certificate ID</param>
    /// <param name="isEnabled">The new enabled status</param>
    /// <param name="updatedBy">User ID updating this certificate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="suppressRecalculation">See <see cref="CreateAsync"/>.</param>
    Task ToggleEnabledAsync(
        int id,
        bool isEnabled,
        int updatedBy,
        CancellationToken cancellationToken = default,
        bool suppressRecalculation = false);

    /// <summary>
    /// Soft deletes a property certificate
    /// </summary>
    /// <param name="id">The property certificate ID</param>
    /// <param name="deletedBy">User ID deleting this certificate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="suppressRecalculation">See <see cref="CreateAsync"/>. Used by
    /// PropertyCertificateApplicationService.ReplaceCertificateByTypeAsync, which deletes the old
    /// row and creates the replacement in one call, then publishes exactly one recalculation event
    /// against the final state -- never by the general-purpose, independently-callable
    /// DeleteCertificateByTypeAsync, which must keep recalculating/cleaning up immediately on every
    /// standalone delete.</param>
    Task DeleteAsync(
        int id,
        int deletedBy,
        CancellationToken cancellationToken = default,
        bool suppressRecalculation = false);

    /// <summary>
    /// Unlinks the document binding from a property certificate.
    /// </summary>
    Task UnlinkDocumentBindingAsync(
        int propertyCertificateId,
        int updatedBy,
        CancellationToken cancellationToken = default);
}
