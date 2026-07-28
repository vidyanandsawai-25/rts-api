using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyCertificate;

/// <summary>
/// DTO for PropertyCertificate with certificate type information
/// </summary>
public class PropertyCertificateDto
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int CertificateTypeId { get; set; }
    public string? CertificateTypeCode { get; set; }
    public string? CertificateNo { get; set; }
    public DateTime? IssueDate { get; set; }
    public int? PropertyDetailsId { get; set; }
}

/// <summary>
/// Response DTO when uploading or replacing a certificate document
/// </summary>
public class PropertyCertificateUploadResponseDto
{
    public int PropertyCertificateId { get; set; }
    public Guid DocumentGuid { get; set; }
    public int DocumentId { get; set; }
    public int DocumentBindingId { get; set; }
    public int PropertyId { get; set; }
    public int CertificateTypeId { get; set; }
    public string? CertificateNo { get; set; }
    public DateTime? IssueDate { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public int? PropertyDetailsId { get; set; }
}

/// <summary>
/// DTO representing certificate type with status for a property
/// Used by GET /types-with-status endpoint to load page data
/// </summary>
public class PropertyCertificateWithStatusDto
{
    public int CertificateTypeId { get; set; }
    public string CertificateTypeName { get; set; } = string.Empty;
    public string CertificateTypeCode { get; set; } = string.Empty;
    public bool IsProtected { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public bool HasCertificate { get; set; }
    public int? PropertyCertificateId { get; set; }
    public bool IsActive { get; set; }
    public string? CertificateNo { get; set; }
    public DateTime? IssueDate { get; set; }
    public Guid? DocumentGuid { get; set; }
    public string? FileName { get; set; }

    /// <summary>
    /// NULL = property-level certificate (Building Permission "Apply to Property" scope).
    /// Set = floor-level certificate tied to this PropertyDetails/floor row.
    /// </summary>
    public int? PropertyDetailsId { get; set; }
}

