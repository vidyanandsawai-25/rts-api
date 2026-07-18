using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class AssetTypeService
    : BaseCommonCrudService<
        AssetTypeEntity,
        AssetTypeDto,
        CreateAssetTypeDto,
        UpdateAssetTypeDto,
        AssetTypeQueryParameters,
        int>,
      IAssetTypeService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetTypeService(
        IRepository<AssetTypeEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        AssetTypeEntity currentEntity,
        AssetTypeEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssetTypeEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        AssetTypeEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<AssetTypeEntity>(id, cancellationToken);
    }
}