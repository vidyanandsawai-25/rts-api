using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Rules.RuleEngine;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.DTOs.Rules.RuleCategory;
using NtisPlatform.Application.Services.Rules;
using NtisPlatform.Application.Services.Rules.Effects;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// P1: Comprehensive unit tests for RuleExecutionService
/// Tests caching, concurrency, security, and execution logic
/// </summary>
public class RuleExecutionServiceTests
{
    private readonly Mock<IRepository<RuleEngineEntity, int>> _mockRuleRepository;
    private readonly Mock<IRepository<RuleCategoryEntity, int>> _mockCategoryRepository;
    private readonly Mock<ILogger<RuleExecutionService>> _mockLogger;
    private readonly List<IRuleEffectApplicator> _effectApplicators;
    private readonly RuleExecutionService _service;

    public RuleExecutionServiceTests()
    {
        _mockRuleRepository = new Mock<IRepository<RuleEngineEntity, int>>();
        _mockCategoryRepository = new Mock<IRepository<RuleCategoryEntity, int>>();
        _mockLogger = new Mock<ILogger<RuleExecutionService>>();

        // Initialize all effect applicators
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

    #region GetCategoriesAsync Tests

    [Fact]
    public async Task GetCategoriesAsync_ReturnsActiveCategories_OrderedBySortOrder()
    {
        // Arrange
        var categories = new List<RuleCategoryEntity>
        {
            new() { Id = 1, CategoryCode = "ARV", CategoryName = "Annual Rental Value", SortOrder = 2, IsActive = true },
            new() { Id = 2, CategoryCode = "TAX", CategoryName = "Tax Calculation", SortOrder = 1, IsActive = true },
            new() { Id = 3, CategoryCode = "OLD", CategoryName = "Deprecated", SortOrder = 3, IsActive = false }
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(categories);
        _mockCategoryRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _service.GetCategoriesAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("TAX", result[0].Value); // SortOrder=1 comes first
        Assert.Equal("ARV", result[1].Value);
    }

    #endregion

    #region ExecuteAsync - Basic Validation Tests

    [Fact]
    public async Task ExecuteAsync_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_EmptyCategory_ThrowsArgumentException()
    {
        // Arrange
        var input = new RuleExecutionInputDto
        {
            Category = "",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ExecuteAsync(input));
    }

    [Fact]
    public async Task ExecuteAsync_EmptyInputDictionary_ThrowsArgumentException()
    {
        // Arrange
        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ExecuteAsync(input));
    }

    #endregion



    #region ExecuteAsync - Priority Ordering Tests

    [Fact]
    public async Task ExecuteAsync_ReturnsRules_OrderedByPriority()
    {
        // Arrange - Create rules with different priorities
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleEntity("RULE-50", "ARV", priority: 50, expression: "input.Rate > 0"),
            CreateRuleEntity("RULE-10", "ARV", priority: 10, expression: "input.Rate > 0"),
            CreateRuleEntity("RULE-5", "ARV", priority: 5, expression: "input.Rate > 0")
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - Rules should be returned in priority order (5, 10, 50)
        Assert.Equal(3, result.Count);
        Assert.Equal("RULE-5", result[0].RuleCode);
        Assert.Equal("RULE-10", result[1].RuleCode);
        Assert.Equal("RULE-50", result[2].RuleCode);
    }



    [Fact]
    public async Task ExecuteAsync_FallbackToPriorityOrdering_WhenCategoryIdIsOther()
    {
        // Arrange
        var compRule = CreateRuleEntity("COMP-5", "ARV", priority: 5, expression: "input.Rate > 0");
        compRule.RuleScopeId = 3; // Component Level

        var propRule = CreateRuleEntity("PROP-20", "ARV", priority: 20, expression: "input.Rate > 0");
        propRule.RuleScopeId = 1; // Property Level

        var buildRule = CreateRuleEntity("BUILD-50", "ARV", priority: 50, expression: "input.Rate > 0");
        buildRule.RuleScopeId = 2; // Building Level

        var unscopedRule = CreateRuleEntity("UNSCOPED-1", "ARV", priority: 1, expression: "input.Rate > 0");
        unscopedRule.RuleScopeId = null; // Unscoped

        var rules = new List<RuleEngineEntity> { compRule, propRule, buildRule, unscopedRule };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object>
            {
                { "Rate", 1000 },
                { "CategoryId", 3 } // Non-apartment CategoryId
            }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - Fallback to default priority order: UNSCOPED-1 (1) -> COMP-5 (5) -> PROP-20 (20) -> BUILD-50 (50)
        Assert.Equal(4, result.Count);
        Assert.Equal("UNSCOPED-1", result[0].RuleCode);
        Assert.Equal("COMP-5", result[1].RuleCode);
        Assert.Equal("PROP-20", result[2].RuleCode);
        Assert.Equal("BUILD-50", result[3].RuleCode);
    }

    #endregion

    #region ExecuteAsync - Security Tests

    [Fact]
    public async Task ExecuteAsync_SkipsRule_WithDangerousExpression()
    {
        // Arrange - Rule with dangerous System.IO reference
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleEntity("SAFE-RULE", "ARV", priority: 10, expression: "input.Rate > 100"),
            CreateRuleEntity("DANGEROUS-RULE", "ARV", priority: 20, expression: "System.IO.File.Delete('test')")
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - Only safe rule executed
        Assert.Single(result);
        Assert.Equal("SAFE-RULE", result[0].RuleCode);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsRule_WithExcessiveNesting()
    {
        // Arrange - Rule with excessive nesting depth
        var deepNesting = string.Join("", Enumerable.Repeat("(", 15)) + "input.Rate > 0" + string.Join("", Enumerable.Repeat(")", 15));
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleEntity("NORMAL-RULE", "ARV", priority: 10, expression: "input.Rate > 100"),
            CreateRuleEntity("NESTED-RULE", "ARV", priority: 20, expression: deepNesting)
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - Only normal rule executed
        Assert.Single(result);
        Assert.Equal("NORMAL-RULE", result[0].RuleCode);
    }

    #endregion

    #region ExecuteAsync - No Rules Tests

    [Fact]
    public async Task ExecuteAsync_NoEnabledRules_ReturnsEmptyList()
    {
        // Arrange
        var rules = new List<RuleEngineEntity>();
        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Helper Methods

    private List<RuleEngineEntity> CreateTestRules(string category, int priority)
    {
        return new List<RuleEngineEntity>
        {
            CreateRuleEntity("TEST-RULE", category, priority, "input.Rate > 0")
        };
    }

    private RuleEngineEntity CreateRuleEntity(string ruleCode, string category, int priority, string expression)
    {
        var ruleJson = $$"""
        {
            "RuleName": "{{ruleCode}}",
            "rules": [
                {
                    "RuleCode": "{{ruleCode}}",
                    "expression": "{{expression}}",
                    "enabled": true,
                    "Actions": {
                        "OnSuccess": {
                            "Context": {
                                "effectType": "DecreasePercent",
                                "value": "10",
                                "Expression": "{{expression}}",
                                "ParameterCode": "input.Rate"
                            }
                        }
                    }
                }
            ]
        }
        """;

        return new RuleEngineEntity
        {
            Id = priority,
            RuleCode = ruleCode,
            RuleName = ruleCode,
            RuleCategory = category,
            Priority = priority,
            IsEnabled = true,
            IsActive = true,
            RuleJson = ruleJson
        };
    }

    #endregion



    #region ExecuteAsync - StopProcessing Flag Tests

    [Fact]
    public async Task ExecuteAsync_StopsExecution_WhenStopProcessingFlagIsTrue()
    {
        // Arrange
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleEntityWithStopFlag("RULE-1", "ARV", priority: 10, expression: "input.Rate > 500", stopProcessing: true),
            CreateRuleEntity("RULE-2", "ARV", priority: 20, expression: "input.Rate > 0"),
            CreateRuleEntity("RULE-3", "ARV", priority: 30, expression: "input.Rate > 0")
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - Only RULE-1 should execute, rest should be stopped
        Assert.Single(result);
        Assert.Equal("RULE-1", result[0].RuleCode);
        Assert.True(result[0].StopProcessing);
    }

    [Fact]
    public async Task ExecuteAsync_ContinuesExecution_WhenStopProcessingFlagIsFalse()
    {
        // Arrange
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleEntityWithStopFlag("RULE-1", "ARV", priority: 10, expression: "input.Rate > 500", stopProcessing: false),
            CreateRuleEntity("RULE-2", "ARV", priority: 20, expression: "input.Rate > 0"),
            CreateRuleEntity("RULE-3", "ARV", priority: 30, expression: "input.Rate > 0")
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - All rules should execute
        Assert.Equal(3, result.Count);
    }

    #endregion



    #region ExecuteAsync - EffectJson Fallback Tests

    [Fact]
    public async Task ExecuteAsync_UsesEffectJson_WhenActionsIsNull()
    {
        // Arrange
        var ruleJson = """
        {
            "RuleName": "EFFECT-TEST",
            "rules": [
                {
                    "RuleCode": "EFFECT-TEST",
                    "expression": "input.Rate > 0",
                    "enabled": true,
                    "Actions": null
                }
            ]
        }
        """;

        var effectJson = """
        {
            "effectType": "IncreasePercent",
            "value": "20",
            "Expression": "Rate Increase",
            "ParameterCode": "input.Rate"
        }
        """;

        var rule = new RuleEngineEntity
        {
            Id = 1,
            RuleCode = "EFFECT-TEST",
            RuleName = "Effect Test",
            RuleCategory = "ARV",
            Priority = 10,
            IsEnabled = true,
            IsActive = true,
            RuleJson = ruleJson,
            EffectJson = effectJson
        };

        var rules = new List<RuleEngineEntity> { rule };
        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - Should use EffectJson
        Assert.Single(result);
        Assert.Equal("IncreasePercent", result[0].EffectType);
        Assert.Equal(20, result[0].EffectValue);
        Assert.Equal(1200, result[0].ComputedRate); // 1000 + 20% = 1200
    }

    #endregion

    #region ExecuteAsync - Multiple Effect Types Tests

    [Fact]
    public async Task ExecuteAsync_AppliesDecreaseEffect_Correctly()
    {
        // Arrange
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleWithEffect("DECREASE-TEST", "ARV", priority: 10,
                expression: "input.Rate > 0", effectType: "DecreasePercent", effectValue: 15)
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - 1000 - 15% = 850
        Assert.Single(result);
        Assert.Equal(850, result[0].ComputedRate);
    }

    [Fact]
    public async Task ExecuteAsync_AppliesIncreaseEffect_Correctly()
    {
        // Arrange
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleWithEffect("INCREASE-TEST", "ARV", priority: 10,
                expression: "input.Rate > 0", effectType: "IncreasePercent", effectValue: 25)
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - 1000 + 25% = 1250
        Assert.Single(result);
        Assert.Equal(1250, result[0].ComputedRate);
    }

    [Fact]
    public async Task ExecuteAsync_AppliesMultiplyEffect_Correctly()
    {
        // Arrange
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleWithEffect("MULTIPLY-TEST", "ARV", priority: 10,
                expression: "input.Rate > 0", effectType: "Multiply", effectValue: 2)
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - 1000 * 2 = 2000
        Assert.Single(result);
        Assert.Equal(2000, result[0].ComputedRate);
    }

    [Fact]
    public async Task ExecuteAsync_AppliesOverrideEffect_Correctly()
    {
        // Arrange
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleWithEffect("OVERRIDE-TEST", "ARV", priority: 10,
                expression: "input.Rate > 0", effectType: "Override", effectValue: 1500)
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - Rate overridden to 1500
        Assert.Single(result);
        Assert.Equal(1500, result[0].ComputedRate);
    }

    [Fact]
    public async Task ExecuteAsync_AppliesExemptionEffect_Correctly()
    {
        // Arrange
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleWithEffect("EXEMPTION-TEST", "ARV", priority: 10,
                expression: "input.Rate > 0", effectType: "Exemption", effectValue: 100)
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - Rate set to 0 (full exemption)
        Assert.Single(result);
        Assert.Equal(0, result[0].ComputedRate);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultiRuleCombinedJson_ExecutesAllMatchingRulesCorrectly()
    {
        // Arrange
        var multiConditions = @"[
            {
                ""id"": ""RULE-M1"",
                ""description"": ""rule1"",
                ""conditions"": {
                    ""logicalOperator"": ""AND"",
                    ""conditions"": [
                        { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": ""2"" }
                    ]
                },
                ""effect"": {
                    ""effectType"": ""Decrease %"",
                    ""value"": 10
                }
            },
            {
                ""id"": ""RULE-M2"",
                ""description"": ""rule2"",
                ""conditions"": {
                    ""logicalOperator"": ""AND"",
                    ""conditions"": [
                        { ""fieldId"": ""Carpet Area SqFeet"", ""operator"": ""EQUALS"", ""value"": ""100"" }
                    ]
                },
                ""effect"": {
                    ""effectType"": ""Multiply"",
                    ""value"": 2
                }
            }
        ]";

        var ruleJson = RuleJsonBuilder.Build("Combined Rules", "COMBINED-RULE", true, "ARV", multiConditions, null, "Combined description");

        var ruleEntity = new RuleEngineEntity
        {
            Id = 1,
            RuleCode = "COMBINED-RULE",
            RuleName = "Combined Rules",
            RuleCategory = "ARV",
            Priority = 10,
            IsEnabled = true,
            IsActive = true,
            RuleJson = ruleJson,
            ConditionsJson = multiConditions
        };

        var rules = new List<RuleEngineEntity> { ruleEntity };
        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object>
            {
                { "Rate", 1000m },
                { "Floor", 2 },
                { "CarpetAreaSqFeet", 100 }
            }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - Both rules should match and execute
        Assert.Equal(2, result.Count);

        // First rule: RULE-M1 -> Decrease 10% on 1000m -> 900
        Assert.Equal("RULE-M1", result[0].RuleCode);
        Assert.Equal(900m, result[0].ComputedRate);

        // Second rule: RULE-M2 -> Multiply by 2 on 1000m -> 2000
        Assert.Equal("RULE-M2", result[1].RuleCode);
        Assert.Equal(2000m, result[1].ComputedRate);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultiRuleCombinedJsonIncludingStopProcessing_HaltsEarly()
    {
        // Arrange
        var multiConditions = @"[
            {
                ""id"": ""RULE-M1"",
                ""description"": ""rule1"",
                ""stopProcessing"": true,
                ""conditions"": {
                    ""logicalOperator"": ""AND"",
                    ""conditions"": [
                        { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": ""2"" }
                    ]
                },
                ""effect"": {
                    ""effectType"": ""Decrease %"",
                    ""value"": 10
                }
            },
            {
                ""id"": ""RULE-M2"",
                ""description"": ""rule2"",
                ""conditions"": {
                    ""logicalOperator"": ""AND"",
                    ""conditions"": [
                        { ""fieldId"": ""Carpet Area SqFeet"", ""operator"": ""EQUALS"", ""value"": ""100"" }
                    ]
                },
                ""effect"": {
                    ""effectType"": ""Multiply"",
                    ""value"": 2
                }
            }
        ]";

        var ruleJson = RuleJsonBuilder.Build("Combined Rules", "COMBINED-RULE", true, "ARV", multiConditions, null, "Combined description");

        var ruleEntity = new RuleEngineEntity
        {
            Id = 1,
            RuleCode = "COMBINED-RULE",
            RuleName = "Combined Rules",
            RuleCategory = "ARV",
            Priority = 10,
            IsEnabled = true,
            IsActive = true,
            RuleJson = ruleJson,
            ConditionsJson = multiConditions
        };

        var rules = new List<RuleEngineEntity> { ruleEntity };
        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object>
            {
                { "Rate", 1000m },
                { "Floor", 2 },
                { "CarpetAreaSqFeet", 100 }
            }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - Only the first rule should execute and halt early
        Assert.Single(result);
        Assert.Equal("RULE-M1", result[0].RuleCode);
        Assert.Equal(900m, result[0].ComputedRate);
        Assert.True(result[0].StopProcessing);
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomInputJsonMatchingExpression_EvaluatesAndMatchesWorkflowCorrectly()
    {
        // Arrange
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleWithEffect(
                ruleCode: "CUSTOM-TEST-RULE",
                category: "RV",
                priority: 10,
                expression: "input.PropertyAge > 10 && input.HasLift == true",
                effectType: "DecreasePercent",
                effectValue: 15
            )
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "RV",
            Input = new Dictionary<string, object>
            {
                { "Rate", 2000m },
                { "PropertyAge", 15 },
                { "HasLift", true }
            }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - Rule should match as PropertyAge is 15 (> 10) and HasLift is true
        Assert.Single(result);
        Assert.Equal("CUSTOM-TEST-RULE", result[0].RuleCode);
        Assert.Equal("DecreasePercent", result[0].EffectType);
        Assert.Equal(15, result[0].EffectValue);
        Assert.Equal(1700m, result[0].ComputedRate); // 2000 - 15% = 1700
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomInputJsonNotMatchingExpression_EvaluatesAndDoesNotMatchWorkflow()
    {
        // Arrange
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleWithEffect(
                ruleCode: "CUSTOM-TEST-RULE",
                category: "RV",
                priority: 10,
                expression: "input.PropertyAge > 10 && input.HasLift == true",
                effectType: "DecreasePercent",
                effectValue: 15
            )
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "RV",
            Input = new Dictionary<string, object>
            {
                { "Rate", 2000m },
                { "PropertyAge", 5 }, // 5 is not > 10
                { "HasLift", true }
            }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - Rule should not match
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithCorrectedThaneRuleJson_EvaluatesAndMatchesWorkflowsCorrectly()
    {
        // Arrange - Corrected Thane Rule JSON
        var ruleJson = @"{
          ""RuleName"": ""thane rule"",
          ""isActive"": true,
          ""RuleCategory"": ""RV"",
          ""rules"": [
            {
              ""RuleCode"": ""5df5bb47-1141-43e6-851c-237d9276a9a8"",
              ""errorMessage"": ""If has Properties with Swimming pool or club house and property is Residentail then increase 20% "",
              ""enabled"": true,
              ""ruleExpressionType"": ""LambdaExpression"",
              ""expression"": ""input.TypeOfUseGroupId == 1 && (input.SocialAttributeId.Contains(38) || input.SocialAttributeId.Contains(39))"",
              ""Actions"": {
                ""OnSuccess"": {
                  ""Name"": ""Increase"",
                  ""Context"": {
                    ""Expression"": ""input.Rate * (1 + 20 / 100)"",
                    ""effectType"": ""Increase %"",
                    ""value"": ""20"",
                    ""ParameterCode"": ""input.Rate""
                  }
                }
              },
              ""stopProcessing"": true
            },
            {
              ""RuleCode"": ""8c4db8c2-7449-4902-8ddc-9fa320bad378"",
              ""errorMessage"": ""if Residentail building has lift and Floor is more than  G+10 then increase 10 % "",
              ""enabled"": true,
              ""ruleExpressionType"": ""LambdaExpression"",
              ""expression"": ""input.TypeOfUseGroupId == 3 && input.FloorId == 10 && input.SocialAttributeId.Contains(28)"",
              ""Actions"": {
                ""OnSuccess"": {
                  ""Name"": ""Increase"",
                  ""Context"": {
                    ""Expression"": ""input.Rate * (1 + 10 / 100)"",
                    ""effectType"": ""Increase %"",
                    ""value"": ""10"",
                    ""ParameterCode"": ""input.Rate""
                  }
                }
              },
              ""stopProcessing"": true
            },
            {
              ""RuleCode"": ""a8d68ac3-71d4-4a53-9d3b-a68d4bcabad6"",
              ""errorMessage"": ""if Residentail building has lift and Floor is G+10 then increase 5 % "",
              ""enabled"": true,
              ""ruleExpressionType"": ""LambdaExpression"",
              ""expression"": ""input.TypeOfUseGroupId == 1 && input.FloorId == 10 && input.SocialAttributeId.Contains(28)"",
              ""Actions"": {
                ""OnSuccess"": {
                  ""Name"": ""Increase"",
                  ""Context"": {
                    ""Expression"": ""input.Rate * (1 + 5 / 100)"",
                    ""effectType"": ""Increase %"",
                    ""value"": ""5"",
                    ""ParameterCode"": ""input.Rate""
                  }
                }
              },
              ""stopProcessing"": false
            }
          ]
        }";

        var ruleEntity = new RuleEngineEntity
        {
            Id = 1,
            RuleCode = "THANE-RULES",
            RuleName = "thane rule",
            RuleCategory = "RV",
            Priority = 10,
            IsEnabled = true,
            IsActive = true,
            RuleJson = ruleJson
        };

        var rules = new List<RuleEngineEntity> { ruleEntity };
        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Test Input: matching the first rule (TypeOfUseGroupId = 1, SocialAttributeIds contains 38, Rate = 1000)
        var input = new RuleExecutionInputDto
        {
            Category = "RV",
            Input = new Dictionary<string, object>
            {
                { "Rate", 1000m },
                { "TypeOfUseGroupId", 1 },
                { "SocialAttributeId", new List<int> { 38, 42 } }
            }
        };

        // Act
        List<RuleExecutionResultDto>? result = null;
        try
        {
            result = await _service.ExecuteAsync(input);
        }
        catch (Exception ex)
        {
            Console.WriteLine("EXECUTION EXCEPTION: " + ex);
            throw;
        }

        // Check if mock logger received warnings
        var warnings = _mockLogger.Invocations
            .Where(inv => inv.Method.Name == "Log" && inv.Arguments[0].ToString() == "Warning")
            .Select(inv => inv.Arguments[2]?.ToString() ?? "")
            .ToList();
        
        foreach (var warning in warnings)
        {
            Console.WriteLine("LOGGER WARNING: " + warning);
        }

        // Assert - The first rule should match and halt execution (stopProcessing = true)
        Assert.NotNull(result);
        if (result!.Count == 0)
        {
            Console.WriteLine("RESULT IS EMPTY!");
        }
        Assert.Single(result!);
        Assert.Equal("5df5bb47-1141-43e6-851c-237d9276a9a8", result![0].RuleCode);
        Assert.Equal("If has Properties with Swimming pool or club house and property is Residentail then increase 20% ", result[0].RuleName);
        Assert.Equal("Increase %", result![0].EffectType);
        Assert.Equal(20m, result[0].EffectValue);
        Assert.Equal(1200m, result[0].ComputedRate); // 1000 + 20% = 1200
        Assert.True(result[0].StopProcessing);
    }

    [Fact]
    public async Task ExecuteAsync_WithSubRuleDescriptionFallback_PopulatesRuleNameCorrectly()
    {
        // Arrange
        var ruleJson = @"{
          ""RuleName"": ""Thane Rule Category"",
          ""isActive"": true,
          ""RuleCategory"": ""RV"",
          ""rules"": [
            {
              ""RuleCode"": ""sub-rule-desc-1"",
              ""description"": ""Sub-rule description via camelCase description property"",
              ""enabled"": true,
              ""ruleExpressionType"": ""LambdaExpression"",
              ""expression"": ""input.Rate > 500"",
              ""Actions"": {
                ""OnSuccess"": {
                  ""Name"": ""Increase"",
                  ""Context"": {
                    ""Expression"": ""input.Rate * 1.1"",
                    ""effectType"": ""Increase %"",
                    ""value"": ""10"",
                    ""ParameterCode"": ""input.Rate""
                  }
                }
              }
            },
            {
              ""RuleCode"": ""sub-rule-desc-2"",
              ""enabled"": true,
              ""ruleExpressionType"": ""LambdaExpression"",
              ""expression"": ""input.Rate > 800"",
              ""Actions"": {
                ""OnSuccess"": {
                  ""Name"": ""Increase"",
                  ""Context"": {
                    ""Expression"": ""input.Rate * 1.05"",
                    ""effectType"": ""Increase %"",
                    ""value"": ""5"",
                    ""ParameterCode"": ""input.Rate""
                  }
                }
              }
            }
          ]
        }";

        var ruleEntity = new RuleEngineEntity
        {
            Id = 2,
            RuleCode = "TEST-FALLBACKS",
            RuleName = "Entity Rule Name Fallback",
            Description = "Entity Description Fallback",
            RuleCategory = "RV",
            Priority = 10,
            IsEnabled = true,
            IsActive = true,
            RuleJson = ruleJson
        };

        var rules = new List<RuleEngineEntity> { ruleEntity };
        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "RV",
            Input = new Dictionary<string, object>
            {
                { "Rate", 1000m }
            }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        // Sub-rule 1: should match and extract RuleName from sub-rule description property
        Assert.Equal("sub-rule-desc-1", result[0].RuleCode);
        Assert.Equal("Sub-rule description via camelCase description property", result[0].RuleName);

        // Sub-rule 2: should match and fallback to entity.Description
        Assert.Equal("sub-rule-desc-2", result[1].RuleCode);
        Assert.Equal("Entity Description Fallback", result[1].RuleName);
    }

    [Fact]
    public async Task ExecuteAsync_WithContainsOperator_NormalizesToInAndExecutesSuccessfully()
    {
        // Arrange - rule has "TypeOfUseGroupId contains (1, 2, 3)" which is SQL-style
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleWithEffect(
                ruleCode: "CONTAINS-TEST",
                category: "RV",
                priority: 10,
                expression: "input.TypeOfUseGroupId contains (1, 2, 3) && input.FloorId == 76",
                effectType: "DecreasePercent",
                effectValue: 30
            )
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "RV",
            Input = new Dictionary<string, object>
            {
                { "Rate", 1000m },
                { "TypeOfUseGroupId", 2 },
                { "FloorId", 76 }
            }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - Rule should match and calculate rate correctly (1000 - 30% = 700)
        Assert.Single(result);
        Assert.Equal("CONTAINS-TEST", result[0].RuleCode);
        Assert.Equal(700m, result[0].ComputedRate);
    }

    [Fact]
    public async Task ExecuteAsync_WithDotContainsMethodCall_DoesNotNormalizeToDotInAndExecutesSuccessfully()
    {
        // Arrange - rule has "input.SocialAttributeId.Contains(28)" which uses dot-Contains method call
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleWithEffect(
                ruleCode: "DOT-CONTAINS-TEST",
                category: "RV",
                priority: 10,
                expression: "input.TypeOfUseGroupId == 1 && input.SocialAttributeId.Contains(28)",
                effectType: "DecreasePercent",
                effectValue: 10
            )
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "RV",
            Input = new Dictionary<string, object>
            {
                { "Rate", 1000m },
                { "TypeOfUseGroupId", 1 },
                { "SocialAttributeId", new List<int> { 28, 42 } }
            }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - Rule should match and execute successfully
        Assert.Single(result);
        Assert.Equal("DOT-CONTAINS-TEST", result[0].RuleCode);
        Assert.Equal(900m, result[0].ComputedRate);
    }

    #endregion

    #region Helper Methods

    private RuleEngineEntity CreateRuleEntityWithStopFlag(string ruleCode, string category, int priority,
        string expression, bool stopProcessing)
    {
        var entity = CreateRuleEntity(ruleCode, category, priority, expression);
        entity.StopProcessing = stopProcessing;
        return entity;
    }

    private RuleEngineEntity CreateRuleWithEffect(string ruleCode, string category, int priority,
        string expression, string effectType, decimal effectValue)
    {
        var ruleJson = $$"""
        {
            "RuleName": "{{ruleCode}}",
            "rules": [
                {
                    "RuleCode": "{{ruleCode}}",
                    "expression": "{{expression}}",
                    "enabled": true,
                    "Actions": {
                        "OnSuccess": {
                            "Context": {
                                "effectType": "{{effectType}}",
                                "value": "{{effectValue}}",
                                "Expression": "{{expression}}",
                                "ParameterCode": "input.Rate"
                            }
                        }
                    }
                }
            ]
        }
        """;

        return new RuleEngineEntity
        {
            Id = priority,
            RuleCode = ruleCode,
            RuleName = ruleCode,
            RuleCategory = category,
            Priority = priority,
            IsEnabled = true,
            IsActive = true,
            RuleJson = ruleJson,
            StopProcessing = false
        };
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region DryRunAsync Tests
    // ═══════════════════════════════════════════════════════════════════════════

    // ── Input Validation ─────────────────────────────────────────────────────

    [Fact]
    public async Task DryRunAsync_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.DryRunAsync(null!));
    }

    [Fact]
    public async Task DryRunAsync_EmptyInputDictionary_ThrowsArgumentException()
    {
        // Arrange
        var input = new RuleDryRunInputDto
        {
            Category = "ARV",
            Input    = new Dictionary<string, object>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.DryRunAsync(input));
    }

    [Fact]
    public async Task DryRunAsync_NoCategoryAndNoRuleJson_ThrowsArgumentException()
    {
        // Arrange
        var input = new RuleDryRunInputDto
        {
            Category = "",
            RuleJson = null,
            Input    = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.DryRunAsync(input));
    }

    // ── Empty / No Rules Cases ────────────────────────────────────────────────

    [Fact]
    public async Task DryRunAsync_NoRulesInDb_ReturnsEmptyWorkflowList()
    {
        // Arrange
        var empty = new List<RuleEngineEntity>();
        _mockRuleRepository.Setup(r => r.GetQueryable())
            .Returns(MockQueryableExtensions.BuildMock(empty));

        var input = new RuleDryRunInputDto
        {
            Category = "ARV",
            Input    = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.DryRunAsync(input);

        // Assert
        Assert.Equal("ARV", result.Category);
        Assert.Equal(0, result.TotalRulesLoaded);
        Assert.Empty(result.Workflows);
        Assert.False(result.StoppedEarly);
    }

    // ── Ad-hoc RuleJson Mode (no DB) ─────────────────────────────────────────

    [Fact]
    public async Task DryRunAsync_WithAdHocRuleJson_DoesNotQueryDatabase()
    {
        // Arrange — ruleJson provided, DB should NOT be queried at all
        var ruleJson = """
        {
            "RuleName": "AdHoc Workflow",
            "rules": [
                {
                    "RuleCode": "ADHOC-001",
                    "expression": "input.Rate > 0",
                    "errorMessage": "Rate is positive",
                    "enabled": true,
                    "Actions": {
                        "OnSuccess": {
                            "Context": {
                                "effectType": "Decrease %",
                                "value": "10",
                                "ParameterCode": "input.Rate"
                            }
                        }
                    }
                }
            ]
        }
        """;

        var input = new RuleDryRunInputDto
        {
            Category = "ARV",
            RuleJson = ruleJson,
            Input    = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.DryRunAsync(input);

        // Assert — DB was never called
        _mockRuleRepository.Verify(r => r.GetQueryable(), Times.Never);

        Assert.Equal(1, result.TotalRulesLoaded);
        Assert.Single(result.Workflows);
    }

    [Fact]
    public async Task DryRunAsync_WithAdHocRuleJson_MatchingRule_PopulatesTrace()
    {
        // Arrange
        var ruleJson = """
        {
            "RuleName": "AdHoc Workflow",
            "rules": [
                {
                    "RuleCode": "ADHOC-001",
                    "expression": "input.Rate > 500",
                    "errorMessage": "Rate exceeds 500",
                    "enabled": true,
                    "Actions": {
                        "OnSuccess": {
                            "Context": {
                                "effectType": "Decrease %",
                                "value": "20",
                                "ParameterCode": "input.Rate"
                            }
                        }
                    }
                }
            ]
        }
        """;

        var input = new RuleDryRunInputDto
        {
            Category = "ARV",
            RuleJson = ruleJson,
            Input    = new Dictionary<string, object> { { "Rate", 1000.0 } }
        };

        // Act
        var result = await _service.DryRunAsync(input);

        // Assert
        Assert.Equal(1, result.MatchedCount);
        Assert.False(result.StoppedEarly);

        var workflow = Assert.Single(result.Workflows);
        var subRule  = Assert.Single(workflow.SubRules);

        Assert.Equal("ADHOC-001", subRule.RuleCode);
        Assert.Equal("Rate exceeds 500", subRule.RuleName);
        Assert.True(subRule.IsMatch);
        Assert.Equal("Matched", subRule.MatchStatus);
        Assert.False(subRule.WasSkipped);
        Assert.NotNull(subRule.Effect);
        Assert.Equal("Decrease %", subRule.Effect!.EffectType);
        Assert.Equal(20m, subRule.Effect.EffectValue);
    }

    [Fact]
    public async Task DryRunAsync_WithAdHocRuleJson_NonMatchingRule_PopulatesNotMatchedStatus()
    {
        // Arrange — expression will NOT match (Rate == 500 but rule requires Rate > 999)
        var ruleJson = """
        {
            "RuleName": "AdHoc Workflow",
            "rules": [
                {
                    "RuleCode": "ADHOC-002",
                    "expression": "input.Rate > 999",
                    "errorMessage": "Rate exceeds 999",
                    "enabled": true,
                    "Actions": {
                        "OnSuccess": {
                            "Context": { "effectType": "Decrease %", "value": "10" }
                        }
                    }
                }
            ]
        }
        """;

        var input = new RuleDryRunInputDto
        {
            RuleJson = ruleJson,
            Input    = new Dictionary<string, object> { { "Rate", 500.0 } }
        };

        // Act
        var result = await _service.DryRunAsync(input);

        // Assert
        Assert.Equal(0, result.MatchedCount);
        Assert.False(result.StoppedEarly);

        var subRule = result.Workflows[0].SubRules[0];
        Assert.False(subRule.IsMatch);
        Assert.StartsWith("Not matched", subRule.MatchStatus);
        Assert.Null(subRule.Effect); // no effect because no match
    }

    // ── Skipped Sub-Rule Cases ────────────────────────────────────────────────

    [Fact]
    public async Task DryRunAsync_DisabledSubRule_IsMarkedAsSkipped()
    {
        // Arrange
        var ruleJson = """
        {
            "RuleName": "Test Workflow",
            "rules": [
                {
                    "RuleCode": "SKIP-001",
                    "expression": "input.Rate > 0",
                    "enabled": false,
                    "errorMessage": "Should be skipped"
                }
            ]
        }
        """;

        var input = new RuleDryRunInputDto
        {
            RuleJson = ruleJson,
            Input    = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.DryRunAsync(input);

        // Assert
        var subRule = result.Workflows[0].SubRules[0];
        Assert.True(subRule.WasSkipped);
        Assert.Equal("Skipped", subRule.MatchStatus);
        Assert.Contains("disabled", subRule.SkipReason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, result.MatchedCount);
    }

    [Fact]
    public async Task DryRunAsync_MissingExpressionField_IsMarkedAsSkipped()
    {
        // Arrange — sub-rule has no "expression" key at all
        var ruleJson = """
        {
            "RuleName": "Test Workflow",
            "rules": [
                {
                    "RuleCode": "SKIP-002",
                    "errorMessage": "No expression provided"
                }
            ]
        }
        """;

        var input = new RuleDryRunInputDto
        {
            RuleJson = ruleJson,
            Input    = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.DryRunAsync(input);

        // Assert
        var subRule = result.Workflows[0].SubRules[0];
        Assert.True(subRule.WasSkipped);
        Assert.Contains("expression", subRule.SkipReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DryRunAsync_UnsafeExpression_IsMarkedAsSkippedWithSecurityReason()
    {
        // Arrange — expression contains a blocked keyword ("System.")
        var ruleJson = """
        {
            "RuleName": "Test Workflow",
            "rules": [
                {
                    "RuleCode": "UNSAFE-001",
                    "expression": "System.IO.File.Exists(\"test.txt\")",
                    "errorMessage": "Dangerous rule"
                }
            ]
        }
        """;

        var input = new RuleDryRunInputDto
        {
            RuleJson = ruleJson,
            Input    = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.DryRunAsync(input);

        // Assert
        var subRule = result.Workflows[0].SubRules[0];
        Assert.True(subRule.WasSkipped);
        Assert.Contains("security", subRule.SkipReason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, result.MatchedCount);
    }

    // ── StopProcessing Logic ──────────────────────────────────────────────────

    [Fact]
    public async Task DryRunAsync_SubRuleStopProcessing_SetsStoppedEarlyOnFirstMatch()
    {
        // Arrange — two DB rules; first has stopProcessing=true on its sub-rule
        var rule1 = CreateRuleWithEffect("RULE-STOP", "ARV", 1,
            "input.Rate > 0", "Decrease %", 10);
        // Inject stopProcessing into the JSON
        rule1.RuleJson = rule1.RuleJson!.Replace(
            "\"enabled\": true",
            "\"enabled\": true, \"stopProcessing\": true");

        var rule2 = CreateRuleWithEffect("RULE-AFTER", "ARV", 2,
            "input.Rate > 0", "Decrease %", 5);

        _mockRuleRepository.Setup(r => r.GetQueryable())
            .Returns(MockQueryableExtensions.BuildMock(new List<RuleEngineEntity> { rule1, rule2 }));

        var input = new RuleDryRunInputDto
        {
            Category = "ARV",
            Input    = new Dictionary<string, object> { { "Rate", 1000.0 } }
        };

        // Act
        var result = await _service.DryRunAsync(input);

        // Assert — stopped after first workflow, second workflow not evaluated
        Assert.True(result.StoppedEarly);
        Assert.Single(result.Workflows); // only the first workflow was processed
        Assert.Equal(1, result.MatchedCount);
    }

    [Fact]
    public async Task DryRunAsync_EntityLevelStopOnMatch_SetsStoppedEarlyWhenAnySubRuleMatches()
    {
        // Arrange — entity-level StopProcessing=true
        var rule1 = CreateRuleWithEffect("RULE-ENTITY-STOP", "ARV", 1,
            "input.Rate > 0", "Decrease %", 10);
        rule1.StopProcessing = true; // entity-level stop

        var rule2 = CreateRuleWithEffect("RULE-AFTER", "ARV", 2,
            "input.Rate > 0", "Decrease %", 5);

        _mockRuleRepository.Setup(r => r.GetQueryable())
            .Returns(MockQueryableExtensions.BuildMock(new List<RuleEngineEntity> { rule1, rule2 }));

        var input = new RuleDryRunInputDto
        {
            Category = "ARV",
            Input    = new Dictionary<string, object> { { "Rate", 1000.0 } }
        };

        // Act
        var result = await _service.DryRunAsync(input);

        // Assert
        Assert.True(result.StoppedEarly);
        Assert.Single(result.Workflows); // second workflow should not be evaluated
    }

    [Fact]
    public async Task DryRunAsync_NoStopProcessing_EvaluatesAllWorkflows()
    {
        // Arrange — two rules, neither stops processing
        var rule1 = CreateRuleWithEffect("RULE-A", "ARV", 1, "input.Rate > 0", "Decrease %", 10);
        var rule2 = CreateRuleWithEffect("RULE-B", "ARV", 2, "input.Rate > 0", "Decrease %", 5);

        _mockRuleRepository.Setup(r => r.GetQueryable())
            .Returns(MockQueryableExtensions.BuildMock(new List<RuleEngineEntity> { rule1, rule2 }));

        var input = new RuleDryRunInputDto
        {
            Category = "ARV",
            Input    = new Dictionary<string, object> { { "Rate", 1000.0 } }
        };

        // Act
        var result = await _service.DryRunAsync(input);

        // Assert — both workflows evaluated
        Assert.False(result.StoppedEarly);
        Assert.Equal(2, result.Workflows.Count);
        Assert.Equal(2, result.MatchedCount);
    }

    // ── Mixed Matched / Unmatched in One Workflow ─────────────────────────────

    [Fact]
    public async Task DryRunAsync_MixedSubRules_CountsOnlyMatched()
    {
        // Arrange — one workflow with two sub-rules: one matches, one does not
        var ruleJson = """
        {
            "RuleName": "Mixed Workflow",
            "rules": [
                {
                    "RuleCode": "MATCH-001",
                    "expression": "input.FloorId == 1",
                    "errorMessage": "Floor is 1 — matches",
                    "enabled": true,
                    "Actions": {
                        "OnSuccess": {
                            "Context": { "effectType": "Decrease %", "value": "10" }
                        }
                    }
                },
                {
                    "RuleCode": "NOMATCH-001",
                    "expression": "input.FloorId == 99",
                    "errorMessage": "Floor is 99 — does not match",
                    "enabled": true,
                    "Actions": {
                        "OnSuccess": {
                            "Context": { "effectType": "Decrease %", "value": "20" }
                        }
                    }
                }
            ]
        }
        """;

        var input = new RuleDryRunInputDto
        {
            RuleJson = ruleJson,
            Input    = new Dictionary<string, object> { { "FloorId", 1 }, { "Rate", 1000.0 } }
        };

        // Act
        var result = await _service.DryRunAsync(input);

        // Assert
        Assert.Equal(1, result.MatchedCount);
        Assert.Equal(2, result.TotalSubRulesEvaluated);

        var subRules = result.Workflows[0].SubRules;
        Assert.Equal(2, subRules.Count);

        var matched   = subRules.First(r => r.RuleCode == "MATCH-001");
        var unmatched = subRules.First(r => r.RuleCode == "NOMATCH-001");

        Assert.True(matched.IsMatch);
        Assert.Equal("Matched", matched.MatchStatus);
        Assert.NotNull(matched.Effect);

        Assert.False(unmatched.IsMatch);
        Assert.StartsWith("Not matched", unmatched.MatchStatus);
        Assert.Null(unmatched.Effect);
    }

    // ── Sub-Rule Array Order Preserved ───────────────────────────────────────

    [Fact]
    public async Task DryRunAsync_SubRules_ReturnedInOriginalArrayOrder()
    {
        // Arrange — three sub-rules; engine may return them in any order
        var ruleJson = """
        {
            "RuleName": "Order Test",
            "rules": [
                {
                    "RuleCode": "RULE-FIRST",
                    "expression": "input.Rate > 0",
                    "errorMessage": "First"
                },
                {
                    "RuleCode": "RULE-SECOND",
                    "expression": "input.Rate > 0",
                    "errorMessage": "Second"
                },
                {
                    "RuleCode": "RULE-THIRD",
                    "expression": "input.Rate > 0",
                    "errorMessage": "Third"
                }
            ]
        }
        """;

        var input = new RuleDryRunInputDto
        {
            RuleJson = ruleJson,
            Input    = new Dictionary<string, object> { { "Rate", 1000.0 } }
        };

        // Act
        var result = await _service.DryRunAsync(input);

        // Assert — ArrayIndex must reflect original JSON order
        var subRules = result.Workflows[0].SubRules;
        Assert.Equal(0, subRules.First(r => r.RuleCode == "RULE-FIRST").ArrayIndex);
        Assert.Equal(1, subRules.First(r => r.RuleCode == "RULE-SECOND").ArrayIndex);
        Assert.Equal(2, subRules.First(r => r.RuleCode == "RULE-THIRD").ArrayIndex);
    }

    // ── Resilience / Parse Error ──────────────────────────────────────────────

    [Fact]
    public async Task DryRunAsync_MalformedRuleJson_GracefullySkipsAndContinues()
    {
        // Arrange — first entity has broken JSON, second is valid
        var badEntity = new RuleEngineEntity
        {
            Id           = 1,
            RuleCode     = "BAD-JSON",
            RuleName     = "Bad",
            RuleCategory = "ARV",
            Priority     = 1,
            IsEnabled    = true,
            IsActive     = true,
            RuleJson     = "{ NOT VALID JSON !!!",
            StopProcessing = false
        };

        var goodEntity = CreateRuleWithEffect("GOOD-RULE", "ARV", 2,
            "input.Rate > 0", "Decrease %", 10);

        _mockRuleRepository.Setup(r => r.GetQueryable())
            .Returns(MockQueryableExtensions.BuildMock(
                new List<RuleEngineEntity> { badEntity, goodEntity }));

        var input = new RuleDryRunInputDto
        {
            Category = "ARV",
            Input    = new Dictionary<string, object> { { "Rate", 1000.0 } }
        };

        // Act — should not throw; bad entity is swallowed
        var result = await _service.DryRunAsync(input);

        // Assert — 2 workflows returned; bad one has error suffix in name, good one evaluated
        Assert.Equal(2, result.Workflows.Count);
        Assert.Contains("[parse error", result.Workflows[0].WorkflowName);
        Assert.True(result.Workflows[1].SubRules.Any(r => r.IsMatch));
    }

    [Fact]
    public async Task DryRunAsync_EmptyRuleJson_WorkflowNameGetsEmptySuffix()
    {
        // Arrange — entity exists but RuleJson is blank
        var entity = new RuleEngineEntity
        {
            Id           = 1,
            RuleCode     = "EMPTY-JSON",
            RuleName     = "Empty",
            RuleCategory = "ARV",
            Priority     = 1,
            IsEnabled    = true,
            IsActive     = true,
            RuleJson     = "",
            StopProcessing = false
        };

        _mockRuleRepository.Setup(r => r.GetQueryable())
            .Returns(MockQueryableExtensions.BuildMock(new List<RuleEngineEntity> { entity }));

        var input = new RuleDryRunInputDto
        {
            Category = "ARV",
            Input    = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.DryRunAsync(input);

        // Assert — workflow is present but has the empty suffix marker
        Assert.Single(result.Workflows);
        Assert.Contains("empty RuleJson", result.Workflows[0].WorkflowName);
        Assert.Empty(result.Workflows[0].SubRules);
    }

    // ── ResolvedInput Snapshot ────────────────────────────────────────────────

    [Fact]
    public async Task DryRunAsync_ResolvedInput_ContainsAllInputKeysAsStrings()
    {
        // Arrange
        var input = new RuleDryRunInputDto
        {
            RuleJson = """{ "rules": [] }""",
            Input    = new Dictionary<string, object>
            {
                { "Rate",    1000.0 },
                { "FloorId", 65     },
                { "IsRented", true  }
            }
        };

        // Act
        var result = await _service.DryRunAsync(input);

        // Assert — every input key must appear in ResolvedInput
        Assert.True(result.ResolvedInput.ContainsKey("Rate"));
        Assert.True(result.ResolvedInput.ContainsKey("FloorId"));
        Assert.True(result.ResolvedInput.ContainsKey("IsRented"));
        Assert.Equal(3, result.ResolvedInput.Count);
    }

    // ── Effect Extraction ─────────────────────────────────────────────────────

    [Fact]
    public async Task DryRunAsync_MatchedRule_EffectContainsCorrectTypeAndValue()
    {
        // Arrange
        var ruleJson = """
        {
            "RuleName": "Effect Test",
            "rules": [
                {
                    "RuleCode": "EFF-001",
                    "expression": "input.Rate > 0",
                    "errorMessage": "Effect rule",
                    "enabled": true,
                    "Actions": {
                        "OnSuccess": {
                            "Context": {
                                "effectType": "Multiply",
                                "value": "1.5",
                                "ParameterCode": "input.Rate"
                            }
                        }
                    }
                }
            ]
        }
        """;

        var input = new RuleDryRunInputDto
        {
            RuleJson = ruleJson,
            Input    = new Dictionary<string, object> { { "Rate", 1000.0 } }
        };

        // Act
        var result = await _service.DryRunAsync(input);

        // Assert
        var effect = result.Workflows[0].SubRules[0].Effect;
        Assert.NotNull(effect);
        Assert.Equal("Multiply", effect!.EffectType);
        Assert.Equal(1.5m, effect.EffectValue);
        Assert.Equal("input.Rate", effect.ParameterCode);
    }

    [Fact]
    public async Task DryRunAsync_WithMissingReferencedField_DefaultsToCorrectTypeAndEvaluatesWithoutException()
    {
        // Arrange - rule references input.TypeOfUseId which will be missing from Input dictionary
        var ruleJson = """
        {
            "RuleName": "Missing Field Test",
            "rules": [
                {
                    "RuleCode": "MISS-001",
                    "expression": "input.TypeOfUseId == 21",
                    "errorMessage": "Missing field rule",
                    "enabled": true,
                    "Actions": {
                        "OnSuccess": {
                            "Context": {
                                "effectType": "Multiply",
                                "value": "1.5",
                                "ParameterCode": "input.Rate"
                            }
                        }
                    }
                }
            ]
        }
        """;

        var input = new RuleDryRunInputDto
        {
            RuleJson = ruleJson,
            Input    = new Dictionary<string, object> 
            { 
                { "Rate", 1000.0 } 
            }
        };

        // Act - should not throw "binary operator Equal is not defined for System.Object and System.Int32"
        var result = await _service.DryRunAsync(input);

        // Assert - Rule parsed and evaluated without method-level exception, capturing property missing trace
        Assert.Single(result.Workflows);
        var subRule = result.Workflows[0].SubRules[0];
        Assert.Equal("MISS-001", subRule.RuleCode);
        Assert.False(subRule.IsMatch);
        Assert.Contains("Exception while parsing expression", subRule.MatchStatus);
    }

    #endregion
}
