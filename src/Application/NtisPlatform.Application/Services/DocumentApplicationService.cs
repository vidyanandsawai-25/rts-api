using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.Common;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application service for CORE.Document operations ONLY
/// Does NOT handle business entities like PropertyCertificate
/// </summary>
public class DocumentApplicationService : IDocumentApplicationService
{
    private readonly IDocumentService _documentService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DocumentApplicationService> _logger;
    private readonly int _bufferSizeBytes;
    private readonly long _maxFileSizeBytes;

    public DocumentApplicationService(
        IDocumentService documentService,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        IOptions<FileStorageOptions> fileStorageOptions,
        ILogger<DocumentApplicationService> logger)
    {
        _documentService = documentService;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
        _logger = logger;

        var fileStorage = fileStorageOptions.Value;
        _bufferSizeBytes = fileStorage.BufferSizeBytes;
        _maxFileSizeBytes = fileStorage.MaxFileSizeBytes;
    }

    public async Task<DocumentUploadResponseDto> UploadDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        DocumentUploadDto uploadDto,
        int uploadedBy,
        CancellationToken cancellationToken = default)
    {
        // Input validation using Guard clauses
        Guard.AgainstInvalidStream(fileStream, nameof(fileStream));
        Guard.AgainstNullOrWhiteSpace(originalFileName, nameof(originalFileName));
        Guard.AgainstExceedingLength(originalFileName, 255, nameof(originalFileName));
        Guard.AgainstNullOrWhiteSpace(mimeType, nameof(mimeType));
        Guard.AgainstNegativeOrZero(fileSizeBytes, nameof(fileSizeBytes));
        Guard.AgainstOutOfRange(fileSizeBytes, 1, _maxFileSizeBytes, nameof(fileSizeBytes));
        Guard.AgainstNull(uploadDto, nameof(uploadDto));
        Guard.AgainstNegativeOrZero(uploadedBy, nameof(uploadedBy));

        _logger.LogInformation("Starting document upload: {FileName}, Size: {FileSize} bytes, User: {UserId}",
            originalFileName, fileSizeBytes, uploadedBy);

        var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var tempFilePath = Path.GetTempFileName();

        try
        {
            // Compute checksum while buffering to temp file in a single pass
            // Note: SHA256 hashing is CPU-bound and performed synchronously per buffer,
            // but we optimize by doing async I/O and hashing in parallel during the read/write cycle
            string checksumSha256;
            await using (var tempFileStream = new FileStream(
                tempFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                _bufferSizeBytes,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                using var sha256 = SHA256.Create();
                var buffer = new byte[_bufferSizeBytes];
                int bytesRead;

                while ((bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    // Write to temp file asynchronously
                    await tempFileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);

                    // Hash computation: While TransformBlock is synchronous, it's a fast in-memory operation
                    // that doesn't block I/O. For very large files, consider using Task.Run for true parallelism,
                    // but the overhead typically outweighs benefits for typical buffer sizes (80KB)
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                }

                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                checksumSha256 = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
                _logger.LogDebug("Computed SHA-256 checksum: {Checksum} for file: {FileName}",
                    checksumSha256, originalFileName);
            }

            // Save file from temp location to storage
            await using (var tempFileStream = new FileStream(
                tempFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                _bufferSizeBytes,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var storagePath = await _fileStorageService.SaveFileAsync(tempFileStream, originalFileName, cancellationToken);
                _logger.LogInformation("File saved to storage: {StoragePath}", storagePath);

                try
                {
                    // Begin transaction for multi-step database operations
                    await _unitOfWork.BeginTransactionAsync(cancellationToken);

                    try
                    {
                        // Create Document
                        var (documentId, documentGuid) = await _documentService.CreateDocumentAsync(
                            uploadedBy,
                            uploadDto.OwnerUserId,
                            Path.GetFileName(storagePath),
                            originalFileName,
                            fileExtension,
                            mimeType,
                            fileSizeBytes,
                            storagePath,
                            null,
                            checksumSha256,
                            uploadDto.DocumentType ?? DocumentType.Certificate.ToTypeString(),
                            cancellationToken);
                        _logger.LogInformation("Document created: DocumentId={DocumentId}, DocumentGuid={DocumentGuid}",
                            documentId, documentGuid);

                        // Create binding if valid data provided
                        int? bindingId = null;
                        if (ShouldCreateBinding(uploadDto))
                        {
                            bindingId = await _documentService.CreateDocumentBindingAsync(
                                documentId,
                                uploadDto.DepartmentId!.Value,
                                uploadDto.ModuleId!.Value,
                                uploadDto.ReferenceTableName!,
                                uploadDto.ReferenceTableId,
                                uploadDto.ReferenceTableIdGuid,
                                uploadDto.ReferencePropertyName ?? "Id",
                                uploadDto.BindingPurpose,
                                uploadDto.IsPrimaryDocument,
                                uploadDto.AuthDepartmentId,
                                uploadDto.AuthReferenceId,
                                uploadedBy,
                                cancellationToken);
                            _logger.LogInformation("Document binding created: BindingId={BindingId}", bindingId);
                        }

                        // Commit transaction - all database operations succeeded
                        await _unitOfWork.CommitTransactionAsync(cancellationToken);

                        _logger.LogInformation("Document upload completed successfully: {DocumentGuid}", documentGuid);
                        return new DocumentUploadResponseDto
                        {
                            DocumentGuid = documentGuid,
                            DocumentId = documentId,
                            DocumentBindingId = bindingId,
                            FileName = originalFileName,
                            FileSizeBytes = fileSizeBytes,
                            StoragePath = storagePath
                        };
                    }
                    catch
                    {
                        // Rollback transaction on any database error
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        throw;
                    }
                }
                catch
                {
                    // If any database operation fails after the file was saved to storage,
                    // attempt to delete the orphaned file to prevent storage accumulation.
                    // NOTE: If this cleanup fails, orphaned files may exist temporarily in storage
                    // and should be cleaned up manually or via a periodic maintenance task.
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(storagePath, cancellationToken);
                        _logger.LogWarning("Deleted orphaned file during upload rollback: {StoragePath}", storagePath);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, 
                            "Failed to delete stored file during upload rollback: {StoragePath}. File may remain orphaned and require manual cleanup.", 
                            storagePath);
                    }

                    throw;
                }
            }
        }
        finally
        {
            // Clean up temporary file with proper error handling
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                    _logger.LogDebug("Deleted temporary file: {TempFilePath}", tempFilePath);
                }
                catch (Exception ex)
                {
                    // Log but don't throw - temp file cleanup failure shouldn't fail the upload
                    _logger.LogWarning(ex, "Failed to delete temporary file: {TempFilePath}. File may need manual cleanup.", tempFilePath);
                }
            }
        }
    }

    public async Task<DocumentDto?> GetDocumentAsync(Guid documentGuid, CancellationToken cancellationToken = default)
    {
        // Input validation
        Guard.AgainstEmptyGuid(documentGuid, nameof(documentGuid));

        var document = await _documentService.GetDocumentByGuidAsync(documentGuid, cancellationToken);
        if (document == null)
        {
            _logger.LogWarning("Document not found: {DocumentGuid}", documentGuid);
            return null;
        }

        return new DocumentDto
        {
            Id = document.Id,
            DocumentGuid = document.DocumentGuid,
            UploadedBy = document.UploadedByUserId,
            FileName = document.FileName,
            OriginalFileName = document.OriginalFileName,
            FileExtension = document.FileExtension,
            MimeType = document.MimeType,
            FileSizeBytes = document.FileSizeBytes,
            StorageProvider = document.StorageProvider,
            StoragePath = document.StoragePath,
            DocumentType = document.DocumentType,
            UploadStatusCode = document.UploadStatusCode,
            DownloadCount = document.DownloadCount,
            CreatedDate = document.CreatedDate,
            IsActive = document.IsActive
        };
    }

    public async Task<(Stream? FileStream, string FileName, string MimeType)> DownloadDocumentAsync(
        Guid documentGuid,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        Guard.AgainstEmptyGuid(documentGuid, nameof(documentGuid));
        Guard.AgainstNegativeOrZero(userId, nameof(userId));

        var document = await _documentService.GetDocumentByGuidAsync(documentGuid, cancellationToken);
        if (document == null)
        {
            _logger.LogWarning("Download failed - document not found: {DocumentGuid}", documentGuid);
            return (null, string.Empty, string.Empty);
        }

        var fileStream = await _fileStorageService.ReadFileAsync(document.StoragePath, cancellationToken);
        if (fileStream != null)
        {
            await _documentService.IncrementDownloadCountAsync(documentGuid, userId, cancellationToken);
            _logger.LogInformation("Document downloaded: {DocumentGuid} by user {UserId}", documentGuid, userId);
        }
        else
        {
            _logger.LogWarning("File not found in storage: {StoragePath} for document {DocumentGuid}",
                document.StoragePath, documentGuid);
        }

        return (fileStream, document.OriginalFileName, document.MimeType);
    }

    public async Task<(Stream? FileStream, string FileName, string MimeType)> ViewDocumentAsync(
        Guid documentGuid,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        Guard.AgainstEmptyGuid(documentGuid, nameof(documentGuid));

        var document = await _documentService.GetDocumentByGuidAsync(documentGuid, cancellationToken);
        if (document == null)
        {
            _logger.LogWarning("View failed - document not found: {DocumentGuid}", documentGuid);
            return (null, string.Empty, string.Empty);
        }

        var fileStream = await _fileStorageService.ReadFileAsync(document.StoragePath, cancellationToken);
        if (fileStream == null)
        {
            _logger.LogWarning("File not found in storage for view: {StoragePath}, document {DocumentGuid}",
                document.StoragePath, documentGuid);
        }
        return (fileStream, document.OriginalFileName, document.MimeType);
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentGuid, int deletedBy, CancellationToken cancellationToken = default)
    {
        // Input validation
        Guard.AgainstEmptyGuid(documentGuid, nameof(documentGuid));
        Guard.AgainstNegativeOrZero(deletedBy, nameof(deletedBy));

        var result = await _documentService.DeleteDocumentAsync(documentGuid, deletedBy, cancellationToken);
        if (result)
        {
            _logger.LogInformation("Document deleted: {DocumentGuid} by user {UserId}", documentGuid, deletedBy);
        }
        else
        {
            _logger.LogWarning("Delete failed - document not found: {DocumentGuid}", documentGuid);
        }
        return result;
    }

    public async Task UpdateDocumentBindingReferenceAsync(
        int documentBindingId,
        int referenceTableId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        Guard.AgainstNegativeOrZero(documentBindingId, nameof(documentBindingId));
        Guard.AgainstNegativeOrZero(referenceTableId, nameof(referenceTableId));
        Guard.AgainstNegativeOrZero(updatedBy, nameof(updatedBy));

        await _documentService.UpdateDocumentBindingReferenceAsync(documentBindingId, referenceTableId, updatedBy, cancellationToken);
    }

    /// <summary>
    /// Determines if a document binding should be created based on the upload DTO.
    /// Returns false if binding information is missing or invalid.
    /// </summary>
    /// <param name="dto">The document upload DTO containing binding information</param>
    /// <returns>True if a valid binding should be created, false otherwise</returns>
    private bool ShouldCreateBinding(DocumentUploadDto dto)
    {
        // Check if DepartmentId is provided and valid
        if (!dto.DepartmentId.HasValue || dto.DepartmentId.Value <= 0)
            return false;

        // Check if ModuleId is provided and valid
        if (!dto.ModuleId.HasValue || dto.ModuleId.Value <= 0)
            return false;

        // Check if ReferenceTableName is provided and not a placeholder/test value
        if (string.IsNullOrWhiteSpace(dto.ReferenceTableName))
            return false;

        // Validate ReferenceTableName format (starts with a letter, alphanumeric only, 2-100 chars)
        if (!System.Text.RegularExpressions.Regex.IsMatch(dto.ReferenceTableName, @"^[A-Za-z][A-Za-z0-9]{1,99}$"))
            return false;

        // Validate at least one reference ID is provided (XOR: exactly one must be present)
        bool hasIntId = dto.ReferenceTableId.HasValue && dto.ReferenceTableId.Value > 0;
        bool hasGuidId = dto.ReferenceTableIdGuid.HasValue && dto.ReferenceTableIdGuid.Value != Guid.Empty;

        // XOR check: exactly one ID type should be provided, not both and not neither
        return hasIntId ^ hasGuidId;
    }
}
