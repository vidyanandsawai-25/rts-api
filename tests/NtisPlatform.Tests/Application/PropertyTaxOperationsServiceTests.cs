using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.PropertyTaxOperations;
using NtisPlatform.Application.DTOs.CapitalValue;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService;
using NtisPlatform.Application.Services.PropertyTaxOperations;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace NtisPlatform.Tests.Application;

public class PropertyTaxOperationsServiceTests
{
    private readonly Mock<IRateableValueService> _rv = new();
    private readonly Mock<ICapitalValueService> _cv = new();
    private readonly Mock<IPolicyConfigurationService> _policyConfig = new();
    private readonly Mock<IRepository<PropertyEntity, int>> _propertyRepo = new();
    private readonly Mock<IRepository<PropertyTaxJobEntity, int>> _jobRepo = new();
    private readonly Mock<IRepository<PropertyTaxJobDetailEntity, int>> _jobDetailRepo = new();
    private readonly Mock<IRepository<PolicyTaxDetailsEntity, int>> _policyRepo = new();
    private readonly Mock<IRepository<PropertyScreenLockEntity, int>> _lockRepo = new();
    private readonly Mock<IRepository<PropertyDetailsEntity, int>> _detailsRepo = new();
    private readonly Mock<IRepository<YearMasterEntity, int>> _yearMasterRepo = new();
    private readonly Mock<IRepository<WardEntity, int>> _wardRepo = new();
    private readonly Mock<IRepository<ZoneEntity, int>> _zoneRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserScreenAccessService> _screenAccessService = new();
    private readonly Mock<IConfiguration> _config = new();

    public PropertyTaxOperationsServiceTests()
    {
        _policyConfig.Setup(p => p.GetPolicyValueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string code, string def, CancellationToken ct) => def);

