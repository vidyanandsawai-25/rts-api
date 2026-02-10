using AutoMapper;
using Moq;
using NtisPlatform.Application.DTOs.Master.DepartmentMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for DepartmentMasterService
/// </summary>
public class DepartmentMasterServiceTests
{
    private readonly Mock<IRepository<DepartmentMasterEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IConfigurationProvider> _configurationProviderMock;
    private readonly DepartmentMasterService _service;

    public DepartmentMasterServiceTests()
    {
        _repositoryMock = new Mock<IRepository<DepartmentMasterEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _configurationProviderMock = new Mock<IConfigurationProvider>();

        _mapperMock.Setup(m => m.ConfigurationProvider).Returns(_configurationProviderMock.Object);

        _service = new DepartmentMasterService(
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
        var entity = new DepartmentMasterEntity
        {
            DepartmentMasterId = 1,
            DepartmentCode = "DEPT001",
            DepartmentName = "Finance",
            DepartmentNameLocal = "वित्त",
            IsActive = true
        };

        var dto = new DepartmentMasterDto
        {
            DepartmentMasterId = 1,
            DepartmentCode = "DEPT001",
            DepartmentName = "Finance",
            DepartmentNameLocal = "वित्त",
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map<DepartmentMasterDto>(entity))
            .Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.DepartmentMasterId);
        Assert.Equal("DEPT001", result.DepartmentCode);
        Assert.Equal("Finance", result.DepartmentName);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepartmentMasterEntity?)null);

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
        var createDto = new CreateDepartmentMasterDto
        {
            DepartmentCode = "DEPT001",
            DepartmentName = "Finance",
            DepartmentNameLocal = "वित्त",
            IsActive = true
        };

        var entity = new DepartmentMasterEntity
        {
            DepartmentMasterId = 1,
            DepartmentCode = "DEPT001",
            DepartmentName = "Finance",
            DepartmentNameLocal = "वित्त",
            IsActive = true
        };

        var returnDto = new DepartmentMasterDto
        {
            DepartmentMasterId = 1,
            DepartmentCode = "DEPT001",
            DepartmentName = "Finance",
            DepartmentNameLocal = "वित्त",
            IsActive = true
        };

        _mapperMock.Setup(x => x.Map<DepartmentMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<DepartmentMasterDto>(entity))
            .Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("DEPT001", result.DepartmentCode);
        Assert.Equal("Finance", result.DepartmentName);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<DepartmentMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesAndReturnsDto()
    {
        // Arrange
        var updateDto = new UpdateDepartmentMasterDto
        {
            DepartmentCode = "DEPT001",
            DepartmentName = "Finance Updated",
            DepartmentNameLocal = "वित्त अद्यतन",
            IsActive = true
        };

        var existingEntity = new DepartmentMasterEntity
        {
            DepartmentMasterId = 1,
            DepartmentCode = "DEPT001",
            DepartmentName = "Finance",
            IsActive = true
        };

        var returnDto = new DepartmentMasterDto
        {
            DepartmentMasterId = 1,
            DepartmentCode = "DEPT001",
            DepartmentName = "Finance Updated",
            DepartmentNameLocal = "वित्त अद्यतन",
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
        _mapperMock.Setup(x => x.Map<DepartmentMasterDto>(existingEntity))
            .Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Finance Updated", result.DepartmentName);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<DepartmentMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateDepartmentMasterDto
        {
            DepartmentCode = "DEPT001",
            DepartmentName = "Finance Updated"
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepartmentMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto);

        // Assert
        Assert.Null(result);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<DepartmentMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var entity = new DepartmentMasterEntity
        {
            DepartmentMasterId = 1,
            DepartmentCode = "DEPT001",
            DepartmentName = "Finance"
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepartmentMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
