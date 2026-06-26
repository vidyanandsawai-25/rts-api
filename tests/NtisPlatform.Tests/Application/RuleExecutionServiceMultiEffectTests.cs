using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.Services.Rules;
using NtisPlatform.Application.Services.Rules.Effects;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for RuleExecutionService covering the multi-effect enhancement.
///
/// Enhancement summary:
///   - RuleJsonBuilder now accepts effectJson as a JSON array → emits Context.effects[]
///   - BuildRuleResultAsync detects Context.effects[] → returns List&lt;RuleExecutionResultDto&gt;
///     with one DTO per effect instead of a single DTO
///   - ExtractDryRunEffects detects Context.effects[] → returns List&lt;RuleDryRunEffect&gt;
///   - RuleDryRunSubRuleResult.Effects replaces the single Effect property
///   - All single-effect rules continue to work unchanged (backward compat)
/// </summary>
public class RuleExecutionServiceMultiEffectTests
{
    private readonly Mock<IRepository<RuleEngineEntity, int>> _mockRuleRepository;
    private readonly Mock<IRepository<RuleCategoryEntity, int>> _mockCategoryRepository;
    private readonly Mock<ILogger<RuleExecutionService>> _mockLogger;
    private readonly List<IRuleEffectApplicator> _effectApplicators;
    private readonly RuleExecutionService _service;

    public RuleExecutionServiceMultiEffectTests()
    {
        _mockRuleRepository    = new Mock<IRepository<RuleEngineEntity, int>>();
        _mockCategoryRepository = new Mock<IRepository<RuleCategoryEntity, int>>();
        _mockLogger            = new Mock<ILogger<RuleExecutionService>>();

        _effectApplicators = new List<IRuleEffectApplicator>
        {
            new DecreasePercentApplicator(),
            new IncreasePercentApplicator(),
            new MultiplyApplicator(),
            new OverrideApplicator(),
            new ExemptionApplicator()
        };

        _service = new RuleExecutionService(
            _mockRuleRepository.Object,
            _mockCategoryRepository.Object,
            _effectApplicators,
            _mockLogger.Object);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a RuleEngineEntity with a sub-rule ConditionsJson array where
    /// each sub-rule has an "effect" ARRAY (multi-effect).
    /// </summary>
    private static RuleEngineEntity CreateMultiEffectSubRuleEntity(
        string ruleCode,
        string category,
        int priority,
        string expression,
        (string effectType, decimal value)[] effects)
    {
        // Build the effects array JSON
        var effectsJson = "[" + string.Join(",",
            effects.Select(e => $@"{{""effectType"":""{e.effectType}"",""value"":""{e.value}"",""overrideRate"":""Rate""}}"))
            + "]";

        var multiConditions = $@"[
            {{
                ""id"": ""{ruleCode}"",
                ""description"": ""{ruleCode}"",
                ""conditions"": {{
                    ""logicalOperator"": ""AND"",
                    ""conditions"": []
                }},
                ""effect"": {effectsJson}
            }}
        ]";

        // Use a hand-built ruleJson that injects the expression directly
        // (RuleJsonBuilder will build expression from conditions; we override here for test isolation)
        var ruleJson = RuleJsonBuilder.Build(ruleCode, ruleCode, true, category, multiConditions, null);

        return new RuleEngineEntity
        {
            Id           = 1,
            RuleCode     = ruleCode,
            RuleName     = ruleCode,
            RuleCategory = category,
            Priority     = priority,
            IsEnabled    = true,
            IsActive     = true,
            RuleJson     = ruleJson,
            ConditionsJson = multiConditions
        };
    }

