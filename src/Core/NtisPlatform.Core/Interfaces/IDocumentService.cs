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
        string moduleCode,
        string referenceTableName,
        int? referenceTableId,
        Guid? referenceTableIdGuid,
        string? bindingPurpose,
        bool isPrimaryDocument,
        string? authModuleCode,
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
}
