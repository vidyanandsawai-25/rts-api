using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyMergeDetails;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Comprehensive unit tests for <see cref="PropertyMergeController"/> covering all endpoints,
/// response status codes, error handling, edge cases, and cancellation token forwarding.
/// </summary>
public class PropertyMergeControllerTests
{
    private readonly Mock<IPropertyMergeService> _mockPropertyMergeService;
    private readonly Mock<ILogger<PropertyMergeController>> _mockLogger;
    private readonly PropertyMergeController _controller;

    public PropertyMergeControllerTests()
    {
        _mockPropertyMergeService = new Mock<IPropertyMergeService>();
        _mockLogger = new Mock<ILogger<PropertyMergeController>>();
        _controller = new PropertyMergeController(_mockPropertyMergeService.Object, _mockLogger.Object);
    }

    #region Constructor & Attribute Tests

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var controller = new PropertyMergeController(_mockPropertyMergeService.Object, _mockLogger.Object);

        // Assert
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Controller_ShouldHaveExpectedAttributes()
    {
        // Arrange
        var controllerType = typeof(PropertyMergeController);

        // Assert
        controllerType.GetCustomAttribute<ApiControllerAttribute>().Should().NotBeNull();
        controllerType.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();

        var routeAttr = controllerType.GetCustomAttribute<RouteAttribute>();
        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be("api/[controller]");
    }

    #endregion

    #region MergePropertyAsync (POST api/PropertyMerge/merge)

