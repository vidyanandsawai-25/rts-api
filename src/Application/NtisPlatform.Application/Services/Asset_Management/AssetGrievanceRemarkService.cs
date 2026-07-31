using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

public class AssetGrievanceRemarkService :
    BaseCommonCrudService<AssetGrievanceRemarkMasterEntity, AssetGrievanceRemarkDto, CreateAssetGrievanceRemarkDto, UpdateAssetGrievanceRemarkDto, AssetGrievanceRemarkQueryParameters, int>,
    IAssetGrievanceRemarkService
{
    private readonly IRepository<AssetGrievanceCategoryEntity, int> _categoryRepository;
    private readonly IReferenceValidationService _referenceValidator;

    public AssetGrievanceRemarkService(
        IRepository<AssetGrievanceRemarkMasterEntity, int> repository,
        IRepository<AssetGrievanceCategoryEntity, int> categoryRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _categoryRepository = categoryRepository;
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetGrievanceRemarkMasterEntity entity, CancellationToken ct = default)
    {
        var categoryExists = await _categoryRepository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(c => c.Id == entity.GrievanceCategoryId && !c.MarkedForDeletion && c.IsActive, ct);

        if (!categoryExists)
            return ValidationResult.Failure(nameof(entity.GrievanceCategoryId), "AssetGrievanceRemark_GrievanceCategoryId_Invalid");

        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.GrievanceCategoryId == entity.GrievanceCategoryId && x.Remark == entity.Remark && !x.MarkedForDeletion, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.Remark), "AssetGrievanceRemark_Remark_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id, AssetGrievanceRemarkMasterEntity currentEntity, AssetGrievanceRemarkMasterEntity updatedEntity, CancellationToken ct = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            var refResult = await _referenceValidator.ValidateReferencesAsync<AssetGrievanceRemarkMasterEntity>(id, ct);
            if (!refResult.IsValid) return refResult;
        }

        var categoryExists = await _categoryRepository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(c => c.Id == updatedEntity.GrievanceCategoryId && !c.MarkedForDeletion && c.IsActive, ct);

        if (!categoryExists)
            return ValidationResult.Failure(nameof(updatedEntity.GrievanceCategoryId), "AssetGrievanceRemark_GrievanceCategoryId_Invalid");

        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.Id != id && x.GrievanceCategoryId == updatedEntity.GrievanceCategoryId && x.Remark == updatedEntity.Remark && !x.MarkedForDeletion, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(updatedEntity.Remark), "AssetGrievanceRemark_Remark_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, AssetGrievanceRemarkMasterEntity entity, CancellationToken ct = default)
        => await _referenceValidator.ValidateReferencesAsync<AssetGrievanceRemarkMasterEntity>(id, ct);
}
