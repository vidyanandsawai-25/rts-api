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

public class WaterRateMasterService
    : BaseCommonCrudService<WaterRateMasterEntity, WaterRateMasterDto, CreateWaterRateMasterDto, UpdateWaterRateMasterDto, WaterRateMasterQueryParameters, int>,
      IWaterRateMasterService
{
    private readonly IReferenceValidationService _referenceValidator;

    public WaterRateMasterService(
        IRepository<WaterRateMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override IQueryable<WaterRateMasterEntity> ApplyIncludes(IQueryable<WaterRateMasterEntity> query)
    {
        return query
            .Include(x => x.WaterConnectionType)
            .Include(x => x.WaterConnectionSize)
            .Include(x => x.FinanceYear);
    }

    public override async Task<WaterRateMasterDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable()
            .Include(x => x.WaterConnectionType)
            .Include(x => x.WaterConnectionSize)
            .Include(x => x.FinanceYear)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
            return default;

        var dto = _mapper.Map<WaterRateMasterDto>(entity);

        return dto;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        WaterRateMasterEntity currentEntity,
        WaterRateMasterEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<WaterRateMasterEntity>(id, cancellationToken);
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        WaterRateMasterEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<WaterRateMasterEntity>(id, cancellationToken);
    }
}
