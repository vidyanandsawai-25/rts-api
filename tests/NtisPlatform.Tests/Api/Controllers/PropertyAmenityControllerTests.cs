using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertyAmenity;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyAmenityControllerTests
{
    private readonly Mock<IPropertyAmenityService> _mockPropertyAmenityService;
    private readonly Mock<ILogger<PropertyAmenityController>> _mockLogger;
    private readonly PropertyAmenityController _controller;

    public PropertyAmenityControllerTests()
    {
        _mockPropertyAmenityService = new Mock<IPropertyAmenityService>();
        _mockLogger = new Mock<ILogger<PropertyAmenityController>>();
        _controller = new PropertyAmenityController(_mockPropertyAmenityService.Object, _mockLogger.Object);
    }

    #region GetMaxPropertyAmenity Tests

    [Fact]
    public async Task GetMaxPropertyAmenity_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("WardId", "WardId is required");
        var request = new MaxPropertyAmenityQueryParameters();

        // Act
        var result = await _controller.GetMaxPropertyAmenity(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetMaxPropertyAmenity_ServiceReturnsFailure_ReturnsBadRequest()
    {
        // Arrange
        var request = new MaxPropertyAmenityQueryParameters { UserId = 1, WardId = 10, PropertyNo = "P-100" };
        var serviceResponse = new ApiResponse<GetMaxPropertyAmenityResponseDto>
        {
            Success = false,
            Message = "Given ward is not allocated to this user."
        };

        _mockPropertyAmenityService
            .Setup(s => s.GetMaxPropertyAmenityAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.GetMaxPropertyAmenity(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(serviceResponse, badRequestResult.Value);
    }

    [Fact]
    public async Task GetMaxPropertyAmenity_ServiceReturnsSuccess_ReturnsOk()
    {
        // Arrange
        var request = new MaxPropertyAmenityQueryParameters { UserId = 1, WardId = 10, PropertyNo = "P-100" };
        var serviceResponse = new ApiResponse<GetMaxPropertyAmenityResponseDto>
        {
            Success = true,
            Message = "Max amenity partition fetched successfully.",
            Items = new GetMaxPropertyAmenityResponseDto { NextAmenityPartition = "AM1" }
        };

        _mockPropertyAmenityService
            .Setup(s => s.GetMaxPropertyAmenityAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.GetMaxPropertyAmenity(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(serviceResponse, okResult.Value);
    }

    [Fact]
    public async Task GetMaxPropertyAmenity_ServiceThrowsException_Returns500InternalServerError()
    {
        // Arrange
        var request = new MaxPropertyAmenityQueryParameters();
        _mockPropertyAmenityService
            .Setup(s => s.GetMaxPropertyAmenityAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act
        var result = await _controller.GetMaxPropertyAmenity(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<GetMaxPropertyAmenityResponseDto>>(objectResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("An unexpected error occurred while fetching max property amenity number.", apiResponse.Message);
    }

    #endregion

    #region GetAllAmenities Tests

    [Fact]
    public async Task GetAllAmenities_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("WardId", "WardId is required");
        var request = new AmenityQueryParameters();

        // Act
        var result = await _controller.GetAllAmenities(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetAllAmenities_ServiceReturnsData_ReturnsOk()
    {
        // Arrange
        var request = new AmenityQueryParameters { UserId = 1, WardId = 10 };
        var pagedResult = new PagedResult<AmenityPropertyDto>(
            new List<AmenityPropertyDto> { new() { Id = 1, PropertyNo = "P-1" } }, 1, 1, 10);

        _mockPropertyAmenityService
            .Setup(s => s.GetAllAmenitiesAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAllAmenities(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(pagedResult, okResult.Value);
    }

    [Fact]
    public async Task GetAllAmenities_ServiceThrowsException_Returns500InternalServerError()
    {
        // Arrange
        var request = new AmenityQueryParameters();
        _mockPropertyAmenityService
            .Setup(s => s.GetAllAmenitiesAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database timeout"));

        // Act
        var result = await _controller.GetAllAmenities(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        var pagedResult = Assert.IsType<PagedResult<AmenityPropertyDto>>(objectResult.Value);
        Assert.Equal(0, pagedResult.TotalCount);
    }

    #endregion

    #region SoftDeleteAmenity Tests

    [Fact]
    public async Task SoftDeleteAmenity_InvalidPropertyId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SoftDeleteAmenity(0, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<bool>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Invalid PropertyId.", response.Message);
    }

    [Fact]
    public async Task SoftDeleteAmenity_ServiceReturnsFalse_ReturnsBadRequest()
    {
        // Arrange
        int propertyId = 99;
        _mockPropertyAmenityService
            .Setup(s => s.DeleteAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.SoftDeleteAmenity(propertyId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<bool>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Failed to delete amenity or it was not found.", response.Message);
    }

    [Fact]
    public async Task SoftDeleteAmenity_ServiceReturnsTrue_ReturnsOk()
    {
        // Arrange
        int propertyId = 10;
        _mockPropertyAmenityService
            .Setup(s => s.DeleteAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SoftDeleteAmenity(propertyId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Amenity soft-deleted successfully.", response.Message);
        Assert.True(response.Items);
    }

    [Fact]
    public async Task SoftDeleteAmenity_ServiceThrowsException_Returns500InternalServerError()
    {
        // Arrange
        int propertyId = 10;
        _mockPropertyAmenityService
            .Setup(s => s.DeleteAsync(propertyId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unhandled exception"));

        // Act
        var result = await _controller.SoftDeleteAmenity(propertyId, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        var response = Assert.IsType<ApiResponse<bool>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("An unexpected error occurred while soft deleting amenity.", response.Message);
    }

    #endregion

    #region UpdateAmenity Tests

    [Fact]
    public async Task UpdateAmenity_InvalidPropertyId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UpdateAmenity(-1, new UpdateAmenityDto(), CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AmenityPropertyDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Invalid PropertyId.", response.Message);
    }

    [Fact]
    public async Task UpdateAmenity_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("TaxZoneId", "TaxZoneId is required");

        // Act
        var result = await _controller.UpdateAmenity(10, new UpdateAmenityDto(), CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateAmenity_ServiceReturnsFailure_ReturnsBadRequest()
    {
        // Arrange
        int propertyId = 10;
        var dto = new UpdateAmenityDto();
        var serviceResponse = new ApiResponse<AmenityPropertyDto>
        {
            Success = false,
            Message = "Property not found."
        };

        _mockPropertyAmenityService
            .Setup(s => s.UpdateAmenityAsync(propertyId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.UpdateAmenity(propertyId, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(serviceResponse, badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateAmenity_ServiceReturnsSuccess_ReturnsOk()
    {
        // Arrange
        int propertyId = 10;
        var dto = new UpdateAmenityDto();
        var serviceResponse = new ApiResponse<AmenityPropertyDto>
        {
            Success = true,
            Message = "Amenity updated successfully.",
            Items = new AmenityPropertyDto { Id = propertyId }
        };

        _mockPropertyAmenityService
            .Setup(s => s.UpdateAmenityAsync(propertyId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.UpdateAmenity(propertyId, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(serviceResponse, okResult.Value);
    }

    [Fact]
    public async Task UpdateAmenity_ServiceThrowsException_Returns500InternalServerError()
    {
        // Arrange
        int propertyId = 10;
        var dto = new UpdateAmenityDto();

        _mockPropertyAmenityService
            .Setup(s => s.UpdateAmenityAsync(propertyId, dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unhandled update error"));

        // Act
        var result = await _controller.UpdateAmenity(propertyId, dto, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        var response = Assert.IsType<ApiResponse<AmenityPropertyDto>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("An unexpected error occurred while updating amenity.", response.Message);
    }

    #endregion
}
