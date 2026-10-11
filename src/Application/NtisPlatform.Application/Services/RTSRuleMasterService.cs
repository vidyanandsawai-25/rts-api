using AutoMapper;
using NtisPlatform.Application.DTOs.Bulk;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSRuleMasterService : BaseCommonCrudService<RTSRuleMasterEntity, RTSRuleMasterDto,
    CreateRTSRuleMasterRequestDto, UpdateRTSRuleMasterRequestDto, RTSRuleMasterQueryParameters, int>,
    IRTSRuleMasterService
{
    private readonly IRTSFeeRuleService _feeRules;
    private readonly IRTSStageAllocationRuleService _allocationRules;

    public RTSRuleMasterService(IRepository<RTSRuleMasterEntity, int> repository,
        IUnitOfWork unitOfWork, IMapper mapper, IRTSFeeRuleService feeRules,
        IRTSStageAllocationRuleService allocationRules) : base(repository, unitOfWork, mapper)
    {
        _feeRules = feeRules;
        _allocationRules = allocationRules;
    }

    protected override IQueryable<RTSRuleMasterEntity> ApplyIncludes(IQueryable<RTSRuleMasterEntity> query) =>
        query.Include(x => x.Conditions);

    public override async Task<BulkResult<RTSRuleMasterDto>> BulkCreateAsync(CreateRTSRuleMasterRequestDto[] items, CancellationToken cancellationToken = default)
    {
        if (items == null || items.Length == 0)
            return new BulkResult<RTSRuleMasterDto>(0, 0, []);

        var entities = new List<RTSRuleMasterEntity>();

        foreach (var batch in items)
        {
            if (batch.Rules == null || batch.Rules.Count == 0) continue;

            var mapped = _mapper.Map<List<RTSRuleMasterEntity>>(batch.Rules);
            foreach (var e in mapped)
            {
                e.ServiceId = batch.ServiceId;
                e.CreatedBy = batch.CreatedBy;
                e.CreatedDate = DateTime.UtcNow;
                e.IsActive = true;
            }

            entities.AddRange(mapped);
        }

        if (entities.Count > 0)
        {
            await _repository.AddRangeAsync(entities, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var dtos = _mapper.Map<List<RTSRuleMasterDto>>(entities);
        return new BulkResult<RTSRuleMasterDto>(dtos.Count, 0, dtos);
    }


    public override async Task<RTSRuleMasterDto?> UpdateAsync(int serviceId, UpdateRTSRuleMasterRequestDto updateDto,CancellationToken cancellationToken = default)
    public override async Task<RTSRuleMasterDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable().AsNoTracking().Include(x => x.Conditions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity == null ? null : _mapper.Map<RTSRuleMasterDto>(entity);
    }

    public async Task<RTSRuleMasterResponseDto> CreateRulesAsync(
        CreateRTSRuleMasterRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Rules == null || request.Rules.Count == 0)
            throw new InvalidOperationException("At least one rule is required.");
        foreach (var rule in request.Rules)
        {
            ValidateAction(rule.RuleGroup, rule.RuleActionType);
            ValidateDetails(rule.RuleDescription, rule.Priority);
            await _allocationRules.ValidateAsync(request.ServiceId, rule.UserId, rule.ActionValue,
                rule.ConditionMatchType, rule.Conditions, cancellationToken);
        }

        var entities = request.Rules.Select(rule =>
        {
            var entity = new RTSRuleMasterEntity { ServiceId = request.ServiceId, CreatedBy = request.CreatedBy,
                CreatedDate = DateTime.UtcNow, IsActive = true };
            Apply(rule, entity);
            return entity;
        }).ToList();
        await _repository.AddRangeAsync(entities, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Response(request.ServiceId, entities);
    }

    public async Task<RTSRuleMasterResponseDto> UpdateRulesAsync(int serviceId,
        UpdateRTSRuleMasterRequestDto request, CancellationToken cancellationToken = default)
    {
        if (serviceId <= 0 || request.ServiceId != serviceId)
            throw new InvalidOperationException("Service ID does not match the rule request.");
        if (request.Rules == null || request.Rules.Count == 0)
            throw new InvalidOperationException("At least one rule is required for update.");

        RTSRuleMasterEntity? firstUpdated = null;

        foreach (var ruleDto in updateDto.Rules)
        {
            if (!ruleDto.Id.HasValue || ruleDto.Id.Value <= 0)
                continue;

            var entity = await _repository.GetByIdAsync(ruleDto.Id.Value, cancellationToken);
            if (entity == null) continue;

            _mapper.Map(ruleDto, entity);
            entity.ServiceId = serviceId;
            entity.UpdatedBy = updateDto.UpdatedBy;
            entity.UpdatedDate = DateTime.UtcNow;

            await _repository.UpdateAsync(entity, cancellationToken);
            firstUpdated ??= entity;
        var ids = request.Rules.Select(x => x.Id ?? 0).ToList();
        if (ids.Any(x => x <= 0) || ids.Distinct().Count() != ids.Count)
            throw new InvalidOperationException("Every updated rule needs a distinct valid ID.");
        var entities = await _repository.GetQueryable().Include(x => x.Conditions)
            .Where(x => ids.Contains(x.Id) && x.ServiceId == serviceId && x.RuleGroup == "USER_ALLOCATION")
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (entities.Count != ids.Count)
            throw new InvalidOperationException("One or more allocation rules were not found for this service.");
        foreach (var rule in request.Rules)
        {
            ValidateAction(rule.RuleGroup, rule.RuleActionType);
            ValidateDetails(rule.RuleDescription, rule.Priority);
            await _allocationRules.ValidateAsync(serviceId, rule.UserId, rule.ActionValue,
                rule.ConditionMatchType, rule.Conditions, cancellationToken);
            var entity = entities[rule.Id!.Value];
            Apply(rule, entity);
            entity.IsActive = rule.IsActive;
            entity.UpdatedBy = request.UpdatedBy;
            entity.UpdatedDate = DateTime.UtcNow;
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Response(serviceId, entities.Values);
    }

        return firstUpdated == null ? null : _mapper.Map<RTSRuleMasterDto>(firstUpdated);
    public Task<RTSFeeEvaluationResponseDto> EvaluateFeeAsync(RTSFeeEvaluationRequestDto request,
        CancellationToken cancellationToken = default) => _feeRules.EvaluateFeeAsync(request, cancellationToken);

    private static void ValidateAction(string? group, string? action)
    {
        if (!string.Equals(group, "USER_ALLOCATION", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(action, "STAGE_ALLOCATION", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use USER_ALLOCATION and STAGE_ALLOCATION for assignment rules; fee rules use the RTS Fee Rule API.");
    }

    private static void ValidateDetails(string? description, int priority)
    {
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 200)
            throw new InvalidOperationException("Enter a rule description of up to 200 characters.");
        if (priority < 1)
            throw new InvalidOperationException("Rule priority must be a positive number.");
    }

    private static void Apply(CreateRTSRuleDto source, RTSRuleMasterEntity target)
    {
        target.RuleDescription = source.RuleDescription?.Trim() ?? string.Empty;
        target.RuleGroup = "USER_ALLOCATION";
        target.RuleActionType = "STAGE_ALLOCATION";
        target.ConditionMatchType = source.ConditionMatchType.Trim().ToUpperInvariant();
        target.UserId = source.UserId;
        target.ActionValue = source.ActionValue;
        target.Priority = source.Priority;
        target.RateAmount = null;
        target.CalculationType = null;
        target.QuantityFieldDefinitionId = null;
        target.IsCumulative = false;
        ReplaceConditions(source.Conditions, target);
    }

    private static void Apply(UpdateRTSRuleDto source, RTSRuleMasterEntity target)
    {
        target.RuleDescription = source.RuleDescription?.Trim() ?? string.Empty;
        target.ConditionMatchType = source.ConditionMatchType.Trim().ToUpperInvariant();
        target.UserId = source.UserId;
        target.ActionValue = source.ActionValue;
        target.Priority = source.Priority;
        ReplaceConditions(source.Conditions, target);
    }

    private static void ReplaceConditions(IEnumerable<RTSRuleConditionDto> conditions, RTSRuleMasterEntity target)
    {
        var existing = target.Conditions.OrderBy(x => x.ConditionOrder).ToList();
        var requested = conditions.OrderBy(x => x.ConditionOrder).ToList();
        for (var index = 0; index < requested.Count; index++)
        {
            var condition = requested[index];
            var row = index < existing.Count ? existing[index] : new RTSRuleConditionEntity { CreatedDate = DateTime.UtcNow };
            row.FieldDefinitionId = condition.FieldDefinitionId;
            row.ComparisonOperator = condition.ComparisonOperator.Trim().ToUpperInvariant();
            row.ExpectedValue = condition.ExpectedValue.Trim();
            row.ConditionOrder = index + 1;
            row.IsActive = true;
            if (index >= existing.Count) target.Conditions.Add(row);
        }
        foreach (var stale in existing.Skip(requested.Count)) target.Conditions.Remove(stale);
    }

    private RTSRuleMasterResponseDto Response(int serviceId, IEnumerable<RTSRuleMasterEntity> entities)
    {
        var rules = entities.Select(x => _mapper.Map<RTSRuleMasterDto>(x)).ToList();
        return new RTSRuleMasterResponseDto { ServiceId = serviceId, RuleCount = rules.Count, Rules = rules };
    }
}
