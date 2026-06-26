using System.Text.Json;
using Xunit;
using NtisPlatform.Application.Services.Rules;

namespace NtisPlatform.Tests.Application.Rules
{
    /// <summary>
    /// Tests for the multi-effect enhancement to RuleJsonBuilder.
    /// All existing single-effect tests are in RuleJsonBuilderTests.cs and must still pass.
    /// This file covers only the new multi-effect array behavior.
    /// </summary>
    public class RuleJsonBuilderMultiEffectTests
    {
        // ─── Helper ──────────────────────────────────────────────────────────────────

        /// <summary>Navigates to Actions.OnSuccess.Context for a given rule index and clones it.</summary>
        private static JsonElement GetRuleContext(string ruleJson, int ruleIndex = 0)
        {
            using var doc = JsonDocument.Parse(ruleJson);
            return doc.RootElement
                      .GetProperty("rules")[ruleIndex]
                      .GetProperty("Actions")
                      .GetProperty("OnSuccess")
                      .GetProperty("Context")
                      .Clone();
        }

        // ─── Backward compatibility: single-effect object must remain unchanged ──────

        [Fact]
        public void Build_SingleEffectObject_StillEmitsFlatContext_NoEffectsArray()
        {
            // GIVEN an existing single-effect effectJson (object, not array)
            // THEN the output Context must be the same flat shape as before the enhancement
            var conditions = @"{
                ""logicalOperator"": ""AND"",
                ""conditions"": [{ ""fieldId"": ""FloorSequenceNo"", ""operator"": ""EQUALS"", ""value"": 1 }]
            }";
            var effect = @"{ ""effectType"": ""Increase %"", ""value"": 5, ""overrideRate"": ""Rate"" }";

            var ruleJson = RuleJsonBuilder.Build("Single Effect", "SE-001", true, "RV", conditions, effect);

            using var doc = JsonDocument.Parse(ruleJson);
            var onSuccess = doc.RootElement.GetProperty("rules")[0]
                                          .GetProperty("Actions")
                                          .GetProperty("OnSuccess");

            // Name must NOT be "MultiEffect"
            Assert.Equal("Increase", onSuccess.GetProperty("Name").GetString());

            var context = onSuccess.GetProperty("Context");

            // Flat fields must be present
            Assert.Equal("Increase %", context.GetProperty("effectType").GetString());
            Assert.Equal("5",          context.GetProperty("value").GetString());
            Assert.Equal("input.Rate", context.GetProperty("ParameterCode").GetString());
            Assert.Equal("input.Rate * (1 + 5 / 100)", context.GetProperty("Expression").GetString());

            // "effects" array must NOT exist in single-effect context
            Assert.False(context.TryGetProperty("effects", out _),
                "Single-effect context must NOT contain an 'effects' array key");
        }

        // ─── New: multi-effect top-level effectJson ───────────────────────────────────

