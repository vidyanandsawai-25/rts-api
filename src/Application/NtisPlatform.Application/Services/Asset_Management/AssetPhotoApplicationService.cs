using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Common;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Asset_Management;

namespace NtisPlatform.Application.Services.Asset_Management;

/// <summary>
/// Application service for AssetPhoto operations.
/// </summary>
public class AssetPhotoApplicationService : IAssetPhotoApplicationService
{
    private readonly IAssetPhotoService _photoService;
    private readonly IRepository<AssetPhotoTypeEntity, int> _photoTypeRepository;
    private readonly IRepository<AssetMasterEntity, int> _assetMasterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssetPhotoApplicationService> _logger;

    public AssetPhotoApplicationService(
        IAssetPhotoService photoService,
        IUnitOfWork unitOfWork,
        IRepository<AssetPhotoTypeEntity, int> photoTypeRepository,
        IRepository<AssetMasterEntity, int> assetMasterRepository,
        ILogger<AssetPhotoApplicationService> logger)
    {
        _photoService = photoService;
        _unitOfWork = unitOfWork;
        _photoTypeRepository = photoTypeRepository;
        _assetMasterRepository = assetMasterRepository;
        _logger = logger;
    }

    public async Task<List<AssetPhotoDto>> GetPhotosByAssetAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(assetId, nameof(assetId));
        var photos = await _photoService.GetLatestByAssetIdAsync(assetId, cancellationToken);
        return photos.Select(MapToDto).ToList();
    }

    public async Task<AssetPhotoGalleryDto> GetGroupedPhotosByAssetAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(assetId, nameof(assetId));

        var assetList = await _assetMasterRepository.GetAsync(a => a.Id == assetId, cancellationToken);
        var asset = assetList.FirstOrDefault();
        if (asset == null)
        {
            throw new ArgumentException($"Asset with ID {assetId} not found", nameof(assetId));
        }

        var existingPhotos = await _photoService.GetLatestByAssetIdAsync(assetId, cancellationToken);
        var existingPhotoTypeIds = existingPhotos.Select(p => p.PhotoTypeId).Distinct().ToList();

        var allTypes = await _photoTypeRepository.GetAsync(
            t => t.IsActive 
                 && ((t.AssetCategoryId == null && t.AssetTypeId == null)
                     || (asset.AssetCategoryId != null
                         && t.AssetCategoryId == asset.AssetCategoryId
                         && (t.AssetTypeId == null || t.AssetTypeId == asset.AssetTypeId))
                     || existingPhotoTypeIds.Contains(t.Id)), 
            cancellationToken);

        var photosByType = existingPhotos
            .GroupBy(p => p.PhotoTypeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var groups = allTypes.OrderBy(t => t.DisplayOrder).Select(type =>
        {
            photosByType.TryGetValue(type.Id, out var typePhotos);
            var photos = (typePhotos ?? new List<AssetPhotoEntity>())
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.Id)
                .Select(MapToDto)
                .ToList();

            return new AssetPhotoTypeGroupDto
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

        return new AssetPhotoGalleryDto
        {
            AssetId = assetId,
            TotalPhotos = existingPhotos.Count,
            PhotoTypes = groups
        };
    }

    public async Task<List<AssetPhotoTypeWithStatusDto>> GetPhotoTypesWithStatusAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(assetId, nameof(assetId));

        var assetList = await _assetMasterRepository.GetAsync(a => a.Id == assetId, cancellationToken);
        var asset = assetList.FirstOrDefault();
        if (asset == null)
        {
            throw new ArgumentException($"Asset with ID {assetId} not found", nameof(assetId));
        }

        var existingPhotos = await _photoService.GetLatestByAssetIdIncludingInactiveAsync(assetId, cancellationToken);
        var existingPhotoTypeIds = existingPhotos.Select(p => p.PhotoTypeId).Distinct().ToList();

        var allTypes = await _photoTypeRepository.GetAsync(
            t => t.IsActive 
                 && ((t.AssetCategoryId == null && t.AssetTypeId == null)
                     || (asset.AssetCategoryId != null
                         && t.AssetCategoryId == asset.AssetCategoryId
                         && (t.AssetTypeId == null || t.AssetTypeId == asset.AssetTypeId))
                     || existingPhotoTypeIds.Contains(t.Id)), 
            cancellationToken);

        var photosByType = existingPhotos
            .GroupBy(p => p.PhotoTypeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return allTypes.OrderBy(t => t.DisplayOrder).Select(type =>
        {
            photosByType.TryGetValue(type.Id, out var typePhotos);
            var count = typePhotos?.Count(p => p.IsActive && !p.MarkedForDeletion) ?? 0;
            var representative = typePhotos?.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id).FirstOrDefault();

            return new AssetPhotoTypeWithStatusDto
            {
                PhotoTypeId = type.Id,
                PhotoTypeCode = type.PhotoTypeCode,
                PhotoTypeName = type.PhotoTypeName,
                DisplayOrder = type.DisplayOrder,
                HasPhoto = count > 0,
                PhotoCount = count,
                PhotoId = representative?.Id,
                Remarks = representative?.Remarks,
                DocumentBindingId = representative?.DocumentBindingId,
                DocumentGuid = representative != null ? GetSafeDocumentGuid(representative.DocumentBinding) : null,
                FileName = representative != null ? GetSafeFileName(representative.DocumentBinding) : null,
                MimeType = representative != null ? GetSafeMimeType(representative.DocumentBinding) : null
            };
        }).ToList();
    }

    public async Task<AssetPhotoBulkSaveResponseDto> BulkSaveAllAsync(
        AssetPhotoBulkSaveDto bulkDto,
        int userId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(bulkDto.AssetId, nameof(bulkDto.AssetId));
        Guard.AgainstNegativeOrZero(userId, nameof(userId));

        _logger.LogInformation("Bulk saving {Count} photos for AssetId={AssetId}, User={UserId}",
            bulkDto.Photos.Count, bulkDto.AssetId, userId);

        var response = new AssetPhotoBulkSaveResponseDto
        {
            AssetId = bulkDto.AssetId,
            TotalProcessed = bulkDto.Photos.Count
        };

        var existingPhotos = await _photoService.GetLatestByAssetIdIncludingInactiveAsync(
            bulkDto.AssetId,
            cancellationToken);

        var existingLookup = existingPhotos
            .GroupBy(p => p.PhotoTypeId)
            .ToDictionary(g => g.Key, g => g.First());

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var photoDto in bulkDto.Photos)
            {
                try
                {
                    var exists = existingLookup.TryGetValue(photoDto.PhotoTypeId, out var existingPhoto);

                    if (photoDto.IsEnabled)
                    {
                        if (!exists)
                        {
                            var newPhotoId = await _photoService.CreateAsync(
                                bulkDto.AssetId,
                                photoDto.PhotoTypeId,
                                photoDto.DisplayOrder,
                                photoDto.Remarks,
                                userId,
                                cancellationToken);

                            await _photoService.ToggleEnabledAsync(
                                newPhotoId,
                                true,
                                userId,
                                cancellationToken);

                            response.EnabledCount++;
                            _logger.LogDebug("Created and enabled new asset photo: TypeId={TypeId}, PhotoId={PhotoId}",
                                photoDto.PhotoTypeId, newPhotoId);
                        }
                        else
                        {
                            await _photoService.UpdateAsync(
                                existingPhoto!.Id,
                                photoDto.DisplayOrder,
                                photoDto.Remarks,
                                userId,
                                cancellationToken);

                            if (!existingPhoto.IsActive)
                            {
                                await _photoService.ToggleEnabledAsync(
                                    existingPhoto.Id,
                                    true,
                                    userId,
                                    cancellationToken);
                            }

                            response.EnabledCount++;
                            _logger.LogDebug("Updated and enabled existing asset photo: PhotoId={PhotoId}",
                                existingPhoto.Id);
                        }
                    }
                    else
                    {
                        if (exists && existingPhoto != null && existingPhoto.IsActive)
                        {
                            await _photoService.ToggleEnabledAsync(
                                existingPhoto.Id,
                                false,
                                userId,
                                cancellationToken);

                            response.DisabledCount++;
                            _logger.LogDebug("Disabled asset photo: PhotoId={PhotoId}", existingPhoto.Id);
                        }
                        else
                        {
                            response.DisabledCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process bulk asset photo item: TypeId={TypeId}",
                        photoDto.PhotoTypeId);
                    response.Errors.Add($"Photo Type {photoDto.PhotoTypeId}: {ex.Message}");
                }
            }

            response.UpdatedPhotoTypes = await GetPhotoTypesWithStatusAsync(
                bulkDto.AssetId,
                cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

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

    private static AssetPhotoDto MapToDto(AssetPhotoEntity p) => new()
    {
        PhotoId = p.Id,
        AssetId = p.AssetId,
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

    private static Guid? GetSafeDocumentGuid(DocumentBindingEntity? documentBinding)
    {
        var document = documentBinding?.Document;
        if (document == null || !document.IsActive || document.MarkedForDeletion)
            return null;

        return document.DocumentGuid;
    }

    private static string? GetSafeFileName(DocumentBindingEntity? documentBinding)
    {
        var document = documentBinding?.Document;
        if (document == null || !document.IsActive || document.MarkedForDeletion)
            return null;

        return document.OriginalFileName;
    }

    private static string? GetSafeMimeType(DocumentBindingEntity? documentBinding)
    {
        var document = documentBinding?.Document;
        if (document == null || !document.IsActive || document.MarkedForDeletion)
            return null;

        return document.MimeType;
    }
}
