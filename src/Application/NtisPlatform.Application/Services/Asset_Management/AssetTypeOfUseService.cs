using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

public class AssetTypeOfUseService :
    BaseCommonCrudService<AssetTypeOfUseMasterEntity, AssetTypeOfUseDto, CreateAssetTypeOfUseDto, UpdateAssetTypeOfUseDto, AssetTypeOfUseQueryParameters, int>,
    IAssetTypeOfUseService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetTypeOfUseService(
        IRepository<AssetTypeOfUseMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetTypeOfUseMasterEntity entity, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(entity.TypeOfUseCode))
        {
            var duplicate = await _repository.GetQueryable()
                .AsNoTracking()
                .AnyAsync(x => x.TypeOfUseCode == entity.TypeOfUseCode && !x.MarkedForDeletion, ct);

            if (duplicate)
                return ValidationResult.Failure(nameof(entity.TypeOfUseCode), "AssetTypeOfUse_TypeOfUseCode_Duplicate");
        }

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id, AssetTypeOfUseMasterEntity currentEntity, AssetTypeOfUseMasterEntity updatedEntity, CancellationToken ct = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<AssetTypeOfUseMasterEntity>(id, ct);

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, AssetTypeOfUseMasterEntity entity, CancellationToken ct = default)
        => await _referenceValidator.ValidateReferencesAsync<AssetTypeOfUseMasterEntity>(id, ct);
}