    // ─── Execute: multi-effect returns multiple DTOs ──────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_MultiEffectRule_ReturnsOneDtoPerEffect()
    {
        // GIVEN a sub-rule with two effects: Increase 5% then Decrease 10%
        // WHEN the rule condition matches
        // THEN ExecuteAsync returns 2 RuleExecutionResultDto items (one per effect)

        var multiConditions = @"[
            {
                ""id"": ""RULE-ME"",
                ""description"": ""Multi-effect"",
                ""conditions"": {
                    ""logicalOperator"": ""AND"",
                    ""conditions"": [
                        { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": 1 }
                    ]
                },
                ""effect"": [
                    { ""effectType"": ""Increase %"", ""value"": ""5"",  ""overrideRate"": ""Rate"" },
                    { ""effectType"": ""Decrease %"", ""value"": ""10"", ""overrideRate"": ""Rate"" }
                ]
            }
        ]";

        var ruleJson = RuleJsonBuilder.Build("ME-Rule", "RULE-ME", true, "RV", multiConditions, null);
        var entity = new RuleEngineEntity
        {
            Id = 1, RuleCode = "RULE-ME", RuleName = "ME-Rule", RuleCategory = "RV",
            Priority = 10, IsEnabled = true, IsActive = true,
            RuleJson = ruleJson, ConditionsJson = multiConditions
        };

        _mockRuleRepository.Setup(r => r.GetQueryable())
                           .Returns(MockQueryableExtensions.BuildMock(new List<RuleEngineEntity> { entity }));

        var input = new RuleExecutionInputDto
        {
            Category = "RV",
            Input = new Dictionary<string, object> { { "Rate", 1000m }, { "Floor", 1 } }
        };

        // Act
        var results = await _service.ExecuteAsync(input);

        // Assert: two results, one per effect
        Assert.Equal(2, results.Count);

        Assert.Equal("Increase %", results[0].EffectType);
        Assert.Equal(5m,           results[0].EffectValue);
        Assert.Equal(1000m,        results[0].BaseRate);
        Assert.Equal(1050m,        results[0].ComputedRate);  // 1000 * 1.05

        Assert.Equal("Decrease %", results[1].EffectType);
        Assert.Equal(10m,          results[1].EffectValue);
        Assert.Equal(1000m,        results[1].BaseRate);
        Assert.Equal(900m,         results[1].ComputedRate);  // 1000 * 0.90
    }

