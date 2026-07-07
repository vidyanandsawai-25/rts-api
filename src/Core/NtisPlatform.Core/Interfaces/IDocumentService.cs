using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Service for CORE.Document and CORE.DocumentBinding operations ONLY
/// Does NOT handle business entities like PropertyCertificate
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Creates a document record in CORE.Document
    /// </summary>
    Task<(int DocumentId, Guid DocumentGuid)> CreateDocumentAsync(
        int uploadedBy,
        int? ownerUserId,
        string fileName,
        string originalFileName,
        string fileExtension,
        string mimeType,
        long fileSizeBytes,
        string storagePath,
        string? thumbnailPath,
        string? checksumSha256,
        string? documentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a document binding record in CORE.DocumentBinding
    /// </summary>
    Task<int> CreateDocumentBindingAsync(
        int documentId,
        int departmentId,
        int moduleId,
        string referenceTableName,
        int? referenceTableId,
        Guid? referenceTableIdGuid,
        string referencePropertyName,
        string? bindingPurpose,
        bool isPrimaryDocument,
        int? authDepartmentId,
        int? authReferenceId,
        int createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates DocumentBinding.ReferenceTableId
    /// </summary>
    Task UpdateDocumentBindingReferenceAsync(
        int documentBindingId,
        int referenceTableId,
        int updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document by GUID
    /// </summary>
    Task<DocumentEntity?> GetDocumentByGuidAsync(Guid documentGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document by ID
    /// </summary>
    Task<DocumentEntity?> GetDocumentByIdAsync(int documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a document
    /// </summary>
    Task<bool> DeleteDocumentAsync(Guid documentGuid, int deletedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments download count
    /// </summary>
    Task IncrementDownloadCountAsync(Guid documentGuid, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents for a reference entity
    /// </summary>
    Task<List<DocumentEntity>> GetDocumentsByReferenceAsync(
        string referenceTableName,
        int? referenceTableId,
        Guid? referenceTableIdGuid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document binding by ID
    /// </summary>
    Task<DocumentBindingEntity?> GetDocumentBindingByIdAsync(
        int documentBindingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document by DocumentBindingId (O(1) access via binding).
    /// </summary>
    Task<DocumentEntity?> GetDocumentByBindingIdAsync(
        int documentBindingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents by department, module, reference table, and reference ID.
    /// Used for by-reference lookups.
    /// </summary>
    Task<List<DocumentEntity>> GetDocumentsByDepartmentModuleReferenceAsync(
        int departmentId,
        int moduleId,
        string referenceTableName,
        int? referenceTableId,
        Guid? referenceTableIdGuid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets soft-deleted documents marked for deletion before cutoff date.
    /// Used by background cleanup service to find orphaned files to delete.
    /// </summary>
    Task<List<DocumentEntity>> GetSoftDeletedDocumentsAsync(
        DateTime cutoffDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard deletes a document record from the database.
    /// The caller is responsible for deleting the underlying storage object before invoking this method.
    /// </summary>
    Task HardDeleteDocumentAsync(int documentId, CancellationToken cancellationToken = default);
}
