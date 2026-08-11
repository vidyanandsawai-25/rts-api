using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.DTOs.Rules.ConditionEvaluation;
using NtisPlatform.Application.Interfaces.Rules;

namespace NtisPlatform.Application.Services.Rules
{
    /// <summary>
    /// Stateless flat condition-list evaluator for Dynamic Tax Register's CONDITION_BASED rows
    /// (<c>TaxConditionRuleEntity.ConditionsJson</c>). Each condition (after the first) joins the
    /// running result via its own AND/OR, folded strictly left-to-right — no parentheses or
    /// operator precedence (e.g. "A AND B OR C" means "(A AND B) OR C"). Operator vocabulary and
    /// comparison semantics mirror <see cref="RuleJsonBuilder"/>'s OperatorMap 1:1, just evaluated
    /// directly at runtime instead of compiled into a C# expression string, and without any
    /// dependency on the MS RulesEngine package.
    /// </summary>
    public class ConditionRuleEvaluator : IConditionRuleEvaluator
    {
        public ConditionEvaluationResult Evaluate(string? conditionsJson, IReadOnlyDictionary<string, object> fieldValues)
        {
            var items = ParseConditions(conditionsJson);
            var result = new ConditionEvaluationResult();

            bool? accumulated = null;
            foreach (var item in items)
            {
                var trace = EvaluateOne(item, fieldValues);
                result.ConditionResults.Add(trace);

                if (accumulated == null)
                {
                    // The first condition's own LogicalOperator is meaningless — nothing precedes
                    // it to join with — always reported as "AND" (set inside EvaluateOne).
                    accumulated = trace.IsMatch;
                }
                else
                {
                    var isOr = string.Equals(trace.LogicalOperator, "OR", StringComparison.OrdinalIgnoreCase);
                    accumulated = isOr ? accumulated.Value || trace.IsMatch : accumulated.Value && trace.IsMatch;
                }
            }

            // An empty conditions list is vacuously true — an "always matches" catch-all row.
            result.IsMatch = accumulated ?? true;
            return result;
        }

        private static List<TaxConditionItemDto> ParseConditions(string? conditionsJson)
        {
            if (string.IsNullOrWhiteSpace(conditionsJson))
                return new();

            try
            {
                return JsonSerializer.Deserialize<List<TaxConditionItemDto>>(conditionsJson) ?? new();
            }
            catch
            {
                // Malformed/legacy JSON => treat as invalid rule, never a catch-all match.
                return new() { new TaxConditionItemDto { FieldId = "__INVALID_JSON__", Operator = "__INVALID__" } };
            }
        }

        /// <inheritdoc />
        public bool TryResolveNumericField(
            string? fieldId,
            IReadOnlyDictionary<string, object> fieldValues,
            out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(fieldId)) return false;

            // Identical key handling to EvaluateOne below — trim, then strip spaces — so the field
            // list offered for arithmetic is exactly the field list conditions can match on.
            var fieldKey = fieldId.Trim().Replace(" ", string.Empty);
            if (!fieldValues.TryGetValue(fieldKey, out var raw)) return false;

            return TryToDecimal(raw, out value);
        }

        private static ConditionItemEvaluationTrace EvaluateOne(
            TaxConditionItemDto item,
            IReadOnlyDictionary<string, object> fieldValues)
        {
            var fieldId = (item.FieldId ?? string.Empty).Trim();
            var fieldKey = fieldId.Replace(" ", string.Empty); // same convention as RuleJsonBuilder.ResolveInputProp
            var expected = UnwrapJson(item.Value);

            var normalizedLogicalOperator = string.Equals(item.LogicalOperator, "OR", StringComparison.OrdinalIgnoreCase)
                ? "OR"
                : "AND";

            var trace = new ConditionItemEvaluationTrace
            {
                FieldId = fieldId,
                Operator = item.Operator ?? string.Empty,
                LogicalOperator = normalizedLogicalOperator,
                ExpectedValue = expected,
            };

            if (!fieldValues.TryGetValue(fieldKey, out var actual))
            {
                trace.FieldResolved = false;
                trace.IsMatch = false;
                return trace;
            }

            trace.FieldResolved = true;
            trace.ActualValue = actual;

            var normalizedOperator = (item.Operator ?? string.Empty).Trim().Replace(" ", "_").ToUpperInvariant();
            trace.IsMatch = Compare(normalizedOperator, actual, expected);
            return trace;
        }

        // ─── Operator dispatch ─────────────────────────────────────────────────────

        private static bool Compare(string normalizedOperator, object? actual, object? expected)
        {
            switch (normalizedOperator)
            {
                case "EQUALS":
                case "=":
                case "==":
                case "EQUAL_TO":
                    return ValuesEqual(actual, expected);

                case "NOT_EQUALS":
                case "!=":
                case "NOT_EQUAL_TO":
                    return !ValuesEqual(actual, expected);

                case "GREATER_THAN":
                case ">":
                    return TryCompareNumeric(actual, expected, out var cmpGt) && cmpGt > 0;

                case "LESS_THAN":
                case "<":
                    return TryCompareNumeric(actual, expected, out var cmpLt) && cmpLt < 0;

                case "GREATER_THAN_OR_EQUALS":
                case ">=":
                case "GREATER_THAN_OR_EQUAL_TO":
                    return TryCompareNumeric(actual, expected, out var cmpGe) && cmpGe >= 0;

                case "LESS_THAN_OR_EQUALS":
                case "<=":
                case "LESS_THAN_OR_EQUAL_TO":
                    return TryCompareNumeric(actual, expected, out var cmpLe) && cmpLe <= 0;

                case "BETWEEN":
                case "VALUE_BETWEEN_RANGE":
                    return IsBetween(actual, expected);

                case "IN":
                case "VALUE_EXISTS_IN_LIST":
                    return IsInList(actual, expected);

                case "NOT_IN":
                case "VALUE_DOES_NOT_EXIST_IN_LIST":
                    return !IsInList(actual, expected);

                case "CONTAINS":
                case "CONTAINS_ANY":
                case "CONTAINS_ANY_MATCHING_VALUE":
                case "CONTAINS_ANY_MATCHING_VALUES":
                    return ContainsAny(actual, expected);

                case "CONTAINS_ALL":
                case "CONTAINS_ALL_MATCHING_VALUE":
                case "CONTAINS_ALL_MATCHING_VALUES":
                    return ContainsAll(actual, expected);

                default:
                    // Unknown operator — never throws, just never matches.
                    return false;
            }
        }

