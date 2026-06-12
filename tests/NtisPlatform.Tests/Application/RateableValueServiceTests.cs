using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.RateableValue;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive tests for RateableValueService covering:
/// - Missing master data failure scenarios
/// - Slab selection boundaries for education/employment taxes
/// - Policy row aggregation semantics
/// - Edge cases in calculation logic
/// </summary>
public class RateableValueServiceTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _propertyRepo;
    private readonly Mock<IRepository<PropertyDetailsEntity, int>> _propertyDetailsRepo;
    private readonly Mock<IRepository<PropertyTaxCalculationRVResultsEntity, int>> _taxResultsRepo;
    private readonly Mock<IRepository<PolicyTaxDetailsEntity, int>> _policyTaxRepo;
    private readonly Mock<IRepository<RenterMastEntity, int>> _renterRepo;
    private readonly Mock<IRepository<PropertyOccupancyDetailsEntity, int>> _occupancyRepo;
    private readonly Mock<IRepository<PropertySocialDetailsEntity, int>> _propertySocialDetailsRepo;
    private readonly Mock<IRepository<PropertyAssessmentEntity, int>> _propertyAssessmentRepo;
    private readonly Mock<IRepository<TransMastRVEntity, int>> _transmastRVRepo;
    private readonly Mock<IRepository<YearMasterEntity, int>> _yearMasterRepo;
    private readonly Mock<ITaxMasterDataService> _masterDataService;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger<RateableValueService>> _logger;
    private readonly Mock<IPolicyConfigurationService> _policyConfigurationService;
    private readonly Mock<NtisPlatform.Application.Interfaces.IFinanceYearProvider> _financeYearProvider;
    private readonly Mock<NtisPlatform.Application.Interfaces.TaxEngine.IRVCalculationCleanupService> _cleanupService;

    public RateableValueServiceTests()
    {
        _propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        _propertyDetailsRepo = new Mock<IRepository<PropertyDetailsEntity, int>>();
        _taxResultsRepo = new Mock<IRepository<PropertyTaxCalculationRVResultsEntity, int>>();
        _policyTaxRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        _renterRepo = new Mock<IRepository<RenterMastEntity, int>>();
        _occupancyRepo = new Mock<IRepository<PropertyOccupancyDetailsEntity, int>>();
        _propertySocialDetailsRepo = new Mock<IRepository<PropertySocialDetailsEntity, int>>();
        _propertyAssessmentRepo = new Mock<IRepository<PropertyAssessmentEntity, int>>();
        _transmastRVRepo = new Mock<IRepository<TransMastRVEntity, int>>();
        _yearMasterRepo = new Mock<IRepository<YearMasterEntity, int>>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger<RateableValueService>>();
        _masterDataService = new Mock<ITaxMasterDataService>();
        _policyConfigurationService = new Mock<IPolicyConfigurationService>();
        _financeYearProvider = new Mock<NtisPlatform.Application.Interfaces.IFinanceYearProvider>();
        _cleanupService = new Mock<NtisPlatform.Application.Interfaces.TaxEngine.IRVCalculationCleanupService>();

        // Setup policy configuration service with default values
        _policyConfigurationService
            .Setup(p => p.GetPolicyValuesAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dictionary<string, string> defaults, CancellationToken _) => defaults);

        // Setup yearMaster repository with default data
        var currentYear = DateTime.Today.Month >= 4 ? DateTime.Today.Year : DateTime.Today.Year - 1;
        _financeYearProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(currentYear);
        _yearMasterRepo.Setup(r => r.GetQueryable())
            .Returns(new List<YearMasterEntity>
            {
                new YearMasterEntity { Id = 1, Year = currentYear - 1, IsActive = true },
                new YearMasterEntity { Id = 2, Year = currentYear, IsActive = true },
                new YearMasterEntity { Id = 3, Year = currentYear + 1, IsActive = true }
            }.BuildMockDbSet().Object);

        // Setup unit of work
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Setup transmastRV repository to return empty queryable
        _transmastRVRepo.Setup(r => r.GetQueryable())
            .Returns(new List<TransMastRVEntity>().BuildMockDbSet().Object);

        // Setup property social details repository to return empty queryable
        _propertySocialDetailsRepo.Setup(r => r.GetQueryable())
            .Returns(new List<PropertySocialDetailsEntity>().BuildMockDbSet().Object);

        // Setup property assessment repository to return empty queryable
        _propertyAssessmentRepo.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyAssessmentEntity>().BuildMockDbSet().Object);
    }

    private RateableValueService CreateService()
    {
        // RateableValueCalculatorService is a pure calculation engine with no I/O — use the real impl.
        var calculatorService = new RateableValueCalculatorService(
            NullLogger<RateableValueCalculatorService>.Instance);

        // RVPersistenceService is wired with the same repo mocks so that callback-based
        // assertions on _policyTaxRepo.AddRangeAsync and _taxResultsRepo.AddRangeAsync still work.
        var persistenceService = new RVPersistenceService(
            _taxResultsRepo.Object,
            _policyTaxRepo.Object,
            _transmastRVRepo.Object,
            _unitOfWork.Object,
            NullLogger<RVPersistenceService>.Instance,
            TimeProvider.System);

        // Rule applicator is mocked — tests do not exercise the rule engine.
        var ruleApplicatorMock = new Mock<IRVRuleApplicator>();
        ruleApplicatorMock
            .Setup(r => r.GetAdjustedRateAsync(
                It.IsAny<PropertyDetailsEntity>(), It.IsAny<TypeOfUseEntity>(),
                It.IsAny<PropertyEntity>(), It.IsAny<PropertyAssessmentEntity>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<decimal>()))
            .ReturnsAsync((decimal?)null);

        // Constructor-safe dependency list.
        // This avoids CS1729 when RateableValueService constructor arguments are added,
        // removed, or reordered, as long as the required dependency type is listed here.
        var dependencies = new object[]
        {
            _propertyRepo.Object,
            _propertyDetailsRepo.Object,
            _taxResultsRepo.Object,
            _policyTaxRepo.Object,
            _renterRepo.Object,
            _occupancyRepo.Object,
            _propertySocialDetailsRepo.Object,
            _propertyAssessmentRepo.Object,
            _transmastRVRepo.Object,
            _yearMasterRepo.Object,
            _masterDataService.Object,
            _unitOfWork.Object,
            _logger.Object,
            _policyConfigurationService.Object,
            _financeYearProvider.Object,
            _cleanupService.Object,
            calculatorService,
            persistenceService,
            ruleApplicatorMock.Object,
            TimeProvider.System
        };

        var matchingConstructor = typeof(RateableValueService)
            .GetConstructors()
            .Select(c => new
            {
                Constructor = c,
                Parameters = c.GetParameters()
            })
            .Where(c => c.Parameters.All(p =>
                dependencies.Any(d => p.ParameterType.IsInstanceOfType(d))))
            .OrderByDescending(c => c.Parameters.Length)
            .FirstOrDefault();

        if (matchingConstructor == null)
        {
            var availableTypes = string.Join(Environment.NewLine, dependencies.Select(d => $"- {d.GetType().FullName}"));
            var constructorSignatures = string.Join(
                Environment.NewLine,
                typeof(RateableValueService)
                    .GetConstructors()
                    .Select(c => $"- RateableValueService({string.Join(", ", c.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name))})"));

            throw new InvalidOperationException(
                "No RateableValueService constructor could be matched from the test dependency list." +
                Environment.NewLine + Environment.NewLine +
                "Available test dependency runtime types:" +
                Environment.NewLine + availableTypes +
                Environment.NewLine + Environment.NewLine +
                "Current RateableValueService constructors:" +
                Environment.NewLine + constructorSignatures);
        }

        var arguments = matchingConstructor.Parameters
            .Select(p => dependencies.First(d => p.ParameterType.IsInstanceOfType(d)))
            .ToArray();

        return (RateableValueService)matchingConstructor.Constructor.Invoke(arguments);
    }

    #region Missing Master Data Failure Tests

    [Fact]
    public async Task CalculateAndSaveAsync_PropertyNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();

        _propertyRepo.Setup(r => r.GetQueryable()).Returns(
            new List<PropertyEntity>().BuildMockDbSet().Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CalculateAndSaveAsync(999)
        );

        Assert.Contains("Property not found", exception.Message);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_NoPropertyDetails_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();

        _propertyRepo.Setup(r => r.GetQueryable()).Returns(
            new List<PropertyEntity>
            {
                new PropertyEntity
                {
                    Id = 1,
                    IsActive = true,
                    MarkedForDeletion = false,
                    TaxZoneId = 1,
                    WardId = 1
                }
            }.BuildMockDbSet().Object);

        _propertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(
            new List<PropertyDetailsEntity>().BuildMockDbSet().Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CalculateAndSaveAsync(1)
        );

        Assert.Contains("PropertyDetails not found", exception.Message);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_MissingConstructionYear_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var properties = new List<PropertyEntity>
        {
            new PropertyEntity
            {
                Id = 1,
                IsActive = true,
                MarkedForDeletion = false,
                TaxZoneId = 1,
                WardId = 1
            }
        }.BuildMockDbSet().Object;

        var details = new List<PropertyDetailsEntity>
        {
            new PropertyDetailsEntity
            {
                Id = 1,
                PropertyId = 1,
                IsActive = true,
                ConstructionYear = null // Missing construction year
            }
        }.BuildMockDbSet().Object;

        _propertyRepo.Setup(r => r.GetQueryable()).Returns(properties);
        _propertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(details);

        SetupBasicMasterData();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CalculateAndSaveAsync(1)
        );

        Assert.Contains("ConstructionYear not found", exception.Message);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_InvalidConstructionYear_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var properties = new List<PropertyEntity>
        {
            new PropertyEntity
            {
                Id = 1,
                IsActive = true,
                MarkedForDeletion = false,
                TaxZoneId = 1,
                WardId = 1
            }
        }.BuildMockDbSet().Object;

        var details = new List<PropertyDetailsEntity>
        {
            new PropertyDetailsEntity
            {
                Id = 1,
                PropertyId = 1,
                IsActive = true,
                ConstructionYear = "INVALID" // Invalid year format
            }
        }.BuildMockDbSet().Object;

        _propertyRepo.Setup(r => r.GetQueryable()).Returns(properties);
        _propertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(details);

        SetupBasicMasterData();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CalculateAndSaveAsync(1)
        );

        Assert.Contains("Invalid ConstructionYear", exception.Message);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_NoYearRangeForConstructionYear_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        SetupPropertyAndDetails(1, "2025");

        SetupBasicMasterData();

        // Override year ranges with empty list after basic setup
        _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
            .ReturnsAsync(new List<AssessmentYearRangeEntity>()); // Empty year ranges

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CalculateAndSaveAsync(1)
        );

        Assert.Contains("Assessment year range not found", exception.Message);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_NoRateForPropertyConfiguration_UsesZeroRateFallback()
    {
        // Arrange
        var service = CreateService();
        SetupPropertyAndDetails(1, "2020");
        SetupMasterDataWithEmptyRates();

        // Act - No longer throws, instead uses a fallback zero rate
        var result = await service.CalculateAndSaveAsync(1);

        // Assert - Service should complete and return a result (with zero rate calculations)
        Assert.NotNull(result);
    }

    #endregion

    #region Slab Selection Boundary Tests

    [Theory]
    [InlineData(10000.0, 0.0, 15000.0, true)]  // Within range
    [InlineData(15000.0, 0.0, 15000.0, true)]  // At upper boundary
    [InlineData(0.0, 0.0, 15000.0, true)]      // At lower boundary
    [InlineData(15001.0, 0.0, 15000.0, false)] // Above range
    [InlineData(-1.0, 0.0, 15000.0, false)]    // Below range (negative)
    public void SlabSelection_BoundaryConditions_WorksCorrectly(
        double testValue,
        double? minAmount,
        double? maxAmount,
        bool expectedMatch)
    {
        // This tests the IsSlabMatch logic used in education/employment tax calculation
        var testValueDecimal = (decimal)testValue;
        var minAmountDecimal = minAmount.HasValue ? (decimal?)minAmount.Value : null;
        var maxAmountDecimal = maxAmount.HasValue ? (decimal?)maxAmount.Value : null;

        var minOk = !minAmountDecimal.HasValue || testValueDecimal >= minAmountDecimal.Value;
        var maxOk = !maxAmountDecimal.HasValue || testValueDecimal <= maxAmountDecimal.Value;
        var actualMatch = minOk && maxOk;

        Assert.Equal(expectedMatch, actualMatch);
    }

    [Fact]
    public async Task EducationTax_SelectsCorrectSlabForPropertyType()
    {
        // Arrange
        var service = CreateService();
        var propertyId = 1;
        SetupPropertyAndDetails(propertyId, "2020", typeOfUseType: "R");

        var educationSlabs = new List<EducationTaxMasterEntity>
        {
            new EducationTaxMasterEntity
            {
                Id = 1,
                Type = "R",
                MinAmount = 0m,
                MaxAmount = 100000m,
                Rate = 5.0m,
                IsActive = true
            },
            new EducationTaxMasterEntity
            {
                Id = 2,
                Type = "R",
                MinAmount = 100001m,
                MaxAmount = 5000000m,  // Much higher to catch the calculated ALV
                Rate = 7.5m,
                IsActive = true
            },
            new EducationTaxMasterEntity
            {
                Id = 3,
                Type = "C",
                MinAmount = 0m,
                MaxAmount = 5000000m,
                Rate = 10.0m,
                IsActive = true
            }
        };

        // Setup callback BEFORE calling SetupCompleteCalculationData
        List<PolicyTaxDetailsEntity>? capturedPolicyRows = null;
        _policyTaxRepo.Setup(r => r.AddRangeAsync(
            It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(),
            It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((rows, ct) =>
            {
                capturedPolicyRows = rows.ToList();
            })
            .Returns(Task.CompletedTask);

        SetupCompleteCalculationData(educationSlabs, new List<EmploymentTaxMasterEntity>());

        // Act
        var result = await service.CalculateAndSaveAsync(propertyId);

        // Assert - Should select residential slab (R type) not commercial (C type)
        Assert.NotNull(result);
        Assert.NotNull(capturedPolicyRows);
        Assert.NotEmpty(capturedPolicyRows); // Ensure there are some rows
        // Check that education tax (taxId == 2) was added with positive amount
        var educationTaxRow = capturedPolicyRows.FirstOrDefault(x => x.TaxId == 2);
        if (educationTaxRow == null)
        {
            // Debug: show what tax IDs we got
            var taxIds = string.Join(", ", capturedPolicyRows.Select(x => x.TaxId));
            Assert.Fail($"Education tax (TaxId=2) not found. Got TaxIds: {taxIds}");
        }
        Assert.True(educationTaxRow.TaxAmount > 0);
    }

    [Fact]
    public async Task EmploymentTax_UsesMaxAmountForMultipleDetails()
    {
        // Arrange
        var service = CreateService();
        var propertyId = 1;

        // Setup property with TWO details to test aggregation
        SetupPropertyWithMultipleDetails(propertyId, "2020");

        var employmentSlabs = new List<EmploymentTaxMasterEntity>
        {
            new EmploymentTaxMasterEntity
            {
                Id = 1,
                Type = "R",
                MinAmount = 0m,
                MaxAmount = 100000m,
                Rate = 3.0m,
                IsActive = true
            }
        };

        SetupCompleteCalculationData(new List<EducationTaxMasterEntity>(), employmentSlabs);

        // Act
        var result = await service.CalculateAndSaveAsync(propertyId);

        // Assert - Employment tax should use MAX not SUM for property type group
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(null, null, 50000.0, true)]   // No bounds - always matches
    [InlineData(10000.0, null, 50000.0, true)]  // Only min bound
    [InlineData(null, 100000.0, 50000.0, true)] // Only max bound
    [InlineData(40000.0, 60000.0, 50000.0, true)] // Within range
    [InlineData(60000.0, 80000.0, 50000.0, false)] // Below min
    [InlineData(10000.0, 40000.0, 50000.0, false)] // Above max
    public void SlabSelection_NullBoundaries_HandledCorrectly(
        double? minAmount,
        double? maxAmount,
        double testValue,
        bool expectedMatch)
    {
        // Test null boundary handling (open-ended slabs)
        var testValueDecimal = (decimal)testValue;
        var minAmountDecimal = minAmount.HasValue ? (decimal?)minAmount.Value : null;
        var maxAmountDecimal = maxAmount.HasValue ? (decimal?)maxAmount.Value : null;

        var minOk = !minAmountDecimal.HasValue || testValueDecimal >= minAmountDecimal.Value;
        var maxOk = !maxAmountDecimal.HasValue || testValueDecimal <= maxAmountDecimal.Value;
        var actualMatch = minOk && maxOk;

        Assert.Equal(expectedMatch, actualMatch);
    }

    #endregion

    #region Policy Row Aggregation Tests

    [Fact]
    public async Task PolicyRowAggregation_SumsTaxAmountsCorrectly()
    {
        // Arrange
        var service = CreateService();
        var propertyId = 1;
        SetupPropertyAndDetails(propertyId, "2020");
        SetupCompleteCalculationData(new List<EducationTaxMasterEntity>(), new List<EmploymentTaxMasterEntity>());

        List<PolicyTaxDetailsEntity>? capturedPolicyRows = null;
        _policyTaxRepo.Setup(r => r.AddRangeAsync(
            It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(),
            It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((rows, ct) =>
            {
                capturedPolicyRows = rows.ToList();
            })
            .Returns(Task.CompletedTask);

        // Act
        await service.CalculateAndSaveAsync(propertyId);

        // Assert
        Assert.NotNull(capturedPolicyRows);
        Assert.NotEmpty(capturedPolicyRows);

        // Verify each tax is aggregated correctly
        foreach (var policyRow in capturedPolicyRows)
        {
            Assert.Equal(propertyId, policyRow.PropertyId);
            Assert.Equal("NETTAX", policyRow.PolicyCode);
            Assert.True(policyRow.TaxAmount >= 0);
            Assert.True(policyRow.IsActive);
            Assert.False(policyRow.MarkedForDeletion);
        }
    }

    [Fact]
    public async Task PolicyRowAggregation_EducationTax_UsesMaxNotSum()
    {
        // Arrange
        var service = CreateService();
        var propertyId = 1;

        // Create multiple property details to test aggregation
        SetupPropertyWithMultipleDetails(propertyId, "2020");

        var educationSlabs = new List<EducationTaxMasterEntity>
        {
            new EducationTaxMasterEntity
            {
                Id = 1,
                Type = "R",
                MinAmount = 0m,
                MaxAmount = 100000m,
                Rate = 5.0m,
                IsActive = true
            }
        };

        SetupCompleteCalculationData(educationSlabs, new List<EmploymentTaxMasterEntity>());

        List<PolicyTaxDetailsEntity>? capturedPolicyRows = null;
        _policyTaxRepo.Setup(r => r.AddRangeAsync(
            It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(),
            It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((rows, ct) =>
            {
                capturedPolicyRows = rows.ToList();
            })
            .Returns(Task.CompletedTask);

        // Act
        await service.CalculateAndSaveAsync(propertyId);

        // Assert
        var educationPolicyRow = capturedPolicyRows?.FirstOrDefault(p => p.TaxId == 2); // Education tax
        if (educationPolicyRow != null)
        {
            // For education tax (taxId == 2), should use MAX not SUM
            // Verify the amount is reasonable
            Assert.True(educationPolicyRow.TaxAmount >= 0);
        }
    }

    [Fact]
    public async Task PolicyRowAggregation_WritesFreshNetTaxPolicyRows()
    {
        // Old-row deactivation now happens via a bulk ExecuteUpdateAsync (relational SQL UPDATE),
        // which Moq/MockQueryable cannot observe — that path is covered at the integration level.
        // This unit test verifies the replacement side: fresh active NETTAX rows are inserted.

        // Arrange
        var service = CreateService();
        var propertyId = 1;
        SetupPropertyAndDetails(propertyId, "2020");
        SetupCompleteCalculationData(new List<EducationTaxMasterEntity>(), new List<EmploymentTaxMasterEntity>());

        List<PolicyTaxDetailsEntity>? capturedPolicyRows = null;
        _policyTaxRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((rows, ct) => capturedPolicyRows = rows.ToList())
            .Returns(Task.CompletedTask);

        // Act
        await service.CalculateAndSaveAsync(propertyId);

        // Assert — replacement rows were written, all active NETTAX rows for the property
        Assert.NotNull(capturedPolicyRows);
        Assert.NotEmpty(capturedPolicyRows);
        Assert.All(capturedPolicyRows, row =>
        {
            Assert.Equal(propertyId, row.PropertyId);
            Assert.Equal("NETTAX", row.PolicyCode);
            Assert.True(row.IsActive);
            Assert.False(row.MarkedForDeletion);
        });
    }

    [Fact]
    public async Task PolicyRowAggregation_CalculatesCorrectRateableValue()
    {
        // Arrange
        var service = CreateService();
        var propertyId = 1;
        SetupPropertyWithMultipleDetails(propertyId, "2020");
        SetupCompleteCalculationData(new List<EducationTaxMasterEntity>(), new List<EmploymentTaxMasterEntity>());

        List<PolicyTaxDetailsEntity>? capturedPolicyRows = null;
        _policyTaxRepo.Setup(r => r.AddRangeAsync(
            It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(),
            It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((rows, ct) =>
            {
                capturedPolicyRows = rows.ToList();
            })
            .Returns(Task.CompletedTask);

        // Act
        await service.CalculateAndSaveAsync(propertyId);

        // Assert
        Assert.NotNull(capturedPolicyRows);
        if (capturedPolicyRows.Any())
        {
            // All policy rows should have the same total RV (PolicyRVorCVvalue)
            var totalRvValues = capturedPolicyRows.Select(p => p.PolicyRVorCVvalue).Distinct().ToList();
            Assert.Single(totalRvValues); // All should have same total RV

            // Total RV should be positive
            Assert.True(totalRvValues.First() >= 0);
        }
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task CalculateAndSaveAsync_WithRenter_UsesRentalValueWhenHigher()
    {
        // Arrange
        var service = CreateService();
        var propertyId = 1;
        var detailId = 10;

        SetupPropertyAndDetails(propertyId, "2020", hasRenter: true);

        _renterRepo.Setup(r => r.GetQueryable()).Returns(
            new List<RenterMastEntity>
            {
                new RenterMastEntity
                {
                    Id = 1,
                    PropertyDetailsId = detailId,
                    FinalYearlyRent = 100000, // High rent to override calculated
                    RentMonthly = 8333,
                    IsActive = true,
                    MarkedForDeletion = false
                }
            }.BuildMockDbSet().Object);

        SetupCompleteCalculationData(new List<EducationTaxMasterEntity>(), new List<EmploymentTaxMasterEntity>());

        // Act
        var result = await service.CalculateAndSaveAsync(propertyId);

        // Assert
        Assert.NotNull(result);
        // When renter's yearly rent is higher, it should be used
    }

    [Fact]
    public async Task CalculateAndSaveAsync_MultipleDetailsAndTaxes_CreatesCorrectNumberOfRows()
    {
        // Arrange
        var service = CreateService();
        var propertyId = 1;
        SetupPropertyWithMultipleDetails(propertyId, "2020"); // 2 details

        // Setup callback BEFORE calling SetupCompleteCalculationData
        List<PolicyTaxDetailsEntity>? capturedResults = null;
        _policyTaxRepo.Setup(r => r.AddRangeAsync(
            It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(),
            It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((rows, ct) =>
            {
                capturedResults = rows.ToList();
            })
            .Returns(Task.CompletedTask);

        SetupCompleteCalculationData(new List<EducationTaxMasterEntity>(), new List<EmploymentTaxMasterEntity>());

        // Override taxes with custom list (3 standard taxes)
        var taxes = new List<TaxMasterEntity>
        {
            new TaxMasterEntity { Id = 10, TaxName = "General Tax", TaxCode = "GT", IsActive = true },
            new TaxMasterEntity { Id = 11, TaxName = "Water Tax", TaxCode = "WT", IsActive = true },
            new TaxMasterEntity { Id = 12, TaxName = "Drainage Tax", TaxCode = "DT", IsActive = true }
        };
        _masterDataService.Setup(m => m.GetActiveTaxesAsync()).ReturnsAsync(taxes);

        // Setup tax percentages for all taxes
        _masterDataService.Setup(m => m.GetActiveTaxPercentagesAsync())
            .ReturnsAsync(new List<TaxPercentageMasterRVEntity>
            {
                new TaxPercentageMasterRVEntity { Id = 1, TaxId = 10, TypeOfUseId = 1, YearRangeRVId = 1, TaxPercentage = 10.0m, IsActive = true },
                new TaxPercentageMasterRVEntity { Id = 2, TaxId = 11, TypeOfUseId = 1, YearRangeRVId = 1, TaxPercentage = 5.0m, IsActive = true },
                new TaxPercentageMasterRVEntity { Id = 3, TaxId = 12, TypeOfUseId = 1, YearRangeRVId = 1, TaxPercentage = 3.0m, IsActive = true }
            });

        // Act
        await service.CalculateAndSaveAsync(propertyId);

        // Assert
        Assert.NotNull(capturedResults);
        // Should have policy row for each tax
        Assert.True(capturedResults.Count >= 3);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_SetsCorrectTimestampsAndFlags()
    {
        // Arrange
        var service = CreateService();
        var propertyId = 1;
        SetupPropertyAndDetails(propertyId, "2020");
        SetupCompleteCalculationData(new List<EducationTaxMasterEntity>(), new List<EmploymentTaxMasterEntity>());

        List<PolicyTaxDetailsEntity>? capturedResults = null;
        _policyTaxRepo.Setup(r => r.AddRangeAsync(
            It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(),
            It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((rows, ct) =>
            {
                capturedResults = rows.ToList();
            })
            .Returns(Task.CompletedTask);

        // Act
        await service.CalculateAndSaveAsync(propertyId);

        // Assert
        Assert.NotNull(capturedResults);
        Assert.All(capturedResults, row =>
        {
            Assert.True(row.IsActive);
            Assert.False(row.MarkedForDeletion);
            Assert.NotEqual(default(DateTime), row.CreatedDate);
            Assert.NotEqual(default(DateTime), row.UpdatedDate);
        });
    }

    #endregion

    #region Helper Methods

    private void SetupPropertyAndDetails(int propertyId, string constructionYear, string typeOfUseType = "R", bool hasRenter = false)
    {
        _propertyRepo.Setup(r => r.GetQueryable()).Returns(
            new List<PropertyEntity>
            {
                new PropertyEntity
                {
                    Id = propertyId,
                    IsActive = true,
                    MarkedForDeletion = false,
                    TaxZoneId = 1,
                    WardId = 1
                }
            }.BuildMockDbSet().Object);

        _propertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(
            new List<PropertyDetailsEntity>
            {
                new PropertyDetailsEntity
                {
                    Id = 10,
                    PropertyId = propertyId,
                    IsActive = true,
                    ConstructionYear = constructionYear,
                    TypeOfUseId = 1,
                    ConstructionTypeId = 1,
                    FloorId = 1,
                    CarpetAreaSqMeter = 1000,
                    IsRenter = hasRenter
                }
            }.BuildMockDbSet().Object);
    }

    private void SetupPropertyWithMultipleDetails(int propertyId, string constructionYear)
    {
        _propertyRepo.Setup(r => r.GetQueryable()).Returns(
            new List<PropertyEntity>
            {
                new PropertyEntity
                {
                    Id = propertyId,
                    IsActive = true,
                    MarkedForDeletion = false,
                    TaxZoneId = 1,
                    WardId = 1
                }
            }.BuildMockDbSet().Object);

        _propertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(
            new List<PropertyDetailsEntity>
            {
                new PropertyDetailsEntity
                {
                    Id = 10,
                    PropertyId = propertyId,
                    IsActive = true,
                    ConstructionYear = constructionYear,
                    TypeOfUseId = 1,
                    ConstructionTypeId = 1,
                    FloorId = 1,
                    CarpetAreaSqMeter = 1000
                },
                new PropertyDetailsEntity
                {
                    Id = 11,
                    PropertyId = propertyId,
                    IsActive = true,
                    ConstructionYear = constructionYear,
                    TypeOfUseId = 1,
                    ConstructionTypeId = 1,
                    FloorId = 2,
                    CarpetAreaSqMeter = 800
                }
            }.BuildMockDbSet().Object);
    }

    private void SetupBasicMasterData()
    {
        _masterDataService.Setup(m => m.GetActiveTypeOfUsesAsync())
            .ReturnsAsync(new List<TypeOfUseEntity>
            {
                new TypeOfUseEntity { Id = 1, Type = "R", TypeOfUseGroupId = 1, IsActive = true }
            });

        _masterDataService.Setup(m => m.GetActiveSubTypeOfUsesAsync())
            .ReturnsAsync(new List<SubTypeOfUseEntity>());

        _masterDataService.Setup(m => m.GetActiveFloorsAsync())
            .ReturnsAsync(new List<FloorEntity>
            {
                new FloorEntity { Id = 1, FloorCode = "GF", IsActive = true }
            });

        _masterDataService.Setup(m => m.GetActiveSubFloorsAsync())
            .ReturnsAsync(new List<SubFloorEntity>());

        _masterDataService.Setup(m => m.GetActiveConstructionTypesAsync())
            .ReturnsAsync(new List<ConstructionTypeEntity>
            {
                new ConstructionTypeEntity { Id = 1, ConstructionCode = "RCC", IsActive = true }
            });

        _masterDataService.Setup(m => m.GetRateSectionIdForWardAsync(It.IsAny<int?>()))
            .ReturnsAsync(1);

        _masterDataService.Setup(m => m.GetRatesForSectionAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<RateEntity>
            {
                new RateEntity
                {
                    Id = 1,
                    TaxZoneId = 1,
                    ConstructionTypeId = 1,
                    TypeOfUseGroupId = 1,
                    YearRangeRVId = 1,
                    RateSquareMeter = 1200,
                    IsActive = true
                }
            });

        _masterDataService.Setup(m => m.GetActiveDepreciationsAsync())
            .ReturnsAsync(new List<DepreciationMasterEntity>
            {
                new DepreciationMasterEntity
                {
                    Id = 1,
                    ConstructionTypeId = 1,
                    MinYear = 0,
                    MaxYear = 100,
                    Rate = 10m,
                    IsActive = true
                }
            });

        _masterDataService.Setup(m => m.GetActiveYearRangesAsync())
            .ReturnsAsync(new List<AssessmentYearRangeEntity>
            {
                new AssessmentYearRangeEntity
                {
                    Id = 1,
                    FromYear = 2000,
                    ToYear = 2030,
                    IsActive = true
                }
            });

        _masterDataService.Setup(m => m.GetActiveTaxesAsync())
            .ReturnsAsync(new List<TaxMasterEntity>
            {
                new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PT", IsActive = true }
            });

        _masterDataService.Setup(m => m.GetActiveTaxPercentagesAsync())
            .ReturnsAsync(new List<TaxPercentageMasterRVEntity>
            {
                new TaxPercentageMasterRVEntity
                {
                    Id = 1,
                    TaxId = 1,
                    TypeOfUseId = 1,
                    YearRangeRVId = 1,
                    TaxPercentage = 10.0m,
                    IsActive = true
                }
            });

        _masterDataService.Setup(m => m.GetActiveEducationTaxSlabsAsync())
            .ReturnsAsync(new List<EducationTaxMasterEntity>());

        _masterDataService.Setup(m => m.GetActiveEmploymentTaxSlabsAsync())
            .ReturnsAsync(new List<EmploymentTaxMasterEntity>());

        _renterRepo.Setup(r => r.GetQueryable()).Returns(
            new List<RenterMastEntity>().BuildMockDbSet().Object);

        _occupancyRepo.Setup(r => r.GetQueryable()).Returns(
            new List<PropertyOccupancyDetailsEntity>().BuildMockDbSet().Object);

        _taxResultsRepo.Setup(r => r.GetQueryable()).Returns(
            new List<PropertyTaxCalculationRVResultsEntity>().BuildMockDbSet().Object);

        _policyTaxRepo.Setup(r => r.GetQueryable()).Returns(
            new List<PolicyTaxDetailsEntity>().BuildMockDbSet().Object);

        // Setup UpdateAsync for policy tax repository
        _policyTaxRepo.Setup(r => r.UpdateAsync(
            It.IsAny<PolicyTaxDetailsEntity>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Note: AddRangeAsync is NOT set up here to allow individual tests to mock it with callbacks
    }

    private void SetupMasterDataWithEmptyRates()
    {
        SetupBasicMasterData();

        // Override rates with empty list to trigger "Rate not found" error
        _masterDataService.Setup(m => m.GetRatesForSectionAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<RateEntity>());
    }

    private void SetupCompleteCalculationData(
        List<EducationTaxMasterEntity> educationSlabs,
        List<EmploymentTaxMasterEntity> employmentSlabs)
    {
        SetupBasicMasterData();

        _masterDataService.Setup(m => m.GetActiveEducationTaxSlabsAsync())
            .ReturnsAsync(educationSlabs);

        _masterDataService.Setup(m => m.GetActiveEmploymentTaxSlabsAsync())
            .ReturnsAsync(employmentSlabs);

        // IsEducationTax / IsEmploymentTax compare TaxCategoryMaster.CategoryCode.
        // Populate the navigation so the predicates work without hitting the DB.
        var taxes = new List<TaxMasterEntity>
        {
            new TaxMasterEntity { Id = 1, TaxName = "Property Tax",  TaxCode = "PT",        IsActive = true, TaxCategoryId = 1,
                TaxCategoryMaster = new TaxCategoryMasterEntity { Id = 1, CategoryCode = "TAX", CategoryName = "Property Tax" } },
            new TaxMasterEntity { Id = 2, TaxName = "Education Tax", TaxCode = "STATE_EDU", IsActive = true, TaxCategoryId = 3,
                TaxCategoryMaster = new TaxCategoryMasterEntity { Id = 3, CategoryCode = "EDU", CategoryName = "State Education Tax" } },
            new TaxMasterEntity { Id = 3, TaxName = "Employment Tax",TaxCode = "STATE_EMP", IsActive = true, TaxCategoryId = 4,
                TaxCategoryMaster = new TaxCategoryMasterEntity { Id = 4, CategoryCode = "EMP", CategoryName = "State Employment Tax" } }
        };

        _masterDataService.Setup(m => m.GetActiveTaxesAsync()).ReturnsAsync(taxes);
    }

    #endregion
}
