using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.Common;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.DTOs.PropertyCertificate;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application service for PropertyCertificate operations.
/// SEPARATE from Document service.
/// </summary>
public class PropertyCertificateApplicationService : IPropertyCertificateApplicationService
{
    private readonly IPropertyCertificateService _propertyCertificateService;
    private readonly IDocumentService _documentService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<DepartmentMasterEntity, int> _departmentRepository;
    private readonly IRepository<ModuleMasterEntity, int> _moduleRepository;
    private readonly IRepository<PropertyCertificateTypeMasterEntity, int> _certificateTypeRepository;
    private readonly ILogger<PropertyCertificateApplicationService> _logger;
    private readonly int _bufferSizeBytes;
    private readonly long _maxFileSizeBytes;

    public PropertyCertificateApplicationService(
        IPropertyCertificateService propertyCertificateService,
        IDocumentService documentService,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        IRepository<DepartmentMasterEntity, int> departmentRepository,
        IRepository<ModuleMasterEntity, int> moduleRepository,
        IRepository<PropertyCertificateTypeMasterEntity, int> certificateTypeRepository,
        IOptions<FileStorageOptions> fileStorageOptions,
        ILogger<PropertyCertificateApplicationService> logger)
    {
        _propertyCertificateService = propertyCertificateService;
        _documentService = documentService;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
        _departmentRepository = departmentRepository;
        _moduleRepository = moduleRepository;
        _certificateTypeRepository = certificateTypeRepository;
        _logger = logger;

        var fileStorage = fileStorageOptions.Value;
        _bufferSizeBytes = fileStorage.BufferSizeBytes;
        _maxFileSizeBytes = fileStorage.MaxFileSizeBytes;
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

                    // 4. Create PTIS.PropertyCertificates first (without DocumentBinding)
                    var propertyCertificateId = await _propertyCertificateService.CreateAsync(
                        propertyId,
                        certificateTypeId,
                        certificateNo,
                        issueDate,
                        uploadedBy,
                        cancellationToken);
                    _logger.LogInformation("PropertyCertificate created: Id={PropertyCertificateId}",
                        propertyCertificateId);

                    // 5. Get DepartmentId and ModuleId from database
                    // For PTIS department and PropertyCertificate module
                    // These will be determined from the actual database data
                    var (departmentId, moduleId) = await GetDepartmentAndModuleIdsAsync(cancellationToken);

                    // 6. Create CORE.DocumentBinding with actual PropertyCertificate ID
                    var documentBindingId = await _documentService.CreateDocumentBindingAsync(
                        documentId,
                        departmentId,  // DepartmentId from database
                        moduleId,  // ModuleId from database
                        "PropertyCertificates",  // Use plural table name to match [PTIS].[PropertyCertificates]
                        propertyCertificateId,  // Actual PropertyCertificate ID
                        null,  // ReferenceTableIdGuid
                        "Id",  // ReferencePropertyName
                        DocumentBindingPurpose.MainDocument.ToPurposeString(),
                        true,  // IsPrimaryDocument
                        departmentId,  // AuthDepartmentId
                        uploadedBy,  // AuthReferenceId = userId (for proper authorization)
                        uploadedBy,
                        cancellationToken);

                    // 7. Update PropertyCertificate with DocumentBinding ID
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

    /// <summary>
    /// Safely extracts the FileName from a DocumentBinding if the document is valid and active.
    /// Returns null if the document is inactive, marked for deletion, or if any part of the chain is null.
    /// </summary>
    private static string? GetSafeFileName(DocumentBindingEntity? documentBinding)
    {
        if (documentBinding?.Document == null)
            return null;

        var document = documentBinding.Document;

        if (!document.IsActive || document.MarkedForDeletion)
            return null;

        return document.OriginalFileName;
    }

    /// <summary>
    /// Gets the DepartmentId and ModuleId from the database dynamically.
    /// Finds the department that contains PropertyMast table and its primary module.
    /// No hardcoding - fully database-driven.
    /// </summary>
    private async Task<(int DepartmentId, int ModuleId)> GetDepartmentAndModuleIdsAsync(
        CancellationToken cancellationToken = default)
    {
        // Step 1: Find department that owns PropertyCertificate functionality
        // PropertyCertificate is linked to Property, which is in PTIS schema
        // Use exact match first for deterministic selection, then fallback to substring matching
        var allDepartments = await _departmentRepository.GetAsync(
            d => d.IsActive,
            cancellationToken);

        // Try exact matches first (deterministic), ordered by ID for consistency
        var exactMatches = allDepartments
            .Where(d => !string.IsNullOrEmpty(d.DepartmentCode))
            .Where(d => 
                d.DepartmentCode!.Equals("PTIS", StringComparison.OrdinalIgnoreCase) ||
                d.DepartmentCode!.Equals("PROPERTY", StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => d.Id)
            .ToList();

        DepartmentMasterEntity? department = null;

        if (exactMatches.Count == 1)
        {
            department = exactMatches[0];
        }
        else if (exactMatches.Count > 1)
        {
            // Multiple exact matches - ambiguous configuration
            var matchedCodes = string.Join(", ", exactMatches.Select(d => $"{d.DepartmentCode} (ID: {d.Id})"));
            throw new InvalidOperationException(
                $"Multiple departments match PTIS/PROPERTY criteria: {matchedCodes}. " +
                "Please ensure only one department with exact code 'PTIS' or 'PROPERTY' exists in DepartmentMaster table.");
        }
        else
        {
            // No exact match, try substring match
            var substringMatches = allDepartments
                .Where(d => !string.IsNullOrEmpty(d.DepartmentCode))
                .Where(d =>
                    d.DepartmentCode!.Contains("PTIS", StringComparison.OrdinalIgnoreCase) ||
                    d.DepartmentCode!.Contains("PROPERTY", StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.Id)
                .ToList();

            if (substringMatches.Count == 1)
            {
                department = substringMatches[0];
            }
            else if (substringMatches.Count > 1)
            {
                // Multiple substring matches - ambiguous configuration
                var matchedCodes = string.Join(", ", substringMatches.Select(d => $"{d.DepartmentCode} (ID: {d.Id})"));
                throw new InvalidOperationException(
                    $"Multiple departments contain PTIS/PROPERTY in their code: {matchedCodes}. " +
                    "Please ensure only one department matches this criteria or add an exact 'PTIS' or 'PROPERTY' code.");
            }
        }

        if (department == null)
        {
            // No department found, throw descriptive error
            var availableDepts = string.Join(", ", allDepartments.Select(d => d.DepartmentCode ?? "NULL"));
            throw new InvalidOperationException(
                $"No department found for PropertyCertificate. Available departments: {availableDepts}. " +
                "Please ensure a department with exact code 'PTIS' or 'PROPERTY' exists in DepartmentMaster table.");
        }

        // Step 2: Find PropertyCertificate module under this department
        // Use exact match first, then substring match, always ordered by ID for deterministic selection
        var modules = await _moduleRepository.GetAsync(
            m => m.DepartmentId == department.Id && m.IsActive,
            cancellationToken);

        // Try exact matches first (deterministic)
        var module = modules
            .Where(m => m.ModuleCode != null)
            .OrderBy(m => m.Id)
            .FirstOrDefault(m =>
                m.ModuleCode!.Equals("PROPERTY", StringComparison.OrdinalIgnoreCase) ||
                m.ModuleCode!.Equals("PROPERTYCERTIFICATE", StringComparison.OrdinalIgnoreCase) ||
                m.ModuleCode!.Equals("CERTIFICATE", StringComparison.OrdinalIgnoreCase));

        // Fallback to substring match if no exact match (still ordered by ID)
        if (module == null)
        {
            module = modules
                .Where(m => m.ModuleCode != null)
                .OrderBy(m => m.Id)
                .FirstOrDefault(m =>
                    m.ModuleCode!.Contains("PROPERTY", StringComparison.OrdinalIgnoreCase) ||
                    m.ModuleCode!.Contains("CERTIFICATE", StringComparison.OrdinalIgnoreCase));
        }

        // Fail fast if no match found with descriptive error
        if (module == null)
        {
            var availableModules = string.Join(", ", modules.Select(m => $"{m.ModuleCode ?? "NULL"} (ID: {m.Id})"));
            throw new InvalidOperationException(
                $"No module with code PROPERTY/PROPERTYCERTIFICATE/CERTIFICATE found for department '{department.DepartmentCode}' (ID: {department.Id}). Available modules: {availableModules}.");
        }

        if (module == null)
        {
            throw new InvalidOperationException(
                $"No active module found for department '{department.DepartmentCode}' (ID: {department.Id}). " +
                "Please ensure ModuleMaster table has at least one active module for this department.");
        }

        _logger.LogDebug("Resolved PropertyCertificate context: Department={DeptCode} (ID={DeptId}), Module={ModCode} (ID={ModId})",
            department.DepartmentCode, department.Id, module.ModuleCode, module.Id);

        return (department.Id, module.Id);
    }

    public async Task<List<PropertyCertificateWithStatusDto>> GetCertificateTypesWithStatusAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(propertyId, nameof(propertyId));

        // Get all active certificate types
        var allTypes = await _certificateTypeRepository.GetAsync(
            ct => ct.IsActive,
            cancellationToken);

        // Get existing certificates for this property (including inactive ones so status is accurate)
        var existingCertificates = await _propertyCertificateService.GetByPropertyIdIncludingInactiveAsync(
            propertyId,
            PropertyCertificateIncludeOptions.DocumentBinding | PropertyCertificateIncludeOptions.Document,
            cancellationToken);

        // Create a lookup for quick access
        var certificateLookup = existingCertificates
            .GroupBy(c => c.CertificateTypeId)
            .ToDictionary(g => g.Key, g => g.First());

        // Build result combining all types with their status
        var result = allTypes.OrderBy(t => t.DisplayOrder).Select(type =>
        {
            var hasCertificate = certificateLookup.TryGetValue(type.Id, out var certificate);

            return new PropertyCertificateWithStatusDto
            {
                CertificateTypeId = type.Id,
                CertificateTypeName = type.CertificateTypeName,
                DisplayOrder = type.DisplayOrder,
                HasCertificate = hasCertificate,
                PropertyCertificateId = hasCertificate && certificate != null ? certificate.Id : null,
                IsActive = hasCertificate && certificate != null && certificate.IsActive,
                CertificateNo = hasCertificate && certificate != null ? certificate.CertificateNo : null,
                IssueDate = hasCertificate && certificate != null ? certificate.IssueDate : null,
                DocumentGuid = hasCertificate && certificate != null ? GetSafeDocumentGuid(certificate.DocumentBinding) : null,
                FileName = hasCertificate && certificate != null ? GetSafeFileName(certificate.DocumentBinding) : null
            };
        }).ToList();

        return result;
    }

    public async Task<PropertyCertificateUploadResponseDto> ReplaceDocumentAsync(
        int propertyCertificateId,
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        int uploadedBy,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        Guard.AgainstNegativeOrZero(propertyCertificateId, nameof(propertyCertificateId));
        Guard.AgainstInvalidStream(fileStream, nameof(fileStream));
        Guard.AgainstNullOrWhiteSpace(originalFileName, nameof(originalFileName));
        Guard.AgainstNullOrWhiteSpace(mimeType, nameof(mimeType));
        Guard.AgainstNegativeOrZero(fileSizeBytes, nameof(fileSizeBytes));
        Guard.AgainstNegativeOrZero(uploadedBy, nameof(uploadedBy));

        _logger.LogInformation("Replacing document for PropertyCertificateId={Id}, NewFile={FileName}",
            propertyCertificateId, originalFileName);

        // Get existing certificate
        var certificate = await _propertyCertificateService.GetByIdAsync(
            propertyCertificateId,
            cancellationToken);

        if (certificate == null)
        {
            throw new InvalidOperationException($"PropertyCertificate with ID {propertyCertificateId} not found.");
        }

        var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var tempFilePath = Path.GetTempFileName();
        string? storagePath = null;
        string? oldStoragePath = certificate.DocumentBinding?.Document?.StoragePath;

        try
        {
            // Buffer and hash the new file
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
                await tempFileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
            }

            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var checksumSha256 = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();

            // Save new file to storage
            tempFileStream.Position = 0;
            storagePath = await _fileStorageService.SaveFileAsync(tempFileStream, originalFileName, cancellationToken);

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Create new document
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

                var (departmentId, moduleId) = await GetDepartmentAndModuleIdsAsync(cancellationToken);

                // Create new document binding
                var documentBindingId = await _documentService.CreateDocumentBindingAsync(
                    documentId,
                    departmentId,
                    moduleId,
                    "PropertyCertificates",  // Use plural table name to match [PTIS].[PropertyCertificates]
                    propertyCertificateId,
                    null,
                    "Id",
                    DocumentBindingPurpose.MainDocument.ToPurposeString(),
                    true,
                    departmentId,
                    uploadedBy,  // AuthReferenceId = userId (for proper authorization)
                    uploadedBy,
                    cancellationToken);

                // Update certificate with new binding
                await _propertyCertificateService.UpdateDocumentBindingAsync(
                    propertyCertificateId,
                    documentBindingId,
                    uploadedBy,
                    cancellationToken);

                // Soft-delete the old document so it can no longer be accessed after replacement.
                var oldDocumentGuid = certificate.DocumentBinding?.Document?.DocumentGuid;
                if (oldDocumentGuid.HasValue)
                {
                    await _documentService.DeleteDocumentAsync(oldDocumentGuid.Value, uploadedBy, cancellationToken);
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Delete old file from storage
                if (!string.IsNullOrEmpty(oldStoragePath))
                {
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(oldStoragePath, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old file: {Path}", oldStoragePath);
                    }
                }

                return new PropertyCertificateUploadResponseDto
                {
                    PropertyCertificateId = propertyCertificateId,
                    DocumentGuid = documentGuid,
                    DocumentId = documentId,
                    DocumentBindingId = documentBindingId,
                    PropertyId = certificate.PropertyId,
                    CertificateTypeId = certificate.CertificateTypeId,
                    CertificateNo = certificate.CertificateNo,
                    IssueDate = certificate.IssueDate,
                    FileName = originalFileName,
                    FileSizeBytes = fileSizeBytes,
                    StoragePath = storagePath
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                // Delete new file if transaction failed
                if (!string.IsNullOrEmpty(storagePath))
                {
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(storagePath, cancellationToken);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Failed to delete new file after rollback: {Path}", storagePath);
                    }
                }
                throw;
            }
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temp file: {Path}", tempFilePath);
                }
            }
        }
    }

    public async Task<PropertyCertificateBulkSaveResponseDto> BulkSaveAllAsync(
        PropertyCertificateBulkSaveDto bulkDto,
        int userId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(bulkDto.PropertyId, nameof(bulkDto.PropertyId));
        Guard.AgainstNegativeOrZero(userId, nameof(userId));

        _logger.LogInformation("Bulk saving {Count} certificates for PropertyId={PropertyId}, User={UserId}",
            bulkDto.Certificates.Count, bulkDto.PropertyId, userId);

        var response = new PropertyCertificateBulkSaveResponseDto
        {
            PropertyId = bulkDto.PropertyId,
            TotalProcessed = bulkDto.Certificates.Count
        };

        // Get existing certificates for this property (including inactive ones)
        // We need all certificates regardless of IsActive status to properly handle re-enabling
        var existingCertificates = await _propertyCertificateService.GetByPropertyIdIncludingInactiveAsync(
            bulkDto.PropertyId,
            PropertyCertificateIncludeOptions.None,
            cancellationToken);

        var existingLookup = existingCertificates
            .GroupBy(c => c.CertificateTypeId)
            .ToDictionary(g => g.Key, g => g.First());

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var certDto in bulkDto.Certificates)
            {
                try
                {
                    var exists = existingLookup.TryGetValue(certDto.CertificateTypeId, out var existingCert);

                    if (certDto.IsEnabled)
                    {
                        // User wants this certificate enabled
                        if (!exists)
                        {
                            // Create new certificate
                            var newCertId = await _propertyCertificateService.CreateAsync(
                                bulkDto.PropertyId,
                                certDto.CertificateTypeId,
                                certDto.CertificateNumber,
                                certDto.CertificateDate,
                                userId,
                                cancellationToken);

                            // Enable it
                            await _propertyCertificateService.ToggleEnabledAsync(
                                newCertId,
                                true,
                                userId,
                                cancellationToken);

                            response.EnabledCount++;
                            _logger.LogDebug("Created and enabled new certificate: TypeId={TypeId}, CertId={CertId}",
                                certDto.CertificateTypeId, newCertId);
                        }
                        else
                        {
                            // Update existing certificate (always call UpdateAsync to allow clearing values)
                            await _propertyCertificateService.UpdateAsync(
                                existingCert!.Id,
                                certDto.CertificateNumber,
                                certDto.CertificateDate,
                                userId,
                                cancellationToken);

                            // Enable if not already enabled
                            if (!existingCert!.IsActive)
                            {
                                await _propertyCertificateService.ToggleEnabledAsync(
                                    existingCert.Id,
                                    true,
                                    userId,
                                    cancellationToken);
                            }

                            response.EnabledCount++;
                            _logger.LogDebug("Updated and enabled existing certificate: CertId={CertId}",
                                existingCert.Id);
                        }
                    }
                    else
                    {
                        // User wants this certificate disabled
                        if (exists && existingCert != null && existingCert.IsActive)
                        {
                            // Disable existing certificate
                            await _propertyCertificateService.ToggleEnabledAsync(
                                existingCert.Id,
                                false,
                                userId,
                                cancellationToken);

                            response.DisabledCount++;
                            _logger.LogDebug("Disabled certificate: CertId={CertId}", existingCert.Id);
                        }
                        else
                        {
                            response.DisabledCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process certificate: TypeId={TypeId}",
                        certDto.CertificateTypeId);
                    response.Errors.Add($"Certificate Type {certDto.CertificateTypeId}: {ex.Message}");
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Get updated status for all certificates
            response.UpdatedCertificates = await GetCertificateTypesWithStatusAsync(
                bulkDto.PropertyId,
                cancellationToken);

            _logger.LogInformation("Bulk save completed: Enabled={Enabled}, Disabled={Disabled}, Errors={ErrorCount}",
                response.EnabledCount, response.DisabledCount, response.Errors.Count);

            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
