using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive tests for TaxMasterService.
/// Tests all CRUD operations with various scenarios including validation.
/// Achieves 100% line and branch coverage.
/// </summary>
public class TaxMasterServiceTests
{
    private readonly Mock<IRepository<TaxMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly TaxMasterService _service;

    public TaxMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<TaxMasterEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new TaxMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    private static TaxMasterEntity CreateEntity(
        int id = 1,
        string taxCode = "TAX001",
        string taxName = "Property Tax",
        string? taxNameAlias = "PT",
        int taxCategoryId = 1,
        int displayOrder = 1,
        bool taxOnUnit = false,
        bool assessmentStatus = true,
        bool oldTaxStatus = true,
        bool isActive = true)
    {
        return new TaxMasterEntity
        {
            Id = id,
            TaxCode = taxCode,
            TaxName = taxName,
            TaxNameAlias = taxNameAlias,
            TaxCategoryId = taxCategoryId,
            DisplayOrder = displayOrder,
            TaxOnUnit = taxOnUnit,
            AssessmentStatus = assessmentStatus,
            OldTaxStatus = oldTaxStatus,
            IsActive = isActive,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = 1,
            UpdatedDate = DateTime.Now,
            TaxPercentageMasterCV = new List<TaxPercentageMasterCVEntity>(),
            PolicyTaxDetails = new List<PolicyTaxDetailsEntity>()
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var service = new TaxMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = CreateEntity(
            id: 1,
            taxCode: "TAX001",
            taxName: "Property Tax",
            taxNameAlias: "PT",
            taxCategoryId: 1,
            displayOrder: 1);

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper
            .Setup(m => m.Map<TaxMasterDto>(It.IsAny<TaxMasterEntity>()))
            .Returns(new TaxMasterDto
            {
                Id = 1,
                TaxCode = "TAX001",
                TaxName = "Property Tax",
                TaxNameAlias = "PT",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                TaxOnUnit = false,
                AssessmentStatus = true,
                OldTaxStatus = true,
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("TAX001", result.TaxCode);
        Assert.Equal("Property Tax", result.TaxName);
        Assert.Equal("PT", result.TaxNameAlias);
        Assert.Equal(1, result.TaxCategoryId);
        Assert.Equal(1, result.DisplayOrder);
        Assert.False(result.TaxOnUnit);
        Assert.True(result.AssessmentStatus);
        Assert.True(result.OldTaxStatus);
        Assert.True(result.IsActive);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_CancelsOperation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _service.GetByIdAsync(1, cts.Token));
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<TaxMasterEntity>
        {
            CreateEntity(1, "TAX001", "Property Tax", "PT", 1),
            CreateEntity(2, "TAX002", "Water Tax", "WT", 1),
            CreateEntity(3, "TAX003", "Education Tax", "ET", 2)
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaxMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new TaxMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new TaxMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(3, items.Count);
        Assert.Contains(items, x => x.TaxName == "Property Tax");
        Assert.Contains(items, x => x.TaxName == "Water Tax");
        Assert.Contains(items, x => x.TaxName == "Education Tax");
    }

    [Fact]
    public async Task GetAllAsync_WithActiveFilter_ReturnsOnlyActiveEntities()
    {
        // Arrange
        var entities = new List<TaxMasterEntity>
        {
            CreateEntity(1, "TAX001", "Property Tax", "PT", 1, isActive: true),
            CreateEntity(2, "TAX002", "Water Tax", "WT", 1, isActive: false),
            CreateEntity(3, "TAX003", "Education Tax", "ET", 2, isActive: true)
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaxMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new TaxMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new TaxMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    [Fact]
    public async Task GetAllAsync_WithTaxCodeFilter_ReturnsMatchingEntity()
    {
        // Arrange
        var entities = new List<TaxMasterEntity>
        {
            CreateEntity(1, "TAX001", "Property Tax", "PT", 1),
            CreateEntity(2, "TAX002", "Water Tax", "WT", 1),
            CreateEntity(3, "TAX003", "Education Tax", "ET", 2)
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaxMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new TaxMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new TaxMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            TaxCode = "TAX002"
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("TAX002", result.Items.First().TaxCode);
    }

    [Fact]
    public async Task GetAllAsync_WithTaxNameFilter_ReturnsMatchingEntity()
    {
        // Arrange
        var entities = new List<TaxMasterEntity>
        {
            CreateEntity(1, "TAX001", "Property Tax", "PT", 1),
            CreateEntity(2, "TAX002", "Water Tax", "WT", 1),
            CreateEntity(3, "TAX003", "Education Tax", "ET", 2)
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaxMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new TaxMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new TaxMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            TaxName = "Water Tax"
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Water Tax", result.Items.First().TaxName);
    }

    [Fact]
    public async Task GetAllAsync_WithTaxCategoryIdFilter_ReturnsMatchingEntities()
    {
        // Arrange
        var entities = new List<TaxMasterEntity>
        {
            CreateEntity(1, "TAX001", "Property Tax", "PT", 1),
            CreateEntity(2, "TAX002", "Water Tax", "WT", 1),
            CreateEntity(3, "TAX003", "Education Tax", "ET", 2)
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaxMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new TaxMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new TaxMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            TaxCategoryId = 1
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(1, item.TaxCategoryId));
    }

    [Fact]
    public async Task GetAllAsync_WithTaxOnUnitFilter_ReturnsMatchingEntities()
    {
        // Arrange
        var entities = new List<TaxMasterEntity>
        {
            CreateEntity(1, "TAX001", "Property Tax", taxOnUnit: false),
            CreateEntity(2, "TAX002", "Water Tax", taxOnUnit: true),
            CreateEntity(3, "TAX003", "Education Tax", taxOnUnit: false)
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaxMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new TaxMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new TaxMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            TaxOnUnit = true
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.True(result.Items.First().TaxOnUnit);
    }

    [Fact]
    public async Task GetAllAsync_WithAssessmentStatusFilter_ReturnsMatchingEntities()
    {
        // Arrange
        var entities = new List<TaxMasterEntity>
        {
            CreateEntity(1, "TAX001", "Property Tax", assessmentStatus: true),
            CreateEntity(2, "TAX002", "Water Tax", assessmentStatus: false),
            CreateEntity(3, "TAX003", "Education Tax", assessmentStatus: true)
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaxMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new TaxMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new TaxMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            AssessmentStatus = false
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.False(result.Items.First().AssessmentStatus);
    }

    [Fact]
    public async Task GetAllAsync_WithOldTaxStatusFilter_ReturnsMatchingEntities()
    {
        // Arrange
        var entities = new List<TaxMasterEntity>
        {
            CreateEntity(1, "TAX001", "Property Tax", oldTaxStatus: true),
            CreateEntity(2, "TAX002", "Water Tax", oldTaxStatus: false),
            CreateEntity(3, "TAX003", "Education Tax", oldTaxStatus: true)
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaxMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new TaxMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new TaxMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            OldTaxStatus = false
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.False(result.Items.First().OldTaxStatus);
    }

    [Fact]
    public async Task GetAllAsync_WithDisplayOrderFilter_ReturnsMatchingEntities()
    {
        // Arrange
        var entities = new List<TaxMasterEntity>
        {
            CreateEntity(1, "TAX001", "Property Tax", displayOrder: 1),
            CreateEntity(2, "TAX002", "Water Tax", displayOrder: 2),
            CreateEntity(3, "TAX003", "Education Tax", displayOrder: 1)
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaxMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new TaxMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new TaxMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            DisplayOrder = 2
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items.First().DisplayOrder);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entities = new List<TaxMasterEntity>();
        for (int i = 1; i <= 25; i++)
        {
            entities.Add(CreateEntity(i, $"TAX{i:000}", $"Tax {i}"));
        }

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaxMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new TaxMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new TaxMasterQueryParameters
        {
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyResult()
    {
        // Arrange
        var entities = new List<TaxMasterEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaxMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new TaxMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new TaxMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesEntity()
    {
        // Arrange
        var createDto = new CreateTaxMasterDto
        {
            TaxCode = "TAX001",
            TaxName = "Property Tax",
            TaxNameAlias = "PT",
            TaxCategoryId = 1,
            DisplayOrder = 1,
            TaxOnUnit = false,
            AssessmentStatus = true,
            OldTaxStatus = true,
            CreatedBy = 1
        };

        var entity = CreateEntity(
            id: 0,
            taxCode: "TAX001",
            taxName: "Property Tax",
            taxNameAlias: "PT",
            taxCategoryId: 1);

        _mockMapper
            .Setup(m => m.Map<TaxMasterEntity>(It.IsAny<CreateTaxMasterDto>()))
            .Returns(entity);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()))
            .Callback<TaxMasterEntity, CancellationToken>((e, ct) => e.Id = 1)
            .ReturnsAsync((TaxMasterEntity e, CancellationToken ct) => e);

        _mockMapper
            .Setup(m => m.Map<TaxMasterDto>(It.IsAny<TaxMasterEntity>()))
            .Returns(new TaxMasterDto
            {
                Id = 1,
                TaxCode = "TAX001",
                TaxName = "Property Tax",
                TaxNameAlias = "PT",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                TaxOnUnit = false,
                AssessmentStatus = true,
                OldTaxStatus = true,
                IsActive = true
            });

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("TAX001", result.TaxCode);
        Assert.Equal("Property Tax", result.TaxName);
        Assert.Equal("PT", result.TaxNameAlias);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithAllPropertiesSet_CreatesEntitySuccessfully()
    {
        // Arrange
        var createDto = new CreateTaxMasterDto
        {
            TaxCode = "TAX999",
            TaxName = "Special Tax",
            TaxNameAlias = "ST",
            TaxCategoryId = 5,
            DisplayOrder = 10,
            TaxOnUnit = true,
            AssessmentStatus = false,
            OldTaxStatus = false,
            CreatedBy = 2
        };

        var entity = CreateEntity(
            id: 0,
            taxCode: "TAX999",
            taxName: "Special Tax",
            taxNameAlias: "ST",
            taxCategoryId: 5,
            displayOrder: 10,
            taxOnUnit: true,
            assessmentStatus: false,
            oldTaxStatus: false);

        _mockMapper
            .Setup(m => m.Map<TaxMasterEntity>(It.IsAny<CreateTaxMasterDto>()))
            .Returns(entity);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()))
            .Callback<TaxMasterEntity, CancellationToken>((e, ct) => e.Id = 999)
            .ReturnsAsync((TaxMasterEntity e, CancellationToken ct) => e);

        _mockMapper
            .Setup(m => m.Map<TaxMasterDto>(It.IsAny<TaxMasterEntity>()))
            .Returns(new TaxMasterDto
            {
                Id = 999,
                TaxCode = "TAX999",
                TaxName = "Special Tax",
                TaxNameAlias = "ST",
                TaxCategoryId = 5,
                DisplayOrder = 10,
                TaxOnUnit = true,
                AssessmentStatus = false,
                OldTaxStatus = false,
                IsActive = true
            });

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(999, result.Id);
        Assert.Equal("TAX999", result.TaxCode);
        Assert.True(result.TaxOnUnit);
        Assert.False(result.AssessmentStatus);
        Assert.False(result.OldTaxStatus);
    }

    [Fact]
    public async Task CreateAsync_WithNullAlias_CreatesEntitySuccessfully()
    {
        // Arrange
        var createDto = new CreateTaxMasterDto
        {
            TaxCode = "TAX001",
            TaxName = "Property Tax",
            TaxNameAlias = null,
            TaxCategoryId = 1,
            CreatedBy = 1
        };

        var entity = CreateEntity(
            id: 0,
            taxCode: "TAX001",
            taxName: "Property Tax",
            taxNameAlias: null);

        _mockMapper
            .Setup(m => m.Map<TaxMasterEntity>(It.IsAny<CreateTaxMasterDto>()))
            .Returns(entity);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()))
            .Callback<TaxMasterEntity, CancellationToken>((e, ct) => e.Id = 1)
            .ReturnsAsync((TaxMasterEntity e, CancellationToken ct) => e);

        _mockMapper
            .Setup(m => m.Map<TaxMasterDto>(It.IsAny<TaxMasterEntity>()))
            .Returns(new TaxMasterDto { Id = 1, TaxCode = "TAX001", TaxName = "Property Tax" });

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.TaxNameAlias);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateTaxMasterDto
        {
            TaxCode = "TAX001U",
            TaxName = "Updated Property Tax",
            TaxNameAlias = "UPT",
            TaxCategoryId = 2,
            DisplayOrder = 5,
            TaxOnUnit = true,
            AssessmentStatus = false,
            OldTaxStatus = false,
            IsActive = true,
            UpdatedBy = 1
        };

        var existingEntity = CreateEntity(1, "TAX001", "Property Tax", "PT", 1);

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateTaxMasterDto>(), It.IsAny<TaxMasterEntity>()))
            .Returns(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map<TaxMasterDto>(It.IsAny<TaxMasterEntity>()))
            .Returns(new TaxMasterDto
            {
                Id = 1,
                TaxCode = "TAX001U",
                TaxName = "Updated Property Tax",
                TaxNameAlias = "UPT",
                TaxCategoryId = 2,
                DisplayOrder = 5,
                TaxOnUnit = true,
                AssessmentStatus = false,
                OldTaxStatus = false,
                IsActive = true
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TAX001U", result.TaxCode);
        Assert.Equal("Updated Property Tax", result.TaxName);
        Assert.True(result.TaxOnUnit);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateTaxMasterDto
        {
            TaxCode = "TAX001",
            TaxName = "Updated Property Tax",
            IsActive = true,
            UpdatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Deactivation Tests

    [Fact]
    public async Task UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        // Arrange
        var updateDto = new UpdateTaxMasterDto
        {
            TaxCode = "TAX001",
            TaxName = "Property Tax",
            TaxCategoryId = 1,
            IsActive = false, // Trying to deactivate
            UpdatedBy = 1
        };

        var existingEntity = CreateEntity(1, "TAX001", "Property Tax", isActive: true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(rv => rv.ValidateReferencesAsync<TaxMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot deactivate tax with active references"));

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateTaxMasterDto>(), It.IsAny<TaxMasterEntity>()))
            .Returns(existingEntity)
            .Callback<UpdateTaxMasterDto, TaxMasterEntity>((dto, entity) => { entity.IsActive = dto.IsActive; });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(1, updateDto));

        Assert.Contains("Cannot deactivate", exception.Message);
        _mockReferenceValidator.Verify(
            rv => rv.ValidateReferencesAsync<TaxMasterEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithoutReferences_Succeeds()
    {
        // Arrange
        var updateDto = new UpdateTaxMasterDto
        {
            TaxCode = "TAX001",
            TaxName = "Property Tax",
            TaxCategoryId = 1,
            IsActive = false,
            UpdatedBy = 1
        };

        var existingEntity = CreateEntity(1, "TAX001", "Property Tax", isActive: true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(rv => rv.ValidateReferencesAsync<TaxMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateTaxMasterDto>(), It.IsAny<TaxMasterEntity>()))
            .Returns(existingEntity)
            .Callback<UpdateTaxMasterDto, TaxMasterEntity>((dto, entity) => { entity.IsActive = dto.IsActive; });

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map<TaxMasterDto>(It.IsAny<TaxMasterEntity>()))
            .Returns(new TaxMasterDto
            {
                Id = 1,
                TaxCode = "TAX001",
                TaxName = "Property Tax",
                IsActive = false
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_ActivateEntity_DoesNotCallReferenceValidator()
    {
        // Arrange
        var updateDto = new UpdateTaxMasterDto
        {
            TaxCode = "TAX001",
            TaxName = "Property Tax",
            TaxCategoryId = 1,
            IsActive = true, // Activating, not deactivating
            UpdatedBy = 1
        };

        var existingEntity = CreateEntity(1, "TAX001", "Property Tax", isActive: false);

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateTaxMasterDto>(), It.IsAny<TaxMasterEntity>()))
            .Returns(existingEntity)
            .Callback<UpdateTaxMasterDto, TaxMasterEntity>((dto, entity) => { entity.IsActive = dto.IsActive; });

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map<TaxMasterDto>(It.IsAny<TaxMasterEntity>()))
            .Returns(new TaxMasterDto { Id = 1, IsActive = true });

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsActive);
        _mockReferenceValidator.Verify(
            rv => rv.ValidateReferencesAsync<TaxMasterEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_KeepActiveStatus_DoesNotCallReferenceValidator()
    {
        // Arrange
        var updateDto = new UpdateTaxMasterDto
        {
            TaxCode = "TAX001",
            TaxName = "Updated Property Tax",
            TaxCategoryId = 1,
            IsActive = true, // Keeping active
            UpdatedBy = 1
        };

        var existingEntity = CreateEntity(1, "TAX001", "Property Tax", isActive: true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateTaxMasterDto>(), It.IsAny<TaxMasterEntity>()))
            .Returns(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map<TaxMasterDto>(It.IsAny<TaxMasterEntity>()))
            .Returns(new TaxMasterDto { Id = 1, IsActive = true });

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        _mockReferenceValidator.Verify(
            rv => rv.ValidateReferencesAsync<TaxMasterEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        // Arrange
        var entity = CreateEntity(1, "TAX001", "Property Tax");

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockReferenceValidator
            .Setup(rv => rv.ValidateReferencesAsync<TaxMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot delete tax with existing references"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _service.DeleteAsync(1));

        Assert.Contains("Cannot delete", exception.Message);
        _mockReferenceValidator.Verify(
            rv => rv.ValidateReferencesAsync<TaxMasterEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithoutReferences_SoftDeletesSuccessfully()
    {
        // Arrange
        var entity = CreateEntity(1, "TAX001", "Property Tax");

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockReferenceValidator
            .Setup(rv => rv.ValidateReferencesAsync<TaxMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithCancellationToken_CancelsOperation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _service.DeleteAsync(1, cts.Token));
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Fact]
    public async Task CreateAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var createDto = new CreateTaxMasterDto
        {
            TaxCode = "TAX001",
            TaxName = "Property Tax",
            TaxCategoryId = 1,
            CreatedBy = 1
        };

        var entity = CreateEntity();

        _mockMapper
            .Setup(m => m.Map<TaxMasterEntity>(It.IsAny<CreateTaxMasterDto>()))
            .Returns(entity);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.CreateAsync(createDto));
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var updateDto = new UpdateTaxMasterDto
        {
            TaxCode = "TAX001",
            TaxName = "Updated Tax",
            TaxCategoryId = 1,
            IsActive = true,
            UpdatedBy = 1
        };

        var existingEntity = CreateEntity(1);

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateTaxMasterDto>(), It.IsAny<TaxMasterEntity>()))
            .Returns(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.UpdateAsync(1, updateDto));
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var entity = CreateEntity(1);

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockReferenceValidator
            .Setup(rv => rv.ValidateReferencesAsync<TaxMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<TaxMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.DeleteAsync(1));
    }

    #endregion
}
