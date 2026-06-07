using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class AssetAuthorityMasterService
    : BaseCommonCrudService<
        AssetAuthorityMasterEntity,
        AssetAuthorityMasterDto,
        CreateAssetAuthorityMasterDto,
        UpdateAssetAuthorityMasterDto,
        AssetAuthorityMasterQueryParameters,
        int>,
      IAssetAuthorityMasterService
{
    private readonly IReferenceValidationService _referenceValidator;

    public AssetAuthorityMasterService(
        IRepository<AssetAuthorityMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        AssetAuthorityMasterEntity entity,
        CancellationToken cancellationToken = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.AuthorityCode == entity.AuthorityCode, cancellationToken);

        if (duplicate)
        {
            return ValidationResult.Failure(nameof(entity.AuthorityCode), "AssetAuthorityMaster_AuthorityCode_Duplicate");
        }

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        AssetAuthorityMasterEntity currentEntity,
        AssetAuthorityMasterEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssetAuthorityMasterEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        AssetAuthorityMasterEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<AssetAuthorityMasterEntity>(id, cancellationToken);
    }
}
