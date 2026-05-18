using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master.SocialAttributeMaster;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Tests.Api.Controllers.Master;

/// <summary>
/// Comprehensive tests for SocialAttributeController
/// </summary>
public class SocialAttributeControllerTests
{
    private readonly Mock<ISocialAttributeService> _mockService;
    private readonly Mock<ILogger<SocialAttributeController>> _mockLogger;
    private readonly SocialAttributeController _controller;

    public SocialAttributeControllerTests()
    {
        _mockService = new Mock<ISocialAttributeService>();
        _mockLogger = new Mock<ILogger<SocialAttributeController>>();
        _controller = new SocialAttributeController(_mockLogger.Object, _mockService.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithValidQueryParameters_ReturnsOkResult()
    {
        // Arrange
        var queryParameters = new SocialAttributeMasterQueryParameters();
        var pagedResult = new PagedResult<SocialAttributeDto>
        {
            Items = new List<SocialAttributeDto>
            {
                new() { Id = 1, SocialAttributeCode = "SA001", SocialAttributeName = "Test Attribute" }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<SocialAttributeDto>>(okResult.Value);
        Assert.Single(returnedResult.Items);
        _mockService.Verify(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithEmptyResult_ReturnsOkResultWithEmptyList()
    {
        // Arrange
        var queryParameters = new SocialAttributeMasterQueryParameters();
        var pagedResult = new PagedResult<SocialAttributeDto>
        {
            Items = new List<SocialAttributeDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAll_WithFilterParameters_ReturnsFilteredResults()
    {
        // Arrange
        var queryParameters = new SocialAttributeMasterQueryParameters
        {
            SocialAttributeCode = "SA001",
            IsActive = true
        };
        var pagedResult = new PagedResult<SocialAttributeDto>
        {
            Items = new List<SocialAttributeDto>
            {
                new() { Id = 1, SocialAttributeCode = "SA001", SocialAttributeName = "Filtered Attribute" }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<SocialAttributeDto>>(okResult.Value);
        Assert.Single(returnedResult.Items);
    }

    [Fact]
    public async Task GetAll_WithFilterValidationException_ReturnsBadRequest()
    {
        // Arrange
        var queryParameters = new SocialAttributeMasterQueryParameters();
        var errors = new Dictionary<string, string> { { "SocialAttributeCode", "Invalid filter parameter" } };

        _mockService.Setup(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FilterValidationException("Filter validation failed", errors));

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);

        var responseType = badRequestResult.Value.GetType();
        var messageProperty = responseType.GetProperty("message");
        Assert.NotNull(messageProperty);
        var messageValue = messageProperty.GetValue(badRequestResult.Value) as string;
        Assert.Equal("Filter validation failed", messageValue);
    }

    [Fact]
    public async Task GetAll_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var queryParameters = new SocialAttributeMasterQueryParameters();

        _mockService.Setup(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<SocialAttributeDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("error occurred", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var id = 1;
        var dto = new SocialAttributeDto
        {
            Id = id,
            SocialAttributeCode = "SA001",
            SocialAttributeName = "Test Attribute",
            DataType = "Boolean"
        };

        _mockService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<SocialAttributeDto>(okResult.Value);
        Assert.Equal(id, returnedDto.Id);
        Assert.Equal("SA001", returnedDto.SocialAttributeCode);
        _mockService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var id = 999;

        _mockService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialAttributeDto?)null);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _mockService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var id = 1;

        _mockService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<SocialAttributeDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsOkResult()
    {
        // Arrange
        var createDto = new CreateSocialAttributeDto
        {
            SocialAttributeCode = "SA001",
            SocialAttributeName = "New Attribute",
            DataType = "Boolean",
            IsDiscountApplicable = true
        };
        var resultDto = new SocialAttributeDto
        {
            Id = 1,
            SocialAttributeCode = "SA001",
            SocialAttributeName = "New Attribute",
            DataType = "Boolean",
            IsDiscountApplicable = true
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SocialAttributeDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("inserted successfully", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(1, apiResponse.Items.Id);
        _mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithParentAttributeId_ReturnsOkResult()
    {
        // Arrange
        var createDto = new CreateSocialAttributeDto
        {
            SocialAttributeCode = "SA002",
            SocialAttributeName = "Child Attribute",
            DataType = "Text",
            ParentAttributeId = 1,
            IsRequiredWhenParentTrue = true
        };
        var resultDto = new SocialAttributeDto
        {
            Id = 2,
            SocialAttributeCode = "SA002",
            SocialAttributeName = "Child Attribute",
            DataType = "Text",
            ParentAttributeId = 1,
            IsRequiredWhenParentTrue = true
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SocialAttributeDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(1, apiResponse.Items.ParentAttributeId);
    }

    [Fact]
    public async Task Create_WithDuplicateConstraint_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreateSocialAttributeDto
        {
            SocialAttributeCode = "SA001",
            SocialAttributeName = "Duplicate Attribute",
            DataType = "Boolean"
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Duplicate key constraint violation"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SocialAttributeDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("already exists", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var createDto = new CreateSocialAttributeDto
        {
            SocialAttributeCode = "SA001",
            SocialAttributeName = "New Attribute",
            DataType = "Boolean"
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database insert failed"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<SocialAttributeDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("error occurred", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidIdAndDto_ReturnsOkResult()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateSocialAttributeDto
        {
            SocialAttributeCode = "SA001_UPDATED",
            SocialAttributeName = "Updated Attribute",
            DataType = "Text"
        };
        var resultDto = new SocialAttributeDto
        {
            Id = id,
            SocialAttributeCode = "SA001_UPDATED",
            SocialAttributeName = "Updated Attribute",
            DataType = "Text"
        };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SocialAttributeDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("updated successfully", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(id, apiResponse.Items.Id);
        _mockService.Verify(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithNonExistentId_ReturnsOkWithFailure()
    {
        // Arrange
        var id = 999;
        var updateDto = new UpdateSocialAttributeDto
        {
            SocialAttributeCode = "SA001",
            SocialAttributeName = "Updated Attribute",
            DataType = "Boolean"
        };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialAttributeDto?)null);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SocialAttributeDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("not found", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(apiResponse.Items);
    }

    [Fact]
    public async Task Update_WithDuplicateConstraint_ReturnsConflict()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateSocialAttributeDto
        {
            SocialAttributeCode = "SA001",
            SocialAttributeName = "Duplicate Attribute",
            DataType = "Boolean"
        };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Duplicate constraint violation"));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SocialAttributeDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("already exists", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateSocialAttributeDto
        {
            SocialAttributeCode = "SA001",
            SocialAttributeName = "Updated Attribute",
            DataType = "Boolean"
        };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database update failed"));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<SocialAttributeDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("error occurred", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var id = 1;

        _mockService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SocialAttributeDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("marked for deletion", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        _mockService.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ReturnsOkWithFailure()
    {
        // Arrange
        var id = 999;

        _mockService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SocialAttributeDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("not found", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        _mockService.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var id = 1;

        _mockService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database delete failed"));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<SocialAttributeDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("error occurred", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange
        var mockService = new Mock<ISocialAttributeService>();
        var mockLogger = new Mock<ILogger<SocialAttributeController>>();

        // Act
        var controller = new SocialAttributeController(mockLogger.Object, mockService.Object);

        // Assert
        Assert.NotNull(controller);
    }

    #endregion

    #region Service Interaction Verification Tests

    [Fact]
    public async Task GetAll_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var queryParameters = new SocialAttributeMasterQueryParameters
        {
            SocialAttributeCode = "SA001",
            SocialAttributeName = "Test"
        };
        var pagedResult = new PagedResult<SocialAttributeDto>
        {
            Items = new List<SocialAttributeDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_CallsServiceWithCorrectId()
    {
        // Arrange
        var id = 5;
        var dto = new SocialAttributeDto { Id = id };

        _mockService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        await _controller.GetById(id, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_CallsServiceWithCorrectDto()
    {
        // Arrange
        var createDto = new CreateSocialAttributeDto
        {
            SocialAttributeCode = "SA001",
            SocialAttributeName = "Test",
            DataType = "Boolean"
        };
        var resultDto = new SocialAttributeDto { Id = 1 };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        await _controller.Create(createDto, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_CallsServiceWithCorrectIdAndDto()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateSocialAttributeDto
        {
            SocialAttributeCode = "SA001",
            SocialAttributeName = "Updated",
            DataType = "Text"
        };
        var resultDto = new SocialAttributeDto { Id = id };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_CallsServiceWithCorrectId()
    {
        // Arrange
        var id = 1;

        _mockService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _controller.Delete(id, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
