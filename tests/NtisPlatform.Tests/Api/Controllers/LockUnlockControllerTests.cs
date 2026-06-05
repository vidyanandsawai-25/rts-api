using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.LockUnlock;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;

namespace NtisPlatform.Tests.Api.Controllers;

public class LockUnlockControllerTests
{
    private readonly Mock<ILockUnlockService> _mockService;
    private readonly Mock<ILogger<LockUnlockController>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly LockUnlockController _controller;

    public LockUnlockControllerTests()
    {
        _mockService = new Mock<ILockUnlockService>();
        _mockLogger = new Mock<ILogger<LockUnlockController>>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");

        _controller = new LockUnlockController(
            _mockService.Object,
            _mockLogger.Object,
            _mockEnvironment.Object);

        SetupAuthenticatedUser(1);
    }

    private void SetupAuthenticatedUser(int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private void SetupUnauthenticatedUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GetScreens Tests

    [Fact]
    public async Task GetScreens_ReturnsOkResult_WithScreensList()
    {
        // Arrange
        var screens = new List<LockableScreenDto>
        {
            new() { Id = 1, ScreenCode = "SCR001", ScreenName = "Screen 1" },
            new() { Id = 2, ScreenCode = "SCR002", ScreenName = "Screen 2" }
        };
        _mockService.Setup(s => s.GetLockableScreensAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(screens);

        // Act
        var result = await _controller.GetScreens(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<List<LockableScreenDto>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(2, apiResponse.Items.Count);
    }

    [Fact]
    public async Task GetScreens_ReturnsOkResult_WithEmptyList_WhenNoScreens()
    {
        // Arrange
        _mockService.Setup(s => s.GetLockableScreensAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LockableScreenDto>());

        // Act
        var result = await _controller.GetScreens(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<List<LockableScreenDto>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Items);
        Assert.Empty(apiResponse.Items);
    }

    [Fact]
    public async Task GetScreens_ReturnsUnauthorized_WhenUnauthorizedAccessException()
    {
        // Arrange
        _mockService.Setup(s => s.GetLockableScreensAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Not authorized"));

        // Act
        var result = await _controller.GetScreens(CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.CorrelationId);
    }

    [Fact]
    public async Task GetScreens_Returns500_WhenExceptionOccurs()
    {
        // Arrange
        _mockService.Setup(s => s.GetLockableScreensAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetScreens(CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(statusCodeResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Database error", response.Message); // Development mode shows error
    }

    [Fact]
    public async Task GetScreens_HidesErrorDetails_InProduction()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var controller = new LockUnlockController(
            _mockService.Object,
            _mockLogger.Object,
            _mockEnvironment.Object);
        controller.ControllerContext = _controller.ControllerContext;

        _mockService.Setup(s => s.GetLockableScreensAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Sensitive error"));

        // Act
        var result = await controller.GetScreens(CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(statusCodeResult.Value);
        Assert.Equal("An error occurred", response.Message);
        Assert.DoesNotContain("Sensitive error", response.Message);
    }

    #endregion

    #region GetProperties Tests

    [Fact]
    public async Task GetProperties_ReturnsOkResult_WithPagedResult()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto { WardId = 1, PageNumber = 1, PageSize = 10 };
        var items = new List<PropertyLockRowDto>
        {
            new() { PropertyId = 1, PropertyNo = "P001", WardNo = "W01" },
            new() { PropertyId = 2, PropertyNo = "P002", WardNo = "W01" }
        };
        var pagedResult = new PagedResult<PropertyLockRowDto>(items, 2, 1, 10);

        _mockService.Setup(s => s.GetPropertyLocksAsync(It.IsAny<FilterPropertyLocksRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetProperties(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PagedResult<PropertyLockRowDto>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(2, apiResponse.Items.TotalCount);
        Assert.Equal(2, apiResponse.Items.Items.Count());
    }

    [Fact]
    public async Task GetProperties_ReturnsBadRequest_WhenArgumentException()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto { WardId = 0 };
        _mockService.Setup(s => s.GetPropertyLocksAsync(It.IsAny<FilterPropertyLocksRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("WardId is required."));

        // Act
        var result = await _controller.GetProperties(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("WardId is required.", response.Message);
    }

    [Fact]
    public async Task GetProperties_ReturnsUnauthorized_WhenUnauthorizedAccessException()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto { WardId = 1 };
        _mockService.Setup(s => s.GetPropertyLocksAsync(It.IsAny<FilterPropertyLocksRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetProperties(request, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetProperties_Returns500_WhenExceptionOccurs()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto { WardId = 1 };
        _mockService.Setup(s => s.GetPropertyLocksAsync(It.IsAny<FilterPropertyLocksRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetProperties(request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetProperties_PassesRequestToService()
    {
        // Arrange
        var request = new FilterPropertyLocksRequestDto 
        { 
            WardId = 5, 
            FromPropertyNo = "P001", 
            ToPropertyNo = "P100",
            Search = "test",
            PageNumber = 2,
            PageSize = 20
        };
        var pagedResult = new PagedResult<PropertyLockRowDto>(new List<PropertyLockRowDto>(), 0, 2, 20);

        FilterPropertyLocksRequestDto? capturedRequest = null;
        _mockService.Setup(s => s.GetPropertyLocksAsync(It.IsAny<FilterPropertyLocksRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<FilterPropertyLocksRequestDto, CancellationToken>((r, ct) => capturedRequest = r)
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.GetProperties(request, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(5, capturedRequest.WardId);
        Assert.Equal("P001", capturedRequest.FromPropertyNo);
        Assert.Equal("P100", capturedRequest.ToPropertyNo);
        Assert.Equal("test", capturedRequest.Search);
    }

    #endregion

    #region Bulk Tests

    [Fact]
    public async Task Bulk_ReturnsOkResult_WithBulkLockResult()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1, 2 },
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };
        var bulkResult = new BulkLockResultDto
        {
            TotalRequested = 2,
            SuccessCount = 2,
            FailedCount = 0
        };

        _mockService.Setup(s => s.BulkApplyAsync(It.IsAny<BulkLockRequestDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.Bulk(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<BulkLockResultDto>(okResult.Value);
        Assert.Equal(2, returnedResult.TotalRequested);
        Assert.Equal(2, returnedResult.SuccessCount);
        Assert.Equal(0, returnedResult.FailedCount);
    }

    [Fact]
    public async Task Bulk_PassesUserIdToService()
    {
        // Arrange
        SetupAuthenticatedUser(123);
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };
        var bulkResult = new BulkLockResultDto { TotalRequested = 1, SuccessCount = 1 };

        int capturedUserId = 0;
        _mockService.Setup(s => s.BulkApplyAsync(It.IsAny<BulkLockRequestDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<BulkLockRequestDto, int, CancellationToken>((r, userId, ct) => capturedUserId = userId)
            .ReturnsAsync(bulkResult);

        // Act
        await _controller.Bulk(request, CancellationToken.None);

        // Assert
        Assert.Equal(123, capturedUserId);
    }

    [Fact]
    public async Task Bulk_ReturnsUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };

        // Act
        var result = await _controller.Bulk(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.Equal("Valid user identification is required.", response.Message);
    }

    [Fact]
    public async Task Bulk_ReturnsUnauthorized_WhenUserIdIsInvalid()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "invalid")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };

        // Act
        var result = await _controller.Bulk(request, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Bulk_ReturnsUnauthorized_WhenUserIdIsZero()
    {
        // Arrange
        SetupAuthenticatedUser(0);
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };

        // Act
        var result = await _controller.Bulk(request, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Bulk_ReturnsUnauthorized_WhenUserIdIsNegative()
    {
        // Arrange
        SetupAuthenticatedUser(-1);
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };

        // Act
        var result = await _controller.Bulk(request, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Bulk_ReturnsBadRequest_WhenArgumentException()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int>(),
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };
        _mockService.Setup(s => s.BulkApplyAsync(It.IsAny<BulkLockRequestDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("At least one property must be selected."));

        // Act
        var result = await _controller.Bulk(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("At least one property must be selected.", response.Message);
    }

    [Fact]
    public async Task Bulk_Returns500_WhenExceptionOccurs()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };
        _mockService.Setup(s => s.BulkApplyAsync(It.IsAny<BulkLockRequestDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Bulk(request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Bulk_LogsWarning_WhenValidationErrorOccurs()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int>(),
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };
        _mockService.Setup(s => s.BulkApplyAsync(It.IsAny<BulkLockRequestDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Validation error"));

        // Act
        await _controller.Bulk(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task Bulk_LogsError_WhenExceptionOccurs()
    {
        // Arrange
        var request = new BulkLockRequestDto
        {
            PropertyIds = new List<int> { 1 },
            ScreenIds = new List<int> { 1 },
            Action = "lock"
        };
        _mockService.Setup(s => s.BulkApplyAsync(It.IsAny<BulkLockRequestDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        await _controller.Bulk(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    #endregion

    #region Controller Configuration Tests

    [Fact]
    public void Controller_HasApiControllerAttribute()
    {
        // Assert
        var attribute = typeof(LockUnlockController).GetCustomAttributes(typeof(ApiControllerAttribute), true);
        Assert.Single(attribute);
    }

    [Fact]
    public void Controller_HasRouteAttribute()
    {
        // Assert
        var attribute = typeof(LockUnlockController).GetCustomAttributes(typeof(RouteAttribute), true);
        Assert.Single(attribute);
        var routeAttr = attribute[0] as RouteAttribute;
        Assert.Equal("api/[controller]", routeAttr?.Template);
    }

    [Fact]
    public void Controller_HasAuthorizeAttribute()
    {
        // Assert
        var attribute = typeof(LockUnlockController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);
        Assert.Single(attribute);
    }

    [Fact]
    public void GetScreens_HasHttpGetAttribute()
    {
        // Assert
        var method = typeof(LockUnlockController).GetMethod("GetScreens");
        var attribute = method?.GetCustomAttributes(typeof(HttpGetAttribute), true);
        Assert.NotNull(attribute);
        Assert.Single(attribute);
        var httpGetAttr = attribute[0] as HttpGetAttribute;
        Assert.Equal("screens", httpGetAttr?.Template);
    }

    [Fact]
    public void GetProperties_HasHttpGetAttribute()
    {
        // Assert
        var method = typeof(LockUnlockController).GetMethod("GetProperties");
        var attribute = method?.GetCustomAttributes(typeof(HttpGetAttribute), true);
        Assert.NotNull(attribute);
        Assert.Single(attribute);
        var httpGetAttr = attribute[0] as HttpGetAttribute;
        Assert.Equal("properties", httpGetAttr?.Template);
    }

    [Fact]
    public void Bulk_HasHttpPostAttribute()
    {
        // Assert
        var method = typeof(LockUnlockController).GetMethod("Bulk");
        var attribute = method?.GetCustomAttributes(typeof(HttpPostAttribute), true);
        Assert.NotNull(attribute);
        Assert.Single(attribute);
        var httpPostAttr = attribute[0] as HttpPostAttribute;
        Assert.Equal("bulk", httpPostAttr?.Template);
    }

    #endregion

    #region Integration-Style Tests

    [Fact]
    public async Task FullLockWorkflow_Success()
    {
        // Arrange
        var screens = new List<LockableScreenDto>
        {
            new() { Id = 1, ScreenCode = "SCR001", ScreenName = "Screen 1" }
        };
        _mockService.Setup(s => s.GetLockableScreensAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(screens);

        var properties = new List<PropertyLockRowDto>
        {
            new() { PropertyId = 1, PropertyNo = "P001", IsLocked = false }
        };
        var pagedResult = new PagedResult<PropertyLockRowDto>(properties, 1, 1, 10);
        _mockService.Setup(s => s.GetPropertyLocksAsync(It.IsAny<FilterPropertyLocksRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var bulkResult = new BulkLockResultDto { TotalRequested = 1, SuccessCount = 1 };
        _mockService.Setup(s => s.BulkApplyAsync(It.IsAny<BulkLockRequestDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act - Get screens
        var screensResult = await _controller.GetScreens(CancellationToken.None);
        Assert.IsType<OkObjectResult>(screensResult);

        // Act - Get properties
        var propertiesResult = await _controller.GetProperties(
            new FilterPropertyLocksRequestDto { WardId = 1 }, 
            CancellationToken.None);
        Assert.IsType<OkObjectResult>(propertiesResult);

        // Act - Lock property
        var lockResult = await _controller.Bulk(
            new BulkLockRequestDto { PropertyIds = new List<int> { 1 }, ScreenIds = new List<int> { 1 }, Action = "lock" },
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(lockResult);
        var result = Assert.IsType<BulkLockResultDto>(okResult.Value);
        Assert.Equal(1, result.SuccessCount);
    }

    #endregion
}
