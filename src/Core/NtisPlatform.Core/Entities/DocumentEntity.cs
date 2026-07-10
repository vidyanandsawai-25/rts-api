using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Central repository for all uploaded documents (CORE.Document).
/// Implements IHardDeletable to support background cleanup service for orphaned files.
/// </summary>
public class DocumentEntity : BaseEntity, IHardDeletable
{
    public static DocumentEntity Create(
        int uploadedByUserId,
        string fileName,
        string originalFileName,
        string fileExtension,
        string mimeType,
        long fileSizeBytes,
        string storagePath,
        string? documentType = null)
    {
        return new DocumentEntity
        {
            DocumentGuid = Guid.NewGuid(),
            UploadedByUserId = uploadedByUserId,
            FileName = fileName,
            OriginalFileName = originalFileName,
            FileExtension = fileExtension.ToLowerInvariant(),
            MimeType = mimeType,
            FileSizeBytes = fileSizeBytes,
            StoragePath = storagePath,
            StorageProvider = "FOLDER",
UploadStatusCode = "ACTIVE",
            DocumentType = documentType,
            Version = 1,
            IsLatestVersion = true,
            IsActive = true
        };
    }

    public Guid DocumentGuid { get; set; }
    public int? DepartmentId { get; set; }
    public int? DepartmentEntityId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StorageProvider { get; set; } = "FOLDER";
    public string StoragePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public string? ChecksumSha256 { get; set; }
    public string? ScanStatusCode { get; set; }
    public DateTime? ScanCompletedDate { get; set; }
    public string? ScanDetails { get; set; }
    public string UploadStatusCode { get; set; } = "ACTIVE";
    public string? DocumentTitle { get; set; } = null;
    public string? Description { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentCategory { get; set; }
    public string? Tags { get; set; }
    public string? Language { get; set; }
    public int Version { get; set; } = 1;
    public int? ParentDocumentId { get; set; }
    public bool IsLatestVersion { get; set; } = true;
    public bool IsPublic { get; set; } = false;
    public bool InheritPermissions { get; set; } = true;
    public string? ConfidentialityLevel { get; set; }
    public int? PageCount { get; set; }
    public string? SearchableText { get; set; }
    public string? ExtractionStatus { get; set; }
    public string? EncryptionKeyId { get; set; }
    public bool IsEncrypted { get; set; } = false;
    public int DownloadCount { get; set; } = 0;
    public string? SourceSystem { get; set; }
    public int UploadedByUserId { get; set; }
    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }
    public byte[]? RowVersion { get; set; }

    public DocumentEntity? ParentDocument { get; set; }
    public ICollection<DocumentBindingEntity> DocumentBindings { get; set; } = new List<DocumentBindingEntity>();

    public void MarkForDeletion(int deletedByUserId)
    {
        if (deletedByUserId <= 0)
            throw new ArgumentException("Deleted by user ID must be greater than zero.", nameof(deletedByUserId));
        MarkedForDeletion = true;
        MarkedForDeletionDate = DateTime.Now;
        IsActive = false;
    }

    public void RecordDownload(int userId)
    {
        if (userId <= 0)
            throw new ArgumentException("User ID must be greater than zero.", nameof(userId));
        DownloadCount++;
    }

    public void RestoreFromDeletion()
    {
        MarkedForDeletion = false;
        MarkedForDeletionDate = null;
        IsActive = true;
    }

    public void UpdateScanStatus(string scanStatus, string? scanDetails = null)
    {
        if (string.IsNullOrWhiteSpace(scanStatus))
            throw new ArgumentException("Scan status cannot be empty.", nameof(scanStatus));

        // Normalize to uppercase for consistency
        var normalizedStatus = scanStatus.Trim().ToUpperInvariant();

        // Validate against known scan statuses
        var validStatuses = new[] { "PENDING", "SCANNING", "CLEAN", "INFECTED", "QUARANTINED", "UNKNOWN", "ERROR" };
        if (!validStatuses.Contains(normalizedStatus))
            throw new ArgumentException($"Invalid scan status '{scanStatus}'. Must be one of: {string.Join(", ", validStatuses)}", nameof(scanStatus));

        ScanStatusCode = normalizedStatus;
        ScanCompletedDate = DateTime.Now;
        ScanDetails = scanDetails;

        if (normalizedStatus == "INFECTED" || normalizedStatus == "QUARANTINED")
            IsActive = false;
    }

    public void SetChecksum(string checksumSha256)
    {
        if (string.IsNullOrWhiteSpace(checksumSha256) || checksumSha256.Length != 64)
            throw new ArgumentException("SHA256 checksum must be 64 characters.", nameof(checksumSha256));
        ChecksumSha256 = checksumSha256.ToLowerInvariant();
    }

    public void SetDepartmentEntity(int? departmentId, int? departmentEntityId)
    {
        if (departmentEntityId.HasValue && !departmentId.HasValue)
            throw new ArgumentException("DepartmentEntityId requires DepartmentId to be set.", nameof(departmentId));
        DepartmentId = departmentId;
        DepartmentEntityId = departmentEntityId;
    }
}
