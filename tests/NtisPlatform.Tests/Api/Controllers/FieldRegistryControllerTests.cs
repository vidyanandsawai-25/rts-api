using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.FieldRegistry;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System.Text.Json;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class FieldRegistryControllerTests
{
    private static FieldRegistryController Create(out Mock<IFieldRegistryService> service, bool isDevelopment = false)
    {
        service = new Mock<IFieldRegistryService>();
        var logger = new Mock<ILogger<FieldRegistryController>>();
        var controller = new FieldRegistryController(service.Object, logger.Object);

        var mockEnvironment = new Mock<IWebHostEnvironment>();
        mockEnvironment.Setup(e => e.EnvironmentName).Returns(isDevelopment ? "Development" : "Production");

        var services = new ServiceCollection();
        services.AddSingleton(mockEnvironment.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() }
        };

        return controller;
    }

    // ============== SetFieldRegistryStatus Tests ==============

    [Fact]
    public async Task SetFieldRegistryStatus_ReturnsOk_WithActivatedMessage_WhenIsActiveTrue()
    {
        var controller = Create(out var service);
        service.Setup(s => s.SetActiveStatusAsync("Update_ContactNo", true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.SetFieldRegistryStatus("Update_ContactNo", isActive: true, updatedBy: null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("activated", doc.RootElement.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetFieldRegistryStatus_ReturnsOk_WithDeactivatedMessage_WhenIsActiveFalse()
    {
        var controller = Create(out var service);
        service.Setup(s => s.SetActiveStatusAsync("Update_ContactNo", false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.SetFieldRegistryStatus("Update_ContactNo", isActive: false, updatedBy: null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("deactivated", doc.RootElement.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetFieldRegistryStatus_Returns404_WhenUpdateCodeNotFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.SetActiveStatusAsync("Unknown_Code", It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await controller.SetFieldRegistryStatus("Unknown_Code", isActive: false, updatedBy: null, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var json = JsonSerializer.Serialize(notFound.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task SetFieldRegistryStatus_PassesUpdatedBy_ToService()
    {
        var controller = Create(out var service);
        service.Setup(s => s.SetActiveStatusAsync("Update_ContactNo", false, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.SetFieldRegistryStatus("Update_ContactNo", isActive: false, updatedBy: 7, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.SetActiveStatusAsync("Update_ContactNo", false, 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetFieldRegistryStatus_Returns500_WhenServiceThrowsException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.SetActiveStatusAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.SetFieldRegistryStatus("Update_ContactNo", isActive: true, updatedBy: null, CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var json = JsonSerializer.Serialize(serverError.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
    }

    // ============== GetAll Tests ==============

    [Fact]
    public async Task GetAll_ReturnsOk_WithData()
    {
        var controller = Create(out var service);
        var data = new List<FieldRegistryDto>
        {
            new() { SchemaName = "dbo" },
            new() { SchemaName = "audit" }
        };
        service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(data);

        var result = await controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<FieldRegistryDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Items!.Count);
        Assert.Equal("dbo", response.Items[0].SchemaName);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithEmptyList()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FieldRegistryDto>());

        var result = await controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<FieldRegistryDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Empty(response.Items!);
    }

    [Fact]
    public async Task GetAll_Returns500_WhenServiceThrowsException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.GetAll(CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(serverError.Value);
        Assert.False(response.Success);
        Assert.Equal("An error occurred while retrieving field registry schemas", response.Message);
    }

    // ============== GetDetailsBySchema Tests ==============

    [Fact]
    public async Task GetDetailsBySchema_ReturnsOk_WithPagedResult()
    {
        var controller = Create(out var service);
        var queryParameters = new FieldRegistryDetailsQueryParameters { SchemaName = "dbo" };
        var pagedResult = new PagedResult<FieldRegistryDetailsDto>(
            new List<FieldRegistryDetailsDto> { new() { SchemaName = "dbo", TableName = "Employees" } },
            totalCount: 1,
            pageNumber: 1,
            pageSize: 10);
        service.Setup(s => s.GetDetailsBySchemaAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetDetailsBySchema(queryParameters, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResult<FieldRegistryDetailsDto>>(okResult.Value);
        Assert.Equal(1, response.TotalCount);
        Assert.Single(response.Items);
        Assert.Equal("Employees", response.Items.First().TableName);
    }

    [Fact]
    public async Task GetDetailsBySchema_Returns500_WhenServiceThrowsException()
    {
        var controller = Create(out var service);
        var queryParameters = new FieldRegistryDetailsQueryParameters { SchemaName = "dbo" };
        service.Setup(s => s.GetDetailsBySchemaAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.GetDetailsBySchema(queryParameters, CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var json = JsonSerializer.Serialize(serverError.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(
            "An error occurred while retrieving field registry details",
            doc.RootElement.GetProperty("message").GetString());
    }

    // ============== GetDetailsByTable Tests ==============

    [Fact]
    public async Task GetDetailsByTable_ReturnsOk_WithPagedResult()
    {
        var controller = Create(out var service);
        var queryParameters = new FieldRegistryTableDetailsQueryParameters { SchemaName = "dbo", TableName = "Employees" };
        var pagedResult = new PagedResult<FieldRegistryTableDetailsDto>(
            new List<FieldRegistryTableDetailsDto> { new() { ColumnName = "EmployeeId" } },
            totalCount: 1,
            pageNumber: 1,
            pageSize: 10);
        service.Setup(s => s.GetDetailsByTableAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetDetailsByTable(queryParameters, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResult<FieldRegistryTableDetailsDto>>(okResult.Value);
        Assert.Equal(1, response.TotalCount);
        Assert.Single(response.Items);
        Assert.Equal("EmployeeId", response.Items.First().ColumnName);
    }

    [Fact]
    public async Task GetDetailsByTable_Returns500_WhenServiceThrowsException()
    {
        var controller = Create(out var service);
        var queryParameters = new FieldRegistryTableDetailsQueryParameters { SchemaName = "dbo", TableName = "Employees" };
        service.Setup(s => s.GetDetailsByTableAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.GetDetailsByTable(queryParameters, CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var json = JsonSerializer.Serialize(serverError.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(
            "An error occurred while retrieving field registry table details",
            doc.RootElement.GetProperty("message").GetString());
    }

    // ============== GetFieldRegistries Tests ==============
    // Note: BaseQueryParameters.PageSize's setter (src/Application/NtisPlatform.Application/DTOs/Queries/BaseQueryParameters.cs)
    // clamps any value that is not -1 and less than 1 up to 10 (and anything above 100 down to 100) at assignment time.
    // Likewise PageNumber's setter clamps values less than 1 up to 1. That means the controller-level guards
    // `queryParameters.PageSize != -1 && queryParameters.PageSize < 1` and `queryParameters.PageNumber < 1` can never
    // be true when the DTO is populated through normal property assignment/model binding - the invalid values are
    // already sanitized before the controller sees them, making those branches effectively dead/unreachable code.
    // We therefore only test the reachable, valid combinations (PageSize = -1 for "all records" and a normal PageSize).

    [Fact]
    public async Task GetFieldRegistries_ReturnsOk_WithPagedResult_WhenPageSizeIsValid()
    {
        var controller = Create(out var service);
        var queryParameters = new FieldRegistryQueryParameters { PageSize = 10, PageNumber = 1 };
        var pagedResult = new PagedResult<FieldRegistryResponseDto>(
            new List<FieldRegistryResponseDto> { new() { MasterId = 1, UpdateCode = "Update_Test" } },
            totalCount: 1,
            pageNumber: 1,
            pageSize: 10);
        service.Setup(s => s.GetFieldRegistriesAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetFieldRegistries(queryParameters, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResult<FieldRegistryResponseDto>>(okResult.Value);
        Assert.Equal(1, response.TotalCount);
        Assert.Equal("Update_Test", response.Items.First().UpdateCode);
    }

    [Fact]
    public async Task GetFieldRegistries_ReturnsOk_WhenPageSizeIsMinusOneForAllRecords()
    {
        var controller = Create(out var service);
        var queryParameters = new FieldRegistryQueryParameters { PageSize = -1, PageNumber = 1 };
        var pagedResult = new PagedResult<FieldRegistryResponseDto>(
            new List<FieldRegistryResponseDto>
            {
                new() { MasterId = 1, UpdateCode = "Update_One" },
                new() { MasterId = 2, UpdateCode = "Update_Two" }
            },
            totalCount: 2,
            pageNumber: 1,
            pageSize: -1);
        service.Setup(s => s.GetFieldRegistriesAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetFieldRegistries(queryParameters, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResult<FieldRegistryResponseDto>>(okResult.Value);
        Assert.Equal(2, response.TotalCount);
    }

    [Fact]
    public async Task GetFieldRegistries_Returns400_WhenServiceThrowsArgumentException()
    {
        var controller = Create(out var service);
        var queryParameters = new FieldRegistryQueryParameters();
        service.Setup(s => s.GetFieldRegistriesAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid filter"));

        var result = await controller.GetFieldRegistries(queryParameters, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonSerializer.Serialize(badRequest.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Invalid filter", doc.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task GetFieldRegistries_Returns500_WhenServiceThrowsException()
    {
        var controller = Create(out var service);
        var queryParameters = new FieldRegistryQueryParameters();
        service.Setup(s => s.GetFieldRegistriesAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.GetFieldRegistries(queryParameters, CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var json = JsonSerializer.Serialize(serverError.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(
            "An error occurred while retrieving field registries",
            doc.RootElement.GetProperty("message").GetString());
        // IWebHostEnvironment.IsDevelopment() is false by default in this test helper, so detail should be null.
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("detail").ValueKind);
    }

    [Fact]
    public async Task GetFieldRegistries_Returns500_WithErrorDetail_WhenEnvironmentIsDevelopment()
    {
        var controller = Create(out var service, isDevelopment: true);
        var queryParameters = new FieldRegistryQueryParameters();
        service.Setup(s => s.GetFieldRegistriesAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.GetFieldRegistries(queryParameters, CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var json = JsonSerializer.Serialize(serverError.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Database error", doc.RootElement.GetProperty("detail").GetString());
    }

    // ============== AddFieldRegistry Tests ==============

    private static CreateFieldRegistryDto CreateValidCreateDto() => new()
    {
        UpdateCode = "Update_Test",
        UpdateName = "Test Update",
        ReferenceTableName = "TestTable",
        DisplaySequence = 1,
        FieldConfigs = new List<FieldRegistryFieldConfigDto>
        {
            new()
            {
                FieldName = "TestField",
                DisplayName = "Test Field",
                ControlType = "text",
                DataType = "string"
            }
        }
    };

    [Fact]
    public async Task AddFieldRegistry_ReturnsOk_WithCreatedItem()
    {
        var controller = Create(out var service);
        var createDto = CreateValidCreateDto();
        var responseDto = new FieldRegistryResponseDto { MasterId = 1, UpdateCode = "Update_Test" };
        service.Setup(s => s.AddFieldRegistryAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var result = await controller.AddFieldRegistry(createDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<FieldRegistryResponseDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Field registry created successfully", response.Message);
        Assert.Equal("Update_Test", response.Items!.UpdateCode);
    }

    [Fact]
    public async Task AddFieldRegistry_Returns400_WhenServiceThrowsArgumentException()
    {
        var controller = Create(out var service);
        var createDto = CreateValidCreateDto();
        service.Setup(s => s.AddFieldRegistryAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("UpdateCode already exists"));

        var result = await controller.AddFieldRegistry(createDto, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("UpdateCode already exists", response.Message);
    }

    [Fact]
    public async Task AddFieldRegistry_Returns500_WhenServiceThrowsException()
    {
        var controller = Create(out var service);
        var createDto = CreateValidCreateDto();
        service.Setup(s => s.AddFieldRegistryAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.AddFieldRegistry(createDto, CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(serverError.Value);
        Assert.False(response.Success);
        Assert.Equal("An error occurred while creating the field registry", response.Message);
    }

    // ============== UpdateFieldRegistry Tests ==============

    private static UpdateFieldRegistryDto CreateValidUpdateDto() => new()
    {
        UpdateName = "Test Update",
        ReferenceTableName = "TestTable",
        DisplaySequence = 1,
        FieldConfigs = new List<UpdateFieldRegistryFieldConfigDto>
        {
            new()
            {
                FieldName = "TestField",
                DisplayName = "Test Field",
                ControlType = "text",
                DataType = "string"
            }
        }
    };

    [Fact]
    public async Task UpdateFieldRegistry_ReturnsOk_WhenUpdateSucceeds()
    {
        var controller = Create(out var service);
        var updateDto = CreateValidUpdateDto();
        var responseDto = new FieldRegistryResponseDto { MasterId = 1, UpdateCode = "Update_Test" };
        service.Setup(s => s.UpdateFieldRegistryAsync("Update_Test", updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var result = await controller.UpdateFieldRegistry("Update_Test", updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<FieldRegistryResponseDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Field registry updated successfully", response.Message);
        Assert.Equal("Update_Test", response.Items!.UpdateCode);
    }

    [Fact]
    public async Task UpdateFieldRegistry_Returns404_WhenServiceReturnsNull()
    {
        var controller = Create(out var service);
        var updateDto = CreateValidUpdateDto();
        service.Setup(s => s.UpdateFieldRegistryAsync("Unknown_Code", updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FieldRegistryResponseDto?)null);

        var result = await controller.UpdateFieldRegistry("Unknown_Code", updateDto, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFound.Value);
        Assert.False(response.Success);
        Assert.Equal("Field registry not found", response.Message);
    }

    [Fact]
    public async Task UpdateFieldRegistry_Returns400_WhenServiceThrowsArgumentException()
    {
        var controller = Create(out var service);
        var updateDto = CreateValidUpdateDto();
        service.Setup(s => s.UpdateFieldRegistryAsync("Update_Test", updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("ReferenceTableName is invalid"));

        var result = await controller.UpdateFieldRegistry("Update_Test", updateDto, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("ReferenceTableName is invalid", response.Message);
    }

    [Fact]
    public async Task UpdateFieldRegistry_Returns500_WhenServiceThrowsException()
    {
        var controller = Create(out var service);
        var updateDto = CreateValidUpdateDto();
        service.Setup(s => s.UpdateFieldRegistryAsync("Update_Test", updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.UpdateFieldRegistry("Update_Test", updateDto, CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(serverError.Value);
        Assert.False(response.Success);
        Assert.Equal("An error occurred while updating the field registry", response.Message);
    }

    // ============== PurgeFieldRegistry Tests ==============

    [Fact]
    public async Task PurgeFieldRegistry_Returns400_WhenBothParamsAreNullOrEmpty()
    {
        var controller = Create(out var service);

        var result = await controller.PurgeFieldRegistry(null, null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("Either updateCode or fieldConfigId must be provided", response.Message);
        service.Verify(
            s => s.PurgeFieldRegistryAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PurgeFieldRegistry_Returns400_WhenBothParamsAreWhitespace()
    {
        var controller = Create(out var service);

        var result = await controller.PurgeFieldRegistry("   ", "   ", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        service.Verify(
            s => s.PurgeFieldRegistryAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PurgeFieldRegistry_ReturnsOk_WhenOnlyUpdateCodeGiven()
    {
        var controller = Create(out var service);
        var resultDto = new PurgeFieldRegistryResultDto
        {
            DeletedMasterCount = 1,
            DeletedFieldConfigCount = 3,
            DeletedHistoryCount = 2
        };
        service.Setup(s => s.PurgeFieldRegistryAsync("Update_Test", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.PurgeFieldRegistry("Update_Test", null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PurgeFieldRegistryResultDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Field registry purged successfully", response.Message);
        Assert.Equal(1, response.Items!.DeletedMasterCount);
        service.Verify(s => s.PurgeFieldRegistryAsync("Update_Test", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PurgeFieldRegistry_ReturnsOk_WhenOnlyFieldConfigIdGiven()
    {
        var controller = Create(out var service);
        var resultDto = new PurgeFieldRegistryResultDto
        {
            DeletedMasterCount = 2,
            DeletedFieldConfigCount = 5,
            DeletedHistoryCount = 4
        };
        service.Setup(s => s.PurgeFieldRegistryAsync(null, "1,2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.PurgeFieldRegistry(null, "1,2", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PurgeFieldRegistryResultDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Items!.DeletedMasterCount);
        service.Verify(s => s.PurgeFieldRegistryAsync(null, "1,2", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PurgeFieldRegistry_Returns400_WhenServiceThrowsArgumentException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.PurgeFieldRegistryAsync("Update_Test", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("UpdateCode not found"));

        var result = await controller.PurgeFieldRegistry("Update_Test", null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("UpdateCode not found", response.Message);
    }

    [Fact]
    public async Task PurgeFieldRegistry_Returns500_WhenServiceThrowsException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.PurgeFieldRegistryAsync("Update_Test", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.PurgeFieldRegistry("Update_Test", null, CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(serverError.Value);
        Assert.False(response.Success);
        Assert.Equal("An error occurred while purging the field registry", response.Message);
    }
}
