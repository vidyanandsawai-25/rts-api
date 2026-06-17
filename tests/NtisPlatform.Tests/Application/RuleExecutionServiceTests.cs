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
}