        _wardRepo.Setup(r => r.GetQueryable()).Returns(new List<WardEntity>().BuildMock());
        _zoneRepo.Setup(r => r.GetQueryable()).Returns(new List<ZoneEntity>().BuildMock());
        _yearMasterRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) => new YearMasterEntity { Id = id, Year = id == 1 ? 2025 : 2026, YearCode = id == 1 ? "2025-26" : "2026-27" });
        
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns((string?)null);
        _config.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockSection.Object);
    }

    private PropertyTaxOperationsService CreateService()
        => new(
            _rv.Object,
            _cv.Object,
            _policyConfig.Object,
            _propertyRepo.Object,
            _jobRepo.Object,
            _jobDetailRepo.Object,
            _lockRepo.Object,
            _detailsRepo.Object,
            _yearMasterRepo.Object,
            _wardRepo.Object,
            _zoneRepo.Object,
            _uow.Object,
            NullLogger<PropertyTaxOperationsService>.Instance,
            _screenAccessService.Object,
            _config.Object);

    private static PropertyEntity Property(int id) => new()
    {
        Id = id,
        IsActive = true,
        MarkedForDeletion = false,
        PropertyNo = $"PROP-{id:D8}",
        OwnerName = $"Owner {id}",
    };

    [Fact]
    public async Task GetEligibleCountAsync_ExcludesLocked()
    {
        // 3 properties, all with details: #1 locked, #2 already processed for FY -> only #1 skipped, #2 and #3 eligible.
        var properties = new List<PropertyEntity> { Property(1), Property(2), Property(3) };
        var details = properties.Select(p => new PropertyDetailsEntity { Id = p.Id, PropertyId = p.Id, IsActive = true }).ToList();
        var locks = new List<PropertyScreenLockEntity>
        {
            new() { Id = 1, PropertyId = 1, IsLocked = true, IsActive = true, MarkedForDeletion = false }
        };
        var policies = new List<PolicyTaxDetailsEntity>
        {
            new() { Id = 1, PropertyId = 2, PolicyCode = "NETTAX", PolicyYear = 2025, IsActive = true, MarkedForDeletion = false }
        };

        _propertyRepo.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _detailsRepo.Setup(r => r.GetQueryable()).Returns(details.BuildMock());
        _lockRepo.Setup(r => r.GetQueryable()).Returns(locks.BuildMock());
        _policyRepo.Setup(r => r.GetQueryable()).Returns(policies.BuildMock());

        var service = CreateService();

        var result = await service.GetEligibleCountAsync(
            new EligibleCountRequestDto
            {
                FinanceYearId = 1,
                ScopeType = "Property",
                Operation = "AddTax",
                Scope = new OperationScopeDto { PropertyIds = new List<int> { 1, 2, 3 } }
            },
            actingUserId: 7);

        result.Total.Should().Be(3);
        result.Eligible.Should().Be(2);
        result.Skipped.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonAddTaxOperation_Throws()
    {
        var service = CreateService();

        var act = async () => await service.ExecuteAsync(
            new ExecuteOperationRequestDto
            {
                FinanceYearId = 1,
                Operation = "RemoveTax",
                ScopeType = "Property",
                Scope = new OperationScopeDto { PropertyIds = new List<int> { 1 } }
            },
            new OperationContext(ActingUserId: 7));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoEligibleRecords_Throws()
    {
        // One property but it is locked -> zero eligible.
        var properties = new List<PropertyEntity> { Property(1) };
        var details = new List<PropertyDetailsEntity> { new() { Id = 1, PropertyId = 1, IsActive = true } };
        var locks = new List<PropertyScreenLockEntity>
        {
            new() { Id = 1, PropertyId = 1, IsLocked = true, IsActive = true, MarkedForDeletion = false }
        };

        _propertyRepo.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _detailsRepo.Setup(r => r.GetQueryable()).Returns(details.BuildMock());
        _lockRepo.Setup(r => r.GetQueryable()).Returns(locks.BuildMock());
        _policyRepo.Setup(r => r.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>().BuildMock());

        var service = CreateService();

        var act = async () => await service.ExecuteAsync(
            new ExecuteOperationRequestDto
            {
                FinanceYearId = 1,
                Operation = "AddTax",
                ScopeType = "Property",
                Scope = new OperationScopeDto { PropertyIds = new List<int> { 1 } }
            },
            new OperationContext(ActingUserId: 7));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithOverlappingActiveJob_Throws()
    {
        // 2 properties
        var properties = new List<PropertyEntity> { Property(1), Property(2) };
        var details = properties.Select(p => new PropertyDetailsEntity { Id = p.Id, PropertyId = p.Id, IsActive = true }).ToList();
        
        var activeJobs = new List<PropertyTaxJobEntity>
        {
            new()
            {
                Id = 1,
                JobCode = "JOB-ADD-2025-0001",
                Operation = "AddTax",
                FinanceYearId = 1,
                FinanceYear = new YearMasterEntity { Id = 1, Year = 2025, YearCode = "2025-26" },
                ScopeType = "Property",
                ScopeParamsJson = "{\"PropertyIds\":[1,2]}",
                Status = "InProgress",
                MarkedForDeletion = false,
                StartedByUserName = "Tester"
            }
        };

        var activeDetails = new List<PropertyTaxJobDetailEntity>
        {
            new() { Id = 101, JobId = 1, PropertyId = 1, Status = "Pending", IsActive = true, MarkedForDeletion = false }
        };

        _propertyRepo.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _detailsRepo.Setup(r => r.GetQueryable()).Returns(details.BuildMock());
        _lockRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyScreenLockEntity>().BuildMock());
        _policyRepo.Setup(r => r.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>().BuildMock());
        _jobRepo.Setup(r => r.GetQueryable()).Returns(activeJobs.BuildMock());
        _jobDetailRepo.Setup(r => r.GetQueryable()).Returns(activeDetails.BuildMock());

        var service = CreateService();

        var act = async () => await service.ExecuteAsync(
            new ExecuteOperationRequestDto
            {
                FinanceYearId = 1,
                Operation = "AddTax",
                ScopeType = "Property",
                Scope = new OperationScopeDto { PropertyIds = new List<int> { 1, 2 } }
            },
            new OperationContext(ActingUserId: 7));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("One or more selected properties overlap with an active running job. Please select different properties.");
    }

    [Fact]
    public async Task ProcessJobAsync_Success_ProcessesAllPropertiesAndSaves()
    {
        // 2 target properties
        var properties = new List<PropertyEntity> { Property(1), Property(2) };
        var details = properties.Select(p => new PropertyDetailsEntity { Id = p.Id, PropertyId = p.Id, IsActive = true }).ToList();
        
        var job = new PropertyTaxJobEntity
        {
            Id = 123,
            JobCode = "JOB-ADD-2025-0123",
            Operation = "AddTax",
            FinanceYearId = 1,
            FinanceYear = new YearMasterEntity { Id = 1, Year = 2025, YearCode = "2025-26" },
            ScopeType = "Property",
            ScopeParamsJson = "{\"PropertyIds\":[1,2]}",
            Status = "InProgress",
            MarkedForDeletion = false,
            StartedByUserId = 7
        };

        _propertyRepo.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _detailsRepo.Setup(r => r.GetQueryable()).Returns(details.BuildMock());
        _lockRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyScreenLockEntity>().BuildMock());
        _policyRepo.Setup(r => r.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>().BuildMock());
        _jobRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyTaxJobEntity> { job }.BuildMock());
        _jobDetailRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyTaxJobDetailEntity>().BuildMock());

        var rvResult = new NtisPlatform.Application.DTOs.RateableValue.RateableValueResponseDto
        {
            TotalTax = 100m,
            Policy = new NtisPlatform.Application.DTOs.RateableValue.PolicyTaxDto
            {
                Taxes = new Dictionary<string, decimal>
                {
                    { "GeneralTax", 100m }
                }
            }
        };
        _rv.Setup(s => s.CalculateAndSaveAsync(It.IsAny<int>())).ReturnsAsync(rvResult);

        var service = CreateService();

        await service.ProcessJobAsync(123, CancellationToken.None);

        // Verify that CalculateAndSaveAsync was called for both property IDs
        _rv.Verify(s => s.CalculateAndSaveAsync(1), Times.Once);
        _rv.Verify(s => s.CalculateAndSaveAsync(2), Times.Once);

        // Verify that detail records were added in bulk
        _jobDetailRepo.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<PropertyTaxJobDetailEntity>>(list => list.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);

        // Verify job properties updated to Completed
        job.Status.Should().Be("Completed");
        job.SuccessCount.Should().Be(2);
        job.FailedCount.Should().Be(0);
        job.SkippedCount.Should().Be(0);
        job.RecordsProcessed.Should().Be(2);
    }

    [Fact]
    public async Task ProcessJobAsync_WithCalculationMethodCV_ExecutesCapitalValueCalculation()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            Property(1)
        };

        var details = new List<PropertyDetailsEntity>
        {
            new() { Id = 10, PropertyId = 1, IsActive = true, MarkedForDeletion = false }
        };

        var job = new PropertyTaxJobEntity
        {
            Id = 123,
            JobCode = "JOB-123",
            Operation = "AddTax",
            FinanceYearId = 2,
            FinanceYear = new YearMasterEntity { Id = 2, Year = 2026, YearCode = "2026-27" },
            ScopeType = "Property",
            ScopeParamsJson = "{\"PropertyIds\":[1]}",
            Status = "Pending",
            IsActive = true,
            MarkedForDeletion = false,
            StartedByUserId = 7
        };

        _propertyRepo.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _detailsRepo.Setup(r => r.GetQueryable()).Returns(details.BuildMock());
        _lockRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyScreenLockEntity>().BuildMock());
        _policyRepo.Setup(r => r.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>().BuildMock());
        _jobRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyTaxJobEntity> { job }.BuildMock());
        _jobDetailRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyTaxJobDetailEntity>().BuildMock());

        _policyConfig.Setup(p => p.GetPolicyValueAsync("TaxCalculationMethod", "RV", It.IsAny<CancellationToken>()))
            .ReturnsAsync("CV");

        var cvResults = new List<CapitalValueDto>
        {
            new()
            {
                PropertyId = 1,
                PropertyDetailsId = 10,
                Taxes = new List<TaxHeadDto>
                {
                    new() { TaxName = "WaterTax", Amount = 150m }
                }
            }
        };

        _cv.Setup(s => s.CreateAsync(It.IsAny<CreateCapitalValueDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cvResults);

        var service = CreateService();

        // Act
        await service.ProcessJobAsync(123, CancellationToken.None);

        // Assert
        _cv.Verify(s => s.CreateAsync(It.Is<CreateCapitalValueDto>(dto => dto.PropertyId == 1), It.IsAny<CancellationToken>()), Times.Once);
        _rv.Verify(s => s.CalculateAndSaveAsync(It.IsAny<int>()), Times.Never);

        job.Status.Should().Be("Completed");
        job.SuccessCount.Should().Be(1);
        job.SkippedCount.Should().Be(0);
        job.RecordsProcessed.Should().Be(1);
    }
}
