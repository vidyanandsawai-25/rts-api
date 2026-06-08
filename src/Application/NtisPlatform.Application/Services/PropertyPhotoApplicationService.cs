using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.Common;
using NtisPlatform.Application.DTOs.PropertyPhoto;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application service for PropertyPhoto operations.
/// Orchestrates file storage + CORE.Document + CORE.DocumentBinding + PTIS.PropertyPhoto,
/// reusing the existing Document infrastructure. SEPARATE from the Document service.
/// A property may hold zero, one or many photos per photo type.
/// </summary>
public class PropertyPhotoApplicationService : IPropertyPhotoApplicationService
{
    private readonly IPropertyPhotoService _propertyPhotoService;
    private readonly IDocumentService _documentService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<DepartmentMasterEntity, int> _departmentRepository;
    private readonly IRepository<ModuleMasterEntity, int> _moduleRepository;
    private readonly IRepository<PropertyPhotoTypeEntity, int> _photoTypeRepository;
    private readonly ILogger<PropertyPhotoApplicationService> _logger;
    private readonly int _bufferSizeBytes;
    private readonly long _maxFileSizeBytes;

    public PropertyPhotoApplicationService(
        IPropertyPhotoService propertyPhotoService,
        IDocumentService documentService,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        IRepository<DepartmentMasterEntity, int> departmentRepository,
        IRepository<ModuleMasterEntity, int> moduleRepository,
        IRepository<PropertyPhotoTypeEntity, int> photoTypeRepository,
        IOptions<FileStorageOptions> fileStorageOptions,
        ILogger<PropertyPhotoApplicationService> logger)
    {
        _propertyPhotoService = propertyPhotoService;
        _documentService = documentService;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
        _departmentRepository = departmentRepository;
        _moduleRepository = moduleRepository;
        _photoTypeRepository = photoTypeRepository;
        _logger = logger;

        var fileStorage = fileStorageOptions.Value;
        _bufferSizeBytes = fileStorage.BufferSizeBytes;
        _maxFileSizeBytes = fileStorage.MaxFileSizeBytes;
    }

