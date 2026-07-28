using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

public class InventoryDocumentTypeService :
    BaseCommonCrudService<InventoryDocumentTypeEntity, InventoryDocumentTypeDto, CreateInventoryDocumentTypeDto, UpdateInventoryDocumentTypeDto, InventoryDocumentTypeQueryParameters, int>,
    IInventoryDocumentTypeService
{
    private readonly IReferenceValidationService _referenceValidator;

    public InventoryDocumentTypeService(
        IRepository<InventoryDocumentTypeEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        InventoryDocumentTypeEntity entity, CancellationToken ct = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.DocumentTypeCode == entity.DocumentTypeCode && !x.MarkedForDeletion, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.DocumentTypeCode), "InventoryDocumentType_DocumentTypeCode_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        InventoryDocumentTypeEntity currentEntity,
        InventoryDocumentTypeEntity updatedEntity,
        CancellationToken ct = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            var refResult = await _referenceValidator.ValidateReferencesAsync<InventoryDocumentTypeEntity>(id, ct);
            if (!refResult.IsValid) return refResult;
        }

        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.Id != id && x.DocumentTypeCode == updatedEntity.DocumentTypeCode && !x.MarkedForDeletion, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(updatedEntity.DocumentTypeCode), "InventoryDocumentType_DocumentTypeCode_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        InventoryDocumentTypeEntity entity,
        CancellationToken ct = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<InventoryDocumentTypeEntity>(id, ct);
    }
}
