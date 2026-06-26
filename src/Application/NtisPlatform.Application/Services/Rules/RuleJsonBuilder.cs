using System.Text.Json;
using System.Text.RegularExpressions;

namespace NtisPlatform.Application.Services.Rules
{
    /// <summary>
    /// Builds the MS Rules Engine-compatible ruleJson on the backend from the stored
    /// conditionsJson (ConditionGroupState) and effectJson (EffectState).
    ///
    /// This is the C# equivalent of the frontend's buildRuleJson() + buildGroupExpr() + buildActions().
    /// The frontend NO LONGER needs to send ruleJson — it only sends the visual state columns.
    /// </summary>
    public static class RuleJsonBuilder
    {
        // UI operator code → C# expression operator
        private static readonly Dictionary<string, string> OperatorMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // Standard / technical names
            ["EQUALS"] = "==",
            ["NOT_EQUALS"] = "!=",
            ["GREATER_THAN"] = ">",
            ["LESS_THAN"] = "<",
            ["GREATER_THAN_OR_EQUALS"] = ">=",
            ["LESS_THAN_OR_EQUALS"] = "<=",
            ["IN"] = "in",
            ["NOT_IN"] = "not in",
            ["CONTAINS"] = "in",
            ["CONTAINS_ANY"] = "in",
            ["CONTAINS_ALL"] = "in",

            // Literal operator mappings
            ["="] = "==",
            ["=="] = "==",
            ["!="] = "!=",
            [">"] = ">",
            ["<"] = "<",
            [">="] = ">=",
            ["<="] = "<=",

            // Human-readable master table names (normalized: spaces replaced with underscores)
            ["EQUAL_TO"] = "==",
            ["NOT_EQUAL_TO"] = "!=",
            ["GREATER_THAN_OR_EQUAL_TO"] = ">=",
            ["LESS_THAN_OR_EQUAL_TO"] = "<=",
            ["VALUE_EXISTS_IN_LIST"] = "in",
            ["VALUE_DOES_NOT_EXIST_IN_LIST"] = "not in",
            ["CONTAINS_ANY_MATCHING_VALUE"] = "in",
            ["CONTAINS_ALL_MATCHING_VALUES"] = "in",
            ["CONTAINS_ANY_MATCHING_VALUES"] = "in",
            ["CONTAINS_ALL_MATCHING_VALUE"] = "in",
        };

        /// <summary>
        /// Builds and returns the MS Rules Engine policy JSON string from the visual state columns.
        /// Called by RuleEngineService.CreateAsync / UpdateAsync to generate ruleJson on save.
        /// </summary>
        public static string Build(
            string ruleName,
            string ruleCode,
            bool isActive,
            string? ruleCategory,
            string? conditionsJson,
            string? effectJson,
            string? description = null)
        {
            var rulesList = new List<Dictionary<string, object?>>();
            bool isArray = false;

            if (!string.IsNullOrWhiteSpace(conditionsJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(conditionsJson);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        isArray = true;
                        foreach (var ruleElement in doc.RootElement.EnumerateArray())
                        {
                            var rCode = ruleElement.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? ruleCode : ruleCode;
                            var rDesc = ruleElement.TryGetProperty("description", out var descEl) ? descEl.GetString() : null;

                            string expr = string.Empty;
                            if (ruleElement.TryGetProperty("conditions", out var condsEl))
                            {
                                expr = BuildGroupExpr(condsEl);
                            }

                            object? actions = null;
                            if (ruleElement.TryGetProperty("effect", out var effEl))
                            {
                                actions = BuildActionsForElement(effEl);
                            }

                            var ruleEnabled = true;
                            if (ruleElement.TryGetProperty("enabled", out var enabledEl))
                            {
                                if (enabledEl.ValueKind == JsonValueKind.True) ruleEnabled = true;
                                else if (enabledEl.ValueKind == JsonValueKind.False) ruleEnabled = false;
                            }
                            else if (ruleElement.TryGetProperty("isEnabled", out var isEnabledEl))
                            {
                                if (isEnabledEl.ValueKind == JsonValueKind.True) ruleEnabled = true;
                                else if (isEnabledEl.ValueKind == JsonValueKind.False) ruleEnabled = false;
                            }

                            var ruleStopProcessing = false;
                            if (ruleElement.TryGetProperty("stopProcessing", out var stopEl))
                            {
                                if (stopEl.ValueKind == JsonValueKind.True) ruleStopProcessing = true;
                                else if (stopEl.ValueKind == JsonValueKind.False) ruleStopProcessing = false;
                            }
                            else if (ruleElement.TryGetProperty("StopProcessing", out var stopElCaps))
                            {
                                if (stopElCaps.ValueKind == JsonValueKind.True) ruleStopProcessing = true;
                                else if (stopElCaps.ValueKind == JsonValueKind.False) ruleStopProcessing = false;
                            }

                            var ruleObj = new Dictionary<string, object?>
                            {
                                ["RuleCode"] = rCode,
                                ["errorMessage"] = rDesc ?? $"Rule {ruleName} evaluation failed",
                                ["enabled"] = ruleEnabled,
                                ["ruleExpressionType"] = "LambdaExpression",
                                ["expression"] = expr,
                                ["Actions"] = actions,
                                ["stopProcessing"] = ruleStopProcessing,
                            };
                            rulesList.Add(ruleObj);
                        }
                    }
                }
                catch
                {
                    // Fallback to single rule logic if parsing fails
                    isArray = false;
                }
            }

