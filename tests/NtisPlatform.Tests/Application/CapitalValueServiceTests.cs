using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.CapitalValue;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Tests.Helpers;
using Xunit;

namespace NtisPlatform.Tests.Application;

#region DTO Tests

public class CapitalValueDtoTests
{
    [Fact]
    public void CapitalValueDto_GetSet_Works()
    {
        var dto = new CapitalValueDto
        {
            PropertyId = 1,
            PropertyDetailsId = 10,
            CapitalValue = 1000,
            BaseValue = 500,
            FloorFactor = 1.2,
            SDRR = 100,
            UseFactor = 1,
            NTBFactor = 1,
            AgeFactor = 1
        };

        Assert.Equal(1, dto.PropertyId);
        Assert.Equal(10, dto.PropertyDetailsId);
        Assert.Equal(1000, dto.CapitalValue);
        Assert.Equal(500, dto.BaseValue);
    }
}

public class CreateCapitalValueDtoTests
{
    [Fact]
    public void CreateDto_GetSet_Works()
    {
        var dto = new CreateCapitalValueDto
        {
            PropertyId = 1,
            PropertyDetailsId = 10
        };

        Assert.Equal(1, dto.PropertyId);
        Assert.Equal(10, dto.PropertyDetailsId);
    }
}

#endregion

#region Controller Tests

public class CapitalValueControllerTests
{
    private readonly Mock<ICapitalValueService> _mockService;
    private readonly CapitalValueController _controller;

    public CapitalValueControllerTests()
    {
        _mockService = new Mock<ICapitalValueService>();
        _controller = new CapitalValueController(_mockService.Object);
    }

    [Fact]
    public async Task Get_ReturnsOk()
    {
        var mockData = new List<CapitalValueDto>
        {
            new CapitalValueDto { PropertyId = 1, PropertyDetailsId = 10 }
        };

        _mockService.Setup(x => x.GetAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockData);

        var result = await _controller.Get(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<CapitalValueDto>>(ok.Value);

        Assert.Single(data);
    }
 
}

#endregion

#region Service Tests

public class CapitalValueServiceTests
{
    private readonly Mock<IRepository<PropertyTaxCalculationCVResultsEntity, long>> _cvRepo = new();
    private readonly Mock<IRepository<PropertyEntity, int>> _propertyRepo = new();
    private readonly Mock<IRepository<PropertyDetailsEntity, int>> _pdRepo = new();
    private readonly Mock<IRepository<FlagMasterEntity, int>> _flagRepo = new();
    private readonly Mock<IRepository<TaxMasterEntity, int>> _taxMasterRepo = new();
    private readonly Mock<IRepository<FloorFactorCVMasterEntity, int>> _floorFactorRepo = new();
    private readonly Mock<IRepository<RateMasterForCVEntity, int>> _rateRepo = new();
    private readonly Mock<IRepository<NatureFactorCVMasterEntity, int>> _natureRepo = new();
    private readonly Mock<IRepository<UseFactorCVMasterEntity, int>> _useRepo = new();
    private readonly Mock<IRepository<AgeFactorCVMasterEntity, int>> _ageRepo = new();
    private readonly Mock<IRepository<AssessmentYearRangeCVEntity, int>> _assessmentYearRepo = new();
    private readonly Mock<IRepository<TaxPercentageMasterCVEntity, int>> _taxPercentageRepo = new();
    private readonly Mock<IRepository<CSNDetailsEntity, int>> _csnDetailsRepo = new();
    private readonly Mock<IRepository<PolicyTaxDetailsCVEntity, int>> _policyTaxDetailsCVRepo = new();
    private readonly Mock<IRepository<TransMastCVEntity, int>> _transMastCVRepo = new();
    private readonly Mock<IRepository<YearMasterEntity, int>> _yearMasterRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<CapitalValueService>> _logger = new();

    public CapitalValueServiceTests()
    {
        // Setup UnitOfWork transaction methods to return completed tasks
        _uow.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uow.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uow.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup AddAsync to return the passed entity as a completed task
        _cvRepo.Setup(x => x.AddAsync(It.IsAny<PropertyTaxCalculationCVResultsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTaxCalculationCVResultsEntity entity, CancellationToken ct) => entity);
    }

    private CapitalValueService GetService()
    {
        var mockLogger = new Mock<ILogger<CapitalValueService>>();

        return new CapitalValueService(
            _cvRepo.Object,
            _rateRepo.Object,
            _natureRepo.Object,
            _useRepo.Object,
            _ageRepo.Object,
            _floorFactorRepo.Object,
            _propertyRepo.Object,
            _pdRepo.Object,
            _flagRepo.Object,
            _assessmentYearRepo.Object,
            _taxPercentageRepo.Object,
            _taxMasterRepo.Object,
            _csnDetailsRepo.Object,
            _policyTaxDetailsCVRepo.Object,
            _transMastCVRepo.Object,
            _yearMasterRepo.Object,
            _uow.Object,
            AutoMapperTestHelper.CreateMapper(),
            mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateAsync_PropertyNotFound_Throws()
    {
        _propertyRepo.Setup(x => x.GetQueryable())
            .Returns(new List<PropertyEntity>().BuildMockDbSet().Object);

        var service = GetService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateCapitalValueDto { PropertyId = 1 }));

        // Verify transaction was started but rolled back due to exception
        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_PropertyDetailsNotFound_Throws()
    {
        var propertyList = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 1, MoujaId = 1, CSN = "A1", IsActive = true }
        };

