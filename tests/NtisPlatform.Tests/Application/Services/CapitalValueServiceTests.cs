using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Application.Configuration;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.CapitalValue;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.ICapitalValueService;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Calculation;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Data;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Persistence;
using NtisPlatform.Application.Services.CapitalValue;
using NtisPlatform.Application.Services.CapitalValue.MasterDataProviders;

using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Tests.Helpers;
using Xunit;

using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;

namespace NtisPlatform.Tests.Application.Services;

public class CapitalValueServiceTests
{
    private readonly Mock<IPropertyTaxCalculationCVResultsService> _cvResultsService;
    private readonly Mock<IPolicyTaxDetailsCVService> _policyTaxService;
    private readonly Mock<ITransMastService> _transMastService;
    private readonly Mock<IPropertyDataLoader> _propertyDataLoader;
    private readonly Mock<ICapitalValueMasterDataProvider> _masterDataProvider;
    private readonly Mock<ICapitalValueCalculator> _calculator;
    private readonly Mock<ICapitalValuePersistenceService> _persistenceService;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly IMapper _mapper;
    private readonly Mock<IOptions<CapitalValueOptions>> _options;
    private readonly Mock<ILogger<CapitalValueService>> _logger;

    public CapitalValueServiceTests()
    {
        _cvResultsService = new Mock<IPropertyTaxCalculationCVResultsService>();
        _policyTaxService = new Mock<IPolicyTaxDetailsCVService>();
        _transMastService = new Mock<ITransMastService>();
        _propertyDataLoader = new Mock<IPropertyDataLoader>();
        _masterDataProvider = new Mock<ICapitalValueMasterDataProvider>();
        _calculator = new Mock<ICapitalValueCalculator>();
        _persistenceService = new Mock<ICapitalValuePersistenceService>();
        _uow = new Mock<IUnitOfWork>();
        _mapper = AutoMapperTestHelper.CreateMapper();
        _options = new Mock<IOptions<CapitalValueOptions>>();
        _logger = new Mock<ILogger<CapitalValueService>>();

        // Setup default options
        _options.Setup(x => x.Value).Returns(new CapitalValueOptions
        {
            DefaultPolicyCode = "NETTAX",
            AutoCalculateIfNotExists = true,
            MaxPropertyDetailsPerRequest = 100
        });

        // Setup default transaction behavior
        _uow.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private CapitalValueService GetService()
    {
        return new CapitalValueService(
            _cvResultsService.Object,
            _policyTaxService.Object,
            _transMastService.Object,
            _propertyDataLoader.Object,
            _masterDataProvider.Object,
            _calculator.Object,
            _persistenceService.Object,
            _uow.Object,
             _mapper,
            _options.Object,
            _logger.Object
        );
    }

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_PropertyNotFound_ThrowsException()
    {
        // Arrange
        _propertyDataLoader.Setup(x => x.LoadPropertyDetailsAsync(999, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyDetailsEntity>());

        var service = GetService();

        // Act & Assert
        await Assert.ThrowsAsync<PropertyDetailsNotFoundException>(() =>
            service.GetAsync(999, CancellationToken.None));
    }

    [Fact]
    public async Task GetAsync_ExistingCVRecords_ReturnsData()
    {
        // Arrange
        int propertyId = 1;

        var propertyDetails = new List<PropertyDetailsEntity>
        {
            new PropertyDetailsEntity
            {
                Id = 10,
                PropertyId = propertyId,
                FloorId = 1,
                SubFloorId = null,
                ConstructionYear = "2020",
                AssessmentYear = "2024",
                ConstructionTypeId = 1,
                TypeOfUseId = 1,
                SubTypeOfUseId = null,
                CarpetAreaSqMeter = 100,
                BuiltupAreaSqMeter = 120,
                IsActive = true,
                TypeOfUse = new TypeOfUseEntity 
                { 
                    Id = 1, 
                    TypeOfUseGroupCV = new TypeOfUseGroupCVEntity { Id = 1, TypeOfUseGroupCVCode = "R" }
                }
            }
        };

        // Generate the actual hash that will be computed
        var expectedHash = NtisPlatform.Application.Services.CapitalValue.Utils.CVInputHashGenerator.GenerateHash(
            propertyDetails[0],
            hasLift: false,
            moujaId: 1,
            csn: "ABC123");

        var existingCVResults = new List<PropertyTaxCalculationCVResultsDto>
        {
            new PropertyTaxCalculationCVResultsDto
            {
                PropertyId = propertyId,
                PropertyDetailsId = 10,
                TaxId = 1,
                TaxName = "Property Tax",
                TaxPercentage = 15,
                TaxAmount = 50000,
                CapitalValue = 333333,
                BaseValue = 300000,
                FloorFactor = 1.2,
                UseFactor = 1.0,
                NTBFactor = 1.0,
                AgeFactor = 0.9,
                CVInputHash = expectedHash
            }
        };

        _propertyDataLoader.Setup(x => x.LoadPropertyDetailsAsync(propertyId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(propertyDetails);

        _propertyDataLoader.Setup(x => x.LoadPropertyAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyEntity { Id = propertyId, MoujaId = 1, CSN = "ABC123" });

        _propertyDataLoader.Setup(x => x.LoadLiftFlagAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _cvResultsService.Setup(x => x.GetByPropertyDetailsIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCVResults);

        _cvResultsService.Setup(x => x.GetCVInputHashAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHash);

        _propertyDataLoader.Setup(x => x.LoadPropertyDetailsAsync(propertyId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(propertyDetails);

        var service = GetService();

        // Act
        var result = await service.GetAsync(propertyId);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(propertyId, result[0].PropertyId);
        Assert.Equal(333333, result[0].CapitalValue);
    }

    [Fact]
    public async Task GetAsync_CVInputHashChanged_TriggersRecalculation()
    {
        // Arrange - Use separate options for this test to disable auto-calculation
        var testOptions = new Mock<IOptions<CapitalValueOptions>>();
        testOptions.Setup(x => x.Value).Returns(new CapitalValueOptions
        {
            DefaultPolicyCode = "NETTAX",
            AutoCalculateIfNotExists = false, // Disable to prevent full CreateAsync execution
            MaxPropertyDetailsPerRequest = 100
        });

        int propertyId = 1;

        var propertyDetails = new List<PropertyDetailsEntity>
        {
            new PropertyDetailsEntity
            {
                Id = 10,
                PropertyId = propertyId,
                FloorId = 1,
                SubFloorId = null,
                ConstructionYear = "2020",
                AssessmentYear = "2024",
                ConstructionTypeId = 1,
                TypeOfUseId = 1,
                SubTypeOfUseId = null,
                CarpetAreaSqMeter = 150, // Changed from 100 to 150
                BuiltupAreaSqMeter = 180, // Changed from 120 to 180
                IsActive = true,
                TypeOfUse = new TypeOfUseEntity 
                { 
                    Id = 1, 
                    TypeOfUseGroupCV = new TypeOfUseGroupCVEntity { Id = 1, TypeOfUseGroupCVCode = "R" }
                }
            }
        };

        // Old hash from before the change
        var oldPropertyDetails = new PropertyDetailsEntity
        {
            Id = 10,
            FloorId = 1,
            ConstructionYear = "2020",
            AssessmentYear = "2024",
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            CarpetAreaSqMeter = 100,
            BuiltupAreaSqMeter = 120
        };

        var oldHash = NtisPlatform.Application.Services.CapitalValue.Utils.CVInputHashGenerator.GenerateHash(
            oldPropertyDetails,
            hasLift: false,
            moujaId: 1,
            csn: "ABC123");

        var existingCVResults = new List<PropertyTaxCalculationCVResultsDto>
        {
            new PropertyTaxCalculationCVResultsDto
            {
                PropertyId = propertyId,
                PropertyDetailsId = 10,
                TaxId = 1,
                CapitalValue = 300000,
                CVInputHash = oldHash // Old hash before change
            }
        };

        _propertyDataLoader.Setup(x => x.LoadPropertyDetailsAsync(propertyId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(propertyDetails);

        _propertyDataLoader.Setup(x => x.LoadPropertyAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyEntity { Id = propertyId, MoujaId = 1, CSN = "ABC123" });

        _propertyDataLoader.Setup(x => x.LoadLiftFlagAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _cvResultsService.Setup(x => x.GetByPropertyDetailsIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCVResults);

        _cvResultsService.Setup(x => x.GetCVInputHashAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldHash); // Returns old hash, will mismatch with current

        _cvResultsService.Setup(x => x.DeactivateByPropertyDetailsIdAsync(10, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);


        // Setup mocks for PolicyTaxDetails and TransMast deactivation
        _policyTaxService.Setup(x => x.DeactivateByPropertyIdAsync(propertyId, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _transMastService.Setup(x => x.DeactivateByPropertyIdAsync(propertyId, "CV", null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new CapitalValueService(
            _cvResultsService.Object,
            _policyTaxService.Object,
            _transMastService.Object,
            _propertyDataLoader.Object,
            _masterDataProvider.Object,
            _calculator.Object,
            _persistenceService.Object,
            _uow.Object,
             _mapper,
            testOptions.Object,
            _logger.Object
        );

        // Act
        var result = await service.GetAsync(propertyId);

        // Assert
        _cvResultsService.Verify(x => x.DeactivateByPropertyDetailsIdAsync(10, null, It.IsAny<CancellationToken>()), Times.Once, 
            "Should deactivate old CV records when hash changes");


        _cvResultsService.Verify(x => x.GetCVInputHashAsync(10, It.IsAny<CancellationToken>()), Times.Once,
            "Should check existing hash");

        // Verify PolicyTaxDetails and TransMast are also deactivated when hash changes
        _policyTaxService.Verify(x => x.DeactivateByPropertyIdAsync(propertyId, null, It.IsAny<CancellationToken>()), Times.Once,
            "Should deactivate PolicyTaxDetails records when CV hash changes");
        _transMastService.Verify(x => x.DeactivateByPropertyIdAsync(propertyId, "CV", null, It.IsAny<CancellationToken>()), Times.Once,
            "Should deactivate TransMast records when CV hash changes");
    }

    [Fact]
    public async Task GetAsync_NoCVRecords_AutoCalculates()
    {
        // Arrange
        int propertyId = 1;

        var propertyDetails = new List<PropertyDetailsEntity>
        {
            new PropertyDetailsEntity
            {
                Id = 10,
                PropertyId = propertyId,
                IsActive = true,
                TypeOfUse = new TypeOfUseEntity 
                { 
                    Id = 1, 
                    TypeOfUseGroupCV = new TypeOfUseGroupCVEntity { Id = 1, TypeOfUseGroupCVCode = "R" }
                }
            }
        };

        _propertyDataLoader.Setup(x => x.LoadPropertyDetailsAsync(propertyId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(propertyDetails);

        _cvResultsService.Setup(x => x.GetByPropertyDetailsIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyTaxCalculationCVResultsDto>());

        // Setup for CreateAsync
        SetupSuccessfulCreate(propertyId, 10);

        var service = GetService();

        // Act
        var result = await service.GetAsync(propertyId);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAsync_MissingPropertyDetailsId_TriggersBackfill()
    {
        // Arrange
        int propertyId = 1;

        // But property has two PropertyDetails: 10 (existing) and 20 (new/missing)
        var allPropertyDetails = new List<PropertyDetailsEntity>
        {
            new PropertyDetailsEntity 
            { 
                Id = 10, 
                PropertyId = propertyId, 
                IsActive = true,
                TypeOfUse = new TypeOfUseEntity 
                { 
                    Id = 1, 
                    TypeOfUseGroupCV = new TypeOfUseGroupCVEntity { Id = 1, TypeOfUseGroupCVCode = "R" }
                }
            },
            new PropertyDetailsEntity 
            { 
                Id = 20, 
                PropertyId = propertyId, 
                IsActive = true,
                TypeOfUse = new TypeOfUseEntity 
                { 
                    Id = 1, 
                    TypeOfUseGroupCV = new TypeOfUseGroupCVEntity { Id = 1, TypeOfUseGroupCVCode = "R" }
                }
            }
        };

        // Initial CV results exist for PropertyDetailsId=10 only
        var existingCVResults = new List<PropertyTaxCalculationCVResultsDto>
        {
            new PropertyTaxCalculationCVResultsDto
            {
                PropertyId = propertyId,
                PropertyDetailsId = 10,
                TaxId = 1,
                CapitalValue = 500000
            }
        };

        _propertyDataLoader.Setup(x => x.LoadPropertyDetailsAsync(propertyId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPropertyDetails);

        _cvResultsService.Setup(x => x.GetByPropertyDetailsIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCVResults);

        _cvResultsService.Setup(x => x.GetByPropertyDetailsIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyTaxCalculationCVResultsDto>());

        _propertyDataLoader.Setup(x => x.LoadPropertyDetailsAsync(propertyId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyDetailsEntity> { allPropertyDetails[0] });

        _propertyDataLoader.Setup(x => x.LoadPropertyDetailsAsync(propertyId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyDetailsEntity> { allPropertyDetails[1] });

        // Setup for CreateAsync (called for missing PropertyDetailsId=20)
        SetupSuccessfulCreate(propertyId, 20);

        // Setup for RecalculateAggregatedTotalsAsync
        var property = new PropertyEntity { Id = propertyId, MoujaId = 1, CSN = "A1", IsActive = true };
        _propertyDataLoader.Setup(x => x.LoadPropertyAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);

        var masterData = new MasterDataContext
        {
            TaxTotalHead = new TaxMasterEntity { Id = 999, TaxName = "TaxTotal" },
            YearRanges = new List<AssessmentYearRangeCVEntity>
            {
                new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2015, ToYear = 2025, IsActive = true }
            },
            RateMasters = new List<RateMasterForCVEntity>
            {
                new RateMasterForCVEntity { Id = 1, AssessmentYearRangeId = 1, TypeOfUseGroupCVId = 1, RateAmount = 10000 }
            }
        };
        _masterDataProvider.Setup(x => x.LoadMasterDataAsync(1, "A1", It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(masterData);

        var financeYear = new YearMasterEntity { Id = 1, Year = 2024, IsActive = true };
        _propertyDataLoader.Setup(x => x.LoadFinanceYearAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(financeYear);

        _cvResultsService.Setup(x => x.GetByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyTaxCalculationCVResultsDto>
            {
                new PropertyTaxCalculationCVResultsDto { PropertyId = propertyId, PropertyDetailsId = 10, TaxId = 1, CapitalValue = 500000, TaxAmount = 75000 },
                new PropertyTaxCalculationCVResultsDto { PropertyId = propertyId, PropertyDetailsId = 10, TaxId = 999, CapitalValue = 500000, TaxAmount = 75000 }
            });

        _policyTaxService.Setup(x => x.GetByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PolicyTaxDetailsCVDto>());

        _transMastService.Setup(x => x.GetByPropertyIdAsync(propertyId, "CV", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TransMastDto>());

        var service = GetService();

        // Act
        var result = await service.GetAsync(propertyId);

        // Assert
        // Verify CreateAsync was called for the missing PropertyDetailsId=20
        _persistenceService.Verify(x => x.PersistCVResultsAsync(
            It.Is<List<CreatePropertyTaxCalculationCVResultsDto>>(list =>
                list.Any(cv => cv.PropertyDetailsId == 20)),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify RecalculateAggregatedTotalsAsync was triggered (calls PersistAggregatedDataAsync)
        _persistenceService.Verify(x => x.PersistAggregatedDataAsync(
            propertyId,
            It.IsAny<YearMasterEntity>(),
            It.IsAny<Dictionary<int, (decimal TotalTax, decimal TotalCV)>>(),
            It.IsAny<Dictionary<int, PolicyTaxDetailsCVDto>>(),
            It.IsAny<Dictionary<(int PropertyId, int FinanceYearId, int TaxId), TransMastDto>>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_PropertyNotFound_Throws()
    {
        // Arrange
        _propertyDataLoader.Setup(x => x.LoadPropertyAsync(999, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Property not found"));

        var service = GetService();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.CreateAsync(new CreateCapitalValueDto { PropertyId = 999 }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_PropertyDetailsNotFound_Throws()
    {
        // Arrange
        int propertyId = 1;
        var property = new PropertyEntity { Id = propertyId, MoujaId = 1, CSN = "A1", IsActive = true };

        _propertyDataLoader.Setup(x => x.LoadPropertyAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);

        _propertyDataLoader.Setup(x => x.LoadPropertyDetailsAsync(propertyId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyDetailsEntity>());

        var service = GetService();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.CreateAsync(new CreateCapitalValueDto { PropertyId = propertyId }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_SinglePropertyDetail_Success()
    {
        // Arrange
        int propertyId = 1;
        int propertyDetailsId = 10;

        SetupSuccessfulCreate(propertyId, propertyDetailsId);

        var service = GetService();

        // Act
        var result = await service.CreateAsync(
            new CreateCapitalValueDto { PropertyId = propertyId, PropertyDetailsId = propertyDetailsId },
            CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(propertyId, result[0].PropertyId);
        _persistenceService.Verify(x => x.PersistCVResultsAsync(
            It.IsAny<List<CreatePropertyTaxCalculationCVResultsDto>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MultiplePropertyDetails_CalculatesAll()
    {
        // Arrange
        int propertyId = 1;

        SetupSuccessfulCreate(propertyId, null, propertyDetailsCount: 3);

        var service = GetService();

        // Act
        var result = await service.CreateAsync(
            new CreateCapitalValueDto { PropertyId = propertyId },
            CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        _calculator.Verify(x => x.Calculate(
            It.IsAny<PropertyDetailsEntity>(),
            It.IsAny<MasterDataContext>(),
            It.IsAny<bool>(),
            propertyId,
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<decimal?>()), Times.Exactly(3));
    }

    [Fact]
    public async Task CreateAsync_SkipsExistingCVRecords()
    {
        // Arrange
        int propertyId = 1;
        int propertyDetailsId = 10;

        var existingCVResults = new List<PropertyTaxCalculationCVResultsDto>
        {
            new PropertyTaxCalculationCVResultsDto
            {
                PropertyId = propertyId,
                PropertyDetailsId = propertyDetailsId,
                TaxId = 1 // Tax 1 already exists
            }
        };

        SetupSuccessfulCreate(propertyId, propertyDetailsId, existingCVResults);

        var service = GetService();

        // Act
        var result = await service.CreateAsync(
            new CreateCapitalValueDto { PropertyId = propertyId, PropertyDetailsId = propertyDetailsId },
            CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);
        _persistenceService.Verify(x => x.PersistCVResultsAsync(
            It.Is<List<CreatePropertyTaxCalculationCVResultsDto>>(list =>
                list.All(cv => !(cv.PropertyDetailsId == propertyDetailsId && cv.TaxId == 1))),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_FullPropertyCalculation_PersistsAggregatedData()
    {
        // Arrange
        int propertyId = 1;

        SetupSuccessfulCreate(propertyId, null);

        var service = GetService();

        // Act
        var result = await service.CreateAsync(
            new CreateCapitalValueDto { PropertyId = propertyId, PropertyDetailsId = 0 },
            CancellationToken.None);

        // Assert
        _persistenceService.Verify(x => x.PersistAggregatedDataAsync(
            propertyId,
            It.IsAny<YearMasterEntity>(),
            It.IsAny<Dictionary<int, (decimal TotalTax, decimal TotalCV)>>(),
            It.IsAny<Dictionary<int, PolicyTaxDetailsCVDto>>(),
            It.IsAny<Dictionary<(int PropertyId, int FinanceYearId, int TaxId), TransMastDto>>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SinglePropertyDetail_DoesNotPersistAggregatedData()
    {
        // Arrange
        int propertyId = 1;
        int propertyDetailsId = 10;

        SetupSuccessfulCreate(propertyId, propertyDetailsId);

        var service = GetService();

        // Act
        await service.CreateAsync(
            new CreateCapitalValueDto { PropertyId = propertyId, PropertyDetailsId = propertyDetailsId },
            CancellationToken.None);

        // Assert
        _persistenceService.Verify(x => x.PersistAggregatedDataAsync(
            It.IsAny<int>(),
            It.IsAny<YearMasterEntity>(),
            It.IsAny<Dictionary<int, (decimal TotalTax, decimal TotalCV)>>(),
            It.IsAny<Dictionary<int, PolicyTaxDetailsCVDto>>(),
            It.IsAny<Dictionary<(int PropertyId, int FinanceYearId, int TaxId), TransMastDto>>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UsesDefaultPolicyCode_WhenNotProvided()
    {
        // Arrange
        int propertyId = 1;

        SetupSuccessfulCreate(propertyId, null);

        var service = GetService();

        // Act
        await service.CreateAsync(
            new CreateCapitalValueDto { PropertyId = propertyId, PropertyDetailsId = 0 },
            CancellationToken.None);

        // Assert
        _persistenceService.Verify(x => x.PersistAggregatedDataAsync(
            propertyId,
            It.IsAny<YearMasterEntity>(),
            It.IsAny<Dictionary<int, (decimal TotalTax, decimal TotalCV)>>(),
            It.IsAny<Dictionary<int, PolicyTaxDetailsCVDto>>(),
            It.IsAny<Dictionary<(int PropertyId, int FinanceYearId, int TaxId), TransMastDto>>(),
            "NETTAX", // Default policy code
            It.IsAny<DateTime>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UsesProvidedPolicyCode_WhenGiven()
    {
        // Arrange
        int propertyId = 1;

        SetupSuccessfulCreate(propertyId, null);

        var service = GetService();

        // Act
        await service.CreateAsync(
            new CreateCapitalValueDto
            {
                PropertyId = propertyId,
                PropertyDetailsId = 0,
                PolicyCode = "CUSTOM"
            },
            CancellationToken.None);

        // Assert
        _persistenceService.Verify(x => x.PersistAggregatedDataAsync(
            propertyId,
            It.IsAny<YearMasterEntity>(),
            It.IsAny<Dictionary<int, (decimal TotalTax, decimal TotalCV)>>(),
            It.IsAny<Dictionary<int, PolicyTaxDetailsCVDto>>(),
            It.IsAny<Dictionary<(int PropertyId, int FinanceYearId, int TaxId), TransMastDto>>(),
            "CUSTOM",
            It.IsAny<DateTime>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ExceptionDuringCalculation_PropagatesException()
    {
        // Arrange
        int propertyId = 1;

        _propertyDataLoader.Setup(x => x.LoadPropertyAsync(propertyId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var service = GetService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            service.CreateAsync(new CreateCapitalValueDto { PropertyId = propertyId }, CancellationToken.None));

        Assert.Contains("Database connection failed", exception.Message);
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulCreate(
        int propertyId,
        int? propertyDetailsId = null,
        List<PropertyTaxCalculationCVResultsDto>? existingCVResults = null,
        int propertyDetailsCount = 1)
    {
        var property = new PropertyEntity
        {
            Id = propertyId,
            MoujaId = 1,
            CSN = "A1",
            IsActive = true
        };

        var propertyDetailsList = new List<PropertyDetailsEntity>();
        for (int i = 0; i < propertyDetailsCount; i++)
        {
            propertyDetailsList.Add(new PropertyDetailsEntity
            {
                Id = propertyDetailsId ?? (10 + i),
                PropertyId = propertyId,
                TypeOfUseId = 1,
                ConstructionTypeId = 1,
                AssessmentYear = "2020",
                ConstructionYear = "2015",
                CarpetAreaSqMeter = 100,
                IsActive = true,
                TypeOfUse = new TypeOfUseEntity
                {
                    Id = 1,
                    TypeOfUseGroupCVId = 1,
                    TypeOfUseGroupCV = new TypeOfUseGroupCVEntity
                    {
                        Id = 1,
                        TypeOfUseGroupCVCode = "R"
                    }
                }
            });
        }

        var masterData = new MasterDataContext
        {
            RateMasters = new List<RateMasterForCVEntity>
            {
                new RateMasterForCVEntity { Id = 1, AssessmentYearRangeId = 1, TypeOfUseGroupCVId = 1, RateAmount = 10000 }
            },
            TaxTotalHead = new TaxMasterEntity { Id = 999, TaxName = "TaxTotal" },
            NatureFactors = new Dictionary<(int ConstructionTypeId, int YearRangeCVId), decimal?>(),
            UseFactors = new Dictionary<(int TypeOfUseId, int YearRangeCVId, int SubTypeOfUseId), decimal?>(),
            AgeFactors = new List<AgeFactorCVMasterEntity>(),
            FloorFactors = new Dictionary<(int FloorId, int YearRangeCVId), FloorFactorCVMasterEntity>(),
            YearRanges = new List<AssessmentYearRangeCVEntity>
            {
                new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2015, ToYear = 2025, IsActive = true }
            },
            TaxData = new List<TaxPercentageMasterCVEntity>
            {
                new TaxPercentageMasterCVEntity { TaxId = 1, TaxPercentage = 15, IsActive = true },
                new TaxPercentageMasterCVEntity { TaxId = 2, TaxPercentage = 5, IsActive = true }
            }
        };

        var yearRange = new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2015, ToYear = 2025 };

        var cvDto = new CapitalValueDto
        {
            PropertyId = propertyId,
            PropertyDetailsId = propertyDetailsId ?? 10,
            CapitalValue = 500000,
            BaseValue = 300000,
            FloorFactor = 1.2,
            UseFactor = 1.0,
            NTBFactor = 1.0,
            AgeFactor = 0.9,
            Taxes = new List<TaxHeadDto>
            {
                new TaxHeadDto { TaxId = 1, TaxName = "Property Tax", Percentage = 15, Amount = 75000 },
                new TaxHeadDto { TaxId = 2, TaxName = "Education Tax", Percentage = 5, Amount = 25000 }
            }
        };

        var financeYear = new YearMasterEntity { Id = 1, Year = 2024, IsActive = true };

        _propertyDataLoader.Setup(x => x.LoadPropertyAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);

        // Setup for specific property details ID
        if (propertyDetailsId.HasValue && propertyDetailsId.Value > 0)
        {
            _propertyDataLoader.Setup(x => x.LoadPropertyDetailsAsync(propertyId, propertyDetailsId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(propertyDetailsList);
        }

        // Setup for full property calculation (null or 0)
        _propertyDataLoader.Setup(x => x.LoadPropertyDetailsAsync(propertyId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(propertyDetailsList);

        _propertyDataLoader.Setup(x => x.LoadPropertyDetailsAsync(propertyId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(propertyDetailsList);

        _propertyDataLoader.Setup(x => x.LoadLiftFlagAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _propertyDataLoader.Setup(x => x.LoadFinanceYearAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(financeYear);

        _masterDataProvider.Setup(x => x.LoadMasterDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(masterData);

        // Create factor entities for ID storage
        var floorFactorEntity = new FloorFactorCVMasterEntity 
        { 
            Id = 1, 
            FloorId = 1, 
            FactorWithLift = 1.2m, 
            FactorWithoutLift = 1.0m, 
            YearRangeCVId = 1 
        };

        var ageFactorEntity = new AgeFactorCVMasterEntity 
        { 
            Id = 2, 
            ConstructionTypeId = 1,
            AgeFrom = 0, 
            AgeTo = 10, 
            Factor = 0.9m,
            YearRangeCVId = 1
        };

        var natureFactorEntity = new NatureFactorCVMasterEntity 
        { 
            Id = 3, 
            ConstructionTypeId = 1, 
            Factor = 1.0m, 
            YearRangeCVId = 1 
        };

        var useFactorEntity = new UseFactorCVMasterEntity 
        { 
            Id = 4, 
            TypeOfUseId = 1, 
            SubTypeOfUseId = 1, 
            Factor = 1.0m, 
            YearRangeCVId = 1 
        };

        _calculator.Setup(x => x.Calculate(
            It.IsAny<PropertyDetailsEntity>(),
            It.IsAny<MasterDataContext>(),
            It.IsAny<bool>(),
            propertyId,
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<decimal?>()))
            .Returns(new CapitalValueCalculationResult
            {
                Result = cvDto,
                YearRange = yearRange,
                RateMaster = masterData.RateMasters[0],
                FloorFactorEntity = floorFactorEntity,
                AgeFactorEntity = ageFactorEntity,
                NatureFactorEntity = natureFactorEntity,
                UseFactorEntity = useFactorEntity
            });

        _cvResultsService.Setup(x => x.GetByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCVResults ?? new List<PropertyTaxCalculationCVResultsDto>());

        _policyTaxService.Setup(x => x.GetByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PolicyTaxDetailsCVDto>());

        _transMastService.Setup(x => x.GetByPropertyIdAsync(propertyId, "CV", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TransMastDto>());

        _persistenceService.Setup(x => x.PersistCVResultsAsync(
            It.IsAny<List<CreatePropertyTaxCalculationCVResultsDto>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkResult<PropertyTaxCalculationCVResultsDto>(1, 0, new List<PropertyTaxCalculationCVResultsDto>()));

        _persistenceService.Setup(x => x.PersistAggregatedDataAsync(
            It.IsAny<int>(),
            It.IsAny<YearMasterEntity>(),
            It.IsAny<Dictionary<int, (decimal TotalTax, decimal TotalCV)>>(),
            It.IsAny<Dictionary<int, PolicyTaxDetailsCVDto>>(),
            It.IsAny<Dictionary<(int PropertyId, int FinanceYearId, int TaxId), TransMastDto>>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    #endregion

    #region Rule Engine Integration Tests

    [Fact]
    public async Task CreateAsync_WithRuleEngineServices_AppliesRulesAndSavesLogs()
    {
        // Arrange
        int propertyId = 1;
        int propertyDetailsId = 10;
        int financeYearVal = 2026;

        var mockLoader = new Mock<IPropertyContextLoaderService>();
        var mockApplier = new Mock<IRuleApplierService>();

        var property = new PropertyEntity { Id = propertyId, MoujaId = 5, CSN = "CSN123" };
        var detail = new PropertyDetailsEntity 
        { 
            Id = propertyDetailsId, 
            PropertyId = propertyId, 
            AssessmentYear = "2026",
            TypeOfUse = new TypeOfUseEntity
            {
                Id = 1,
                TypeOfUseGroupCV = new TypeOfUseGroupCVEntity { TypeOfUseGroupCVCode = "R", IsFloorWiseRateApplicable = false }
            }
        };

        var detailsList = new List<PropertyDetailsEntity> { detail };
        var propertyContext = new PropertyCalculationContext
        {
            Property = property,
            Details = detailsList
        };

        var financeYear = new YearMasterEntity { Id = 1, Year = financeYearVal };
        var masterData = new MasterDataContext
        {
            YearRanges = new List<AssessmentYearRangeCVEntity> { new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2025, ToYear = 2027 } },
            RateMasters = new List<RateMasterForCVEntity> { new RateMasterForCVEntity { Id = 100, AssessmentYearRangeId = 1, TypeOfUseGroupCVId = 0, RateAmount = 500m } },
            TaxTotalHead = new TaxMasterEntity { Id = 99 }
        };

        _propertyDataLoader.Setup(x => x.LoadPropertyAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(property);
        _propertyDataLoader.Setup(x => x.LoadPropertyDetailsAsync(propertyId, propertyDetailsId, It.IsAny<CancellationToken>())).ReturnsAsync(detailsList);
        _propertyDataLoader.Setup(x => x.LoadFinanceYearAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(financeYear);
        _propertyDataLoader.Setup(x => x.LoadLiftFlagAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _masterDataProvider.Setup(x => x.LoadMasterDataAsync(5, "CSN123", It.IsAny<List<int>?>(), It.IsAny<CancellationToken>())).ReturnsAsync(masterData);

        mockLoader.Setup(x => x.LoadPropertyContextAsync(propertyId, financeYearVal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(propertyContext);

        var appliedRulesList = new List<RuleApplicationTraceEntry>
        {
            new RuleApplicationTraceEntry { RuleCode = "R1", RuleName = "Test Rule", CumulativeValue = 450m }
        };
        mockApplier.Setup(x => x.ApplyRulesAsync(It.IsAny<RuleApplierContext>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RuleApplicationResult { FinalValue = 450m, AppliedRules = appliedRulesList });

        var cvDto = new CapitalValueDto
        {
            PropertyId = propertyId,
            PropertyDetailsId = propertyDetailsId,
            CapitalValue = 90000m,
            BaseValue = 45000,
            Taxes = new List<TaxHeadDto> { new TaxHeadDto { TaxId = 1, Percentage = 10, Amount = 100 } }
        };

        _calculator.Setup(x => x.Calculate(
            detail,
            masterData,
            false,
            propertyId,
            5,
            "CSN123",
            450m)) // verify ruleAdjustedRate = 450m is passed
            .Returns(new CapitalValueCalculationResult
            {
                Result = cvDto,
                YearRange = masterData.YearRanges[0],
                RateMaster = masterData.RateMasters[0]
            });

        _cvResultsService.Setup(x => x.GetByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<PropertyTaxCalculationCVResultsDto>());
        _cvResultsService.Setup(x => x.GetByPropertyDetailsIdAsync(propertyDetailsId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<PropertyTaxCalculationCVResultsDto>());

        var customService = new CapitalValueService(
            _cvResultsService.Object,
            _policyTaxService.Object,
            _transMastService.Object,
            _propertyDataLoader.Object,
            _masterDataProvider.Object,
            _calculator.Object,
            _persistenceService.Object,
            _uow.Object,
            _mapper,
            _options.Object,
            _logger.Object,
            mockLoader.Object,
            mockApplier.Object
        );

        // Act
        var dto = new CreateCapitalValueDto { PropertyId = propertyId, PropertyDetailsId = propertyDetailsId };
        var result = await customService.CreateAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        mockLoader.Verify(x => x.LoadPropertyContextAsync(propertyId, financeYearVal, It.IsAny<CancellationToken>()), Times.Once);
        mockApplier.Verify(x => x.ApplyRulesAsync(It.Is<RuleApplierContext>(c => c.InitialValue == 500m && c.Category == "CV"), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _persistenceService.Verify(x => x.SaveRuleApplicationLogAsync(
            propertyId,
            financeYearVal,
            propertyDetailsId,
            It.Is<List<RuleApplicationTraceEntry>>(list => 
                list != null && 
                list.Count == appliedRulesList.Count && 
                list.Zip(appliedRulesList).All(pair => 
                    pair.First.RuleCode == pair.Second.RuleCode && 
                    pair.First.RuleName == pair.Second.RuleName && 
                    pair.First.CumulativeValue == pair.Second.CumulativeValue)),
            "CV",
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidAssessmentYear_ThrowsInvalidPropertyDataException()
    {
        // Arrange
        int propertyId = 1;
        int propertyDetailsId = 10;

        var property = new PropertyEntity { Id = propertyId, MoujaId = 5, CSN = "CSN123" };
        var detail = new PropertyDetailsEntity 
        { 
            Id = propertyDetailsId, 
            PropertyId = propertyId, 
            AssessmentYear = "INVALID", // Invalid
            TypeOfUse = new TypeOfUseEntity
            {
                Id = 1,
                TypeOfUseGroupCV = new TypeOfUseGroupCVEntity { TypeOfUseGroupCVCode = "R", IsFloorWiseRateApplicable = false }
            }
        };

        var detailsList = new List<PropertyDetailsEntity> { detail };
        var financeYear = new YearMasterEntity { Id = 1, Year = 2026 };
        var masterData = new MasterDataContext
        {
            YearRanges = new List<AssessmentYearRangeCVEntity>(),
            RateMasters = new List<RateMasterForCVEntity>(),
            TaxTotalHead = new TaxMasterEntity { Id = 99 }
        };

        _propertyDataLoader.Setup(x => x.LoadPropertyAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(property);
        _propertyDataLoader.Setup(x => x.LoadPropertyDetailsAsync(propertyId, propertyDetailsId, It.IsAny<CancellationToken>())).ReturnsAsync(detailsList);
        _propertyDataLoader.Setup(x => x.LoadFinanceYearAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(financeYear);
        _propertyDataLoader.Setup(x => x.LoadLiftFlagAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _masterDataProvider.Setup(x => x.LoadMasterDataAsync(5, "CSN123", It.IsAny<List<int>?>(), It.IsAny<CancellationToken>())).ReturnsAsync(masterData);
        _cvResultsService.Setup(x => x.GetByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyTaxCalculationCVResultsDto>());
        _cvResultsService.Setup(x => x.GetByPropertyDetailsIdAsync(propertyDetailsId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyTaxCalculationCVResultsDto>());

        var service = GetService();

        // Act & Assert
        var dto = new CreateCapitalValueDto { PropertyId = propertyId, PropertyDetailsId = propertyDetailsId };
        await Assert.ThrowsAsync<InvalidPropertyDataException>(() => service.CreateAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WithYearRangeNotFound_ThrowsYearRangeNotFoundException()
    {
        // Arrange
        int propertyId = 1;
        int propertyDetailsId = 10;

        var property = new PropertyEntity { Id = propertyId, MoujaId = 5, CSN = "CSN123" };
        var detail = new PropertyDetailsEntity 
        { 
            Id = propertyDetailsId, 
            PropertyId = propertyId, 
            AssessmentYear = "2026", // Valid year
            TypeOfUse = new TypeOfUseEntity
            {
                Id = 1,
                TypeOfUseGroupCV = new TypeOfUseGroupCVEntity { TypeOfUseGroupCVCode = "R", IsFloorWiseRateApplicable = false }
            }
        };

        var detailsList = new List<PropertyDetailsEntity> { detail };
        var financeYear = new YearMasterEntity { Id = 1, Year = 2026 };
        var masterData = new MasterDataContext
        {
            // Empty YearRanges, so 2026 will not be found
            YearRanges = new List<AssessmentYearRangeCVEntity>(),
            RateMasters = new List<RateMasterForCVEntity>(),
            TaxTotalHead = new TaxMasterEntity { Id = 99 }
        };

        _propertyDataLoader.Setup(x => x.LoadPropertyAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(property);
        _propertyDataLoader.Setup(x => x.LoadPropertyDetailsAsync(propertyId, propertyDetailsId, It.IsAny<CancellationToken>())).ReturnsAsync(detailsList);
        _propertyDataLoader.Setup(x => x.LoadFinanceYearAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(financeYear);
        _propertyDataLoader.Setup(x => x.LoadLiftFlagAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _masterDataProvider.Setup(x => x.LoadMasterDataAsync(5, "CSN123", It.IsAny<List<int>?>(), It.IsAny<CancellationToken>())).ReturnsAsync(masterData);
        _cvResultsService.Setup(x => x.GetByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyTaxCalculationCVResultsDto>());
        _cvResultsService.Setup(x => x.GetByPropertyDetailsIdAsync(propertyDetailsId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyTaxCalculationCVResultsDto>());

        var service = GetService();

        // Act & Assert
        var dto = new CreateCapitalValueDto { PropertyId = propertyId, PropertyDetailsId = propertyDetailsId };
        await Assert.ThrowsAsync<YearRangeNotFoundException>(() => service.CreateAsync(dto, CancellationToken.None));
    }

    #endregion
}

