using System;
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
        public async Task<decimal> ApplyRulesAsync(
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
                    return context.InitialValue;

                // Guard: invalid floor or type-of-use group — rule engine cannot match
                if (p.Detail.FloorId <= 0 || p.DetailTypeOfUse.TypeOfUseGroupId <= 0)
                    return context.InitialValue;

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

                if (ruleResults is { Count: > 0 })
                {
                    decimal cumulativeValue = context.InitialValue;

                    foreach (var rule in ruleResults)
                    {
                        var applicator = _effectApplicators.FirstOrDefault(a => a.CanHandle(rule.EffectType));
                        if (applicator == null)
                            continue;

                        cumulativeValue = await applicator.Apply(cumulativeValue, rule.EffectValue);

                        if (rule.StopProcessing)
                            break;
                    }

                    return cumulativeValue;
                }

                return context.InitialValue;
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
            var flatDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            FlattenObject(property, flatDict);
            FlattenObject(detail, flatDict);
            FlattenObject(detailTypeOfUse, flatDict);
            FlattenObject(assessment, flatDict);

            // ── Step 2: Derived helper values ──────────────────────────────────────
            flatDict["PropertyAge"] = p.FinanceYear - p.ConstructionYearValue;
            flatDict["SocialAttributeId"] = p.SocialAttributeId;
            flatDict["SocialAttribute"] = p.SocialAttributeId;


            if (detail != null)
                flatDict["Rented"] = detail.IsRenter ?? false;

            // ── Step 2b: Social attributes (dynamic — keyed by SocialAttributeCode) ─
            // Any attribute from PTIS.SocialAttributeMaster is injected here automatically.
            // Rule expressions reference them as: input.HAS_SOLAR, input.NO_OF_WELL, input.HAS_LIFT, etc.
            // To add a new attribute to rules: just add it in the DB master + property data entry.
            // NO code change needed here.
            foreach (var (attrCode, attrValue) in p.SocialAttributes)
            {
                flatDict[attrCode] = attrValue;
            }

            // ── Step 3: Copy all scalar/primitive flat values into the input context ─
            var inputContext = new Dictionary<string, object>(flatDict, StringComparer.OrdinalIgnoreCase);

            // ── Step 4: Legacy key aliases (space-separated names used in older rule definitions) ─
            if (detail != null)
            {
                AddLegacyKey(flatDict, inputContext, "FloorId", "FloorId");
                AddLegacyKey(flatDict, inputContext, "ConstructionTypeId", "ConstructionTypeId");
                AddLegacyKey(flatDict, inputContext, "TypeOfUseId", "TypeOfUseId");
                AddLegacyKey(flatDict, inputContext, "CarpetAreaSqMeter", "CarpetAreaSqMeter");
                AddLegacyKey(flatDict, inputContext, "CarpetAreaSqFeet", "CarpetAreaSqFeet");
                AddLegacyKey(flatDict, inputContext, "BuiltupAreaSqMeter", "BuiltupAreaSqMeter");
                AddLegacyKey(flatDict, inputContext, "BuiltupAreaSqFeet", "BuiltupAreaSqFeet");
                AddLegacyKey(flatDict, inputContext, "SubFloorId", "SubFloorId");
            }

            if (detailTypeOfUse != null)
                AddLegacyKey(flatDict, inputContext, "TypeOfUseGroupId", "TypeOfUseGroupId");

            if (property != null)
            {
                AddLegacyKey(flatDict, inputContext, "Id", "Id");
                AddLegacyKey(flatDict, inputContext, "WardId", "WardId");
                AddLegacyKey(flatDict, inputContext, "TaxZoneId", "TaxZoneId");
            }

            if (assessment != null)
                AddLegacyKey(flatDict, inputContext, "OwnerTypeId", "OwnerTypeId");

            // ── Step 5: DB-configured rules fields (final authority) ───────────────
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
                    if (flatDict.TryGetValue(key, out var val))
                    {
                        inputContext[field.FieldName] = val;
                        break;
                    }
                }
            }

            return inputContext;
        }

        /// <summary>
        /// Reflects over all public scalar instance properties of <paramref name="obj"/>
        /// and writes their values into <paramref name="dict"/> using the C# property name as key.
        /// Navigation properties and collections (non-string reference types) are skipped to avoid
        /// serialization loops and unintended key collisions.
        /// </summary>
        private static void FlattenObject(object? obj, Dictionary<string, object> dict)
        {
            if (obj == null) return;

            var properties = obj.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                // Skip navigation properties and collections — only primitive/scalar values
                if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                    continue;

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

        /// <summary>
        /// Copies a value from <paramref name="flatDict"/> under <paramref name="sourceKey"/>
        /// into <paramref name="inputContext"/> under <paramref name="targetKey"/>,
        /// but only if the target key does not already exist (preserving higher-priority values).
        /// </summary>
        private static void AddLegacyKey(
            Dictionary<string, object> flatDict,
            Dictionary<string, object> inputContext,
            string sourceKey,
            string targetKey)
        {
            if (flatDict.TryGetValue(sourceKey, out var val) && !inputContext.ContainsKey(targetKey))
                inputContext[targetKey] = val;
        }
    }
}
