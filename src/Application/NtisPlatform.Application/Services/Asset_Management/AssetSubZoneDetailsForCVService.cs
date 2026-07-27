using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

public class AssetSubZoneDetailsForCVService :
    BaseCommonCrudService<AssetSubZoneDetailsForCVEntity, AssetSubZoneDetailsForCVDto, CreateAssetSubZoneDetailsForCVDto, UpdateAssetSubZoneDetailsForCVDto, AssetSubZoneDetailsForCVQueryParameters, int>,
    IAssetSubZoneDetailsForCVService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetSubZoneDetailsForCVService(
        IRepository<AssetSubZoneDetailsForCVEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetSubZoneDetailsForCVEntity entity, CancellationToken ct = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.MoujaId == entity.MoujaId && x.SubZoneNo == entity.SubZoneNo && !x.MarkedForDeletion, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.SubZoneNo), "AssetSubZoneDetailsForCV_SubZoneNo_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id, AssetSubZoneDetailsForCVEntity currentEntity, AssetSubZoneDetailsForCVEntity updatedEntity, CancellationToken ct = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<AssetSubZoneDetailsForCVEntity>(id, ct);

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, AssetSubZoneDetailsForCVEntity entity, CancellationToken ct = default)
        => await _referenceValidator.ValidateReferencesAsync<AssetSubZoneDetailsForCVEntity>(id, ct);
}
