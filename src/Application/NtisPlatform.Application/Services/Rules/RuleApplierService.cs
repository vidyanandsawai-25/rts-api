using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Services.Rules.Effects;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Rules
{
    /// <summary>
    /// Executes category-scoped rules against a property context and sequentially applies
    /// the resulting effects to a starting value (e.g. a base rate or tax amount).
    ///
    /// <para>
    /// <b>Responsibility chain:</b><br/>
    /// 1. Validates the per-detail context (must have a resolved floor and type-of-use group).<br/>
    /// 2. Fetches the active rules field configuration from the database.<br/>
    /// 3. Flattens all entity properties + scalar parameters into a rules-engine input dictionary.<br/>
    /// 4. Delegates execution to <see cref="IRuleExecutionService"/> with retry logic.<br/>
    /// 5. Applies each matched rule effect via the registered <see cref="IRuleEffectApplicator"/> chain.<br/>
    /// 6. Returns the final adjusted value, or <c>context.InitialValue</c> if no rules matched.
    /// </para>
    ///
    /// <para>
    /// This service is stateless and thread-safe — it can be called concurrently from
    /// parallel detail processing loops.
    /// </para>
    /// </summary>
    public class RuleApplierService : IRuleApplierService
    {
        private readonly IRuleExecutionService _ruleExecutionService;
        private readonly IEnumerable<IRuleEffectApplicator> _effectApplicators;
        private readonly IRepository<RulesFieldEntity, int> _rulesFieldRepo;
        private readonly ILogger<RuleApplierService> _logger;

        public RuleApplierService(
            IRuleExecutionService ruleExecutionService,
            IEnumerable<IRuleEffectApplicator> effectApplicators,
            IRepository<RulesFieldEntity, int> rulesFieldRepo,
            ILogger<RuleApplierService> logger)
        {
            _ruleExecutionService = ruleExecutionService;
            _effectApplicators = effectApplicators;
            _rulesFieldRepo = rulesFieldRepo;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<RuleApplicationResult> ApplyRulesAsync(
            RuleApplierContext context,
            int maxRetries = 3,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var pContext = context.PropertyContext;
                var p = pContext?.Parameters;

                // Guard: context or per-detail entities missing — nothing to evaluate
                if (pContext == null || p?.Detail == null || p.DetailTypeOfUse == null)
                    return new RuleApplicationResult { FinalValue = context.InitialValue, AppliedRules = new() };

                // Guard: invalid floor or type-of-use group — rule engine cannot match
                if (p.Detail.FloorId <= 0 || p.DetailTypeOfUse.TypeOfUseGroupId <= 0)
                    return new RuleApplicationResult { FinalValue = context.InitialValue, AppliedRules = new() };

                // Fetch active field configuration (defines which entity fields the rule engine uses)
                var activeFields = await _rulesFieldRepo.GetQueryable()
                    .Where(f => f.IsActive)
                    .ToListAsync(cancellationToken);

                // Build the flat key/value dictionary the rules engine expects
                var inputContext = BuildRuleInputContext(context, activeFields);
                inputContext[context.ValueKey] = (double)context.InitialValue;

                var ruleInput = new RuleExecutionInputDto
                {
                    Category = context.Category,
                    Input = inputContext
                };

                var ruleResults = await RetryHelper.ExecuteWithRetryAsync(
                    operation: () => _ruleExecutionService.ExecuteAsync(ruleInput, cancellationToken),
                    logger: _logger,
                    operationName: "RuleEngine",
                    contextId: $"PropertyDetailsId={p.Detail.Id}",
                    maxRetries: maxRetries,
                    cancellationToken: cancellationToken);

                var appliedRules = new List<RuleApplicationTraceEntry>();

                if (ruleResults is { Count: > 0 })
                {
                    decimal cumulativeValue = context.InitialValue;
                    int applyOrder = 0;

                    foreach (var rule in ruleResults)
                    {
                        var applicator = _effectApplicators.FirstOrDefault(a => a.CanHandle(rule.EffectType ?? string.Empty));
                        if (applicator == null)
                            continue;

                        applyOrder++;
                        decimal nextValue = await applicator.Apply(cumulativeValue, rule.EffectValue);

                        decimal applyRate = rule.EffectValue;
                        if (rule.EffectType != null)
                        {
                            var effectLower = rule.EffectType.ToLowerInvariant();
                            if (effectLower.Contains("decrease") && (effectLower.Contains("%") || effectLower.Contains("percent")))
                            {
                                applyRate = 100m - rule.EffectValue;
                            }
                            else if (effectLower.Contains("increase") && (effectLower.Contains("%") || effectLower.Contains("percent")))
                            {
                                applyRate = 100m + rule.EffectValue;
                            }
                            else if (effectLower.Contains("exempt"))
                            {
                                applyRate = 0m;
                            }
                            else if (effectLower.Contains("multiply"))
                            {
                                applyRate = rule.EffectValue * 100m;
                            }
                        }

                        appliedRules.Add(new RuleApplicationTraceEntry
                        {
                            RuleCode = rule.RuleCode,
                            RuleName = rule.RuleName,
                            EffectType = rule.EffectType,
                            EffectValue = rule.EffectValue,
                            ApplyRate = applyRate,
                            BaseValue = context.InitialValue,
                            ComputedValue = nextValue,
                            CumulativeValue = nextValue,
                            ApplyOrder = applyOrder,
                            StopProcessing = rule.StopProcessing,
                            RuleScopeId = rule.RuleScopeId,
                            RuleScopeName = rule.RuleScopeName
                        });

                        cumulativeValue = nextValue;

                        if (rule.StopProcessing)
                            break;
                    }

                    return new RuleApplicationResult
                    {
                        FinalValue = cumulativeValue,
                        AppliedRules = appliedRules
                    };
                }

                return new RuleApplicationResult
                {
                    FinalValue = context.InitialValue,
                    AppliedRules = appliedRules
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Rule execution failed for Category={Category}, PropertyDetailsId={DetailId}",
                    context.Category,
                    context.PropertyContext?.Parameters?.Detail?.Id);
                throw;
            }
        }

        // ─── Private Helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Assembles the flat <c>string → object</c> dictionary that the rules engine
        /// consumes as its evaluation context.
        ///
        /// <para>
        /// <b>Population order (each step may override previous):</b><br/>
        /// 1. Scalar properties from all entity objects via reflection (C# property names as keys).<br/>
        /// 2. Derived scalar values (<c>PropertyAge</c>, <c>Lift</c>, <c>Rented</c>).<br/>
        /// 3. Legacy space-separated key aliases for backward compatibility with existing rule definitions.<br/>
        /// 4. Active rules-field DB configuration — final authority on key-to-value resolution.
        /// </para>
        /// </summary>
        private Dictionary<string, object> BuildRuleInputContext(
            RuleApplierContext context,
            List<RulesFieldEntity> activeFields)
        {
            var pContext = context.PropertyContext;
            var p = pContext.Parameters;

            var detail = p.Detail;
            var detailTypeOfUse = p.DetailTypeOfUse;
            var property = pContext.Property;
            var assessment = pContext.PropertyAssessment;

            // ── Step 1: Flatten entity scalar properties via reflection ─────────────
            var entityScalarPropertiesDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            FlattenObject(property, entityScalarPropertiesDict);
            FlattenObject(detail, entityScalarPropertiesDict);
            FlattenObject(detailTypeOfUse, entityScalarPropertiesDict);
            FlattenObject(assessment, entityScalarPropertiesDict);

            // ── Step 2: Derived helper values ────────────────────────────────────
            // PropertyAge: computed formula — no entity column exists for this
            entityScalarPropertiesDict["PropertyAge"] = p.FinanceYear - p.ConstructionYearValue;
            // SocialAttributeId: List<int> of active social attribute IDs for this property.
            // Used in rule expressions as: input.SocialAttributeId.Contains(38)
            // Reflection skips collections, so this must be set manually.
            entityScalarPropertiesDict["SocialAttributeId"] = p.SocialAttributeId;

            // Building floor properties
            entityScalarPropertiesDict["BuildingMaxFloorSequence"] = p.BuildingMaxFloorSequence;

            // Current floor properties for the detail scope
            if (detail?.Floor != null)
            {
                entityScalarPropertiesDict["FloorCode"] = detail.Floor.FloorCode ?? string.Empty;
                entityScalarPropertiesDict["FloorSequenceNo"] = detail.Floor.SequenceNo ?? 0;
            }

            // ── Step 2b: Social attributes (dynamic — keyed by SocialAttributeCode) ─
            // Any attribute from PTIS.SocialAttributeMaster is injected here automatically.
            // Rule expressions reference them as: input.HAS_SOLAR, input.NO_OF_WELL, input.HAS_LIFT, etc.
            // To add a new attribute to rules: just add it in the DB master + property data entry.
            // NO code change needed here.
            foreach (var (attrCode, attrValue) in p.SocialAttributes)
            {
                entityScalarPropertiesDict[attrCode] = attrValue;
            }

            // ── Step 3: Copy all scalar/primitive flat values into the input context ─
            var inputContext = new Dictionary<string, object>(entityScalarPropertiesDict, StringComparer.OrdinalIgnoreCase);

            // ── Step 4: DB-configured rules fields (final authority) ───────────────
            foreach (var field in activeFields)
            {
                if (string.IsNullOrWhiteSpace(field.FieldName))
                    continue;

                // Try to resolve by FieldName or DatabaseColumnName against the flat dict
                var keysToTry = new[] { field.FieldName, field.DatabaseColumnName }
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .Select(k => k!.Replace(" ", string.Empty))
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var key in keysToTry)
                {
                    if (entityScalarPropertiesDict.TryGetValue(key, out var val))
                    {
                        inputContext[field.FieldName] = val;
                        break;
                    }
                }
            }

            return inputContext;
        }

        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();

        /// <summary>
        /// Reflects over all public scalar instance properties of <paramref name="obj"/>
        /// and writes their values into <paramref name="dict"/> using the C# property name as key.
        /// Navigation properties and collections (non-string reference types) are skipped to avoid
        /// serialization loops and unintended key collisions.
        /// </summary>
        private static void FlattenObject(object? obj, Dictionary<string, object> dict)
        {
            if (obj == null) return;

            var type = obj.GetType();
            var properties = _propertyCache.GetOrAdd(type, t =>
                t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                 .Where(p => !(p.PropertyType.IsClass && p.PropertyType != typeof(string)))
                 .ToArray());

            foreach (var prop in properties)
            {
                try
                {
                    var val = prop.GetValue(obj);
                    if (val != null)
                        dict[prop.Name] = val;
                }
                catch
                {
                    // Property read errors are silently ignored (e.g. indexers, abstract getters)
                }
            }
        }

    }
}
