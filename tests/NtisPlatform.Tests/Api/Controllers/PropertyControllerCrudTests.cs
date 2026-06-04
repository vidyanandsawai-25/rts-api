using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Comprehensive tests for PropertyController CRUD operations
/// Target: 100% line coverage and branch coverage
/// </summary>
public class PropertyControllerCrudTests
{
    private readonly Mock<IPropertyService> _mockPropertyService;
    private readonly Mock<ILogger<PropertyController>> _mockLogger;
    private readonly PropertyController _controller;

    public PropertyControllerCrudTests()
    {
        _mockPropertyService = new Mock<IPropertyService>();
        _mockLogger = new Mock<ILogger<PropertyController>>();
        _controller = PropertyControllerTestHelper.CreateController(_mockPropertyService, _mockLogger);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithValidQuery_ReturnsOk()
    {
        // Arrange
        var query = new PropertyQueryParameters { PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<PropertyDto>
        {
            Items = new List<PropertyDto> { new PropertyDto { Id = 1 } },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockPropertyService.Setup(x => x.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAll(query, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
        _mockPropertyService.Verify(x => x.GetAllAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithEmptyResult_ReturnsOk()
    {
        // Arrange
        var query = new PropertyQueryParameters { PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<PropertyDto>
        {
            Items = new List<PropertyDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _mockPropertyService.Setup(x => x.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAll(query, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResult<PropertyDto>>(okResult.Value);
        Assert.Empty(pagedResult.Items);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingId_ReturnsOk()
    {
        // Arrange
        var id = 1;
        var expectedDto = new PropertyDto { Id = id, PropertyNo = "P001" };

        _mockPropertyService.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedDto, okResult.Value);
    }

    [Fact]
    public async Task GetById_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var id = 999;

        _mockPropertyService.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDto?)null);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsOk()
    {
        // Arrange
        var createDto = new CreatePropertyDto { PropertyNo = "P001", WardId = 1 };
        var expectedDto = new PropertyDto { Id = 1, PropertyNo = "P001" };

        _mockPropertyService.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record inserted successfully", response.Message);
    }

    [Fact]
    public async Task Create_WithDuplicateProperty_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreatePropertyDto { PropertyNo = "P001", WardId = 1 };

        _mockPropertyService.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("duplicate key value violates unique constraint"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDto>>(conflictResult.Value);
        Assert.False(response.Success);
        Assert.Contains("already exists", response.Message);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidDto_ReturnsOk()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdatePropertyDto { PropertyNo = "P001-Updated" };
        var expectedDto = new PropertyDto { Id = id, PropertyNo = "P001-Updated" };

        _mockPropertyService.Setup(x => x.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record updated successfully", response.Message);
    }

    [Fact]
    public async Task Update_WithNonExistingId_ReturnsNotFoundResponse()
    {
        // Arrange
        var id = 999;
        var updateDto = new UpdatePropertyDto { PropertyNo = "P999" };

        _mockPropertyService.Setup(x => x.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDto?)null);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingId_ReturnsOk()
    {
        // Arrange
        var id = 1;

        _mockPropertyService.Setup(x => x.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("marked for deletion", response.Message);
    }

    [Fact]
    public async Task Delete_WithNonExistingId_ReturnsNotFoundResponse()
    {
        // Arrange
        var id = 999;

        _mockPropertyService.Setup(x => x.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message);
    }

    #endregion

    #region CreateFromRange Tests

    [Fact]
    public async Task CreateFromRange_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            RangeFrom = "1",
            RangeTo = "5",
            Template = new CreateNewPropertyDto { WardId = 1 }
        };

        var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
            5,
            0,
            new List<CreateNewPropertyResponseDto>
            {
                new CreateNewPropertyResponseDto { PropertyId = 1 },
                new CreateNewPropertyResponseDto { PropertyId = 2 },
                new CreateNewPropertyResponseDto { PropertyId = 3 },
                new CreateNewPropertyResponseDto { PropertyId = 4 },
                new CreateNewPropertyResponseDto { PropertyId = 5 }
            });

        _mockPropertyService.Setup(x => x.CreatePropertiesFromRangeAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CreateFromRange(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var rangeResult = Assert.IsType<RangeResult<CreateNewPropertyResponseDto>>(okResult.Value);
        Assert.Equal(5, rangeResult.SuccessCount);
        Assert.Equal(0, rangeResult.FailedCount);
    }

    [Fact]
    public async Task CreateFromRange_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            RangeFrom = "1",
            RangeTo = "5",
            Template = new CreateNewPropertyDto { WardId = 1 }
        };

        _mockPropertyService.Setup(x => x.CreatePropertiesFromRangeAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateFromRange(request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);

        var responseValue = statusCodeResult.Value;
        var successProp = responseValue?.GetType().GetProperty("Success")?.GetValue(responseValue);
        var messageProp = responseValue?.GetType().GetProperty("Message")?.GetValue(responseValue);

        Assert.Equal(false, successProp);
        Assert.Equal("An unexpected error occurred while processing your request.", messageProp);
    }

    [Fact]
    public async Task CreateFromRange_WithPartialSuccess_ReturnsOkWithErrors()
    {
        // Arrange
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            RangeFrom = "1",
            RangeTo = "5",
            Template = new CreateNewPropertyDto { WardId = 1 }
        };

        var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
            3,
            2,
            new List<CreateNewPropertyResponseDto>
            {
                new CreateNewPropertyResponseDto { PropertyId = 1 },
                new CreateNewPropertyResponseDto { PropertyId = 2 },
                new CreateNewPropertyResponseDto { PropertyId = 3 }
            },
            new List<string> { "Error creating P004", "Error creating P005" });

        _mockPropertyService.Setup(x => x.CreatePropertiesFromRangeAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CreateFromRange(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var rangeResult = Assert.IsType<RangeResult<CreateNewPropertyResponseDto>>(okResult.Value);
        Assert.Equal(3, rangeResult.SuccessCount);
        Assert.Equal(2, rangeResult.FailedCount);
        Assert.True(rangeResult.HasFailures);
        Assert.NotNull(rangeResult.Errors);
        Assert.Equal(2, rangeResult.Errors.Count);
    }

    [Fact]
    public async Task CreateFromRange_WithArgumentException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            RangeFrom = "10",
            RangeTo = "5", // Invalid range
            Template = new CreateNewPropertyDto { WardId = 1 }
        };

        _mockPropertyService.Setup(x => x.CreatePropertiesFromRangeAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid range"));

        // Act
        var result = await _controller.CreateFromRange(request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion
}
