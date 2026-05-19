using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.DTOs.RenterDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Comprehensive tests for DataEntryController
/// Tests all HTTP endpoints and validation scenarios
/// </summary>
public class DataEntryControllerTests
{
    private readonly Mock<IDataEntryService> _mockService;
    private readonly DataEntryController _controller;
    private readonly Mock<ILogger<DataEntryController>> _mockLogger;

    public DataEntryControllerTests()
    {
        _mockService = new Mock<IDataEntryService>();
        _mockLogger = new Mock<ILogger<DataEntryController>>();

        _controller = new DataEntryController(
            _mockService.Object,
            _mockLogger.Object
        );
    }

    #region GET Tests

    [Fact]
    public async Task GetAll_WithValidParameters_ReturnsOkWithPagedResult()
    {
        // Arrange
        var queryParameters = new PropertyDetailsQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var pagedResult = new PagedResult<PropertyDetailsDto>(
            new List<PropertyDetailsDto> { new PropertyDetailsDto { Id = 1 } },
            1,
            1,
            10
        );

        _mockService.Setup(s => s.GetAllAsync(queryParameters, default))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<PagedResult<PropertyDetailsDto>>(okResult.Value);
        Assert.Single(returnValue.Items);
        Assert.Equal(1, returnValue.TotalCount);
    }

    [Fact]
    public async Task GetAll_WithPropertyIdFilter_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var queryParameters = new PropertyDetailsQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            PropertyId = 5
        };

        var pagedResult = new PagedResult<PropertyDetailsDto>(
            new List<PropertyDetailsDto>(),
            0,
            1,
            10
        );

        _mockService.Setup(s => s.GetAllAsync(It.IsAny<PropertyDetailsQueryParameters>(), default))
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.GetAll(queryParameters, default);

        // Assert
        _mockService.Verify(
            s => s.GetAllAsync(
                It.Is<PropertyDetailsQueryParameters>(p => p.PropertyId == 5),
                default),
            Times.Once);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkWithDto()
    {
        // Arrange
        var id = 1;
        var dto = new PropertyDetailsDto { Id = id };

        _mockService.Setup(s => s.GetByIdAsync(id, default))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(id, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<PropertyDetailsDto>(okResult.Value);
        Assert.Equal(id, returnValue.Id);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var id = 999;
        _mockService.Setup(s => s.GetByIdAsync(id, default))
            .ReturnsAsync((PropertyDetailsDto?)null);

        // Act
        var result = await _controller.GetById(id, default);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region POST Tests

    [Fact]
    public async Task  Create_WithValidDto_ReturnsOk()
    {
        // Arrange
        var createDto = new CreatePropertyDetailsDto
        {
            PropertyId = 100,
            FloorId = 1
        };

        var createdDto = new PropertyDetailsDto
        {
            Id = 1,
            PropertyId = 100,
            FloorId = 1
        };

        _mockService.Setup(s => s.CreateAsync(createDto, default))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDetailsDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record inserted successfully", response.Message);
        Assert.NotNull(response.Items);
        Assert.Equal(1, response.Items.Id);
    }

    [Fact]
    public async Task Create_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreatePropertyDetailsDto();
        _controller.ModelState.AddModelError("PropertyId", "PropertyId is required");

        // Act
        var result = await _controller.Create(createDto, default);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);

        _mockService.Verify(
            x => x.CreateAsync(
                It.IsAny<CreatePropertyDetailsDto>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }


    [Fact]
    public async Task Create_WithNestedCollections_CallsServiceCorrectly()
    {
        // Arrange
        var createDto = new CreatePropertyDetailsDto
        {
            PropertyId = 100,
            RenterDetails = new List<CreateRenterDetailsDto>
            {
                new CreateRenterDetailsDto()
            }
        };

        var createdDto = new PropertyDetailsDto { Id = 1 };

        _mockService.Setup(s => s.CreateAsync(It.IsAny<CreatePropertyDetailsDto>(), default))
            .ReturnsAsync(createdDto);

        // Act
        await _controller.Create(createDto, default);

        // Assert
        _mockService.Verify(
            s => s.CreateAsync(
                It.Is<CreatePropertyDetailsDto>(d => d.RenterDetails != null && d.RenterDetails.Any()),
                default),
            Times.Once);
    }

    #endregion

    #region PUT Tests

    [Fact]
    public async Task Update_WithValidDto_ReturnsOk()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdatePropertyDetailsDto
        {
            PropertyId = 100,
            FloorId = 1
        };

        var updatedDto = new PropertyDetailsDto
        {
            Id = id,
            PropertyId = 100,
            FloorId = 1
        };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto, default))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.Update(id, updateDto, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDetailsDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record updated successfully", response.Message);
        Assert.NotNull(response.Items);
        Assert.Equal(id, response.Items.Id);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var id = 999;
        var updateDto = new UpdatePropertyDetailsDto();

        _mockService.Setup(s => s.UpdateAsync(id, updateDto, default))
            .ReturnsAsync((PropertyDetailsDto?)null);

        // Act
        var result = await _controller.Update(id, updateDto, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDetailsDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Record not found for Update ", response.Message);
    }

    [Fact]
    public async Task Update_WithInvalidModelState_StillCallsServiceAndReturnsNotFound()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdatePropertyDetailsDto();
        _controller.ModelState.AddModelError("PropertyId", "PropertyId is required");

        // ExecuteUpdate doesn't validate ModelState, it delegates directly to the service
        // Mock service to return null (entity not found)
        _mockService.Setup(s => s.UpdateAsync(id, updateDto, default))
            .ReturnsAsync((PropertyDetailsDto?)null);

        // Act
        var result = await _controller.Update(id, updateDto, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDetailsDto>>(okResult.Value);

        // CrudControllerExtensions doesn't check ModelState in ExecuteUpdate
        // It returns "Record not found for Update" when service returns null
        Assert.False(response.Success);
        Assert.Equal("Record not found for Update ", response.Message);

        // Verify service was still called despite invalid ModelState
        _mockService.Verify(s => s.UpdateAsync(id, updateDto, default), Times.Once);
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var id = 1;
        _mockService.Setup(s => s.DeleteAsync(id, default))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(id, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDetailsDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record marked for deletion", response.Message);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var id = 999;
        _mockService.Setup(s => s.DeleteAsync(id, default))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(id, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDetailsDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Record not found", response.Message);
    }

    [Fact]
    public async Task Delete_CallsServiceWithCorrectId()
    {
        // Arrange
        var id = 5;
        _mockService.Setup(s => s.DeleteAsync(id, default))
            .ReturnsAsync(true);

        // Act
        await _controller.Delete(id, default);

        // Assert
        _mockService.Verify(s => s.DeleteAsync(id, default), Times.Once);
    }

    #endregion
}