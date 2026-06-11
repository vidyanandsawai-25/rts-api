using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Services.Rules;
using NtisPlatform.Application.Services.Rules.Effects;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application
{
    /// <summary>
    /// Unit tests for <see cref="RuleApplierService"/>.
    ///
    /// Strategy:
    /// — All DB dependencies are mocked.
    /// — Real <see cref="IRuleEffectApplicator"/> implementations are used to verify
    ///   that the applicator chain is wired and executed correctly.
    /// — <see cref="IRuleExecutionService"/> is mocked to control what rule results are returned.
    /// </summary>
    public class RuleApplierServiceTests
    {
        private readonly Mock<IRuleExecutionService> _ruleExecutionServiceMock;
        private readonly Mock<IRepository<RulesFieldEntity, int>> _rulesFieldRepoMock;
        private readonly List<IRuleEffectApplicator> _effectApplicators;
        private readonly RuleApplierService _service;

        public RuleApplierServiceTests()
        {
            _ruleExecutionServiceMock = new Mock<IRuleExecutionService>();

            // Real applicators — intentionally not mocked, to verify integration behaviour
            _effectApplicators = new List<IRuleEffectApplicator>
            {
                new DecreasePercentApplicator(),
                new IncreasePercentApplicator(),
                new MultiplyApplicator(),
                new OverrideApplicator(),
                new ExemptionApplicator()
            };

            // Default: return empty active fields (no DB-configured field overrides)
            _rulesFieldRepoMock = new Mock<IRepository<RulesFieldEntity, int>>();
            _rulesFieldRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<RulesFieldEntity>().BuildMockDbSet().Object);

            _service = new RuleApplierService(
                _ruleExecutionServiceMock.Object,
                _effectApplicators,
                _rulesFieldRepoMock.Object,
                NullLogger<RuleApplierService>.Instance);
        }

        // ─── Guard / Early-exit Tests ──────────────────────────────────────────────

        [Fact]
        public async Task ApplyRulesAsync_NullPropertyContext_ReturnsInitialValue()
        {
            // Arrange
            var context = new RuleApplierContext
            {
                Category = "RV",
                ValueKey = "Rate",
                InitialValue = 1000m,
                PropertyContext = null!
            };

            // Act
            var result = await _service.ApplyRulesAsync(context);

            // Assert
            Assert.Equal(1000m, result);
            _ruleExecutionServiceMock.Verify(
                x => x.ExecuteAsync(It.IsAny<RuleExecutionInputDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ApplyRulesAsync_NullDetailInParameters_ReturnsInitialValue()
        {
            // Arrange — context has no per-detail entities set (root context level)
            var context = CreateTestContext(
                detail: null!,
                detailTypeOfUse: null!,
                property: new PropertyEntity { Id = 1 });

            // Act
            var result = await _service.ApplyRulesAsync(context);

            // Assert
            Assert.Equal(1000m, result);
            _ruleExecutionServiceMock.Verify(
                x => x.ExecuteAsync(It.IsAny<RuleExecutionInputDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ApplyRulesAsync_InvalidFloorId_ReturnsInitialValue()
        {
            // Arrange
            var detail = new PropertyDetailsEntity { FloorId = 0, Id = 1 }; // FloorId = 0 is invalid
            var detailTypeOfUse = new TypeOfUseEntity { TypeOfUseGroupId = 5 };
            var property = new PropertyEntity { Id = 1 };

            var context = CreateTestContext(detail, detailTypeOfUse, property);

            // Act
            var result = await _service.ApplyRulesAsync(context);

            // Assert
            Assert.Equal(1000m, result);
            _ruleExecutionServiceMock.Verify(
                x => x.ExecuteAsync(It.IsAny<RuleExecutionInputDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ApplyRulesAsync_InvalidTypeOfUseGroupId_ReturnsInitialValue()
        {
            // Arrange
            var detail = new PropertyDetailsEntity { FloorId = 1, Id = 1 };
            var detailTypeOfUse = new TypeOfUseEntity { TypeOfUseGroupId = 0 }; // 0 is invalid
            var property = new PropertyEntity { Id = 1 };

            var context = CreateTestContext(detail, detailTypeOfUse, property);

            // Act
            var result = await _service.ApplyRulesAsync(context);

            // Assert
            Assert.Equal(1000m, result);
            _ruleExecutionServiceMock.Verify(
                x => x.ExecuteAsync(It.IsAny<RuleExecutionInputDto>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ─── Core Behaviour Tests ──────────────────────────────────────────────────

        [Fact]
        public async Task ApplyRulesAsync_NoRulesMatched_ReturnsInitialValue()
        {
            // Arrange
            var context = CreateTestContext(
                new PropertyDetailsEntity { FloorId = 1, Id = 1 },
                new TypeOfUseEntity { TypeOfUseGroupId = 2 },
                new PropertyEntity { Id = 1 });

            _ruleExecutionServiceMock
                .Setup(x => x.ExecuteAsync(It.IsAny<RuleExecutionInputDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<RuleExecutionResultDto>());

            // Act
            var result = await _service.ApplyRulesAsync(context);

            // Assert
            Assert.Equal(1000m, result);
            _ruleExecutionServiceMock.Verify(
                x => x.ExecuteAsync(It.IsAny<RuleExecutionInputDto>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ApplyRulesAsync_MultipleRules_AppliesEffectsSequentially()
        {
            // Arrange
            var context = CreateTestContext(
                new PropertyDetailsEntity { FloorId = 1, Id = 1 },
                new TypeOfUseEntity { TypeOfUseGroupId = 2 },
                new PropertyEntity { Id = 1 });

            // Rule chain: 1000 → -10% → 900 → +20% → 1080
            _ruleExecutionServiceMock
                .Setup(x => x.ExecuteAsync(It.IsAny<RuleExecutionInputDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<RuleExecutionResultDto>
                {
                    new() { RuleCode = "RULE-1", EffectType = "DecreasePercent", EffectValue = 10m },
                    new() { RuleCode = "RULE-2", EffectType = "IncreasePercent", EffectValue = 20m }
                });

            // Act
            var result = await _service.ApplyRulesAsync(context);

            // Assert
            Assert.Equal(1080m, result);
        }

        [Fact]
        public async Task ApplyRulesAsync_StopProcessingFlag_HaltsChainAfterCurrentRule()
        {
            // Arrange
            var context = CreateTestContext(
                new PropertyDetailsEntity { FloorId = 1, Id = 1 },
                new TypeOfUseEntity { TypeOfUseGroupId = 2 },
                new PropertyEntity { Id = 1 });

            // Rule chain: 1000 → -10% → 900 → Override(500) STOP → IncreasePercent skipped
            _ruleExecutionServiceMock
                .Setup(x => x.ExecuteAsync(It.IsAny<RuleExecutionInputDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<RuleExecutionResultDto>
                {
                    new() { RuleCode = "RULE-1", EffectType = "DecreasePercent", EffectValue = 10m },
                    new() { RuleCode = "RULE-2", EffectType = "Override",        EffectValue = 500m, StopProcessing = true },
                    new() { RuleCode = "RULE-3", EffectType = "IncreasePercent", EffectValue = 20m } // must be skipped
                });

            // Act
            var result = await _service.ApplyRulesAsync(context);

            // Assert
            Assert.Equal(500m, result);
        }

        [Fact]
        public async Task ApplyRulesAsync_UnknownEffectType_SkipsRuleAndContinues()
        {
            // Arrange
            var context = CreateTestContext(
                new PropertyDetailsEntity { FloorId = 1, Id = 1 },
                new TypeOfUseEntity { TypeOfUseGroupId = 2 },
                new PropertyEntity { Id = 1 });

            // First rule has unknown type (no applicator) — second should still run
            _ruleExecutionServiceMock
                .Setup(x => x.ExecuteAsync(It.IsAny<RuleExecutionInputDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<RuleExecutionResultDto>
                {
                    new() { RuleCode = "RULE-1", EffectType = "UnknownEffectType", EffectValue = 10m },
                    new() { RuleCode = "RULE-2", EffectType = "IncreasePercent",   EffectValue = 10m }  // 1000 → 1100
                });

            // Act
            var result = await _service.ApplyRulesAsync(context);

            // Assert
            Assert.Equal(1100m, result);
        }

        [Fact]
        public async Task ApplyRulesAsync_NonTransientException_Rethrows()
        {
            // Arrange
            var context = CreateTestContext(
                new PropertyDetailsEntity { FloorId = 1, Id = 1 },
                new TypeOfUseEntity { TypeOfUseGroupId = 2 },
                new PropertyEntity { Id = 1 });

            _ruleExecutionServiceMock
                .Setup(x => x.ExecuteAsync(It.IsAny<RuleExecutionInputDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Rules engine failure"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ApplyRulesAsync(context));
        }

        [Fact]
        public async Task ApplyRulesAsync_TransientTimeout_SucceedsAfterRetry()
        {
            // Arrange
            var context = CreateTestContext(
                new PropertyDetailsEntity { FloorId = 1, Id = 1 },
                new TypeOfUseEntity { TypeOfUseGroupId = 2 },
                new PropertyEntity { Id = 1 });

            var calls = 0;

            _ruleExecutionServiceMock
                .Setup(x => x.ExecuteAsync(It.IsAny<RuleExecutionInputDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    calls++;
                    if (calls < 2)
                        throw new TimeoutException("Transient timeout error");

                    return new List<RuleExecutionResultDto>
                    {
                        new() { RuleCode = "RULE-1", EffectType = "IncreasePercent", EffectValue = 10m }
                    };
                });

            // Act
            var result = await _service.ApplyRulesAsync(context, maxRetries: 3);

            // Assert: recovered on second attempt; value is 1000 + 10% = 1100
            Assert.Equal(1100m, result);
            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task ApplyRulesAsync_ActiveFieldsFromDb_AreResolvedIntoInputContext()
        {
            // Arrange — configure a DB field that maps "ConstructionTypeId" → "ConstType"
            var activeField = new RulesFieldEntity
            {
                FieldName = "ConstType",
                DatabaseColumnName = "ConstructionTypeId",
                IsActive = true
            };

            _rulesFieldRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<RulesFieldEntity> { activeField }.BuildMockDbSet().Object);

            RuleExecutionInputDto? capturedInput = null;

            _ruleExecutionServiceMock
                .Setup(x => x.ExecuteAsync(It.IsAny<RuleExecutionInputDto>(), It.IsAny<CancellationToken>()))
                .Callback<RuleExecutionInputDto, CancellationToken>((dto, _) => capturedInput = dto)
                .ReturnsAsync(new List<RuleExecutionResultDto>());

            var detail = new PropertyDetailsEntity
            {
                FloorId = 1,
                Id = 1,
                ConstructionTypeId = 7   // ← this should appear as "ConstType" = 7
            };
            var context = CreateTestContext(detail, new TypeOfUseEntity { TypeOfUseGroupId = 2 }, new PropertyEntity { Id = 1 });

            // Act
            await _service.ApplyRulesAsync(context);

            // Assert
            Assert.NotNull(capturedInput);
            Assert.True(capturedInput!.Input.ContainsKey("ConstType"),
                "Input context should contain the DB-configured field key 'ConstType'");
            Assert.Equal(7, capturedInput.Input["ConstType"]);
        }

        [Fact]
        public async Task ApplyRulesAsync_MapsAllPayloadAttributesCorrectly()
        {
            // Arrange
            // 1. Configure DB-configured rule field mapping "ConstructionTypeId" -> "ConstType"
            var activeField = new RulesFieldEntity
            {
                FieldName = "ConstType",
                DatabaseColumnName = "ConstructionTypeId",
                IsActive = true
            };

            _rulesFieldRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<RulesFieldEntity> { activeField }.BuildMockDbSet().Object);

            RuleExecutionInputDto? capturedInput = null;

            _ruleExecutionServiceMock
                .Setup(x => x.ExecuteAsync(It.IsAny<RuleExecutionInputDto>(), It.IsAny<CancellationToken>()))
                .Callback<RuleExecutionInputDto, CancellationToken>((dto, _) => capturedInput = dto)
                .ReturnsAsync(new List<RuleExecutionResultDto>());

            var property = new PropertyEntity
            {
                Id = 42,
                CategoryId = 6,
                WardId = 12,
                TaxZoneId = 3
            };

            var detail = new PropertyDetailsEntity
            {
                Id = 99,
                FloorId = 4,
                ConstructionTypeId = 5,
                CarpetAreaSqFeet = 250.0,
                BuiltupAreaSqFeet = 280.0,
                IsRenter = true
            };

            var detailTypeOfUse = new TypeOfUseEntity
            {
                TypeOfUseGroupId = 2
            };

            var assessment = new PropertyAssessmentEntity
            {
                OwnerTypeId = 10
            };

            var propertyContext = new PropertyCalculationContext
            {
                Property = property,
                PropertyAssessment = assessment,
                Parameters = new PropertyCalculationParameters
                {
                    FinanceYear = 2026,
                    ConstructionYearValue = 2020,
                    YearRangeRVId = 1,
                    SocialAttributeId = new List<int> { 101, 102, 103 },
                    Detail = detail,
                    DetailTypeOfUse = detailTypeOfUse
                }
            };
            propertyContext.Parameters.SocialAttributes.Add("HAS_SOLAR", true);
            propertyContext.Parameters.SocialAttributes.Add("NO_OF_WELL", 3);

            var context = new RuleApplierContext
            {
                Category = "RV",
                ValueKey = "Rate",
                InitialValue = 1000m,
                PropertyContext = propertyContext
            };

            // Act
            var result = await _service.ApplyRulesAsync(context);

            // Assert
            Assert.NotNull(capturedInput);
            Assert.Equal("RV", capturedInput.Category);

            var inputDict = capturedInput.Input;

            // Check derived properties
            Assert.Equal(6, inputDict["PropertyAge"]); // 2026 - 2020
            var socialAttributeIds = inputDict["SocialAttributeId"] as List<int>;
            Assert.NotNull(socialAttributeIds);
            Assert.Contains(101, socialAttributeIds);
            Assert.Contains(102, socialAttributeIds);
            Assert.Contains(103, socialAttributeIds);
            Assert.Equal(true, inputDict["Rented"]); // detail.IsRenter ?? false

            // Check flattened properties
            Assert.Equal(6, inputDict["CategoryId"]);
            Assert.Equal(12, inputDict["WardId"]);
            Assert.Equal(3, inputDict["TaxZoneId"]);
            Assert.Equal(4, inputDict["FloorId"]);
            Assert.Equal(5, inputDict["ConstructionTypeId"]);
            Assert.Equal(250.0, inputDict["CarpetAreaSqFeet"]);
            Assert.Equal(280.0, inputDict["BuiltupAreaSqFeet"]);
            Assert.Equal(2, inputDict["TypeOfUseGroupId"]);
            Assert.Equal(10, inputDict["OwnerTypeId"]);

            // Check dynamic social attributes
            Assert.Equal(true, inputDict["HAS_SOLAR"]);
            Assert.Equal(3, inputDict["NO_OF_WELL"]);

            // Check DB-configured fields mapping
            Assert.Equal(5, inputDict["ConstType"]); // constructionTypeId (5) is mapped to ConstType

            // Check InitialValue mapping
            Assert.Equal(1000.0, inputDict["Rate"]); // (double)context.InitialValue
        }

        // ─── Factory Helper ────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a <see cref="RuleApplierContext"/> with a fully populated,
        /// per-detail <see cref="PropertyCalculationContext"/> for test use.
        /// </summary>
        private static RuleApplierContext CreateTestContext(
            PropertyDetailsEntity detail,
            TypeOfUseEntity detailTypeOfUse,
            PropertyEntity property)
        {
            var propertyContext = new PropertyCalculationContext
            {
                Property = property,
                PropertyAssessment = null,
                Parameters = new PropertyCalculationParameters
                {
                    FinanceYear = 2026,
                    ConstructionYearValue = 2020,
                    YearRangeRVId = 1,
                    Detail = detail,
                    DetailTypeOfUse = detailTypeOfUse
                }
            };

            return new RuleApplierContext
            {
                Category = "RV",
                ValueKey = "Rate",
                InitialValue = 1000m,
                PropertyContext = propertyContext
            };
        }
    }
}
