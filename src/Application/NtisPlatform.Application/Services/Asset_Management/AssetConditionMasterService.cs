using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class AssetConditionMasterService
    : BaseCommonCrudService<
        AssetConditionMasterEntity,
        AssetConditionMasterDto,
        CreateAssetConditionMasterDto,
        UpdateAssetConditionMasterDto,
        AssetConditionMasterQueryParameters,
        int>,
      IAssetConditionMasterService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetConditionMasterService(
        IRepository<AssetConditionMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetConditionMasterEntity entity,
        CancellationToken cancellationToken = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x =>
                x.ConditionCategory == entity.ConditionCategory &&
                x.CategoryId == entity.CategoryId &&
                x.ConditionName == entity.ConditionName,
                cancellationToken);

        if (duplicate)
        {
            return ValidationResult.Failure(nameof(entity.ConditionName), "AssetConditionMaster_ConditionName_Duplicate");
        }

        return ValidationResult.Success();
    }

    // Note: the base service only invokes this hook (not ValidateForCreateAsync) on Update/BulkUpdate,
    // so the duplicate-combination check is duplicated here — matching AssetAgeFactorCVService's
    // "validate on any update" convention — with the duplicate check excluding the record being updated.
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        AssetConditionMasterEntity currentEntity,
        AssetConditionMasterEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id != id &&
                x.ConditionCategory == updatedEntity.ConditionCategory &&
                x.CategoryId == updatedEntity.CategoryId &&
                x.ConditionName == updatedEntity.ConditionName,
                cancellationToken);

        if (duplicate)
        {
            return ValidationResult.Failure(nameof(updatedEntity.ConditionName), "AssetConditionMaster_ConditionName_Duplicate");
        }

        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssetConditionMasterEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        AssetConditionMasterEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<AssetConditionMasterEntity>(id, cancellationToken);
    }
}
