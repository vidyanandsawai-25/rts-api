using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class DynamicTaxRuleService
    : BaseCommonCrudService<
        DynamicTaxRuleEntity,
        DynamicTaxRuleDto,
        CreateDynamicTaxRuleDto,
        UpdateDynamicTaxRuleDto,
        DynamicTaxRuleQueryParameters,
        int>,
      IDynamicTaxRuleService
{
    private readonly IRepository<TaxMasterEntity, int> _taxMasterRepository;
    private readonly ITaxCalculationModeService _modeService;
    private readonly IReferenceValidationService _referenceValidator;

    public DynamicTaxRuleService(
        IRepository<DynamicTaxRuleEntity, int> repository,
        IRepository<TaxMasterEntity, int> taxMasterRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ITaxCalculationModeService modeService,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _taxMasterRepository = taxMasterRepository;
        _modeService = modeService;
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        DynamicTaxRuleEntity currentEntity,
        DynamicTaxRuleEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<DynamicTaxRuleEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        DynamicTaxRuleEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<DynamicTaxRuleEntity>(id, cancellationToken);
    }

    /// <summary>
    /// A rule's RuleType IS a calculation mode code, so it is validated against
    /// PTIS.TaxCalculationModeMaster rather than a hardcoded list. Previously the only guards were
    /// [Required] + [StringLength(20)], so any 20-character string persisted — and a tax linked to
    /// such a rule would land in a mode nothing in the code can render or compute.
    /// </summary>
    private async Task EnsureRuleTypeIsValidAsync(string? ruleType, OperationType operation, CancellationToken cancellationToken)
    {
        if (await _modeService.GetByCodeAsync(ruleType, cancellationToken) is not null) return;

        var available = await _modeService.GetActiveAsync(cancellationToken);
        throw new ValidationException(
            nameof(CreateDynamicTaxRuleDto.RuleType),
            $"'{ruleType}' is not a valid Rule Type. Must be one of: {string.Join(", ", available.Select(m => m.ModeCode))}.",
            operation);
    }

    public override async Task<DynamicTaxRuleDto> CreateAsync(
        CreateDynamicTaxRuleDto createDto,
        CancellationToken cancellationToken = default)
    {
        await EnsureRuleTypeIsValidAsync(createDto.RuleType, OperationType.Create, cancellationToken);
        return await base.CreateAsync(createDto, cancellationToken);
    }

    public override async Task<DynamicTaxRuleDto?> UpdateAsync(
        int id,
        UpdateDynamicTaxRuleDto updateDto,
        CancellationToken cancellationToken = default)
    {
        await EnsureRuleTypeIsValidAsync(updateDto.RuleType, OperationType.Update, cancellationToken);

        var existingEntity = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingEntity != null
            && !string.IsNullOrWhiteSpace(existingEntity.RuleType)
            && !string.IsNullOrWhiteSpace(updateDto.RuleType)
            && !string.Equals(existingEntity.RuleType.Trim(), updateDto.RuleType.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var newMode = updateDto.RuleType.Trim();

            var referencingTaxes = await _taxMasterRepository.GetQueryable()
                .AsNoTracking()
                .Where(t => t.RuleDefinitionId == id)
                .Include(t => t.CalculationModeMaster)
                .Select(t => new
                {
                    t.Id,
                    t.TaxCode,
                    t.TaxName,
                    ModeCode = t.CalculationModeMaster != null ? t.CalculationModeMaster.ModeCode : null
                })
                .ToListAsync(cancellationToken);

            var conflictingTaxes = referencingTaxes
                .Where(t => t.ModeCode == null || !string.Equals(t.ModeCode, newMode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (conflictingTaxes.Count > 0)
            {
                var firstConflict = conflictingTaxes.First();
                throw new ValidationException(
                    nameof(UpdateDynamicTaxRuleDto.RuleType),
                    $"Cannot change RuleType of rule '{existingEntity.DisplayName}' from '{existingEntity.RuleType}' to '{newMode}' because it is currently referenced by {conflictingTaxes.Count} tax(es) in another mode (e.g. TaxId={firstConflict.Id}, '{firstConflict.TaxName}'). Update or clear the rule reference on those taxes before changing the rule's mode.",
                    OperationType.Update);
            }
        }

        return await base.UpdateAsync(id, updateDto, cancellationToken);
    }
}
