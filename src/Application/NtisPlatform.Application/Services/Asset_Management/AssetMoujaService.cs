using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

public class AssetMoujaService :
    BaseCommonCrudService<AssetMoujaMasterEntity, AssetMoujaDto, CreateAssetMoujaDto, UpdateAssetMoujaDto, AssetMoujaQueryParameters, int>,
    IAssetMoujaService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetMoujaService(
        IRepository<AssetMoujaMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetMoujaMasterEntity entity, CancellationToken ct = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.MoujaNo == entity.MoujaNo && !x.MarkedForDeletion, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.MoujaNo), "AssetMouja_MoujaNo_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id, AssetMoujaMasterEntity currentEntity, AssetMoujaMasterEntity updatedEntity, CancellationToken ct = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<AssetMoujaMasterEntity>(id, ct);

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, AssetMoujaMasterEntity entity, CancellationToken ct = default)
        => await _referenceValidator.ValidateReferencesAsync<AssetMoujaMasterEntity>(id, ct);
}
