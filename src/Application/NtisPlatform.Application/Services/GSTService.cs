using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>CRUD service for the GST/tax rate master.</summary>
public class GSTService :
    BaseCommonCrudService<GSTMasterEntity, GSTDto, CreateGSTDto, UpdateGSTDto, GSTQueryParameters, int>,
    IGSTService
{
    private readonly IReferenceValidationService _referenceValidator;

    public GSTService(
        IRepository<GSTMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        GSTMasterEntity entity, CancellationToken cancellationToken = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.TaxCode == entity.TaxCode, cancellationToken);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.TaxCode), "GST_TaxCode_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id, GSTMasterEntity currentEntity, GSTMasterEntity updatedEntity, CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<GSTMasterEntity>(id, cancellationToken);
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, GSTMasterEntity entity, CancellationToken cancellationToken = default)
        => await _referenceValidator.ValidateReferencesAsync<GSTMasterEntity>(id, cancellationToken);
}
