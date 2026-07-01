using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System.Security.Claims;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class CommonDetailsControllerTests
{
    private static CommonDetailsController Create(out Mock<ICommonDetailsService> service)
    {
        service = new Mock<ICommonDetailsService>();
        var fileValidationHelper = new FileValidationHelper(new ConfigurationBuilder().Build());
        return new CommonDetailsController(service.Object, fileValidationHelper);
    }

    // ============== GetMaster Tests ==============

    [Fact]
    public async Task GetMaster_ReturnsOk_WithMasterList()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var masters = new List<BulkUpdateMasterDto>
        {
            new() { Id = 1, UpdateCode = "PROPERTY_BASIC", UpdateName = "Property Basic Details" },
            new() { Id = 2, UpdateCode = "PROPERTY_RATE", UpdateName = "Property Rate" }
        };

        service.Setup(s => s.GetMenuAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(masters);

        var result = await controller.GetMaster(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var response = okResult.Value as ApiResponse<List<BulkUpdateMasterDto>>;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal(2, response.Items?.Count);
    }

    [Fact]
    public async Task GetMaster_ReturnsOk_WithEmptyList()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        service.Setup(s => s.GetMenuAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BulkUpdateMasterDto>());

        var result = await controller.GetMaster(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as ApiResponse<List<BulkUpdateMasterDto>>;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Empty(response.Items!);
    }

    // Note: Authentication is handled by [Authorize] attribute at the ASP.NET pipeline level.
    // In unit tests without the middleware, the controller method executes regardless of auth state.
    // Integration tests should be used to verify authentication behavior.

    // ============== GetFormFields Tests ==============

    [Fact]
    public async Task GetFormFields_ReturnsOk_WithFieldConfigs()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var updateCode = "PROPERTY_BASIC";
        var fields = new List<BulkUpdateFieldConfigDto>
        {
            new() { Id = 1, FieldName = "PlotArea", DisplayName = "Plot Area", IsRequired = true },
            new() { Id = 2, FieldName = "BuiltupArea", DisplayName = "Builtup Area", IsRequired = false }
        };

        service.Setup(s => s.GetFormFieldsAsync(updateCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fields);

        var result = await controller.GetFormFields(updateCode, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as ApiResponse<List<BulkUpdateFieldConfigDto>>;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal(2, response.Items?.Count);
    }

    [Fact]
    public async Task GetFormFields_ReturnsOk_WithEmptyList_WhenNoFieldsFound()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        service.Setup(s => s.GetFormFieldsAsync("INVALID_CODE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BulkUpdateFieldConfigDto>());

        var result = await controller.GetFormFields("INVALID_CODE", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as ApiResponse<List<BulkUpdateFieldConfigDto>>;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Empty(response.Items!);
    }

    // Note: Parameter validation for updateCode is handled declaratively via [Required] attribute.
    // ASP.NET model binding enforces this before the action method is invoked.
    // Authentication is handled by [Authorize] attribute at the pipeline level.
    // Integration tests should verify these behaviors.

    // ============== GetGridColumns Tests ==============

    [Fact]
    public async Task GetGridColumns_ReturnsOk_WithColumnDefinitions()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var updateCode = "PROPERTY_BASIC";
        var columns = new List<PreviewGridColumnDto>
        {
            new() { Key = "wardNo", Label = "Ward No" },
            new() { Key = "propertyNo", Label = "Property No" },
            new() { Key = "plotArea", Label = "Plot Area" }
        };

        service.Setup(s => s.GetGridColumnsAsync(updateCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(columns);

        var result = await controller.GetGridColumns(updateCode, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as ApiResponse<List<PreviewGridColumnDto>>;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal(3, response.Items?.Count);
    }

    // Note: Parameter validation for updateCode is handled declaratively via [Required] attribute.
    // ASP.NET model binding enforces this before the action method is invoked.
    // Authentication is handled by [Authorize] attribute at the pipeline level.
    // Integration tests should verify these behaviors.

    // ============== FilterProperties Tests ==============

    [Fact]
    public async Task FilterProperties_ReturnsOk_WithPagedProperties()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var request = new FilterPropertiesRequestDto
        {
            UpdateCode = "PROPERTY_BASIC",
            WardId = 1,
            PageNumber = 1,
            PageSize = 10
        };

        var properties = new List<PropertyPreviewDto>
        {
            new() { Id = 100, PropertyNo = "001", WardNo = "1", PartitionNo = "" }
        };

        var pagedResult = new PagedResult<PropertyPreviewDto>(properties, 1, 1, 10);

        service.Setup(s => s.FilterPropertiesAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.FilterProperties(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as ApiResponse<PagedResult<PropertyPreviewDto>>;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(1, response.Items.Items.Count());
        Assert.Equal(1, response.Items.TotalCount);
    }

    [Fact]
    public async Task FilterProperties_ReturnsOk_WithPaginationInfo()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var request = new FilterPropertiesRequestDto
        {
            UpdateCode = "PROPERTY_BASIC",
            WardId = 1,
            PageNumber = 2,
            PageSize = 20
        };

        var pagedResult = new PagedResult<PropertyPreviewDto>(new List<PropertyPreviewDto>(), 100, 2, 20);

        service.Setup(s => s.FilterPropertiesAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.FilterProperties(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as ApiResponse<PagedResult<PropertyPreviewDto>>;
        Assert.NotNull(response?.Items);
        Assert.Equal(2, response.Items.PageNumber);
        Assert.Equal(20, response.Items.PageSize);
        Assert.Equal(100, response.Items.TotalCount);
        Assert.Equal(5, response.Items.TotalPages);
    }

    [Fact]
    public async Task FilterProperties_Returns400_WhenServiceThrowsArgumentException()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var request = new FilterPropertiesRequestDto { UpdateCode = "INVALID" };

        service.Setup(s => s.FilterPropertiesAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Update type not found"));

        var result = await controller.FilterProperties(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertyPreviewDto>>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("Update type not found", response.Message);
    }

    // Note: Request null validation and authentication are handled by ASP.NET pipeline.
    // Integration tests should verify these behaviors.

    // ============== Update (BulkUpdate) Tests ==============

    [Fact]
    public async Task Update_ReturnsOk_WithSuccessfulBulkUpdate()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUserWithId(controller, 42);

        var request = new BulkUpdateRequestDto
        {
            UpdateCode = "PROPERTY_BASIC",
            PropertyIds = new List<long> { 100, 101 },
            UpdateData = new Dictionary<string, object?> { { "PlotArea", 500 } }
        };

        var result = new BulkUpdateResultDto
        {
            TotalRequested = 2,
            SuccessCount = 2,
            FailedCount = 0,
            Errors = new List<string>()
        };

        service.Setup(s => s.BulkUpdateAsync(request, 42, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var response = await controller.Update(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = okResult.Value as ApiResponse<BulkUpdateResultDto>;
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal(2, apiResponse.Items?.SuccessCount);
        Assert.Equal(0, apiResponse.Items?.FailedCount);
    }

    [Fact]
    public async Task Update_ReturnsOk_WithPartialFailure()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUserWithId(controller, 42);

        var request = new BulkUpdateRequestDto
        {
            UpdateCode = "PROPERTY_BASIC",
            PropertyIds = new List<long> { 100, 101 },
            UpdateData = new Dictionary<string, object?> { { "PlotArea", 500 } }
        };

        var result = new BulkUpdateResultDto
        {
            TotalRequested = 2,
            SuccessCount = 1,
            FailedCount = 1,
            Errors = new List<string> { "Property 101: Validation failed" }
        };

        service.Setup(s => s.BulkUpdateAsync(request, 42, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var response = await controller.Update(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = okResult.Value as ApiResponse<BulkUpdateResultDto>;
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal(1, apiResponse.Items?.SuccessCount);
        Assert.Equal(1, apiResponse.Items?.FailedCount);
        Assert.Single(apiResponse.Items?.Errors!);
    }

    // Note: Request validation (null, empty UpdateCode, empty PropertyIds, empty UpdateData)
    // is handled declaratively via [Required] and [MinLength] attributes on BulkUpdateRequestDto.
    // ASP.NET model binding enforces these before the action method is invoked.
    // Integration tests should verify these behaviors.

    [Fact]
    public async Task Update_Returns401_WhenUnauthenticated()
    {
        var controller = Create(out _);
        // Empty HttpContext — no identity claims present
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
        };

        var request = new BulkUpdateRequestDto
        {
            UpdateCode = "PROPERTY_BASIC",
            PropertyIds = new List<long> { 100 },
            UpdateData = new Dictionary<string, object?> { { "PlotArea", 500 } }
        };

        var result = await controller.Update(request, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkUpdateResultDto>>(unauthorized.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Update_Returns401_WhenUserIdIsInvalid()
    {
        var controller = Create(out _);
        SetupAuthenticatedUserWithId(controller, -1);

        var request = new BulkUpdateRequestDto
        {
            UpdateCode = "PROPERTY_BASIC",
            PropertyIds = new List<long> { 100 },
            UpdateData = new Dictionary<string, object?> { { "PlotArea", 500 } }
        };

        var result = await controller.Update(request, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkUpdateResultDto>>(unauthorized.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Update_Returns400_WhenServiceThrowsArgumentException()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUserWithId(controller, 42);

        var request = new BulkUpdateRequestDto
        {
            UpdateCode = "INVALID_CODE",
            PropertyIds = new List<long> { 100 },
            UpdateData = new Dictionary<string, object?> { { "PlotArea", 500 } }
        };

        service.Setup(s => s.BulkUpdateAsync(request, 42, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Update type not found"));

        var result = await controller.Update(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkUpdateResultDto>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("Update type not found", response.Message);
    }

    // ============== Helper Methods ==============

    private static void SetupAuthenticatedUser(CommonDetailsController controller)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42") });
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private static void SetupAuthenticatedUserWithId(CommonDetailsController controller, int userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) });
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
