using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

public class AssetSubTypeOfUseService :
    BaseCommonCrudService<AssetSubTypeOfUseEntity, AssetSubTypeOfUseDto, CreateAssetSubTypeOfUseDto, UpdateAssetSubTypeOfUseDto, AssetSubTypeOfUseQueryParameters, int>,
    IAssetSubTypeOfUseService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetSubTypeOfUseService(
        IRepository<AssetSubTypeOfUseEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetSubTypeOfUseEntity entity, CancellationToken ct = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.TypeOfUseId == entity.TypeOfUseId && x.Description == entity.Description && !x.MarkedForDeletion, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.Description), "AssetSubTypeOfUse_Description_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id, AssetSubTypeOfUseEntity currentEntity, AssetSubTypeOfUseEntity updatedEntity, CancellationToken ct = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<AssetSubTypeOfUseEntity>(id, ct);

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, AssetSubTypeOfUseEntity entity, CancellationToken ct = default)
        => await _referenceValidator.ValidateReferencesAsync<AssetSubTypeOfUseEntity>(id, ct);
}
