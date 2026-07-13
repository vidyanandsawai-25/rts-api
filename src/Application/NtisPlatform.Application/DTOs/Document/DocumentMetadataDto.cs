namespace NtisPlatform.Application.DTOs.Document;

 /// <summary>
 /// Document metadata DTO (no file stream).
 /// Used for metadata-only requests to reduce payload size and improve performance.
 /// </summary>
public class DocumentMetadataDto
{
    /// <summary>
    /// Unique document identifier (GUID)
    /// </summary>
    public Guid DocumentGuid { get; set; }

    /// <summary>
    /// User-friendly title
    /// </summary>
    public string? DocumentTitle { get; set; }

    /// <summary>
    /// Document description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Document type (Certificate, Invoice, Contract, Report, etc.)
    /// </summary>
    public string? DocumentType { get; set; }

    /// <summary>
    /// Document category for classification
    /// </summary>
    public string? DocumentCategory { get; set; }

    /// <summary>
    /// File MIME type
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Original file name as uploaded by user
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// File extension (e.g., ".pdf", ".jpg")
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// User ID who uploaded the document
    /// </summary>
    public int? UploadedByUserId { get; set; }

    /// <summary>
    /// When document was created
    /// </summary>
    public DateTime? CreatedDate { get; set; }

    /// <summary>
    /// SHA-256 checksum for integrity verification
    /// </summary>
    public string? ChecksumSha256 { get; set; }

    /// <summary>
    /// Upload status (ACTIVE, PENDING, FAILED)
    /// </summary>
    public string UploadStatusCode { get; set; } = string.Empty;

    /// <summary>
    /// Virus scan status (NULL, PENDING, CLEAN, INFECTED, ERROR)
    /// </summary>
    public string? ScanStatusCode { get; set; }

    /// <summary>
    /// IDs of document bindings attached to this document
    /// </summary>
    public List<int> DocumentBindingIds { get; set; } = new();

    /// <summary>
    /// Is document marked for deletion
    /// </summary>
    public bool MarkedForDeletion { get; set; }

    /// <summary>
    /// When document was marked for deletion
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }

    /// <summary>
    /// Download count
    /// </summary>
    public int DownloadCount { get; set; }
}
