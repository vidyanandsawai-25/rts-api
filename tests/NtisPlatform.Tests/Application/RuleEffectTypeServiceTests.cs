using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Master.RuleEffectTypeMaster;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive unit tests for RuleEffectTypeService
/// Tests business logic, effect type configuration, and data operations
/// </summary>
public class RuleEffectTypeServiceTests
{
    private readonly Mock<IRepository<RuleEffectTypeEntity, int>> _mockRepository;
    private readonly Mock<IRepository<EffectTypeConfigurationEntity, int>> _mockConfigRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly RuleEffectTypeService _service;

    public RuleEffectTypeServiceTests()
    {
        _mockRepository = new Mock<IRepository<RuleEffectTypeEntity, int>>();
        _mockConfigRepository = new Mock<IRepository<EffectTypeConfigurationEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        // Create real mapper using production configuration
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<RuleEffectTypeMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = mapperConfig.CreateMapper();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new RuleEffectTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mapper,
            _mockConfigRepository.Object);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithValidParameters_ReturnsPagedResult()
    {
        // Arrange
        var entities = new List<RuleEffectTypeEntity>
        {
            new()
            {
                Id = 1,
                EffectType = "Add",
                IsActive = true,
                EffectTypeConfiguration = new()
                {
                    Id = 1,
                    EffectTypeId = 1,
                    DataType = "Number",
                    InputType = "Numeric",
                    IsActive = true
                }
            },
            new()
            {
                Id = 2,
                EffectType = "Multiply",
                IsActive = true,
                EffectTypeConfiguration = new()
                {
                    Id = 2,
                    EffectTypeId = 2,
                    DataType = "Number",
                    InputType = "Numeric",
                    IsActive = true
                }
            }
        };

        var dtos = new List<RuleEffectTypeDto>
        {
            new RuleEffectTypeDto { Id = 1, EffectType = "Add", DataType = "Number", InputType = "Numeric", IsActive = true },
            new RuleEffectTypeDto { Id = 2, EffectType = "Multiply", DataType = "Number", InputType = "Numeric", IsActive = true }
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var queryParams = new RuleEffectTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal("Add", result.Items.First().EffectType);
    }

    [Fact]
    public async Task GetAllAsync_WithFiltering_ReturnsFilteredResults()
    {
        // Arrange
        var entities = new List<RuleEffectTypeEntity>
        {
            new() { Id = 1, EffectType = "Add", IsActive = true },
            new() { Id = 2, EffectType = "SetValue", IsActive = true }
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var queryParams = new RuleEffectTypeQueryParameters
        {
            EffectType = "Add",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, item => Assert.Contains("Add", item.EffectType));
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entities = Enumerable.Range(1, 25).Select(i => new RuleEffectTypeEntity()
        {
            Id = i,
            EffectType = $"Effect{i}",
            IsActive = true
        }).ToList();

        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var queryParams = new RuleEffectTypeQueryParameters
        {
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(11, result.Items.First().Id);
    }

    [Fact]
    public async Task GetAllAsync_WithNoPagination_ReturnsAllResults()
    {
        // Arrange
        var entities = Enumerable.Range(1, 50).Select(i => new RuleEffectTypeEntity()
        {
            Id = i,
            EffectType = $"Effect{i}",
            IsActive = true
        }).ToList();

        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var queryParams = new RuleEffectTypeQueryParameters
        {
            PageSize = -1
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50, result.TotalCount);
        Assert.Equal(50, result.Items.Count());
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsDto()
    {
        // Arrange
        var entity = new RuleEffectTypeEntity()
        {
            Id = 1,
            EffectType = "Add",
            IsActive = true,
            EffectTypeConfiguration = new()
            {
                Id = 1,
                EffectTypeId = 1,
                DataType = "Number",
                InputType = "Numeric",
                IsActive = true
            }
        };

        var dto = new RuleEffectTypeDto
        {
            Id = 1,
            EffectType = "Add",
            DataType = "Number",
            InputType = "Numeric",
            IsActive = true
        };

        var entities = new List<RuleEffectTypeEntity> { entity };
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Add", result.EffectType);
        Assert.Equal("Number", result.DataType);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var entities = new List<RuleEffectTypeEntity>();
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _service.GetByIdAsync(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithoutConfiguration_ReturnsEntityWithoutConfig()
    {
        // Arrange
        var entity = new RuleEffectTypeEntity()
        {
            Id = 1,
            EffectType = "Custom",
            IsActive = true,
            EffectTypeConfiguration = null
        };

        var dto = new RuleEffectTypeDto
        {
            Id = 1,
            EffectType = "Custom",
            IsActive = true
        };

        var entities = new List<RuleEffectTypeEntity> { entity };
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Custom", result.EffectType);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesEntity()
    {
        // Arrange
        var createDto = new CreateRuleEffectTypeDto
        {
            EffectType = "Subtract",
            DataType = "Number",
            InputType = "Numeric",
            CreatedBy = 1
        };

        var entity = new RuleEffectTypeEntity()
        {
            EffectType = "Subtract",
            IsActive = true,
            CreatedBy = 1
        };

        var resultDto = new RuleEffectTypeDto
        {
            Id = 1,
            EffectType = "Subtract",
            DataType = "Number",
            InputType = "Numeric",
            IsActive = true
        };

        RuleEffectTypeEntity? capturedEntity = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RuleEffectTypeEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RuleEffectTypeEntity, CancellationToken>((e, _) =>
            {
                e.Id = 1;
                capturedEntity = e;
            })
            .ReturnsAsync((RuleEffectTypeEntity e, CancellationToken _) => e);

        var entities = new List<RuleEffectTypeEntity>();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(() =>
        {
            if (capturedEntity != null && !entities.Contains(capturedEntity))
            {
                entities.Add(capturedEntity);
            }
            return MockQueryableExtensions.BuildMock(entities.ToList());
        });

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => entities.FirstOrDefault(e => e.Id == id));

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Subtract", result.EffectType);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RuleEffectTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithConfiguration_CreatesEntityAndConfiguration()
    {
        // Arrange
        var createDto = new CreateRuleEffectTypeDto
        {
            EffectType = "SetValue",
            DataType = "Number",
            InputType = "Numeric",
            IsRequired = true,
            MinValue = 0,
            MaxValue = 100,
            CreatedBy = 1
        };

        var entity = new RuleEffectTypeEntity()
        {
            EffectType = "SetValue",
            IsActive = true,
            CreatedBy = 1
        };

        var resultDto = new RuleEffectTypeDto
        {
            Id = 1,
            EffectType = "SetValue",
            DataType = "Number",
            InputType = "Numeric",
            IsRequired = true,
            MinValue = 0,
            MaxValue = 100,
            IsActive = true
        };

        RuleEffectTypeEntity? capturedEntity = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RuleEffectTypeEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RuleEffectTypeEntity, CancellationToken>((e, _) =>
            {
                e.Id = 1;
                capturedEntity = e;
            })
            .ReturnsAsync((RuleEffectTypeEntity e, CancellationToken _) => e);

        var entities = new List<RuleEffectTypeEntity>();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(() =>
        {
            if (capturedEntity != null && !entities.Contains(capturedEntity))
            {
                entities.Add(capturedEntity);
            }
            return MockQueryableExtensions.BuildMock(entities.ToList());
        });

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => entities.FirstOrDefault(e => e.Id == id));

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SetValue", result.EffectType);
        _mockConfigRepository.Verify(r => r.AddAsync(It.IsAny<EffectTypeConfigurationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesEntity()
    {
        // Arrange
        var existingEntity = new RuleEffectTypeEntity()
        {
            Id = 1,
            EffectType = "Add",
            IsActive = true
        };

        var resultDto = new RuleEffectTypeDto
        {
            Id = 1,
            EffectType = "Add",
            DataType = "Number",
            IsActive = true
        };

        var entities = new List<RuleEffectTypeEntity> { existingEntity };
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        var updateDto = new UpdateRuleEffectTypeDto
        {
            EffectType = "Add",
            DataType = "Number",
            UpdatedBy = 1
        };

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Add", result.EffectType);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RuleEffectTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithConfiguration_UpdatesEntityAndConfiguration()
    {
        // Arrange
        var existingConfig = new EffectTypeConfigurationEntity()
        {
            Id = 1,
            EffectTypeId = 1,
            DataType = "Number",
            InputType = "Numeric",
            IsRequired = false,
            IsActive = true
        };

        var existingEntity = new RuleEffectTypeEntity()
        {
            Id = 1,
            EffectType = "Add",
            IsActive = true,
            EffectTypeConfiguration = existingConfig
        };

        var resultDto = new RuleEffectTypeDto
        {
            Id = 1,
            EffectType = "Add",
            DataType = "Number",
            InputType = "Numeric",
            IsRequired = true,
            MinValue = 1,
            MaxValue = 1000,
            IsActive = true
        };

        var entities = new List<RuleEffectTypeEntity> { existingEntity };
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        var updateDto = new UpdateRuleEffectTypeDto
        {
            EffectType = "Add",
            DataType = "Number",
            InputType = "Numeric",
            IsRequired = true,
            MinValue = 1,
            MaxValue = 1000,
            UpdatedBy = 1
        };

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Number", result.DataType);
        _mockConfigRepository.Verify(r => r.UpdateAsync(It.IsAny<EffectTypeConfigurationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var entities = new List<RuleEffectTypeEntity>();
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var updateDto = new UpdateRuleEffectTypeDto
        {
            EffectType = "Updated",
            UpdatedBy = 1
        };

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesEntity()
    {
        // Arrange
        var entity = new RuleEffectTypeEntity()
        {
            Id = 1,
            EffectType = "Add",
            IsActive = true
        };

        var entities = new List<RuleEffectTypeEntity> { entity };
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RuleEffectTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ReturnsFalse()
    {
        // Arrange
        var entities = new List<RuleEffectTypeEntity>();
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _service.DeleteAsync(999, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task GetAllAsync_WithSearchTerm_ReturnsMatchingResults()
    {
        // Arrange
        var entities = new List<RuleEffectTypeEntity>
        {
            new() { Id = 1, EffectType = "Add", IsActive = true },
            new() { Id = 2, EffectType = "Multiply", IsActive = true },
            new() { Id = 3, EffectType = "SetValue", IsActive = true }
        };

        var dtos = new List<RuleEffectTypeDto>
        {
            new RuleEffectTypeDto { Id = 1, EffectType = "Add", IsActive = true }
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var queryParams = new RuleEffectTypeQueryParameters
        {
            EffectType = "Add",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0);
        Assert.Contains(result.Items, item => item.EffectType!.Contains("Add"));
    }

    #endregion
}





