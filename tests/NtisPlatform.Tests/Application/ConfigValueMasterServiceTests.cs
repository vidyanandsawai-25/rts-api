using AutoMapper;
using Moq;
using NtisPlatform.Application.DTOs.Master.ConfigValueMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for ConfigValueMasterService
/// </summary>
public class ConfigValueMasterServiceTests
{
    private readonly Mock<IRepository<ConfigValueMasterEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IConfigurationProvider> _configurationProviderMock;
    private readonly ConfigValueMasterService _service;

    public ConfigValueMasterServiceTests()
    {
        _repositoryMock = new Mock<IRepository<ConfigValueMasterEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _configurationProviderMock = new Mock<IConfigurationProvider>();

        _mapperMock.Setup(m => m.ConfigurationProvider).Returns(_configurationProviderMock.Object);

        _service = new ConfigValueMasterService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsDto()
    {
        // Arrange
        var entity = new ConfigValueMasterEntity
        {
            Id = 1,
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Test Configuration Value",
            IsActive = true
        };

        var dto = new ConfigValueMasterDto
        {
            Id = 1,
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Test Configuration Value",
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map<ConfigValueMasterDto>(entity))
            .Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.ConfigKeyId);
        Assert.Equal(5, result.DepartmentId);
        Assert.Equal(3, result.ModuleId);
        Assert.Equal("Test Configuration Value", result.Value);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigValueMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesAndReturnsDto()
    {
        // Arrange
        var createDto = new CreateConfigValueMasterDto
        {
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "New Configuration Value",
            IsActive = true,
            CreatedBy = 100
        };

        var entity = new ConfigValueMasterEntity
        {
            Id = 1,
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "New Configuration Value",
            IsActive = true,
            CreatedBy = 100
        };

        var returnDto = new ConfigValueMasterDto
        {
            Id = 1,
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "New Configuration Value",
            IsActive = true
        };

        _mapperMock.Setup(x => x.Map<ConfigValueMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<ConfigValueMasterDto>(entity))
            .Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.ConfigKeyId);
        Assert.Equal("New Configuration Value", result.Value);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<ConfigValueMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullDepartmentAndModule_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateConfigValueMasterDto
        {
            ConfigKeyId = 10,
            DepartmentId = null,
            ModuleId = null,
            Value = "Global Configuration Value",
            IsActive = true,
            CreatedBy = 100
        };

        var entity = new ConfigValueMasterEntity
        {
            Id = 1,
            ConfigKeyId = 10,
            DepartmentId = null,
            ModuleId = null,
            Value = "Global Configuration Value",
            IsActive = true,
            CreatedBy = 100
        };

        var returnDto = new ConfigValueMasterDto
        {
            Id = 1,
            ConfigKeyId = 10,
            DepartmentId = null,
            ModuleId = null,
            Value = "Global Configuration Value",
            IsActive = true
        };

        _mapperMock.Setup(x => x.Map<ConfigValueMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<ConfigValueMasterDto>(entity))
            .Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.DepartmentId);
        Assert.Null(result.ModuleId);
        Assert.Equal("Global Configuration Value", result.Value);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesAndReturnsDto()
    {
        // Arrange
        var updateDto = new UpdateConfigValueMasterDto
        {
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Updated Configuration Value",
            IsActive = true,
            UpdatedBy = 200
        };

        var existingEntity = new ConfigValueMasterEntity
        {
            Id = 1,
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Original Value",
            IsActive = true
        };

        var returnDto = new ConfigValueMasterDto
        {
            Id = 1,
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Updated Configuration Value",
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity))
            .Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<ConfigValueMasterDto>(existingEntity))
            .Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Configuration Value", result.Value);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ConfigValueMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateConfigValueMasterDto
        {
            ConfigKeyId = 10,
            Value = "Updated Value",
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigValueMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto);

        // Assert
        Assert.Null(result);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ConfigValueMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ChangingConfigKeyId_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateConfigValueMasterDto
        {
            ConfigKeyId = 20, // Changed from 10
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Updated Value",
            IsActive = true
        };

        var existingEntity = new ConfigValueMasterEntity
        {
            Id = 1,
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Original Value",
            IsActive = true
        };

        var returnDto = new ConfigValueMasterDto
        {
            Id = 1,
            ConfigKeyId = 20,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Updated Value",
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity))
            .Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<ConfigValueMasterDto>(existingEntity))
            .Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(20, result.ConfigKeyId);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesSuccessfully()
    {
        // Arrange
        var entity = new ConfigValueMasterEntity
        {
            Id = 1,
            ConfigKeyId = 10,
            Value = "Test Value",
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(x => x.DeleteAsync(It.IsAny<ConfigValueMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<ConfigValueMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigValueMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<ConfigValueMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CreateAsync_WithMaxLengthValue_CreatesSuccessfully()
    {
        // Arrange
        var maxLengthValue = new string('A', 500); // Max length from database schema
        var createDto = new CreateConfigValueMasterDto
        {
            ConfigKeyId = 10,
            Value = maxLengthValue,
            IsActive = true
        };

        var entity = new ConfigValueMasterEntity
        {
            Id = 1,
            ConfigKeyId = 10,
            Value = maxLengthValue,
            IsActive = true
        };

        var returnDto = new ConfigValueMasterDto
        {
            Id = 1,
            ConfigKeyId = 10,
            Value = maxLengthValue,
            IsActive = true
        };

        _mapperMock.Setup(x => x.Map<ConfigValueMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<ConfigValueMasterDto>(entity))
            .Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(500, result.Value?.Length);
    }

    [Fact]
    public async Task UpdateAsync_SetDepartmentAndModuleToNull_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateConfigValueMasterDto
        {
            ConfigKeyId = 10,
            DepartmentId = null,
            ModuleId = null,
            Value = "Updated to Global",
            IsActive = true
        };

        var existingEntity = new ConfigValueMasterEntity
        {
            Id = 1,
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Department Specific",
            IsActive = true
        };

        var returnDto = new ConfigValueMasterDto
        {
            Id = 1,
            ConfigKeyId = 10,
            DepartmentId = null,
            ModuleId = null,
            Value = "Updated to Global",
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity))
            .Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<ConfigValueMasterDto>(existingEntity))
            .Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.DepartmentId);
        Assert.Null(result.ModuleId);
    }

    #endregion
}
