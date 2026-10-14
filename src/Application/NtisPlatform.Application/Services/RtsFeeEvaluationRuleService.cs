using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Validates and evaluates fee rules. Rule persistence is owned by
/// <see cref="RTSRuleMasterService"/> so every rule group uses one API.
/// </summary>
public sealed class RtsFeeEvaluationRuleService : IRtsFeeEvaluationRuleService
{
    private readonly IRepository<RTSRuleMasterEntity, int> _rules;
    private readonly IRepository<RTSServiceEntity, int> _services;
    private readonly IRepository<RTSFieldDefinitionEntity, int> _fields;

    public RtsFeeEvaluationRuleService(
        IRepository<RTSRuleMasterEntity, int> rules,
        IRepository<RTSServiceEntity, int> services,
        IRepository<RTSFieldDefinitionEntity, int> fields)
    {
        _rules = rules;
        _services = services;
        _fields = fields;
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

    public async Task ValidateRuleAsync(
        int serviceId,
        string? ruleActionType,
        string? conditionMatchType,
        string? calculationType,
        decimal? rateAmount,
        int? quantityFieldDefinitionId,
        IReadOnlyCollection<RTSRuleConditionDto> conditions,
        CancellationToken ct = default)
    {
        var action = Upper(ruleActionType); var calculation = Upper(calculationType); var match = Upper(conditionMatchType);
        if (action is not ("BASE_FEE" or "ADDON_FEE")) throw new InvalidOperationException("Fee rules must use BASE_FEE or ADDON_FEE.");
        if (match is not ("ALL" or "ANY")) throw new InvalidOperationException("Condition match type must be ALL or ANY.");
        if (calculation is not ("FIXED" or "PER_UNIT" or "PER_SQUARE_METER" or "PER_SQUARE_FOOT" or "PERCENTAGE")) throw new InvalidOperationException("Calculation type must be FIXED, PER_UNIT, PER_SQUARE_METER, PER_SQUARE_FOOT, or PERCENTAGE.");
        if (action == "BASE_FEE" && calculation == "PERCENTAGE") throw new InvalidOperationException("BASE_FEE supports fixed and quantity/area-based calculations only.");
        if (!rateAmount.HasValue || rateAmount.Value < 0) throw new InvalidOperationException("Fee rate must be zero or greater.");
        if (IsQuantityCalculation(calculation) && !quantityFieldDefinitionId.HasValue) throw new InvalidOperationException($"{calculation} rules require a numeric {QuantityFieldLabel(calculation)} field.");
        if (!IsQuantityCalculation(calculation) && quantityFieldDefinitionId.HasValue) throw new InvalidOperationException("Only per-unit or per-area rules can have a quantity field.");
        var predicates = RTSRuleConditionTree.GetPredicateConditions(conditionMatchType, conditions);
        var ids = predicates.Select(x => x.FieldDefinitionId!.Value)
            .Append(quantityFieldDefinitionId ?? 0).Where(x => x > 0).Distinct().ToList();
        var fields = await _fields.GetQueryable().AsNoTracking().Where(x => x.ServiceId == serviceId && x.IsActive && !x.MarkedForDeletion && ids.Contains(x.Id)).ToListAsync(ct);
        if (fields.Count != ids.Count) throw new InvalidOperationException("Conditions and quantity field must be active fields of the selected service.");
        foreach (var condition in predicates)
        {
            var field = fields.Single(x => x.Id == condition.FieldDefinitionId!.Value);
            if (!RTSRuleConditionMatcher.IsValidOperator(field.FieldType, condition.ComparisonOperator!)) throw new InvalidOperationException($"Operator {condition.ComparisonOperator} is not valid for {field.FieldLabel}.");
            if (string.IsNullOrWhiteSpace(condition.ExpectedValue)) throw new InvalidOperationException("Every condition requires an expected value.");
            if (!RTSRuleConditionMatcher.IsValidExpectedValue(
                    field.FieldType, condition.ExpectedValue, condition.ComparisonOperator!, out var validationError))
                throw new InvalidOperationException(validationError);
        }
        if (quantityFieldDefinitionId.HasValue && !IsNumeric(fields.Single(x => x.Id == quantityFieldDefinitionId.Value).FieldType)) throw new InvalidOperationException("The quantity field must be numeric.");
    }

    private static decimal Calculate(RTSRuleMasterEntity rule, IReadOnlyDictionary<int, string> values, decimal selectedBase)
    {
        var type = Upper(rule.CalculationType);
        if (type == "FIXED") return Round(rule.RateAmount ?? 0m);
        if (type == "PERCENTAGE") return Round(selectedBase * (rule.RateAmount ?? 0m) / 100m);
        if (!IsQuantityCalculation(type)) throw new InvalidOperationException($"Unsupported fee calculation type '{type}'.");
        if (!rule.QuantityFieldDefinitionId.HasValue || !values.TryGetValue(rule.QuantityFieldDefinitionId.Value, out var raw) || !decimal.TryParse(raw, out var quantity) || quantity < 0)
            throw new InvalidOperationException($"A valid non-negative {QuantityFieldLabel(type)} value is required for fee rule '{rule.RuleDescription}'.");
        return Round((rule.RateAmount ?? 0m) * quantity);
    }

    private static bool IsQuantityCalculation(string calculation) => calculation is "PER_UNIT" or "PER_SQUARE_METER" or "PER_SQUARE_FOOT";

    private static string QuantityFieldLabel(string calculation) => calculation switch
    {
        "PER_SQUARE_METER" => "square-meter area",
        "PER_SQUARE_FOOT" => "square-foot area",
        _ => "quantity"
    };

    private static bool IsNumeric(string type) => Upper(type) is "NUMBER" or "DECIMAL" or "INTEGER" or "INT" or "CURRENCY";
    private static string Upper(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;
    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static RTSFeeMatchedRuleDto ToMatched(RTSRuleMasterEntity rule, decimal amount)
    {
        var first = rule.Conditions
            .Where(x => string.Equals(x.ConditionType, "PREDICATE", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.ConditionOrder)
            .FirstOrDefault();
        return new RTSFeeMatchedRuleDto
        {
            RuleId = rule.Id,
            RuleActionType = rule.RuleActionType,
            ConditionFieldDefinitionId = first?.FieldDefinitionId,
            ExpectedValue = first?.ExpectedValue,
            Amount = amount,
            Priority = rule.Priority,
            CalculationType = rule.CalculationType
        };
    }
}
