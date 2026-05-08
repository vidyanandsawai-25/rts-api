namespace NtisPlatform.Core.Exceptions;

/// <summary>
/// Exception thrown when a document is not found
/// </summary>
public class DocumentNotFoundException : EntityNotFoundException
{
    public DocumentNotFoundException(int documentId)
        : base("Document", documentId, "DOCUMENT_NOT_FOUND")
    {
    }

    public DocumentNotFoundException(Guid documentGuid)
        : base("Document", documentGuid, "DOCUMENT_NOT_FOUND")
    {
    }
}

/// <summary>
/// Exception thrown when a document binding is not found
/// </summary>
public class DocumentBindingNotFoundException : EntityNotFoundException
{
    public DocumentBindingNotFoundException(int bindingId)
        : base("DocumentBinding", bindingId, "BINDING_NOT_FOUND")
    {
    }
}

/// <summary>
/// Exception thrown when attempting to perform operations on a deleted document
/// </summary>
public class DocumentDeletedException : BusinessRuleException
{
    public Guid DocumentGuid { get; }

    public DocumentDeletedException(Guid documentGuid)
        : base($"Cannot perform operation on deleted document: {documentGuid}", "DOCUMENT_DELETED")
    {
        DocumentGuid = documentGuid;
    }
}

/// <summary>
/// Exception thrown when attempting to perform operations on an infected document
/// </summary>
public class DocumentInfectedException : BusinessRuleException
{
    public Guid DocumentGuid { get; }

    public DocumentInfectedException(Guid documentGuid)
        : base($"Cannot perform operation on infected document: {documentGuid}", "DOCUMENT_INFECTED")
    {
        DocumentGuid = documentGuid;
    }
}

/// <summary>
/// Exception thrown when attempting to download an expired document
/// </summary>
public class DocumentExpiredException : BusinessRuleException
{
    public Guid DocumentGuid { get; }
    public DateTime ExpiryDate { get; }

    public DocumentExpiredException(Guid documentGuid, DateTime expiryDate)
        : base($"Document {documentGuid} expired on {expiryDate:yyyy-MM-dd}", "DOCUMENT_EXPIRED")
    {
        DocumentGuid = documentGuid;
        ExpiryDate = expiryDate;
    }
}

/// <summary>
/// Exception thrown when file validation fails
/// </summary>
public class InvalidFileException : ValidationException
{
    public string FileName { get; }
    public string? Reason { get; }

    public InvalidFileException(string fileName, string reason)
        : base($"Invalid file '{fileName}': {reason}", "INVALID_FILE")
    {
        FileName = fileName;
        Reason = reason;
    }
}

/// <summary>
/// Exception thrown when file type is not allowed
/// </summary>
public class InvalidFileTypeException : ValidationException
{
    public string FileName { get; }
    public string ContentType { get; }
    public string FileExtension { get; }

    public InvalidFileTypeException(string fileName, string contentType, string fileExtension)
        : base($"File type not allowed: {fileName} (ContentType: {contentType}, Extension: {fileExtension})", "INVALID_FILE_TYPE")
    {
        FileName = fileName;
        ContentType = contentType;
        FileExtension = fileExtension;
    }
}

/// <summary>
/// Exception thrown when file size exceeds the allowed limit
/// </summary>
public class FileSizeLimitExceededException : ValidationException
{
    public string FileName { get; }
    public long FileSize { get; }
    public long MaxFileSize { get; }

    public FileSizeLimitExceededException(string fileName, long fileSize, long maxFileSize)
        : base($"File '{fileName}' size ({fileSize} bytes) exceeds maximum allowed size ({maxFileSize} bytes)", "FILE_SIZE_EXCEEDED")
    {
        FileName = fileName;
        FileSize = fileSize;
        MaxFileSize = maxFileSize;
    }
}

/// <summary>
/// Exception thrown when file storage operations fail
/// </summary>
public class FileStorageException : NtisPlatformException
{
    public string? FilePath { get; }

    public FileStorageException(string message, string? filePath = null)
        : base(message, "FILE_STORAGE_ERROR")
    {
        FilePath = filePath;
    }

    public FileStorageException(string message, string? filePath, Exception innerException)
        : base(message, "FILE_STORAGE_ERROR", innerException)
    {
        FilePath = filePath;
    }
}

/// <summary>
/// Exception thrown when checksum validation fails
/// </summary>
public class ChecksumMismatchException : BusinessRuleException
{
    public string FileName { get; }
    public string ExpectedChecksum { get; }
    public string ActualChecksum { get; }

    public ChecksumMismatchException(string fileName, string expectedChecksum, string actualChecksum)
        : base($"Checksum mismatch for file '{fileName}'. Expected: {expectedChecksum}, Actual: {actualChecksum}", "CHECKSUM_MISMATCH")
    {
        FileName = fileName;
        ExpectedChecksum = expectedChecksum;
        ActualChecksum = actualChecksum;
    }
}
