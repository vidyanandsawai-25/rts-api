using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs.Master.ConfigKeyMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class ConfigKeyMasterServiceTests
{
    private readonly Mock<IRepository<ConfigKeyMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly ConfigKeyMasterService _service;

    public ConfigKeyMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<ConfigKeyMasterEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        // Setup SaveChangesAsync
        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Optional transaction setups
        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new ConfigKeyMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new ConfigKeyMasterEntity
        {
            Id = 1,
            CategoryId = 1,
            ConfigCode = "APP_TIMEOUT",
            ConfigName = "Application Timeout",
            Description = "Timeout duration for application session",
            DataType = "Integer",
            ControlType = "TextBox",
            DefaultValue = "30",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<ConfigKeyMasterDto>(It.IsAny<ConfigKeyMasterEntity>()))
            .Returns(new ConfigKeyMasterDto
            {
                Id = 1,
                CategoryId = 1,
                ConfigCode = "APP_TIMEOUT",
                ConfigName = "Application Timeout",
                Description = "Timeout duration for application session",
                DataType = "Integer",
                ControlType = "TextBox",
                DefaultValue = "30",
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("APP_TIMEOUT", result.ConfigCode);
        Assert.Equal("Application Timeout", result.ConfigName);
        Assert.Equal("Integer", result.DataType);
        Assert.Equal("TextBox", result.ControlType);
        Assert.Equal("30", result.DefaultValue);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<ConfigKeyMasterDto>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigKeyMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<ConfigKeyMasterDto>(It.IsAny<ConfigKeyMasterEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigKeyMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<ConfigKeyMasterEntity>
        {
            new()
            {
                Id = 1,
                CategoryId = 1,
                ConfigCode = "APP_TIMEOUT",
                ConfigName = "Application Timeout",
                Description = "Timeout duration",
                DataType = "Integer",
                ControlType = "TextBox",
                DefaultValue = "30",
                IsActive = true
            },
            new()
            {
                Id = 2,
                CategoryId = 1,
                ConfigCode = "MAX_LOGIN_ATTEMPTS",
                ConfigName = "Maximum Login Attempts",
                Description = "Maximum allowed login attempts",
                DataType = "Integer",
                ControlType = "TextBox",
                DefaultValue = "5",
                IsActive = true
            },
            new()
            {
                Id = 3,
                CategoryId = 2,
                ConfigCode = "ENABLE_NOTIFICATIONS",
                ConfigName = "Enable Notifications",
                Description = "Enable or disable notifications",
                DataType = "Boolean",
                ControlType = "CheckBox",
                DefaultValue = "true",
                IsActive = false
            }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ConfigKeyMasterEntity, ConfigKeyMasterDto>();
        });

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new ConfigKeyMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new ConfigKeyMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(3, items.Count);
        Assert.Contains(items, x => x.ConfigCode == "APP_TIMEOUT" && x.ConfigName == "Application Timeout");
        Assert.Contains(items, x => x.ConfigCode == "MAX_LOGIN_ATTEMPTS" && x.ConfigName == "Maximum Login Attempts");
        Assert.Contains(items, x => x.ConfigCode == "ENABLE_NOTIFICATIONS" && x.ConfigName == "Enable Notifications");
    }

    [Fact]
    public async Task GetAllAsync_WithConfigCodeFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<ConfigKeyMasterEntity>
        {
            new() { Id = 1, ConfigCode = "APP_TIMEOUT", ConfigName = "Application Timeout", DataType = "Integer", IsActive = true },
            new() { Id = 2, ConfigCode = "APP_THEME", ConfigName = "Application Theme", DataType = "String", IsActive = true },
            new() { Id = 3, ConfigCode = "MAX_LOGIN_ATTEMPTS", ConfigName = "Max Login Attempts", DataType = "Integer", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ConfigKeyMasterEntity, ConfigKeyMasterDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new ConfigKeyMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new ConfigKeyMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            ConfigCode = "APP_TIMEOUT"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        
        var items = result.Items.ToList();
        Assert.Single(items);
        
        var item = items.First();
        Assert.Equal("APP_TIMEOUT", item.ConfigCode);
        Assert.Equal("Application Timeout", item.ConfigName);
        Assert.Equal("Integer", item.DataType);
    }

    [Fact]
    public async Task GetAllAsync_WithCategoryIdFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<ConfigKeyMasterEntity>
        {
            new() { Id = 1, CategoryId = 1, ConfigCode = "APP_TIMEOUT", ConfigName = "Application Timeout", IsActive = true },
            new() { Id = 2, CategoryId = 2, ConfigCode = "DB_CONNECTION", ConfigName = "Database Connection", IsActive = true },
            new() { Id = 3, CategoryId = 1, ConfigCode = "APP_THEME", ConfigName = "Application Theme", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ConfigKeyMasterEntity, ConfigKeyMasterDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new ConfigKeyMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new ConfigKeyMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            CategoryId = 1
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item => Assert.Equal(1, item.CategoryId));
    }

    [Fact]
    public async Task GetAllAsync_WithDataTypeFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<ConfigKeyMasterEntity>
        {
            new() { Id = 1, ConfigCode = "APP_TIMEOUT", ConfigName = "Application Timeout", DataType = "Integer", IsActive = true },
            new() { Id = 2, ConfigCode = "APP_THEME", ConfigName = "Application Theme", DataType = "String", IsActive = true },
            new() { Id = 3, ConfigCode = "ENABLE_FEATURE", ConfigName = "Enable Feature", DataType = "Boolean", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ConfigKeyMasterEntity, ConfigKeyMasterDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new ConfigKeyMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new ConfigKeyMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            DataType = "Integer"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item =>
            Assert.Contains("Integer", item.DataType, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<ConfigKeyMasterEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ConfigKeyMasterEntity, ConfigKeyMasterDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new ConfigKeyMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new ConfigKeyMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entities = Enumerable.Range(1, 25)
            .Select(i => new ConfigKeyMasterEntity
            {
                Id = i,
                ConfigCode = $"CONFIG{i:000}",
                ConfigName = $"Configuration {i}",
                Description = $"Description {i}",
                DataType = "String",
                ControlType = "TextBox",
                DefaultValue = $"Value {i}",
                IsActive = true
            })
            .ToList();

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ConfigKeyMasterEntity, ConfigKeyMasterDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new ConfigKeyMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new ConfigKeyMasterQueryParameters
        {
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
    }

    [Fact]
    public async Task GetAllAsync_WithSearchTerm_ReturnsMatchingEntities()
    {
        // Arrange
        var entities = new List<ConfigKeyMasterEntity>
        {
            new() { Id = 1, ConfigCode = "APP_TIMEOUT", ConfigName = "Application Timeout", DataType = "Integer", IsActive = true },
            new() { Id = 2, ConfigCode = "DB_CONNECTION", ConfigName = "Database Connection", DataType = "String", IsActive = true },
            new() { Id = 3, ConfigCode = "APP_THEME", ConfigName = "Application Theme", DataType = "String", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ConfigKeyMasterEntity, ConfigKeyMasterDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new ConfigKeyMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new ConfigKeyMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Database"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        var items = result.Items.ToList();
        Assert.Contains(items, item =>
            (item.ConfigName != null && item.ConfigName.Contains("Database", StringComparison.OrdinalIgnoreCase)) ||
            (item.ConfigCode != null && item.ConfigCode.Contains("Database", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task GetAllAsync_WithIsActiveFilter_ReturnsOnlyActiveEntities()
    {
        // Arrange
        var entities = new List<ConfigKeyMasterEntity>
        {
            new() { Id = 1, ConfigCode = "ACTIVE_CONFIG", ConfigName = "Active Config", IsActive = true },
            new() { Id = 2, ConfigCode = "INACTIVE_CONFIG", ConfigName = "Inactive Config", IsActive = false },
            new() { Id = 3, ConfigCode = "ANOTHER_ACTIVE", ConfigName = "Another Active", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ConfigKeyMasterEntity, ConfigKeyMasterDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new ConfigKeyMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new ConfigKeyMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateConfigKeyMasterDto
        {
            CategoryId = 1,
            ConfigCode = "NEW_CONFIG",
            ConfigName = "New Configuration",
            Description = "A new configuration key",
            DataType = "String",
            ControlType = "TextBox",
            DefaultValue = "default",
            CreatedBy = 1
        };

        var entity = new ConfigKeyMasterEntity
        {
            Id = 1,
            CategoryId = 1,
            ConfigCode = "NEW_CONFIG",
            ConfigName = "New Configuration",
            Description = "A new configuration key",
            DataType = "String",
            ControlType = "TextBox",
            DefaultValue = "default",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.UtcNow
        };

        var resultDto = new ConfigKeyMasterDto
        {
            Id = 1,
            CategoryId = 1,
            ConfigCode = "NEW_CONFIG",
            ConfigName = "New Configuration",
            Description = "A new configuration key",
            DataType = "String",
            ControlType = "TextBox",
            DefaultValue = "default",
            IsActive = true
        };

        _mockMapper.Setup(m => m.Map<ConfigKeyMasterEntity>(createDto))
            .Returns(entity);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ConfigKeyMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<ConfigKeyMasterDto>(It.IsAny<ConfigKeyMasterEntity>()))
            .Returns(resultDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NEW_CONFIG", result.ConfigCode);
        Assert.Equal("New Configuration", result.ConfigName);
        Assert.Equal("String", result.DataType);
        Assert.Equal("TextBox", result.ControlType);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ConfigKeyMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidDto_ReturnsUpdatedDto()
    {
        // Arrange
        var updateDto = new UpdateConfigKeyMasterDto
        {
            CategoryId = 1,
            ConfigCode = "UPDATED_CONFIG",
            ConfigName = "Updated Configuration",
            Description = "Updated description",
            DataType = "Integer",
            ControlType = "NumericUpDown",
            DefaultValue = "100",
            UpdatedBy = 1
        };

        var existingEntity = new ConfigKeyMasterEntity
        {
            Id = 1,
            CategoryId = 1,
            ConfigCode = "OLD_CONFIG",
            ConfigName = "Old Configuration",
            IsActive = true
        };

        var updatedEntity = new ConfigKeyMasterEntity
        {
            Id = 1,
            CategoryId = 1,
            ConfigCode = "UPDATED_CONFIG",
            ConfigName = "Updated Configuration",
            Description = "Updated description",
            DataType = "Integer",
            ControlType = "NumericUpDown",
            DefaultValue = "100",
            IsActive = true,
            UpdatedBy = 1,
            UpdatedDate = DateTime.UtcNow
        };

        var resultDto = new ConfigKeyMasterDto
        {
            Id = 1,
            CategoryId = 1,
            ConfigCode = "UPDATED_CONFIG",
            ConfigName = "Updated Configuration",
            Description = "Updated description",
            DataType = "Integer",
            ControlType = "NumericUpDown",
            DefaultValue = "100",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper.Setup(m => m.Map(updateDto, existingEntity))
            .Returns(updatedEntity);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ConfigKeyMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper.Setup(m => m.Map<ConfigKeyMasterDto>(It.IsAny<ConfigKeyMasterEntity>()))
            .Returns(resultDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("UPDATED_CONFIG", result.ConfigCode);
        Assert.Equal("Updated Configuration", result.ConfigName);
        Assert.Equal("Integer", result.DataType);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ConfigKeyMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateConfigKeyMasterDto
        {
            ConfigCode = "UPDATED_CONFIG",
            ConfigName = "Updated Configuration",
            UpdatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigKeyMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ConfigKeyMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrue()
    {
        // Arrange
        var entity = new ConfigKeyMasterEntity
        {
            Id = 1,
            ConfigCode = "TO_DELETE",
            ConfigName = "To Delete",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockRepository.Setup(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigKeyMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteAsync_InvalidId_ReturnsFalse(int invalidId)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigKeyMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(invalidId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public async Task GetAllAsync_SortByConfigCode_Ascending_ReturnsSortedResults()
    {
        // Arrange
        var entities = new List<ConfigKeyMasterEntity>
        {
            new() { Id = 1, ConfigCode = "ZEBRA_CONFIG", ConfigName = "Zebra Config", IsActive = true },
            new() { Id = 2, ConfigCode = "ALPHA_CONFIG", ConfigName = "Alpha Config", IsActive = true },
            new() { Id = 3, ConfigCode = "BETA_CONFIG", ConfigName = "Beta Config", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ConfigKeyMasterEntity, ConfigKeyMasterDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new ConfigKeyMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new ConfigKeyMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "ConfigCode",
            SortOrder = "asc"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.True(items.Count > 0);
        Assert.Equal("ALPHA_CONFIG", items[0].ConfigCode);
        Assert.Equal("BETA_CONFIG", items[1].ConfigCode);
        Assert.Equal("ZEBRA_CONFIG", items[2].ConfigCode);
    }

    [Fact]
    public async Task GetAllAsync_SortByConfigName_Descending_ReturnsSortedResults()
    {
        // Arrange
        var entities = new List<ConfigKeyMasterEntity>
        {
            new() { Id = 1, ConfigCode = "CONFIG1", ConfigName = "Apple Config", IsActive = true },
            new() { Id = 2, ConfigCode = "CONFIG2", ConfigName = "Banana Config", IsActive = true },
            new() { Id = 3, ConfigCode = "CONFIG3", ConfigName = "Cherry Config", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ConfigKeyMasterEntity, ConfigKeyMasterDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new ConfigKeyMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new ConfigKeyMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "ConfigName",
            SortOrder = "desc"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Equal("Cherry Config", items[0].ConfigName);
        Assert.Equal("Banana Config", items[1].ConfigName);
        Assert.Equal("Apple Config", items[2].ConfigName);
    }

    #endregion

    #region ControlType and DataType Combination Tests

    [Theory]
    [InlineData("String", "TextBox")]
    [InlineData("Integer", "NumericUpDown")]
    [InlineData("Boolean", "CheckBox")]
    [InlineData("Date", "DatePicker")]
    public async Task GetAllAsync_WithControlTypeAndDataType_ReturnsMatchingEntities(string dataType, string controlType)
    {
        // Arrange
        var entities = new List<ConfigKeyMasterEntity>
        {
            new() { Id = 1, ConfigCode = "STRING_CONFIG", DataType = "String", ControlType = "TextBox", IsActive = true },
            new() { Id = 2, ConfigCode = "INT_CONFIG", DataType = "Integer", ControlType = "NumericUpDown", IsActive = true },
            new() { Id = 3, ConfigCode = "BOOL_CONFIG", DataType = "Boolean", ControlType = "CheckBox", IsActive = true },
            new() { Id = 4, ConfigCode = "DATE_CONFIG", DataType = "Date", ControlType = "DatePicker", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ConfigKeyMasterEntity, ConfigKeyMasterDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new ConfigKeyMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new ConfigKeyMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            DataType = dataType,
            ControlType = controlType
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item =>
        {
            Assert.Contains(dataType, item.DataType, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(controlType, item.ControlType, StringComparison.OrdinalIgnoreCase);
        });
    }

    #endregion
}
