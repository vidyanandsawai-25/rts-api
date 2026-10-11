using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RTSFeeRuleMaster;
using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services.Rules;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>Manages fee rules in the shared RTS.RuleMaster table.</summary>
public sealed class RTSFeeRuleService : IRTSFeeRuleService
{
    private readonly IRepository<RTSRuleMasterEntity, int> _rules;
    private readonly IRepository<RTSServiceEntity, int> _services;
    private readonly IRepository<RTSDepartmentEntity, int> _departments;
    private readonly IRepository<RTSFieldDefinitionEntity, int> _fields;
    private readonly IUnitOfWork _unitOfWork;

    public RTSFeeRuleService(
        IRepository<RTSRuleMasterEntity, int> rules,
        IRepository<RTSServiceEntity, int> services,
        IRepository<RTSDepartmentEntity, int> departments,
        IRepository<RTSFieldDefinitionEntity, int> fields,
        IUnitOfWork unitOfWork)
    {
        _rules = rules;
        _services = services;
        _departments = departments;
        _fields = fields;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<RTSFeeRuleDto>> GetAsync(int serviceId, CancellationToken cancellationToken = default) =>
        (await _rules.GetQueryable().AsNoTracking().Include(x => x.Conditions)
            .Where(x => x.ServiceId == serviceId && x.RuleGroup == "FEE").OrderBy(x => x.Priority).ThenBy(x => x.Id).ToListAsync(cancellationToken))
        .Select(ToDto).ToList();

    public async Task<RTSFeeRuleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _rules.GetQueryable().AsNoTracking().Include(x => x.Conditions)
            .FirstOrDefaultAsync(x => x.Id == id && x.RuleGroup == "FEE", cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<RTSFeeRuleDto> CreateAsync(SaveRTSFeeRuleRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);
        var entity = ToEntity(request);
        entity.CreatedBy = request.UpdatedBy;
        entity.CreatedDate = DateTime.UtcNow;
        await _rules.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<RTSFeeRuleDto> UpdateAsync(int id, SaveRTSFeeRuleRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = await _rules.GetQueryable().Include(x => x.Conditions)
            .FirstOrDefaultAsync(x => x.Id == id && x.RuleGroup == "FEE", cancellationToken) ?? throw new KeyNotFoundException("Fee rule was not found.");
        await ValidateAsync(request, cancellationToken);
        Apply(request, entity);
        entity.UpdatedBy = request.UpdatedBy;
        entity.UpdatedDate = DateTime.UtcNow;
        await _rules.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _rules.GetQueryable().Include(x => x.Conditions)
            .FirstOrDefaultAsync(x => x.Id == id && x.RuleGroup == "FEE", cancellationToken) ?? throw new KeyNotFoundException("Fee rule was not found.");
        await _rules.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<RTSFeeRuleServiceCardDto>> GetServiceCardsAsync(RTSFeeRuleServiceCardQueryParameters query, CancellationToken cancellationToken = default)
    {
        var cards = from service in _services.GetQueryable().AsNoTracking()
                    join department in _departments.GetQueryable().AsNoTracking() on service.DepartmentId equals department.Id
                    join rule in _rules.GetQueryable().AsNoTracking().Where(rule => rule.RuleGroup == "FEE") on service.Id equals rule.ServiceId into rules
                    where service.IsActive && department.IsActive && rules.Any()
                    select new RTSFeeRuleServiceCardDto
        {
            ServiceId = service.Id,
            ServiceName = service.ServiceName,
            DepartmentId = department.Id,
            DepartmentName = department.DepartmentName,
            RuleCount = rules.Count(),
            ActiveRuleCount = rules.Count(rule => rule.IsActive)
        };
        if (query.DepartmentId.HasValue) cards = cards.Where(card => card.DepartmentId == query.DepartmentId.Value);
        if (query.ServiceId.HasValue) cards = cards.Where(card => card.ServiceId == query.ServiceId.Value);
        if (query.IsActive.HasValue)
            cards = cards.Where(card => query.IsActive.Value ? card.ActiveRuleCount > 0 : card.ActiveRuleCount < card.RuleCount);
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            cards = cards.Where(card => card.ServiceName.Contains(term) || card.DepartmentName.Contains(term));
        }
        var total = await cards.CountAsync(cancellationToken);
        var descending = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        cards = (query.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "departmentname" => descending ? cards.OrderByDescending(card => card.DepartmentName).ThenByDescending(card => card.ServiceName) : cards.OrderBy(card => card.DepartmentName).ThenBy(card => card.ServiceName),
            "rulecount" => descending ? cards.OrderByDescending(card => card.RuleCount).ThenBy(card => card.ServiceName) : cards.OrderBy(card => card.RuleCount).ThenBy(card => card.ServiceName),
            "activerulecount" => descending ? cards.OrderByDescending(card => card.ActiveRuleCount).ThenBy(card => card.ServiceName) : cards.OrderBy(card => card.ActiveRuleCount).ThenBy(card => card.ServiceName),
            _ => descending ? cards.OrderByDescending(card => card.ServiceName).ThenByDescending(card => card.ServiceId) : cards.OrderBy(card => card.ServiceName).ThenBy(card => card.ServiceId)
        };
        var items = query.PageSize == -1
            ? await cards.ToListAsync(cancellationToken)
            : await cards.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        var pageSize = query.PageSize == -1 ? Math.Max(1, total) : query.PageSize;
        return new PagedResult<RTSFeeRuleServiceCardDto>(items, total, query.PageSize == -1 ? 1 : query.PageNumber, pageSize);
    }

    public async Task<RTSFeeEvaluationResponseDto> EvaluateFeeAsync(RTSFeeEvaluationRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.ServiceId <= 0) throw new ArgumentException("Service ID must be greater than zero.");
        var service = await _services.GetQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ServiceId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("The selected RTS service is unavailable.");
        var values = request.FieldValues.Where(x => x.FieldDefinitionId > 0)
            .GroupBy(x => x.FieldDefinitionId).ToDictionary(x => x.Key, x => x.Last().Value?.Trim() ?? string.Empty);
        var rules = await _rules.GetQueryable().AsNoTracking().Include(x => x.Conditions)
            .Where(x => x.ServiceId == request.ServiceId && x.IsActive && x.RuleGroup == "FEE")
            .OrderBy(x => x.Priority).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var matches = rules.Where(x => RTSRuleConditionMatcher.Matches(x, values)).ToList();
        var baseRules = matches.Where(x => x.RuleActionType == "BASE_FEE").ToList();
        var fallback = baseRules.Count == 0;
        RTSRuleMasterEntity? selectedBase = null;
        decimal baseFee;
        if (fallback) baseFee = Round(service.Fees ?? 0m);
        else
        {
            var priority = baseRules.Min(x => x.Priority);
            var candidates = baseRules.Where(x => x.Priority == priority).ToList();
            if (candidates.Count > 1) throw new InvalidOperationException("More than one BASE_FEE rule matches at the highest priority. Correct the fee-rule priorities.");
            selectedBase = candidates[0];
            baseFee = Calculate(selectedBase, values, service.Fees ?? 0m);
        }
        var matched = new List<RTSFeeMatchedRuleDto>();
        if (selectedBase is not null) matched.Add(ToMatched(selectedBase, baseFee));
        decimal addons = 0m;
        foreach (var addon in matches.Where(x => x.RuleActionType == "ADDON_FEE"))
        {
            var amount = Calculate(addon, values, baseFee);
            addons += amount;
            matched.Add(ToMatched(addon, amount));
        }
        addons = Round(addons);
        return new RTSFeeEvaluationResponseDto { ServiceId = request.ServiceId, StaticServiceFee = service.Fees ?? 0m, BaseFee = baseFee, AddonFee = addons, PayableAmount = Round(baseFee + addons), UsedServiceFeeFallback = fallback, MatchedRules = matched };
    }

    private async Task ValidateAsync(SaveRTSFeeRuleRequestDto request, CancellationToken ct)
    {
        var action = Upper(request.RuleActionType); var calculation = Upper(request.CalculationType); var match = Upper(request.ConditionMatchType);
        if (action is not ("BASE_FEE" or "ADDON_FEE")) throw new InvalidOperationException("Fee rules must use BASE_FEE or ADDON_FEE.");
        if (match is not ("ALL" or "ANY")) throw new InvalidOperationException("Condition match type must be ALL or ANY.");
        if (calculation is not ("FIXED" or "PER_UNIT" or "PERCENTAGE")) throw new InvalidOperationException("Calculation type must be FIXED, PER_UNIT, or PERCENTAGE.");
        if (action == "BASE_FEE" && calculation == "PERCENTAGE") throw new InvalidOperationException("BASE_FEE supports FIXED and PER_UNIT calculations only.");
        if (calculation == "PER_UNIT" && !request.QuantityFieldDefinitionId.HasValue) throw new InvalidOperationException("PER_UNIT rules require a quantity field.");
        if (calculation != "PER_UNIT" && request.QuantityFieldDefinitionId.HasValue) throw new InvalidOperationException("Only PER_UNIT rules can have a quantity field.");
        if (request.Conditions.Count == 0) throw new InvalidOperationException("At least one condition is required.");
        var ids = request.Conditions.Select(x => x.FieldDefinitionId).Append(request.QuantityFieldDefinitionId ?? 0).Where(x => x > 0).Distinct().ToList();
        var fields = await _fields.GetQueryable().AsNoTracking().Where(x => x.ServiceId == request.ServiceId && x.IsActive && !x.MarkedForDeletion && ids.Contains(x.Id)).ToListAsync(ct);
        if (fields.Count != ids.Count) throw new InvalidOperationException("Conditions and quantity field must be active fields of the selected service.");
        foreach (var condition in request.Conditions)
        {
            var field = fields.Single(x => x.Id == condition.FieldDefinitionId);
            if (!RTSRuleConditionMatcher.IsValidOperator(field.FieldType, condition.ComparisonOperator)) throw new InvalidOperationException($"Operator {condition.ComparisonOperator} is not valid for {field.FieldLabel}.");
            if (string.IsNullOrWhiteSpace(condition.ExpectedValue)) throw new InvalidOperationException("Every condition requires an expected value.");
        }
        if (request.QuantityFieldDefinitionId.HasValue && !IsNumeric(fields.Single(x => x.Id == request.QuantityFieldDefinitionId.Value).FieldType)) throw new InvalidOperationException("The quantity field must be numeric.");
    }

    private static decimal Calculate(RTSRuleMasterEntity rule, IReadOnlyDictionary<int, string> values, decimal selectedBase)
    {
        var type = Upper(rule.CalculationType);
        if (type == "FIXED") return Round(rule.RateAmount ?? 0m);
        if (type == "PERCENTAGE") return Round(selectedBase * (rule.RateAmount ?? 0m) / 100m);
        if (!rule.QuantityFieldDefinitionId.HasValue || !values.TryGetValue(rule.QuantityFieldDefinitionId.Value, out var raw) || !decimal.TryParse(raw, out var quantity) || quantity < 0)
            throw new InvalidOperationException($"A valid non-negative quantity is required for fee rule '{rule.RuleDescription}'.");
        return Round((rule.RateAmount ?? 0m) * quantity);
    }

    private static bool IsNumeric(string type) => Upper(type) is "NUMBER" or "DECIMAL" or "INTEGER" or "INT" or "CURRENCY";
    private static string Upper(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;
    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static RTSFeeMatchedRuleDto ToMatched(RTSRuleMasterEntity rule, decimal amount) => new() { RuleId = rule.Id, RuleActionType = rule.RuleActionType, ConditionFieldDefinitionId = rule.Conditions.OrderBy(x => x.ConditionOrder).FirstOrDefault()?.FieldDefinitionId, ExpectedValue = rule.Conditions.OrderBy(x => x.ConditionOrder).FirstOrDefault()?.ExpectedValue, Amount = amount, Priority = rule.Priority, CalculationType = rule.CalculationType };
    private static RTSRuleMasterEntity ToEntity(SaveRTSFeeRuleRequestDto request) { var entity = new RTSRuleMasterEntity(); Apply(request, entity); return entity; }
    private static void Apply(SaveRTSFeeRuleRequestDto request, RTSRuleMasterEntity entity) { entity.ServiceId = request.ServiceId; entity.RuleDescription = request.RuleDescription.Trim(); entity.RuleActionType = Upper(request.RuleActionType); entity.RuleGroup = "FEE"; entity.ConditionMatchType = Upper(request.ConditionMatchType); entity.CalculationType = Upper(request.CalculationType); entity.RateAmount = request.RateAmount; entity.QuantityFieldDefinitionId = request.QuantityFieldDefinitionId; entity.Priority = request.Priority; entity.IsActive = request.IsActive; entity.UserId = null; entity.ActionValue = null; entity.IsCumulative = false; entity.Conditions.Clear(); entity.Conditions.AddRange(request.Conditions.OrderBy(x => x.ConditionOrder).Select((x, index) => new RTSRuleConditionEntity { FieldDefinitionId = x.FieldDefinitionId, ComparisonOperator = Upper(x.ComparisonOperator), ExpectedValue = x.ExpectedValue.Trim(), ConditionOrder = index + 1, IsActive = true, CreatedDate = DateTime.UtcNow })); }
    private static RTSFeeRuleDto ToDto(RTSRuleMasterEntity entity) => new() { Id = entity.Id, ServiceId = entity.ServiceId, RuleDescription = entity.RuleDescription, RuleActionType = entity.RuleActionType, ConditionMatchType = entity.ConditionMatchType ?? "ALL", CalculationType = entity.CalculationType ?? "FIXED", RateAmount = entity.RateAmount ?? 0m, QuantityFieldDefinitionId = entity.QuantityFieldDefinitionId, Priority = entity.Priority, IsActive = entity.IsActive, Conditions = entity.Conditions.OrderBy(x => x.ConditionOrder).Select(x => new RTSFeeRuleConditionDto { Id = x.Id, FieldDefinitionId = x.FieldDefinitionId, ComparisonOperator = x.ComparisonOperator, ExpectedValue = x.ExpectedValue, ConditionOrder = x.ConditionOrder }).ToList() };
}
