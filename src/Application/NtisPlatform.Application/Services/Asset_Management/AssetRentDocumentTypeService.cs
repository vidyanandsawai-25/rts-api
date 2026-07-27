using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

public class AssetRentDocumentTypeService :
    BaseCommonCrudService<AssetRentDocumentTypeEntity, AssetRentDocumentTypeDto, CreateAssetRentDocumentTypeDto, UpdateAssetRentDocumentTypeDto, AssetRentDocumentTypeQueryParameters, int>,
    IAssetRentDocumentTypeService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetRentDocumentTypeService(
        IRepository<AssetRentDocumentTypeEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetRentDocumentTypeEntity entity, CancellationToken ct = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.DocumentTypeCode == entity.DocumentTypeCode && !x.MarkedForDeletion, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.DocumentTypeCode), "AssetRentDocumentType_DocumentTypeCode_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        AssetRentDocumentTypeEntity currentEntity,
        AssetRentDocumentTypeEntity updatedEntity,
        CancellationToken ct = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            var refResult = await _referenceValidator.ValidateReferencesAsync<AssetRentDocumentTypeEntity>(id, ct);
            if (!refResult.IsValid) return refResult;
        }

        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.Id != id && x.DocumentTypeCode == updatedEntity.DocumentTypeCode && !x.MarkedForDeletion, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(updatedEntity.DocumentTypeCode), "AssetRentDocumentType_DocumentTypeCode_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        AssetRentDocumentTypeEntity entity,
        CancellationToken ct = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<AssetRentDocumentTypeEntity>(id, ct);
    }
}
