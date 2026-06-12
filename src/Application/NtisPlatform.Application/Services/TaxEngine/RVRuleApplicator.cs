using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Services.Rules.Effects;
using NtisPlatform.Core.Entities;
using System.Diagnostics;

namespace NtisPlatform.Application.Services.TaxEngine;

/// <summary>
/// Applies the RV rule engine for a single property detail.
/// Extracted from <c>RateableValueService</c> to satisfy the Single Responsibility Principle.
/// On any exception the method fails open (returns <c>null</c>) so that a rule-engine outage
/// does not block the entire calculation pipeline.
/// </summary>
public sealed class RVRuleApplicator : IRVRuleApplicator
{
    private readonly IRuleExecutionService _ruleExecutionService;
    private readonly IEnumerable<IRuleEffectApplicator> _effectApplicators;
    private readonly ILogger<RVRuleApplicator> _logger;

    public RVRuleApplicator(
        IRuleExecutionService ruleExecutionService,
        IEnumerable<IRuleEffectApplicator> effectApplicators,
        ILogger<RVRuleApplicator> logger)
    {
        _ruleExecutionService = ruleExecutionService;
        _effectApplicators = effectApplicators;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<decimal?> GetAdjustedRateAsync(
        PropertyDetailsEntity detail,
        TypeOfUseEntity detailTypeOfUse,
        PropertyEntity property,
        PropertyAssessmentEntity? propertyAssessment,
        bool hasLift,
        int constructionYearValue,
        int financeYear,
        int yearRangeRVId,
        decimal masterRatePerUnit)
    {
        const string category = "RV";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (detail.FloorId <= 0 || detailTypeOfUse.TypeOfUseGroupId <= 0)
            {
                _logger.LogDebug(
                    "[RuleEngine-{Category}] Skipping PropertyDetailsId={DetailId}: Invalid Floor or TypeOfUseGroup",
                    category, detail.Id);
                return null;
            }

            var inputContext = BuildInputContext(
                detail, detailTypeOfUse, property, propertyAssessment,
                hasLift, constructionYearValue, financeYear, yearRangeRVId);

            inputContext["Rate"] = (double)masterRatePerUnit;

            var ruleInput = new RuleExecutionInputDto { Category = category, Input = inputContext };

            var ruleResults = await RetryHelper.ExecuteWithRetryAsync(
                operation: () => _ruleExecutionService.ExecuteAsync(ruleInput),
                logger: _logger,
                operationName: "RuleEngine",
                contextId: $"PropertyDetailsId={detail.Id}",
                maxRetries: 3);

            stopwatch.Stop();

            LogMetric("RuleExecution.Duration", stopwatch.ElapsedMilliseconds, new Dictionary<string, string>
            {
                { "PropertyDetailsId", detail.Id.ToString() },
                { "Category", category }
            });

            if (ruleResults == null || !ruleResults.Any())
            {
                _logger.LogDebug(
                    "[RuleEngine-{Category}] No rules matched for PropertyDetailsId={DetailId} in {ElapsedMs}ms",
                    category, detail.Id, stopwatch.ElapsedMilliseconds);
                return null;
            }

            decimal cumulative = masterRatePerUnit;
            var appliedRules = new List<string>();

            foreach (var rule in ruleResults)
            {
                var applicator = _effectApplicators.FirstOrDefault(a => a.CanHandle(rule.EffectType));
                if (applicator == null)
                {
                    _logger.LogWarning(
                        "[RuleEngine-{Category}] No applicator for EffectType='{EffectType}' in rule '{RuleCode}', skipping",
                        category, rule.EffectType, rule.RuleCode);
                    continue;
                }

                var previous = cumulative;
                cumulative = await applicator.Apply(cumulative, rule.EffectValue);
                appliedRules.Add($"{rule.RuleCode}({rule.EffectType} {rule.EffectValue}%: {previous:F2}→{cumulative:F2})");

                _logger.LogDebug(
                    "[RuleEngine-{Category}] Rule '{RuleCode}' PropertyDetailsId={DetailId}: {Prev} → {New}",
                    category, rule.RuleCode, detail.Id, previous, cumulative);
            }

            LogMetric("RuleExecution.RulesApplied", ruleResults.Count, new Dictionary<string, string>
            {
                { "PropertyDetailsId", detail.Id.ToString() },
                { "Category", category },
                { "OriginalValue", masterRatePerUnit.ToString("F2") },
                { "FinalValue", cumulative.ToString("F2") }
            });

            _logger.LogInformation(
                "[RuleEngine-{Category}] Applied {Count} rule(s) to PropertyDetailsId={DetailId} in {Ms}ms: " +
                "{Original} → {Final}. Rules: {Rules}",
                category, appliedRules.Count, detail.Id, stopwatch.ElapsedMilliseconds,
                masterRatePerUnit, cumulative, string.Join(" → ", appliedRules));

            if (stopwatch.ElapsedMilliseconds > 100)
            {
                _logger.LogWarning(
                    "[RuleEngine-{Category}] Rule execution took {Ms}ms (>100ms budget) for PropertyDetailsId={DetailId}",
                    category, stopwatch.ElapsedMilliseconds, detail.Id);
            }

            return cumulative;
        }
        catch (ArgumentException argEx)
        {
            stopwatch.Stop();
            _logger.LogWarning(argEx,
                "[RuleEngine-{Category}] Validation error for PropertyDetailsId={DetailId} after {Ms}ms. Using original value.",
                category, detail.Id, stopwatch.ElapsedMilliseconds);
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "[RuleEngine-{Category}] Execution failed for PropertyDetailsId={DetailId} after {Ms}ms. Using original value.",
                category, detail.Id, stopwatch.ElapsedMilliseconds);
            return null;
        }
    }

    private Dictionary<string, object> BuildInputContext(
        PropertyDetailsEntity detail,
        TypeOfUseEntity detailTypeOfUse,
        PropertyEntity property,
        PropertyAssessmentEntity? propertyAssessment,
        bool hasLift,
        int constructionYearValue,
        int financeYear,
        int yearRangeRVId)
    {
        var context = new Dictionary<string, object>
        {
            { "Floor",                detail.FloorId },
            { "Type",                 detailTypeOfUse.TypeOfUseGroupId },
            { "Property Type",        property.Id },
            { "Ward",                 property.WardId },
            { "TaxZone",              property.TaxZoneId },
            { "PropertyDetailsId",    detail.Id },
            { "Construction Type",    detail.ConstructionTypeId },
            { "Type Of Use",          detail.TypeOfUseId },
            { "Carpet Area SqMeter",  detail.CarpetAreaSqMeter ?? 0 },
            { "Carpet Area SqFeet",   detail.CarpetAreaSqFeet ?? 0 },
            { "Builtup Area SqMeter", detail.BuiltupAreaSqMeter ?? 0 },
            { "Builtup Area SqFeet",  detail.BuiltupAreaSqFeet ?? 0 },
            { "NoOfRooms",            detail.NoOfRooms ?? 0 },
            { "Rented",               detail.IsRenter ?? false },
            { "ConstructionYear",     constructionYearValue },
            { "PropertyAge",          financeYear - constructionYearValue },
            { "FinanceYear",          financeYear },
            { "YearRangeRVId",        yearRangeRVId },
            { "Sub Floor",            detail.SubFloorId ?? 0 },
            { "Owner Type",           propertyAssessment?.OwnerTypeId ?? 0 },
            { "Lift",                 hasLift }
        };

        _logger.LogDebug(
            "[RuleEngine] Input context for PropertyDetailsId={DetailId}: Floor={Floor}, TypeGroup={TypeGroup}, " +
            "ConstructionType={ConstructionType}, Ward={Ward}, TaxZone={TaxZone}, " +
            "CarpetSqM={CarpetSqM}, Rooms={Rooms}, Age={Age}, Lift={Lift}, OwnerType={OwnerType}",
            detail.Id, detail.FloorId, detailTypeOfUse.TypeOfUseGroupId,
            detail.ConstructionTypeId, property.WardId, property.TaxZoneId,
            detail.CarpetAreaSqMeter, detail.NoOfRooms,
            financeYear - constructionYearValue, hasLift, propertyAssessment?.OwnerTypeId ?? 0);

        return context;
    }

    private void LogMetric(string metricName, double value, Dictionary<string, string>? properties = null)
    {
        _logger.LogInformation("[Metric] {MetricName} = {Value} {@Properties}",
            metricName, value, properties ?? new Dictionary<string, string>());
    }
}
