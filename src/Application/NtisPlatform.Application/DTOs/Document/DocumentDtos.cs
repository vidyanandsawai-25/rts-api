using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Document;

/// <summary>
/// DTO for document upload request
/// </summary>
public class DocumentUploadDto
{
    public int? OwnerUserId { get; set; }

    public string? DocumentType { get; set; }

    // Binding Information
    /// <summary>
    /// Department ID for document binding (e.g., 3 for PTIS).
    /// Optional: Only required if creating a document binding.
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Module ID for document binding (e.g., 12 for PropertyCertificate).
    /// Optional: Only required if creating a document binding.
    /// </summary>
    public int? ModuleId { get; set; }

    /// <summary>
    /// Reference table name for document binding (e.g., PropertyCertificates).
    /// Optional: Only required if creating a document binding.
    /// </summary>
    [StringLength(100, MinimumLength = 2, ErrorMessage = "ReferenceTableName must be between 2 and 100 characters if provided.")]
    [RegularExpression(@"^[A-Za-z][A-Za-z0-9]*$", ErrorMessage = "ReferenceTableName must start with a letter and contain only alphanumeric characters.")]
    public string? ReferenceTableName { get; set; }

    public int? ReferenceTableId { get; set; }

    public Guid? ReferenceTableIdGuid { get; set; }

    /// <summary>
    /// Name of the primary key column in the reference table (e.g., "Id", "PropertyCertificateId").
    /// Optional: Only required if creating a document binding.
    /// </summary>
    [StringLength(100, MinimumLength = 2, ErrorMessage = "ReferencePropertyName must be between 2 and 100 characters if provided.")]
    public string? ReferencePropertyName { get; set; }

    public string? BindingPurpose { get; set; }

    public bool IsPrimaryDocument { get; set; }

    /// <summary>
    /// Authorization department ID (e.g., 3 for PTIS).
    /// Optional: Used for permission checks.
    /// </summary>
    public int? AuthDepartmentId { get; set; }

    public int? AuthReferenceId { get; set; }
}

/// <summary>
/// DTO for document upload response
/// </summary>
public class DocumentUploadResponseDto
{
    public Guid DocumentGuid { get; set; }
    public int DocumentId { get; set; }
    public int? DocumentBindingId { get; set; }
    public string? FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public string? StoragePath { get; set; }
}

/// <summary>
/// DTO for document details
/// </summary>
public class DocumentDto
{
    public int Id { get; set; }
    public Guid DocumentGuid { get; set; }
    public int? UploadedByUserId { get; set; }
    public string? FileName { get; set; }
    public string? OriginalFileName { get; set; }
    public string? FileExtension { get; set; }
    public string? MimeType { get; set; }
    public long FileSizeBytes { get; set; }
    public string? StorageProvider { get; set; }
    public string? StoragePath { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentCategory { get; set; }
    public string? Description { get; set; }
    public string? UploadStatusCode { get; set; }
    public string? ScanStatusCode { get; set; }
    public int DownloadCount { get; set; }
    public DateTime? CreatedDate { get; set; }
    public bool IsActive { get; set; }
}


/// <summary>
/// Lightweight projection of a DocumentBinding + its associated Document,
/// returned by <see cref="IDocumentApplicationService.GetDocumentsByReferenceTableAsync"/>.
/// Replaces direct repository queries in services that need to enrich DTOs with binding data.
/// </summary>
public class DocumentBindingInfoDto
{
    /// <summary>The integer reference table ID on the binding (e.g. PropertySocialDetailId).</summary>
    public int ReferenceTableId { get; init; }

    /// <summary>The DocumentBinding PK.</summary>
    public int BindingId { get; init; }

    /// <summary>The GUID of the associated Document.</summary>
    public Guid DocumentGuid { get; init; }

    /// <summary>
    /// The binding purpose (e.g. "Photo", "MainDocument", null for generic bindings).
    /// Used to distinguish photo bindings from document bindings for the same reference row.
    /// </summary>
    public string? BindingPurpose { get; init; }

    /// <summary>The original file name of the associated Document.</summary>
    public string? OriginalFileName { get; init; }

    /// <summary>The MIME type of the associated Document.</summary>
    public string? MimeType { get; init; }
}
