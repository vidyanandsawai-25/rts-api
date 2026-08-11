using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class OwnershipTypeService : BaseCommonCrudService<
    OwnershipTypeEntity,
    OwnershipTypeDto,
    CreateOwnershipTypeDto,
    UpdateOwnershipTypeDto,
    OwnershipTypeQueryParameters,
    int>, IOwnershipTypeService
{
    private readonly IReferenceValidationService _referenceValidator;

    public OwnershipTypeService(
        IRepository<OwnershipTypeEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        OwnershipTypeEntity entity, CancellationToken cancellationToken = default)
    {
        return await CheckDuplicateAsync(entity, excludeId: null, cancellationToken);
    }

    // Note: mirrors ValidateForCreateAsync's duplicate check because the base service only invokes
    // this hook (not ValidateForCreateAsync) on Update/BulkUpdate — the duplicate check excludes the
    // row being updated so renaming a type to its own current name is not flagged.
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        OwnershipTypeEntity currentEntity,
        OwnershipTypeEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        var duplicateResult = await CheckDuplicateAsync(updatedEntity, excludeId: id, cancellationToken);
        if (!duplicateResult.IsValid)
            return duplicateResult;

        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<OwnershipTypeEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, OwnershipTypeEntity entity, CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<OwnershipTypeEntity>(id, cancellationToken);
    }

    private async Task<ValidationResult> CheckDuplicateAsync(
        OwnershipTypeEntity entity, int? excludeId, CancellationToken cancellationToken)
    {
        var duplicate = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id != (excludeId ?? 0)
                        && x.OwnershipTypeName == entity.OwnershipTypeName
                        && !x.MarkedForDeletion, cancellationToken);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.OwnershipTypeName), "OwnershipType_OwnershipTypeName_Duplicate")
            : ValidationResult.Success();
    }
}
