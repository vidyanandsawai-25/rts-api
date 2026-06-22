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
    /// Shows which certificates exist (enabled/disabled) and which don't exist yet
    /// </summary>
    Task<List<PropertyCertificateWithStatusDto>> GetCertificateTypesWithStatusAsync(
        int propertyId,
        CancellationToken cancellationToken = default);

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
}

