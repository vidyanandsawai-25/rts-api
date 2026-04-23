using AutoMapper;
using Moq;
using NtisPlatform.Application.DTOs.Master.ModuleMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for ModuleMasterService
/// </summary>
public class ModuleMasterServiceTests
{
    private readonly Mock<IRepository<ModuleMasterEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IConfigurationProvider> _configurationProviderMock;
    private readonly ModuleMasterService _service;

    public ModuleMasterServiceTests()
    {
        _repositoryMock = new Mock<IRepository<ModuleMasterEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _configurationProviderMock = new Mock<IConfigurationProvider>();

        _mapperMock.Setup(m => m.ConfigurationProvider).Returns(_configurationProviderMock.Object);

        _service = new ModuleMasterService(
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
        var entity = new ModuleMasterEntity
        {
            Id = 1,
            ModuleCode = "MOD001",
            ModuleName = "Taxation",
            ModuleNameLocal = "??????",
            DepartmentId = 1,
            IsActive = true
        };

        var dto = new ModuleMasterDto
        {
            Id = 1,
            ModuleCode = "MOD001",
            ModuleName = "Taxation",
            ModuleNameLocal = "??????",
            DepartmentId = 1,
            DepartmentName = "Finance",
            IsActive = true
        };

            _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
            _mapperMock.Setup(x => x.Map<ModuleMasterDto>(entity)).Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("MOD001", result.ModuleCode);
        Assert.Equal("Taxation", result.ModuleName);
        Assert.Equal("Finance", result.DepartmentName);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ModuleMasterEntity?)null);

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
        var createDto = new CreateModuleMasterDto
        {
            ModuleCode = "MOD001",
            ModuleName = "Taxation",
            ModuleNameLocal = "??????",
            DepartmentId = 1,
            IsActive = true
        };

        var entity = new ModuleMasterEntity
        {
            Id = 1,
            ModuleCode = "MOD001",
            ModuleName = "Taxation",
            DepartmentId = 1,
            IsActive = true
        };

        var returnDto = new ModuleMasterDto
        {
            Id = 1,
            ModuleCode = "MOD001",
            ModuleName = "Taxation",
            DepartmentId = 1,
            IsActive = true
        };

            _mapperMock.Setup(x => x.Map<ModuleMasterEntity>(createDto)).Returns(entity);
            _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mapperMock.Setup(x => x.Map<ModuleMasterDto>(entity)).Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("MOD001", result.ModuleCode);
        Assert.Equal("Taxation", result.ModuleName);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<ModuleMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var entity = new ModuleMasterEntity
        {
            Id = 1,
            ModuleCode = "MOD001",
            ModuleName = "Taxation"
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(x => x.DeleteAsync(It.IsAny<ModuleMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<ModuleMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ModuleMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
public async Task UpdateAsync_WithValidData_UpdatesAndReturnsDto()
{
    // Arrange
    var updateDto = new UpdateModuleMasterDto
    {
        Id = 1,
        ModuleCode = "MOD001",
        ModuleName = "Taxation Updated",
        DepartmentId = 1,
        IsActive = true
    };

    var entity = new ModuleMasterEntity
    {
        Id = 1,
        ModuleCode = "MOD001",
        ModuleName = "Taxation",
        DepartmentId = 1,
        IsActive = true
    };

    var returnDto = new ModuleMasterDto
    {
        Id = 1,
        ModuleCode = "MOD001",
        ModuleName = "Taxation Updated",
        DepartmentId = 1,
        IsActive = true
    };

    _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(entity);

    _mapperMock
        .Setup(x => x.Map(updateDto, entity))
        .Callback<UpdateModuleMasterDto, ModuleMasterEntity>((src, dest) =>
        {
            dest.ModuleName = src.ModuleName;
        });

    _repositoryMock.Setup(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

    _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(1);

    _mapperMock.Setup(x => x.Map<ModuleMasterDto>(entity))
               .Returns(returnDto);

    // Act
    var result = await _service.UpdateAsync(1, updateDto);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Taxation Updated", result.ModuleName);
    _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
}

    #endregion
}
