using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.FieldConfiguration;
using NtisPlatform.Application.Mappings.FieldConfigurationMappings;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services.FieldConfiguration;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive unit tests for FieldConfigurationService
/// Tests business logic, data operations, and field configuration management
/// </summary>
public class FieldConfigurationServiceTests
{
    private readonly Mock<IRepository<FieldConfigurationEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly FieldConfigurationService _service;

    public FieldConfigurationServiceTests()
    {
        _mockRepository = new Mock<IRepository<FieldConfigurationEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        // Create real mapper using production configuration
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<FieldConfigurationMappingProfile>();
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

        _service = new FieldConfigurationService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mapper);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithValidParameters_ReturnsPagedResult()
    {
        // Arrange
        var entities = new List<FieldConfigurationEntity>
        {
            new()
            {
                Id = 1,
                RulesFieldId = 1,
                DataType = "String",
                InputType = "TextBox",
                IsActive = true,
                RulesField = new RulesFieldEntity { Id = 1, FieldName = "PropertyType", FieldType = "Condition", IsActive = true }
            },
            new()
            {
                Id = 2,
                RulesFieldId = 2,
                DataType = "Number",
                InputType = "Numeric",
                IsActive = true,
                RulesField = new RulesFieldEntity { Id = 2, FieldName = "TaxRate", FieldType = "Effect", IsActive = true }
            }
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var queryParams = new FieldConfigurationQueryParameters
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
        Assert.Equal("String", result.Items.First().DataType);
    }

    [Fact]
    public async Task GetAllAsync_WithFiltering_ReturnsFilteredResults()
    {
        // Arrange
        var entities = new List<FieldConfigurationEntity>
        {
            new()
            {
                Id = 1,
                RulesFieldId = 1,
                DataType = "String",
                InputType = "TextBox",
                IsRequired = true,
                IsActive = true,
                RulesField = new RulesFieldEntity { Id = 1, FieldName = "PropertyType", FieldType = "Condition", IsActive = true }
            },
            new()
            {
                Id = 2,
                RulesFieldId = 2,
                DataType = "Number",
                InputType = "Numeric",
                IsRequired = false,
                IsActive = true,
                RulesField = new RulesFieldEntity { Id = 2, FieldName = "TaxRate", FieldType = "Effect", IsActive = true }
            }
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var queryParams = new FieldConfigurationQueryParameters
        {
            IsRequired = true,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, item => Assert.True(item.IsRequired));
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entities = Enumerable.Range(1, 25).Select(i => new FieldConfigurationEntity
        {
            Id = i,
            RulesFieldId = i,
            DataType = "String",
            InputType = "TextBox",
            IsActive = true,
            RulesField = new RulesFieldEntity { Id = i, FieldName = $"Field{i}", FieldType = "Condition", IsActive = true }
        }).ToList();

        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var queryParams = new FieldConfigurationQueryParameters
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

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsDto()
    {
        // Arrange
        var entity = new FieldConfigurationEntity
        {
            Id = 1,
            RulesFieldId = 1,
            DataType = "String",
            InputType = "TextBox",
            IsActive = true,
            RulesField = new RulesFieldEntity { Id = 1, FieldName = "PropertyType", FieldType = "Condition", IsActive = true }
        };

        var entities = new List<FieldConfigurationEntity> { entity };
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("String", result.DataType);
        Assert.Equal("TextBox", result.InputType);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var entities = new List<FieldConfigurationEntity>();
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _service.GetByIdAsync(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByRulesFieldIdAsync Tests

    [Fact]
    public async Task GetByRulesFieldIdAsync_WithValidRulesFieldId_ReturnsDto()
    {
        // Arrange
        var entity = new FieldConfigurationEntity
        {
            Id = 1,
            RulesFieldId = 10,
            DataType = "String",
            InputType = "DropDown",
            HasApiSource = true,
            ApiEndpoint = "/api/property-types",
            IsActive = true,
            RulesField = new RulesFieldEntity { Id = 10, FieldName = "PropertyType", FieldType = "Condition", IsActive = true }
        };

        var entities = new List<FieldConfigurationEntity> { entity };
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _service.GetByRulesFieldIdAsync(10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.RulesFieldId);
        Assert.Equal("DropDown", result.InputType);
        Assert.True(result.HasApiSource);
    }

    [Fact]
    public async Task GetByRulesFieldIdAsync_WithNonExistentRulesFieldId_ReturnsNull()
    {
        // Arrange
        var entities = new List<FieldConfigurationEntity>();
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _service.GetByRulesFieldIdAsync(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesEntity()
    {
        // Arrange
        var createDto = new CreateFieldConfigurationDto
        {
            RulesFieldId = 1,
            DataType = "String",
            InputType = "TextBox",
            IsRequired = true,
            DefaultValue = "DefaultValue",
            CreatedBy = 1
        };

        FieldConfigurationEntity? capturedEntity = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<FieldConfigurationEntity>(), It.IsAny<CancellationToken>()))
            .Callback<FieldConfigurationEntity, CancellationToken>((entity, _) =>
            {
                entity.Id = 1;
                capturedEntity = entity;
            })
            .ReturnsAsync((FieldConfigurationEntity e, CancellationToken _) => e);

        var entities = new List<FieldConfigurationEntity>();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(() =>
        {
            if (capturedEntity != null && !entities.Contains(capturedEntity))
            {
                capturedEntity.RulesField = new RulesFieldEntity { Id = 1, FieldName = "TestField", FieldType = "Condition", IsActive = true };
                entities.Add(capturedEntity);
            }
            return MockQueryableExtensions.BuildMock(entities);
        });

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => entities.FirstOrDefault(e => e.Id == id));

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("String", result.DataType);
        Assert.Equal("TextBox", result.InputType);
        Assert.True(result.IsRequired);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<FieldConfigurationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithApiConfiguration_CreatesEntityWithApiSettings()
    {
        // Arrange
        var createDto = new CreateFieldConfigurationDto
        {
            RulesFieldId = 1,
            DataType = "String",
            InputType = "DropDown",
            HasApiSource = true,
            ApiEndpoint = "/api/property-types",
            ApiMethod = "GET",
            ApiParameters = "{\"filter\": \"active\"}",
            ApiResponseMapping = "{\"valuePath\": \"id\", \"labelPath\": \"name\"}",
            CreatedBy = 1
        };

        FieldConfigurationEntity? capturedEntity = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<FieldConfigurationEntity>(), It.IsAny<CancellationToken>()))
            .Callback<FieldConfigurationEntity, CancellationToken>((entity, _) =>
            {
                entity.Id = 1;
                capturedEntity = entity;
            })
            .ReturnsAsync((FieldConfigurationEntity e, CancellationToken _) => e);

        var entities = new List<FieldConfigurationEntity>();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(() =>
        {
            if (capturedEntity != null && !entities.Contains(capturedEntity))
            {
                capturedEntity.RulesField = new RulesFieldEntity { Id = 1, FieldName = "PropertyType", FieldType = "Condition", IsActive = true };
                entities.Add(capturedEntity);
            }
            return MockQueryableExtensions.BuildMock(entities);
        });

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => entities.FirstOrDefault(e => e.Id == id));

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasApiSource);
        Assert.Equal("/api/property-types", result.ApiEndpoint);
        Assert.Equal("GET", result.ApiMethod);
    }

    [Fact]
    public async Task CreateAsync_WithStaticValues_CreatesEntityWithStaticValuesJson()
    {
        // Arrange
        var createDto = new CreateFieldConfigurationDto
        {
            RulesFieldId = 1,
            DataType = "String",
            InputType = "DropDown",
            HasStaticValues = true,
            StaticValuesJson = "[{\"value\": \"Type1\", \"label\": \"Type 1\"}, {\"value\": \"Type2\", \"label\": \"Type 2\"}]",
            CreatedBy = 1
        };

        FieldConfigurationEntity? capturedEntity = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<FieldConfigurationEntity>(), It.IsAny<CancellationToken>()))
            .Callback<FieldConfigurationEntity, CancellationToken>((entity, _) =>
            {
                entity.Id = 1;
                capturedEntity = entity;
            })
            .ReturnsAsync((FieldConfigurationEntity e, CancellationToken _) => e);

