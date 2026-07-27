using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master.RTSFieldDefinition;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSFieldDefinitionService:BaseCommonCrudService<RTSFieldDefinitionEntity, RTSFieldDefinitionDto, CreateRTSFieldDefinitionDto, UpdateRTSFieldDefinitionDto, RTSFieldDefinitionQueryParameters, int>, IRTSFieldDefinitionService
{
    private readonly IReferenceValidationService _referenceValidator;

    public RTSFieldDefinitionService(
        IRepository<RTSFieldDefinitionEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator): base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        RTSFieldDefinitionEntity entity,
        CancellationToken cancellationToken = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.DepartmentId == entity.DepartmentId
                           && x.ServiceId == entity.ServiceId
                           && x.FieldCode == entity.FieldCode, cancellationToken);

        if (duplicate)
        {
            return ValidationResult.Failure(nameof(entity.FieldCode), "RTSFieldDefinition_FieldCode_Duplicate");
        }

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        RTSFieldDefinitionEntity currentEntity,
        RTSFieldDefinitionEntity updatedEntity,
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
        RTSFieldDefinitionEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<AssetFieldDefinitionEntity>(id, cancellationToken);
    }
}
