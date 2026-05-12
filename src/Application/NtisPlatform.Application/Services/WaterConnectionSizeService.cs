using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class WaterConnectionSizeService
    : BaseCommonCrudService<WaterConnectionSizeEntity, WaterConnectionSizeDto, CreateWaterConnectionSizeDto, UpdateWaterConnectionSizeDto, WaterConnectionSizeQueryParameters, int>,
      IWaterConnectionSizeService
{
    private readonly IReferenceValidationService _referenceValidator;

    public WaterConnectionSizeService(
        IRepository<WaterConnectionSizeEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    /// <summary>
    /// Override to force in-memory mapping instead of ProjectTo.
    /// The DTO uses ToString("G29") for DisplayLabel which EF Core cannot translate to SQL.
    /// </summary>
    protected override IQueryable<WaterConnectionSizeEntity> ApplyIncludes(IQueryable<WaterConnectionSizeEntity> query)
    {
        return query.AsNoTracking();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        WaterConnectionSizeEntity currentEntity,
        WaterConnectionSizeEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<WaterConnectionSizeEntity>(id, cancellationToken);
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        WaterConnectionSizeEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<WaterConnectionSizeEntity>(id, cancellationToken);
    }
}
