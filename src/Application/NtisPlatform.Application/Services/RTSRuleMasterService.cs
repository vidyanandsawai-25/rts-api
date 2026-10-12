using System.Globalization;
using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Persists fee and user-allocation rules in the shared RTS.RuleMaster table.
/// Rule-group-specific services provide validation and runtime evaluation only.
/// </summary>
public class RTSRuleMasterService : BaseCommonCrudService<RTSRuleMasterEntity, RTSRuleMasterDto,
    CreateRTSRuleMasterRequestDto, UpdateRTSRuleMasterRequestDto, RTSRuleMasterQueryParameters, int>,
    IRTSRuleMasterService
{
    private readonly IRtsFeeEvaluationRuleService _feeRules;
    private readonly IRTSStageAllocationRuleService _allocationRules;

    public RTSRuleMasterService(
        IRepository<RTSRuleMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRtsFeeEvaluationRuleService feeRules,
        IRTSStageAllocationRuleService allocationRules)
        : base(repository, unitOfWork, mapper)
    {
        _feeRules = feeRules;
        _allocationRules = allocationRules;
    }

    protected override IQueryable<RTSRuleMasterEntity> ApplyIncludes(IQueryable<RTSRuleMasterEntity> query) =>
        query.Include(x => x.Conditions);

    public override async Task<RTSRuleMasterDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable()
            .AsNoTracking()
            .Include(x => x.Conditions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity == null ? null : _mapper.Map<RTSRuleMasterDto>(entity);
    }

    public async Task<RTSRuleMasterResponseDto> CreateRulesAsync(
        CreateRTSRuleMasterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Rules.Count == 0)
            throw new InvalidOperationException("At least one rule is required.");

        foreach (var rule in request.Rules)
        {
            ValidateAction(rule.RuleGroup, rule.RuleActionType);
            ValidateDetails(rule.RuleDescription, rule.Priority);
            if (IsFee(rule.RuleGroup))
            {
                await _feeRules.ValidateRuleAsync(
                    request.ServiceId,
                    rule.RuleActionType,
                    rule.ConditionMatchType,
                    rule.CalculationType,
                    rule.RateAmount,
                    rule.QuantityFieldDefinitionId,
                    rule.Conditions,
                    cancellationToken);
            }
            else
            {
                await _allocationRules.ValidateAsync(
                    request.ServiceId,
                    rule.UserId,
                    rule.ActionValue,
                    rule.ConditionMatchType,
                    rule.Conditions,
                    cancellationToken);
            }
        }

        var entities = request.Rules.Select(rule =>
        {
            var entity = new RTSRuleMasterEntity
            {
                ServiceId = request.ServiceId,
                CreatedBy = request.CreatedBy,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };
            Apply(rule, entity);
            return entity;
        }).ToList();

        await _repository.AddRangeAsync(entities, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Response(request.ServiceId, entities);
    }

    public async Task<RTSRuleMasterResponseDto> UpdateRulesAsync(
        int serviceId,
        UpdateRTSRuleMasterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (serviceId <= 0 || request.ServiceId != serviceId)
            throw new InvalidOperationException("Service ID does not match the rule request.");
        if (request.Rules.Count == 0)
            throw new InvalidOperationException("At least one rule is required for update.");

        var ids = request.Rules.Select(x => x.Id ?? 0).ToList();
        if (ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Count)
            throw new InvalidOperationException("Every updated rule needs a distinct valid ID.");

        var entities = await _repository.GetQueryable()
            .Include(x => x.Conditions)
            .Where(x => ids.Contains(x.Id) && x.ServiceId == serviceId)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (entities.Count != ids.Count)
            throw new InvalidOperationException("One or more rules were not found for this service.");

        foreach (var rule in request.Rules)
        {
            ValidateAction(rule.RuleGroup, rule.RuleActionType);
            ValidateDetails(rule.RuleDescription, rule.Priority);
            var entity = entities[rule.Id!.Value];
            var requestedGroup = NormalizeGroup(rule.RuleGroup);
            if (!string.Equals(entity.RuleGroup, requestedGroup, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A rule cannot be moved to another rule group.");

            if (IsFee(requestedGroup))
            {
                await _feeRules.ValidateRuleAsync(
                    serviceId,
                    rule.RuleActionType,
                    rule.ConditionMatchType,
                    rule.CalculationType,
                    rule.RateAmount,
                    rule.QuantityFieldDefinitionId,
                    rule.Conditions,
                    cancellationToken);
            }
            else
            {
                await _allocationRules.ValidateAsync(
                    serviceId,
                    rule.UserId,
                    rule.ActionValue,
                    rule.ConditionMatchType,
                    rule.Conditions,
                    cancellationToken);
            }

            Apply(rule, entity);
            entity.IsActive = rule.IsActive;
            entity.UpdatedBy = request.UpdatedBy;
            entity.UpdatedDate = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Response(serviceId, entities.Values);
    }

    public Task<RTSFeeEvaluationResponseDto> EvaluateFeeAsync(
        RTSFeeEvaluationRequestDto request,
        CancellationToken cancellationToken = default) =>
        _feeRules.EvaluateFeeAsync(request, cancellationToken);

    private static void ValidateAction(string? group, string? action)
    {
        var normalizedGroup = NormalizeGroup(group);
        var normalizedAction = action?.Trim().ToUpperInvariant();
        var supported = normalizedGroup switch
        {
            "FEE" => normalizedAction is "BASE_FEE" or "ADDON_FEE",
            "USER_ALLOCATION" => normalizedAction == "STAGE_ALLOCATION",
            _ => false
        };
        if (!supported)
            throw new InvalidOperationException("Use BASE_FEE or ADDON_FEE for FEE rules, and STAGE_ALLOCATION for USER_ALLOCATION rules.");
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
        target.RuleGroup = NormalizeGroup(source.RuleGroup);
        target.RuleActionType = source.RuleActionType?.Trim().ToUpperInvariant() ?? string.Empty;
        target.ConditionMatchType = source.ConditionMatchType.Trim().ToUpperInvariant();
        target.Priority = source.Priority;
        ApplyGroupValues(source.RuleGroup, source.CalculationType, source.RateAmount,
            source.QuantityFieldDefinitionId, source.UserId, source.ActionValue, target);
        ReplaceConditions(source.Conditions, target);
    }

    private static void Apply(UpdateRTSRuleDto source, RTSRuleMasterEntity target)
    {
        target.RuleDescription = source.RuleDescription?.Trim() ?? string.Empty;
        target.RuleGroup = NormalizeGroup(source.RuleGroup);
        target.RuleActionType = source.RuleActionType?.Trim().ToUpperInvariant() ?? string.Empty;
        target.ConditionMatchType = source.ConditionMatchType.Trim().ToUpperInvariant();
        target.Priority = source.Priority;
        ApplyGroupValues(source.RuleGroup, source.CalculationType, source.RateAmount,
            source.QuantityFieldDefinitionId, source.UserId, source.ActionValue, target);
        ReplaceConditions(source.Conditions, target);
    }

    private static void ApplyGroupValues(
        string? group,
        string? calculationType,
        decimal? rateAmount,
        int? quantityFieldDefinitionId,
        int? userId,
        string? actionValue,
        RTSRuleMasterEntity target)
    {
        if (IsFee(group))
        {
            target.CalculationType = calculationType?.Trim().ToUpperInvariant();
            target.RateAmount = rateAmount;
            target.QuantityFieldDefinitionId = quantityFieldDefinitionId;
            target.UserId = null;
            target.ActionValue = null;
        }
        else
        {
            target.CalculationType = null;
            target.RateAmount = null;
            target.QuantityFieldDefinitionId = null;
            target.UserId = userId;
            target.ActionValue = actionValue;
        }
        target.IsCumulative = false;
    }

    private static bool IsFee(string? group) =>
        string.Equals(group?.Trim(), "FEE", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeGroup(string? group) =>
        group?.Trim().ToUpperInvariant() ?? string.Empty;

    private static void ReplaceConditions(
        IEnumerable<RTSRuleConditionDto> conditions,
        RTSRuleMasterEntity target)
    {
        var existing = target.Conditions.OrderBy(x => x.ConditionOrder).ToList();
        var requested = conditions.OrderBy(x => x.ConditionOrder).ToList();

        for (var index = 0; index < requested.Count; index++)
        {
            var condition = requested[index];
            var row = index < existing.Count
                ? existing[index]
                : new RTSRuleConditionEntity { CreatedDate = DateTime.UtcNow };

            row.FieldDefinitionId = condition.FieldDefinitionId;
            row.ComparisonOperator = condition.ComparisonOperator.Trim().ToUpperInvariant();
            row.ExpectedValue = condition.ExpectedValue.Trim();
            row.ConditionOrder = index + 1;
            row.IsActive = true;

            if (index >= existing.Count)
                target.Conditions.Add(row);
        }

        foreach (var stale in existing.Skip(requested.Count))
            target.Conditions.Remove(stale);
    }

    private RTSRuleMasterResponseDto Response(
        int serviceId,
        IEnumerable<RTSRuleMasterEntity> entities)
    {
        var rules = entities.Select(x => _mapper.Map<RTSRuleMasterDto>(x)).ToList();
        return new RTSRuleMasterResponseDto
        {
            ServiceId = serviceId,
            RuleCount = rules.Count,
            Rules = rules
        };
    }
}

public class RTSRuleServiceCardService : IRTSRuleServiceCardService
{
    private readonly IRepository<RTSRuleMasterEntity, int> _rules;
    private readonly IRepository<RTSServiceEntity, int> _services;
    private readonly IRepository<RTSDepartmentEntity, int> _departments;

    public RTSRuleServiceCardService(
        IRepository<RTSRuleMasterEntity, int> rules,
        IRepository<RTSServiceEntity, int> services,
        IRepository<RTSDepartmentEntity, int> departments)
    {
        _rules = rules;
        _services = services;
        _departments = departments;
    }

    public async Task<PagedResult<RTSRuleServiceCardDto>> GetCardsAsync(
        RTSRuleServiceCardQueryParameters query,
        CancellationToken ct = default)
    {
        var selectedGroup = query.RuleGroup.Trim().ToUpperInvariant();
        if (selectedGroup.Length == 0)
            throw new ArgumentException("RuleGroup is required.", nameof(query));

        if (selectedGroup is not ("FEE" or "USER_ALLOCATION"))
            return new PagedResult<RTSRuleServiceCardDto>([], 0, 1,
                query.PageSize == -1 ? 1 : query.PageSize);

        var cards =
            from service in _services.GetQueryable().AsNoTracking()
            join department in _departments.GetQueryable().AsNoTracking()
                on service.DepartmentId equals department.Id
            join rule in _rules.GetQueryable().AsNoTracking()
                on service.Id equals rule.ServiceId
            where rule.RuleGroup == selectedGroup
                && service.IsActive
                && department.IsActive
                && (!query.DepartmentId.HasValue || service.DepartmentId == query.DepartmentId.Value)
                && (!query.ServiceId.HasValue || service.Id == query.ServiceId.Value)
            group rule by new
            {
                service.Id,
                service.ServiceName,
                service.DepartmentId,
                department.DepartmentName
            }
            into grouped
            select new RTSRuleServiceCardDto
            {
                ServiceId = grouped.Key.Id,
                ServiceName = grouped.Key.ServiceName,
                DepartmentId = grouped.Key.DepartmentId,
                DepartmentName = grouped.Key.DepartmentName,
                RuleGroup = selectedGroup,
                RuleCount = grouped.Count(),
                ActiveRuleCount = grouped.Count(rule => rule.IsActive)
            };

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim();
            cards = cards.Where(card =>
                card.ServiceName.Contains(search) || card.DepartmentName.Contains(search));
        }

        if (query.IsActive.HasValue)
        {
            cards = cards.Where(card => query.IsActive.Value
                ? card.ActiveRuleCount > 0
                : card.ActiveRuleCount < card.RuleCount);
        }

        var total = await cards.CountAsync(ct);
        var descending = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        var ordered = (query.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "departmentname" => descending
                ? cards.OrderByDescending(card => card.DepartmentName)
                    .ThenByDescending(card => card.ServiceName)
                : cards.OrderBy(card => card.DepartmentName).ThenBy(card => card.ServiceName),
            "rulecount" => descending
                ? cards.OrderByDescending(card => card.RuleCount).ThenBy(card => card.ServiceName)
                : cards.OrderBy(card => card.RuleCount).ThenBy(card => card.ServiceName),
            "activerulecount" => descending
                ? cards.OrderByDescending(card => card.ActiveRuleCount).ThenBy(card => card.ServiceName)
                : cards.OrderBy(card => card.ActiveRuleCount).ThenBy(card => card.ServiceName),
            _ => descending
                ? cards.OrderByDescending(card => card.ServiceName)
                    .ThenByDescending(card => card.ServiceId)
                : cards.OrderBy(card => card.ServiceName).ThenBy(card => card.ServiceId)
        };
        var items = query.PageSize == -1
            ? await ordered.ToListAsync(ct)
            : await ordered.Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize).ToListAsync(ct);
        var size = query.PageSize == -1 ? Math.Max(1, total) : query.PageSize;
        return new PagedResult<RTSRuleServiceCardDto>(items, total, query.PageNumber, size);
    }
}

public sealed class RTSStageAllocationRuleService : IRTSStageAllocationRuleService
{
    private readonly IRepository<RTSRuleMasterEntity, int> _rules;
    private readonly IRepository<RTSApprovalFlowMasterEntity, int> _flows;
    private readonly IRepository<RTSApprovalFlowStageUserAllocationMasterEntity, int> _allocations;
    private readonly IRepository<RTSFieldDefinitionEntity, int> _fields;
    private readonly IRepository<UserEntity, int> _users;

    public RTSStageAllocationRuleService(
        IRepository<RTSRuleMasterEntity, int> rules,
        IRepository<RTSApprovalFlowMasterEntity, int> flows,
        IRepository<RTSApprovalFlowStageUserAllocationMasterEntity, int> allocations,
        IRepository<RTSFieldDefinitionEntity, int> fields,
        IRepository<UserEntity, int> users)
    {
        _rules = rules;
        _flows = flows;
        _allocations = allocations;
        _fields = fields;
        _users = users;
    }

    public async Task ValidateAsync(
        int serviceId,
        int? userId,
        string? actionValue,
        string? conditionMatchType,
        IReadOnlyList<RTSRuleConditionDto> conditions,
        CancellationToken cancellationToken = default)
    {
        if (serviceId <= 0) throw new InvalidOperationException("Select an RTS service.");
        if (conditionMatchType?.Trim().ToUpperInvariant() is not ("ALL" or "ANY"))
            throw new InvalidOperationException("Condition match type must be ALL or ANY.");
        if (conditions.Count == 0)
            throw new InvalidOperationException("At least one condition is required.");
        if (conditions.Any(x => x.FieldDefinitionId <= 0 ||
            string.IsNullOrWhiteSpace(x.ExpectedValue) || x.ExpectedValue.Length > 500))
            throw new InvalidOperationException(
                "Every condition requires a field and expected value (up to 500 characters).");

        var fieldIds = conditions.Select(x => x.FieldDefinitionId).Distinct().ToList();
        var fields = await _fields.GetQueryable().AsNoTracking()
            .Where(x => x.ServiceId == serviceId && x.IsActive &&
                !x.MarkedForDeletion && fieldIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (fields.Count != fieldIds.Count)
            throw new InvalidOperationException(
                "Every condition field must be active and belong to this service.");

        foreach (var condition in conditions)
        {
            var field = fields[condition.FieldDefinitionId];
            if (!RTSRuleConditionMatcher.IsValidOperator(
                field.FieldType, condition.ComparisonOperator))
                throw new InvalidOperationException(
                    $"Operator {condition.ComparisonOperator} is not valid for {field.FieldLabel}.");
        }

        var (stageId, stageOrder) = ParseStage(actionValue);
        var flow = await _flows.GetQueryable().AsNoTracking()
            .Include(x => x.ApprovalFlowStages)
            .Where(x => x.ServiceId == serviceId && x.IsActive)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (flow == null || !flow.ApprovalFlowStages.Any(x =>
            x.Id == stageId && x.StageOrder == stageOrder && x.IsActive))
            throw new InvalidOperationException(
                "Select a stage in the service's active approval workflow.");
        if (!userId.HasValue || userId.Value <= 0 ||
            !await IsEligibleAsync(stageId, userId.Value, cancellationToken))
            throw new InvalidOperationException(
                "Select an active officer allocated to the chosen stage.");
    }

    public async Task<int> ResolveOfficerAsync(
        int serviceId,
        int approvalFlowId,
        int stageId,
        int defaultUserId,
        IReadOnlyDictionary<int, string> values,
        CancellationToken cancellationToken = default)
    {
        var candidates = await _rules.GetQueryable().AsNoTracking()
            .Include(x => x.Conditions)
            .Where(x => x.ServiceId == serviceId && x.IsActive &&
                x.RuleGroup == "USER_ALLOCATION" &&
                x.RuleActionType == "STAGE_ALLOCATION")
            .ToListAsync(cancellationToken);
        var matches = candidates
            .Where(x => TargetsStage(x.ActionValue, stageId) &&
                RTSRuleConditionMatcher.Matches(x, values))
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .ToList();
        if (matches.Count == 0) return defaultUserId;
        if (matches.Count > 1 && matches[0].Priority == matches[1].Priority)
            throw new InvalidOperationException(
                $"Multiple allocation rules match stage {stageId} at priority {matches[0].Priority}.");

        var winner = matches[0];
        var stageBelongsToFlow = await _flows.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id == approvalFlowId && x.ServiceId == serviceId &&
                x.ApprovalFlowStages.Any(stage => stage.Id == stageId), cancellationToken);
        if (!stageBelongsToFlow || !winner.UserId.HasValue ||
            !await IsEligibleAsync(stageId, winner.UserId.Value, cancellationToken))
            throw new InvalidOperationException(
                $"The selected officer for allocation rule {winner.Id} is no longer valid for stage {stageId}.");
        return winner.UserId.Value;
    }

    public IReadOnlyDictionary<int, string> NormalizeValues(
        IEnumerable<RTSFieldValueEntity> values) =>
        values.Where(x => x.FieldDefinitionId > 0 && !x.MarkedForDeletion)
            .GroupBy(x => x.FieldDefinitionId)
            .ToDictionary(
                x => x.Key,
                x => x.Last().TextValue?.Trim()
                    ?? x.Last().NumberValue?.ToString(CultureInfo.InvariantCulture)
                    ?? x.Last().DateValue?.ToString("O", CultureInfo.InvariantCulture)
                    ?? x.Last().BooleanValue?.ToString()
                    ?? string.Empty);

    private async Task<bool> IsEligibleAsync(int stageId, int userId, CancellationToken ct) =>
        await _allocations.GetQueryable().AsNoTracking().AnyAsync(x =>
            x.ApprovalFlowStageId == stageId && x.UserId == userId && x.IsActive, ct) &&
        await _users.GetQueryable().AsNoTracking().AnyAsync(x =>
            x.Id == userId && x.IsActive && !x.MarkedForDeletion, ct);

    private static bool TargetsStage(string? actionValue, int stageId)
    {
        try
        {
            return ParseStage(actionValue).StageId == stageId;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static (int StageId, int StageOrder) ParseStage(string? actionValue)
    {
        try
        {
            using var document = JsonDocument.Parse(actionValue ?? string.Empty);
            var root = document.RootElement;
            var stageId = root.GetProperty("approvalFlowStageId").GetInt32();
            var order = root.GetProperty("stageOrder").GetInt32();
            if (stageId > 0 && order > 0) return (stageId, order);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or
            InvalidOperationException or KeyNotFoundException)
        {
        }

        throw new InvalidOperationException("Select a valid approval workflow stage.");
    }
}

public static class RTSRuleConditionMatcher
{
    public static bool Matches(
        RTSRuleMasterEntity rule,
        IReadOnlyDictionary<int, string> values)
    {
        var conditions = rule.Conditions
            .Where(x => x.IsActive)
            .OrderBy(x => x.ConditionOrder)
            .ToList();
        if (conditions.Count == 0) return false;
        var results = conditions.Select(condition =>
            values.TryGetValue(condition.FieldDefinitionId, out var actual) &&
            !string.IsNullOrWhiteSpace(actual) &&
            Compare(actual, condition.ExpectedValue, condition.ComparisonOperator));
        return string.Equals(rule.ConditionMatchType, "ANY", StringComparison.OrdinalIgnoreCase)
            ? results.Any(x => x)
            : results.All(x => x);
    }

    public static bool IsValidOperator(string fieldType, string comparisonOperator)
    {
        var type = fieldType.Trim().ToUpperInvariant();
        var op = comparisonOperator.Trim().ToUpperInvariant();
        if (op is "=" or "!=") return true;
        if (op == "CONTAINS")
            return type is not ("NUMBER" or "DECIMAL" or "INTEGER" or "INT" or
                "CURRENCY" or "AMOUNT" or "YEAR" or "DATE" or "DATETIME" or
                "TIME" or "MONTH");
        return op is ">" or ">=" or "<" or "<=" &&
            type is "NUMBER" or "DECIMAL" or "INTEGER" or "INT" or "CURRENCY" or
                "AMOUNT" or "YEAR" or "DATE" or "DATETIME" or "TIME" or "MONTH";
    }

    public static bool Compare(string actual, string expected, string comparisonOperator)
    {
        var op = comparisonOperator.Trim().ToUpperInvariant();
        if (op == "CONTAINS")
            return actual.Contains(expected, StringComparison.OrdinalIgnoreCase);
        if (op is "=" or "!=")
        {
            var equal = string.Equals(
                actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
            return op == "=" ? equal : !equal;
        }
        if (decimal.TryParse(actual, NumberStyles.Number, CultureInfo.InvariantCulture,
                out var number) &&
            decimal.TryParse(expected, NumberStyles.Number, CultureInfo.InvariantCulture,
                out var expectedNumber))
            return CompareOrdered(number, expectedNumber, op);
        if (DateTime.TryParse(actual, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var date) &&
            DateTime.TryParse(expected, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var expectedDate))
            return CompareOrdered(date, expectedDate, op);
        return false;
    }

    private static bool CompareOrdered<T>(T actual, T expected, string op)
        where T : IComparable<T> => op switch
        {
            ">" => actual.CompareTo(expected) > 0,
            ">=" => actual.CompareTo(expected) >= 0,
            "<" => actual.CompareTo(expected) < 0,
            "<=" => actual.CompareTo(expected) <= 0,
            _ => false
        };
}
