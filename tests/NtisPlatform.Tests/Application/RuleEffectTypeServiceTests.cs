using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Master.RuleEffectTypeMaster;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive unit tests for RuleEffectTypeService
/// Tests business logic, data operations, and service layer functionality
/// </summary>
public class RuleEffectTypeServiceTests
{
    private readonly Mock<IRepository<RuleEffectTypeEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RuleEffectTypeService _service;

    public RuleEffectTypeServiceTests()
    {
        _mockRepository = new Mock<IRepository<RuleEffectTypeEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

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
            _mockMapper.Object);
    }

    /// <summary>
    /// Creates a real IMapper using the production RuleEffectTypeMappingProfile.
    /// This ensures tests validate the actual mapping configuration.
    /// </summary>
    private static IMapper CreateRealMapper()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<RuleEffectTypeMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        // Assert configuration is valid - catches mapping errors at test time
        mapperConfig.AssertConfigurationIsValid();
        return mapperConfig.CreateMapper();
    }

    #region Mapping Profile Tests

    [Fact]
    public void RuleEffectTypeMappingProfile_ConfigurationIsValid()
    {
        // Arrange & Act
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<RuleEffectTypeMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        // Assert - This will throw if configuration is invalid
        mapperConfig.AssertConfigurationIsValid();
    }

    [Fact]
    public void RuleEffectTypeMappingProfile_EntityToDto_MapsCorrectly()
    {
        // Arrange
        var mapper = CreateRealMapper();
        var entity = new RuleEffectTypeEntity
        {
            Id = 1,
            EffectType = "Add",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };

        // Act
        var dto = mapper.Map<RuleEffectTypeDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.EffectType, dto.EffectType);
        Assert.Equal(entity.IsActive, dto.IsActive);
    }

    [Fact]
    public void RuleEffectTypeMappingProfile_CreateDtoToEntity_MapsCorrectly()
    {
        // Arrange
        var mapper = CreateRealMapper();
        var createDto = new CreateRuleEffectTypeDto
        {
            EffectType = "Add",
            IsActive = true,
            CreatedBy = 1
        };

        // Act
        var entity = mapper.Map<RuleEffectTypeEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(createDto.EffectType, entity.EffectType);
        Assert.Equal(createDto.IsActive, entity.IsActive);
    }

    [Fact]
    public void RuleEffectTypeMappingProfile_UpdateDtoToEntity_MapsCorrectly()
    {
        // Arrange
        var mapper = CreateRealMapper();
        var updateDto = new UpdateRuleEffectTypeDto
        {
            EffectType = "Multiply",
            IsActive = false,
            UpdatedBy = 2
        };

        var existingEntity = new RuleEffectTypeEntity
        {
            Id = 1,
            EffectType = "Add",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-1)
        };

        // Act
        mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(updateDto.EffectType, existingEntity.EffectType);
        Assert.Equal(updateDto.IsActive, existingEntity.IsActive);
        Assert.Equal(1, existingEntity.Id); // ID should not change
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new RuleEffectTypeEntity
        {
            Id = 1,
            EffectType = "Add",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = 1,
            UpdatedDate = DateTime.Now
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RuleEffectTypeDto>(It.IsAny<RuleEffectTypeEntity>()))
            .Returns(new RuleEffectTypeDto
            {
                Id = 1,
                EffectType = "Add",
                IsActive = true,
                CreatedDate = entity.CreatedDate,
                UpdatedDate = entity.UpdatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Add", result.EffectType);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<RuleEffectTypeDto>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleEffectTypeEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<RuleEffectTypeDto>(It.IsAny<RuleEffectTypeEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleEffectTypeEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(invalidId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<RuleEffectTypeEntity>
        {
            new() { Id = 1, EffectType = "Add", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now },
            new() { Id = 2, EffectType = "Multiply", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now },
            new() { Id = 3, EffectType = "Subtract", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Use real mapper with production profile
        var mapper = CreateRealMapper();

        var service = new RuleEffectTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new RuleEffectTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(3, items.Count);

        Assert.Contains(items, x => x.EffectType == "Add");
        Assert.Contains(items, x => x.EffectType == "Multiply");
        Assert.Contains(items, x => x.EffectType == "Subtract");
    }

    [Fact]
    public async Task GetAllAsync_WithFiltering_ReturnsFilteredResults()
    {
        // Arrange
        var entities = new List<RuleEffectTypeEntity>
        {
            new() { Id = 1, EffectType = "Add", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now },
            new() { Id = 2, EffectType = "Multiply", IsActive = false, CreatedBy = 1, CreatedDate = DateTime.Now },
            new() { Id = 3, EffectType = "Subtract", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapper = CreateRealMapper();
        var service = new RuleEffectTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new RuleEffectTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entities = new List<RuleEffectTypeEntity>();
        for (int i = 1; i <= 25; i++)
        {
            entities.Add(new RuleEffectTypeEntity
            {
                Id = i,
                EffectType = $"Effect{i}",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            });
        }

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapper = CreateRealMapper();
        var service = new RuleEffectTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new RuleEffectTypeQueryParameters
        {
            PageNumber = 2,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<RuleEffectTypeEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapper = CreateRealMapper();
        var service = new RuleEffectTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new RuleEffectTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateRuleEffectTypeDto
        {
            EffectType = "Add",
            IsActive = true,
            CreatedBy = 1
        };

        var entity = new RuleEffectTypeEntity
        {
            Id = 1,
            EffectType = "Add",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };

        var resultDto = new RuleEffectTypeDto
        {
            Id = 1,
            EffectType = "Add",
            IsActive = true
        };

        _mockMapper.Setup(m => m.Map<RuleEffectTypeEntity>(createDto))
            .Returns(entity);

        _mockMapper.Setup(m => m.Map<RuleEffectTypeDto>(entity))
            .Returns(resultDto);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<RuleEffectTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Add", result.EffectType);
        Assert.True(result.IsActive);

        _mockMapper.Verify(m => m.Map<RuleEffectTypeEntity>(createDto), Times.Once);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RuleEffectTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Add")]
    [InlineData("Multiply")]
    [InlineData("Subtract")]
    [InlineData("Divide")]
    public async Task CreateAsync_DifferentEffectTypes_CreatesSuccessfully(string effectType)
    {
        // Arrange
        var createDto = new CreateRuleEffectTypeDto
        {
            EffectType = effectType,
            IsActive = true,
            CreatedBy = 1
        };

        var entity = new RuleEffectTypeEntity
        {
            Id = 1,
            EffectType = effectType,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };

        _mockMapper.Setup(m => m.Map<RuleEffectTypeEntity>(createDto))
            .Returns(entity);

        _mockMapper.Setup(m => m.Map<RuleEffectTypeDto>(entity))
            .Returns(new RuleEffectTypeDto { Id = 1, EffectType = effectType, IsActive = true });

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<RuleEffectTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(effectType, result.EffectType);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesAndReturnsDto()
    {
        // Arrange
        var existingEntity = new RuleEffectTypeEntity
        {
            Id = 1,
            EffectType = "Add",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-1)
        };

        var updateDto = new UpdateRuleEffectTypeDto
        {
            EffectType = "Multiply",
            IsActive = true,
            UpdatedBy = 2
        };

        var updatedDto = new RuleEffectTypeDto
        {
            Id = 1,
            EffectType = "Multiply",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper.Setup(m => m.Map(updateDto, existingEntity))
            .Returns(existingEntity);

        _mockMapper.Setup(m => m.Map<RuleEffectTypeDto>(existingEntity))
            .Returns(updatedDto);

        _mockRepository.Setup(r => r.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Multiply", result.EffectType);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateRuleEffectTypeDto
        {
            EffectType = "Multiply",
            IsActive = true,
            UpdatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleEffectTypeEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RuleEffectTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingEntity_UpdatesIsActiveFlagCorrectly()
    {
        // Arrange
        var existingEntity = new RuleEffectTypeEntity
        {
            Id = 1,
            EffectType = "Add",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-1)
        };

        var updateDto = new UpdateRuleEffectTypeDto
        {
            EffectType = "Add",
            IsActive = false,
            UpdatedBy = 2
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper.Setup(m => m.Map(updateDto, existingEntity))
            .Callback<UpdateRuleEffectTypeDto, RuleEffectTypeEntity>((dto, entity) =>
            {
                entity.IsActive = dto.IsActive;
                entity.EffectType = dto.EffectType;
            })
            .Returns(existingEntity);

        _mockMapper.Setup(m => m.Map<RuleEffectTypeDto>(existingEntity))
            .Returns(new RuleEffectTypeDto { Id = 1, EffectType = "Add", IsActive = false });

        _mockRepository.Setup(r => r.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingId_DeletesAndReturnsTrue()
    {
        // Arrange
        var entity = new RuleEffectTypeEntity
        {
            Id = 1,
            EffectType = "Add",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<RuleEffectTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleEffectTypeEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RuleEffectTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task DeleteAsync_ValidIds_DeletesSuccessfully(int id)
    {
        // Arrange
        var entity = new RuleEffectTypeEntity
        {
            Id = id,
            EffectType = $"Effect{id}",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<RuleEffectTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Edge Cases and Validation Tests

    [Fact]
    public async Task CreateAsync_WithRealMapper_ValidatesMapping()
    {
        // Arrange
        var mapper = CreateRealMapper();
        var service = new RuleEffectTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var createDto = new CreateRuleEffectTypeDto
        {
            EffectType = "Add",
            IsActive = true,
            CreatedBy = 1
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<RuleEffectTypeEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RuleEffectTypeEntity, CancellationToken>((entity, ct) =>
            {
                entity.Id = 1; // Simulate DB auto-increment
                entity.CreatedDate = DateTime.Now;
            })
            .ReturnsAsync((RuleEffectTypeEntity e, CancellationToken ct) => e);

        // Act
        var result = await service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Add", result.EffectType);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_WithRealMapper_ValidatesMapping()
    {
        // Arrange
        var mapper = CreateRealMapper();
        var service = new RuleEffectTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var existingEntity = new RuleEffectTypeEntity
        {
            Id = 1,
            EffectType = "Add",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-1)
        };

        var updateDto = new UpdateRuleEffectTypeDto
        {
            EffectType = "Multiply",
            IsActive = false,
            UpdatedBy = 2
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<RuleEffectTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Multiply", result.EffectType);
        Assert.False(result.IsActive);
        Assert.Equal(1, result.Id); // ID should remain unchanged
    }

    #endregion
}