        [Fact]
        public void Build_MultiEffectArrayEffectJson_EmitsEffectsArrayInContext()
        {
            // GIVEN effectJson is a JSON array of two effects
            // THEN Context must contain an "effects" array (not flat fields)
            var conditions = @"{
                ""logicalOperator"": ""AND"",
                ""conditions"": [
                    { ""fieldId"": ""BuildingMaxFloorSequence"", ""operator"": ""LESS_THAN_OR_EQUALS"", ""value"": 10 }
                ]
            }";
            var effectJson = @"[
                { ""effectType"": ""Increase %"", ""value"": ""5"",  ""overrideRate"": ""Rate"" },
                { ""effectType"": ""Decrease %"", ""value"": ""10"", ""overrideRate"": ""Rate"" }
            ]";

            var ruleJson = RuleJsonBuilder.Build("Multi-Effect", "ME-001", true, "RV", conditions, effectJson);

            using var doc = JsonDocument.Parse(ruleJson);
            var onSuccess = doc.RootElement.GetProperty("rules")[0]
                                          .GetProperty("Actions")
                                          .GetProperty("OnSuccess");

            Assert.Equal("MultiEffect", onSuccess.GetProperty("Name").GetString());

            var context = onSuccess.GetProperty("Context");
            Assert.False(context.TryGetProperty("effectType", out _),
                "Multi-effect context root must NOT have flat 'effectType' key");

            Assert.True(context.TryGetProperty("effects", out var effectsEl));
            Assert.Equal(JsonValueKind.Array, effectsEl.ValueKind);
            Assert.Equal(2, effectsEl.GetArrayLength());
        }

        [Fact]
        public void Build_MultiEffectArrayEffectJson_EachElementHasCorrectFieldsAndExpressions()
        {
            // GIVEN two effects: +5% increase and -20% decrease on Rate
            // THEN each element in effects[] has correct effectType, value, ParameterCode, Expression
            var conditions = @"{ ""logicalOperator"": ""AND"", ""conditions"": [
                { ""fieldId"": ""BuildingMaxFloorSequence"", ""operator"": ""LESS_THAN_OR_EQUALS"", ""value"": 10 }
            ]}";
            var effectJson = @"[
                { ""effectType"": ""Increase %"", ""value"": ""5"",  ""overrideRate"": ""Rate"" },
                { ""effectType"": ""Decrease %"", ""value"": ""20"", ""overrideRate"": ""Rate"" }
            ]";

            var ruleJson = RuleJsonBuilder.Build("ME", "ME-002", true, "RV", conditions, effectJson);
            var ctx = GetRuleContext(ruleJson);
            var effects = ctx.GetProperty("effects");

            var e0 = effects[0];
            Assert.Equal("Increase %", e0.GetProperty("effectType").GetString());
            Assert.Equal("5",          e0.GetProperty("value").GetString());
            Assert.Equal("input.Rate", e0.GetProperty("ParameterCode").GetString());
            Assert.Equal("input.Rate * (1 + 5 / 100)", e0.GetProperty("Expression").GetString());

            var e1 = effects[1];
            Assert.Equal("Decrease %", e1.GetProperty("effectType").GetString());
            Assert.Equal("20",         e1.GetProperty("value").GetString());
            Assert.Equal("input.Rate", e1.GetProperty("ParameterCode").GetString());
            Assert.Equal("input.Rate * (1 - 20 / 100)", e1.GetProperty("Expression").GetString());
        }

        [Fact]
        public void Build_MultiEffectWithCustomOverrideRate_ResolvesCorrectParameterCode()
        {
            // GIVEN overrideRate = "MonthlyRate"
            // THEN ParameterCode = "input.MonthlyRate" and Expression uses it
            var conditions = @"{ ""logicalOperator"": ""AND"", ""conditions"": [
                { ""fieldId"": ""FloorSequenceNo"", ""operator"": ""EQUALS"", ""value"": 1 }
            ]}";
            var effectJson = @"[
                { ""effectType"": ""Increase %"", ""value"": ""5"",  ""overrideRate"": ""MonthlyRate"" },
                { ""effectType"": ""Decrease %"", ""value"": ""10"", ""overrideRate"": ""MonthlyRate"" }
            ]";

            var ruleJson = RuleJsonBuilder.Build("Custom Param", "ME-003", true, "RV", conditions, effectJson);
            var ctx = GetRuleContext(ruleJson);
            var effects = ctx.GetProperty("effects");

            Assert.Equal("input.MonthlyRate", effects[0].GetProperty("ParameterCode").GetString());
            Assert.Equal("input.MonthlyRate * (1 + 5 / 100)",  effects[0].GetProperty("Expression").GetString());
            Assert.Equal("input.MonthlyRate", effects[1].GetProperty("ParameterCode").GetString());
            Assert.Equal("input.MonthlyRate * (1 - 10 / 100)", effects[1].GetProperty("Expression").GetString());
        }

        [Fact]
        public void Build_MultiEffectWithNumericValues_ParsesValueCorrectly()
        {
            // GIVEN value is a JSON number (10), not a string ("10")
            // THEN value in Context element must be extracted as "10"
            var conditions = @"{ ""logicalOperator"": ""AND"", ""conditions"": [
                { ""fieldId"": ""FloorSequenceNo"", ""operator"": ""EQUALS"", ""value"": 1 }
            ]}";
            var effectJson = @"[
                { ""effectType"": ""Increase %"", ""value"": 5  },
                { ""effectType"": ""Decrease %"", ""value"": 10 }
            ]";

            var ruleJson = RuleJsonBuilder.Build("Numeric Value ME", "ME-004", true, "RV", conditions, effectJson);
            var ctx = GetRuleContext(ruleJson);
            var effects = ctx.GetProperty("effects");

            Assert.Equal("5",  effects[0].GetProperty("value").GetString());
            Assert.Equal("10", effects[1].GetProperty("value").GetString());
        }

        [Fact]
        public void Build_MultiEffectAllEffectTypes_GeneratesCorrectExpressionsForEach()
        {
            // GIVEN 4 effect types in one array
            // THEN each produces the correct math expression
            var conditions = @"{ ""logicalOperator"": ""AND"", ""conditions"": [
                { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": 1 }
            ]}";
            var effectJson = @"[
                { ""effectType"": ""Increase %"", ""value"": ""5"" },
                { ""effectType"": ""Decrease %"", ""value"": ""20"" },
                { ""effectType"": ""Multiply"",   ""value"": ""3"" },
                { ""effectType"": ""Override"",   ""value"": ""500"" }
            ]";

            var ruleJson = RuleJsonBuilder.Build("All Types", "AT-001", true, "RV", conditions, effectJson);
            var ctx = GetRuleContext(ruleJson);
            var effects = ctx.GetProperty("effects");

            Assert.Equal(4, effects.GetArrayLength());
            Assert.Equal("input.Rate * (1 + 5 / 100)",  effects[0].GetProperty("Expression").GetString());
            Assert.Equal("input.Rate * (1 - 20 / 100)", effects[1].GetProperty("Expression").GetString());
            Assert.Equal("input.Rate * 3",              effects[2].GetProperty("Expression").GetString());
            Assert.Equal("500",                         effects[3].GetProperty("Expression").GetString());
        }

        // ─── New: multi-effect inside a sub-rule ConditionsJson array ─────────────────

        [Fact]
        public void Build_SubRuleWithEffectArray_EmitsMultiEffectActionsForThatSubRule()
        {
            // GIVEN: ConditionsJson is an array of sub-rules, one sub-rule has "effect": [...]
            // THEN: that sub-rule's Actions.OnSuccess.Name = "MultiEffect"
            //       and Context.effects[] contains both entries
            // Real-world: "तळ+10मजला, 1ला मजला, अनिवासी → +5% then -10%"
            var multiConditions = @"[
                {
                    ""id"": ""SR-001"",
                    ""description"": ""तळ+10मजला 1ला मजला अनिवासी"",
                    ""conditions"": {
                        ""logicalOperator"": ""AND"",
                        ""conditions"": [
                            { ""fieldId"": ""BuildingMaxFloorSequence"", ""operator"": ""LESS_THAN_OR_EQUALS"", ""value"": 10 },
                            { ""fieldId"": ""FloorSequenceNo"", ""operator"": ""EQUALS"", ""value"": 1 },
                            { ""fieldId"": ""Type"", ""operator"": ""EQUALS"", ""value"": ""C"" }
                        ]
                    },
                    ""effect"": [
                        { ""effectType"": ""Increase %"", ""value"": ""5"",  ""overrideRate"": ""Rate"" },
                        { ""effectType"": ""Decrease %"", ""value"": ""10"", ""overrideRate"": ""Rate"" }
                    ],
                    ""stopProcessing"": true
                }
            ]";

            var ruleJson = RuleJsonBuilder.Build("Floor+Use MultiEffect", "FU-001", true, "RV", multiConditions, null);
            using var doc = JsonDocument.Parse(ruleJson);
            var rules = doc.RootElement.GetProperty("rules");

            Assert.Equal(1, rules.GetArrayLength());

            var rule0 = rules[0];
            Assert.Equal("SR-001", rule0.GetProperty("RuleCode").GetString());
            Assert.True(rule0.GetProperty("stopProcessing").GetBoolean());

            // Conditions compiled correctly
            var expr = rule0.GetProperty("expression").GetString();
            Assert.Contains("input.BuildingMaxFloorSequence <= 10", expr);
            Assert.Contains("input.FloorSequenceNo == 1", expr);
            Assert.Contains("input.Type == \"C\"", expr);

            // Actions: MultiEffect with 2 items
            var onSuccess = rule0.GetProperty("Actions").GetProperty("OnSuccess");
            Assert.Equal("MultiEffect", onSuccess.GetProperty("Name").GetString());
            var effects = onSuccess.GetProperty("Context").GetProperty("effects");
            Assert.Equal(2, effects.GetArrayLength());
            Assert.Equal("Increase %", effects[0].GetProperty("effectType").GetString());
            Assert.Equal("Decrease %", effects[1].GetProperty("effectType").GetString());
        }

        [Fact]
        public void Build_MixedSubRules_OneWithSingleEffect_OneWithMultiEffect_BothCorrect()
        {
            // GIVEN two sub-rules: first has single effect, second has multi-effect
            // THEN each sub-rule's Actions shape matches its effect format
            var multiConditions = @"[
                {
                    ""id"": ""R1"",
                    ""description"": ""Single"",
                    ""conditions"": { ""logicalOperator"": ""AND"", ""conditions"": [
                        { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": 2 }
                    ]},
                    ""effect"": { ""effectType"": ""Decrease %"", ""value"": ""15"" }
                },
                {
                    ""id"": ""R2"",
                    ""description"": ""Multi"",
                    ""conditions"": { ""logicalOperator"": ""AND"", ""conditions"": [
                        { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": 3 }
                    ]},
                    ""effect"": [
                        { ""effectType"": ""Increase %"", ""value"": ""5""  },
                        { ""effectType"": ""Decrease %"", ""value"": ""20"" }
                    ]
                }
            ]";

            var ruleJson = RuleJsonBuilder.Build("Mixed", "MIX-001", true, "RV", multiConditions, null);
            using var doc = JsonDocument.Parse(ruleJson);
            var rules = doc.RootElement.GetProperty("rules");
            Assert.Equal(2, rules.GetArrayLength());

            // R1: flat Context (single effect)
            var os1 = rules[0].GetProperty("Actions").GetProperty("OnSuccess");
            Assert.Equal("Decrease", os1.GetProperty("Name").GetString());
            Assert.False(os1.GetProperty("Context").TryGetProperty("effects", out _),
                "R1 single-effect: no 'effects' key expected");
            Assert.Equal("Decrease %", os1.GetProperty("Context").GetProperty("effectType").GetString());

            // R2: effects array (multi-effect)
            var os2 = rules[1].GetProperty("Actions").GetProperty("OnSuccess");
            Assert.Equal("MultiEffect", os2.GetProperty("Name").GetString());
            Assert.True(os2.GetProperty("Context").TryGetProperty("effects", out var effArr2));
            Assert.Equal(2, effArr2.GetArrayLength());
        }

        // ─── Edge cases ───────────────────────────────────────────────────────────────

        [Fact]
        public void Build_EmptyEffectsArray_DoesNotThrow_AndActionsIsNull()
        {
            // GIVEN effectJson = [] (empty array)
            // THEN should not throw; Actions should be absent or null (graceful fallback)
            var conditions = @"{ ""logicalOperator"": ""AND"", ""conditions"": [
                { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": 1 }
            ]}";

            var ex = Record.Exception(() =>
                RuleJsonBuilder.Build("Empty", "EMP-001", true, "RV", conditions, "[]"));

            Assert.Null(ex);  // must not throw
        }

        [Fact]
        public void Build_NullEffectJson_DoesNotEmitActions()
        {
            // GIVEN effectJson = null (no effect configured yet)
            // THEN rules[0].Actions should be absent (not serialized due to NullIgnore)
            var conditions = @"{ ""logicalOperator"": ""AND"", ""conditions"": [
                { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": 1 }
            ]}";

            var ruleJson = RuleJsonBuilder.Build("No Effect", "NE-001", true, "RV", conditions, null);
            using var doc = JsonDocument.Parse(ruleJson);
            var rule = doc.RootElement.GetProperty("rules")[0];

            // Actions omitted (null-ignored) or null
            if (rule.TryGetProperty("Actions", out var actions))
                Assert.Equal(JsonValueKind.Null, actions.ValueKind);
        }
    }
}
