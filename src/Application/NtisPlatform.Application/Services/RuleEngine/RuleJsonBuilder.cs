using System.Text.Json;
using System.Text.RegularExpressions;

namespace NtisPlatform.Application.Services.RuleEngine
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
            ["EQUALS"] = "==",
            ["NOT_EQUALS"] = "!=",
            ["GREATER_THAN"] = ">",
            ["LESS_THAN"] = "<",
            ["GREATER_THAN_OR_EQUALS"] = ">=",
            ["LESS_THAN_OR_EQUALS"] = "<=",
            ["IN"] = "in",
            ["NOT_IN"] = "not in",
            ["CONTAINS_ANY"] = "contains",
            ["CONTAINS_ALL"] = "contains",
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

            var policy = new Dictionary<string, object?>
            {
                ["RuleName"] = ruleName,
                ["isActive"] = isActive,
                ["RuleCategory"] = ruleCategory,
                ["rules"] = new[] { rule },
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
            var op = OperatorMap.TryGetValue(operCode, out var mapped) ? mapped : "==";
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

                var overrideRate = effect.TryGetProperty("overrideRate", out var or) ? or.GetString() : null;

                // Resolve the C# parameter expression for the rate field
                var param = !string.IsNullOrWhiteSpace(overrideRate) ? $"input.{overrideRate}" : "input.Rate";

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

                var actionName = effectType.Replace("%", "").Trim();

                return new
                {
                    OnSuccess = new
                    {
                        Name = actionName,
                        Context = new Dictionary<string, string>
                        {
                            ["Expression"] = expression,
                            ["effectType"] = effectType,
                            ["value"] = value,
                            ["ParameterCode"] = param,
                        }
                    }
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
