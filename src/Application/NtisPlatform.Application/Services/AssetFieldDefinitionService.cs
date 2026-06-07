using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class AssetFieldDefinitionService
    : BaseCommonCrudService<
        AssetFieldDefinitionEntity,
        AssetFieldDefinitionDto,
        CreateAssetFieldDefinitionDto,
        UpdateAssetFieldDefinitionDto,
        AssetFieldDefinitionQueryParameters,
        int>,
      IAssetFieldDefinitionService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetFieldDefinitionService(
        IRepository<AssetFieldDefinitionEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetFieldDefinitionEntity entity,
        CancellationToken cancellationToken = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.AssetCategoryId == entity.AssetCategoryId 
                           && x.AssetTypeId == entity.AssetTypeId 
                           && x.FieldCode == entity.FieldCode, cancellationToken);

        if (duplicate)
        {
            return ValidationResult.Failure(nameof(entity.FieldCode), "AssetFieldDefinition_FieldCode_Duplicate");
        }

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        AssetFieldDefinitionEntity currentEntity,
        AssetFieldDefinitionEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssetFieldDefinitionEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        AssetFieldDefinitionEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<AssetFieldDefinitionEntity>(id, cancellationToken);
    }
}
