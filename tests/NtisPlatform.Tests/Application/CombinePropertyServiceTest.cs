using AutoMapper;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class CombinePropertyServiceTest
{
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IRepository<WardEntity, int>> _mockWardRepository;
    private readonly Mock<IRepository<TransMastRVEntity>> _mockTransMastRepository;
    private readonly Mock<IRepository<TaxPendingDetailsEntity>> _mockTaxPendingRepository;
    private readonly Mock<IRepository<CombinePropertyHistoryEntity>> _mockCombineHistoryRepository;
    private readonly Mock<IRepository<PropertyMastOldEntity, int>> _mockPropertyMastOldRepository;
    private readonly Mock<IRepository<PropertyTypeMasterEntity, int>> _mockPropertyTypeMasterRepository;
    private readonly Mock<IRepository<PropertyCategoryEntity, int>> _mockCategoryRepository;
    private readonly Mock<ICombinePropertyValidator> _mockValidator;
    private readonly Mock<IPropertyDataCopier> _mockDataCopier;
    private readonly Mock<IPropertyDeactivator> _mockDeactivator;
    private readonly Mock<ICombinePropertyTaxService> _mockCombinePropertyTaxService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<CombinePropertyService>> _mockLogger;
    private readonly CombinePropertyService _service;

    public CombinePropertyServiceTest()
    {
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockWardRepository = new Mock<IRepository<WardEntity, int>>();
        _mockTransMastRepository = new Mock<IRepository<TransMastRVEntity>>();
        _mockTaxPendingRepository = new Mock<IRepository<TaxPendingDetailsEntity>>();
        _mockCombineHistoryRepository = new Mock<IRepository<CombinePropertyHistoryEntity>>();
        _mockPropertyMastOldRepository = new Mock<IRepository<PropertyMastOldEntity, int>>();
        _mockPropertyTypeMasterRepository = new Mock<IRepository<PropertyTypeMasterEntity, int>>();
        _mockCategoryRepository = new Mock<IRepository<PropertyCategoryEntity, int>>();
        _mockValidator = new Mock<ICombinePropertyValidator>();
        _mockDataCopier = new Mock<IPropertyDataCopier>();
        _mockDeactivator = new Mock<IPropertyDeactivator>();
        _mockCombinePropertyTaxService = new Mock<ICombinePropertyTaxService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<CombinePropertyService>>();

        // Standard UnitOfWork setup
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Default empty history setup for duplicate checks
        _mockCombineHistoryRepository.Setup(r => r.GetQueryable())
            .Returns(new List<CombinePropertyHistoryEntity>().BuildMock());

        // Default empty property type setup for combine details lookup
        _mockPropertyTypeMasterRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyTypeMasterEntity>().BuildMock());

        // Default CombinePropertyTaxService setup
        _mockCombinePropertyTaxService.Setup(t => t.ProcessCombinePropertyTaxesAsync(
            It.IsAny<int>(), It.IsAny<List<int>>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _service = new CombinePropertyService(
            _mockRepository.Object,
            _mockWardRepository.Object,
            _mockTransMastRepository.Object,
            _mockTaxPendingRepository.Object,
            _mockCombineHistoryRepository.Object,
            _mockPropertyMastOldRepository.Object,
            _mockPropertyTypeMasterRepository.Object,
            _mockCategoryRepository.Object,
            _mockValidator.Object,
            _mockDataCopier.Object,
            _mockDeactivator.Object,
            _mockCombinePropertyTaxService.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockLogger.Object
        );
    }

    #region GetPropertyCombineDetailsAsync Tests

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_WardIdNull_ReturnsEmptyList()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = null,
            PropertyNo = "1",
            PartitionNo = "A"
        };

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_PropertyNoNull_ReturnsAllPropertiesInWard()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 1,
            PropertyNo = null,
            PartitionNo = null
        };

        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, PropertyNo = "1", PartitionNo = "A", OwnerName = "John", IsActive = true },
            new() { Id = 2, WardId = 1, PropertyNo = "2", PartitionNo = "B", OwnerName = "Jane", IsActive = true }
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WardEntity { Id = 1, WardNo = "WARD1", IsActive = true });
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert - PropertyNo is now optional, so it should return all properties in ward
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_PartitionNoNull_ReturnsAllPropertiesMatchingPropertyNo()
    {
        // Arrange - PartitionNo null means no partition filter, should return all properties for PropertyNo
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 1,
            PropertyNo = "1",
            PartitionNo = null
        };

        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, PropertyNo = "1", PartitionNo = "A", OwnerName = "John", IsActive = true },
            new() { Id = 2, WardId = 1, PropertyNo = "1", PartitionNo = "B", OwnerName = "Jane", IsActive = true },
            new() { Id = 3, WardId = 1, PropertyNo = "2", PartitionNo = "A", OwnerName = "Bob", IsActive = true }
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WardEntity { Id = 1, WardNo = "WARD1", IsActive = true });
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert - Should return all properties with PropertyNo "1" (no partition filter)
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal("1", r.PropertyNo));
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_WhitespacePartitionNo_ReturnsEmptyList()
    {
        // Arrange - Whitespace-only partition filter after parsing results in empty list
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 1,
            PropertyNo = "1",
            PartitionNo = "   ,  ,  "
        };

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_EmptyPropertyNo_ReturnsAllPropertiesInWard()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 1,
            PropertyNo = "",
            PartitionNo = null
        };

        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, PropertyNo = "1", PartitionNo = "A", OwnerName = "John", IsActive = true }
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WardEntity { Id = 1, WardNo = "WARD1", IsActive = true });
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert - PropertyNo is now optional, so it should return all properties in ward
        Assert.Single(result);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_EmptyPartitionNo_ReturnsEmptyList()
    {
        // Arrange - Empty string partition filter results in empty list (no valid partitions to filter)
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 1,
            PropertyNo = "1",
            PartitionNo = ""
        };

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert - Empty partition string means no valid partition filter, returns empty
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_ValidParams_ReturnsData()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 60,
            PropertyNo = "1",
            PartitionNo = "A,B"
        };

        var ward = new WardEntity { Id = 60, WardNo = "WARD60", IsActive = true };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 60, PropertyNo = "1", PartitionNo = "A", OwnerName = "Owner A", PropertyMastOldId = 1, PropertyTypeId = 101, IsActive = true },
            new() { Id = 2, WardId = 60, PropertyNo = "1", PartitionNo = "B", OwnerName = "Owner B", PropertyTypeId = 102, IsActive = true }
        };
        var propertyMastOld = new List<PropertyMastOldEntity>
        {
            new() { Id = 1, OldPropertyNo = "OLD-1", IsActive = true, MarkedForDeletion = false }
        };
        var propertyTypes = new List<PropertyTypeMasterEntity>
        {
            new() { Id = 101, PropertyDescription = "Residential", IsActive = true },
            new() { Id = 102, PropertyDescription = "Commercial", IsActive = true }
        };
        var transMast = new List<TransMastRVEntity>
        {
            new() { Id = 1, PropertyId = 1, TaxAmount = 1000, FinanceYearId = 1, TaxId = 1,  RateableValue = 50000, IsActive = true },
            new() { Id = 2, PropertyId = 1, TaxAmount = 500, FinanceYearId = 1, TaxId = 1,  RateableValue = 50000, IsActive = true }
        };
        var taxPending = new List<TaxPendingDetailsEntity>
        {
            new() { Id = 1, PropertyId = 1, PendingAmount = 500, IsActive = true }
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(propertyMastOld.BuildMock());
        _mockPropertyTypeMasterRepository.Setup(r => r.GetQueryable()).Returns(propertyTypes.BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(transMast.BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(taxPending.BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("WARD60", result[0].WardNo);
        Assert.Equal("Owner A", result[0].OwnerName);
        Assert.Equal("Residential", result[0].PropertyDescription);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_NoMatchingProperties_ReturnsEmptyList()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 60,
            PropertyNo = "999",
            PartitionNo = "Z"
        };

        var ward = new WardEntity { Id = 60, WardNo = "WARD60", IsActive = true };

        _mockWardRepository.Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>().BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_WithTaxData_ReturnsCorrectAmounts()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 60,
            PropertyNo = "1",
            PartitionNo = "A"
        };

        var ward = new WardEntity { Id = 60, WardNo = "WARD60", IsActive = true };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 60, PropertyNo = "1", PartitionNo = "A", OwnerName = "Owner A", PropertyMastOldId = 1, IsActive = true }
        };
        var propertyMastOld = new List<PropertyMastOldEntity>();
        var transMast = new List<TransMastRVEntity>
        {
            new() { Id = 1, PropertyId = 1, TaxAmount = 1000, FinanceYearId = 1, TaxId = 1, RateableValue = 50000, IsActive = true },
            new() { Id = 2, PropertyId = 1, TaxAmount = 500, FinanceYearId = 1, TaxId = 1, RateableValue = 50000, IsActive = true }
        };
        var taxPending = new List<TaxPendingDetailsEntity>
        {
            new() { Id = 1, PropertyId = 1, PendingAmount = 200, IsActive = true },
            new() { Id = 2, PropertyId = 1, PendingAmount = 300, IsActive = true }
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(propertyMastOld.BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(transMast.BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(taxPending.BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert
        Assert.Single(result);
        Assert.Equal(1500, result[0].TaxAmount);
        Assert.Equal(500, result[0].PendingAmount);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_InactiveProperties_NotIncluded()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 60,
            PropertyNo = "1",
            PartitionNo = "A,B"
        };

        var ward = new WardEntity { Id = 60, WardNo = "WARD60", IsActive = true };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 60, PropertyNo = "1", PartitionNo = "A", OwnerName = "Owner A", PropertyMastOldId = 1, IsActive = true },
            new() { Id = 2, WardId = 60, PropertyNo = "1", PartitionNo = "B", OwnerName = "Owner B", IsActive = false }
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert
        Assert.Single(result);
        Assert.Equal("A", result[0].PartitionNo);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_NullOwnerName_ReturnsEmptyString()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 60,
            PropertyNo = "1",
            PartitionNo = "A"
        };

        var ward = new WardEntity { Id = 60, WardNo = "WARD60", IsActive = true };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 60, PropertyNo = "1", PartitionNo = "A", OwnerName = null, OccupierName = null, IsActive = true }
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].OwnerName);
        Assert.Equal(string.Empty, result[0].OccupierName);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_WardNotFound_ReturnsEmptyWardNo()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 999,
            PropertyNo = "1",
            PartitionNo = "A"
        };

        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 999, PropertyNo = "1", PartitionNo = "A", OwnerName = "Owner A", IsActive = true }
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((WardEntity?)null);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].WardNo);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_WithZeroTaxAmounts_ReturnsZero()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 60,
            PropertyNo = "1",
            PartitionNo = "A"
        };

        var ward = new WardEntity { Id = 60, WardNo = "WARD60", IsActive = true };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 60, PropertyNo = "1", PartitionNo = "A", OwnerName = "Owner A", IsActive = true }
        };
        var transMast = new List<TransMastRVEntity>
        {
            new() { Id = 1, PropertyId = 1, TaxAmount = 0, FinanceYearId = 1, TaxId = 1, RateableValue = 50000, IsActive = true }
        };
        var taxPending = new List<TaxPendingDetailsEntity>
        {
            new() { Id = 1, PropertyId = 1, PendingAmount = null, IsActive = true }
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(transMast.BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(taxPending.BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert
        Assert.Single(result);
        Assert.Equal(0, result[0].TaxAmount);
        Assert.Equal(0, result[0].PendingAmount);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_WithMarkedForDeletionOldProperty_ExcludesOldPropertyNo()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 60,
            PropertyNo = "1",
            PartitionNo = "A"
        };

        var ward = new WardEntity { Id = 60, WardNo = "WARD60", IsActive = true };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 60, PropertyNo = "1", PartitionNo = "A", OwnerName = "Owner A", PropertyMastOldId = 1, IsActive = true }
        };
        var propertyMastOld = new List<PropertyMastOldEntity>
        {
            new() { Id = 1, OldPropertyNo = "OLD-1", IsActive = true, MarkedForDeletion = true }
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(propertyMastOld.BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].OldPropertyNo);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_WithInactiveOldProperty_ExcludesOldPropertyNo()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 60,
            PropertyNo = "1",
            PartitionNo = "A"
        };

        var ward = new WardEntity { Id = 60, WardNo = "WARD60", IsActive = true };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 60, PropertyNo = "1", PartitionNo = "A", OwnerName = "Owner A", PropertyMastOldId = 1, IsActive = true }
        };
        var propertyMastOld = new List<PropertyMastOldEntity>
        {
            new() { Id = 1, OldPropertyNo = "OLD-1", IsActive = false, MarkedForDeletion = false }
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(propertyMastOld.BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].OldPropertyNo);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_SinglePartition_ReturnsCorrectData()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 60,
            PropertyNo = "1",
            PartitionNo = "A"
        };

        var ward = new WardEntity { Id = 60, WardNo = "WARD60", IsActive = true };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 60, PropertyNo = "1", PartitionNo = "A", OwnerName = "Owner A", OccupierName = "Occupier A", PropertyMastOldId = 1, IsActive = true }
        };
        var propertyMastOld = new List<PropertyMastOldEntity>
        {
            new() { Id = 1, OldPropertyNo = "OLD-1", IsActive = true, MarkedForDeletion = false }
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(propertyMastOld.BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].PropertyId);
        Assert.Equal(60, result[0].WardId);
        Assert.Equal("WARD60", result[0].WardNo);
        Assert.Equal("1", result[0].PropertyNo);
        Assert.Equal("A", result[0].PartitionNo);
        Assert.Equal("OLD-1", result[0].OldPropertyNo);
        Assert.Equal("Owner A", result[0].OwnerName);
        Assert.Equal("Occupier A", result[0].OccupierName);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_MultiplePartitions_ReturnsAllMatchingRecords()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 60,
            PropertyNo = "1",
            PartitionNo = "A,B,C"
        };

        var ward = new WardEntity { Id = 60, WardNo = "WARD60", IsActive = true };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 60, PropertyNo = "1", PartitionNo = "A", OwnerName = "Owner A", PropertyMastOldId = 1, IsActive = true },
            new() { Id = 2, WardId = 60, PropertyNo = "1", PartitionNo = "B", OwnerName = "Owner A", PropertyMastOldId = 2, IsActive = true },
            new() { Id = 3, WardId = 60, PropertyNo = "1", PartitionNo = "C", OwnerName = "Owner A", PropertyMastOldId = 3, IsActive = true }
        };
        var propertyMastOld = new List<PropertyMastOldEntity>
        {
            new() { Id = 1, OldPropertyNo = "OLD-1", IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, OldPropertyNo = "OLD-2", IsActive = true, MarkedForDeletion = false },
            new() { Id = 3, OldPropertyNo = "OLD-3", IsActive = true, MarkedForDeletion = false }
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(propertyMastOld.BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, r => r.PartitionNo == "A");
        Assert.Contains(result, r => r.PartitionNo == "B");
        Assert.Contains(result, r => r.PartitionNo == "C");
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_WithSpecificPartitionNo_ExcludesNullPartitions()
    {
        // Arrange - When filtering by specific partition, null partitions should NOT be included
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 60,
            PropertyNo = "1",
            PartitionNo = "A"
        };

        var ward = new WardEntity { Id = 60, WardNo = "WARD60", IsActive = true };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 60, PropertyNo = "1", PartitionNo = "A", OwnerName = "Owner A", PropertyTypeId = 101, IsActive = true },
            new() { Id = 2, WardId = 60, PropertyNo = "1", PartitionNo = null, OwnerName = "Owner B", PropertyTypeId = 101, IsActive = true }
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockPropertyTypeMasterRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyTypeMasterEntity>().BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert - Only partition "A" should be returned, not null partition
        Assert.Single(result);
        Assert.Equal("A", result[0].PartitionNo);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_WithoutPartitionNoFilter_IncludesAllPartitions()
    {
        // Arrange - When NO partition filter is specified, all partitions SHOULD be included
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 60,
            PropertyNo = "1",
            PartitionNo = null  // No partition filter
        };

        var ward = new WardEntity { Id = 60, WardNo = "WARD60", IsActive = true };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 60, PropertyNo = "1", PartitionNo = "A", OwnerName = "Owner A", PropertyTypeId = 101, IsActive = true },
            new() { Id = 2, WardId = 60, PropertyNo = "1", PartitionNo = null, OwnerName = "Owner B", PropertyTypeId = 101, IsActive = true }
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockPropertyTypeMasterRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyTypeMasterEntity>().BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert - Both partitions should be returned when no filter is specified
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.PartitionNo == "A");
        Assert.Contains(result, r => r.PartitionNo == null);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_WithZeroPlaceholder_IncludesEmptyPartitions()
    {
        // Arrange - "0" represents empty/blank partition numbers
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 60,
            PropertyNo = "1",
            PartitionNo = "0,A,A1,A2"  // "0" = empty partition
        };

        var ward = new WardEntity { Id = 60, WardNo = "WARD60", IsActive = true };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 60, PropertyNo = "1", PartitionNo = "", OwnerName = "Owner Empty", IsActive = true },  // Empty partition
            new() { Id = 2, WardId = 60, PropertyNo = "1", PartitionNo = null, OwnerName = "Owner Null", IsActive = true },  // Null partition
            new() { Id = 3, WardId = 60, PropertyNo = "1", PartitionNo = "A", OwnerName = "Owner A", IsActive = true },
            new() { Id = 4, WardId = 60, PropertyNo = "1", PartitionNo = "A1", OwnerName = "Owner A1", IsActive = true },
            new() { Id = 5, WardId = 60, PropertyNo = "1", PartitionNo = "A2", OwnerName = "Owner A2", IsActive = true },
            new() { Id = 6, WardId = 60, PropertyNo = "1", PartitionNo = "B", OwnerName = "Owner B", IsActive = true }  // Should NOT be included
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockPropertyTypeMasterRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyTypeMasterEntity>().BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert - Should return empty/null partitions + A, A1, A2 (but NOT B)
        Assert.Equal(5, result.Count);
        Assert.Contains(result, r => string.IsNullOrWhiteSpace(r.PartitionNo));  // Empty or null
        Assert.Contains(result, r => r.PartitionNo == "A");
        Assert.Contains(result, r => r.PartitionNo == "A1");
        Assert.Contains(result, r => r.PartitionNo == "A2");
        Assert.DoesNotContain(result, r => r.PartitionNo == "B");
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_WithOnlyZeroPlaceholder_ReturnsOnlyEmptyPartitions()
    {
        // Arrange - Only "0" means only empty partitions
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 60,
            PropertyNo = "1",
            PartitionNo = "0"  // Only empty partitions
        };

        var ward = new WardEntity { Id = 60, WardNo = "WARD60", IsActive = true };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 60, PropertyNo = "1", PartitionNo = "", OwnerName = "Owner Empty", IsActive = true },
            new() { Id = 2, WardId = 60, PropertyNo = "1", PartitionNo = null, OwnerName = "Owner Null", IsActive = true },
            new() { Id = 3, WardId = 60, PropertyNo = "1", PartitionNo = "A", OwnerName = "Owner A", IsActive = true }  // Should NOT be included
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>())).ReturnsAsync(ward);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockPropertyTypeMasterRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyTypeMasterEntity>().BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert - Should only return properties with empty/null partitions
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(string.IsNullOrWhiteSpace(r.PartitionNo)));
    }

    #endregion

    #region CombinePropertiesAsync Tests

    [Fact]
    public async Task CombinePropertiesAsync_EmptyPropertyIds_ReturnsFailure()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "",
            CombineReason = "Test"
        };

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No valid property IDs provided", result.Message);
    }

    [Fact]
    public async Task CombinePropertiesAsync_WhitespacePropertyIds_ReturnsFailure()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "   ",
            CombineReason = "Test"
        };

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No valid property IDs provided", result.Message);
    }

    [Fact]
    public async Task CombinePropertiesAsync_InvalidPropertyIds_ReturnsFailure()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "abc,xyz",
            CombineReason = "Test"
        };

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No valid property IDs provided", result.Message);
    }

    [Fact]
    public async Task CombinePropertiesAsync_MainPropertyNotFound_ReturnsFailure()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 999,
            CombinedPropertyIds = "2,3",
            CombineReason = "Test"
        };

        // Setup repository to return null for source property (defensive check)
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyEntity?)null);

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("SourcePropertyId not found", result.Message);
        Assert.Equal(999, result.SourcePropertyId);

        // Verify validator was never called because defensive check caught it first
        _mockValidator.Verify(v => v.ValidatePropertiesForCombinationAsync(It.IsAny<int>(), It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CombinePropertiesAsync_MainPropertyNotFoundInValidator_ReturnsFailure()
    {
        // Arrange - Test the validator path (when defensive check passes but validator fails)
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 888,
            CombinedPropertyIds = "2,3",
            CombineReason = "Test"
        };

        // Setup repository to return a valid property for defensive check
        _mockRepository.Setup(r => r.GetByIdAsync(888, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyEntity { Id = 888, OwnerName = "Test Owner", IsActive = true });

        // But validator still reports not found (e.g., different repository instance)
        _mockValidator.Setup(v => v.ValidatePropertiesForCombinationAsync(888, It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "SourcePropertyId not found.", new List<PropertyEntity>()));

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("SourcePropertyId not found", result.Message);
    }

    [Fact]
    public async Task CombinePropertiesAsync_CombinedPropertyIdsNotFound_ReturnsFailure()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "999,998",
            CombineReason = "Test"
        };

        // Setup repository to return a valid property for defensive check
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyEntity { Id = 1, OwnerName = "Test Owner", IsActive = true });

        _mockValidator.Setup(v => v.ValidatePropertiesForCombinationAsync(1, It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "One or more CombinedPropertyIds not found.", new List<PropertyEntity>()));

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("One or more CombinedPropertyIds not found", result.Message);
    }

    [Fact]
    public async Task CombinePropertiesAsync_OwnerNameMismatch_ReturnsFailure()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "2,3",
            CombineReason = "Test"
        };

        // Setup repository to return a valid property for defensive check
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyEntity { Id = 1, OwnerName = "Test Owner", IsActive = true });

        _mockValidator.Setup(v => v.ValidatePropertiesForCombinationAsync(1, It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Owner name must match for all properties.", new List<PropertyEntity>()));

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Owner name must match for all properties.", result.Message);
    }

    [Fact]
    public async Task CombinePropertiesAsync_AllIdsAreMainPropertyOrDuplicates_ReturnsFailure()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "1,1,1",
            CombineReason = "Test",
            CreatedBy = 100
        };

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No valid property IDs provided to combine", result.Message);
    }

    [Fact]
    public async Task CombinePropertiesAsync_NegativePropertyIds_AreFiltered()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "-1,-2,-3",
            CombineReason = "Test negative IDs"
        };

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No valid property IDs provided", result.Message);
    }

    [Fact]
    public async Task CombinePropertiesAsync_ZeroPropertyIds_AreFiltered()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "0,0,0",
            CombineReason = "Test zero IDs"
        };

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No valid property IDs provided", result.Message);
    }

    [Fact]
    public async Task CombinePropertiesAsync_SomeCombinedPropertiesInactive_ReturnsFailure()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "2,3",
            CombineReason = "Test"
        };

        // Setup repository to return a valid property for defensive check
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyEntity { Id = 1, OwnerName = "Test Owner", IsActive = true });

        _mockValidator.Setup(v => v.ValidatePropertiesForCombinationAsync(1, It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "One or more CombinedPropertyIds not found.", new List<PropertyEntity>()));

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("One or more CombinedPropertyIds not found", result.Message);
    }

    [Fact]
    public async Task CombinePropertiesAsync_MixedValidAndInvalidIds_FiltersInvalid()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "2,abc,3,-5,0",
            CombineReason = "Test mixed IDs"
        };

        // Setup repository to return a valid property for defensive check
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyEntity { Id = 1, OwnerName = "Test Owner", IsActive = true });

        _mockValidator.Setup(v => v.ValidatePropertiesForCombinationAsync(1, It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Owner name must match for all properties.", new List<PropertyEntity>()));

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert - Should fail because owner name mismatch, but validates that valid IDs (2,3) were processed
        Assert.False(result.Success);
        Assert.Contains("Owner name must match for all properties.", result.Message);
    }

    [Fact]
    public async Task CombinePropertiesAsync_TaxProcessingThrows_AfterCommit_StillReturnsSuccess()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "2,3",
            CombineReason = "Test",
            CreatedBy = 100
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyEntity { Id = 1, OwnerName = "Main Owner", IsActive = true });

        _mockValidator.Setup(v => v.ValidatePropertiesForCombinationAsync(1, It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, string.Empty, new List<PropertyEntity>()));

        _mockDataCopier.Setup(d => d.CopyPropertyDataAsync(
                1,
                It.IsAny<List<int>>(),
                100,
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDeactivator.Setup(d => d.DeactivateCombinedPropertiesAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDeactivator.Setup(d => d.EnsureMainPropertyRecordsActiveAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockCombinePropertyTaxService.Setup(t => t.ProcessCombinePropertyTaxesAsync(
                1,
                It.IsAny<List<int>>(),
                100,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Tax processing failure"));

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Properties combined successfully.", result.Message);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetCombinePropertyHistoryAsync Tests

    [Fact]
    public async Task GetCombinePropertyHistoryAsync_NullSourcePropertyId_ReturnsSourcePropertiesWithCombineReason()
    {
        // Arrange - No SourcePropertyId filter, should return distinct source properties from history with CombineReason
        int? sourcePropertyId = null;

        var historyRecords = new List<CombinePropertyHistoryEntity>
        {
            new() { Id = 1, SourcePropertyId = 100, CombinedPropertyId = 101, CombineReason = "Merged adjacent properties", IsActive = true },
            new() { Id = 2, SourcePropertyId = 100, CombinedPropertyId = 102, CombineReason = "Merged adjacent properties", IsActive = true },
            new() { Id = 3, SourcePropertyId = 200, CombinedPropertyId = 201, CombineReason = "Adjacent plot combined", IsActive = true }
        };

        _mockCombineHistoryRepository.Setup(r => r.GetQueryable())
            .Returns(historyRecords.BuildMock());

        var properties = new List<PropertyEntity>
        {
            new() { Id = 100, WardId = 60, PropertyNo = "1", PartitionNo = "A1", OwnerName = "Source Owner 1", CategoryId = 6, IsActive = true },
            new() { Id = 200, WardId = 60, PropertyNo = "2", PartitionNo = "B1", OwnerName = "Source Owner 2", CategoryId = 6, IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockPropertyTypeMasterRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyTypeMasterEntity>().BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { new() { Id = 60, WardNo = "WARD60", IsActive = true } }.BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable())
            .Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable())
            .Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetCombinePropertyHistoryAsync(sourcePropertyId, default);

        // Assert - Should return only the source properties (100 and 200), not combined
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.PropertyId == 100);
        Assert.Contains(result, r => r.PropertyId == 200);
        // Source properties SHOULD have CombineReason from the history table
        Assert.Equal("Merged adjacent properties", result.First(r => r.PropertyId == 100).CombineReason);
        Assert.Equal("Adjacent plot combined", result.First(r => r.PropertyId == 200).CombineReason);
    }

    [Fact]
    public async Task GetCombinePropertyHistoryAsync_NullSourcePropertyId_NoHistory_ReturnsEmptyList()
    {
        // Arrange - No SourcePropertyId filter and no history records
        int? sourcePropertyId = null;

        _mockCombineHistoryRepository.Setup(r => r.GetQueryable())
            .Returns(new List<CombinePropertyHistoryEntity>().BuildMock());

        // Act
        var result = await _service.GetCombinePropertyHistoryAsync(sourcePropertyId, default);

        // Assert - Should return empty list when no history exists
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCombinePropertyHistoryAsync_WithSourcePropertyId_ReturnsOnlyCombinedProperties()
    {
        // Arrange - With SourcePropertyId, should return ONLY combined properties (not source)
        int? sourcePropertyId = 100;

        var historyRecords = new List<CombinePropertyHistoryEntity>
        {
            new() { Id = 1, SourcePropertyId = 100, CombinedPropertyId = 101, CombineReason = "Adjacent properties merged", IsActive = true },
            new() { Id = 2, SourcePropertyId = 100, CombinedPropertyId = 102, CombineReason = "Adjacent properties merged", IsActive = true }
        };

        _mockCombineHistoryRepository.Setup(r => r.GetQueryable())
            .Returns(historyRecords.BuildMock());

        var properties = new List<PropertyEntity>
        {
            new() { Id = 101, WardId = 60, PropertyNo = "1", PartitionNo = "A2", OwnerName = "Combined Owner 1", CategoryId = 6, IsActive = false },
            new() { Id = 102, WardId = 60, PropertyNo = "1", PartitionNo = "A3", OwnerName = "Combined Owner 2", CategoryId = 6, IsActive = false }
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockPropertyTypeMasterRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyTypeMasterEntity>().BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { new() { Id = 60, WardNo = "WARD60", IsActive = true } }.BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable())
            .Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable())
            .Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetCombinePropertyHistoryAsync(sourcePropertyId, default);

        // Assert - Should return ONLY combined properties (101 and 102), NOT source (100)
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, r => r.PropertyId == 100); // Source should NOT be included
        Assert.Contains(result, r => r.PropertyId == 101);
        Assert.Contains(result, r => r.PropertyId == 102);
        // Combined properties should have CombineReason
        Assert.Equal("Adjacent properties merged", result.First(r => r.PropertyId == 101).CombineReason);
        Assert.Equal("Adjacent properties merged", result.First(r => r.PropertyId == 102).CombineReason);
    }

    [Fact]
    public async Task GetCombinePropertyHistoryAsync_WithSourcePropertyId_NoHistory_ReturnsEmptyList()
    {
        // Arrange - SourcePropertyId provided but no matching history records
        int? sourcePropertyId = 999;

        _mockCombineHistoryRepository.Setup(r => r.GetQueryable())
            .Returns(new List<CombinePropertyHistoryEntity>().BuildMock());

        // Act
        var result = await _service.GetCombinePropertyHistoryAsync(sourcePropertyId, default);

        // Assert - Should return empty list when no history exists for the source
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCombinePropertyHistoryAsync_WithSourcePropertyId_ReturnsPropertyDetails()
    {
        // Arrange - Verify that combined properties include all expected details
        int? sourcePropertyId = 100;

        var historyRecords = new List<CombinePropertyHistoryEntity>
        {
            new() { Id = 1, SourcePropertyId = 100, CombinedPropertyId = 101, CombineReason = "Merged", IsActive = true }
        };

        _mockCombineHistoryRepository.Setup(r => r.GetQueryable())
            .Returns(historyRecords.BuildMock());

        var properties = new List<PropertyEntity>
        {
            new() { Id = 101, WardId = 60, PropertyNo = "1", PartitionNo = "A2", OwnerName = "Combined Owner", OccupierName = "Combined Occupier", CategoryId = 6, PropertyTypeId = 101, PropertyMastOldId = 1, IsActive = false }
        };

        var propertyMastOld = new List<PropertyMastOldEntity>
        {
            new() { Id = 1, OldPropertyNo = "OLD-101", IsActive = true, MarkedForDeletion = false }
        };

        var propertyTypes = new List<PropertyTypeMasterEntity>
        {
            new() { Id = 101, PropertyDescription = "Residential", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable())
            .Returns(propertyMastOld.BuildMock());
        _mockPropertyTypeMasterRepository.Setup(r => r.GetQueryable())
            .Returns(propertyTypes.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { new() { Id = 60, WardNo = "WARD60", IsActive = true } }.BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable())
            .Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable())
            .Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetCombinePropertyHistoryAsync(sourcePropertyId, default);

        // Assert - Verify all property details are returned
        Assert.Single(result);
        var combinedProperty = result[0];
        Assert.Equal(101, combinedProperty.PropertyId);
        Assert.Equal(60, combinedProperty.WardId);
        Assert.Equal("WARD60", combinedProperty.WardNo);
        Assert.Equal("1", combinedProperty.PropertyNo);
        Assert.Equal("A2", combinedProperty.PartitionNo);
        Assert.Equal("OLD-101", combinedProperty.OldPropertyNo);
        Assert.Equal("Combined Owner", combinedProperty.OwnerName);
        Assert.Equal("Combined Occupier", combinedProperty.OccupierName);
        Assert.Equal(6, combinedProperty.CategoryId);
        Assert.Equal(101, combinedProperty.PropertyTypeId);
        Assert.Equal("Residential", combinedProperty.PropertyDescription);
        Assert.Equal("Merged", combinedProperty.CombineReason);
    }

    [Fact]
    public async Task GetCombinePropertyHistoryAsync_NullSourcePropertyId_ReturnsDistinctSourcePropertiesWithFirstCombineReason()
    {
        // Arrange - Verify that same source with multiple combined returns only once with first CombineReason
        int? sourcePropertyId = null;

        var historyRecords = new List<CombinePropertyHistoryEntity>
        {
            new() { Id = 1, SourcePropertyId = 100, CombinedPropertyId = 101, CombineReason = "First reason", IsActive = true },
            new() { Id = 2, SourcePropertyId = 100, CombinedPropertyId = 102, CombineReason = "Second reason", IsActive = true },
            new() { Id = 3, SourcePropertyId = 100, CombinedPropertyId = 103, CombineReason = "Third reason", IsActive = true }
        };

        _mockCombineHistoryRepository.Setup(r => r.GetQueryable())
            .Returns(historyRecords.BuildMock());

        var properties = new List<PropertyEntity>
        {
            new() { Id = 100, WardId = 60, PropertyNo = "1", PartitionNo = "A1", OwnerName = "Source Owner", CategoryId = 6, IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(properties.BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockPropertyTypeMasterRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyTypeMasterEntity>().BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { new() { Id = 60, WardNo = "WARD60", IsActive = true } }.BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable())
            .Returns(new List<TransMastRVEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable())
            .Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetCombinePropertyHistoryAsync(sourcePropertyId, default);

        Assert.Single(result);
        Assert.Equal(100, result[0].PropertyId);
        Assert.Equal("First reason", result[0].CombineReason); // First CombineReason from history
    }

    [Fact]
    public async Task GetAllAsync_WithWardIdSorting_PerformsDatabaseSortingAndPaging()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 2, PropertyNo = "10", PartitionNo = "A", IsActive = true },
            new() { Id = 2, WardId = 1, PropertyNo = "2", PartitionNo = "B", IsActive = true },
            new() { Id = 3, WardId = 1, PropertyNo = "1", PartitionNo = "C", IsActive = true }
        };
        var wards = new List<WardEntity>
        {
            new() { Id = 1, WardNo = "WARD1", IsActive = true },
            new() { Id = 2, WardNo = "WARD2", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyCategoryEntity?)null);

        var queryParams = new CombinePropertyQueryParameters
        {
            SortBy = "wardid",
            SortOrder = "ASC",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        var items = result.Items.ToList();
        // Since sorted by WardId ASC, items in WardId 1 should come before WardId 2
        Assert.Equal(1, items[0].WardId);
        Assert.Equal(1, items[1].WardId);
        Assert.Equal(2, items[2].WardId);
    }

    [Fact]
    public async Task GetAllAsync_WithPropertyNoSorting_PerformsNaturalInMemorySorting()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, PropertyNo = "10", PartitionNo = "A", IsActive = true },
            new() { Id = 2, WardId = 1, PropertyNo = "2", PartitionNo = "B", IsActive = true },
            new() { Id = 3, WardId = 1, PropertyNo = "1", PartitionNo = "C", IsActive = true }
        };
        var wards = new List<WardEntity>
        {
            new() { Id = 1, WardNo = "WARD1", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyCategoryEntity?)null);

        var queryParams = new CombinePropertyQueryParameters
        {
            SortBy = "propertyno",
            SortOrder = "ASC",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        var items = result.Items.ToList();
        // Sorted naturally by PropertyNo ASC: "1" -> "2" -> "10"
        Assert.Equal("1", items[0].PropertyNo);
        Assert.Equal("2", items[1].PropertyNo);
        Assert.Equal("10", items[2].PropertyNo);
    }

    [Fact]
    public async Task GetAllAsync_WithPropertyNoSorting_PerformsNaturalInMemorySorting_MixedValues()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, PropertyNo = "A10", PartitionNo = "A", IsActive = true },
            new() { Id = 2, WardId = 1, PropertyNo = "A2", PartitionNo = "B", IsActive = true },
            new() { Id = 3, WardId = 1, PropertyNo = "A1", PartitionNo = "C", IsActive = true }
        };
        var wards = new List<WardEntity>
        {
            new() { Id = 1, WardNo = "WARD1", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyCategoryEntity?)null);

        var queryParams = new CombinePropertyQueryParameters
        {
            SortBy = "propertyno",
            SortOrder = "ASC",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        var items = result.Items.ToList();
        // Sorted naturally by PropertyNo ASC: "A1" -> "A2" -> "A10"
        Assert.Equal("A1", items[0].PropertyNo);
        Assert.Equal("A2", items[1].PropertyNo);
        Assert.Equal("A10", items[2].PropertyNo);
    }

    [Fact]
    public async Task GetAllAsync_WithDefaultSorting_PerformsSortingByWardPropertyAndPartition()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 2, PropertyNo = "1", PartitionNo = "A", IsActive = true },
            new() { Id = 2, WardId = 1, PropertyNo = "10", PartitionNo = "A", IsActive = true },
            new() { Id = 3, WardId = 1, PropertyNo = "2", PartitionNo = "A", IsActive = true }
        };
        var wards = new List<WardEntity>
        {
            new() { Id = 1, WardNo = "WARD1", IsActive = true },
            new() { Id = 2, WardNo = "WARD2", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyCategoryEntity?)null);

        var queryParams = new CombinePropertyQueryParameters
        {
            SortBy = null, // Trigger default sort
            SortOrder = "ASC",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, default);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        // Expected order:
        // 1. WardId = 1, PropertyNo = "2"
        // 2. WardId = 1, PropertyNo = "10"
        // 3. WardId = 2, PropertyNo = "1"
        Assert.Equal(1, items[0].WardId);
        Assert.Equal("2", items[0].PropertyNo);
        Assert.Equal(1, items[1].WardId);
        Assert.Equal("10", items[1].PropertyNo);
        Assert.Equal(2, items[2].WardId);
        Assert.Equal("1", items[2].PropertyNo);
    }

    [Fact]
    public async Task GetAllAsync_WithStablePaging_ReturnsConsistentPagedResults()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 2, PropertyNo = "1", PartitionNo = "B", IsActive = true },
            new() { Id = 2, WardId = 1, PropertyNo = "1", PartitionNo = "C", IsActive = true },
            new() { Id = 3, WardId = 2, PropertyNo = "1", PartitionNo = "A", IsActive = true }
        };
        var wards = new List<WardEntity>
        {
            new() { Id = 1, WardNo = "WARD1", IsActive = true },
            new() { Id = 2, WardNo = "WARD2", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyCategoryEntity?)null);

        // Sort by PropertyNo. All have PropertyNo = "1".
        // Tie-breaker 1: WardId. So WardId = 1 comes first.
        // Tie-breaker 2: PartitionNo. So among WardId = 2, PartitionNo = "A" comes before PartitionNo = "B".
        // Complete expected order:
        // 1. PropertyId = 2 (WardId = 1, PartitionNo = "C")
        // 2. PropertyId = 3 (WardId = 2, PartitionNo = "A")
        // 3. PropertyId = 1 (WardId = 2, PartitionNo = "B")

        // Act & Assert Page 1 (Size = 1)
        var resultPage1 = await _service.GetAllAsync(new CombinePropertyQueryParameters
        {
            SortBy = "propertyno",
            SortOrder = "ASC",
            PageNumber = 1,
            PageSize = 1
        }, default);

        Assert.Single(resultPage1.Items);
        Assert.Equal(2, resultPage1.Items.First().Id);

        // Act & Assert Page 2 (Size = 1)
        var resultPage2 = await _service.GetAllAsync(new CombinePropertyQueryParameters
        {
            SortBy = "propertyno",
            SortOrder = "ASC",
            PageNumber = 2,
            PageSize = 1
        }, default);

        Assert.Single(resultPage2.Items);
        Assert.Equal(3, resultPage2.Items.First().Id);

        // Act & Assert Page 3 (Size = 1)
        var resultPage3 = await _service.GetAllAsync(new CombinePropertyQueryParameters
        {
            SortBy = "propertyno",
            SortOrder = "ASC",
            PageNumber = 3,
            PageSize = 1
        }, default);

        Assert.Single(resultPage3.Items);
        Assert.Equal(1, resultPage3.Items.First().Id);
    }

    #endregion
}


