using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>CRUD service for the penalty rule master.</summary>
public class PenaltyRuleService :
    BaseCommonCrudService<PenaltyRuleMasterEntity, PenaltyRuleDto, CreatePenaltyRuleDto, UpdatePenaltyRuleDto, PenaltyRuleQueryParameters, int>,
    IPenaltyRuleService
{
    private readonly IReferenceValidationService _referenceValidator;

    public PenaltyRuleService(
        IRepository<PenaltyRuleMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        PenaltyRuleMasterEntity entity, CancellationToken cancellationToken = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.PenaltyCode == entity.PenaltyCode, cancellationToken);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.PenaltyCode), "Penaltyrule_Code_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id, PenaltyRuleMasterEntity currentEntity, PenaltyRuleMasterEntity updatedEntity, CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
            return await _referenceValidator.ValidateReferencesAsync<PenaltyRuleMasterEntity>(id, cancellationToken);
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id, PenaltyRuleMasterEntity entity, CancellationToken cancellationToken = default)
        => await _referenceValidator.ValidateReferencesAsync<PenaltyRuleMasterEntity>(id, cancellationToken);
}
