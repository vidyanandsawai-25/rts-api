using NtisPlatform.Application.DTOs.Document;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Application service for CORE.Document operations ONLY
/// Does NOT handle business entities - use separate services for those
/// </summary>
public interface IDocumentApplicationService
{
    /// <summary>
    /// Uploads a document with optional binding
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
    /// Downloads document file
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
    /// Soft deletes a document
    /// </summary>
    Task<bool> DeleteDocumentAsync(Guid documentGuid, int deletedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates DocumentBinding.ReferenceTableId
    /// </summary>
    Task UpdateDocumentBindingReferenceAsync(
        int documentBindingId,
        int referenceTableId,
        int updatedBy,
        CancellationToken cancellationToken = default);
}
