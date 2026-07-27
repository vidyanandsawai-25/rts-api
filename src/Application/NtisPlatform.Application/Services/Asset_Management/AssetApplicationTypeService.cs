using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class AssetApplicationTypeService : BaseCommonCrudService<
    AssetApplicationTypeEntity,
    AssetApplicationTypeDto,
    CreateAssetApplicationTypeDto,
    UpdateAssetApplicationTypeDto,
    AssetApplicationTypeQueryParameters,
    int>, IAssetApplicationTypeService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetApplicationTypeService(
        IRepository<AssetApplicationTypeEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        AssetApplicationTypeEntity currentEntity,
        AssetApplicationTypeEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssetApplicationTypeEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        AssetApplicationTypeEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<AssetApplicationTypeEntity>(id, cancellationToken);
    }
}
