using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class WaterConnectionStatusService
    : BaseCommonCrudService<WaterConnectionStatusEntity, WaterConnectionStatusDto, CreateWaterConnectionStatusDto, UpdateWaterConnectionStatusDto, WaterConnectionStatusQueryParameters, int>,
      IWaterConnectionStatusService
{
    private readonly IReferenceValidationService _referenceValidator;

    public WaterConnectionStatusService(
        IRepository<WaterConnectionStatusEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        WaterConnectionStatusEntity currentEntity,
        WaterConnectionStatusEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<WaterConnectionStatusEntity>(id, cancellationToken);
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        WaterConnectionStatusEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<WaterConnectionStatusEntity>(id, cancellationToken);
    }
}
