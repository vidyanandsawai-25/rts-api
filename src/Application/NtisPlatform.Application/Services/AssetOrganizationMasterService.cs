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

public class AssetOrganizationMasterService
    : BaseCommonCrudService<
        AssetOrganizationMasterEntity,
        AssetOrganizationMasterDto,
        CreateAssetOrganizationMasterDto,
        UpdateAssetOrganizationMasterDto,
        AssetOrganizationMasterQueryParameters,
        int>,
      IAssetOrganizationMasterService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetOrganizationMasterService(
        IRepository<AssetOrganizationMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetOrganizationMasterEntity entity,
        CancellationToken cancellationToken = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.OrganizationCode == entity.OrganizationCode, cancellationToken);

        if (duplicate)
        {
            return ValidationResult.Failure(nameof(entity.OrganizationCode), "AssetOrganizationMaster_OrganizationCode_Duplicate");
        }

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        AssetOrganizationMasterEntity currentEntity,
        AssetOrganizationMasterEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssetOrganizationMasterEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        AssetOrganizationMasterEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<AssetOrganizationMasterEntity>(id, cancellationToken);
    }
}
