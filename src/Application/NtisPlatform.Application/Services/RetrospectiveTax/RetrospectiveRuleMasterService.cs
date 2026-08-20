using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Constants.RetrospectiveTax;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleMaster;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition;
using NtisPlatform.Application.DTOs.Range;
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
    private readonly IRepository<RetrospectiveRuleAuditLogEntity, int> _auditLogRepository;
    private readonly IRetrospectiveRuleEvidenceConditionService _evidenceConditionService;
    private readonly IRepository<RetrospectiveRuleDateConditionEntity, int> _dateConditionRepository;
    private readonly IRepository<RetrospectiveRuleActionEntity, int> _actionRepository;
    private readonly IRepository<RetrospectivePenaltyRuleEntity, int> _penaltyRepository;
    private readonly IRetrospectiveRuleSummaryService _ruleSummaryService;

    public RetrospectiveRuleMasterService(
        IRepository<RetrospectiveRuleMasterEntity, int> repository,
        IRepository<RetrospectiveRuleAuditLogEntity, int> auditLogRepository,
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

    private static readonly string[] MatchTypes = { "CONDITION_BASED", "EXACT_EVIDENCE_MATCH", "PRIORITY_BASED" };
    private static readonly string[] AuthorizationStatuses = { "AUTHORIZED", "UNAUTHORIZED", "UNDETERMINED" };
    private static readonly string[] CompareOperators = { "BEFORE", "AFTER", "ON_OR_BEFORE", "ON_OR_AFTER", "BETWEEN", "OLDER_THAN_YEARS", "WITHIN_YEARS" };
    private static readonly string[] ElseActions = { "NONE", "MANUAL_REVIEW" };

    /// <summary>
    /// Validates every CHECK-constraint-backed field SaveAsync writes, before touching the
    /// database. Without this, an invalid value from the UI (typo, stale dropdown option, etc.)
    /// reaches SQL Server and surfaces as an unhandled DbUpdateException/SqlException (500)
    /// instead of a clean 400 naming the offending field.
    /// </summary>
    private static void ValidateEnums(SaveRetrospectiveRuleDto request)
    {
        ValidateEnum(nameof(request.MatchType), request.MatchType, MatchTypes, required: true);
        ValidateEnum(nameof(request.AuthorizationStatus), request.AuthorizationStatus, AuthorizationStatuses, required: false);

        if (request.DateCondition is not null)
        {
            ValidateEnum(
                $"{nameof(request.DateCondition)}.{nameof(request.DateCondition.ComparatorCode)}",
                request.DateCondition.ComparatorCode,
                RetrospectiveRuleDateConditionOptions.ComparatorCodes.Select(o => o.Code),
                required: true);
            ValidateEnum(
                $"{nameof(request.DateCondition)}.{nameof(request.DateCondition.CompareOperator)}",
                request.DateCondition.CompareOperator,
                CompareOperators,
                required: false);
        }

        ValidateEnum(
            $"{nameof(request.Action)}.{nameof(request.Action.TaxStartMode)}",
            request.Action.TaxStartMode,
            RetrospectiveRuleActionOptions.TaxStartModes.Select(o => o.Code),
            required: true);
        ValidateEnum(
            $"{nameof(request.Action)}.{nameof(request.Action.RetrospectiveLimitType)}",
            request.Action.RetrospectiveLimitType,
            RetrospectiveRuleActionOptions.RetrospectiveLimitTypes.Select(o => o.Code),
            required: true);
        ValidateEnum(
            $"{nameof(request.Action)}.{nameof(request.Action.TaxCalculationMode)}",
            request.Action.TaxCalculationMode,
            RetrospectiveRuleActionOptions.TaxCalculationModes.Select(o => o.Code),
            required: true);

        if (request.PenaltyRule is not null)
        {
            ValidateEnum(
                $"{nameof(request.PenaltyRule)}.{nameof(request.PenaltyRule.PenaltyMode)}",
                request.PenaltyRule.PenaltyMode,
                RetrospectivePenaltyRuleOptions.PenaltyModes.Select(o => o.Code),
                required: true);
            ValidateEnum(
                $"{nameof(request.PenaltyRule)}.{nameof(request.PenaltyRule.PenaltyDateSourceType)}",
                request.PenaltyRule.PenaltyDateSourceType,
                RetrospectivePenaltyRuleOptions.PenaltyDateSourceTypes.Select(o => o.Code),
                required: false);
            ValidateEnum(
                $"{nameof(request.PenaltyRule)}.{nameof(request.PenaltyRule.PenaltyDateCondition)}",
                request.PenaltyRule.PenaltyDateCondition,
                RetrospectivePenaltyRuleOptions.PenaltyDateConditions.Select(o => o.Code),
                required: false);
            ValidateEnum(
                $"{nameof(request.PenaltyRule)}.{nameof(request.PenaltyRule.ElseAction)}",
                request.PenaltyRule.ElseAction,
                ElseActions,
                required: false);
        }
    }

    private static void ValidateEnum(string fieldName, string? value, IEnumerable<string> allowedValues, bool required)
    {
        if (string.IsNullOrEmpty(value))
        {
            if (required)
            {
                throw new ValidationException(fieldName, $"{fieldName} is required.", OperationType.Update);
            }
            return;
        }

        if (!allowedValues.Contains(value))
        {
            throw new ValidationException(fieldName, $"'{value}' is not a valid value for {fieldName}.", OperationType.Update);
        }
    }

    public async Task<RetrospectiveRuleDetailDto?> SaveAsync(SaveRetrospectiveRuleDto request, CancellationToken cancellationToken = default)
    {
        ValidateEnums(request);

        RetrospectiveRuleMasterEntity rule;

        if (request.Id is int id && id > 0)
        {
            var existingRule = await _repository.GetByIdAsync(id, cancellationToken);
            if (existingRule is null)
                return null;

            rule = existingRule;
            rule.RuleCode = request.RuleCode;
            rule.RuleName = request.RuleName;
            rule.RuleDescription = request.RuleDescription;
            rule.PriorityNo = request.PriorityNo;
            rule.MatchType = request.MatchType;
            rule.IsFallbackRule = request.IsFallbackRule;
            rule.AuthorizationStatus = request.AuthorizationStatus;
            rule.LegalCapEnabled = request.LegalCapEnabled;
            rule.LegalCapYears = request.LegalCapYears;
            rule.NoticeDays = request.NoticeDays;
            rule.VersionNo = request.VersionNo;
            rule.ResolutionRef = request.ResolutionRef;
            rule.EffectiveFrom = request.EffectiveFrom;
            rule.EffectiveTo = request.EffectiveTo;
            rule.Remarks = request.Remarks;
            rule.UpdatedBy = request.UpdatedBy;
            rule.UpdatedDate = DateTime.Now;
            await _repository.UpdateAsync(rule, cancellationToken);
        }
        else
        {
            rule = new RetrospectiveRuleMasterEntity
            {
                RuleCode = request.RuleCode,
                RuleName = request.RuleName,
                RuleDescription = request.RuleDescription,
                PriorityNo = request.PriorityNo,
                MatchType = request.MatchType,
                IsFallbackRule = request.IsFallbackRule,
                RuleStatus = "Draft",
                AuthorizationStatus = request.AuthorizationStatus,
                LegalCapEnabled = request.LegalCapEnabled,
                LegalCapYears = request.LegalCapYears,
                NoticeDays = request.NoticeDays,
                VersionNo = request.VersionNo,
                ResolutionRef = request.ResolutionRef,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = request.EffectiveTo,
                Remarks = request.Remarks,
                IsActive = true,
                CreatedBy = request.UpdatedBy,
                CreatedDate = DateTime.Now
            };
            await _repository.AddAsync(rule, cancellationToken);
        }

        // Flush so a newly-created rule has its Id assigned before child sections reference it.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _evidenceConditionService.SetEvidenceStateForRuleAsync(rule.Id, new SetRetrospectiveRuleEvidenceConditionStateDto
        {
            AvailableEvidenceTypeIds = request.AvailableEvidenceTypeIds,
            UnavailableEvidenceTypeIds = request.UnavailableEvidenceTypeIds,
            UpdatedBy = request.UpdatedBy
        }, cancellationToken);

        await SaveDateConditionAsync(rule.Id, request.DateCondition, request.UpdatedBy, cancellationToken);
        await SaveActionAsync(rule.Id, request.Action, request.UpdatedBy, cancellationToken);
        await SavePenaltyRuleAsync(rule.Id, request.PenaltyRule, request.UpdatedBy, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(rule.Id, cancellationToken);
    }

    private async Task SaveDateConditionAsync(int ruleId, SaveRetrospectiveRuleDateConditionDto? section, int? updatedBy, CancellationToken cancellationToken)
    {
        var existing = await _dateConditionRepository.GetQueryable()
            .FirstOrDefaultAsync(c => c.RuleId == ruleId && c.IsActive, cancellationToken);

        if (section is null)
        {
            if (existing is not null)
            {
                existing.IsActive = false;
                existing.UpdatedBy = updatedBy;
                existing.UpdatedDate = DateTime.Now;
                await _dateConditionRepository.UpdateAsync(existing, cancellationToken);
            }
            return;
        }

        if (existing is not null)
        {
            existing.ComparatorCode = section.ComparatorCode;
            existing.LeftEvidenceTypeId = section.LeftEvidenceTypeId;
            existing.RightEvidenceTypeId = section.RightEvidenceTypeId;
            existing.CompareOperator = section.CompareOperator;
            existing.CompareDate = section.CompareDate;
            existing.CompareDateTo = section.CompareDateTo;
            existing.CompareYears = section.CompareYears;
            existing.UpdatedBy = updatedBy;
            existing.UpdatedDate = DateTime.Now;
            await _dateConditionRepository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            await _dateConditionRepository.AddAsync(new RetrospectiveRuleDateConditionEntity
            {
                RuleId = ruleId,
                ComparatorCode = section.ComparatorCode,
                LeftEvidenceTypeId = section.LeftEvidenceTypeId,
                RightEvidenceTypeId = section.RightEvidenceTypeId,
                CompareOperator = section.CompareOperator,
                CompareDate = section.CompareDate,
                CompareDateTo = section.CompareDateTo,
                CompareYears = section.CompareYears,
                IsActive = true,
                CreatedBy = updatedBy,
                CreatedDate = DateTime.Now
            }, cancellationToken);
        }
    }

    private async Task SaveActionAsync(int ruleId, SaveRetrospectiveRuleActionDto section, int? updatedBy, CancellationToken cancellationToken)
    {
        var existing = await _actionRepository.GetQueryable()
            .FirstOrDefaultAsync(a => a.RuleId == ruleId && a.IsActive, cancellationToken);

        if (existing is not null)
        {
            existing.TaxStartMode = section.TaxStartMode;
            existing.StartEvidenceTypeId = section.StartEvidenceTypeId;
            existing.OffsetMonths = section.OffsetMonths;
            existing.RetrospectiveLimitType = section.RetrospectiveLimitType;
            existing.MaximumYears = section.MaximumYears;
            existing.CutoffDate = section.CutoffDate;
            existing.TaxCalculationMode = section.TaxCalculationMode;
            existing.TaxMultiplier = section.TaxMultiplier;
            existing.SplitStartEvidenceTypeId = section.SplitStartEvidenceTypeId;
            existing.SplitEndEvidenceTypeId = section.SplitEndEvidenceTypeId;
            existing.SplitMultiplier = section.SplitMultiplier;
            existing.AfterSplitMultiplier = section.AfterSplitMultiplier;
            existing.UpdatedBy = updatedBy;
            existing.UpdatedDate = DateTime.Now;
            await _actionRepository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            await _actionRepository.AddAsync(new RetrospectiveRuleActionEntity
            {
                RuleId = ruleId,
                TaxStartMode = section.TaxStartMode,
                StartEvidenceTypeId = section.StartEvidenceTypeId,
                OffsetMonths = section.OffsetMonths,
                RetrospectiveLimitType = section.RetrospectiveLimitType,
                MaximumYears = section.MaximumYears,
                CutoffDate = section.CutoffDate,
                TaxCalculationMode = section.TaxCalculationMode,
                TaxMultiplier = section.TaxMultiplier,
                SplitStartEvidenceTypeId = section.SplitStartEvidenceTypeId,
                SplitEndEvidenceTypeId = section.SplitEndEvidenceTypeId,
                SplitMultiplier = section.SplitMultiplier,
                AfterSplitMultiplier = section.AfterSplitMultiplier,
                IsActive = true,
                CreatedBy = updatedBy,
                CreatedDate = DateTime.Now
            }, cancellationToken);
        }
    }

    private async Task SavePenaltyRuleAsync(int ruleId, SaveRetrospectivePenaltyRuleDto? section, int? updatedBy, CancellationToken cancellationToken)
    {
        var existing = await _penaltyRepository.GetQueryable()
            .FirstOrDefaultAsync(p => p.RuleId == ruleId && p.IsActive, cancellationToken);

        if (section is null)
        {
            if (existing is not null)
            {
                existing.IsActive = false;
                existing.UpdatedBy = updatedBy;
                existing.UpdatedDate = DateTime.Now;
                await _penaltyRepository.UpdateAsync(existing, cancellationToken);
            }
            return;
        }

        if (existing is not null)
        {
            existing.IsPenaltyApplicable = section.IsPenaltyApplicable;
            existing.PenaltyMode = section.PenaltyMode;
            existing.PenaltyPercent = section.PenaltyPercent;
            existing.PenaltyDateSourceType = section.PenaltyDateSourceType;
            existing.PenaltyDateEvidenceTypeId = section.PenaltyDateEvidenceTypeId;
            existing.PenaltyDateCondition = section.PenaltyDateCondition;
            existing.CompareDate = section.CompareDate;
            existing.CompareDateTo = section.CompareDateTo;
            existing.ElseAction = section.ElseAction;
            existing.RequiresManualReview = section.RequiresManualReview;
            existing.Remarks = section.Remarks;
            existing.UpdatedBy = updatedBy;
            existing.UpdatedDate = DateTime.Now;
            await _penaltyRepository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            await _penaltyRepository.AddAsync(new RetrospectivePenaltyRuleEntity
            {
                RuleId = ruleId,
                IsPenaltyApplicable = section.IsPenaltyApplicable,
                PenaltyMode = section.PenaltyMode,
                PenaltyPercent = section.PenaltyPercent,
                PenaltyDateSourceType = section.PenaltyDateSourceType,
                PenaltyDateEvidenceTypeId = section.PenaltyDateEvidenceTypeId,
                PenaltyDateCondition = section.PenaltyDateCondition,
                CompareDate = section.CompareDate,
                CompareDateTo = section.CompareDateTo,
                ElseAction = section.ElseAction,
                RequiresManualReview = section.RequiresManualReview,
                Remarks = section.Remarks,
                IsActive = true,
                CreatedBy = updatedBy,
                CreatedDate = DateTime.Now
            }, cancellationToken);
        }
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

    public async Task<RangeResult<RetrospectiveRuleMasterDto>> CreateFromRangeAsync(RangeCreateRequest<CreateRetrospectiveRuleMasterDto> request, CancellationToken cancellationToken = default)
    {
        Func<CreateRetrospectiveRuleMasterDto, string, int, CreateRetrospectiveRuleMasterDto> transformer = (template, rangeValue, sequenceNo) =>
            new CreateRetrospectiveRuleMasterDto
            {
                RuleCode = rangeValue,
                RuleName = string.IsNullOrEmpty(template.RuleName) ? rangeValue : template.RuleName.Replace("{value}", rangeValue),
                RuleDescription = template.RuleDescription,
                PriorityNo = template.PriorityNo,
                MatchType = template.MatchType,
                IsFallbackRule = template.IsFallbackRule,
                RuleStatus = template.RuleStatus,
                AuthorizationStatus = template.AuthorizationStatus,
                LegalCapEnabled = template.LegalCapEnabled,
                LegalCapYears = template.LegalCapYears,
                NoticeDays = template.NoticeDays,
                VersionNo = template.VersionNo,
                ResolutionRef = template.ResolutionRef,
                EffectiveFrom = template.EffectiveFrom,
                EffectiveTo = template.EffectiveTo,
                Remarks = template.Remarks,
                IsActive = template.IsActive,
                CreatedBy = template.CreatedBy
            };

        return await base.CreateFromRangeAsync(request, transformer, cancellationToken);
    }
}
