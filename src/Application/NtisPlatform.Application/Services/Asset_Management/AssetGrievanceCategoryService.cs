using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

public class AssetGrievanceCategoryService :
    BaseCommonCrudService<AssetGrievanceCategoryEntity, AssetGrievanceCategoryDto, CreateAssetGrievanceCategoryDto, UpdateAssetGrievanceCategoryDto, AssetGrievanceCategoryQueryParameters, int>,
    IAssetGrievanceCategoryService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetGrievanceCategoryService(
        IRepository<AssetGrievanceCategoryEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetGrievanceCategoryEntity entity, CancellationToken ct = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.CategoryName == entity.CategoryName && !x.MarkedForDeletion, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.CategoryName), "AssetGrievanceCategory_CategoryName_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id, AssetGrievanceCategoryEntity currentEntity, AssetGrievanceCategoryEntity updatedEntity, CancellationToken ct = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            var refResult = await _referenceValidator.ValidateReferencesAsync<AssetGrievanceCategoryEntity>(id, ct);
            if (!refResult.IsValid) return refResult;
        }

        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.Id != id && x.CategoryName == updatedEntity.CategoryName && !x.MarkedForDeletion, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(updatedEntity.CategoryName), "AssetGrievanceCategory_CategoryName_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, AssetGrievanceCategoryEntity entity, CancellationToken ct = default)
        => await _referenceValidator.ValidateReferencesAsync<AssetGrievanceCategoryEntity>(id, ct);
}