    [Fact]
    public async Task ExecuteAsync_MultiEffectRule_AllResultsCarryTheSameRuleCode()
    {
        // GIVEN a multi-effect sub-rule
        // THEN all returned DTOs must carry the same RuleCode so callers can group them
        var multiConditions = @"[
            {
                ""id"": ""FLOOR-RULE"",
                ""description"": ""Floor rule"",
                ""conditions"": { ""logicalOperator"": ""AND"", ""conditions"": [
                    { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": 2 }
                ]},
                ""effect"": [
                    { ""effectType"": ""Increase %"", ""value"": ""5""  },
                    { ""effectType"": ""Decrease %"", ""value"": ""20"" }
                ]
            }
        ]";

        var ruleJson = RuleJsonBuilder.Build("Floor", "FLOOR-RULE", true, "RV", multiConditions, null);
        var entity = new RuleEngineEntity
        {
            Id = 1, RuleCode = "FLOOR-RULE", RuleName = "Floor", RuleCategory = "RV",
            Priority = 10, IsEnabled = true, IsActive = true,
            RuleJson = ruleJson, ConditionsJson = multiConditions
        };

        _mockRuleRepository.Setup(r => r.GetQueryable())
                           .Returns(MockQueryableExtensions.BuildMock(new List<RuleEngineEntity> { entity }));

        var results = await _service.ExecuteAsync(new RuleExecutionInputDto
        {
            Category = "RV",
            Input = new Dictionary<string, object> { { "Rate", 1000m }, { "Floor", 2 } }
        });

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("FLOOR-RULE", r.RuleCode));
    }

    [Fact]
    public async Task ExecuteAsync_MultiEffectRule_ConditionDoesNotMatch_ReturnsEmpty()
    {
        // GIVEN a multi-effect rule that checks Floor == 1
        // WHEN input has Floor = 5 (no match)
        // THEN result must be empty — no effects applied
        var multiConditions = @"[
            {
                ""id"": ""ME-NOMATCH"",
                ""description"": ""Floor 1 only"",
                ""conditions"": { ""logicalOperator"": ""AND"", ""conditions"": [
                    { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": 1 }
                ]},
                ""effect"": [
                    { ""effectType"": ""Increase %"", ""value"": ""5"" },
                    { ""effectType"": ""Decrease %"", ""value"": ""10"" }
                ]
            }
        ]";

        var ruleJson = RuleJsonBuilder.Build("ME-NoMatch", "ME-NOMATCH", true, "RV", multiConditions, null);
        var entity = new RuleEngineEntity
        {
            Id = 1, RuleCode = "ME-NOMATCH", RuleName = "ME-NoMatch", RuleCategory = "RV",
            Priority = 10, IsEnabled = true, IsActive = true,
            RuleJson = ruleJson, ConditionsJson = multiConditions
        };

        _mockRuleRepository.Setup(r => r.GetQueryable())
                           .Returns(MockQueryableExtensions.BuildMock(new List<RuleEngineEntity> { entity }));

        var results = await _service.ExecuteAsync(new RuleExecutionInputDto
        {
            Category = "RV",
            Input = new Dictionary<string, object> { { "Rate", 1000m }, { "Floor", 5 } }
        });

        Assert.Empty(results);
    }

    [Fact]
    public async Task ExecuteAsync_MultiEffectRule_WithStopProcessing_HaltsAfterAllEffectsOfMatchedRule()
    {
        // GIVEN a multi-effect sub-rule with stopProcessing = true
        //   AND a second sub-rule that would also match
        // THEN all effects of the first rule are returned
        //   AND the second sub-rule is NOT evaluated (stop fires after the full match)
        var multiConditions = @"[
            {
                ""id"": ""ME-STOP"",
                ""description"": ""Multi-effect with stop"",
                ""stopProcessing"": true,
                ""conditions"": { ""logicalOperator"": ""AND"", ""conditions"": [
                    { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": 1 }
                ]},
                ""effect"": [
                    { ""effectType"": ""Increase %"", ""value"": ""5"" },
                    { ""effectType"": ""Decrease %"", ""value"": ""10"" }
                ]
            },
            {
                ""id"": ""ME-SKIP"",
                ""description"": ""Should be skipped"",
                ""conditions"": { ""logicalOperator"": ""AND"", ""conditions"": [
                    { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": 1 }
                ]},
                ""effect"": { ""effectType"": ""Multiply"", ""value"": ""3"" }
            }
        ]";

        var ruleJson = RuleJsonBuilder.Build("Stop Rule", "ME-STOP", true, "RV", multiConditions, null);
        var entity = new RuleEngineEntity
        {
            Id = 1, RuleCode = "ME-STOP", RuleName = "Stop Rule", RuleCategory = "RV",
            Priority = 10, IsEnabled = true, IsActive = true,
            RuleJson = ruleJson, ConditionsJson = multiConditions
        };

        _mockRuleRepository.Setup(r => r.GetQueryable())
                           .Returns(MockQueryableExtensions.BuildMock(new List<RuleEngineEntity> { entity }));

        var results = await _service.ExecuteAsync(new RuleExecutionInputDto
        {
            Category = "RV",
            Input = new Dictionary<string, object> { { "Rate", 1000m }, { "Floor", 1 } }
        });

        // Only the two effects from ME-STOP, not Multiply from ME-SKIP
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("ME-STOP", r.RuleCode));
        Assert.True(results.All(r => r.StopProcessing));
    }

    // ─── Execute: backward compat — single-effect still returns 1 DTO ────────────────

    [Fact]
    public async Task ExecuteAsync_SingleEffectRule_StillReturnsExactlyOneDtoPerMatchedSubRule()
    {
        // GIVEN an existing single-effect rule (object, not array)
        // THEN behavior is unchanged: 1 DTO per matched sub-rule
        var multiConditions = @"[
            {
                ""id"": ""SE-COMPAT"",
                ""description"": ""Backward compat"",
                ""conditions"": { ""logicalOperator"": ""AND"", ""conditions"": [
                    { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": 2 }
                ]},
                ""effect"": { ""effectType"": ""Decrease %"", ""value"": 15 }
            }
        ]";

        var ruleJson = RuleJsonBuilder.Build("SE-Compat", "SE-COMPAT", true, "RV", multiConditions, null);
        var entity = new RuleEngineEntity
        {
            Id = 1, RuleCode = "SE-COMPAT", RuleName = "SE-Compat", RuleCategory = "RV",
            Priority = 10, IsEnabled = true, IsActive = true,
            RuleJson = ruleJson, ConditionsJson = multiConditions
        };

        _mockRuleRepository.Setup(r => r.GetQueryable())
                           .Returns(MockQueryableExtensions.BuildMock(new List<RuleEngineEntity> { entity }));

        var results = await _service.ExecuteAsync(new RuleExecutionInputDto
        {
            Category = "RV",
            Input = new Dictionary<string, object> { { "Rate", 1000m }, { "Floor", 2 } }
        });

        Assert.Single(results);
        Assert.Equal("Decrease %", results[0].EffectType);
        Assert.Equal(850m, results[0].ComputedRate);   // 1000 * 0.85
    }

    // ─── DryRun: Effects list ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DryRunAsync_MultiEffectRule_MatchedSubRuleHasEffectsListWithTwoEntries()
    {
        // GIVEN a multi-effect sub-rule that matches
        // THEN the dry-run trace RuleDryRunSubRuleResult.Effects has 2 RuleDryRunEffect entries
        //   AND the backward-compat Effect alias returns the first entry
        var multiConditions = @"[
            {
                ""id"": ""DR-ME"",
                ""description"": ""DryRun multi-effect"",
                ""conditions"": { ""logicalOperator"": ""AND"", ""conditions"": [
                    { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": 1 }
                ]},
                ""effect"": [
                    { ""effectType"": ""Increase %"", ""value"": ""5"",  ""overrideRate"": ""Rate"" },
                    { ""effectType"": ""Decrease %"", ""value"": ""10"", ""overrideRate"": ""Rate"" }
                ]
            }
        ]";

        var ruleJson = RuleJsonBuilder.Build("DR-ME", "DR-ME", true, "RV", multiConditions, null);
        var entity = new RuleEngineEntity
        {
            Id = 1, RuleCode = "DR-ME", RuleName = "DR-ME", RuleCategory = "RV",
            Priority = 10, IsEnabled = true, IsActive = true,
            RuleJson = ruleJson, ConditionsJson = multiConditions
        };

        _mockRuleRepository.Setup(r => r.GetQueryable())
                           .Returns(MockQueryableExtensions.BuildMock(new List<RuleEngineEntity> { entity }));

        var dryRunInput = new RuleDryRunInputDto
        {
            Category = "RV",
            BaseValue = 1000m,
            Input = new Dictionary<string, object> { { "Rate", 1000m }, { "Floor", 1 } }
        };

        // Act
        var dryRunResult = await _service.DryRunAsync(dryRunInput);

        // Assert: one workflow, one sub-rule matched
        Assert.NotNull(dryRunResult);
        Assert.Single(dryRunResult.Workflows);

        var workflow = dryRunResult.Workflows[0];
        var matchedSubRule = workflow.SubRules.SingleOrDefault(s => s.IsMatch);
        Assert.NotNull(matchedSubRule);

        // Effects list has 2 entries
        Assert.Equal(2, matchedSubRule.Effects.Count);
        Assert.Equal("Increase %", matchedSubRule.Effects[0].EffectType);
        Assert.Equal(5m,           matchedSubRule.Effects[0].EffectValue);
        Assert.Equal("Decrease %", matchedSubRule.Effects[1].EffectType);
        Assert.Equal(10m,          matchedSubRule.Effects[1].EffectValue);

        // Backward-compat: Effect (singular alias) returns first entry
        Assert.NotNull(matchedSubRule.Effect);
        Assert.Equal("Increase %", matchedSubRule.Effect!.EffectType);
    }

    [Fact]
    public async Task DryRunAsync_MultiEffectRule_ComputedValueIsAppliedAfterAllEffects()
    {
        // GIVEN +5% then -10%
        // THEN subTrace.ComputedValue = final value after last effect (from original base)
        //      Effects[0].ComputedValue = 1050 (1000 * 1.05)
        //      Effects[1].ComputedValue = 900  (1000 * 0.90)
        var multiConditions = @"[
            {
                ""id"": ""DR-CV"",
                ""description"": ""ComputedValue per effect"",
                ""conditions"": { ""logicalOperator"": ""AND"", ""conditions"": [
                    { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": 1 }
                ]},
                ""effect"": [
                    { ""effectType"": ""Increase %"", ""value"": ""5"",  ""overrideRate"": ""Rate"" },
                    { ""effectType"": ""Decrease %"", ""value"": ""10"", ""overrideRate"": ""Rate"" }
                ]
            }
        ]";

        var ruleJson = RuleJsonBuilder.Build("DR-CV", "DR-CV", true, "RV", multiConditions, null);
        var entity = new RuleEngineEntity
        {
            Id = 1, RuleCode = "DR-CV", RuleName = "DR-CV", RuleCategory = "RV",
            Priority = 10, IsEnabled = true, IsActive = true,
            RuleJson = ruleJson, ConditionsJson = multiConditions
        };

        _mockRuleRepository.Setup(r => r.GetQueryable())
                           .Returns(MockQueryableExtensions.BuildMock(new List<RuleEngineEntity> { entity }));

        var result = await _service.DryRunAsync(new RuleDryRunInputDto
        {
            Category = "RV",
            BaseValue = 1000m,
            Input = new Dictionary<string, object> { { "Rate", 1000m }, { "Floor", 1 } }
        });

        var sub = result.Workflows[0].SubRules.Single(s => s.IsMatch);

        // Each effect shows its own ComputedValue (from original base 1000)
        Assert.Equal(1050m, sub.Effects[0].ComputedValue);  // +5%
        Assert.Equal(900m,  sub.Effects[1].ComputedValue);  // -10%

        // SubTrace.ComputedValue = last effect's computed rate
        Assert.Equal(900m, sub.ComputedValue);
        Assert.Equal(1000m, sub.BaseRate);
    }

    [Fact]
    public async Task DryRunAsync_SingleEffectRule_EffectsListHasExactlyOneEntry_BackwardCompat()
    {
        // GIVEN a single-effect rule (old format)
        // THEN Effects list has exactly 1 entry; Effect alias also returns it
        var multiConditions = @"[
            {
                ""id"": ""DR-SE"",
                ""description"": ""Single"",
                ""conditions"": { ""logicalOperator"": ""AND"", ""conditions"": [
                    { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": 2 }
                ]},
                ""effect"": { ""effectType"": ""Decrease %"", ""value"": 15 }
            }
        ]";

        var ruleJson = RuleJsonBuilder.Build("DR-SE", "DR-SE", true, "RV", multiConditions, null);
        var entity = new RuleEngineEntity
        {
            Id = 1, RuleCode = "DR-SE", RuleName = "DR-SE", RuleCategory = "RV",
            Priority = 10, IsEnabled = true, IsActive = true,
            RuleJson = ruleJson, ConditionsJson = multiConditions
        };

        _mockRuleRepository.Setup(r => r.GetQueryable())
                           .Returns(MockQueryableExtensions.BuildMock(new List<RuleEngineEntity> { entity }));

        var result = await _service.DryRunAsync(new RuleDryRunInputDto
        {
            Category = "RV",
            BaseValue = 1000m,
            Input = new Dictionary<string, object> { { "Rate", 1000m }, { "Floor", 2 } }
        });

        var sub = result.Workflows[0].SubRules.Single(s => s.IsMatch);

        // Exactly 1 effect
        Assert.Single(sub.Effects);
        Assert.Equal("Decrease %", sub.Effects[0].EffectType);
        Assert.Equal(15m,          sub.Effects[0].EffectValue);

        // Backward-compat alias
        Assert.NotNull(sub.Effect);
        Assert.Equal("Decrease %", sub.Effect!.EffectType);
    }

    [Fact]
    public async Task DryRunAsync_NonMatchedRule_EffectsListIsEmpty()
    {
        // GIVEN a rule that does NOT match the input
        // THEN Effects list must be empty (not null)
        var multiConditions = @"[
            {
                ""id"": ""DR-NM"",
                ""description"": ""No match"",
                ""conditions"": { ""logicalOperator"": ""AND"", ""conditions"": [
                    { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": 99 }
                ]},
                ""effect"": [
                    { ""effectType"": ""Increase %"", ""value"": ""5"" },
                    { ""effectType"": ""Decrease %"", ""value"": ""10"" }
                ]
            }
        ]";

        var ruleJson = RuleJsonBuilder.Build("DR-NM", "DR-NM", true, "RV", multiConditions, null);
        var entity = new RuleEngineEntity
        {
            Id = 1, RuleCode = "DR-NM", RuleName = "DR-NM", RuleCategory = "RV",
            Priority = 10, IsEnabled = true, IsActive = true,
            RuleJson = ruleJson, ConditionsJson = multiConditions
        };

        _mockRuleRepository.Setup(r => r.GetQueryable())
                           .Returns(MockQueryableExtensions.BuildMock(new List<RuleEngineEntity> { entity }));

        var result = await _service.DryRunAsync(new RuleDryRunInputDto
        {
            Category = "RV",
            BaseValue = 1000m,
            Input = new Dictionary<string, object> { { "Rate", 1000m }, { "Floor", 1 } }
        });

        var sub = result.Workflows[0].SubRules.Single(s => !s.IsMatch);
        Assert.Empty(sub.Effects);
        Assert.Null(sub.Effect);   // alias returns null when list is empty
    }

    // ─── RuleDryRunResultDto shape tests ─────────────────────────────────────────────

    [Fact]
    public void RuleDryRunSubRuleResult_EffectAlias_ReturnsFirstEntry()
    {
        // GIVEN: Effects list with two entries
        // THEN: Effect computed property returns the first one
        var sub = new RuleDryRunSubRuleResult
        {
            Effects = new List<RuleDryRunEffect>
            {
                new() { EffectType = "Increase %", EffectValue = 5 },
                new() { EffectType = "Decrease %", EffectValue = 10 }
            }
        };

        Assert.NotNull(sub.Effect);
        Assert.Equal("Increase %", sub.Effect!.EffectType);
        Assert.Equal(5m,           sub.Effect.EffectValue);
    }

    [Fact]
    public void RuleDryRunSubRuleResult_EffectAlias_ReturnsNull_WhenEffectsEmpty()
    {
        var sub = new RuleDryRunSubRuleResult { Effects = new List<RuleDryRunEffect>() };
        Assert.Null(sub.Effect);
    }

    [Fact]
    public void RuleDryRunEffect_ComputedValue_IsNullByDefault()
    {
        var effect = new RuleDryRunEffect { EffectType = "Increase %", EffectValue = 5 };
        Assert.Null(effect.ComputedValue);
    }

    [Fact]
    public void RuleDryRunEffect_ComputedValue_CanBeSet()
    {
        var effect = new RuleDryRunEffect { EffectType = "Increase %", EffectValue = 5, ComputedValue = 1050m };
        Assert.Equal(1050m, effect.ComputedValue);
    }
}
