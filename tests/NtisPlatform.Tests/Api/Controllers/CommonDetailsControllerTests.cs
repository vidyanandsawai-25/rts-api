using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Enums;
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
            UpdateCode = ["PROPERTY_BASIC"],
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
            UpdateCode = ["PROPERTY_BASIC"],
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

        var request = new FilterPropertiesRequestDto { UpdateCode = ["INVALID"] };

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

    // ============== FilterPropertiesByCategory Tests ==============

    [Fact]
    public async Task FilterPropertiesByCategory_ReturnsOk_WithPagedProperties()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var request = new FilterPropertiesByCategoryRequestDto
        {
            UpdateCode = ["PROPERTY_BASIC"],
            SearchCategory = PropertySearchCategory.WardWise,
            WardId = 1,
            PageNumber = 1,
            PageSize = 10
        };

        var properties = new List<PropertyPreviewDto>
        {
            new() { Id = 100, PropertyNo = "001", WardNo = "1", PartitionNo = "" }
        };

        var pagedResult = new PagedResult<PropertyPreviewDto>(properties, 1, 1, 10);

        service.Setup(s => s.FilterPropertiesByCategoryAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.FilterPropertiesByCategory(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as ApiResponse<PagedResult<PropertyPreviewDto>>;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(1, response.Items.Items.Count());
        Assert.Equal(1, response.Items.TotalCount);
        Assert.Equal("1 properties found", response.Message);
    }

    [Fact]
    public async Task FilterPropertiesByCategory_Returns400_WhenServiceThrowsPropertyValidationException()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var request = new FilterPropertiesByCategoryRequestDto
        {
            UpdateCode = ["PROPERTY_BASIC"],
            SearchCategory = PropertySearchCategory.ZoneWise
        };

        service.Setup(s => s.FilterPropertiesByCategoryAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PropertyValidationException("ZoneId is required for Zone-wise search"));

        var result = await controller.FilterPropertiesByCategory(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertyPreviewDto>>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("ZoneId is required for Zone-wise search", response.Message);
    }

    [Fact]
    public async Task FilterPropertiesByCategory_Returns400_WhenServiceThrowsArgumentException()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var request = new FilterPropertiesByCategoryRequestDto
        {
            UpdateCode = ["INVALID"],
            SearchCategory = PropertySearchCategory.WardWise,
            WardId = 1
        };

        service.Setup(s => s.FilterPropertiesByCategoryAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Update type not found"));

        var result = await controller.FilterPropertiesByCategory(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertyPreviewDto>>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("Update type not found", response.Message);
    }
 
    // ============== Update (BulkUpdate) Tests ==============

    [Fact]
    public async Task Update_ReturnsOk_WithSuccessfulBulkUpdate()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUserWithId(controller, 42);

        var requests = new List<BulkUpdateRequestDto>
        {
            new()
            {
                UpdateCode = "PROPERTY_BASIC",
                PropertyIds = new List<long> { 100, 101 },
                UpdateData = new Dictionary<string, object?> { { "PlotArea", 500 } }
            }
        };

        var result = new BulkUpdateResultDto
        {
            UpdateCode = "PROPERTY_BASIC",
            TotalRequested = 2,
            SuccessCount = 2,
            FailedCount = 0,
            Errors = new List<string>()
        };

        service.Setup(s => s.BulkUpdateBatchAsync(requests, 42, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BulkUpdateResultDto> { result });

        var response = await controller.Update(requests, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = okResult.Value as ApiResponse<List<BulkUpdateResultDto>>;
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Single(apiResponse.Items!);
        Assert.Equal(2, apiResponse.Items![0].SuccessCount);
        Assert.Equal(0, apiResponse.Items![0].FailedCount);
    }

    [Fact]
    public async Task Update_ReturnsOk_WithPartialFailure()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUserWithId(controller, 42);

        var requests = new List<BulkUpdateRequestDto>
        {
            new()
            {
                UpdateCode = "PROPERTY_BASIC",
                PropertyIds = new List<long> { 100, 101 },
                UpdateData = new Dictionary<string, object?> { { "PlotArea", 500 } }
            }
        };

        var result = new BulkUpdateResultDto
        {
            UpdateCode = "PROPERTY_BASIC",
            TotalRequested = 2,
            SuccessCount = 1,
            FailedCount = 1,
            Errors = new List<string> { "Property 101: Validation failed" }
        };

        service.Setup(s => s.BulkUpdateBatchAsync(requests, 42, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BulkUpdateResultDto> { result });

        var response = await controller.Update(requests, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = okResult.Value as ApiResponse<List<BulkUpdateResultDto>>;
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal(1, apiResponse.Items![0].SuccessCount);
        Assert.Equal(1, apiResponse.Items![0].FailedCount);
        Assert.NotNull(apiResponse.Errors);
        Assert.Single(apiResponse.Errors!);
    }

    [Fact]
    public async Task Update_ReturnsOk_WithMixedBatchResults()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUserWithId(controller, 42);

        var requests = new List<BulkUpdateRequestDto>
        {
            new() { UpdateCode = "GOOD_CODE", PropertyIds = new List<long> { 1 }, UpdateData = new() { { "OwnerName", "X" } } },
            new() { UpdateCode = "BAD_CODE", PropertyIds = new List<long> { 2 }, UpdateData = new() { { "OwnerName", "Y" } } },
        };

        var results = new List<BulkUpdateResultDto>
        {
            new() { UpdateCode = "GOOD_CODE", TotalRequested = 1, SuccessCount = 1, FailedCount = 0, Errors = new List<string>() },
            new() { UpdateCode = "BAD_CODE", TotalRequested = 1, SuccessCount = 0, FailedCount = 1, Errors = new List<string> { "Update type not found" } },
        };

        service.Setup(s => s.BulkUpdateBatchAsync(requests, 42, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        var response = await controller.Update(requests, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = okResult.Value as ApiResponse<List<BulkUpdateResultDto>>;
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal(2, apiResponse.Items!.Count);
        Assert.NotNull(apiResponse.Errors);
        Assert.NotEmpty(apiResponse.Errors!);
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

        var requests = new List<BulkUpdateRequestDto>
        {
            new()
            {
                UpdateCode = "PROPERTY_BASIC",
                PropertyIds = new List<long> { 100 },
                UpdateData = new Dictionary<string, object?> { { "PlotArea", 500 } }
            }
        };

        var result = await controller.Update(requests, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<BulkUpdateResultDto>>>(unauthorized.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Update_Returns401_WhenUserIdIsInvalid()
    {
        var controller = Create(out _);
        SetupAuthenticatedUserWithId(controller, -1);

        var requests = new List<BulkUpdateRequestDto>
        {
            new()
            {
                UpdateCode = "PROPERTY_BASIC",
                PropertyIds = new List<long> { 100 },
                UpdateData = new Dictionary<string, object?> { { "PlotArea", 500 } }
            }
        };

        var result = await controller.Update(requests, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<BulkUpdateResultDto>>>(unauthorized.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Update_Returns400_WhenRequestListIsEmpty()
    {
        var controller = Create(out _);
        SetupAuthenticatedUserWithId(controller, 42);

        var result = await controller.Update(new List<BulkUpdateRequestDto>(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<BulkUpdateResultDto>>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("At least one update item is required.", response.Message);
    }

    // ============== ExportExcel Tests ==============

    [Fact]
    public async Task ExportExcel_ReturnsFile_WithXlsxContentType()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var request = new ExportPropertiesRequestDto { UpdateCode = "UPDATE_ADDRESS", WardId = 1 };
        var bytes = new byte[] { 1, 2, 3, 4 };

        service.Setup(s => s.ExportPropertiesToExcelAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        var result = await controller.ExportExcel(request, CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.Equal(bytes, fileResult.FileContents);
        Assert.StartsWith("UPDATE_ADDRESS_", fileResult.FileDownloadName);
        Assert.EndsWith(".xlsx", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task ExportExcel_Returns400_WhenServiceThrowsArgumentException()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var request = new ExportPropertiesRequestDto { UpdateCode = "INVALID", WardId = 1 };

        service.Setup(s => s.ExportPropertiesToExcelAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Update type not found"));

        var result = await controller.ExportExcel(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("Update type not found", response.Message);
    }

    // ============== ImportExcel Tests ==============

    [Fact]
    public async Task ImportExcel_ReturnsOk_WhenAllRowsSucceed()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUserWithId(controller, 42);

        var form = new ExcelImportFormDto
        {
            UpdateCode = "UPDATE_ADDRESS",
            File = MakeFile("data.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet").Object
        };

        var result = new BulkUpdateResultDto { TotalRequested = 3, SuccessCount = 3, FailedCount = 0 };
        service.Setup(s => s.ImportPropertiesFromExcelAsync(
                "UPDATE_ADDRESS", It.IsAny<Stream>(), 42, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var response = await controller.ImportExcel(form, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<BulkUpdateResultDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(3, apiResponse.Items?.SuccessCount);
    }

    [Fact]
    public async Task ImportExcel_ReturnsOk_ResultFailure_WhenRowsFail()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUserWithId(controller, 42);

        var form = new ExcelImportFormDto
        {
            UpdateCode = "UPDATE_ADDRESS",
            File = MakeFile("data.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet").Object
        };

        var result = new BulkUpdateResultDto
        {
            TotalRequested = 2,
            SuccessCount = 0,
            FailedCount = 1,
            Errors = new List<string> { "Row 2: no property found for wardNo='MM11', propertyNo='10', partitionNo=''." }
        };
        service.Setup(s => s.ImportPropertiesFromExcelAsync(
                "UPDATE_ADDRESS", It.IsAny<Stream>(), 42, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var response = await controller.ImportExcel(form, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var apiResponse = Assert.IsType<ApiResponse<BulkUpdateResultDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal(1, apiResponse.Items?.FailedCount);
        Assert.NotNull(apiResponse.Errors);
        Assert.Single(apiResponse.Errors!);
    }

    [Fact]
    public async Task ImportExcel_Returns400_WhenFileIsEmpty()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUserWithId(controller, 42);

        var form = new ExcelImportFormDto
        {
            UpdateCode = "UPDATE_ADDRESS",
            File = MakeFile("data.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", length: 0).Object
        };

        var result = await controller.ImportExcel(form, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkUpdateResultDto>>(badRequest.Value);
        Assert.False(response.Success);
        service.Verify(s => s.ImportPropertiesFromExcelAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportExcel_Returns400_WhenInvalidFileType()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUserWithId(controller, 42);

        var form = new ExcelImportFormDto
        {
            UpdateCode = "UPDATE_ADDRESS",
            File = MakeFile("report.exe", "application/x-msdownload").Object
        };

        var result = await controller.ImportExcel(form, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkUpdateResultDto>>(badRequest.Value);
        Assert.False(response.Success);
        service.Verify(s => s.ImportPropertiesFromExcelAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportExcel_Returns401_WhenUnauthenticated()
    {
        var controller = Create(out _);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var form = new ExcelImportFormDto
        {
            UpdateCode = "UPDATE_ADDRESS",
            File = MakeFile("data.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet").Object
        };

        var result = await controller.ImportExcel(form, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkUpdateResultDto>>(unauthorized.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ImportExcel_Returns400_WhenServiceThrowsArgumentException()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUserWithId(controller, 42);

        var form = new ExcelImportFormDto
        {
            UpdateCode = "UPDATE_ADDRESS",
            File = MakeFile("data.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet").Object
        };

        service.Setup(s => s.ImportPropertiesFromExcelAsync(
                "UPDATE_ADDRESS", It.IsAny<Stream>(), 42, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Update type not found"));

        var result = await controller.ImportExcel(form, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkUpdateResultDto>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("Update type not found", response.Message);
    }

    // ============== ImportExcelValidate Tests ==============

    [Fact]
    public async Task ImportExcelValidate_ReturnsOk_WhenServiceSucceeds()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var form = new ExcelImportFormDto
        {
            UpdateCode = "UPDATE_ADDRESS",
            File = MakeFile("data.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet").Object
        };

        var validationResult = new ExcelValidationResultDto
        {
            Columns = ["wardNo", "propertyNo", "partitionNo", "ValidationRemark"],
            Rows = [new Dictionary<string, object?> { ["wardNo"] = "MM11", ["ValidationRemark"] = "No property found" }],
            TotalRows = 2,
            FlaggedRowCount = 1
        };
        service.Setup(s => s.ValidateImportExcelAsync("UPDATE_ADDRESS", It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var result = await controller.ImportExcelValidate(form, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<ExcelValidationResultDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal(1, apiResponse.Items?.FlaggedRowCount);
        Assert.Single(apiResponse.Items!.Rows);
    }

    [Fact]
    public async Task ImportExcelValidate_Returns400_WhenFileIsEmpty()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var form = new ExcelImportFormDto
        {
            UpdateCode = "UPDATE_ADDRESS",
            File = MakeFile("data.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", length: 0).Object
        };

        var result = await controller.ImportExcelValidate(form, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        service.Verify(s => s.ValidateImportExcelAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportExcelValidate_Returns400_WhenInvalidFileType()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var form = new ExcelImportFormDto
        {
            UpdateCode = "UPDATE_ADDRESS",
            File = MakeFile("report.exe", "application/x-msdownload").Object
        };

        var result = await controller.ImportExcelValidate(form, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        service.Verify(s => s.ValidateImportExcelAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportExcelValidate_Returns400_WhenServiceThrowsArgumentException()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var form = new ExcelImportFormDto
        {
            UpdateCode = "UPDATE_ADDRESS",
            File = MakeFile("data.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet").Object
        };

        service.Setup(s => s.ValidateImportExcelAsync("UPDATE_ADDRESS", It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Update type not found"));

        var result = await controller.ImportExcelValidate(form, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("Update type not found", response.Message);
    }

    // ============== GetUpdateHistory Tests ==============

    [Fact]
    public async Task GetUpdateHistory_ReturnsOk_WithPopulatedResults()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var request = new UpdateHistoryQueryParameters
        {
            UpdateName = "PROPERTY_BASIC",
            WardNo = "1",
            PageNumber = 1,
            PageSize = 10
        };

        var history = new List<UpdateHistoryDto>
        {
            new()
            {
                Id = 1,
                UpdateName = "PROPERTY_BASIC",
                WardNo = "1",
                PropertyNo = "001",
                PartitionNo = "",
                OldValue = "500",
                NewValue = "600",
                UpdatedColumns = "PlotArea"
                 
            },
            new()
            {
                Id = 2,
                UpdateName = "PROPERTY_BASIC",
                WardNo = "1",
                PropertyNo = "002",
                PartitionNo = "",
                OldValue = "400",
                NewValue = "450",
                UpdatedColumns = "PlotArea" 
            }
        };

        var pagedResult = new PagedResult<UpdateHistoryDto>(history, 2, 1, 10);

        service.Setup(s => s.GetUpdateHistoryAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetUpdateHistory(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as ApiResponse<PagedResult<UpdateHistoryDto>>;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(2, response.Items.TotalCount);
        Assert.Equal(2, response.Items.Items.Count());
        Assert.Equal("2 update history record(s) found", response.Message);
    }

    [Fact]
    public async Task GetUpdateHistory_ReturnsOk_WithEmptyResultSet()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var request = new UpdateHistoryQueryParameters
        {
            UpdateName = "NON_EXISTENT",
            PageNumber = 1,
            PageSize = 10
        };

        var pagedResult = new PagedResult<UpdateHistoryDto>(new List<UpdateHistoryDto>(), 0, 1, 10);

        service.Setup(s => s.GetUpdateHistoryAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetUpdateHistory(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as ApiResponse<PagedResult<UpdateHistoryDto>>;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Empty(response.Items.Items);
        Assert.Equal(0, response.Items.TotalCount);
        Assert.Equal("0 update history record(s) found", response.Message);
    }

    // ============== ExportUpdateHistoryExcel Tests ==============

    [Fact]
    public async Task ExportUpdateHistoryExcel_ReturnsFile_WithXlsxContentType()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var request = new UpdateHistoryQueryParameters { UpdateName = "PROPERTY_BASIC" };
        var bytes = new byte[] { 1, 2, 3, 4 };

        service.Setup(s => s.ExportUpdateHistoryToExcelAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        var result = await controller.ExportUpdateHistoryExcel(request, CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.Equal(bytes, fileResult.FileContents);
        Assert.StartsWith("UpdateHistory_", fileResult.FileDownloadName);
        Assert.EndsWith(".xlsx", fileResult.FileDownloadName);
    }

    // ============== GetUpdateActivity Tests ==============

    [Fact]
    public async Task GetUpdateActivity_ReturnsOk_WithPopulatedResults()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var request = new UpdateActivityQueryParameters
        {
            ActivityType = "Screen",
            PageNumber = 1,
            PageSize = 10
        };

        var activity = new List<UpdateActivityDto>
        {
            new()
            {
                Id = 1,
                ActivityType = "Screen",
                ActivityStatus = "Success",
                CreatedDate = DateTime.Now,
                DoneBy = "admin"
            }
        };
        var pagedResult = new PagedResult<UpdateActivityDto>(activity, 1, 1, 10);

        service.Setup(s => s.GetUpdateActivityAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetUpdateActivity(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as ApiResponse<PagedResult<UpdateActivityDto>>;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(1, response.Items.TotalCount);
        Assert.Equal("1 update activity record(s) found", response.Message);
    }

    [Fact]
    public async Task GetUpdateActivity_ReturnsOk_WithEmptyResultSet()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var request = new UpdateActivityQueryParameters { ActivityStatus = "Failed", PageNumber = 1, PageSize = 10 };
        var pagedResult = new PagedResult<UpdateActivityDto>(new List<UpdateActivityDto>(), 0, 1, 10);

        service.Setup(s => s.GetUpdateActivityAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetUpdateActivity(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as ApiResponse<PagedResult<UpdateActivityDto>>;
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Empty(response.Items.Items);
        Assert.Equal(0, response.Items.TotalCount);
        Assert.Equal("0 update activity record(s) found", response.Message);
    }

    // ============== ExportUpdateActivityExcel Tests ==============

    [Fact]
    public async Task ExportUpdateActivityExcel_ReturnsFile_WithXlsxContentType()
    {
        var controller = Create(out var service);
        SetupAuthenticatedUser(controller);

        var request = new UpdateActivityQueryParameters { ActivityType = "Screen" };
        var bytes = new byte[] { 1, 2, 3, 4 };

        service.Setup(s => s.ExportUpdateActivityToExcelAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        var result = await controller.ExportUpdateActivityExcel(request, CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.Equal(bytes, fileResult.FileContents);
        Assert.StartsWith("UpdateActivity_", fileResult.FileDownloadName);
        Assert.EndsWith(".xlsx", fileResult.FileDownloadName);
    }

    // ============== Helper Methods ==============

    private static Mock<IFormFile> MakeFile(string fileName, string contentType, long length = 10)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(length);
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(new byte[] { 1, 2, 3 }));
        return file;
    }

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
