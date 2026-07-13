using NtisPlatform.Application.DTOs.Document;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Global application service for ALL document operations across every module.
/// All upload, view, download, retrieval, and delete operations are routed through
/// this single service — no module should access document or binding repositories directly.
/// </summary>
public interface IDocumentApplicationService
{
    /// <summary>
    /// Uploads a document with optional binding.
    /// Entity-specific side-effects (e.g. linking back DocumentBindingId to a business entity)
    /// are delegated to registered <c>IDocumentBindingHandler</c> implementations.
    /// </summary>
    Task<DocumentUploadResponseDto> UploadDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        DocumentUploadDto uploadDto,
        int uploadedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document details by GUID
    /// </summary>
    Task<DocumentDto?> GetDocumentAsync(Guid documentGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads document file (increments download count)
    /// </summary>
    Task<(Stream? FileStream, string FileName, string MimeType)> DownloadDocumentAsync(
        Guid documentGuid,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Views document file (without incrementing download count)
    /// </summary>
    Task<(Stream? FileStream, string FileName, string MimeType)> ViewDocumentAsync(
        Guid documentGuid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a document. Entity-specific cleanup is delegated to
    /// registered <c>IDocumentBindingHandler</c> implementations.
    /// </summary>
    Task<bool> DeleteDocumentAsync(Guid documentGuid, int deletedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates DocumentBinding.ReferenceTableId — used when the referencing entity ID
    /// is only available after the entity has been persisted.
    /// </summary>
    Task UpdateDocumentBindingReferenceAsync(
        int documentBindingId,
        int referenceTableId,
        int updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document by DocumentBindingId (O(1) access via binding).
    /// Returns the first document bound to the binding.
    /// </summary>
    Task<DocumentDto?> GetDocumentByBindingAsync(
        int documentBindingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document by reference table and ID (lookup via binding).
    /// Returns the first (most recent) document bound to the reference.
    /// </summary>
    Task<DocumentDto?> GetDocumentByReferenceAsync(
        int departmentId,
        int moduleId,
        string referenceTableName,
        int referenceTableId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active document bindings for a given reference table name and a list of
    /// reference IDs. Used by services that need to enrich multiple DTOs in a single database
    /// round-trip (e.g. <c>PropertySocialDetailsService.EnrichDtosAsync</c>).
    /// Returns binding-info records ordered by binding ID ascending.
    /// </summary>
    Task<IReadOnlyList<DocumentBindingInfoDto>> GetDocumentsByReferenceTableAsync(
        string referenceTableName,
        IReadOnlyList<int> referenceTableIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document metadata only (no file stream).
    /// Safe for bulk metadata calls and listing operations.
    /// </summary>
    Task<DocumentMetadataDto?> GetDocumentMetadataAsync(
        Guid documentGuid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes (marks for deletion) a single DocumentBinding by its ID.
    /// Modules that manage binding lifecycle (e.g. <c>PropertySocialDetailsService</c>)
    /// call this instead of accessing the binding repository directly.
    /// </summary>
    Task DeactivateDocumentBindingAsync(
        int documentBindingId,
        int updatedBy,
        CancellationToken cancellationToken = default);
}
