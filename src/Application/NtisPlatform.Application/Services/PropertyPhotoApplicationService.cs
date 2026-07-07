using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Common;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.DTOs.PropertyPhoto;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;

// DocumentBindingHelper is in NtisPlatform.Application.Common — no extra using needed (same assembly).

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application service for PropertyPhoto operations.
/// Delegates all file handling to DocumentApplicationService.
/// A property may hold zero, one or many photos per photo type.
/// </summary>
public class PropertyPhotoApplicationService : IPropertyPhotoApplicationService
{
    private readonly IPropertyPhotoService _propertyPhotoService;
    private readonly IDocumentApplicationService _documentApplicationService;
    private readonly IModuleLookupService _moduleLookupService;
    private readonly IRepository<PropertyPhotoTypeEntity, int> _photoTypeRepository;
    private readonly ILogger<PropertyPhotoApplicationService> _logger;

    public PropertyPhotoApplicationService(
        IPropertyPhotoService propertyPhotoService,
        IDocumentApplicationService documentApplicationService,
        IModuleLookupService moduleLookupService,
        IRepository<PropertyPhotoTypeEntity, int> photoTypeRepository,
        ILogger<PropertyPhotoApplicationService> logger)
    {
        _propertyPhotoService = propertyPhotoService;
        _documentApplicationService = documentApplicationService;
        _moduleLookupService = moduleLookupService;
        _photoTypeRepository = photoTypeRepository;
        _logger = logger;
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

        try
        {
            // 1. Resolve PTIS department + property module from the database FIRST (before creating photo)
            var (departmentId, moduleId) = await GetDepartmentAndModuleIdsAsync(cancellationToken);
            _logger.LogInformation("Department and Module resolved: DepartmentId={DepartmentId}, ModuleId={ModuleId}",
                departmentId, moduleId);

            // ATOMIC OPERATION: Create photo + upload document together.
            // If document upload fails, PropertyPhoto is rolled back to prevent orphans.
            int propertyPhotoId = 0;

            try
            {
                // 2. Create PropertyPhoto (without DocumentBinding); validates property
                propertyPhotoId = await _propertyPhotoService.CreateAsync(
                    propertyId,
                    photoTypeId,
                    displayOrder,
                    remarks,
                    uploadedBy,
                    cancellationToken);
                _logger.LogInformation("PropertyPhoto created: Id={PropertyPhotoId}", propertyPhotoId);

                // 3. Delegate file handling to DocumentApplicationService
                var uploadDto = new DocumentUploadDto
                {
                    DepartmentId = departmentId,
                    ModuleId = moduleId,
                    ReferenceTableName = "PropertyPhoto",
                    ReferenceTableId = propertyPhotoId,
                    ReferencePropertyName = "Id",
                    BindingPurpose = null,  // No specific binding purpose for photos
                    IsPrimaryDocument = false,
                    AuthDepartmentId = departmentId,
                    AuthReferenceId = propertyId,
                    DocumentType = photoType.PhotoTypeCode  // Use photo type's code as document type
                };

                var docResponse = await _documentApplicationService.UploadDocumentAsync(
                    fileStream,
                    originalFileName,
                    mimeType,
                    fileSizeBytes,
                    uploadDto,
                    uploadedBy,
                    cancellationToken);
                _logger.LogInformation("Document uploaded: DocumentGuid={DocumentGuid}, DocumentBindingId={DocumentBindingId}",
                    docResponse.DocumentGuid, docResponse.DocumentBindingId);

                // 4. Link the binding back to the PropertyPhoto row (ALWAYS, not conditional)
                if (docResponse.DocumentBindingId.HasValue)
                {
                    await _propertyPhotoService.UpdateDocumentBindingAsync(
                        propertyPhotoId,
                        docResponse.DocumentBindingId.Value,
                        uploadedBy,
                        cancellationToken);
                    _logger.LogInformation("DocumentBinding linked: PropertyPhotoId={PropertyPhotoId}, BindingId={BindingId}",
                        propertyPhotoId, docResponse.DocumentBindingId.Value);
                }
                else
                {
                    _logger.LogWarning("Document uploaded but NO DocumentBindingId returned. PropertyPhotoId={PropertyPhotoId}, DocumentGuid={DocumentGuid}",
                        propertyPhotoId, docResponse.DocumentGuid);
                }

                _logger.LogInformation("PropertyPhoto upload completed: PropertyPhotoId={PropertyPhotoId}, DocumentGuid={DocumentGuid}",
                    propertyPhotoId, docResponse.DocumentGuid);

                return new PropertyPhotoUploadResponseDto
                {
                    PropertyPhotoId = propertyPhotoId,
                    DocumentGuid = docResponse.DocumentGuid,
                    DocumentId = docResponse.DocumentId,
                    DocumentBindingId = docResponse.DocumentBindingId ?? 0,
                    PropertyId = propertyId,
                    PhotoTypeId = photoTypeId,
                    DisplayOrder = displayOrder,
                    Remarks = remarks,
                    FileName = originalFileName,
                    FileSizeBytes = fileSizeBytes,
                    StoragePath = docResponse.StoragePath ?? string.Empty
                };
            }
            catch (Exception uploadEx)
            {
                // COMPENSATION: If document upload fails after PropertyPhoto was created,
                // delete the orphaned PropertyPhoto to prevent broken gallery slots
                if (propertyPhotoId > 0)
                {
                    try
                    {
                        _logger.LogWarning(uploadEx,
                            "Document upload failed for PropertyPhotoId={PropertyPhotoId}. Rolling back PropertyPhoto creation.",
                            propertyPhotoId);

                        await _propertyPhotoService.DeleteAsync(propertyPhotoId, uploadedBy, cancellationToken);
                        _logger.LogInformation("Orphaned PropertyPhoto deleted: {PropertyPhotoId}", propertyPhotoId);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogError(deleteEx,
                            "Failed to cleanup orphaned PropertyPhoto {PropertyPhotoId} after upload failure. Manual cleanup may be needed.",
                            propertyPhotoId);
                    }
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PropertyPhoto upload failed. Exception: {Message}", ex.Message);
            throw;
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

        try
        {
            int newPropertyPhotoId = 0;

            try
            {
                // 1. Supersede the current row. The old row, its document and file are
                //    retained for audit history.
                await _propertyPhotoService.MarkAsSupersededAsync(propertyPhotoId, uploadedBy, cancellationToken);

                // 2. Create the new latest PropertyPhoto row (carry over slot + ordering)
                newPropertyPhotoId = await _propertyPhotoService.CreateAsync(
                    existing.PropertyId,
                    existing.PhotoTypeId,
                    existing.DisplayOrder,
                    string.IsNullOrWhiteSpace(remarks) ? existing.Remarks : remarks,
                    uploadedBy,
                    cancellationToken);

                // 3. Resolve department + module
                var (departmentId, moduleId) = await GetDepartmentAndModuleIdsAsync(cancellationToken);

                // 4. Upload new file via DocumentApplicationService
                var uploadDto = new DocumentUploadDto
                {
                    DepartmentId = departmentId,
                    ModuleId = moduleId,
                    ReferenceTableName = "PropertyPhoto",
                    ReferenceTableId = newPropertyPhotoId,
                    ReferencePropertyName = "Id",
                    BindingPurpose = null,  // No specific binding purpose for photos
                    IsPrimaryDocument = false,
                    AuthDepartmentId = departmentId,
                    AuthReferenceId = existing.PropertyId,
                    DocumentType = photoType.PhotoTypeCode  // Use photo type's code as document type
                };

                var docResponse = await _documentApplicationService.UploadDocumentAsync(
                    fileStream,
                    originalFileName,
                    mimeType,
                    fileSizeBytes,
                    uploadDto,
                    uploadedBy,
                    cancellationToken);

                // 5. Link the new binding to the new row
                if (docResponse.DocumentBindingId.HasValue)
                {
                    await _propertyPhotoService.UpdateDocumentBindingAsync(
                        newPropertyPhotoId,
                        docResponse.DocumentBindingId.Value,
                        uploadedBy,
                        cancellationToken);
                }

                _logger.LogInformation("PropertyPhoto replaced: OldId={OldId}, NewId={NewId}, DocumentGuid={DocumentGuid}",
                    propertyPhotoId, newPropertyPhotoId, docResponse.DocumentGuid);

                return new PropertyPhotoUploadResponseDto
                {
                    PropertyPhotoId = newPropertyPhotoId,
                    DocumentGuid = docResponse.DocumentGuid,
                    DocumentId = docResponse.DocumentId,
                    DocumentBindingId = docResponse.DocumentBindingId ?? 0,
                    PropertyId = existing.PropertyId,
                    PhotoTypeId = existing.PhotoTypeId,
                    DisplayOrder = existing.DisplayOrder,
                    Remarks = string.IsNullOrWhiteSpace(remarks) ? existing.Remarks : remarks,
                    FileName = originalFileName,
                    FileSizeBytes = fileSizeBytes,
                    StoragePath = docResponse.StoragePath ?? string.Empty
                };
            }
            catch (Exception uploadEx)
            {
                // COMPENSATION: If document upload fails after superseding the old photo,
                // restore the old photo to latest state and delete the orphaned new photo
                if (newPropertyPhotoId > 0)
                {
                    try
                    {
                        _logger.LogWarning(uploadEx,
                            "Document upload failed during replacement. Rolling back: restoring old photo (Id={OldId}), deleting new photo (Id={NewId})",
                            propertyPhotoId, newPropertyPhotoId);

                        // Delete the new photo that never got a document
                        await _propertyPhotoService.DeleteAsync(newPropertyPhotoId, uploadedBy, cancellationToken);

                        // Restore the old photo back to latest (undo the superseding)
                        await _propertyPhotoService.RestoreFromSupersedingAsync(propertyPhotoId, uploadedBy, cancellationToken);

                        _logger.LogInformation("Rollback completed: Old photo restored to latest, orphaned new photo deleted");
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx,
                            "Failed to rollback photo replacement (Old={OldId}, New={NewId}). Manual intervention may be needed.",
                            propertyPhotoId, newPropertyPhotoId);
                    }
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PropertyPhoto replacement failed for Id={PropertyPhotoId}. Exception: {Message}",
                propertyPhotoId, ex.Message);
            throw;
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

                .OrderBy(p => p.DisplayOrder)

                .ThenBy(p => p.Id)

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

            var representative = typePhotos?.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id).FirstOrDefault();

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
                DocumentGuid = representative != null ? DocumentBindingHelper.GetSafeDocumentGuid(representative.DocumentBinding) : null,
                FileName = representative != null ? DocumentBindingHelper.GetSafeFileName(representative.DocumentBinding) : null,
                MimeType = representative != null ? DocumentBindingHelper.GetSafeMimeType(representative.DocumentBinding) : null
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
    /// Maps a PropertyPhoto entity (with PhotoType + DocumentBinding.Document loaded) to its DTO.
    /// </summary>
    /// <summary>
    /// Maps a PropertyPhoto entity (with PhotoType + DocumentBinding.Document loaded) to its DTO.
    /// Navigation-safe document fields are extracted via the shared <see cref="DocumentBindingHelper"/>.
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
        DocumentGuid = DocumentBindingHelper.GetSafeDocumentGuid(p.DocumentBinding),
        FileName = DocumentBindingHelper.GetSafeFileName(p.DocumentBinding),
        MimeType = DocumentBindingHelper.GetSafeMimeType(p.DocumentBinding)
    };

    /// <summary>
    /// Resolves the PTIS department and its property module from the database. No hardcoding.
    /// </summary>
    private async Task<(int DepartmentId, int ModuleId)> GetDepartmentAndModuleIdsAsync(CancellationToken cancellationToken)
    {
        // Delegate to IModuleLookupService for table-driven module/department resolution
        return await _moduleLookupService.GetDepartmentAndModuleAsync("PTIS", "PROPERTY", cancellationToken);
    }
}
