using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

public class AssetPhotoTypeService :
    BaseCommonCrudService<AssetPhotoTypeEntity, AssetPhotoTypeDto, CreateAssetPhotoTypeDto, UpdateAssetPhotoTypeDto, AssetPhotoTypeQueryParameters, int>,
    IAssetPhotoTypeService
{
    private readonly IRepository<AssetCategoryEntity, int> _categoryRepository;
    private readonly IRepository<AssetTypeEntity, int> _typeRepository;
    private readonly IReferenceValidationService _referenceValidator;

    public AssetPhotoTypeService(
        IRepository<AssetPhotoTypeEntity, int> repository,
        IRepository<AssetCategoryEntity, int> categoryRepository,
        IRepository<AssetTypeEntity, int> typeRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _categoryRepository = categoryRepository;
        _typeRepository = typeRepository;
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetPhotoTypeEntity entity, CancellationToken ct = default)
    {
        if (entity.AssetCategoryId.HasValue && entity.AssetCategoryId.Value > 0)
        {
            var categoryExists = await _categoryRepository.GetQueryable()
                .AsNoTracking()
                .AnyAsync(c => c.Id == entity.AssetCategoryId.Value && !c.MarkedForDeletion, ct);

            if (!categoryExists)
                return ValidationResult.Failure(nameof(entity.AssetCategoryId), "AssetPhotoType_AssetCategoryId_Invalid");
        }

        if (entity.AssetTypeId.HasValue && entity.AssetTypeId.Value > 0)
        {
            var typeExists = await _typeRepository.GetQueryable()
                .AsNoTracking()
                .AnyAsync(t => t.Id == entity.AssetTypeId.Value && !t.MarkedForDeletion, ct);

            if (!typeExists)
                return ValidationResult.Failure(nameof(entity.AssetTypeId), "AssetPhotoType_AssetTypeId_Invalid");
        }

        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.PhotoTypeCode == entity.PhotoTypeCode, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.PhotoTypeCode), "AssetPhotoType_PhotoTypeCode_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id, AssetPhotoTypeEntity currentEntity, AssetPhotoTypeEntity updatedEntity, CancellationToken ct = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            var refResult = await _referenceValidator.ValidateReferencesAsync<AssetPhotoTypeEntity>(id, ct);
            if (!refResult.IsValid) return refResult;
        }

        if (updatedEntity.AssetCategoryId.HasValue && updatedEntity.AssetCategoryId.Value > 0)
        {
            var categoryExists = await _categoryRepository.GetQueryable()
                .AsNoTracking()
                .AnyAsync(c => c.Id == updatedEntity.AssetCategoryId.Value && !c.MarkedForDeletion, ct);

            if (!categoryExists)
                return ValidationResult.Failure(nameof(updatedEntity.AssetCategoryId), "AssetPhotoType_AssetCategoryId_Invalid");
        }

        if (updatedEntity.AssetTypeId.HasValue && updatedEntity.AssetTypeId.Value > 0)
        {
            var typeExists = await _typeRepository.GetQueryable()
                .AsNoTracking()
                .AnyAsync(t => t.Id == updatedEntity.AssetTypeId.Value && !t.MarkedForDeletion, ct);

            if (!typeExists)
                return ValidationResult.Failure(nameof(updatedEntity.AssetTypeId), "AssetPhotoType_AssetTypeId_Invalid");
        }

        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.Id != id && x.PhotoTypeCode == updatedEntity.PhotoTypeCode, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(updatedEntity.PhotoTypeCode), "AssetPhotoType_PhotoTypeCode_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, AssetPhotoTypeEntity entity, CancellationToken ct = default)
        => await _referenceValidator.ValidateReferencesAsync<AssetPhotoTypeEntity>(id, ct);
}