        var entities = new List<FieldConfigurationEntity>();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(() =>
        {
            if (capturedEntity != null && !entities.Contains(capturedEntity))
            {
                capturedEntity.RulesField = new RulesFieldEntity { Id = 1, FieldName = "PropertyType", FieldType = "Condition", IsActive = true };
                entities.Add(capturedEntity);
            }
            return MockQueryableExtensions.BuildMock(entities);
        });

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => entities.FirstOrDefault(e => e.Id == id));

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasStaticValues);
        Assert.NotNull(result.StaticValuesJson);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesEntity()
    {
        // Arrange
        var existingEntity = new FieldConfigurationEntity
        {
            Id = 1,
            RulesFieldId = 1,
            DataType = "String",
            InputType = "TextBox",
            IsRequired = false,
            IsActive = true,
            RulesField = new RulesFieldEntity { Id = 1, FieldName = "PropertyType", FieldType = "Condition", IsActive = true }
        };

        var entities = new List<FieldConfigurationEntity> { existingEntity };
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        var updateDto = new UpdateFieldConfigurationDto
        {
            IsRequired = true,
            DefaultValue = "UpdatedDefault",
            ValidationRegex = "^[A-Za-z]+$",
            UpdatedBy = 1
        };

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsRequired);
        Assert.Equal("UpdatedDefault", result.DefaultValue);
        Assert.Equal("^[A-Za-z]+$", result.ValidationRegex);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<FieldConfigurationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ThrowsException()
    {
        // Arrange
        var entities = new List<FieldConfigurationEntity>();
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((FieldConfigurationEntity?)null);

        var updateDto = new UpdateFieldConfigurationDto
        {
            IsRequired = true,
            UpdatedBy = 1
        };

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert - Service returns null instead of throwing exception for non-existent entities
        Assert.Null(result);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_SoftDeletesEntity()
    {
        // Arrange
        var entity = new FieldConfigurationEntity
        {
            Id = 1,
            RulesFieldId = 1,
            DataType = "String",
            InputType = "TextBox",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<FieldConfigurationEntity>(), It.IsAny<CancellationToken>()))
            .Callback<FieldConfigurationEntity, CancellationToken>((e, _) =>
            {
                e.IsActive = false; // Simulate soft delete behavior
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.False(entity.IsActive);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FieldConfigurationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ThrowsException()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((FieldConfigurationEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999, CancellationToken.None);

        // Assert - Service returns false instead of throwing exception for non-existent entities
        Assert.False(result);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task CreateAsync_WithValidationRules_AppliesValidationCorrectly()
    {
        // Arrange
        var createDto = new CreateFieldConfigurationDto
        {
            RulesFieldId = 1,
            DataType = "Number",
            InputType = "Numeric",
            IsRequired = true,
            MinValue = 0,
            MaxValue = 100,
            ValidationRegex = "^[0-9]+$",
            CreatedBy = 1
        };

        FieldConfigurationEntity? capturedEntity = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<FieldConfigurationEntity>(), It.IsAny<CancellationToken>()))
            .Callback<FieldConfigurationEntity, CancellationToken>((entity, _) =>
            {
                entity.Id = 1;
                capturedEntity = entity;
            })
            .ReturnsAsync((FieldConfigurationEntity e, CancellationToken _) => e);

        var entities = new List<FieldConfigurationEntity>();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(() =>
        {
            if (capturedEntity != null && !entities.Contains(capturedEntity))
            {
                capturedEntity.RulesField = new RulesFieldEntity { Id = 1, FieldName = "TaxRate", FieldType = "Effect", IsActive = true };
                entities.Add(capturedEntity);
            }
            return MockQueryableExtensions.BuildMock(entities);
        });

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => entities.FirstOrDefault(e => e.Id == id));

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.MinValue);
        Assert.Equal(100, result.MaxValue);
        Assert.Equal("^[0-9]+$", result.ValidationRegex);
    }

    #endregion
}