    [Fact]
    public async Task MergePropertyAsync_Success_ReturnsOkWithApiResponse()
    {
        // Arrange
        var createDto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 },
            Latitude = "18.5204",
            Longitude = "73.8567",
            CreatedBy = 10
        };

        var returnedDto = new PropertyMergeDto
        {
            Success = true,
            Message = "Properties merged successfully",
            Data = new List<PropertyMergeDetailDto>
            {
                new() { Id = 1, PropertyNo = "P1", OldPropertyNo = "OP1" }
            }
        };

        _mockPropertyMergeService
            .Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedDto);

        // Act
        var result = await _controller.MergePropertyAsync(createDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Record inserted successfully");
        apiResponse.Items.Should().BeEquivalentTo(returnedDto);
    }

    [Fact]
    public async Task MergePropertyAsync_WhenServiceThrowsGenericException_ReturnsInternalServerError()
    {
        // Arrange
        var createDto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 }
        };

        _mockPropertyMergeService
            .Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failure"));

        // Act
        var result = await _controller.MergePropertyAsync(createDto, CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);

        var apiResponse = objectResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("An error occurred while creating the record");
        apiResponse.Items.Should().BeNull();
    }

    [Fact]
    public async Task MergePropertyAsync_WhenServiceThrowsDuplicateConstraintException_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 }
        };

        _mockPropertyMergeService
            .Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cannot insert duplicate key in object with unique index constraint"));

        // Act
        var result = await _controller.MergePropertyAsync(createDto, CancellationToken.None);

        // Assert
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        var apiResponse = conflictResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;

        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("A record with the same details already exists.");
    }

    [Fact]
    public async Task MergePropertyAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var createDto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 }
        };

        _mockPropertyMergeService
            .Setup(s => s.CreateAsync(createDto, cts.Token))
            .ReturnsAsync(new PropertyMergeDto { Success = true });

        // Act
        await _controller.MergePropertyAsync(createDto, cts.Token);

        // Assert
        _mockPropertyMergeService.Verify(s => s.CreateAsync(createDto, cts.Token), Times.Once);
    }

    #endregion

    #region DemergeProperty (PUT api/PropertyMerge/demerge)

    [Fact]
    public async Task DemergeProperty_Success_ReturnsOkWithApiResponse()
    {
        // Arrange
        var updateDto = new UpdatePropertyMergeDto
        {
            PropertyIds = new List<int> { 10 },
            PropertyOldIds = new List<int> { 100 },
            PropertySide = "New",
            UpdatedBy = 5
        };

        var returnedDto = new PropertyMergeDto
        {
            Success = true,
            Message = "Demerged successfully"
        };

        _mockPropertyMergeService
            .Setup(s => s.UpdateAsync(10, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedDto);

        // Act
        var result = await _controller.DemergeProperty(updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Record updated successfully");
        apiResponse.Items.Should().BeEquivalentTo(returnedDto);
    }

    [Fact]
    public async Task DemergeProperty_WhenServiceReturnsNull_ReturnsOkWithFailedSuccessFlag()
    {
        // Arrange
        var updateDto = new UpdatePropertyMergeDto
        {
            PropertyIds = new List<int> { 999 },
            PropertyOldIds = new List<int> { 100 }
        };

        _mockPropertyMergeService
            .Setup(s => s.UpdateAsync(999, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMergeDto?)null);

        // Act
        var result = await _controller.DemergeProperty(updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;

        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("Record not found for Update ");
        apiResponse.Items.Should().BeNull();
    }

    [Fact]
    public async Task DemergeProperty_WithNullPropertyIds_PassesZeroAsId()
    {
        // Arrange
        var updateDto = new UpdatePropertyMergeDto
        {
            PropertyIds = null,
            PropertyOldIds = new List<int> { 100 }
        };

        _mockPropertyMergeService
            .Setup(s => s.UpdateAsync(0, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyMergeDto { Success = true });

        // Act
        await _controller.DemergeProperty(updateDto, CancellationToken.None);

        // Assert
        _mockPropertyMergeService.Verify(s => s.UpdateAsync(0, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DemergeProperty_WithEmptyPropertyIds_PassesZeroAsId()
    {
        // Arrange
        var updateDto = new UpdatePropertyMergeDto
        {
            PropertyIds = new List<int>(),
            PropertyOldIds = new List<int> { 100 }
        };

        _mockPropertyMergeService
            .Setup(s => s.UpdateAsync(0, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyMergeDto { Success = true });

        // Act
        await _controller.DemergeProperty(updateDto, CancellationToken.None);

        // Assert
        _mockPropertyMergeService.Verify(s => s.UpdateAsync(0, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DemergeProperty_WhenServiceThrowsGenericException_ReturnsInternalServerError()
    {
        // Arrange
        var updateDto = new UpdatePropertyMergeDto
        {
            PropertyIds = new List<int> { 10 },
            PropertyOldIds = new List<int> { 100 }
        };

        _mockPropertyMergeService
            .Setup(s => s.UpdateAsync(10, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database timeout"));

        // Act
        var result = await _controller.DemergeProperty(updateDto, CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);

        var apiResponse = objectResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("An error occurred while updating the record");
        apiResponse.Items.Should().BeNull();
    }

    [Fact]
    public async Task DemergeProperty_WhenServiceThrowsDuplicateConstraintException_ReturnsConflict()
    {
        // Arrange
        var updateDto = new UpdatePropertyMergeDto
        {
            PropertyIds = new List<int> { 10 },
            PropertyOldIds = new List<int> { 100 }
        };

        _mockPropertyMergeService
            .Setup(s => s.UpdateAsync(10, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Violated UNIQUE constraint on demerge record"));

        // Act
        var result = await _controller.DemergeProperty(updateDto, CancellationToken.None);

        // Assert
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        var apiResponse = conflictResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;

        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("A record with the same details already exists.");
    }

    [Fact]
    public async Task DemergeProperty_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var updateDto = new UpdatePropertyMergeDto
        {
            PropertyIds = new List<int> { 10 },
            PropertyOldIds = new List<int> { 100 }
        };

        _mockPropertyMergeService
            .Setup(s => s.UpdateAsync(10, updateDto, cts.Token))
            .ReturnsAsync(new PropertyMergeDto { Success = true });

        // Act
        await _controller.DemergeProperty(updateDto, cts.Token);

        // Assert
        _mockPropertyMergeService.Verify(s => s.UpdateAsync(10, updateDto, cts.Token), Times.Once);
    }

    #endregion

    #region GetPropertyMergeDetailsById (GET api/PropertyMerge/{propertyId}/merge-details)

    [Fact]
    public async Task GetPropertyMergeDetailsById_WhenExists_ReturnsOkResult()
    {
        // Arrange
        int propertyId = 15;
        var expectedDto = new PropertyMergeDto
        {
            Success = true,
            Message = "Success",
            Data = new List<PropertyMergeDetailDto>
            {
                new() { Id = 1, WardId = 2, PropertyNo = "P15", OldPropertyNo = "OP100" }
            }
        };

        _mockPropertyMergeService
            .Setup(s => s.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetPropertyMergeDetailsById(propertyId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedValue = okResult.Value.Should().BeAssignableTo<PropertyMergeDto>().Subject;
        returnedValue.Should().BeEquivalentTo(expectedDto);
    }

    [Fact]
    public async Task GetPropertyMergeDetailsById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        int propertyId = 999;
        _mockPropertyMergeService
            .Setup(s => s.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMergeDto?)null);

        // Act
        var result = await _controller.GetPropertyMergeDetailsById(propertyId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetPropertyMergeDetailsById_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        int propertyId = 15;
        _mockPropertyMergeService
            .Setup(s => s.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database read error"));

        // Act
        var result = await _controller.GetPropertyMergeDetailsById(propertyId, CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);

        var apiResponse = objectResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("An error occurred while processing your request.");
    }

    [Fact]
    public async Task GetPropertyMergeDetailsById_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        int propertyId = 15;

        _mockPropertyMergeService
            .Setup(s => s.GetByIdAsync(propertyId, cts.Token))
            .ReturnsAsync(new PropertyMergeDto { Success = true });

        // Act
        await _controller.GetPropertyMergeDetailsById(propertyId, cts.Token);

        // Assert
        _mockPropertyMergeService.Verify(s => s.GetByIdAsync(propertyId, cts.Token), Times.Once);
    }

    #endregion

    #region GetUnMergePropertyDetailsAsync (GET api/PropertyMerge/unmerge-details)

    [Fact]
    public async Task GetUnMergePropertyDetailsAsync_Success_ReturnsOkResultWithPagedData()
    {
        // Arrange
        var queryParams = new PropertyMergeQueryParameters
        {
            PropertyId = 1,
            PropertyType = "New",
            PageNumber = 1,
            PageSize = 10
        };

        var pagedResult = new PagedResult<PropertyMergeDto>
        {
            Items = new List<PropertyMergeDto>
            {
                new() { Success = true, Message = "Success" }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockPropertyMergeService
            .Setup(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetUnMergePropertyDetailsAsync(queryParams, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedValue = okResult.Value.Should().BeAssignableTo<PagedResult<PropertyMergeDto>>().Subject;
        returnedValue.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task GetUnMergePropertyDetailsAsync_FilterValidationException_ReturnsBadRequest()
    {
        // Arrange
        var queryParams = new PropertyMergeQueryParameters
        {
            PropertyId = 0,
            PropertyType = "New"
        };

        var validationException = new FilterValidationException(
            "Validation failed",
            new Dictionary<string, string> { { "PropertyId", "PropertyId is required." } });

        _mockPropertyMergeService
            .Setup(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ThrowsAsync(validationException);

        // Act
        var result = await _controller.GetUnMergePropertyDetailsAsync(queryParams, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUnMergePropertyDetailsAsync_WhenServiceThrowsGenericException_ReturnsInternalServerError()
    {
        // Arrange
        var queryParams = new PropertyMergeQueryParameters
        {
            PropertyId = 1,
            PropertyType = "New"
        };

        _mockPropertyMergeService
            .Setup(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Query execution error"));

        // Act
        var result = await _controller.GetUnMergePropertyDetailsAsync(queryParams, CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);

        var apiResponse = objectResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("An error occurred while processing your request.");
    }

    [Fact]
    public async Task GetUnMergePropertyDetailsAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var queryParams = new PropertyMergeQueryParameters { PropertyId = 1 };

        _mockPropertyMergeService
            .Setup(s => s.GetAllAsync(queryParams, cts.Token))
            .ReturnsAsync(new PagedResult<PropertyMergeDto>());

        // Act
        await _controller.GetUnMergePropertyDetailsAsync(queryParams, cts.Token);

        // Assert
        _mockPropertyMergeService.Verify(s => s.GetAllAsync(queryParams, cts.Token), Times.Once);
    }

    #endregion

    #region MergeMultiplePropertyAsync (POST api/PropertyMerge/merge-multiple)

    [Fact]
    public async Task MergeMultiplePropertyAsync_Success_ReturnsOkWithApiResponse()
    {
        // Arrange
        var request = new PropertyMergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto>
            {
                new() { PropertyOldId = 101, PropertyId = 1 }
            }
        };

        var serviceResult = new PropertyMergeDto
        {
            Success = true,
            Message = "Multiple properties merged successfully"
        };

        _mockPropertyMergeService
            .Setup(s => s.MergeMultiplePropertyAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.MergeMultiplePropertyAsync(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Multiple properties merged successfully");
        apiResponse.Items.Should().BeEquivalentTo(serviceResult);
    }

    [Fact]
    public async Task MergeMultiplePropertyAsync_FailureResult_ReturnsOkWithFailedApiResponse()
    {
        // Arrange
        var request = new PropertyMergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto>()
        };

        var serviceResult = new PropertyMergeDto
        {
            Success = false,
            Message = "No properties selected for merge"
        };

        _mockPropertyMergeService
            .Setup(s => s.MergeMultiplePropertyAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.MergeMultiplePropertyAsync(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;

        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("No properties selected for merge");
        apiResponse.Items.Should().BeEquivalentTo(serviceResult);
    }

    [Fact]
    public async Task MergeMultiplePropertyAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var request = new PropertyMergeMultipleDto();

        _mockPropertyMergeService
            .Setup(s => s.MergeMultiplePropertyAsync(request, cts.Token))
            .ReturnsAsync(new PropertyMergeDto { Success = true });

        // Act
        await _controller.MergeMultiplePropertyAsync(request, cts.Token);

        // Assert
        _mockPropertyMergeService.Verify(s => s.MergeMultiplePropertyAsync(request, cts.Token), Times.Once);
    }

    #endregion

    #region DemergeMultiplePropertyAsync (POST api/PropertyMerge/demerge-multiple)

    [Fact]
    public async Task DemergeMultiplePropertyAsync_Success_ReturnsOkWithApiResponse()
    {
        // Arrange
        var request = new PropertyDemergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto>
            {
                new() { PropertyOldId = 101, PropertyId = 1 }
            }
        };

        var serviceResult = new PropertyMergeDto
        {
            Success = true,
            Message = "Multiple properties demerged successfully"
        };

        _mockPropertyMergeService
            .Setup(s => s.DemergeMultiplePropertyAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.DemergeMultiplePropertyAsync(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Multiple properties demerged successfully");
        apiResponse.Items.Should().BeEquivalentTo(serviceResult);
    }

    [Fact]
    public async Task DemergeMultiplePropertyAsync_FailureResult_ReturnsOkWithFailedApiResponse()
    {
        // Arrange
        var request = new PropertyDemergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto>()
        };

        var serviceResult = new PropertyMergeDto
        {
            Success = false,
            Message = "No properties provided for demerge"
        };

        _mockPropertyMergeService
            .Setup(s => s.DemergeMultiplePropertyAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.DemergeMultiplePropertyAsync(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<PropertyMergeDto>>().Subject;

        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("No properties provided for demerge");
        apiResponse.Items.Should().BeEquivalentTo(serviceResult);
    }

    [Fact]
    public async Task DemergeMultiplePropertyAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var request = new PropertyDemergeMultipleDto();

        _mockPropertyMergeService
            .Setup(s => s.DemergeMultiplePropertyAsync(request, cts.Token))
            .ReturnsAsync(new PropertyMergeDto { Success = true });

        // Act
        await _controller.DemergeMultiplePropertyAsync(request, cts.Token);

        // Assert
        _mockPropertyMergeService.Verify(s => s.DemergeMultiplePropertyAsync(request, cts.Token), Times.Once);
    }

    #endregion
}