            if (!isArray)
            {
                var expression = BuildExpression(conditionsJson);
                var actions = BuildActions(effectJson);

                var rule = new Dictionary<string, object?>
                {
                    ["RuleCode"] = ruleCode,
                    ["errorMessage"] = description ?? $"Rule {ruleName} evaluation failed",
                    ["enabled"] = isActive,
                    ["ruleExpressionType"] = "LambdaExpression",
                    ["expression"] = expression,
                    ["Actions"] = actions,
                };
                rulesList.Add(rule);
            }

            var policy = new Dictionary<string, object?>
            {
                ["RuleName"] = ruleName,
                ["isActive"] = isActive,
                ["RuleCategory"] = ruleCategory,
                ["rules"] = rulesList.ToArray(),
            };

            return JsonSerializer.Serialize(policy, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // Prevent escaping &&, ||, etc.
            });
        }

        // ─── Expression builder ────────────────────────────────────────────────────

        private static string BuildExpression(string? conditionsJson)
        {
            if (string.IsNullOrWhiteSpace(conditionsJson))
                return string.Empty; // No conditions saved yet

            try
            {
                var root = JsonSerializer.Deserialize<JsonElement>(conditionsJson);
                var expr = BuildGroupExpr(root);
                return string.IsNullOrWhiteSpace(expr) ? string.Empty : expr;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string BuildGroupExpr(JsonElement group)
        {
            var logicalOp = "&&"; // default AND
            if (group.TryGetProperty("logicalOperator", out var opEl))
                logicalOp = opEl.GetString()?.ToUpperInvariant() == "OR" ? "||" : "&&";

            var parts = new List<string>();

            // Flat conditions
            if (group.TryGetProperty("conditions", out var condsEl) && condsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var cond in condsEl.EnumerateArray())
                    parts.Add(BuildConditionExpr(cond));
            }

            // Nested groups
            if (group.TryGetProperty("groups", out var groupsEl) && groupsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var sub in groupsEl.EnumerateArray())
                {
                    var subExpr = BuildGroupExpr(sub);
                    if (!string.IsNullOrWhiteSpace(subExpr))
                        parts.Add($"({subExpr})");
                }
            }

            var nonEmpty = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            return nonEmpty.Count == 0 ? string.Empty : string.Join($" {logicalOp} ", nonEmpty);
        }

        private static string BuildConditionExpr(JsonElement cond)
        {
            var fieldId = cond.TryGetProperty("fieldId", out var f) ? f.GetString() ?? "" : "";
            var operCode = cond.TryGetProperty("operator", out var o) ? o.GetString() ?? "==" : "==";
            var valueEl = cond.TryGetProperty("value", out var v) ? v : default;

            var prop = ResolveInputProp(fieldId);

            // Normalize operator code: e.g. "Not In" -> "NOT_IN", "contains any" -> "CONTAINS_ANY"
            var normalizedOperCode = operCode.Trim().Replace(" ", "_").ToUpperInvariant();

            // Special handling for SocialAttributeId list collection
            if (fieldId.Equals("SocialAttributeId", StringComparison.OrdinalIgnoreCase))
            {
                if (valueEl.ValueKind == JsonValueKind.Array)
                {
                    var items = valueEl.EnumerateArray()
                        .Select(el => el.ValueKind == JsonValueKind.Number ? el.GetRawText() : el.GetString())
                        .Where(valStr => !string.IsNullOrEmpty(valStr) && int.TryParse(valStr, out _))
                        .Select(int.Parse)
                        .ToList();

                    if (items.Any())
                    {
                        var containsCalls = items.Select(item => $"input.SocialAttributeId.Contains({item})");
                        var joinOp = normalizedOperCode.Contains("ALL") ? " && " : " || ";
                        return $"({string.Join(joinOp, containsCalls)})";
                    }
                }
                else
                {
                    var valStr = valueEl.ValueKind == JsonValueKind.Number ? valueEl.GetRawText() : valueEl.GetString();
                    if (!string.IsNullOrEmpty(valStr) && int.TryParse(valStr, out int intVal))
                    {
                        return $"input.SocialAttributeId.Contains({intVal})";
                    }
                }
            }

            // Special handling for Value Between Range
            if (normalizedOperCode == "VALUE_BETWEEN_RANGE" || normalizedOperCode == "BETWEEN")
            {
                if (valueEl.ValueKind == JsonValueKind.Array && valueEl.GetArrayLength() == 2)
                {
                    var items = valueEl.EnumerateArray().ToList();
                    var minVal = FormatScalar(items[0]);
                    var maxVal = FormatScalar(items[1]);
                    return $"{prop} >= {minVal} && {prop} <= {maxVal}";
                }
            }

            // Special handling for IN / VALUE_EXISTS_IN_LIST
            if (normalizedOperCode == "IN" || normalizedOperCode == "VALUE_EXISTS_IN_LIST")
            {
                if (valueEl.ValueKind == JsonValueKind.Array)
                {
                    var items = valueEl.EnumerateArray().Select(FormatScalar).ToList();
                    if (items.Any())
                    {
                        var equalityExprs = items.Select(item => $"{prop} == {item}");
                        return $"({string.Join(" || ", equalityExprs)})";
                    }
                }
            }

            // Special handling for NOT_IN / VALUE_DOES_NOT_EXIST_IN_LIST
            if (normalizedOperCode == "NOT_IN" || normalizedOperCode == "VALUE_DOES_NOT_EXIST_IN_LIST")
            {
                if (valueEl.ValueKind == JsonValueKind.Array)
                {
                    var items = valueEl.EnumerateArray().Select(FormatScalar).ToList();
                    if (items.Any())
                    {
                        var inequalityExprs = items.Select(item => $"{prop} != {item}");
                        return $"({string.Join(" && ", inequalityExprs)})";
                    }
                }
            }

            var op = OperatorMap.TryGetValue(normalizedOperCode, out var mapped) ? mapped : "==";
            var val = FormatValue(valueEl);

            return $"{prop} {op} {val}";
        }

        /// <summary>
        /// Resolves the C# lambda property name for a field.
        /// fieldId from the frontend IS the property name (e.g. "Floor", "TypeOfUseGroup").
        /// Strips spaces and prefixes with "input.".
        /// </summary>
        private static string ResolveInputProp(string fieldId)
        {
            // fieldId from the frontend is already the property name (e.g. "Floor", "TypeOfUseGroup")
            // Just strip any spaces and prefix with "input."
            var propName = Regex.Replace(fieldId, @"\s+", "");
            return $"input.{propName}";
        }

        private static string FormatValue(JsonElement valueEl)
        {
            if (valueEl.ValueKind == JsonValueKind.Array)
            {
                var items = valueEl.EnumerateArray().Select(FormatScalar);
                return $"({string.Join(", ", items)})";
            }
            return FormatScalar(valueEl);
        }

        private static string FormatScalar(JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Number => el.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.String => double.TryParse(el.GetString(), out _)
                    ? el.GetString()!
                    : JsonSerializer.Serialize(el.GetString()), // Proper C# string escaping via JSON serialization
                _ => JsonSerializer.Serialize(el.GetRawText()),  // Proper C# string escaping via JSON serialization
            };
        }

        // ─── Actions builder ───────────────────────────────────────────────────────

        private static object? BuildActions(string? effectJson)
        {
            if (string.IsNullOrWhiteSpace(effectJson))
                return null;

            try
            {
                var effect = JsonSerializer.Deserialize<JsonElement>(effectJson);
                return BuildActionsForElement(effect);
            }
            catch
            {
                return null;
            }
        }

        private static object? BuildActionsForElement(JsonElement effect)
        {
            try
            {
                // ── Multi-effect: the "effect" field in ConditionsJson is a JSON array ────────
                // Emits: Actions.OnSuccess.Context = { "effects": [ {...}, {...} ] }
                // RuleExecutionService detects this and produces one RuleExecutionResultDto per entry.
                if (effect.ValueKind == JsonValueKind.Array)
                {
                    var effectsList = new List<Dictionary<string, string>>();
                    foreach (var item in effect.EnumerateArray())
                    {
                        var ctx = BuildSingleEffectContext(item);
                        if (ctx != null)
                            effectsList.Add(ctx);
                    }

                    if (!effectsList.Any())
                        return null;

                    return new
                    {
                        OnSuccess = new
                        {
                            Name = "MultiEffect",
                            Context = new Dictionary<string, object>
                            {
                                ["effects"] = effectsList
                            }
                        }
                    };
                }

                // ── Single effect: existing behavior ─────────────────────────────────────────
                var singleCtx = BuildSingleEffectContext(effect);
                if (singleCtx == null) return null;

                var actionName = (singleCtx.TryGetValue("effectType", out var et) ? et : string.Empty)
                    .Replace("%", "").Trim();

                return new
                {
                    OnSuccess = new
                    {
                        Name = actionName,
                        Context = singleCtx
                    }
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Builds a single effect context dictionary from a JSON element representing one effect.
        /// Returns a flat dictionary with keys: Expression, effectType, value, ParameterCode.
        /// Returns null if the element cannot be parsed.
        /// </summary>
        private static Dictionary<string, string>? BuildSingleEffectContext(JsonElement effect)
        {
            try
            {
                var effectType = effect.TryGetProperty("effectType", out var et) ? et.GetString() ?? "" : "";

                // Extract value without quotes: handle both JSON strings ("10") and numbers (10)
                var value = "0";
                if (effect.TryGetProperty("value", out var vv))
                {
                    value = vv.ValueKind switch
                    {
                        JsonValueKind.Number => vv.GetRawText(),           // 10 → "10"
                        JsonValueKind.String => vv.GetString() ?? "0",     // "10" → "10" (strips quotes)
                        _ => vv.GetRawText()
                    };
                }

                string? overrideRate = null;
                if (effect.TryGetProperty("overrideRate", out var or))
                {
                    overrideRate = or.ValueKind switch
                    {
                        JsonValueKind.String => or.GetString(),
                        JsonValueKind.Number => or.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        _ => null
                    };
                }

                // Resolve the C# parameter expression for the rate field
                // Ensure overrideRate is a valid C# property identifier (e.g. not a boolean/number like "1" or "true")
                var isPropertyIdentifier = !string.IsNullOrWhiteSpace(overrideRate) &&
                                           Regex.IsMatch(overrideRate, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
                var param = isPropertyIdentifier ? $"input.{overrideRate}" : "input.Rate";

                // Build expression from effectType
                var typeLower = effectType.ToLowerInvariant();
                string expression;
                if (typeLower.Contains("decrease"))
                    expression = $"{param} * (1 - {value} / 100)";
                else if (typeLower.Contains("increase"))
                    expression = $"{param} * (1 + {value} / 100)";
                else if (typeLower.Contains("multiply"))
                    expression = $"{param} * {value}";
                else if (typeLower.Contains("override") || typeLower.Contains("exempt"))
                    expression = value;
                else
                    expression = $"{param} * (1 - {value} / 100)"; // fallback

                return new Dictionary<string, string>
                {
                    ["Expression"] = expression,
                    ["effectType"] = effectType,
                    ["value"] = value,
                    ["ParameterCode"] = param,
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
