using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RuleEngine;
using NtisPlatform.Application.Services.RuleEngine;
using NtisPlatform.Application.Services.RuleEngine.Effects;
using NtisPlatform.Core.Entities.Master;
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
    private readonly Mock<IRepository<RuleExclusionEntity, int>> _mockRuleExclusionRepository;
    private readonly Mock<ILogger<RuleExecutionService>> _mockLogger;
    private readonly IMemoryCache _memoryCache;
    private readonly List<IRuleEffectApplicator> _effectApplicators;
    private readonly RuleExecutionService _service;

    public RuleExecutionServiceTests()
    {
        _mockRuleRepository = new Mock<IRepository<RuleEngineEntity, int>>();
        _mockCategoryRepository = new Mock<IRepository<RuleCategoryEntity, int>>();
        _mockRuleExclusionRepository = new Mock<IRepository<RuleExclusionEntity, int>>();
        _mockLogger = new Mock<ILogger<RuleExecutionService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 100
        });

        // Setup empty exclusions by default
        var emptyExclusions = new List<RuleExclusionEntity>();
        var mockExclusionQueryable = MockQueryableExtensions.BuildMock(emptyExclusions);
        _mockRuleExclusionRepository.Setup(r => r.GetQueryable()).Returns(mockExclusionQueryable);

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
            _mockRuleExclusionRepository.Object,
            _effectApplicators,
            _mockLogger.Object,
            _memoryCache);
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

    #region ExecuteAsync - Caching Tests

    [Fact]
    public async Task ExecuteAsync_CachesRulesEngine_OnFirstCall()
    {
        // Arrange
        var rules = CreateTestRules("ARV", priority: 10);
        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act - First call
        await _service.ExecuteAsync(input);

        // Assert - Repository called once
        _mockRuleRepository.Verify(r => r.GetQueryable(), Times.Once);

        // Act - Second call
        await _service.ExecuteAsync(input);

        // Assert - Repository still called only once (cache hit)
        _mockRuleRepository.Verify(r => r.GetQueryable(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrentRequests_UseSameCache()
    {
        // Arrange
        var rules = CreateTestRules("ARV", priority: 10);
        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act - Warm the cache with a single call first
        await _service.ExecuteAsync(input);

        // Assert - Cache was populated (repository called once)
        _mockRuleRepository.Verify(r => r.GetQueryable(), Times.Once);

        // Act - Simulate concurrent requests (cache is already warm)
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _service.ExecuteAsync(input))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - Repository still called only once (all concurrent calls used cache)
        _mockRuleRepository.Verify(r => r.GetQueryable(), Times.Once);
    }

    #endregion

    #region ExecuteAsync - Cache Invalidation Tests

    [Fact]
    public async Task InvalidateCache_RemovesCategoryCache()
    {
        // Arrange
        var rules = CreateTestRules("ARV", priority: 10);
        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act - First call to populate cache
        await _service.ExecuteAsync(input);
        _mockRuleRepository.Verify(r => r.GetQueryable(), Times.Once);

        // Invalidate cache
        _service.InvalidateCache("ARV");

        // Act - Second call after invalidation
        await _service.ExecuteAsync(input);

        // Assert - Repository called again (cache miss)
        _mockRuleRepository.Verify(r => r.GetQueryable(), Times.Exactly(2));
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

    #region ExecuteAsync - Rule Exclusion Tests

    [Fact]
    public async Task ExecuteAsync_SkipsExcludedRule_WhenAppliedRuleMatches()
    {
        // Arrange
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleEntity("RULE-1", "ARV", priority: 10, expression: "input.Rate > 500"), // Will match
            CreateRuleEntity("RULE-2", "ARV", priority: 20, expression: "input.Rate > 0"),   // Should be skipped
            CreateRuleEntity("RULE-3", "ARV", priority: 30, expression: "input.Rate > 0")    // Should execute
        };

        var exclusions = new List<RuleExclusionEntity>
        {
            new() { Id = 1, AppliedRuleId = 10, SkipRuleId = 20, IsActive = true } // RULE-1 excludes RULE-2
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        var mockExclusionQueryable = MockQueryableExtensions.BuildMock(exclusions);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);
        _mockRuleExclusionRepository.Setup(r => r.GetQueryable()).Returns(mockExclusionQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - RULE-2 should be skipped
        Assert.Equal(2, result.Count);
        Assert.Equal("RULE-1", result[0].RuleCode);
        Assert.Equal("RULE-3", result[1].RuleCode);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleExclusions_SkipsAllExcludedRules()
    {
        // Arrange
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleEntity("RULE-1", "ARV", priority: 10, expression: "input.Rate > 500"),
            CreateRuleEntity("RULE-2", "ARV", priority: 20, expression: "input.Rate > 0"),
            CreateRuleEntity("RULE-3", "ARV", priority: 30, expression: "input.Rate > 0"),
            CreateRuleEntity("RULE-4", "ARV", priority: 40, expression: "input.Rate > 0")
        };

        var exclusions = new List<RuleExclusionEntity>
        {
            new() { Id = 1, AppliedRuleId = 10, SkipRuleId = 20, IsActive = true }, // RULE-1 excludes RULE-2
            new() { Id = 2, AppliedRuleId = 10, SkipRuleId = 30, IsActive = true }  // RULE-1 excludes RULE-3
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(rules);
        var mockExclusionQueryable = MockQueryableExtensions.BuildMock(exclusions);
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);
        _mockRuleExclusionRepository.Setup(r => r.GetQueryable()).Returns(mockExclusionQueryable);

        var input = new RuleExecutionInputDto
        {
            Category = "ARV",
            Input = new Dictionary<string, object> { { "Rate", 1000 } }
        };

        // Act
        var result = await _service.ExecuteAsync(input);

        // Assert - RULE-2 and RULE-3 should be skipped
        Assert.Equal(2, result.Count);
        Assert.Equal("RULE-1", result[0].RuleCode);
        Assert.Equal("RULE-4", result[1].RuleCode);
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

    #region ExecuteAsync - Expression Normalization Tests

    [Fact]
    public async Task ExecuteAsync_NormalizesANDOperator_ToCSharpSyntax()
    {
        // Arrange - SQL-style AND
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleEntity("AND-TEST", "ARV", priority: 10, expression: "input.Rate > 500 AND input.Rate < 2000")
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

        // Assert - Rule should match (normalized to &&)
        Assert.Single(result);
        Assert.Equal("AND-TEST", result[0].RuleCode);
    }

    [Fact]
    public async Task ExecuteAsync_NormalizesOROperator_ToCSharpSyntax()
    {
        // Arrange - SQL-style OR
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleEntity("OR-TEST", "ARV", priority: 10, expression: "input.Rate < 100 OR input.Rate > 900")
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

        // Assert - Rule should match (normalized to ||)
        Assert.Single(result);
        Assert.Equal("OR-TEST", result[0].RuleCode);
    }

    [Fact]
    public async Task ExecuteAsync_NormalizesNOTOperator_ToCSharpSyntax()
    {
        // Arrange - SQL-style NOT
        var rules = new List<RuleEngineEntity>
        {
            CreateRuleEntity("NOT-TEST", "ARV", priority: 10, expression: "NOT (input.Rate < 500)")
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

        // Assert - Rule should match (normalized to !)
        Assert.Single(result);
        Assert.Equal("NOT-TEST", result[0].RuleCode);
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
