using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Services.Rules;
using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
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

        private readonly Mock<IRepository<PropertyEntity, int>>               _propertyRepo;
        private readonly Mock<IRepository<PropertyDetailsEntity, int>>        _propertyDetailsRepo;
        private readonly Mock<IRepository<PropertyAssessmentEntity, int>>     _propertyAssessmentRepo;
        private readonly Mock<IRepository<PropertySocialDetailsEntity, int>>  _propertySocialDetailsRepo;
        private readonly Mock<IRepository<RenterMastEntity, int>>             _renterRepo;
        private readonly Mock<IRepository<PropertyOccupancyDetailsEntity, int>> _occupancyRepo;
        private readonly Mock<TaxMasterDataService>                           _masterDataService;

        // Standard valid year range used across most tests
        private static readonly AssessmentYearRangeEntity DefaultYearRange = new()
        {
            Id       = 10,
            FromYear = 2000,
            ToYear   = 2030,
            IsActive = true
        };

        public PropertyContextLoaderServiceTests()
        {
            _propertyRepo              = new Mock<IRepository<PropertyEntity, int>>();
            _propertyDetailsRepo       = new Mock<IRepository<PropertyDetailsEntity, int>>();
            _propertyAssessmentRepo    = new Mock<IRepository<PropertyAssessmentEntity, int>>();
            _propertySocialDetailsRepo = new Mock<IRepository<PropertySocialDetailsEntity, int>>();
            _renterRepo                = new Mock<IRepository<RenterMastEntity, int>>();
            _occupancyRepo             = new Mock<IRepository<PropertyOccupancyDetailsEntity, int>>();

            // Build TaxMasterDataService mock with the minimal constructor repos it needs
            var typeOfUseRepo          = new Mock<IRepository<TypeOfUseEntity, int>>();
            var subTypeOfUseRepo       = new Mock<IRepository<SubTypeOfUseEntity, int>>();
            var floorRepo              = new Mock<IRepository<FloorEntity, int>>();
            var subFloorRepo           = new Mock<IRepository<SubFloorEntity, int>>();
            var constructionTypeRepo   = new Mock<IRepository<ConstructionTypeEntity, int>>();
            var rateRepo               = new Mock<IRepository<RateEntity, int>>();
            var rateSectionRepo        = new Mock<IRepository<RateSectionEntity, int>>();
            var rateSectionDetailsRepo = new Mock<IRepository<RateSectionDetailsEntity, int>>();
            var depreciationRepo       = new Mock<IRepository<DepreciationMasterEntity, int>>();
            var yearRangeRepo          = new Mock<IRepository<AssessmentYearRangeEntity, int>>();
            var taxMasterRepo          = new Mock<IRepository<TaxMasterEntity, int>>();
            var taxPercentageRepo      = new Mock<IRepository<TaxPercentageMasterRVEntity, int>>();
            var educationTaxRepo       = new Mock<IRepository<EducationTaxMasterEntity, int>>();
            var employmentTaxRepo      = new Mock<IRepository<EmploymentTaxMasterEntity, int>>();

            _masterDataService = new Mock<TaxMasterDataService>(
                typeOfUseRepo.Object,
                subTypeOfUseRepo.Object,
                floorRepo.Object,
                subFloorRepo.Object,
                constructionTypeRepo.Object,
                rateRepo.Object,
                rateSectionRepo.Object,
                rateSectionDetailsRepo.Object,
                depreciationRepo.Object,
                yearRangeRepo.Object,
                taxMasterRepo.Object,
                taxPercentageRepo.Object,
                educationTaxRepo.Object,
                employmentTaxRepo.Object);

            // Default: all repos return empty async-capable mocks.
            // This matters because LoadPropertyContextAsync uses Task.WhenAll — all 4 queries
            // run simultaneously. If any repo returns a plain non-async IQueryable (Moq loose
            // default), EF Core's ToListAsync/FirstOrDefaultAsync throws
            // "The source 'IQueryable' doesn't implement IAsyncEnumerable<T>".
            // Individual tests override these defaults as needed.
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
        public async Task LoadPropertyContextAsync_NoPropertyDetails_ThrowsInvalidOperationException()
        {
            // Arrange
            SetupValidProperty(propertyId: 1);
            _propertyDetailsRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyDetailsEntity>().BuildMockDbSet().Object);

            var sut = CreateService();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.LoadPropertyContextAsync(1, 2026));

            Assert.Contains("PropertyDetails not found", ex.Message);
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
            const int propertyId            = 1;
            const int financeYear           = 2026;
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
            Assert.Equal(financeYear,           ctx.Parameters.FinanceYear);
            Assert.Equal(constructionYearValue,  ctx.Parameters.ConstructionYearValue);
            Assert.Equal(DefaultYearRange.Id,    ctx.Parameters.YearRangeRVId);

            // Per-detail fields are null at root context level (set only by CloneForDetail)
            Assert.Null(ctx.Parameters.Detail);
            Assert.Null(ctx.Parameters.DetailTypeOfUse);
        }

        [Fact]
        public async Task LoadPropertyContextAsync_PropertyHasLift_SetsHasLiftTrue()
        {
            // Arrange
            const int propertyId = 1;
            SetupValidProperty(propertyId);
            SetupPropertyDetails(propertyId, constructionYear: "2010");
            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            // Simulate a "HAS_LIFT" BIT social attribute with value = true
            // BitValue must be true and DataType must be "BIT" to be resolved as bool true.
            _propertySocialDetailsRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertySocialDetailsEntity>
                {
                    new()
                    {
                        Id = 1, PropertyId = propertyId, IsActive = true,
                        BitValue = true,   // ← required: actual stored value
                        SocialAttribute = new SocialAttributeEntity
                        {
                            Id = 99, SocialAttributeCode = "HAS_LIFT", DataType = "BIT"
                        }
                    }
                }.BuildMockDbSet().Object);

            var sut = CreateService();

            // Act
            var ctx = await sut.LoadPropertyContextAsync(propertyId, 2026);

            // Assert — HasLift shortcut
            Assert.True(ctx.Parameters.HasLift);

            // Assert — SocialAttributes dictionary also contains HAS_LIFT
            Assert.True(ctx.Parameters.SocialAttributes.ContainsKey("HAS_LIFT"));
            Assert.Equal(true, ctx.Parameters.SocialAttributes["HAS_LIFT"]);
        }

        [Fact]
        public async Task LoadPropertyContextAsync_PropertyHasNoLift_SetsHasLiftFalse()
        {
            // Arrange
            const int propertyId = 1;
            SetupValidProperty(propertyId);
            SetupPropertyDetails(propertyId, constructionYear: "2010");
            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            // No social detail record for HAS_LIFT — default setup returns empty list
            _propertySocialDetailsRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertySocialDetailsEntity>().BuildMockDbSet().Object);

            var sut = CreateService();

            // Act
            var ctx = await sut.LoadPropertyContextAsync(propertyId, 2026);

            // Assert
            Assert.False(ctx.Parameters.HasLift);
        }

        [Fact]
        public async Task LoadPropertyContextAsync_AssessmentNotFound_ReturnsNullAssessmentAndContinues()
        {
            // Arrange
            SetupValidProperty(propertyId: 1);
            SetupPropertyDetails(propertyId: 1, constructionYear: "2010");
            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            // No assessment record — loader should warn but not throw
            _propertyAssessmentRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyAssessmentEntity>().BuildMockDbSet().Object);

            var sut = CreateService();

            // Act — must not throw
            var ctx = await sut.LoadPropertyContextAsync(1, 2026);

            // Assert
            Assert.Null(ctx.PropertyAssessment);
        }

        [Fact]
        public async Task LoadPropertyContextAsync_ChildCollections_ArePopulatedCorrectly()
        {
            // Arrange
            const int propertyId = 1;
            const int detailId   = 42;

            SetupValidProperty(propertyId);
            SetupPropertyDetails(propertyId, constructionYear: "2012", detailId: detailId);
            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            _renterRepo.Setup(r => r.GetQueryable())
                .Returns(new List<RenterMastEntity>
                {
                    new() { Id = 1, PropertyDetailsId = detailId, IsActive = true, MarkedForDeletion = false }
                }.BuildMockDbSet().Object);

            _occupancyRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyOccupancyDetailsEntity>
                {
                    new() { Id = 1, PropertyDetailId = detailId, IsActive = true, MarkedForDeletion = false }
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
            SetupPropertyDetails(propertyId: 1, constructionYear: "2010");
            _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
                .ReturnsAsync(new List<AssessmentYearRangeEntity> { DefaultYearRange });

            var sut = CreateService();
            var ctx = await sut.LoadPropertyContextAsync(1, 2026);

            var detail          = ctx.Details[0];
            var detailTypeOfUse = new TypeOfUseEntity { Id = 5, TypeOfUseGroupId = 3 };

            // Act
            var cloned = ctx.CloneForDetail(detail, detailTypeOfUse);

            // Assert — per-detail fields are now populated in the clone
            Assert.Same(detail,          cloned.Parameters.Detail);
            Assert.Same(detailTypeOfUse, cloned.Parameters.DetailTypeOfUse);

            // Assert — global params are preserved verbatim
            Assert.Equal(ctx.Parameters.FinanceYear,           cloned.Parameters.FinanceYear);
            Assert.Equal(ctx.Parameters.ConstructionYearValue, cloned.Parameters.ConstructionYearValue);
            Assert.Equal(ctx.Parameters.HasLift,               cloned.Parameters.HasLift);

            // Assert — entity references are shared (not deep-cloned, read-only safe)
            Assert.Same(ctx.Property, cloned.Property);
            Assert.Same(ctx.Details,  cloned.Details);
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
                NullLogger<PropertyContextLoaderService>.Instance);
        }

        private void SetupValidProperty(int propertyId)
        {
            _propertyRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyEntity>
                {
                    new()
                    {
                        Id = propertyId, IsActive = true, MarkedForDeletion = false,
                        TaxZoneId = 1, WardId = 1
                    }
                }.BuildMockDbSet().Object);
        }

        private void SetupPropertyDetails(
            int    propertyId,
            string? constructionYear,
            int    detailId = 1)
        {
            _propertyDetailsRepo.Setup(r => r.GetQueryable())
                .Returns(new List<PropertyDetailsEntity>
                {
                    new()
                    {
                        Id = detailId, PropertyId = propertyId,
                        IsActive = true, MarkedForDeletion = false,
                        ConstructionYear = constructionYear,
                        FloorId = 1, TypeOfUseId = 1
                    }
                }.BuildMockDbSet().Object);
        }
    }
}
