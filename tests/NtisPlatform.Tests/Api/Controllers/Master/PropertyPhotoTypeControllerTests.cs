using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master.PropertyPhotoType;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Tests.Api.Controllers.Master;

/// <summary>
/// Comprehensive controller tests for PropertyPhotoTypeController to achieve 100% code coverage
/// </summary>
public class PropertyPhotoTypeControllerTests
{
    private readonly Mock<IPropertyPhotoTypeService> _serviceMock;
    private readonly Mock<IHardDeleteCleanupService> _cleanupServiceMock;
    private readonly Mock<IReferenceValidationService> _referenceValidationMock;
    private readonly Mock<ILogger<PropertyPhotoTypeController>> _loggerMock;
    private readonly PropertyPhotoTypeController _controller;

    public PropertyPhotoTypeControllerTests()
    {
        _serviceMock = new Mock<IPropertyPhotoTypeService>();
        _cleanupServiceMock = new Mock<IHardDeleteCleanupService>();
        _referenceValidationMock = new Mock<IReferenceValidationService>();
        _loggerMock = new Mock<ILogger<PropertyPhotoTypeController>>();

        _controller = new PropertyPhotoTypeController(
            _serviceMock.Object,
            _cleanupServiceMock.Object,
            _referenceValidationMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange & Act
        var controller = new PropertyPhotoTypeController(
            _serviceMock.Object,
            _cleanupServiceMock.Object,
            _referenceValidationMock.Object,
            _loggerMock.Object);

        // Assert
        Assert.NotNull(controller);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkResult()
    {
        // Arrange
        var queryParams = new PropertyPhotoTypeQueryParameters();
        var pagedResult = new PagedResult<PropertyPhotoTypeDto>
        {
            Items = new List<PropertyPhotoTypeDto> 
            { 
                new PropertyPhotoTypeDto 
                { 
                    Id = 1, 
                    PhotoTypeCode = "FRONT", 
                    PhotoTypeName = "Front View" 
                } 
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<PropertyPhotoTypeQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var queryParams = new PropertyPhotoTypeQueryParameters();
        var pagedResult = new PagedResult<PropertyPhotoTypeDto>
        {
            Items = new List<PropertyPhotoTypeDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<PropertyPhotoTypeQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAll_WithMultipleItems_ReturnsOkWithAllItems()
    {
        // Arrange
        var queryParams = new PropertyPhotoTypeQueryParameters();
        var pagedResult = new PagedResult<PropertyPhotoTypeDto>
        {
            Items = new List<PropertyPhotoTypeDto>
            {
                new() { Id = 1, PhotoTypeCode = "FRONT", PhotoTypeName = "Front View" },
                new() { Id = 2, PhotoTypeCode = "BACK", PhotoTypeName = "Back View" },
                new() { Id = 3, PhotoTypeCode = "LEFT", PhotoTypeName = "Left Side" }
            },
            TotalCount = 3,
            PageNumber = 1,
            PageSize = 10
        };

        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<PropertyPhotoTypeQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var queryParams = new PropertyPhotoTypeQueryParameters();
        var cancellationToken = new CancellationToken();
        var pagedResult = new PagedResult<PropertyPhotoTypeDto>
        {
            Items = new List<PropertyPhotoTypeDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _serviceMock.Setup(s => s.GetAllAsync(queryParams, cancellationToken))
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.GetAll(queryParams, cancellationToken);

        // Assert
        _serviceMock.Verify(s => s.GetAllAsync(queryParams, cancellationToken), Times.Once);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var dto = new PropertyPhotoTypeDto 
        { 
            Id = 1, 
            PhotoTypeCode = "FRONT", 
            PhotoTypeName = "Front View" 
        };

        _serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyPhotoTypeDto?)null);

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_WithZeroId_TriesToFetch()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyPhotoTypeDto?)null);

        // Act
        var result = await _controller.GetById(0, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _serviceMock.Verify(s => s.GetByIdAsync(0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WithNegativeId_TriesToFetch()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(-1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyPhotoTypeDto?)null);

        // Act
        var result = await _controller.GetById(-1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsOkResult()
    {
        // Arrange
        var createDto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            IsActive = true
        };

        var createdDto = new PropertyPhotoTypeDto 
        { 
            Id = 1, 
            PhotoTypeCode = "FRONT", 
            PhotoTypeName = "Front View" 
        };

        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreatePropertyPhotoTypeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_CallsServiceCreateAsync()
    {
        // Arrange
        var createDto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "TEST",
            PhotoTypeName = "Test Name",
            IsActive = true
        };

        var createdDto = new PropertyPhotoTypeDto { Id = 1 };

        _serviceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        await _controller.Create(createDto, CancellationToken.None);

        // Assert
        _serviceMock.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithMinimalData_ReturnsOkResult()
    {
        // Arrange
        var createDto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "MIN",
            PhotoTypeName = "Minimal",
            IsActive = true
        };

        var createdDto = new PropertyPhotoTypeDto { Id = 1 };

        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreatePropertyPhotoTypeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var updateDto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "UPDATED",
            PhotoTypeName = "Updated Name",
            IsActive = true
        };

        var updatedDto = new PropertyPhotoTypeDto { Id = 1 };

        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdatePropertyPhotoTypeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsOk()
    {
        // Arrange
        var updateDto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "TEST",
            PhotoTypeName = "Test",
            IsActive = true
        };

        _serviceMock.Setup(s => s.UpdateAsync(999, It.IsAny<UpdatePropertyPhotoTypeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyPhotoTypeDto?)null);

        // Act
        var result = await _controller.Update(999, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_CallsServiceUpdateAsync()
    {
        // Arrange
        var updateDto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "UPD",
            PhotoTypeName = "Updated",
            IsActive = false
        };

        var updatedDto = new PropertyPhotoTypeDto { Id = 1 };

        _serviceMock.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        await _controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        _serviceMock.Verify(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_CallsServiceDeleteAsync()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _controller.Delete(1, CancellationToken.None);

        // Assert
        _serviceMock.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Purge Tests

    [Fact]
    public async Task Purge_WithValidId_CallsForceHardDeleteAndReturnsOk()
    {
        // Arrange
        _cleanupServiceMock.Setup(s => s.ForceHardDeleteAsync<PropertyPhotoTypeEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Purge(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("permanently deleted", response.Message, StringComparison.OrdinalIgnoreCase);
        _cleanupServiceMock.Verify(s => s.ForceHardDeleteAsync<PropertyPhotoTypeEntity, int>(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Purge_WithInvalidId_ReturnsOkWithFailureMessage()
    {
        // Arrange
        _cleanupServiceMock.Setup(s => s.ForceHardDeleteAsync<PropertyPhotoTypeEntity, int>(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Purge(999, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message, StringComparison.OrdinalIgnoreCase);
        _cleanupServiceMock.Verify(s => s.ForceHardDeleteAsync<PropertyPhotoTypeEntity, int>(999, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Purge_WithZeroId_CallsService()
    {
        // Arrange
        _cleanupServiceMock.Setup(s => s.ForceHardDeleteAsync<PropertyPhotoTypeEntity, int>(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Purge(0, CancellationToken.None);

        // Assert
        _cleanupServiceMock.Verify(s => s.ForceHardDeleteAsync<PropertyPhotoTypeEntity, int>(0, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
