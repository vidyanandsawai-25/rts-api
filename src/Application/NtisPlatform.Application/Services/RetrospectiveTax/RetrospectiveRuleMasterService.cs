using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleMaster;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using OperationType = NtisPlatform.Application.Enums.OperationType;

namespace NtisPlatform.Application.Services.RetrospectiveTax;

public class RetrospectiveRuleMasterService : BaseCommonCrudService<RetrospectiveRuleMasterEntity, RetrospectiveRuleMasterDto, CreateRetrospectiveRuleMasterDto, UpdateRetrospectiveRuleMasterDto, RetrospectiveRuleMasterQueryParameters, int>, IRetrospectiveRuleMasterService
{
    private readonly IReferenceValidationService _referenceValidator;
    private readonly IRepository<RetrospectiveRuleAuditLogEntity, long> _auditLogRepository;
    private readonly IRetrospectiveRuleEvidenceConditionService _evidenceConditionService;
    private readonly IRepository<RetrospectiveRuleDateConditionEntity, int> _dateConditionRepository;
    private readonly IRepository<RetrospectiveRuleActionEntity, int> _actionRepository;
    private readonly IRepository<RetrospectivePenaltyRuleEntity, int> _penaltyRepository;
    private readonly IRetrospectiveRuleSummaryService _ruleSummaryService;

    public RetrospectiveRuleMasterService(
        IRepository<RetrospectiveRuleMasterEntity, int> repository,
        IRepository<RetrospectiveRuleAuditLogEntity, long> auditLogRepository,
        IRetrospectiveRuleEvidenceConditionService evidenceConditionService,
        IRepository<RetrospectiveRuleDateConditionEntity, int> dateConditionRepository,
        IRepository<RetrospectiveRuleActionEntity, int> actionRepository,
        IRepository<RetrospectivePenaltyRuleEntity, int> penaltyRepository,
        IRetrospectiveRuleSummaryService ruleSummaryService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
        _auditLogRepository = auditLogRepository;
        _evidenceConditionService = evidenceConditionService;
        _dateConditionRepository = dateConditionRepository;
        _actionRepository = actionRepository;
        _penaltyRepository = penaltyRepository;
        _ruleSummaryService = ruleSummaryService;
    }

    public async Task<RetrospectiveRuleDetailDto?> GetDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var rule = await _repository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
            return null;

        var evidenceConditions = await _evidenceConditionService.GetEvidenceStateForRuleAsync(id, cancellationToken);

        var dateConditionEntity = await _dateConditionRepository.GetQueryable()
            .FirstOrDefaultAsync(c => c.RuleId == id && c.IsActive, cancellationToken);

        var actionEntity = await _actionRepository.GetQueryable()
            .FirstOrDefaultAsync(a => a.RuleId == id && a.IsActive, cancellationToken);

        var penaltyEntity = await _penaltyRepository.GetQueryable()
            .FirstOrDefaultAsync(p => p.RuleId == id && p.IsActive, cancellationToken);

        var summary = await _ruleSummaryService.GetForRuleAsync(id, cancellationToken);

        return new RetrospectiveRuleDetailDto
        {
            Rule = _mapper.Map<RetrospectiveRuleMasterDto>(rule),
            EvidenceConditions = evidenceConditions,
            DateCondition = dateConditionEntity is null ? null : _mapper.Map<DTOs.RetrospectiveTax.RetrospectiveRuleDateCondition.RetrospectiveRuleDateConditionDto>(dateConditionEntity),
            Action = actionEntity is null ? null : _mapper.Map<DTOs.RetrospectiveTax.RetrospectiveRuleAction.RetrospectiveRuleActionDto>(actionEntity),
            PenaltyRule = penaltyEntity is null ? null : _mapper.Map<DTOs.RetrospectiveTax.RetrospectivePenaltyRule.RetrospectivePenaltyRuleDto>(penaltyEntity),
            Summary = summary
        };
    }

    public async Task<RetrospectiveRuleMasterDto?> PublishAsync(int id, PublishRetrospectiveRuleDto request, CancellationToken cancellationToken = default)
    {
        var rule = await _repository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
            return null;

        if (rule.RuleStatus == "Active")
        {
            throw new ValidationException("RuleStatus", "This rule is already published (Active).", OperationType.Update);
        }

        var previousStatus = rule.RuleStatus;
        rule.RuleStatus = "Active";
        rule.UpdatedBy = request.PublishedBy;
        rule.UpdatedDate = DateTime.Now;
        await _repository.UpdateAsync(rule, cancellationToken);

        await _auditLogRepository.AddAsync(new RetrospectiveRuleAuditLogEntity
        {
            RuleId = id,
            ActionType = "PUBLISH",
            OldValue = previousStatus,
            NewValue = "Active",
            Remarks = request.Remarks,
            CreatedBy = request.PublishedBy,
            CreatedDate = DateTime.Now
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<RetrospectiveRuleMasterDto>(rule);
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        RetrospectiveRuleMasterEntity currentEntity,
        RetrospectiveRuleMasterEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<RetrospectiveRuleMasterEntity>(id, cancellationToken);
        }

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        RetrospectiveRuleMasterEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<RetrospectiveRuleMasterEntity>(id, cancellationToken);
    }
}
