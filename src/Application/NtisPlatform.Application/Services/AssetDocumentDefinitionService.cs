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

public class AssetDocumentDefinitionService
    : BaseCommonCrudService<
        AssetDocumentDefinitionEntity,
        AssetDocumentDefinitionDto,
        CreateAssetDocumentDefinitionDto,
        UpdateAssetDocumentDefinitionDto,
        AssetDocumentDefinitionQueryParameters,
        int>,
      IAssetDocumentDefinitionService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetDocumentDefinitionService(
        IRepository<AssetDocumentDefinitionEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetDocumentDefinitionEntity entity,
        CancellationToken cancellationToken = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.AssetCategoryId == entity.AssetCategoryId 
                           && x.AssetTypeId == entity.AssetTypeId 
                           && x.DocumentCode == entity.DocumentCode, cancellationToken);

        if (duplicate)
        {
            return ValidationResult.Failure(nameof(entity.DocumentCode), "AssetDocumentDefinition_DocumentCode_Duplicate");
        }

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        AssetDocumentDefinitionEntity currentEntity,
        AssetDocumentDefinitionEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssetDocumentDefinitionEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        AssetDocumentDefinitionEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<AssetDocumentDefinitionEntity>(id, cancellationToken);
    }
}
