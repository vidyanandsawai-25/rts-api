using NtisPlatform.Application.DTOs.PropertyCertificate;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Application service for PropertyCertificate operations
/// SEPARATE from Document service
/// </summary>
public interface IPropertyCertificateApplicationService
{
    /// <summary>
    /// Uploads PropertyCertificate with document
    /// Creates: PTIS.PropertyCertificate + CORE.Document + CORE.DocumentBinding
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
    /// Gets PropertyCertificates by PropertyId
    /// </summary>
    Task<List<PropertyCertificateDto>> GetByPropertyIdAsync(
        int propertyId,
        CancellationToken cancellationToken = default);
}
