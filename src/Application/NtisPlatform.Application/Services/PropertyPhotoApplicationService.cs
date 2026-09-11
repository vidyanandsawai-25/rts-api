using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Common;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.DTOs.PropertyPhoto;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application service for PropertyPhoto operations (Property 'P', Society 'S', Wing 'W').
/// Delegates file handling to DocumentApplicationService.
/// </summary>
public class PropertyPhotoApplicationService : IPropertyPhotoApplicationService
{
    private readonly IPropertyPhotoService _propertyPhotoService;
    private readonly IDocumentApplicationService _documentApplicationService;
    private readonly IModuleLookupService _moduleLookupService;
    private readonly IRepository<PropertyPhotoTypeEntity, int> _photoTypeRepository;
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyCategoryEntity, int> _categoryRepository;
    private readonly IRepository<SocietyDetailsEntity, int> _societyRepository;
    private readonly ILogger<PropertyPhotoApplicationService> _logger;

    public PropertyPhotoApplicationService(
        IPropertyPhotoService propertyPhotoService,
        IDocumentApplicationService documentApplicationService,
        IModuleLookupService moduleLookupService,
        IRepository<PropertyPhotoTypeEntity, int> photoTypeRepository,
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyCategoryEntity, int> categoryRepository,
        IRepository<SocietyDetailsEntity, int> societyRepository,
        ILogger<PropertyPhotoApplicationService> logger)
    {
        _propertyPhotoService = propertyPhotoService;
        _documentApplicationService = documentApplicationService;
        _moduleLookupService = moduleLookupService;
        _photoTypeRepository = photoTypeRepository;
        _propertyRepository = propertyRepository;
        _categoryRepository = categoryRepository;
        _societyRepository = societyRepository;
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

        var photoType = await GetActivePhotoTypeAsync(photoTypeId, cancellationToken);
        if (photoType == null)
        {
            throw new ArgumentException($"PhotoTypeId {photoTypeId} is not a valid active photo type", nameof(photoTypeId));
        }

        _logger.LogInformation("Starting PropertyPhoto upload: {FileName}, PropertyId: {PropertyId}, PhotoTypeId: {PhotoTypeId}, User: {UserId}",
            originalFileName, propertyId, photoTypeId, uploadedBy);

        try
        {
            var (departmentId, moduleId) = await GetDepartmentAndModuleIdsAsync(cancellationToken);

            int propertyPhotoId = 0;

            try
            {
                propertyPhotoId = await _propertyPhotoService.CreateWithDetailsAsync(
                    propertyId,
                    photoTypeId,
                    displayOrder,
                    remarks,
                    uploadedBy,
                    cancellationToken);

                var uploadDto = new DocumentUploadDto
                {
                    DepartmentId = departmentId,
                    ModuleId = moduleId,
                    ReferenceTableName = "PropertyPhoto",
                    ReferenceTableId = propertyPhotoId,
                    ReferencePropertyName = "Id",
                    BindingPurpose = null,
                    IsPrimaryDocument = false,
                    AuthDepartmentId = departmentId,
                    AuthReferenceId = propertyId,
                    DocumentType = photoType.PhotoTypeCode
                };

                var docResponse = await _documentApplicationService.UploadDocumentAsync(
                    fileStream,
                    originalFileName,
                    mimeType,
                    fileSizeBytes,
                    uploadDto,
                    uploadedBy,
                    cancellationToken);

                if (docResponse.DocumentBindingId.HasValue)
                {
                    await _propertyPhotoService.UpdateDocumentBindingAsync(
                        propertyPhotoId,
                        docResponse.DocumentBindingId.Value,
                        uploadedBy,
                        cancellationToken);
                }

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
                if (propertyPhotoId > 0)
                {
                    try
                    {
                        await _propertyPhotoService.DeleteAsync(propertyPhotoId, uploadedBy, cancellationToken);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogError(deleteEx, "Failed to cleanup orphaned PropertyPhoto {PropertyPhotoId}", propertyPhotoId);
                    }
                }
                throw uploadEx;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PropertyPhoto upload failed: {Message}", ex.Message);
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

        var existing = await _propertyPhotoService.GetByIdAsync(propertyPhotoId, cancellationToken);
        if (existing == null)
        {
            throw new PropertyPhotoNotFoundException(propertyPhotoId);
        }

        if (!existing.IsLatest)
        {
            throw new ArgumentException($"PropertyPhoto with ID {propertyPhotoId} is superseded.", nameof(propertyPhotoId));
        }

        var photoType = await GetActivePhotoTypeAsync(existing.PhotoTypeId, cancellationToken)
            ?? throw new InvalidOperationException($"Photo type {existing.PhotoTypeId} is inactive.");

        int newPropertyPhotoId = 0;

        try
        {
            await _propertyPhotoService.MarkAsSupersededAsync(propertyPhotoId, uploadedBy, cancellationToken);

            newPropertyPhotoId = await _propertyPhotoService.CreateWithDetailsAsync(
                existing.PropertyId ?? 0,
                existing.PhotoTypeId,
                existing.DisplayOrder,
                string.IsNullOrWhiteSpace(remarks) ? existing.Remarks : remarks,
                uploadedBy,
                cancellationToken);

            var (departmentId, moduleId) = await GetDepartmentAndModuleIdsAsync(cancellationToken);

            var uploadDto = new DocumentUploadDto
            {
                DepartmentId = departmentId,
                ModuleId = moduleId,
                ReferenceTableName = "PropertyPhoto",
                ReferenceTableId = newPropertyPhotoId,
                ReferencePropertyName = "Id",
                BindingPurpose = null,
                IsPrimaryDocument = false,
                AuthDepartmentId = departmentId,
                AuthReferenceId = existing.PropertyId ?? 0,
                DocumentType = photoType.PhotoTypeCode
            };

            var docResponse = await _documentApplicationService.UploadDocumentAsync(
                fileStream,
                originalFileName,
                mimeType,
                fileSizeBytes,
                uploadDto,
                uploadedBy,
                cancellationToken);

            if (docResponse.DocumentBindingId.HasValue)
            {
                await _propertyPhotoService.UpdateDocumentBindingAsync(
                    newPropertyPhotoId,
                    docResponse.DocumentBindingId.Value,
                    uploadedBy,
                    cancellationToken);
            }

            return new PropertyPhotoUploadResponseDto
            {
                PropertyPhotoId = newPropertyPhotoId,
                DocumentGuid = docResponse.DocumentGuid,
                DocumentId = docResponse.DocumentId,
                DocumentBindingId = docResponse.DocumentBindingId ?? 0,
                PropertyId = existing.PropertyId ?? 0,
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
            if (newPropertyPhotoId > 0)
            {
                try
                {
                    await _propertyPhotoService.DeleteAsync(newPropertyPhotoId, uploadedBy, cancellationToken);
                    await _propertyPhotoService.RestoreFromSupersedingAsync(propertyPhotoId, uploadedBy, cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Failed to rollback replacement for Old={OldId}", propertyPhotoId);
                }
            }
            throw uploadEx;
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

        var photoScope = await ResolvePhotoScopeAsync(propertyId, cancellationToken);
        var allTypesRaw = await _photoTypeRepository.GetAsync(
            t => t.IsActive && (t.PhotoScope == photoScope
                || (photoScope == "AMENITY" && (t.PhotoScope == "AMENITY" || t.PhotoTypeCode == "PROPERTY_PLAN" || t.PhotoTypeCode == "PHOTO_PLAN"))
                || (photoScope == "SOCIETY" && t.PhotoScope == "WING")
                || (photoScope == "SOCIETY" && t.PhotoTypeCode == "PROPERTY_PLAN")),
            cancellationToken);

        var existingPhotos = await _propertyPhotoService.GetLatestByPropertyIdAsync(propertyId, cancellationToken);

        var photosByType = existingPhotos
            .GroupBy(p => p.PhotoTypeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var allTypes = allTypesRaw.ToList();

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

        var photoScope = await ResolvePhotoScopeAsync(propertyId, cancellationToken);
        var allTypesRaw = await _photoTypeRepository.GetAsync(
            t => t.IsActive && (t.PhotoScope == photoScope
                || (photoScope == "AMENITY" && (t.PhotoScope == "AMENITY" || t.PhotoTypeCode == "PROPERTY_PLAN" || t.PhotoTypeCode == "PHOTO_PLAN"))
                || (photoScope == "SOCIETY" && t.PhotoScope == "WING")
                || (photoScope == "SOCIETY" && t.PhotoTypeCode == "PROPERTY_PLAN")),
            cancellationToken);

        var existingPhotos = await _propertyPhotoService.GetLatestByPropertyIdAsync(propertyId, cancellationToken);

        var photosByType = existingPhotos
            .GroupBy(p => p.PhotoTypeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var allTypes = allTypesRaw.ToList();

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

    // ========== Society Photos ==========

    public async Task<List<PropertyPhotoDto>> GetPhotosBySocietyAsync(
        int societyId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(societyId, nameof(societyId));
        var photos = await _propertyPhotoService.GetLatestBySocietyIdAsync(societyId, cancellationToken);
        return photos.Select(MapToDto).ToList();
    }

    public async Task<PropertyPhotoGalleryDto> GetGroupedPhotosBySocietyAsync(
        int societyId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(societyId, nameof(societyId));

        var allTypes = await _photoTypeRepository.GetAsync(t => t.IsActive && t.PhotoScope == "SOCIETY", cancellationToken);
        var existingPhotos = await _propertyPhotoService.GetLatestBySocietyIdAsync(societyId, cancellationToken);

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
            PropertyId = societyId,
            TotalPhotos = existingPhotos.Count,
            PhotoTypes = groups
        };
    }

    public async Task<List<PropertyPhotoTypeWithStatusDto>> GetPhotoTypesWithStatusForSocietyAsync(
        int societyId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(societyId, nameof(societyId));

        var allTypes = await _photoTypeRepository.GetAsync(t => t.IsActive && t.PhotoScope == "SOCIETY", cancellationToken);
        var existingPhotos = await _propertyPhotoService.GetLatestBySocietyIdAsync(societyId, cancellationToken);

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

    // ========== Wing Photos ==========

    public async Task<List<PropertyPhotoDto>> GetPhotosByWingAsync(
        int wingId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(wingId, nameof(wingId));
        var photos = await _propertyPhotoService.GetLatestByWingIdAsync(wingId, cancellationToken);
        return photos.Select(MapToDto).ToList();
    }

    public async Task<PropertyPhotoGalleryDto> GetGroupedPhotosByWingAsync(
        int wingId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(wingId, nameof(wingId));

        var allTypes = await _photoTypeRepository.GetAsync(t => t.IsActive && t.PhotoScope == "WING", cancellationToken);
        var existingPhotos = await _propertyPhotoService.GetLatestByWingIdAsync(wingId, cancellationToken);

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
            PropertyId = wingId,
            TotalPhotos = existingPhotos.Count,
            PhotoTypes = groups
        };
    }

    public async Task<List<PropertyPhotoTypeWithStatusDto>> GetPhotoTypesWithStatusForWingAsync(
        int wingId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(wingId, nameof(wingId));

        var allTypes = await _photoTypeRepository.GetAsync(t => t.IsActive && t.PhotoScope == "WING", cancellationToken);
        var existingPhotos = await _propertyPhotoService.GetLatestByWingIdAsync(wingId, cancellationToken);

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
            return false;
        }

        await _propertyPhotoService.DeleteAsync(propertyPhotoId, deletedBy, cancellationToken);
        return true;
    }

    // ========== Helpers ==========

    private async Task<PropertyPhotoTypeEntity?> GetActivePhotoTypeAsync(int photoTypeId, CancellationToken cancellationToken)
    {
        var types = await _photoTypeRepository.GetAsync(t => t.Id == photoTypeId && t.IsActive, cancellationToken);
        return types.FirstOrDefault();
    }

    private static PropertyPhotoDto MapToDto(PropertyPhotoEntity p) => new()
    {
        PropertyPhotoId = p.Id,
        PropertyId = p.PropertyId ?? (p.SocietyDetailId ?? p.WingDetailId ?? 0),
        PhotoTypeId = p.PhotoTypeId,
        PhotoTypeCode = p.PhotoType?.PhotoTypeCode ?? string.Empty,
        PhotoTypeName = p.PhotoType?.PhotoTypeName ?? string.Empty,
        DisplayOrder = p.DisplayOrder,
        Remarks = p.Remarks,
        DocumentBindingId = p.DocumentBindingId,
        DocumentGuid = DocumentBindingHelper.GetSafeDocumentGuid(p.DocumentBinding),
        FileName = DocumentBindingHelper.GetSafeFileName(p.DocumentBinding),
        MimeType = DocumentBindingHelper.GetSafeMimeType(p.DocumentBinding),
        WingDetailId = p.WingDetailId,
        WingName = p.WingDetail != null ? (p.WingDetail.WingName ?? p.WingDetail.WingMaster?.WingNo) : null
    };

    private async Task<(int DepartmentId, int ModuleId)> GetDepartmentAndModuleIdsAsync(CancellationToken cancellationToken)
    {
        return await _moduleLookupService.GetDepartmentAndModuleAsync("PTIS", "PROPERTY", cancellationToken);
    }

    private async Task<bool> IsApartmentOrWingPropertyAsync(int propertyId, CancellationToken cancellationToken)
    {
        var property = await _propertyRepository.GetByIdAsync(propertyId, cancellationToken);
        if (property == null) return false;

        if (property.WingDetailId.HasValue) return true;

        if (property.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(property.CategoryId.Value, cancellationToken);
            if (category != null && !string.IsNullOrEmpty(category.PropertyCategoryName))
            {
                return NtisPlatform.Core.Constants.PropertyCategoryConstants.ApartmentCategoryNames
                    .Any(ac => category.PropertyCategoryName.Contains(ac, StringComparison.OrdinalIgnoreCase));
            }
        }

        return false;
    }

    private async Task<string> ResolvePhotoScopeAsync(int propertyId, CancellationToken cancellationToken)
    {
        var property = await _propertyRepository.GetByIdAsync(propertyId, cancellationToken);
        if (property != null)
        {
            // PartitionNo (not FlatOrShopNo, which is just a display label) is the codebase-wide
            // signal for "this property row is a unit" -- see PropertySearchRepository,
            // PropertyRepository, and every ApartmentQC workflow-stage repository's
            // structure-vs-unit split.
            bool isUnit = !string.IsNullOrWhiteSpace(property.PartitionNo)
                          && property.PartitionNo.Trim() != "-";

            if (property.PropertyTypeId == 140 || (isUnit && property.PartitionNo?.Trim().StartsWith("AM", StringComparison.OrdinalIgnoreCase) == true))
            {
                return "AMENITY";
            }

            if (isUnit)
            {
                return "PROPERTY";
            }

            if (property.CategoryId.HasValue)
            {
                var category = await _categoryRepository.GetByIdAsync(property.CategoryId.Value, cancellationToken);
                if (category != null && !string.IsNullOrEmpty(category.PropertyCategoryName))
                {
                    var catName = category.PropertyCategoryName;
                    if (catName.Contains("Society", StringComparison.OrdinalIgnoreCase))
                    {
                        return "SOCIETY";
                    }
                    if (catName.Contains("Wing", StringComparison.OrdinalIgnoreCase))
                    {
                        return "WING";
                    }
                    if (catName.Contains("Amenity", StringComparison.OrdinalIgnoreCase))
                    {
                        return "AMENITY";
                    }
                    if (catName.Contains("Apartment", StringComparison.OrdinalIgnoreCase))
                    {
                        return "SOCIETY";
                    }
                }
            }

            if (property.WingDetailId.HasValue)
            {
                return "WING";
            }
        }
        return "PROPERTY";
    }
}
