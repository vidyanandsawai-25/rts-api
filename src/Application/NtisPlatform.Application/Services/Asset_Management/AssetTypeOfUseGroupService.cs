using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

public class AssetTypeOfUseGroupService :
    BaseCommonCrudService<AssetTypeOfUseGroupEntity, AssetTypeOfUseGroupDto, CreateAssetTypeOfUseGroupDto, UpdateAssetTypeOfUseGroupDto, AssetTypeOfUseGroupQueryParameters, int>,
    IAssetTypeOfUseGroupService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetTypeOfUseGroupService(
        IRepository<AssetTypeOfUseGroupEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetTypeOfUseGroupEntity entity, CancellationToken ct = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.TypeOfUseGroupCode == entity.TypeOfUseGroupCode && !x.MarkedForDeletion, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.TypeOfUseGroupCode), "AssetTypeOfUseGroup_GroupCode_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id, AssetTypeOfUseGroupEntity currentEntity, AssetTypeOfUseGroupEntity updatedEntity, CancellationToken ct = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<AssetTypeOfUseGroupEntity>(id, ct);

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, AssetTypeOfUseGroupEntity entity, CancellationToken ct = default)
        => await _referenceValidator.ValidateReferencesAsync<AssetTypeOfUseGroupEntity>(id, ct);
}