        _propertyRepo.Setup(x => x.GetQueryable())
            .Returns(propertyList.BuildMockDbSet().Object);



        _pdRepo.Setup(x => x.GetQueryable())
            .Returns(new List<PropertyDetailsEntity>().BuildMockDbSet().Object);

        var service = GetService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateCapitalValueDto { PropertyId = 1 }));
    }

    [Fact]
    public async Task CreateAsync_NullMoujaId_Throws()
    {
        var propertyList = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 1, MoujaId = null, CSN = "A1", IsActive = true } // MoujaId is null
        };

        _propertyRepo.Setup(x => x.GetQueryable())
            .Returns(propertyList.BuildMockDbSet().Object);

        var service = GetService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateCapitalValueDto { PropertyId = 1 }));

        Assert.Contains("MoujaId", exception.Message);
        Assert.Contains("required for rate calculation", exception.Message);

        // Verify transaction was started but rolled back due to exception
        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NullCSN_Throws()
    {
        var propertyList = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 1, MoujaId = 1, CSN = null, IsActive = true } // CSN is null
        };

        _propertyRepo.Setup(x => x.GetQueryable())
            .Returns(propertyList.BuildMockDbSet().Object);

        var service = GetService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateCapitalValueDto { PropertyId = 1 }));

        Assert.Contains("CSN", exception.Message);
        Assert.Contains("required for rate calculation", exception.Message);

        // Verify transaction was started but rolled back due to exception
        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SkipExistingTaxRows()
    {
        var propertyId = 1;

        _propertyRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyEntity>
            {
            new PropertyEntity
            {
                Id = propertyId,
                MoujaId = 1,
                CSN = "A1",
                IsActive = true
            }
            }.BuildMockDbSet().Object);

        _pdRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyDetailsEntity>
            {
            new PropertyDetailsEntity
            {
                Id = 10,
                PropertyId = propertyId,
                AssessmentYear = "2020",
                ConstructionYear = "2015",
                TypeOfUseId = 1,
                TypeOfUse = new TypeOfUseEntity
                {
                    Id = 1,
                    Type = "R",
                    TypeOfUseGroupId = 1,
                    TypeOfUseGroup = new TypeOfUseGroupEntity
                    {
                        Id = 1,
                        IsActive = true,
                        IsFloorWiseRateApplicable = true
                    },
                    IsActive = true
                },
                FloorId = 1,
                Floor = new FloorEntity
                {
                    Id = 1,
                    FloorGroupId = 0,
                    IsActive = true
                },
                ConstructionTypeId = 1,
                CarpetAreaSqMeter = 100,
                IsActive = true
            }
            }.BuildMockDbSet().Object);

        _cvRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyTaxCalculationCVResultsEntity>
            {
            new PropertyTaxCalculationCVResultsEntity
            {
                PropertyId = propertyId,
                PropertyDetailsId = 10,
                TaxId = 1,
                IsActive = true
            }
            }.BuildMockDbSet().Object);

        _flagRepo.Setup(x => x.GetQueryable()).Returns(
            new List<FlagMasterEntity>().BuildMockDbSet().Object);

        _floorFactorRepo.Setup(x => x.GetQueryable()).Returns(
            new List<FloorFactorCVMasterEntity>().BuildMockDbSet().Object);

        _assessmentYearRepo.Setup(x => x.GetQueryable()).Returns(
            new List<AssessmentYearRangeCVEntity>
            {
            new AssessmentYearRangeCVEntity
            {
                Id = 1,
                FromYear = 2000,
                ToYear = 2030,
                IsActive = true
            }
            }.BuildMockDbSet().Object);

        _natureRepo.Setup(x => x.GetQueryable()).Returns(
            new List<NatureFactorCVMasterEntity>().BuildMockDbSet().Object);

        _useRepo.Setup(x => x.GetQueryable()).Returns(
            new List<UseFactorCVMasterEntity>().BuildMockDbSet().Object);

        _ageRepo.Setup(x => x.GetQueryable()).Returns(
            new List<AgeFactorCVMasterEntity>().BuildMockDbSet().Object);

        _csnDetailsRepo.Setup(x => x.GetQueryable()).Returns(
            new List<CSNDetailsEntity>
            {
            new CSNDetailsEntity
            {
                Id = 1,
                RateMasterCVId = 1,
                CSN = "A1",
                IsActive = true
            }
            }.BuildMockDbSet().Object);

        _rateRepo.Setup(x => x.GetQueryable()).Returns(
            new List<RateMasterForCVEntity>
            {
            new RateMasterForCVEntity
            {
                Id = 1,
                SubZoneId = 1,
                TypeOfUseGroupCVId = 1,
                FloorGroupId = 0,
                AssessmentYearRangeId = 1,
                RateAmount = 10,
                IsActive = true
            }
            }.BuildMockDbSet().Object);

        _taxMasterRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TaxMasterEntity>
            {
            new TaxMasterEntity { Id = 1, TaxName = "Tax1", IsActive = true },
            new TaxMasterEntity { Id = 2, TaxName = "Tax2", IsActive = true },
            new TaxMasterEntity { Id = 999, TaxName = "TaxTotal", IsActive = true }
            }.BuildMockDbSet().Object);

        _taxPercentageRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TaxPercentageMasterCVEntity>
            {
            new TaxPercentageMasterCVEntity
            {
                TaxId = 1,
                TypeOfUseId = 1,
                YearRangeCVId = 1,
                TaxPercentage = 10,
                IsActive = true
            },
            new TaxPercentageMasterCVEntity
            {
                TaxId = 2,
                TypeOfUseId = 1,
                YearRangeCVId = 1,
                TaxPercentage = 5,
                IsActive = true
            }
            }.BuildMockDbSet().Object);

        _yearMasterRepo.Setup(x => x.GetQueryable()).Returns(
            new List<YearMasterEntity>
            {
            new YearMasterEntity { Id = 1, Year = 2024, IsActive = true }
            }.BuildMockDbSet().Object);

        _policyTaxDetailsCVRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PolicyTaxDetailsCVEntity>().BuildMockDbSet().Object);

        _transMastCVRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TransMastCVEntity>().BuildMockDbSet().Object);

        var service = GetService();

        var result = await service.CreateAsync(new CreateCapitalValueDto { PropertyId = propertyId });

        Assert.NotNull(result);
        Assert.Single(result);

        var tax2 = result[0].Taxes.FirstOrDefault(t => t.TaxId == 2);
        Assert.NotNull(tax2);
        Assert.Equal("Tax2", tax2.TaxName);

        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task CreateAsync_YearRangeNotFound_Throws()
    {
        var propertyId = 1;

        _propertyRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyEntity>
            {
            new PropertyEntity { Id = propertyId, MoujaId = 1, CSN = "A1", IsActive = true }
            }.BuildMockDbSet().Object);



        _pdRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyDetailsEntity>
            {
            new PropertyDetailsEntity
            {
                Id = 10,
                PropertyId = propertyId,
                AssessmentYear = "2050", // no range
                IsActive = true
            }
            }.BuildMockDbSet().Object);



        _cvRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyTaxCalculationCVResultsEntity>().BuildMockDbSet().Object);



        _flagRepo.Setup(x => x.GetQueryable()).Returns(
            new List<FlagMasterEntity>().BuildMockDbSet().Object);



        _assessmentYearRepo.Setup(x => x.GetQueryable())
            .Returns(new List<AssessmentYearRangeCVEntity>().BuildMockDbSet().Object);

        var service = GetService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateCapitalValueDto { PropertyId = propertyId }));
    }

    [Fact]
    public async Task GetAsync_ReturnsData()
    {
        int propertyId = 1;

        _propertyRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyEntity>
            {
                new PropertyEntity { Id = propertyId, MoujaId = 1, CSN = "A1", IsActive = true }
            }.BuildMockDbSet().Object);



        _pdRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyDetailsEntity>
            {
                new PropertyDetailsEntity
                {
                    Id = 10,
                    PropertyId = propertyId,
                    TypeOfUseId = 1,
                    ConstructionTypeId = 1,
                    AssessmentYear = "2020",
                    ConstructionYear = "2015",
                    CarpetAreaSqMeter = 100,
                    IsActive = true
                }
            }.BuildMockDbSet().Object);



        _cvRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyTaxCalculationCVResultsEntity>
            {
                new PropertyTaxCalculationCVResultsEntity
                {
                    PropertyId = propertyId,
                    PropertyDetailsId = 10,
                    TaxId = 1,
                    IsActive = true
                }
            }.BuildMockDbSet().Object);



        _flagRepo.Setup(x => x.GetQueryable()).Returns(
            new List<FlagMasterEntity>().BuildMockDbSet().Object);



        _assessmentYearRepo.Setup(x => x.GetQueryable()).Returns(
            new List<AssessmentYearRangeCVEntity>
            {
                new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2030, IsActive = true }
            }.BuildMockDbSet().Object);



        _csnDetailsRepo.Setup(x => x.GetQueryable()).Returns(
            new List<CSNDetailsEntity>
            {
                new CSNDetailsEntity
                {
                    Id = 1,
                    RateMasterCVId = 1,
                    CSN = "A1",
                    IsActive = true
                }
            }.BuildMockDbSet().Object);



        _rateRepo.Setup(x => x.GetQueryable()).Returns(
            new List<RateMasterForCVEntity>
            {
                new RateMasterForCVEntity { Id = 1, RateAmount = 10, IsActive = true }
            }.BuildMockDbSet().Object);



        _taxPercentageRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TaxPercentageMasterCVEntity>
            {
                new TaxPercentageMasterCVEntity { TaxId = 1, TypeOfUseId = 1, YearRangeCVId = 1, TaxPercentage = 10, IsActive = true }
            }.BuildMockDbSet().Object);



        _floorFactorRepo.Setup(x => x.GetQueryable()).Returns(
            new List<FloorFactorCVMasterEntity>().BuildMockDbSet().Object);



        _natureRepo.Setup(x => x.GetQueryable()).Returns(
            new List<NatureFactorCVMasterEntity>().BuildMockDbSet().Object);



        _useRepo.Setup(x => x.GetQueryable()).Returns(
            new List<UseFactorCVMasterEntity>().BuildMockDbSet().Object);



        _ageRepo.Setup(x => x.GetQueryable()).Returns(
            new List<AgeFactorCVMasterEntity>().BuildMockDbSet().Object);



        _taxMasterRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TaxMasterEntity>
            {
                new TaxMasterEntity { Id = 1, TaxName = "Test Tax", IsActive = true }
            }.BuildMockDbSet().Object);        var service = GetService();

        var result = await service.GetAsync(propertyId);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateAsync_RateMasterNotFound_Throws()
    {
        var propertyId = 1;

        _propertyRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyEntity>
            {
            new PropertyEntity { Id = propertyId, MoujaId = 1, CSN = "A1", IsActive = true }
            }.BuildMockDbSet().Object);



        _pdRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyDetailsEntity>
            {
            new PropertyDetailsEntity
            {
                Id = 10,
                PropertyId = propertyId,
                AssessmentYear = "2020",
                TypeOfUseId = 1,
                ConstructionTypeId = 1,
                IsActive = true
            }
            }.BuildMockDbSet().Object);



        _cvRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyTaxCalculationCVResultsEntity>().BuildMockDbSet().Object);



        _flagRepo.Setup(x => x.GetQueryable()).Returns(
            new List<FlagMasterEntity>().BuildMockDbSet().Object);



        _assessmentYearRepo.Setup(x => x.GetQueryable()).Returns(
            new List<AssessmentYearRangeCVEntity>
            {
            new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2030, IsActive = true }
            }.BuildMockDbSet().Object);



        _csnDetailsRepo.Setup(x => x.GetQueryable())
            .Returns(new List<CSNDetailsEntity>().BuildMockDbSet().Object);



        _rateRepo.Setup(x => x.GetQueryable())
            .Returns(new List<RateMasterForCVEntity>().BuildMockDbSet().Object);

        var service = GetService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateCapitalValueDto { PropertyId = propertyId }));
    }

    [Fact]
    public async Task CreateAsync_InvalidType_Throws()
    {
        var propertyId = 1;

        _propertyRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyEntity>
            {
            new PropertyEntity { Id = propertyId, MoujaId = 1, CSN = "A1", IsActive = true }
            }.BuildMockDbSet().Object);



        _pdRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyDetailsEntity>
            {
            new PropertyDetailsEntity
            {
                Id = 10,
                PropertyId = propertyId,
                AssessmentYear = "2020",
                TypeOfUseId = 1,
                ConstructionTypeId = 1,
                IsActive = true
            }
            }.BuildMockDbSet().Object);



        _cvRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyTaxCalculationCVResultsEntity>().BuildMockDbSet().Object);



        _flagRepo.Setup(x => x.GetQueryable()).Returns(
            new List<FlagMasterEntity>().BuildMockDbSet().Object);

        _assessmentYearRepo.Setup(x => x.GetQueryable()).Returns(
            new List<AssessmentYearRangeCVEntity>
            {
            new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2030, IsActive = true }
            }.BuildMockDbSet().Object);

        var service = GetService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateCapitalValueDto { PropertyId = propertyId }));
    }



    [Fact]
    public async Task CreateAsync_ValidData_CalculatesSuccessfully()
    {
        var propertyId = 1;

        _propertyRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyEntity>
            {
            new PropertyEntity { Id = propertyId, MoujaId = 1, CSN = "A1", IsActive = true }
            }.BuildMockDbSet().Object);

        _pdRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyDetailsEntity>
            {
            new PropertyDetailsEntity
            {
                Id = 10,
                PropertyId = propertyId,
                AssessmentYear = "2020",
                ConstructionYear = "2015",
                TypeOfUseId = 1,
                TypeOfUse = new TypeOfUseEntity
                {
                    Id = 1,
                    Type = "R",
                    TypeOfUseGroupId = 1,
                    TypeOfUseGroup = new TypeOfUseGroupEntity
                    {
                        Id = 1,
                        IsActive = true,
                        IsFloorWiseRateApplicable = true
                    },
                    IsActive = true
                },
                FloorId = 1,
                Floor = new FloorEntity
                {
                    Id = 1,
                    FloorGroupId = 0,
                    IsActive = true
                },
                ConstructionTypeId = 1,
                CarpetAreaSqMeter = 100,
                IsActive = true
            }
            }.BuildMockDbSet().Object);

        _assessmentYearRepo.Setup(x => x.GetQueryable()).Returns(
            new List<AssessmentYearRangeCVEntity>
            {
            new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2030, IsActive = true }
            }.BuildMockDbSet().Object);

        _csnDetailsRepo.Setup(x => x.GetQueryable()).Returns(
            new List<CSNDetailsEntity>
            {
            new CSNDetailsEntity { Id = 1, RateMasterCVId = 1, CSN = "A1", IsActive = true }
            }.BuildMockDbSet().Object);

        _cvRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyTaxCalculationCVResultsEntity>().BuildMockDbSet().Object);

        _flagRepo.Setup(x => x.GetQueryable()).Returns(
            new List<FlagMasterEntity>().BuildMockDbSet().Object);

        _floorFactorRepo.Setup(x => x.GetQueryable()).Returns(
            new List<FloorFactorCVMasterEntity>().BuildMockDbSet().Object);

        _natureRepo.Setup(x => x.GetQueryable()).Returns(
            new List<NatureFactorCVMasterEntity>().BuildMockDbSet().Object);

        _useRepo.Setup(x => x.GetQueryable()).Returns(
            new List<UseFactorCVMasterEntity>().BuildMockDbSet().Object);

        _ageRepo.Setup(x => x.GetQueryable()).Returns(
            new List<AgeFactorCVMasterEntity>().BuildMockDbSet().Object);

        _rateRepo.Setup(x => x.GetQueryable()).Returns(
            new List<RateMasterForCVEntity>
            {
            new RateMasterForCVEntity
            {
                Id = 1,
                SubZoneId = 1,
                TypeOfUseGroupCVId = 1,
                FloorGroupId = 0,
                AssessmentYearRangeId = 1,
                RateAmount = 10,
                IsActive = true
            }
            }.BuildMockDbSet().Object);

        _taxMasterRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TaxMasterEntity>
            {
            new TaxMasterEntity { Id = 1, TaxName = "Tax", IsActive = true },
            new TaxMasterEntity { Id = 999, TaxName = "TaxTotal", IsActive = true }
            }.BuildMockDbSet().Object);

        _taxPercentageRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TaxPercentageMasterCVEntity>
            {
            new TaxPercentageMasterCVEntity { TaxId = 1, TypeOfUseId = 1, YearRangeCVId = 1, TaxPercentage = 10, IsActive = true }
            }.BuildMockDbSet().Object);

        _yearMasterRepo.Setup(x => x.GetQueryable()).Returns(
            new List<YearMasterEntity>
            {
            new YearMasterEntity { Id = 1, Year = 2024, IsActive = true }
            }.BuildMockDbSet().Object);

        _policyTaxDetailsCVRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PolicyTaxDetailsCVEntity>().BuildMockDbSet().Object);

        _transMastCVRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TransMastCVEntity>().BuildMockDbSet().Object);

        var service = GetService();

        var result = await service.CreateAsync(new CreateCapitalValueDto { PropertyId = propertyId });

        Assert.NotNull(result);
        Assert.Single(result);

        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_InsertsMissingPropertyDetails()
    {
        int propertyId = 1;

        _propertyRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyEntity>
            {
            new PropertyEntity
            {
                Id = propertyId,
                MoujaId = 1,
                CSN = "A1",
                IsActive = true
            }
            }.BuildMockDbSet().Object);

        _pdRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyDetailsEntity>
            {
            new PropertyDetailsEntity
            {
                Id = 10,
                PropertyId = propertyId,
                TypeOfUseId = 1,
                TypeOfUse = new TypeOfUseEntity
                {
                    Id = 1,
                    Type = "R",
                    TypeOfUseGroupId = 1,
                    TypeOfUseGroup = new TypeOfUseGroupEntity
                    {
                        Id = 1,
                        IsActive = true,
                        IsFloorWiseRateApplicable = true
                    },
                    IsActive = true
                },
                FloorId = 1,
                Floor = new FloorEntity
                {
                    Id = 1,
                    FloorGroupId = 0,
                    IsActive = true
                },
                ConstructionTypeId = 1,
                AssessmentYear = "2020",
                ConstructionYear = "2015",
                CarpetAreaSqMeter = 100,
                IsActive = true
            }
            }.BuildMockDbSet().Object);

        _cvRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyTaxCalculationCVResultsEntity>().BuildMockDbSet().Object);

        _flagRepo.Setup(x => x.GetQueryable()).Returns(
            new List<FlagMasterEntity>().BuildMockDbSet().Object);

        _assessmentYearRepo.Setup(x => x.GetQueryable()).Returns(
            new List<AssessmentYearRangeCVEntity>
            {
            new AssessmentYearRangeCVEntity
            {
                Id = 1,
                FromYear = 2000,
                ToYear = 2030,
                IsActive = true
            }
            }.BuildMockDbSet().Object);

        _csnDetailsRepo.Setup(x => x.GetQueryable()).Returns(
            new List<CSNDetailsEntity>
            {
            new CSNDetailsEntity
            {
                Id = 1,
                RateMasterCVId = 1,
                CSN = "A1",
                IsActive = true
            }
            }.BuildMockDbSet().Object);

        _rateRepo.Setup(x => x.GetQueryable()).Returns(
            new List<RateMasterForCVEntity>
            {
            new RateMasterForCVEntity
            {
                Id = 1,
                SubZoneId = 1,
                TypeOfUseGroupCVId = 1,
                FloorGroupId = 0,
                AssessmentYearRangeId = 1,
                RateAmount = 10,
                IsActive = true
            }
            }.BuildMockDbSet().Object);

        _taxMasterRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TaxMasterEntity>
            {
            new TaxMasterEntity { Id = 1, TaxName = "Tax", IsActive = true },
            new TaxMasterEntity { Id = 999, TaxName = "TaxTotal", IsActive = true }
            }.BuildMockDbSet().Object);

        _taxPercentageRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TaxPercentageMasterCVEntity>
            {
            new TaxPercentageMasterCVEntity
            {
                TaxId = 1,
                TypeOfUseId = 1,
                YearRangeCVId = 1,
                TaxPercentage = 10,
                IsActive = true
            }
            }.BuildMockDbSet().Object);

        _floorFactorRepo.Setup(x => x.GetQueryable()).Returns(
            new List<FloorFactorCVMasterEntity>().BuildMockDbSet().Object);

        _natureRepo.Setup(x => x.GetQueryable()).Returns(
            new List<NatureFactorCVMasterEntity>().BuildMockDbSet().Object);

        _useRepo.Setup(x => x.GetQueryable()).Returns(
            new List<UseFactorCVMasterEntity>().BuildMockDbSet().Object);

        _ageRepo.Setup(x => x.GetQueryable()).Returns(
            new List<AgeFactorCVMasterEntity>().BuildMockDbSet().Object);

        _yearMasterRepo.Setup(x => x.GetQueryable()).Returns(
            new List<YearMasterEntity>
            {
            new YearMasterEntity { Id = 1, Year = 2024, IsActive = true }
            }.BuildMockDbSet().Object);

        _policyTaxDetailsCVRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PolicyTaxDetailsCVEntity>().BuildMockDbSet().Object);

        _transMastCVRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TransMastCVEntity>().BuildMockDbSet().Object);

        var service = GetService();

        var result = await service.GetAsync(propertyId);

        Assert.NotNull(result);

        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(Skip = "Backfill logic requires EF Core includes that don't work with mocked DbSets. Use integration tests instead.")]
    public async Task GetAsync_BackfillsMissingTaxHeads()
    {
        // Scenario: PropertyDetails 10 has CV records for Tax 1, but Tax 2 is added later
        // GetAsync should detect missing Tax 2 and call CreateAsync to backfill it

        int propertyId = 1;

        _pdRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyDetailsEntity>
            {
                new PropertyDetailsEntity {
                    Id = 10, 
                    PropertyId = propertyId, 
                    TypeOfUseId = 1,
                    TypeOfUse = new TypeOfUseEntity {
                        Id = 1,
                        Type = "R",
                        TypeOfUseGroupId = 1,
                        TypeOfUseGroup = new TypeOfUseGroupEntity { Id = 1, IsActive = true },
                        IsActive = true
                    },
                    ConstructionTypeId = 1,
                    AssessmentYear = "2020",
                    ConstructionYear = "2015",
                    CarpetAreaSqMeter = 100,
                    IsActive = true 
                }
            }.BuildMockDbSet().Object);

        // CV record exists for PropertyDetailsId=10, TaxId=1 (but Tax 2 is missing)
        _cvRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyTaxCalculationCVResultsEntity>
            {
                new PropertyTaxCalculationCVResultsEntity { 
                    PropertyId = propertyId, 
                    PropertyDetailsId = 10, 
                    TaxId = 1, // Only Tax 1 exists
                    IsActive = true 
                }
            }.BuildMockDbSet().Object);



        _propertyRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyEntity>
            {
                new PropertyEntity { Id = propertyId, MoujaId = 1, CSN = "A1", IsActive = true }
            }.BuildMockDbSet().Object);



        _flagRepo.Setup(x => x.GetQueryable()).Returns(
            new List<FlagMasterEntity>().BuildMockDbSet().Object);



        _floorFactorRepo.Setup(x => x.GetQueryable()).Returns(
            new List<FloorFactorCVMasterEntity>().BuildMockDbSet().Object);

        _assessmentYearRepo.Setup(x => x.GetQueryable()).Returns(
            new List<AssessmentYearRangeCVEntity>
            {
                new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2030, IsActive = true }
            }.BuildMockDbSet().Object);



        _natureRepo.Setup(x => x.GetQueryable()).Returns(
            new List<NatureFactorCVMasterEntity>().BuildMockDbSet().Object);



        _useRepo.Setup(x => x.GetQueryable()).Returns(
            new List<UseFactorCVMasterEntity>().BuildMockDbSet().Object);



        _ageRepo.Setup(x => x.GetQueryable()).Returns(
            new List<AgeFactorCVMasterEntity>().BuildMockDbSet().Object);



        _csnDetailsRepo.Setup(x => x.GetQueryable()).Returns(
            new List<CSNDetailsEntity>
            {
                new CSNDetailsEntity
                {Id = 1,RateMasterCVId = 1,CSN = "A1",IsActive = true}
            }.BuildMockDbSet().Object);



        _rateRepo.Setup(x => x.GetQueryable()).Returns(
            new List<RateMasterForCVEntity>
            {
                new RateMasterForCVEntity {
                    Id = 1,
                    SubZoneId = 1,
                    TypeOfUseGroupCVId = 1,
                    FloorGroupId = 0,
                    AssessmentYearRangeId = 1,
                    RateAmount = 10,
                    IsActive = true
                }
            }.BuildMockDbSet().Object);



        _taxMasterRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TaxMasterEntity>
            {
                new TaxMasterEntity { Id = 1, TaxName = "Tax1", IsActive = true },
                new TaxMasterEntity { Id = 2, TaxName = "Tax2", IsActive = true }, // Tax 2 exists in system but not in CV
                new TaxMasterEntity { Id = 999, TaxName = "TaxTotal", IsActive = true }  // Required for CreateAsync
            }.BuildMockDbSet().Object);

        // BOTH Tax 1 and Tax 2 are expected for TypeOfUseId=1
        _taxPercentageRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TaxPercentageMasterCVEntity>
            {
                new TaxPercentageMasterCVEntity { TaxId = 1, TypeOfUseId = 1, YearRangeCVId = 1, TaxPercentage = 10, IsActive = true },
                new TaxPercentageMasterCVEntity { TaxId = 2, TypeOfUseId = 1, YearRangeCVId = 1, TaxPercentage = 5, IsActive = true } // Tax 2 is expected
            }.BuildMockDbSet().Object);



        _yearMasterRepo.Setup(x => x.GetQueryable()).Returns(
            new List<YearMasterEntity>
            {
                new YearMasterEntity { Id = 1, Year = 2024, IsActive = true }
            }.BuildMockDbSet().Object);



        _policyTaxDetailsCVRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PolicyTaxDetailsCVEntity>().BuildMockDbSet().Object);



        _transMastCVRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TransMastCVEntity>().BuildMockDbSet().Object);

        var service = GetService();

        var result = await service.GetAsync(propertyId);

        // Verify that GetAsync detected missing Tax 2 and called CreateAsync
        Assert.NotNull(result);

        // Verify transaction methods were called (indicating CreateAsync was triggered)
        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(Skip = "Backfill logic requires EF Core includes that don't work with mocked DbSets. Use integration tests instead.")]
    public async Task GetAsync_BackfillsMissingTaxHeads_ForDifferentAssessmentYearsAndTypes()
    {
        // Scenario: Property has multiple PropertyDetails with different AssessmentYears and TypeOfUseIds
        // Each should be checked independently for missing taxes
        // PropertyDetails 10: Year=2020, Type=1 ? has Tax 1, missing Tax 2
        // PropertyDetails 11: Year=2023, Type=2 ? has Tax 3, missing Tax 4

        int propertyId = 1;

        _pdRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyDetailsEntity>
            {
                new PropertyDetailsEntity {
                    Id = 10, 
                    PropertyId = propertyId, 
                    TypeOfUseId = 1, // Residential
                    TypeOfUse = new TypeOfUseEntity {
                        Id = 1,
                        Type = "R",
                        TypeOfUseGroupId = 1,
                        TypeOfUseGroup = new TypeOfUseGroupEntity { Id = 1, IsActive = true },
                        IsActive = true
                    },
                    ConstructionTypeId = 1,
                    AssessmentYear = "2020",
                    ConstructionYear = "2015",
                    CarpetAreaSqMeter = 100,
                    IsActive = true 
                },
                new PropertyDetailsEntity {
                    Id = 11, 
                    PropertyId = propertyId, 
                    TypeOfUseId = 2, // Commercial
                    TypeOfUse = new TypeOfUseEntity {
                        Id = 2,
                        Type = "C",
                        TypeOfUseGroupId = 2,
                        TypeOfUseGroup = new TypeOfUseGroupEntity { Id = 2, IsActive = true },
                        IsActive = true
                    },
                    ConstructionTypeId = 1,
                    AssessmentYear = "2023",
                    ConstructionYear = "2018",
                    CarpetAreaSqMeter = 150,
                    IsActive = true 
                }
            }.BuildMockDbSet().Object);

        // CV records: PropertyDetails 10 has Tax 1, PropertyDetails 11 has Tax 3
        // Both are missing their second tax (Tax 2 and Tax 4 respectively)
        _cvRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyTaxCalculationCVResultsEntity>
            {
                new PropertyTaxCalculationCVResultsEntity { 
                    PropertyId = propertyId, 
                    PropertyDetailsId = 10, 
                    TaxId = 1, // PropertyDetails 10 has Tax 1
                    IsActive = true 
                },
                new PropertyTaxCalculationCVResultsEntity { 
                    PropertyId = propertyId, 
                    PropertyDetailsId = 11, 
                    TaxId = 3, // PropertyDetails 11 has Tax 3
                    IsActive = true 
                }
            }.BuildMockDbSet().Object);



        _propertyRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PropertyEntity>
            {
                new PropertyEntity { Id = propertyId, MoujaId = 1, CSN = "A1", IsActive = true }
            }.BuildMockDbSet().Object);



        _flagRepo.Setup(x => x.GetQueryable()).Returns(
            new List<FlagMasterEntity>().BuildMockDbSet().Object);



        _floorFactorRepo.Setup(x => x.GetQueryable()).Returns(
            new List<FloorFactorCVMasterEntity>().BuildMockDbSet().Object);        // Two year ranges for different assessment years
        _assessmentYearRepo.Setup(x => x.GetQueryable()).Returns(
            new List<AssessmentYearRangeCVEntity>
            {
                new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2015, ToYear = 2022, IsActive = true }, // For 2020
                new AssessmentYearRangeCVEntity { Id = 2, FromYear = 2023, ToYear = 2030, IsActive = true }  // For 2023
            }.BuildMockDbSet().Object);



        _natureRepo.Setup(x => x.GetQueryable()).Returns(
            new List<NatureFactorCVMasterEntity>().BuildMockDbSet().Object);



        _useRepo.Setup(x => x.GetQueryable()).Returns(
            new List<UseFactorCVMasterEntity>().BuildMockDbSet().Object);



        _ageRepo.Setup(x => x.GetQueryable()).Returns(
            new List<AgeFactorCVMasterEntity>().BuildMockDbSet().Object);



        _csnDetailsRepo.Setup(x => x.GetQueryable()).Returns(
            new List<CSNDetailsEntity>
            {
                new CSNDetailsEntity
                {
                    Id = 1,
                    RateMasterCVId = 1,
                    CSN = "A1",
                    IsActive = true
                },
                new CSNDetailsEntity
                {
                    Id = 1,
                    RateMasterCVId = 2,
                    CSN = "A1",
                    IsActive = true
                }
            }.BuildMockDbSet().Object);



        _rateRepo.Setup(x => x.GetQueryable()).Returns(
            new List<RateMasterForCVEntity>
            {
                new RateMasterForCVEntity {
                    Id = 1,
                    SubZoneId = 1,
                    TypeOfUseGroupCVId = 1,
                    FloorGroupId = 0,
                    AssessmentYearRangeId = 1,
                    RateAmount = 10,
                    IsActive = true
                },
                new RateMasterForCVEntity {
                    Id = 2,
                    SubZoneId = 1,
                    TypeOfUseGroupCVId = 1,
                    FloorGroupId = 0,
                    AssessmentYearRangeId = 2,
                    RateAmount = 15,
                    IsActive = true
                }
            }.BuildMockDbSet().Object);



        _taxMasterRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TaxMasterEntity>
            {
                new TaxMasterEntity { Id = 1, TaxName = "Tax1", IsActive = true },
                new TaxMasterEntity { Id = 2, TaxName = "Tax2", IsActive = true },
                new TaxMasterEntity { Id = 3, TaxName = "Tax3", IsActive = true },
                new TaxMasterEntity { Id = 4, TaxName = "Tax4", IsActive = true },
                new TaxMasterEntity { Id = 999, TaxName = "TaxTotal", IsActive = true }  // Required for CreateAsync
            }.BuildMockDbSet().Object);

        // Tax configuration:
        // TypeOfUseId=1 (Residential), YearRange=1 (2015-2022) ? expects Tax 1, 2
        // TypeOfUseId=2 (Commercial), YearRange=2 (2023-2030) ? expects Tax 3, 4
        _taxPercentageRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TaxPercentageMasterCVEntity>
            {
                new TaxPercentageMasterCVEntity { TaxId = 1, TypeOfUseId = 1, YearRangeCVId = 1, TaxPercentage = 10, IsActive = true },
                new TaxPercentageMasterCVEntity { TaxId = 2, TypeOfUseId = 1, YearRangeCVId = 1, TaxPercentage = 5, IsActive = true }, // Missing for PD 10
                new TaxPercentageMasterCVEntity { TaxId = 3, TypeOfUseId = 2, YearRangeCVId = 2, TaxPercentage = 8, IsActive = true },
                new TaxPercentageMasterCVEntity { TaxId = 4, TypeOfUseId = 2, YearRangeCVId = 2, TaxPercentage = 12, IsActive = true } // Missing for PD 11
            }.BuildMockDbSet().Object);



        _yearMasterRepo.Setup(x => x.GetQueryable()).Returns(
            new List<YearMasterEntity>
            {
                new YearMasterEntity { Id = 1, Year = 2024, IsActive = true }
            }.BuildMockDbSet().Object);



        _policyTaxDetailsCVRepo.Setup(x => x.GetQueryable()).Returns(
            new List<PolicyTaxDetailsCVEntity>().BuildMockDbSet().Object);



        _transMastCVRepo.Setup(x => x.GetQueryable()).Returns(
            new List<TransMastCVEntity>().BuildMockDbSet().Object);

        var service = GetService();

        var result = await service.GetAsync(propertyId);

        // Verify that GetAsync detected missing taxes and called CreateAsync
        Assert.NotNull(result);

        // Verify transaction methods were called (indicating CreateAsync was triggered)
        _uow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

}

#endregion