    public async Task<PropertyPhotoUploadResponseDto> UploadPhotoAsync(
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        int propertyId,
        int photoTypeId,
        int? displayOrder,
        string? remarks,
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
        Guard.AgainstNegativeOrZero(photoTypeId, nameof(photoTypeId));
        Guard.AgainstNegativeOrZero(uploadedBy, nameof(uploadedBy));

        if (!string.IsNullOrWhiteSpace(remarks))
        {
            Guard.AgainstExceedingLength(remarks, 500, nameof(remarks));
        }

        // Validate the photo type up front (before any file I/O) and use its DB code as the
        // document type. Multiple photos are allowed per type, so there is no duplicate check.
        var photoType = await GetActivePhotoTypeAsync(photoTypeId, cancellationToken);
        if (photoType == null)
        {
            throw new ArgumentException($"PhotoTypeId {photoTypeId} is not a valid active photo type", nameof(photoTypeId));
        }

        _logger.LogInformation("Starting PropertyPhoto upload: {FileName}, PropertyId: {PropertyId}, PhotoTypeId: {PhotoTypeId}, User: {UserId}",
            originalFileName, propertyId, photoTypeId, uploadedBy);

        var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var tempFilePath = Path.GetTempFileName();
        string? storagePath = null;

        try
        {
            // 1. Buffer the upload once while computing the checksum in the same pass
            await using var tempFileStream = new FileStream(
                tempFilePath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                _bufferSizeBytes,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var checksumSha256 = await BufferAndHashAsync(fileStream, tempFileStream, cancellationToken);
            _logger.LogDebug("Computed SHA-256 checksum: {Checksum} for PropertyPhoto file: {FileName}",
                checksumSha256, originalFileName);

            // 2. Save the buffered file to storage
            tempFileStream.Position = 0;
            storagePath = await _fileStorageService.SaveFileAsync(tempFileStream, originalFileName, cancellationToken);
            _logger.LogInformation("PropertyPhoto file saved to storage: {StoragePath}", storagePath);

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    // 3. Create CORE.Document (document type = the photo type's DB code)
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
                        photoType.PhotoTypeCode,
                        cancellationToken);

                    // 4. Create PTIS.PropertyPhoto (without DocumentBinding); validates property
                    var propertyPhotoId = await _propertyPhotoService.CreateAsync(
                        propertyId,
                        photoTypeId,
                        displayOrder,
                        remarks,
                        uploadedBy,
                        cancellationToken);
                    _logger.LogInformation("PropertyPhoto created: Id={PropertyPhotoId}", propertyPhotoId);

                    // 5. Resolve PTIS department + property module from the database
                    var (departmentId, moduleId) = await GetDepartmentAndModuleIdsAsync(cancellationToken);

                    // 6. Create CORE.DocumentBinding pointing at the PropertyPhoto row
                    var documentBindingId = await _documentService.CreateDocumentBindingAsync(
                        documentId,
                        departmentId,
                        moduleId,
                        "PropertyPhoto",
                        propertyPhotoId,
                        null,
                        "Id",
                        null,
                        false,
                        departmentId,
                        uploadedBy,
                        uploadedBy,
                        cancellationToken);

                    // 7. Link the binding back to the PropertyPhoto row
                    await _propertyPhotoService.UpdateDocumentBindingAsync(
                        propertyPhotoId,
                        documentBindingId,
                        uploadedBy,
                        cancellationToken);

                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    _logger.LogInformation("PropertyPhoto upload completed: PropertyPhotoId={PropertyPhotoId}, DocumentGuid={DocumentGuid}",
                        propertyPhotoId, documentGuid);

                    return new PropertyPhotoUploadResponseDto
                    {
                        PropertyPhotoId = propertyPhotoId,
                        DocumentGuid = documentGuid,
                        DocumentId = documentId,
                        DocumentBindingId = documentBindingId,
                        PropertyId = propertyId,
                        PhotoTypeId = photoTypeId,
                        DisplayOrder = displayOrder,
                        Remarks = remarks,
                        FileName = originalFileName,
                        FileSizeBytes = fileSizeBytes,
                        StoragePath = storagePath
                    };
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }
            catch
            {
                // The file was already saved to storage; clean it up to avoid orphans.
                await TryDeleteStoredFileAsync(storagePath, "upload rollback", cancellationToken);
                throw;
            }
        }
        finally
        {
            TryDeleteTempFile(tempFilePath);
        }
    }

    public async Task<PropertyPhotoUploadResponseDto> ReplacePhotoAsync(
        int propertyPhotoId,
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        string? remarks,
        int uploadedBy,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        Guard.AgainstNegativeOrZero(propertyPhotoId, nameof(propertyPhotoId));
        Guard.AgainstInvalidStream(fileStream, nameof(fileStream));
        Guard.AgainstNullOrWhiteSpace(originalFileName, nameof(originalFileName));
        Guard.AgainstExceedingLength(originalFileName, 255, nameof(originalFileName));
        Guard.AgainstNullOrWhiteSpace(mimeType, nameof(mimeType));
        Guard.AgainstNegativeOrZero(fileSizeBytes, nameof(fileSizeBytes));
        Guard.AgainstOutOfRange(fileSizeBytes, 1, _maxFileSizeBytes, nameof(fileSizeBytes));
        Guard.AgainstNegativeOrZero(uploadedBy, nameof(uploadedBy));

        if (!string.IsNullOrWhiteSpace(remarks))
        {
            Guard.AgainstExceedingLength(remarks, 500, nameof(remarks));
        }

        _logger.LogInformation("Replacing PropertyPhoto Id={Id}, NewFile={FileName}", propertyPhotoId, originalFileName);

        // Load the photo being replaced (it carries the property/type/order we copy forward)
        var existing = await _propertyPhotoService.GetByIdAsync(propertyPhotoId, cancellationToken);

        if (existing == null)
        {
            throw new PropertyPhotoNotFoundException(propertyPhotoId);
        }

        // Only the current (latest) photo can be replaced; the previous version is retained.
        if (!existing.IsLatest)
        {
            throw new ArgumentException(
                $"PropertyPhoto with ID {propertyPhotoId} is a superseded version and cannot be replaced. Replace the current photo instead.",
                nameof(propertyPhotoId));
        }

        // Document type = the photo type's DB code (must be active to create a new latest row)
        var photoType = await GetActivePhotoTypeAsync(existing.PhotoTypeId, cancellationToken)
            ?? throw new InvalidOperationException($"Photo type with ID {existing.PhotoTypeId} is inactive; cannot replace PropertyPhoto {propertyPhotoId}.");
        var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var tempFilePath = Path.GetTempFileName();
        string? storagePath = null;

        try
        {
            // 1. Buffer + hash the new file
            await using var tempFileStream = new FileStream(
                tempFilePath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                _bufferSizeBytes,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var checksumSha256 = await BufferAndHashAsync(fileStream, tempFileStream, cancellationToken);

            // 2. Save the new file to storage
            tempFileStream.Position = 0;
            storagePath = await _fileStorageService.SaveFileAsync(tempFileStream, originalFileName, cancellationToken);

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    // 3. Supersede the current row. The old row, its document and file are
                    //    retained for audit history.
                    await _propertyPhotoService.MarkAsSupersededAsync(propertyPhotoId, uploadedBy, cancellationToken);

                    // 4. Create the new CORE.Document
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
                        photoType?.PhotoTypeCode,
                        cancellationToken);

                    // 5. Create the new latest PropertyPhoto row (carry over slot + ordering)
                    var newPropertyPhotoId = await _propertyPhotoService.CreateAsync(
                        existing.PropertyId,
                        existing.PhotoTypeId,
                        existing.DisplayOrder,
                        string.IsNullOrWhiteSpace(remarks) ? existing.Remarks : remarks,
                        uploadedBy,
                        cancellationToken);

                    // 6. Resolve department + module and create the new binding
                    var (departmentId, moduleId) = await GetDepartmentAndModuleIdsAsync(cancellationToken);

                    var documentBindingId = await _documentService.CreateDocumentBindingAsync(
                        documentId,
                        departmentId,
                        moduleId,
                        "PropertyPhoto",
                        newPropertyPhotoId,
                        null,
                        "Id",
                        null,
                        false,
                        departmentId,
                        uploadedBy,
                        uploadedBy,
                        cancellationToken);

                    // 7. Link the new binding to the new row
                    await _propertyPhotoService.UpdateDocumentBindingAsync(
                        newPropertyPhotoId,
                        documentBindingId,
                        uploadedBy,
                        cancellationToken);

                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    _logger.LogInformation("PropertyPhoto replaced: OldId={OldId}, NewId={NewId}, DocumentGuid={DocumentGuid}",
                        propertyPhotoId, newPropertyPhotoId, documentGuid);

                    return new PropertyPhotoUploadResponseDto
                    {
                        PropertyPhotoId = newPropertyPhotoId,
                        DocumentGuid = documentGuid,
                        DocumentId = documentId,
                        DocumentBindingId = documentBindingId,
                        PropertyId = existing.PropertyId,
                        PhotoTypeId = existing.PhotoTypeId,
                        DisplayOrder = existing.DisplayOrder,
                        Remarks = string.IsNullOrWhiteSpace(remarks) ? existing.Remarks : remarks,
                        FileName = originalFileName,
                        FileSizeBytes = fileSizeBytes,
                        StoragePath = storagePath
                    };
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }
            catch
            {
                await TryDeleteStoredFileAsync(storagePath, "replace rollback", cancellationToken);
                throw;
            }
        }
        finally
        {
            TryDeleteTempFile(tempFilePath);
        }
    }

    public async Task<List<PropertyPhotoDto>> GetPhotosByPropertyAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(propertyId, nameof(propertyId));

        var photos = await _propertyPhotoService.GetLatestByPropertyIdAsync(propertyId, cancellationToken);

        return photos.Select(MapToDto).ToList();
    }

    public async Task<PropertyPhotoGalleryDto> GetGroupedPhotosByPropertyAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(propertyId, nameof(propertyId));

        // All active photo types (the full set of slots)
        var allTypes = await _photoTypeRepository.GetAsync(t => t.IsActive, cancellationToken);

        // Current photos for this property, grouped by type (a type may have many)
        var existingPhotos = await _propertyPhotoService.GetLatestByPropertyIdAsync(propertyId, cancellationToken);

        var photosByType = existingPhotos
            .GroupBy(p => p.PhotoTypeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var groups = allTypes.OrderBy(t => t.DisplayOrder).Select(type =>
        {
            photosByType.TryGetValue(type.Id, out var typePhotos);
            var photos = (typePhotos ?? new List<PropertyPhotoEntity>())
                .Select(MapToDto)
                .ToList();

            return new PropertyPhotoTypeGroupDto
            {
                PhotoTypeId = type.Id,
                PhotoTypeCode = type.PhotoTypeCode,
                PhotoTypeName = type.PhotoTypeName,
                DisplayOrder = type.DisplayOrder,
                HasPhoto = photos.Count > 0,
                PhotoCount = photos.Count,
                Photos = photos
            };
        }).ToList();

        return new PropertyPhotoGalleryDto
        {
            PropertyId = propertyId,
            TotalPhotos = existingPhotos.Count,
            PhotoTypes = groups
        };
    }

    public async Task<List<PropertyPhotoTypeWithStatusDto>> GetPhotoTypesWithStatusAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(propertyId, nameof(propertyId));

        // All active photo types (the full set of slots)
        var allTypes = await _photoTypeRepository.GetAsync(t => t.IsActive, cancellationToken);

        // Current photos for this property, grouped by type (a type may have many)
        var existingPhotos = await _propertyPhotoService.GetLatestByPropertyIdAsync(propertyId, cancellationToken);

        var photosByType = existingPhotos
            .GroupBy(p => p.PhotoTypeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return allTypes.OrderBy(t => t.DisplayOrder).Select(type =>
        {
            photosByType.TryGetValue(type.Id, out var typePhotos);
            var count = typePhotos?.Count ?? 0;
            var representative = typePhotos?.FirstOrDefault();

            return new PropertyPhotoTypeWithStatusDto
            {
                PhotoTypeId = type.Id,
                PhotoTypeCode = type.PhotoTypeCode,
                PhotoTypeName = type.PhotoTypeName,
                DisplayOrder = type.DisplayOrder,
                HasPhoto = count > 0,
                PhotoCount = count,
                PropertyPhotoId = representative?.Id,
                Remarks = representative?.Remarks,
                DocumentBindingId = representative?.DocumentBindingId,
                DocumentGuid = representative != null ? GetSafeDocumentGuid(representative.DocumentBinding) : null,
                FileName = representative != null ? GetSafeFileName(representative.DocumentBinding) : null,
                MimeType = representative != null ? GetSafeMimeType(representative.DocumentBinding) : null
            };
        }).ToList();
    }

    public async Task<bool> DeletePhotoAsync(
        int propertyPhotoId,
        int deletedBy,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(propertyPhotoId, nameof(propertyPhotoId));
        Guard.AgainstNegativeOrZero(deletedBy, nameof(deletedBy));

        var existing = await _propertyPhotoService.GetByIdAsync(propertyPhotoId, cancellationToken);
        if (existing == null)
        {
            _logger.LogWarning("Delete failed - PropertyPhoto not found: {Id}", propertyPhotoId);
            return false;
        }

        await _propertyPhotoService.DeleteAsync(propertyPhotoId, deletedBy, cancellationToken);
        _logger.LogInformation("PropertyPhoto deleted: {Id} by user {UserId}", propertyPhotoId, deletedBy);
        return true;
    }

    // ========== Helpers ==========

    private async Task<PropertyPhotoTypeEntity?> GetActivePhotoTypeAsync(int photoTypeId, CancellationToken cancellationToken)
    {
        var types = await _photoTypeRepository.GetAsync(t => t.Id == photoTypeId && t.IsActive, cancellationToken);
        return types.FirstOrDefault();
    }

    /// <summary>
    /// Streams the source into the temp file while computing the SHA-256 checksum in one pass.
    /// </summary>
    private async Task<string> BufferAndHashAsync(Stream source, Stream tempFileStream, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        var buffer = new byte[_bufferSizeBytes];
        int bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            await tempFileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
    }

    private async Task TryDeleteStoredFileAsync(string? storagePath, string context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(storagePath))
            return;

        try
        {
            await _fileStorageService.DeleteFileAsync(storagePath, cancellationToken);
            _logger.LogWarning("Deleted orphaned file during PropertyPhoto {Context}: {StoragePath}", context, storagePath);
        }
        catch (Exception cleanupEx)
        {
            _logger.LogWarning(cleanupEx,
                "Failed to delete stored file during PropertyPhoto {Context}: {StoragePath}. File may remain orphaned and require manual cleanup.",
                context, storagePath);
        }
    }

    private void TryDeleteTempFile(string tempFilePath)
    {
        if (!File.Exists(tempFilePath))
            return;

        try
        {
            File.Delete(tempFilePath);
            _logger.LogDebug("Deleted temporary file: {TempFilePath}", tempFilePath);
        }
        catch (Exception ex)
        {
            // Log but don't throw - temp file cleanup failure shouldn't fail the operation
            _logger.LogWarning(ex, "Failed to delete temporary file: {TempFilePath}. File may need manual cleanup.", tempFilePath);
        }
    }

    /// <summary>
    /// Maps a PropertyPhoto entity (with PhotoType + DocumentBinding.Document loaded) to its DTO.
    /// </summary>
    private static PropertyPhotoDto MapToDto(PropertyPhotoEntity p) => new()
    {
        PropertyPhotoId = p.Id,
        PropertyId = p.PropertyId,
        PhotoTypeId = p.PhotoTypeId,
        PhotoTypeCode = p.PhotoType?.PhotoTypeCode ?? string.Empty,
        PhotoTypeName = p.PhotoType?.PhotoTypeName ?? string.Empty,
        DisplayOrder = p.DisplayOrder,
        Remarks = p.Remarks,
        DocumentBindingId = p.DocumentBindingId,
        DocumentGuid = GetSafeDocumentGuid(p.DocumentBinding),
        FileName = GetSafeFileName(p.DocumentBinding),
        MimeType = GetSafeMimeType(p.DocumentBinding)
    };

    /// <summary>
    /// Safely extracts the DocumentGuid from a DocumentBinding if the document is valid and active.
    /// </summary>
    private static Guid? GetSafeDocumentGuid(DocumentBindingEntity? documentBinding)
    {
        var document = documentBinding?.Document;
        if (document == null || !document.IsActive || document.MarkedForDeletion)
            return null;

        return document.DocumentGuid;
    }

    /// <summary>
    /// Safely extracts the original file name from a DocumentBinding if the document is valid and active.
    /// </summary>
    private static string? GetSafeFileName(DocumentBindingEntity? documentBinding)
    {
        var document = documentBinding?.Document;
        if (document == null || !document.IsActive || document.MarkedForDeletion)
            return null;

        return document.OriginalFileName;
    }

    /// <summary>
    /// Safely extracts the MIME type from a DocumentBinding if the document is valid and active.
    /// </summary>
    private static string? GetSafeMimeType(DocumentBindingEntity? documentBinding)
    {
        var document = documentBinding?.Document;
        if (document == null || !document.IsActive || document.MarkedForDeletion)
            return null;

        return document.MimeType;
    }

    /// <summary>
    /// Resolves the PTIS department and its property module from the database. No hardcoding.
    /// </summary>
    private async Task<(int DepartmentId, int ModuleId)> GetDepartmentAndModuleIdsAsync(CancellationToken cancellationToken)
    {
        // Step 1: Find the PTIS / PROPERTY department (exact match first, ordered by Id)
        var matchingDepartments = await _departmentRepository.GetAsync(
            d => d.IsActive && (d.DepartmentCode == "PTIS" || d.DepartmentCode == "PROPERTY"),
            cancellationToken);

        var department = matchingDepartments.OrderBy(d => d.Id).FirstOrDefault();

        if (department == null)
        {
            var departments = await _departmentRepository.GetAsync(d => d.IsActive, cancellationToken);
            var availableDepts = string.Join(", ",
                departments.Select(d => $"{d.DepartmentCode ?? "NULL"} (ID: {d.Id})"));
            throw new InvalidOperationException(
                $"No department with code PTIS or PROPERTY found in database. Available departments: {availableDepts}. " +
                "Please ensure a department with exact code 'PTIS' or 'PROPERTY' exists in DepartmentMaster table.");
        }

        // Step 2: Find a property/photo module under this department
        var modules = await _moduleRepository.GetAsync(
            m => m.DepartmentId == department.Id && m.IsActive,
            cancellationToken);

        var module = modules
            .Where(m => m.ModuleCode != null)
            .OrderBy(m => m.Id)
            .FirstOrDefault(m =>
                m.ModuleCode!.Equals("PROPERTY", StringComparison.OrdinalIgnoreCase) ||
                m.ModuleCode!.Equals("PROPERTYPHOTO", StringComparison.OrdinalIgnoreCase) ||
                m.ModuleCode!.Equals("PHOTO", StringComparison.OrdinalIgnoreCase));

        // Fallback to substring match (still ordered by Id for determinism)
        if (module == null)
        {
            module = modules
                .Where(m => m.ModuleCode != null)
                .OrderBy(m => m.Id)
                .FirstOrDefault(m =>
                    m.ModuleCode!.Contains("PROPERTY", StringComparison.OrdinalIgnoreCase) ||
                    m.ModuleCode!.Contains("PHOTO", StringComparison.OrdinalIgnoreCase));
        }

        if (module == null)
        {
            var availableModules = string.Join(", ", modules.Select(m => $"{m.ModuleCode ?? "NULL"} (ID: {m.Id})"));
            throw new InvalidOperationException(
                $"No module with code PROPERTY/PROPERTYPHOTO/PHOTO found for department '{department.DepartmentCode}' (ID: {department.Id}). Available modules: {availableModules}. " +
                "Please ensure ModuleMaster table has at least one active module for this department.");
        }

        _logger.LogDebug("Resolved PropertyPhoto context: Department={DeptCode} (ID={DeptId}), Module={ModCode} (ID={ModId})",
            department.DepartmentCode, department.Id, module.ModuleCode, module.Id);

        return (department.Id, module.Id);
    }
}
