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
    /// Module code for document binding (e.g., PROPERTY, BUILDING).
    /// Optional: Only required if creating a document binding.
    /// </summary>
    [StringLength(50, MinimumLength = 2, ErrorMessage = "ModuleCode must be between 2 and 50 characters if provided.")]
    [RegularExpression(@"^[A-Z_]+$", ErrorMessage = "ModuleCode must contain only uppercase letters and underscores.")]
    public string? ModuleCode { get; set; }

    /// <summary>
    /// Reference table name for document binding (e.g., PropertyCertificate).
    /// Optional: Only required if creating a document binding.
    /// </summary>
    [StringLength(100, MinimumLength = 2, ErrorMessage = "ReferenceTableName must be between 2 and 100 characters if provided.")]
    [RegularExpression(@"^[A-Za-z][A-Za-z0-9]*$", ErrorMessage = "ReferenceTableName must start with a letter and contain only alphanumeric characters.")]
    public string? ReferenceTableName { get; set; }

    public int? ReferenceTableId { get; set; }

    public Guid? ReferenceTableIdGuid { get; set; }

    public string? BindingPurpose { get; set; }

    public bool IsPrimaryDocument { get; set; }

    public string? AuthModuleCode { get; set; }

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
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
}

/// <summary>
/// DTO for document details
/// </summary>
public class DocumentDto
{
    public int Id { get; set; }
    public Guid DocumentGuid { get; set; }
    public int UploadedBy { get; set; }
    public int? OwnerUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StorageProvider { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public string UploadStatusCode { get; set; } = string.Empty;
    public int DownloadCount { get; set; }
    public DateTime? CreatedDate { get; set; }
    public bool IsActive { get; set; }
}

