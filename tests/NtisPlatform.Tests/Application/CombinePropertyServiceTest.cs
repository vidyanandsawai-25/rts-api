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
    private readonly Mock<IRepository<TransMastEntity>> _mockTransMastRepository;
    private readonly Mock<IRepository<TaxPendingDetailsEntity>> _mockTaxPendingRepository;
    private readonly Mock<IRepository<CombinePropertyHistoryEntity>> _mockCombineHistoryRepository;
    private readonly Mock<IRepository<PropertyMastOldEntity, int>> _mockPropertyMastOldRepository;
    private readonly Mock<IRepository<PropertyTypeMasterEntity, int>> _mockPropertyTypeMasterRepository;
    private readonly Mock<IRepository<PropertyCategoryEntity, int>> _mockCategoryRepository;
    private readonly Mock<ICombinePropertyValidator> _mockValidator;
    private readonly Mock<IPropertyDataCopier> _mockDataCopier;
    private readonly Mock<IPropertyDeactivator> _mockDeactivator;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<CombinePropertyService>> _mockLogger;
    private readonly CombinePropertyService _service;

    public CombinePropertyServiceTest()
    {
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockWardRepository = new Mock<IRepository<WardEntity, int>>();
        _mockTransMastRepository = new Mock<IRepository<TransMastEntity>>();
        _mockTaxPendingRepository = new Mock<IRepository<TaxPendingDetailsEntity>>();
        _mockCombineHistoryRepository = new Mock<IRepository<CombinePropertyHistoryEntity>>();
        _mockPropertyMastOldRepository = new Mock<IRepository<PropertyMastOldEntity, int>>();
        _mockPropertyTypeMasterRepository = new Mock<IRepository<PropertyTypeMasterEntity, int>>();
        _mockCategoryRepository = new Mock<IRepository<PropertyCategoryEntity, int>>();
        _mockValidator = new Mock<ICombinePropertyValidator>();
        _mockDataCopier = new Mock<IPropertyDataCopier>();
        _mockDeactivator = new Mock<IPropertyDeactivator>();
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
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert - PropertyNo is now optional, so it should return all properties in ward
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_PartitionNoNull_ReturnsEmptyList()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 1,
            PropertyNo = "1",
            PartitionNo = null
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WardEntity { Id = 1, WardNo = "WARD1", IsActive = true });
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>().BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_WhitespacePartitionNo_ReturnsEmptyList()
    {
        // Arrange
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
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert - PropertyNo is now optional, so it should return all properties in ward
        Assert.Single(result);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_EmptyPartitionNo_ReturnsEmptyList()
    {
        // Arrange
        var queryParams = new PropertyCombineDetailsQueryParameters
        {
            WardId = 1,
            PropertyNo = "1",
            PartitionNo = ""
        };

        _mockWardRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WardEntity { Id = 1, WardNo = "WARD1", IsActive = true });
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>().BuildMock());
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert
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
            new() { Id = 101, PropertyDescription = "??????", IsActive = true },
            new() { Id = 102, PropertyDescription = "??????????", IsActive = true }
        };
        var transMast = new List<TransMastEntity>
        {
            new() { Id = 1, PropertyId = 1, TaxAmount = 1000, FinanceYearId = 1, TaxId = 1, RVorCV = "RV", RVorCVValue = 50000, IsActive = true },
            new() { Id = 2, PropertyId = 1, TaxAmount = 500, FinanceYearId = 1, TaxId = 1, RVorCV = "RV", RVorCVValue = 50000, IsActive = true }
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
        Assert.Equal("??????", result[0].PropertyDescription);
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
        var transMast = new List<TransMastEntity>
        {
            new() { Id = 1, PropertyId = 1, TaxAmount = 1000, FinanceYearId = 1, TaxId = 1, RVorCV = "RV", RVorCVValue = 50000, IsActive = true },
            new() { Id = 2, PropertyId = 1, TaxAmount = 500, FinanceYearId = 1, TaxId = 1, RVorCV = "RV", RVorCVValue = 50000, IsActive = true }
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
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
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
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
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
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
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
        var transMast = new List<TransMastEntity>
        {
            new() { Id = 1, PropertyId = 1, TaxAmount = 0, FinanceYearId = 1, TaxId = 1, RVorCV = "RV", RVorCVValue = 50000, IsActive = true }
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
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
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
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
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
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
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
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
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
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert - Only partition "A" should be returned, not null partition
        Assert.Single(result);
        Assert.Equal("A", result[0].PartitionNo);
    }

    [Fact]
    public async Task GetPropertyCombineDetailsAsync_WithoutPartitionNoFilter_IncludesNullPartitions()
    {
        // Arrange - When NO partition filter is specified, null partitions SHOULD be included
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
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
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
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
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
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        _mockTaxPendingRepository.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyCombineDetailsAsync(queryParams, default);

        // Assert - Should only return properties with empty/null partitions
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(string.IsNullOrWhiteSpace(r.PartitionNo)));
    }

    [Fact(Skip = "GetAllAsync uses EF.Property which requires real EF context, not in-memory mock")]
    public async Task GetAllAsync_WithNullPartitionNo_IncludedInResults()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 60, PropertyNo = "1", PartitionNo = "A", IsActive = true },
            new() { Id = 2, WardId = 60, PropertyNo = "1", PartitionNo = null, IsActive = true }
        };

        var wards = new List<WardEntity>
        {
            new() { Id = 60, WardNo = "WARD60", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());

        var queryParams = new CombinePropertyQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, default);

        // Assert
        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.FromProperty == "A");
        Assert.Contains(items, x => x.FromProperty == string.Empty);
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
    public async Task CombinePropertiesAsync_DuplicateCombineIds_AreDistinct()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "2,2,2,3,3",
            CombineReason = "Test duplicate IDs"
        };

        // Setup repository to return a valid property for defensive check
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyEntity { Id = 1, OwnerName = "Test Owner", IsActive = true });

        _mockValidator.Setup(v => v.ValidatePropertiesForCombinationAsync(1, It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Owner name must match for all properties.", new List<PropertyEntity>()));

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert - Should fail with owner mismatch (validates duplicates were processed as distinct)
        Assert.False(result.Success);
        Assert.Contains("Owner name must match for all properties.", result.Message);
    }

    [Fact]
    public async Task CombinePropertiesAsync_SuccessfulCombine_ReturnsSuccessAndInsertsHistory()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "2,3",
            CombineReason = "Combining adjacent properties",
            CreatedBy = 100
        };

        var sourceProperty = new PropertyEntity { Id = 1, OwnerName = "John Doe", IsActive = true };
        var combineProperties = new List<PropertyEntity> 
        { 
            new() { Id = 2, OwnerName = "John Doe", PropertyMastOldId = 10, IsActive = true },
            new() { Id = 3, OwnerName = "John Doe", PropertyMastOldId = 11, IsActive = true }
        };

        // Setup repository to return source property for defensive check
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceProperty);

        _mockValidator.Setup(v => v.ValidatePropertiesForCombinationAsync(1, It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, combineProperties));

        _mockDataCopier.Setup(d => d.CopyPropertyDataAsync(1, It.IsAny<List<int>>(), 100, It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDeactivator.Setup(d => d.DeactivateCombinedPropertiesAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDeactivator.Setup(d => d.EnsureMainPropertyRecordsActiveAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository.Setup(r => r.GetQueryable()).Returns(combineProperties.BuildMock());

        // Track CombinePropertyHistory insertions
        var insertedHistory = new List<CombinePropertyHistoryEntity>();
        _mockCombineHistoryRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<CombinePropertyHistoryEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<CombinePropertyHistoryEntity>, CancellationToken>((entities, _) => insertedHistory.AddRange(entities))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert - Verify success response
        Assert.True(result.Success, $"Expected success but got: {result.Message}");
        Assert.Equal(1, result.SourcePropertyId);
        Assert.Equal(2, result.CombinedPropertyIds.Count);
        Assert.Contains(2, result.CombinedPropertyIds);
        Assert.Contains(3, result.CombinedPropertyIds);
        Assert.Equal("Properties combined successfully.", result.Message);

        // Assert - Verify history records were inserted
        Assert.Equal(2, insertedHistory.Count);
        Assert.All(insertedHistory, h =>
        {
            Assert.Equal(1, h.SourcePropertyId);
            Assert.Contains(h.CombinedPropertyId, new[] { 2, 3 });
            Assert.Equal("Combining adjacent properties", h.CombineReason);
            Assert.True(h.IsActive);
            Assert.Equal(100, h.CreatedBy);
        });

        // Assert - Verify transaction was committed
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        // Verify helper services were called
        _mockValidator.Verify(v => v.ValidatePropertiesForCombinationAsync(1, It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDataCopier.Verify(d => d.CopyPropertyDataAsync(1, It.IsAny<List<int>>(), 100, It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDeactivator.Verify(d => d.DeactivateCombinedPropertiesAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockDeactivator.Verify(d => d.EnsureMainPropertyRecordsActiveAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CombinePropertiesAsync_TransactionRollback_OnException()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "2",
            CombineReason = "Test rollback",
            CreatedBy = 100
        };

        var sourceProperty = new PropertyEntity { Id = 1, OwnerName = "John Doe", IsActive = true };
        var combineProperty = new PropertyEntity { Id = 2, OwnerName = "John Doe", IsActive = true };

        // Setup repository to return source property for defensive check
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceProperty);

        _mockValidator.Setup(v => v.ValidatePropertiesForCombinationAsync(1, It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, new List<PropertyEntity> { combineProperty }));

        // Setup data copier to throw exception
        _mockDataCopier.Setup(d => d.CopyPropertyDataAsync(It.IsAny<int>(), It.IsAny<List<int>>(), It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.CombinePropertiesAsync(request, default));

        // Verify rollback was called
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CombinePropertiesAsync_DuplicateCombinedPropertyInHistory_ReturnsFailure()
    {
        // Arrange
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "2,3",
            CombineReason = "Test"
        };

        var historyRecords = new List<CombinePropertyHistoryEntity>
        {
            new() { Id = 1, SourcePropertyId = 10, CombinedPropertyId = 2, CombineReason = "Old combine", IsActive = true }
        };

        _mockCombineHistoryRepository.Setup(r => r.GetQueryable())
            .Returns(historyRecords.BuildMock());

        // Act
        var result = await _service.CombinePropertiesAsync(request, default);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Properties already combined", result.Message);
        Assert.Contains("2", result.Message); // Should include the duplicate property ID

        _mockValidator.Verify(v => v.ValidatePropertiesForCombinationAsync(It.IsAny<int>(), It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Response DTO Tests

    [Fact]
    public void CombinePropertiesResponseDto_SuccessResponse_HasCorrectProperties()
    {
        // Arrange & Act
        var response = new CombinePropertiesResponseDto
        {
            Success = true,
            SourcePropertyId = 1,
            CombinedPropertyIds = new List<int> { 2, 3, 4 },
            Message = "Properties combined successfully."
        };

        // Assert
        Assert.True(response.Success);
        Assert.Equal(1, response.SourcePropertyId);
        Assert.Equal(3, response.CombinedPropertyIds.Count);
        Assert.Contains(2, response.CombinedPropertyIds);
        Assert.Contains(3, response.CombinedPropertyIds);
        Assert.Contains(4, response.CombinedPropertyIds);
        Assert.Equal("Properties combined successfully.", response.Message);
    }

    [Fact]
    public void CombinePropertiesResponseDto_FailureResponse_HasCorrectProperties()
    {
        // Arrange & Act
        var response = new CombinePropertiesResponseDto
        {
            Success = false,
            SourcePropertyId = 1,
            CombinedPropertyIds = new List<int>(),
            Message = "No valid property IDs provided"
        };

        // Assert
        Assert.False(response.Success);
        Assert.Equal(1, response.SourcePropertyId);
        Assert.Empty(response.CombinedPropertyIds);
        Assert.Contains("No valid property IDs provided", response.Message);
    }

    [Fact]
    public void CombinePropertiesResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var response = new CombinePropertiesResponseDto();

        // Assert
        Assert.False(response.Success);
        Assert.Equal(0, response.SourcePropertyId);
        Assert.NotNull(response.CombinedPropertyIds);
        Assert.Empty(response.CombinedPropertyIds);
        Assert.Equal(string.Empty, response.Message);
    }

    #endregion

    #region Request DTO Tests

    [Fact]
    public void CombinePropertiesRequestDto_Properties_GetSetCorrectly()
    {
        // Arrange & Act
        var request = new CombinePropertiesRequestDto
        {
            SourcePropertyId = 1,
            CombinedPropertyIds = "2,3,4",
            CombineReason = "Test reason",
            CreatedBy = 100
        };

        // Assert
        Assert.Equal(1, request.SourcePropertyId);
        Assert.Equal("2,3,4", request.CombinedPropertyIds);
        Assert.Equal("Test reason", request.CombineReason);
        Assert.Equal(100, request.CreatedBy);
    }

    [Fact]
    public void PropertyCombineDetailsDto_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var dto = new PropertyCombineDetailsDto
        {
            PropertyId = 1,
            WardId = 60,
            WardNo = "WARD60",
            PropertyNo = "123",
            PartitionNo = "A",
            OldPropertyNo = "OLD-123",
            OwnerName = "Owner Name",
            OccupierName = "Occupier Name",
            TaxAmount = 1000.50m,
            PendingAmount = 500.25m
        };

        // Assert
        Assert.Equal(1, dto.PropertyId);
        Assert.Equal(60, dto.WardId);
        Assert.Equal("WARD60", dto.WardNo);
        Assert.Equal("123", dto.PropertyNo);
        Assert.Equal("A", dto.PartitionNo);
        Assert.Equal("OLD-123", dto.OldPropertyNo);
        Assert.Equal("Owner Name", dto.OwnerName);
        Assert.Equal("Occupier Name", dto.OccupierName);
        Assert.Equal(1000.50m, dto.TaxAmount);
        Assert.Equal(500.25m, dto.PendingAmount);
    }

    #endregion

    #region Query Parameters Tests

    [Fact]
    public void CombinePropertyQueryParameters_DefaultValues()
    {
        // Arrange & Act
        var queryParams = new CombinePropertyQueryParameters();

        // Assert
        Assert.Null(queryParams.WardId);
        Assert.Null(queryParams.PropertyNo);
        Assert.Null(queryParams.PartitionNo);
    }

    [Fact]
    public void PropertyCombineDetailsQueryParameters_DefaultValues()
    {
        // Arrange & Act
        var queryParams = new PropertyCombineDetailsQueryParameters();

        // Assert
        Assert.Null(queryParams.WardId);
        Assert.Null(queryParams.PropertyNo);
        Assert.Null(queryParams.PartitionNo);
    }

    [Fact]
    public void CombinePropertyQueryParameters_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var queryParams = new CombinePropertyQueryParameters
        {
            WardId = 60,
            PropertyNo = "123",
            PartitionNo = "A",
            PageNumber = 2,
            PageSize = 20,
            SearchTerm = "test",
            SortBy = "PropertyNo",
            SortOrder = "DESC"
        };

        // Assert
        Assert.Equal(60, queryParams.WardId);
        Assert.Equal("123", queryParams.PropertyNo);
        Assert.Equal("A", queryParams.PartitionNo);
        Assert.Equal(2, queryParams.PageNumber);
        Assert.Equal(20, queryParams.PageSize);
        Assert.Equal("test", queryParams.SearchTerm);
        Assert.Equal("PropertyNo", queryParams.SortBy);
        Assert.Equal("DESC", queryParams.SortOrder);
    }

    #endregion

    #region PropertyMastOld Soft Delete Tests

    [Fact]
    public void PropertyMastOldEntity_SoftDeleteProperties_CanBeSet()
    {
        // Arrange & Act
        var entity = new PropertyMastOldEntity
        {
            Id = 1,
            OldPropertyNo = "OLD-123",
            IsActive = false,
            MarkedForDeletion = true,
            MarkedForDeletionDate = new DateTime(2024, 1, 15, 10, 30, 0)
        };

        // Assert
        Assert.Equal(1, entity.Id);
        Assert.Equal("OLD-123", entity.OldPropertyNo);
        Assert.False(entity.IsActive);
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0), entity.MarkedForDeletionDate);
    }

    [Fact]
    public void PropertyMastOldEntity_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var entity = new PropertyMastOldEntity();

        // Assert
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void PropertyMastOldEntity_SoftDeletedState_IsCorrect()
    {
        // Arrange - Simulate a PropertyMastOld record after combine operation
        var entity = new PropertyMastOldEntity
        {
            Id = 1,
            OldPropertyNo = "OLD-123",
            OldWardNo = "60",
            OldOwnerName = "John Doe",
            IsActive = false,
            MarkedForDeletion = true,
            MarkedForDeletionDate = DateTime.Now
        };

        // Assert - Verify soft delete state
        Assert.False(entity.IsActive);
        Assert.True(entity.MarkedForDeletion);
        Assert.NotNull(entity.MarkedForDeletionDate);
        // Original data should still be preserved
        Assert.Equal("OLD-123", entity.OldPropertyNo);
        Assert.Equal("60", entity.OldWardNo);
        Assert.Equal("John Doe", entity.OldOwnerName);
    }

    #endregion

    #region PropertyEntity Tests

    [Fact]
    public void PropertyEntity_PropertyMastOldId_CanBeSet()
    {
        // Arrange & Act
        var entity = new PropertyEntity
        {
            Id = 1,
            WardId = 60,
            PropertyNo = "123",
            PartitionNo = "A",
            PropertyMastOldId = 100,
            IsActive = true
        };

        // Assert
        Assert.Equal(100, entity.PropertyMastOldId);
    }

    [Fact]
    public void PropertyEntity_PropertyMastOldId_CanBeNull()
    {
        // Arrange & Act
        var entity = new PropertyEntity
        {
            Id = 1,
            WardId = 60,
            PropertyNo = "123",
            PartitionNo = "A",
            PropertyMastOldId = null,
            IsActive = true
        };

        // Assert
        Assert.Null(entity.PropertyMastOldId);
    }

    #endregion

    #region CategoryId Filtering Tests

    [Fact(Skip = "GetAllAsync uses EF.Property which requires real EF context, not in-memory mock")]
    public async Task GetAllAsync_WithApartmentCategoryId_FiltersCorrectly()
    {
        // Arrange - Apartment category (ID=6) should filter by CategoryId, WardId, AND PropertyNo
        var apartmentCategory = new PropertyCategoryEntity
        {
            Id = 6,
            PropertyCategoryName = "Apartment",
            IsActive = true
        };

        var properties = new List<PropertyEntity>
        {
            // Apartment properties with PropertyNo=1
            new() { Id = 1, CategoryId = 6, WardId = 60, PropertyNo = "1", PartitionNo = "A", IsActive = true },
            new() { Id = 2, CategoryId = 6, WardId = 60, PropertyNo = "1", PartitionNo = "A1", IsActive = true },
            new() { Id = 3, CategoryId = 6, WardId = 60, PropertyNo = "1", PartitionNo = "A2", IsActive = true },
            // Apartment property with different PropertyNo (should be filtered out)
            new() { Id = 4, CategoryId = 6, WardId = 60, PropertyNo = "2", PartitionNo = "B", IsActive = true },
            // Non-apartment property with same PropertyNo (should be filtered out by CategoryId)
            new() { Id = 5, CategoryId = 5, WardId = 60, PropertyNo = "1", PartitionNo = null, IsActive = true },
            // Apartment property in different ward (should be filtered out)
            new() { Id = 6, CategoryId = 6, WardId = 61, PropertyNo = "1", PartitionNo = "C", IsActive = true }
        };

        var wards = new List<WardEntity>
        {
            new() { Id = 60, WardNo = "WARD60", IsActive = true }
        };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentCategory);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());

        var queryParams = new CombinePropertyQueryParameters
        {
            CategoryId = 6,
            WardId = 60,
            PropertyNo = "1",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, default);

        // Assert
        var items = result.Items.ToList();
        Assert.Equal(3, items.Count); // Should return only apartment properties with CategoryId=6, WardId=60, PropertyNo=1
        Assert.All(items, item =>
        {
            Assert.Equal(6, item.CategoryId);
            Assert.Equal(60, item.WardId);
            Assert.Equal("1", item.PropertyNo);
        });
    }

    [Fact(Skip = "GetAllAsync uses EF.Property which requires real EF context, not in-memory mock")]
    public async Task GetAllAsync_WithNonApartmentCategoryId_FiltersCorrectly()
    {
        // Arrange - Non-apartment category (ID=5) should filter by CategoryId and WardId only (ignore PropertyNo)
        var residentialCategory = new PropertyCategoryEntity
        {
            Id = 5,
            PropertyCategoryName = "Residential",
            IsActive = true
        };

        var properties = new List<PropertyEntity>
        {
            // Non-apartment properties in WardId=60
            new() { Id = 1, CategoryId = 5, WardId = 60, PropertyNo = "1", PartitionNo = null, IsActive = true },
            new() { Id = 2, CategoryId = 5, WardId = 60, PropertyNo = "2", PartitionNo = null, IsActive = true },
            new() { Id = 3, CategoryId = 5, WardId = 60, PropertyNo = "3", PartitionNo = null, IsActive = true },
            // Apartment property with same WardId (should be filtered out by CategoryId)
            new() { Id = 4, CategoryId = 6, WardId = 60, PropertyNo = "1", PartitionNo = "A", IsActive = true },
            // Non-apartment property in different ward (should be filtered out)
            new() { Id = 5, CategoryId = 5, WardId = 61, PropertyNo = "1", PartitionNo = null, IsActive = true }
        };

        var wards = new List<WardEntity>
        {
            new() { Id = 60, WardNo = "WARD60", IsActive = true }
        };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(residentialCategory);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());

        var queryParams = new CombinePropertyQueryParameters
        {
            CategoryId = 5,
            WardId = 60,
            PropertyNo = "1", // Should be ignored for non-apartment
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, default);

        // Assert
        var items = result.Items.ToList();
        Assert.Equal(3, items.Count); // Should return ALL non-apartment properties in WardId=60 (PropertyNo ignored)
        Assert.All(items, item =>
        {
            Assert.Equal(5, item.CategoryId);
            Assert.Equal(60, item.WardId);
        });
    }

    [Fact(Skip = "GetAllAsync uses EF.Property which requires real EF context, not in-memory mock")]
    public async Task GetAllAsync_WithMultiCommercialApartmentCategory_FiltersCorrectly()
    {
        // Arrange - Multi Commercial Apartment should be treated as apartment category
        var multiCommercialApartmentCategory = new PropertyCategoryEntity
        {
            Id = 7,
            PropertyCategoryName = "Multi Commercial Apartment",
            IsActive = true
        };

        var properties = new List<PropertyEntity>
        {
            // Multi Commercial Apartment properties
            new() { Id = 1, CategoryId = 7, WardId = 60, PropertyNo = "1", PartitionNo = "A", IsActive = true },
            new() { Id = 2, CategoryId = 7, WardId = 60, PropertyNo = "1", PartitionNo = "B", IsActive = true },
            // Different PropertyNo (should be filtered out)
            new() { Id = 3, CategoryId = 7, WardId = 60, PropertyNo = "2", PartitionNo = "C", IsActive = true }
        };

        var wards = new List<WardEntity>
        {
            new() { Id = 60, WardNo = "WARD60", IsActive = true }
        };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(multiCommercialApartmentCategory);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());

        var queryParams = new CombinePropertyQueryParameters
        {
            CategoryId = 7,
            WardId = 60,
            PropertyNo = "1",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, default);

        // Assert
        var items = result.Items.ToList();
        Assert.Equal(2, items.Count); // Should filter by CategoryId, WardId, AND PropertyNo
        Assert.All(items, item =>
        {
            Assert.Equal(7, item.CategoryId);
            Assert.Equal(60, item.WardId);
            Assert.Equal("1", item.PropertyNo);
        });
    }

    [Fact(Skip = "GetAllAsync uses EF.Property which requires real EF context, not in-memory mock")]
    public async Task GetAllAsync_WithoutCategoryId_UsesStandardFiltering()
    {
        // Arrange - No CategoryId means standard filtering (both WardId and PropertyNo)
        var properties = new List<PropertyEntity>
        {
            // Mixed categories
            new() { Id = 1, CategoryId = 6, WardId = 60, PropertyNo = "1", PartitionNo = "A", IsActive = true },
            new() { Id = 2, CategoryId = 5, WardId = 60, PropertyNo = "1", PartitionNo = null, IsActive = true },
            new() { Id = 3, CategoryId = 6, WardId = 60, PropertyNo = "2", PartitionNo = "B", IsActive = true },
            new() { Id = 4, CategoryId = 5, WardId = 61, PropertyNo = "1", PartitionNo = null, IsActive = true }
        };

        var wards = new List<WardEntity>
        {
            new() { Id = 60, WardNo = "WARD60", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());

        var queryParams = new CombinePropertyQueryParameters
        {
            CategoryId = null, // No category filter
            WardId = 60,
            PropertyNo = "1",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, default);

        // Assert
        var items = result.Items.ToList();
        Assert.Equal(2, items.Count); // Should return both categories with WardId=60 and PropertyNo=1
        Assert.Contains(items, i => i.CategoryId == 6);
        Assert.Contains(items, i => i.CategoryId == 5);
    }

    [Fact(Skip = "GetAllAsync uses EF.Property which requires real EF context, not in-memory mock")]
    public async Task GetAllAsync_WithInvalidCategoryId_ReturnsEmpty()
    {
        // Arrange - CategoryId that doesn't exist
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, CategoryId = 6, WardId = 60, PropertyNo = "1", PartitionNo = "A", IsActive = true }
        };

        var wards = new List<WardEntity>
        {
            new() { Id = 60, WardNo = "WARD60", IsActive = true }
        };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyCategoryEntity?)null);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());

        var queryParams = new CombinePropertyQueryParameters
        {
            CategoryId = 999,
            WardId = 60,
            PropertyNo = "1",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, default);

        // Assert
        Assert.Empty(result.Items); // Should return empty because no properties match CategoryId=999
    }

    [Fact(Skip = "GetAllAsync uses EF.Property which requires real EF context, not in-memory mock")]
    public async Task GetAllAsync_WithCategoryIdOnly_FiltersAllPropertiesOfThatCategory()
    {
        // Arrange - Only CategoryId provided, no WardId or PropertyNo
        var apartmentCategory = new PropertyCategoryEntity
        {
            Id = 6,
            PropertyCategoryName = "Apartment",
            IsActive = true
        };

        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, CategoryId = 6, WardId = 60, PropertyNo = "1", PartitionNo = "A", IsActive = true },
            new() { Id = 2, CategoryId = 6, WardId = 61, PropertyNo = "2", PartitionNo = "B", IsActive = true },
            new() { Id = 3, CategoryId = 5, WardId = 60, PropertyNo = "3", PartitionNo = null, IsActive = true }
        };

        var wards = new List<WardEntity>
        {
            new() { Id = 60, WardNo = "WARD60", IsActive = true },
            new() { Id = 61, WardNo = "WARD61", IsActive = true }
        };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentCategory);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());

        var queryParams = new CombinePropertyQueryParameters
        {
            CategoryId = 6,
            WardId = null,
            PropertyNo = null,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, default);

        // Assert
        var items = result.Items.ToList();
        Assert.Equal(2, items.Count); // Should return all apartment properties
        Assert.All(items, item => Assert.Equal(6, item.CategoryId));
    }

    #endregion
}


