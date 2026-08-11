using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Core.Entities.Rules;

namespace NtisPlatform.Application.Services.Rules
{
    /// <summary>
    /// Flattens a detail-scoped <see cref="PropertyCalculationContext"/> into the flat
    /// <c>string → object</c> dictionary consumed by rule/condition evaluators.
    ///
    /// <para>
    /// Extracted verbatim from <see cref="RuleApplierService"/>'s former private
    /// <c>BuildRuleInputContext</c>/<c>FlattenObject</c> helpers so both the MS RulesEngine-based
    /// RV rule pipeline and the lightweight <see cref="IConditionRuleEvaluator"/> share exactly
    /// the same field-resolution behavior — no duplication, no behavior change to existing rules.
    /// </para>
    ///
    /// <para>
    /// <b>Population order (each step may override previous):</b><br/>
    /// 1. Scalar properties from all entity objects via reflection (C# property names as keys).<br/>
    /// 2. Derived scalar values (<c>PropertyAge</c>, <c>SocialAttributeId</c>, floor helpers).<br/>
    /// 3. Dynamic social attributes, keyed by their attribute code.<br/>
    /// 4. Active rules-field DB configuration — final authority on key-to-value resolution.
    /// </para>
    /// </summary>
    public class PropertyFieldFlattenerService : IPropertyFieldFlattenerService
    {
        public Dictionary<string, object> Flatten(
            PropertyCalculationContext context,
            List<RulesFieldEntity> activeFields)
        {
            var pContext = context;
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

            // Resolve per-detail YearRangeRVId (used by rate lookups and year-scoped rules)
            entityScalarPropertiesDict["YearRangeRVId"] = p.YearRangeRVIdForDetail ?? p.YearRangeRVId;
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
