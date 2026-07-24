using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Application.Services.Rules;
using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Application
{
    /// <summary>
    /// Unit tests for <see cref="PropertyContextLoaderService"/>.
    ///
    /// Each test covers a distinct execution path through <c>LoadPropertyContextAsync</c>:
    /// — DB query composition and parallel fetch
    /// — All validation checks (property, details, construction year, year range)
    /// — The shape and values of the assembled <see cref="PropertyCalculationContext"/>
    /// </summary>
    public class PropertyContextLoaderServiceTests
    {
        // ─── Shared Mocks ─────────────────────────────────────────────────────────

        private readonly Mock<IRepository<PropertyEntity, int>> _propertyRepo;
        private readonly Mock<IRepository<PropertyDetailsEntity, int>> _propertyDetailsRepo;
        private readonly Mock<IRepository<PropertyAssessmentEntity, int>> _propertyAssessmentRepo;
        private readonly Mock<IRepository<PropertySocialDetailsEntity, int>> _propertySocialDetailsRepo;
        private readonly Mock<IRepository<RenterMastEntity, int>> _renterRepo;
        private readonly Mock<IRepository<PropertyOccupancyDetailsEntity, int>> _occupancyRepo;
        private readonly Mock<ITaxMasterDataService> _masterDataService;

        private readonly Mock<IFinanceYearProvider> _financeYearProvider;
        private readonly Mock<IRepository<YearMasterEntity, int>> _yearMasterRepo;
        private readonly Mock<IRVCalculationCleanupService> _rvCalculationCleanupService;
        private readonly Mock<IUnitOfWork> _unitOfWork;

        // Standard valid year range used across most tests
        private static readonly AssessmentYearRangeEntity DefaultYearRange = new()
        {
            Id = 10,
            FromYear = 2000,
            ToYear = 2030,
            IsActive = true
        };

        public PropertyContextLoaderServiceTests()
        {
            _propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
            _propertyDetailsRepo = new Mock<IRepository<PropertyDetailsEntity, int>>();
            _propertyAssessmentRepo = new Mock<IRepository<PropertyAssessmentEntity, int>>();
            _propertySocialDetailsRepo = new Mock<IRepository<PropertySocialDetailsEntity, int>>();
            _renterRepo = new Mock<IRepository<RenterMastEntity, int>>();
            _occupancyRepo = new Mock<IRepository<PropertyOccupancyDetailsEntity, int>>();

            _financeYearProvider = new Mock<IFinanceYearProvider>();
            _yearMasterRepo = new Mock<IRepository<YearMasterEntity, int>>();
            _rvCalculationCleanupService = new Mock<IRVCalculationCleanupService>();
            _unitOfWork = new Mock<IUnitOfWork>();

            _masterDataService = new Mock<ITaxMasterDataService>();

            // Default: all repos return empty async-capable mocks.
            _propertyRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyEntity>().BuildMockDbSet().Object);

            _propertyDetailsRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyDetailsEntity>().BuildMockDbSet().Object);

            _propertyAssessmentRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyAssessmentEntity>().BuildMockDbSet().Object);

            _propertySocialDetailsRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertySocialDetailsEntity>().BuildMockDbSet().Object);

            _renterRepo.Setup(r => r.GetQueryable())
                .Returns(new List<RenterMastEntity>().BuildMockDbSet().Object);

            _occupancyRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyOccupancyDetailsEntity>().BuildMockDbSet().Object);

            _financeYearProvider.Setup(x => x.GetCurrentFinanceYear())
                .Returns(2026);

            _yearMasterRepo.Setup(r => r.GetQueryable())
                .Returns(new List<YearMasterEntity>().BuildMockDbSet().Object);

            _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        // ─── Failure / Validation Tests ──────────────────────────────────────────

        [Fact]
        public async Task LoadPropertyContextAsync_PropertyNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            _propertyRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyEntity>().BuildMockDbSet().Object);

            var sut = CreateService();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.LoadPropertyContextAsync(99, 2026));

            Assert.Contains("Property not found", ex.Message);
            Assert.Contains("99", ex.Message);
        }

        [Fact]
        public async Task LoadPropertyContextAsync_NoPropertyDetails_DeactivatesExistingRVCalculationsAndReturnsEmptyContext()
        {
            // Arrange
            const int propertyId = 1;
            const int financeYear = 2026;
            const int yearMasterId = 7;

            SetupValidProperty(propertyId);

            _propertyDetailsRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyDetailsEntity>().BuildMockDbSet().Object);

            _financeYearProvider.Setup(x => x.GetCurrentFinanceYear())
                .Returns(financeYear);

            _yearMasterRepo.Setup(r => r.GetQueryable())
                .Returns(new List<YearMasterEntity>
                {
                    new()
                    {
                        Id = yearMasterId,
                        Year = financeYear,
                        IsActive = true
                    }
                }.BuildMockDbSet().Object);

            var sut = CreateService();

            // Act
            var ctx = await sut.LoadPropertyContextAsync(propertyId, financeYear);

            // Assert — context signals empty details so callers can return a zero result
            Assert.NotNull(ctx);
            Assert.Empty(ctx.Details);
            Assert.Equal(financeYear, ctx.Parameters.FinanceYear);

            _rvCalculationCleanupService.Verify(
                x => x.DeactivateExistingRVCalculationsAsync(
                    propertyId,
                    financeYear,
                    yearMasterId),
                Times.Once);

            _unitOfWork.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task LoadPropertyContextAsync_ConstructionYearIsNull_ThrowsInvalidOperationException()
        {
            // Arrange
            SetupValidProperty(propertyId: 1);
            SetupPropertyDetails(propertyId: 1, constructionYear: null);

            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            var sut = CreateService();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.LoadPropertyContextAsync(1, 2026));

            Assert.Contains("ConstructionYear not found", ex.Message);
        }

        [Fact]
        public async Task LoadPropertyContextAsync_ConstructionYearIsNotNumeric_ThrowsInvalidOperationException()
        {
            // Arrange
            SetupValidProperty(propertyId: 1);
            SetupPropertyDetails(propertyId: 1, constructionYear: "INVALID");

            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            var sut = CreateService();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.LoadPropertyContextAsync(1, 2026));

            Assert.Contains("Invalid ConstructionYear", ex.Message);
            Assert.Contains("INVALID", ex.Message);
        }

        [Fact]
        public async Task LoadPropertyContextAsync_NoMatchingYearRange_ThrowsInvalidOperationException()
        {
            // Arrange — construction year 1850 is outside DefaultYearRange (2000-2030)
            SetupValidProperty(propertyId: 1);
            SetupPropertyDetails(propertyId: 1, constructionYear: "1850");

            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            var sut = CreateService();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.LoadPropertyContextAsync(1, 2026));

            Assert.Contains("Assessment year range not found", ex.Message);
            Assert.Contains("1850", ex.Message);
        }

        // ─── Happy Path Tests ────────────────────────────────────────────────────

        [Fact]
        public async Task LoadPropertyContextAsync_ValidInput_ReturnsPopulatedContext()
        {
            // Arrange
            const int propertyId = 1;
            const int financeYear = 2026;
            const int constructionYearValue = 2015;

            SetupValidProperty(propertyId);
            SetupPropertyDetails(propertyId, constructionYear: constructionYearValue.ToString());

            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            var sut = CreateService();

            // Act
            var ctx = await sut.LoadPropertyContextAsync(propertyId, financeYear);

            // Assert — entity aggregates
            Assert.NotNull(ctx);
            Assert.NotNull(ctx.Property);
            Assert.Equal(propertyId, ctx.Property.Id);
            Assert.NotEmpty(ctx.Details);

            // Assert — typed parameters
            Assert.Equal(financeYear, ctx.Parameters.FinanceYear);
            Assert.Equal(constructionYearValue, ctx.Parameters.ConstructionYearValue);
            Assert.Equal(DefaultYearRange.Id, ctx.Parameters.YearRangeRVId);

            // Per-detail fields are null at root context level
            Assert.Null(ctx.Parameters.Detail);
            Assert.Null(ctx.Parameters.DetailTypeOfUse);
        }

        [Fact]
        public async Task LoadPropertyContextAsync_ValidInput_CalculatesBuildingMaxFloorParametersCorrectly()
        {
            // Arrange
            const int propertyId = 1;
            SetupValidProperty(propertyId);

            var floor10 = new FloorEntity { FloorCode = "10", SequenceNo = 21 };
            var floor2 = new FloorEntity { FloorCode = "2", SequenceNo = 13 };

            _propertyDetailsRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyDetailsEntity>
                {
                    new()
                    {
                        Id = 1,
                        PropertyId = propertyId,
                        IsActive = true,
                        MarkedForDeletion = false,
                        ConstructionYear = "2015",
                        FloorId = 1,
                        Floor = floor2
                    },
                    new()
                    {
                        Id = 2,
                        PropertyId = propertyId,
                        IsActive = true,
                        MarkedForDeletion = false,
                        ConstructionYear = "2015",
                        FloorId = 2,
                        Floor = floor10
                    }
                }.BuildMockDbSet().Object);

            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            var sut = CreateService();

            // Act
            var ctx = await sut.LoadPropertyContextAsync(propertyId, 2026);

            // Assert
            Assert.NotNull(ctx);
            Assert.Equal(21, ctx.Parameters.BuildingMaxFloorSequence);
        }

        [Fact]
        public async Task LoadPropertyContextAsync_PropertyHasSocialAttributes_PopulatesSocialAttributeIds()
        {
            // Arrange
            const int propertyId = 1;

            SetupValidProperty(propertyId);
            SetupPropertyDetails(propertyId, constructionYear: "2010");

            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            _propertySocialDetailsRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertySocialDetailsEntity>
                {
                    new()
                    {
                        Id = 1,
                        PropertyId = propertyId,
                        IsActive = true,
                        SocialAttributeId = 99,
                        BitValue = true,
                        SocialAttribute = new SocialAttributeEntity
                        {
                            Id = 99,
                            SocialAttributeCode = "HAS_LIFT",
                            DataType = "BIT"
                        }
                    }
                }.BuildMockDbSet().Object);

            var sut = CreateService();

            // Act
            var ctx = await sut.LoadPropertyContextAsync(propertyId, 2026);

            // Assert
            Assert.Contains(99, ctx.Parameters.SocialAttributeId);
            Assert.True(ctx.Parameters.SocialAttributes.ContainsKey("HAS_LIFT"));
            Assert.Equal(true, ctx.Parameters.SocialAttributes["HAS_LIFT"]);
        }

        [Fact]
        public async Task LoadPropertyContextAsync_AssessmentNotFound_ReturnsNullAssessmentAndContinues()
        {
            // Arrange
            SetupValidProperty(propertyId: 1);
            SetupPropertyDetails(propertyId: 1, constructionYear: "2010");

            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            _propertyAssessmentRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyAssessmentEntity>().BuildMockDbSet().Object);

            var sut = CreateService();

            // Act
            var ctx = await sut.LoadPropertyContextAsync(1, 2026);

            // Assert
            Assert.Null(ctx.PropertyAssessment);
        }

        [Fact]
        public async Task LoadPropertyContextAsync_ChildCollections_ArePopulatedCorrectly()
        {
            // Arrange
            const int propertyId = 1;
            const int detailId = 42;

            SetupValidProperty(propertyId);
            SetupPropertyDetails(propertyId, constructionYear: "2012", detailId: detailId);

            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            _renterRepo.Setup(r => r.GetQueryable())
                .Returns(new List<RenterMastEntity>
                {
                    new()
                    {
                        Id = 1,
                        PropertyDetailsId = detailId,
                        IsActive = true,
                        MarkedForDeletion = false
                    }
                }.BuildMockDbSet().Object);

            _occupancyRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyOccupancyDetailsEntity>
                {
                    new()
                    {
                        Id = 1,
                        PropertyDetailId = detailId,
                        IsActive = true,
                        MarkedForDeletion = false
                    }
                }.BuildMockDbSet().Object);

            var sut = CreateService();

            // Act
            var ctx = await sut.LoadPropertyContextAsync(propertyId, 2026);

            // Assert
            Assert.Single(ctx.Renters);
            Assert.Single(ctx.Occupancies);
        }

        [Fact]
        public async Task LoadPropertyContextAsync_CloneForDetail_SetsPerDetailParameters()
        {
            // Arrange
            SetupValidProperty(propertyId: 1);
            SetupPropertyDetails(propertyId: 1, constructionYear: "2010", assessmentYear: "2015");

            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            var sut = CreateService();
            var ctx = await sut.LoadPropertyContextAsync(1, 2026);

            var detail = ctx.Details[0];
            var detailTypeOfUse = new TypeOfUseEntity
            {
                Id = 5,
                TypeOfUseGroupId = 3
            };

            // Act
            var cloned = ctx.CloneForDetail(detail, detailTypeOfUse);

            // Assert — per-detail fields
            Assert.Same(detail, cloned.Parameters.Detail);
            Assert.Same(detailTypeOfUse, cloned.Parameters.DetailTypeOfUse);
            Assert.NotNull(cloned.Parameters.AssessmentYearValue);
            Assert.NotNull(cloned.Parameters.YearRangeRVIdForDetail);

            // Assert — global parameters (should be same across all details)
            Assert.Equal(ctx.Parameters.FinanceYear, cloned.Parameters.FinanceYear);
            Assert.Equal(ctx.Parameters.ConstructionYearValue, cloned.Parameters.ConstructionYearValue);
            Assert.Equal(ctx.Parameters.YearRangeRVId, cloned.Parameters.YearRangeRVId);
            Assert.Equal(ctx.Parameters.SocialAttributeId, cloned.Parameters.SocialAttributeId);

            // Assert — entity references (shared read-only)
            Assert.Same(ctx.Property, cloned.Property);
            Assert.Same(ctx.Details, cloned.Details);
            Assert.Same(ctx.YearRanges, cloned.YearRanges);
        }

        // ─── Private Helpers ─────────────────────────────────────────────────────

        private PropertyContextLoaderService CreateService()
        {
            return new PropertyContextLoaderService(
                _propertyRepo.Object,
                _propertyDetailsRepo.Object,
                _propertyAssessmentRepo.Object,
                _propertySocialDetailsRepo.Object,
                _renterRepo.Object,
                _occupancyRepo.Object,
                _masterDataService.Object,
                _financeYearProvider.Object,
                _yearMasterRepo.Object,
                _rvCalculationCleanupService.Object,
                _unitOfWork.Object,
                NullLogger<PropertyContextLoaderService>.Instance);
        }

        private void SetupValidProperty(int propertyId)
        {
            _propertyRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyEntity>
                {
                    new()
                    {
                        Id = propertyId,
                        IsActive = true,
                        MarkedForDeletion = false,
                        TaxZoneId = 1,
                        WardId = 1
                    }
                }.BuildMockDbSet().Object);
        }

        private void SetupPropertyDetails(
            int propertyId,
            string? constructionYear,
            int detailId = 1,
            string? assessmentYear = null)
        {
            _propertyDetailsRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyDetailsEntity>
                {
                    new()
                    {
                        Id = detailId,
                        PropertyId = propertyId,
                        IsActive = true,
                        MarkedForDeletion = false,
                        ConstructionYear = constructionYear,
                        AssessmentYear = assessmentYear,
                        FloorId = 1,
                        TypeOfUseId = 1
                    }
                }.BuildMockDbSet().Object);
        }

        [Fact]
        public async Task LoadPropertyContextAsync_MultiplePropertiesSameBuilding_CalculatesMaxFloorAcrossAllProperties()
        {
            // Arrange
            const int targetPropertyId = 1;
            const int otherPropertyId = 2;
            const string propertyNo = "APT100";

            // Setup properties sharing WardId and PropertyNo
            _propertyRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyEntity>
                {
                    new()
                    {
                        Id = targetPropertyId,
                        IsActive = true,
                        MarkedForDeletion = false,
                        TaxZoneId = 1,
                        WardId = 1,
                        PropertyNo = propertyNo
                    },
                    new()
                    {
                        Id = otherPropertyId,
                        IsActive = true,
                        MarkedForDeletion = false,
                        TaxZoneId = 1,
                        WardId = 1,
                        PropertyNo = propertyNo
                    }
                }.BuildMockDbSet().Object);

            var floor5 = new FloorEntity { FloorCode = "5", SequenceNo = 5 }; // Should be ignored as < 12
            var floor12 = new FloorEntity { FloorCode = "12", SequenceNo = 12 };
            var floor13 = new FloorEntity { FloorCode = "13", SequenceNo = 13 };

            _propertyDetailsRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyDetailsEntity>
                {
                    new()
                    {
                        Id = 101,
                        PropertyId = targetPropertyId,
                        IsActive = true,
                        MarkedForDeletion = false,
                        ConstructionYear = "2015",
                        FloorId = 1,
                        Floor = floor5
                    },
                    new()
                    {
                        Id = 102,
                        PropertyId = targetPropertyId,
                        IsActive = true,
                        MarkedForDeletion = false,
                        ConstructionYear = "2015",
                        FloorId = 2,
                        Floor = floor12
                    },
                    new()
                    {
                        Id = 103,
                        PropertyId = otherPropertyId,
                        IsActive = true,
                        MarkedForDeletion = false,
                        ConstructionYear = "2015",
                        FloorId = 3,
                        Floor = floor13
                    }
                }.BuildMockDbSet().Object);

            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            var sut = CreateService();

            // Act
            var ctx = await sut.LoadPropertyContextAsync(targetPropertyId, 2026);

            // Assert
            Assert.NotNull(ctx);
            // Should be 13 (floor13), since floor12 is smaller and floor5 is ignored (SequenceNo < 12)
            Assert.Equal(13, ctx.Parameters.BuildingMaxFloorSequence);
        }

        [Fact]
        public async Task LoadPropertyContextAsync_PartitionedProperty_MergesSocialAttributesFromMainProperty()
        {
            // Arrange
            const int targetPropertyId = 10;
            const int mainPropertyId = 20;
            const string propertyNo = "PROP-100";

            // Setup properties: partitioned property and main property
            _propertyRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyEntity>
                {
                    new()
                    {
                        Id = targetPropertyId,
                        IsActive = true,
                        MarkedForDeletion = false,
                        TaxZoneId = 1,
                        WardId = 1,
                        PropertyNo = propertyNo,
                        PartitionNo = "A"
                    },
                    new()
                    {
                        Id = mainPropertyId,
                        IsActive = true,
                        MarkedForDeletion = false,
                        TaxZoneId = 1,
                        WardId = 1,
                        PropertyNo = propertyNo,
                        PartitionNo = null
                    }
                }.BuildMockDbSet().Object);

            SetupPropertyDetails(targetPropertyId, constructionYear: "2010");

            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            // Setup social attributes for both properties
            _propertySocialDetailsRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertySocialDetailsEntity>
                {
                    // Partitioned property attributes
                    new()
                    {
                        Id = 1,
                        PropertyId = targetPropertyId,
                        IsActive = true,
                        SocialAttributeId = 101,
                        BitValue = false,
                        SocialAttribute = new SocialAttributeEntity
                        {
                            Id = 101,
                            SocialAttributeCode = "HAS_LIFT",
                            DataType = "BIT"
                        }
                    },
                    new()
                    {
                        Id = 2,
                        PropertyId = targetPropertyId,
                        IsActive = true,
                        SocialAttributeId = 102,
                        BitValue = true,
                        SocialAttribute = new SocialAttributeEntity
                        {
                            Id = 102,
                            SocialAttributeCode = "HAS_PARKING",
                            DataType = "BIT"
                        }
                    },
                    // Main property attributes (with overlapping "HAS_LIFT")
                    new()
                    {
                        Id = 3,
                        PropertyId = mainPropertyId,
                        IsActive = true,
                        SocialAttributeId = 101,
                        BitValue = true, // Main property says true, but target property says false
                        SocialAttribute = new SocialAttributeEntity
                        {
                            Id = 101,
                            SocialAttributeCode = "HAS_LIFT",
                            DataType = "BIT"
                        }
                    },
                    new()
                    {
                        Id = 4,
                        PropertyId = mainPropertyId,
                        IsActive = true,
                        SocialAttributeId = 103,
                        BitValue = true, // Unique to main property
                        SocialAttribute = new SocialAttributeEntity
                        {
                            Id = 103,
                            SocialAttributeCode = "HAS_SOLAR",
                            DataType = "BIT"
                        }
                    }
                }.BuildMockDbSet().Object);

            var sut = CreateService();

            // Act
            var ctx = await sut.LoadPropertyContextAsync(targetPropertyId, 2026);

            // Assert
            Assert.NotNull(ctx);
            // Overlapping attribute: partitioned property (targetPropertyId) takes precedence
            Assert.True(ctx.Parameters.SocialAttributes.ContainsKey("HAS_LIFT"));
            Assert.Equal(false, ctx.Parameters.SocialAttributes["HAS_LIFT"]);

            // Partitioned property specific attribute
            Assert.True(ctx.Parameters.SocialAttributes.ContainsKey("HAS_PARKING"));
            Assert.Equal(true, ctx.Parameters.SocialAttributes["HAS_PARKING"]);

            // Main property specific attribute (inherited)
            Assert.True(ctx.Parameters.SocialAttributes.ContainsKey("HAS_SOLAR"));
            Assert.Equal(true, ctx.Parameters.SocialAttributes["HAS_SOLAR"]);

            // SocialAttributeId list should contain all of them (distinct)
            Assert.Contains(101, ctx.Parameters.SocialAttributeId);
            Assert.Contains(102, ctx.Parameters.SocialAttributeId);
            Assert.Contains(103, ctx.Parameters.SocialAttributeId);
            Assert.Equal(3, ctx.Parameters.SocialAttributeId.Count);
        }
    }
}