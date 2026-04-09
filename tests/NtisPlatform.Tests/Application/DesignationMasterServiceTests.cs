using AutoMapper;
using Moq;
using NtisPlatform.Application.DTOs.Master.DesignationMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for DesignationMasterService
/// </summary>
public class DesignationMasterServiceTests
{
    private readonly Mock<IRepository<DesignationMasterEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IConfigurationProvider> _configurationProviderMock;
    private readonly DesignationMasterService _service;

    public DesignationMasterServiceTests()
    {
        _repositoryMock = new Mock<IRepository<DesignationMasterEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _configurationProviderMock = new Mock<IConfigurationProvider>();

        _mapperMock.Setup(m => m.ConfigurationProvider).Returns(_configurationProviderMock.Object);

        _service = new DesignationMasterService(
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
        var entity = new DesignationMasterEntity
        {
            Id = 1,
            DesignationCode = "DES001",
            DesignationName = "Manager",
            DesignationLocal = "प्रबंधक",
            IsActive = true
        }; 

        var dto = new DesignationMasterDto
        {
            Id = 1,
            DesignationCode = "DES001",
            DesignationName = "Manager",
            DesignationLocal = "प्रबंधक",
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map<DesignationMasterDto>(entity))
            .Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("DES001", result.DesignationCode);
        Assert.Equal("Manager", result.DesignationName);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DesignationMasterEntity?)null);

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
        var createDto = new CreateDesignationMasterDto
        {
            DesignationCode = "DES001",
            DesignationName = "Manager",
            DesignationLocal = "प्रबंधक",
            IsActive = true
        };

        var entity = new DesignationMasterEntity
        {
            Id = 1,
            DesignationCode = "DES001",
            DesignationName = "Manager",
            IsActive = true
        };

        var returnDto = new DesignationMasterDto
        {
            Id = 1,
            DesignationCode = "DES001",
            DesignationName = "Manager",
            IsActive = true
        };

        _mapperMock.Setup(x => x.Map<DesignationMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<DesignationMasterDto>(entity))
            .Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("DES001", result.DesignationCode);
        Assert.Equal("Manager", result.DesignationName);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<DesignationMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesAndReturnsDto()
    {
        // Arrange
        var updateDto = new UpdateDesignationMasterDto
        {
            DesignationCode = "DES001",
            DesignationName = "Senior Manager",
            IsActive = true
        };

        var existingEntity = new DesignationMasterEntity
        {
            Id = 1,
            DesignationCode = "DES001",
            DesignationName = "Manager"
        };

        var returnDto = new DesignationMasterDto
        {
            Id = 1,
            DesignationCode = "DES001",
            DesignationName = "Senior Manager",
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
        _mapperMock.Setup(x => x.Map<DesignationMasterDto>(existingEntity))
            .Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Senior Manager", result.DesignationName);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var entity = new DesignationMasterEntity
        {
            Id = 1,
            DesignationCode = "DES001",
            DesignationName = "Manager"
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
    }

    #endregion
}
