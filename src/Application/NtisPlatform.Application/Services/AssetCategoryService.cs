using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class AssetCategoryService
    : BaseCommonCrudService<
        AssetCategoryEntity,
        AssetCategoryDto,
        CreateAssetCategoryDto,
        UpdateAssetCategoryDto,
        AssetCategoryQueryParameters,
        int>,
      IAssetCategoryService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetCategoryService(
        IRepository<AssetCategoryEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetCategoryEntity entity,
        CancellationToken cancellationToken = default)
    {
        var duplicateCode = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.CategoryCode == entity.CategoryCode, cancellationToken);

        if (duplicateCode)
        {
            return ValidationResult.Failure(nameof(entity.CategoryCode), "AssetCategory_CategoryCode_Duplicate");
        }

        var duplicateName = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.CategoryName == entity.CategoryName, cancellationToken);

        if (duplicateName)
        {
            return ValidationResult.Failure(nameof(entity.CategoryName), "AssetCategory_CategoryName_Duplicate");
        }

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        AssetCategoryEntity currentEntity,
        AssetCategoryEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        // Prevent deactivating a category that has active asset types
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssetCategoryEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        AssetCategoryEntity entity,
        CancellationToken cancellationToken = default)
    {
        // Prevent deleting a category that has asset types
        return await _referenceValidator.ValidateReferencesAsync<AssetCategoryEntity>(id, cancellationToken);
    }
}