        private static bool ValuesEqual(object? actual, object? expected)
        {
            if (TryToDecimal(actual, out var a) && TryToDecimal(expected, out var e))
                return a == e;
            return string.Equals(ToComparableString(actual), ToComparableString(expected), StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryCompareNumeric(object? actual, object? expected, out int comparison)
        {
            comparison = 0;
            if (!TryToDecimal(actual, out var a) || !TryToDecimal(expected, out var e))
                return false; // non-numeric ⇒ false, never throws
            comparison = a.CompareTo(e);
            return true;
        }

        private static bool IsBetween(object? actual, object? expected)
        {
            var range = ToScalarList(expected);
            if (range.Count != 2) return false;
            if (!TryToDecimal(actual, out var a)) return false;
            if (!TryToDecimal(range[0], out var min) || !TryToDecimal(range[1], out var max)) return false;
            return a >= min && a <= max;
        }

        private static bool IsInList(object? actual, object? expected)
        {
            var list = ToScalarList(expected);
            return list.Any(item => ValuesEqual(actual, item));
        }

        /// <summary>Actual must resolve to an enumerable collection (e.g. SocialAttributeId) —
        /// true if it contains at least one of expected's values.</summary>
        private static bool ContainsAny(object? actual, object? expected)
        {
            var actualList = ToScalarList(actual);
            var expectedList = ToScalarList(expected);
            if (actualList.Count == 0 || expectedList.Count == 0) return false;
            return expectedList.Any(e => actualList.Any(a => ValuesEqual(a, e)));
        }

        /// <summary>True if actual's collection contains every one of expected's values.</summary>
        private static bool ContainsAll(object? actual, object? expected)
        {
            var actualList = ToScalarList(actual);
            var expectedList = ToScalarList(expected);
            if (expectedList.Count == 0) return true;
            if (actualList.Count == 0) return false;
            return expectedList.All(e => actualList.Any(a => ValuesEqual(a, e)));
        }

        // ─── Value coercion helpers ─────────────────────────────────────────────────

        /// <summary>Unwraps a boxed <see cref="JsonElement"/> (System.Text.Json's default
        /// representation for an <c>object</c>-typed property) into a plain .NET value/list.
        /// Values that are already plain .NET types (from the field-flattener dictionary) pass
        /// through unchanged.</summary>
        private static object? UnwrapJson(object? val)
        {
            if (val is JsonElement je)
            {
                return je.ValueKind switch
                {
                    JsonValueKind.String => je.GetString(),
                    JsonValueKind.Number => je.TryGetInt64(out var l) ? (object)l : je.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    JsonValueKind.Array => je.EnumerateArray().Select(el => UnwrapJson(el)).ToList(),
                    _ => je.GetRawText(),
                };
            }
            return val;
        }

        /// <summary>Normalizes any scalar, JSON array, or .NET collection (but not a string) into
        /// a flat list of unwrapped scalar values — used for IN/NOT_IN/BETWEEN/CONTAINS_*.</summary>
        private static List<object?> ToScalarList(object? val)
        {
            var unwrapped = UnwrapJson(val);
            switch (unwrapped)
            {
                case null:
                    return new List<object?>();
                case string:
                    return new List<object?> { unwrapped };
                case IEnumerable enumerable:
                    var list = new List<object?>();
                    foreach (var item in enumerable) list.Add(UnwrapJson(item));
                    return list;
                default:
                    return new List<object?> { unwrapped };
            }
        }

        private static bool TryToDecimal(object? val, out decimal result)
        {
            result = 0;
            switch (UnwrapJson(val))
            {
                case null:
                    return false;
                case decimal d:
                    result = d; return true;
                case int i:
                    result = i; return true;
                case long l:
                    result = l; return true;
                case short s:
                    result = s; return true;
                // Guarded, not a bare cast: area fields are double? (PropertyDetailsEntity), and a
                // NaN/Infinity/out-of-range value would make (decimal)x throw OverflowException —
                // out of a method whose contract promises it never throws.
                case double db:
                    if (double.IsNaN(db) || double.IsInfinity(db) || db < (double)decimal.MinValue || db > (double)decimal.MaxValue)
                        return false;
                    result = (decimal)db; return true;
                case float f:
                    if (float.IsNaN(f) || float.IsInfinity(f) || f < (float)decimal.MinValue || f > (float)decimal.MaxValue)
                        return false;
                    result = (decimal)f; return true;
                case bool b:
                    result = b ? 1 : 0; return true;
                case string str:
                    return decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                default:
                    return false;
            }
        }

        private static string ToComparableString(object? val)
        {
            return UnwrapJson(val) switch
            {
                null => string.Empty,
                bool b => b.ToString(),
                var other => Convert.ToString(other, CultureInfo.InvariantCulture) ?? string.Empty,
            };
        }
    }
}
