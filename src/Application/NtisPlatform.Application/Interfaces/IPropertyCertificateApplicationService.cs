using NtisPlatform.Application.DTOs.PropertyCertificate;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Application service for Property Certificate operations.
/// Aligned with Building Permissions and Certificates UI.
/// </summary>
public interface IPropertyCertificateApplicationService
{
    /// <summary>
    /// 1. GET - Gets all certificate types with their status for a property
    /// Shows which certificates exist (enabled/disabled) and which don't exist yet.
    /// Pass propertyDetailsId to scope to one floor's certificates; leave null for property-wise
    /// (PropertyDetailsId IS NULL) certificates only.
    /// </summary>
    Task<List<PropertyCertificateWithStatusDto>> GetCertificateTypesWithStatusAsync(
        int propertyId,
        CancellationToken cancellationToken = default,
        int? propertyDetailsId = null);

    /// <summary>
    /// 2. POST - Uploads PropertyCertificate with document
    /// Creates: PTIS.PropertyCertificates + CORE.Document + CORE.DocumentBinding
    /// </summary>
    Task<PropertyCertificateUploadResponseDto> UploadWithDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        int propertyId,
        int certificateTypeId,
        string? certificateNo,
        DateTime? issueDate,
        int uploadedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 3. POST - Replaces the document of an existing property certificate
    /// </summary>
    Task<PropertyCertificateUploadResponseDto> ReplaceDocumentAsync(
        int propertyCertificateId,
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        int uploadedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 4. POST - Bulk save all certificates for a property (single Save button)
    /// Matches UI where user can enable/disable multiple certificates and save all at once
    /// </summary>
    Task<PropertyCertificateBulkSaveResponseDto> BulkSaveAllAsync(
        PropertyCertificateBulkSaveDto bulkDto,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the document associated with a property certificate.
    /// </summary>
    Task DeleteDocumentAsync(
        int propertyCertificateId,
        int deletedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a property certificate's metadata row by (PropertyId, CertificateTypeId,
    /// PropertyDetailsId). Resolves to the matching active row; if it has an attached document,
    /// that document is cascade-deleted first (unlinked and soft-deleted, the same steps
    /// <see cref="DeleteDocumentAsync"/> performs) so metadata deletion never leaves an orphaned,
    /// still-active document behind. Then soft-deletes the certificate row itself and re-triggers
    /// the certificate-change pipeline (RV refresh then Occupation Tax apply) exactly like
    /// save/update, since removing a certificate can change which policy applies to the property.
    /// Throws <see cref="NtisPlatform.Core.Exceptions.PropertyCertificateNotFoundException"/> if no
    /// matching row exists.
    /// </summary>
    /// <param name="propertyDetailsId">Null for the property-wise certificate; the floor's PropertyDetailsId for a floor-wise one.</param>
    Task DeleteCertificateByTypeAsync(
        int propertyId,
        int certificateTypeId,
        int? propertyDetailsId,
        int deletedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically replaces an existing certificate row with a new one under a DIFFERENT
    /// (PropertyId, CertificateTypeId, newPropertyDetailsId) scope -- e.g. moving a certificate
    /// from property-wide to a specific floor, or between floors -- in a single call. Unlike
    /// <see cref="DeleteCertificateByTypeAsync"/> followed by a separate save call, this method
    /// suppresses the recalculation both the internal delete and the internal create would
    /// otherwise each publish on their own, and instead publishes exactly ONE
    /// PropertyCertificateChangedEvent after both steps complete, so the Occupation Tax pipeline
    /// only ever sees the FINAL state (new certificate present) -- never the momentarily-certificate-
    /// less intermediate state a delete-then-create over two separate calls would expose, which
    /// could otherwise fall back to Electric Bill (or no tax at all) with the wrong retro years for
    /// the duration between the two calls.
    /// If the old certificate's scope and the new scope are the SAME (PropertyDetailsId unchanged),
    /// prefer <see cref="SaveCertificateAsync"/> instead -- it already updates the existing row in
    /// place with zero delete step and zero intermediate state, and is simpler. Use this method only
    /// when the certificate's floor/property-wide scope is actually changing.
    /// Throws <see cref="NtisPlatform.Core.Exceptions.PropertyCertificateNotFoundException"/> if no
    /// existing row matches (propertyId, certificateTypeId, oldPropertyDetailsId).
    /// </summary>
    /// <param name="propertyId">The property ID</param>
    /// <param name="certificateTypeId">The certificate type ID</param>
    /// <param name="oldPropertyDetailsId">Current scope of the certificate being replaced (null for property-wise).</param>
    /// <param name="newPropertyDetailsId">New scope for the replacement certificate (null for property-wise).</param>
    /// <param name="newCertificateNo">The replacement certificate's number</param>
    /// <param name="newIssueDate">The replacement certificate's issue date</param>
    /// <param name="userId">User ID performing the replacement</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The new certificate's PropertyCertificateId.</returns>
    Task<int> ReplaceCertificateByTypeAsync(
        int propertyId,
        int certificateTypeId,
        int? oldPropertyDetailsId,
        int? newPropertyDetailsId,
        string? newCertificateNo,
        DateTime? newIssueDate,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// GET - Floor-wise certificate display for the Building Permission tab.
    /// Returns all floors of the property, the selected floor highlighted via isSelected,
    /// each floor's certificate-applicable status and CC/OC/Electric-Bill dates, and the
    /// property-wise certificates (PropertyDetailsId IS NULL).
    /// </summary>
    Task<FloorCertificatesResponseDto> GetFloorCertificatesAsync(
        int propertyId,
        int? selectedPropertyDetailsId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// POST - Building Permission tab "Save" button. Saves/updates certificate metadata only —
    /// document upload always goes through the Global Document endpoint
    /// (<c>POST /api/documents/upload</c> with ReferenceTableName=PropertyCertificates,
    /// ReferenceTableId=the id this call returns), which auto-links DocumentBindingId back onto
    /// this row. For taxable certificate types (IsTaxable=1), triggers Occupation Tax
    /// recalculation (RV refresh then Occupation Tax apply) via PropertyCertificateChangedEvent.
    /// </summary>
    Task<SaveCertificateResponseDto> SaveCertificateAsync(
        SaveCertificateRequestDto request,
        int userId,
        CancellationToken cancellationToken = default);
}

