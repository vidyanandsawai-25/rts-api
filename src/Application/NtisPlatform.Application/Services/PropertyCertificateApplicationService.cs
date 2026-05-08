using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Common;
using NtisPlatform.Application.DTOs.PropertyCertificate;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application service for PropertyCertificate operations
/// SEPARATE from Document service
/// </summary>
public class PropertyCertificateApplicationService : IPropertyCertificateApplicationService
{
    private readonly IPropertyCertificateService _propertyCertificateService;
    private readonly IDocumentService _documentService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PropertyCertificateApplicationService> _logger;
    private readonly int _bufferSizeBytes;
    private readonly long _maxFileSizeBytes;

    public PropertyCertificateApplicationService(
        IPropertyCertificateService propertyCertificateService,
        IDocumentService documentService,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<PropertyCertificateApplicationService> logger)
    {
        _propertyCertificateService = propertyCertificateService;
        _documentService = documentService;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _bufferSizeBytes = configuration.GetValue<int>("FileStorage:BufferSizeBytes", 81920);
        _maxFileSizeBytes = configuration.GetValue<long>("FileStorage:MaxFileSizeBytes", 104857600);
    }

    public async Task<PropertyCertificateUploadResponseDto> UploadWithDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        int propertyId,
        int certificateTypeId,
        string? certificateNo,
        DateTime? issueDate,
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
        Guard.AgainstNegativeOrZero(propertyId, nameof(propertyId));
        Guard.AgainstNegativeOrZero(certificateTypeId, nameof(certificateTypeId));
        Guard.AgainstNegativeOrZero(uploadedBy, nameof(uploadedBy));

        // Optional field validation
        if (!string.IsNullOrWhiteSpace(certificateNo))
        {
            Guard.AgainstExceedingLength(certificateNo, 100, nameof(certificateNo));
        }

        if (issueDate.HasValue)
        {
            Guard.Against(issueDate.Value > DateTime.Now, "Issue date cannot be in the future.", nameof(issueDate));
        }

        _logger.LogInformation("Starting PropertyCertificate upload: {FileName}, PropertyId: {PropertyId}, CertificateTypeId: {CertificateTypeId}, User: {UserId}",
            originalFileName, propertyId, certificateTypeId, uploadedBy);

        var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var tempFilePath = Path.GetTempFileName();
        string? storagePath = null;

        try
        {
            // 1. Buffer the upload once while computing the checksum in the same pass
            // Note: SHA256 hashing is CPU-bound and performed synchronously per buffer,
            // but we optimize by doing async I/O and hashing in parallel during the read/write cycle
            await using var tempFileStream = new FileStream(
                tempFilePath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                _bufferSizeBytes,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

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
            var checksumSha256 = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
            _logger.LogDebug("Computed SHA-256 checksum: {Checksum} for PropertyCertificate file: {FileName}",
                checksumSha256, originalFileName);

            // 2. Save the buffered file to storage
            tempFileStream.Position = 0;
            storagePath = await _fileStorageService.SaveFileAsync(tempFileStream, originalFileName, cancellationToken);
            _logger.LogInformation("PropertyCertificate file saved to storage: {StoragePath}", storagePath);

            try
            {
                // Begin transaction for multi-step database operations
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    // 3. Create CORE.Document
                    var (documentId, documentGuid) = await _documentService.CreateDocumentAsync(
                        uploadedBy,
                        null,
                        Path.GetFileName(storagePath),
                        originalFileName,
                        fileExtension,
                        mimeType,
                        fileSizeBytes,
                        storagePath,
                        null,
                        checksumSha256,
                        DocumentType.Certificate.ToTypeString(),
                        cancellationToken);

                    // 4. Create PTIS.PropertyCertificate first (without DocumentBinding)
                    var propertyCertificateId = await _propertyCertificateService.CreateAsync(
                        propertyId,
                        certificateTypeId,
                        certificateNo,
                        issueDate,
                        uploadedBy,
                        cancellationToken);
                    _logger.LogInformation("PropertyCertificate created: Id={PropertyCertificateId}",
                        propertyCertificateId);

                    // 5. Create CORE.DocumentBinding with actual PropertyCertificate ID
                    var documentBindingId = await _documentService.CreateDocumentBindingAsync(
                        documentId,
                        ModuleCode.Property.ToModuleString(),
                        nameof(DocumentReferenceTable.PropertyCertificate),
                        propertyCertificateId,  // Actual PropertyCertificate ID
                        null,
                        DocumentBindingPurpose.MainDocument.ToPurposeString(),
                        true,
                        ModuleCode.Property.ToModuleString(),
                        propertyId,  // AuthReferenceId = PropertyId
                        uploadedBy,
                        cancellationToken);

                    // 6. Update PropertyCertificate with DocumentBinding ID
                    await _propertyCertificateService.UpdateDocumentBindingAsync(
                        propertyCertificateId,
                        documentBindingId,
                        uploadedBy,
                        cancellationToken);

                    // Commit transaction - all database operations succeeded
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    _logger.LogInformation("PropertyCertificate upload completed successfully: PropertyCertificateId={PropertyCertificateId}, DocumentGuid={DocumentGuid}",
                        propertyCertificateId, documentGuid);

                    return new PropertyCertificateUploadResponseDto
                    {
                        PropertyCertificateId = propertyCertificateId,
                        DocumentGuid = documentGuid,
                        DocumentId = documentId,
                        DocumentBindingId = documentBindingId,
                        PropertyId = propertyId,
                        CertificateTypeId = certificateTypeId,
                        CertificateNo = certificateNo,
                        IssueDate = issueDate,
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
                if (storagePath != null)
                {
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(storagePath, cancellationToken);
                        _logger.LogWarning("Deleted orphaned file during PropertyCertificate upload rollback: {StoragePath}", storagePath);
                    }
                    catch (Exception cleanupException)
                    {
                        _logger.LogWarning(cleanupException,
                            "Failed to delete orphaned property certificate file: {StoragePath}. File may remain orphaned and require manual cleanup.", 
                            storagePath);
                    }
                }
                throw;
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

    public async Task<List<PropertyCertificateDto>> GetByPropertyIdAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        Guard.AgainstNegativeOrZero(propertyId, nameof(propertyId));

        var entities = await _propertyCertificateService.GetByPropertyIdAsync(propertyId, cancellationToken);

        return entities.Select(e => new PropertyCertificateDto
        {
            Id = e.Id,
            PropertyId = e.PropertyId,
            CertificateTypeId = e.CertificateTypeId,
            CertificateTypeName = e.CertificateType?.CertificateTypeName,
            CertificateNo = e.CertificateNo,
            IssueDate = e.IssueDate,
            DocumentBindingId = e.DocumentBindingId,
            DocumentGuid = GetSafeDocumentGuid(e.DocumentBinding),
            IsEnabled = e.IsEnabled
        }).ToList();
    }

    /// <summary>
    /// Safely extracts the DocumentGuid from a DocumentBinding if the document is valid and active.
    /// Returns null if the document is inactive, marked for deletion, or if any part of the chain is null.
    /// </summary>
    private static Guid? GetSafeDocumentGuid(DocumentBindingEntity? documentBinding)
    {
        if (documentBinding?.Document == null)
            return null;

        var document = documentBinding.Document;

        if (!document.IsActive || document.MarkedForDeletion)
            return null;

        return document.DocumentGuid;
    }
}
