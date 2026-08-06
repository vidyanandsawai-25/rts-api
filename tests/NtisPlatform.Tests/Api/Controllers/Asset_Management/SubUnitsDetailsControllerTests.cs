using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Asset_Management;
using NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Asset_Management;

/// <summary>
/// Tests for <see cref="SubUnitsDetailsController"/>. The controller is a thin CRUD shim
/// (GetById, GetByAssetId, Create, Update) per CLAUDE.md Section 5/Step 8 — only
/// controller-level response-shaping is exercised here; the service is fully mocked.
///
/// The Delete and CalculateCapitalValue actions (and the HttpDelete/calculate-capital-value
/// routes) were intentionally removed from this controller: Delete was unused generic CRUD
/// surface, and CalculateCapitalValue exposed a service method
/// (ISubUnitsDetailsService.CalculateAndUpdateCapitalValueAsync) that was itself dead code —
/// it depended on an ICVCalculationService that was never implemented or registered in DI.
/// The "Removed Endpoints" region below locks that removal in as a regression guard.
/// </summary>
public class SubUnitsDetailsControllerTests
{
    private static SubUnitsDetailsController Create(out Mock<ISubUnitsDetailsService> service)
    {
        service = new Mock<ISubUnitsDetailsService>();
        var logger = new Mock<ILogger<SubUnitsDetailsController>>();
        return new SubUnitsDetailsController(logger.Object, service.Object);
    }

    #region Controller Contract Tests

    [Fact]
    public void Controller_RequiresAuthorization()
    {
        var attributes = typeof(SubUnitsDetailsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), false);

        Assert.NotEmpty(attributes);
    }

    [Fact]
    public void Controller_HasExpectedRoutePrefix()
    {
        var attribute = typeof(SubUnitsDetailsController)
            .GetCustomAttributes(typeof(RouteAttribute), false)
            .FirstOrDefault() as RouteAttribute;

        Assert.NotNull(attribute);
        Assert.Equal("api/AssetFloorDetails", attribute!.Template);
    }

    #endregion

    #region Removed Endpoints (regression guard)

    [Fact]
    public void Controller_DoesNotExposeDeleteAction()
    {
        var method = typeof(SubUnitsDetailsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == "Delete");

        Assert.Null(method);
    }

    [Fact]
    public void Controller_DoesNotExposeCalculateCapitalValueAction()
    {
        var method = typeof(SubUnitsDetailsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == "CalculateCapitalValue");

        Assert.Null(method);
    }

    [Fact]
    public void Controller_HasNoHttpDeleteRoute()
    {
        var hasHttpDelete = typeof(SubUnitsDetailsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(m => m.GetCustomAttributes(typeof(HttpDeleteAttribute), false))
            .Any();

        Assert.False(hasHttpDelete);
    }

    [Fact]
    public void Controller_HasNoCalculateCapitalValueRoute()
    {
        var routeTemplates = typeof(SubUnitsDetailsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(m => m.GetCustomAttributes(typeof(HttpMethodAttribute), false).Cast<HttpMethodAttribute>())
            .Select(a => a.Template ?? string.Empty);

        Assert.DoesNotContain(routeTemplates, t => t.Contains("calculate-capital-value", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ServiceInterface_DoesNotExposeCalculateAndUpdateCapitalValueAsync()
    {
        var method = typeof(ISubUnitsDetailsService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == "CalculateAndUpdateCapitalValueAsync");

        Assert.Null(method);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_Existing_ReturnsOkWithDto()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubUnitsDetailsDto { Id = 1, AssetId = 10, FloorId = 1 });

        var result = await controller.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<SubUnitsDetailsDto>(okResult.Value);
        Assert.Equal(1, dto.Id);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubUnitsDetailsDto?)null);

        var result = await controller.GetById(99, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_ServiceThrows_Returns500()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.GetById(1, CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
    }

    #endregion

    #region GetByAssetId Tests

    [Fact]
    public async Task GetByAssetId_ReturnsOkWithApiResponseWrappingSummary()
    {
        var controller = Create(out var service);
        var summary = new SubUnitsDetailsSummaryDto
        {
            FloorDetails = new List<SubUnitsDetailsDto> { new() { Id = 1, AssetId = 10, FloorId = 1 } },
            TotalFloors = 1,
            TotalCapitalValue = 5000m
        };
        service.Setup(s => s.GetByAssetIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(summary);

        var result = await controller.GetByAssetId(10, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SubUnitsDetailsSummaryDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(1, apiResponse.Items!.TotalFloors);
        Assert.Equal(5000m, apiResponse.Items!.TotalCapitalValue);
    }

    [Fact]
    public async Task GetByAssetId_UnhandledException_Propagates()
    {
        // GetByAssetId has no try/catch of its own — verify a service failure is not
        // silently swallowed and instead reaches the global exception handler.
        var controller = Create(out var service);
        service.Setup(s => s.GetByAssetIdAsync(10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.GetByAssetId(10, CancellationToken.None));
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ReturnsOkWithApiResponse()
    {
        var controller = Create(out var service);
        var createDto = new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 };
        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubUnitsDetailsDto { Id = 1, AssetId = 10, FloorId = 1 });

        var result = await controller.Create(createDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SubUnitsDetailsDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(1, apiResponse.Items!.Id);
    }

    [Fact]
    public async Task Create_DuplicateConstraintViolation_ReturnsConflict()
    {
        var controller = Create(out var service);
        var createDto = new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 };
        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Violation of UNIQUE constraint"));

        var result = await controller.Create(createDto, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SubUnitsDetailsDto>>(conflict.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Create_ValidationException_Propagates()
    {
        // ExecuteCreate explicitly excludes ValidationException from its catch clause so the
        // global exception handler middleware can turn it into a structured 400.
        var controller = Create(out var service);
        var createDto = new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 };
        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation failed", new Dictionary<string, string>(), OperationType.Create));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Create(createDto, CancellationToken.None));
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_Existing_ReturnsOkWithSuccessTrue()
    {
        var controller = Create(out var service);
        var updateDto = new UpdateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 };
        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubUnitsDetailsDto { Id = 1, AssetId = 10, FloorId = 1 });

        var result = await controller.Update(1, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SubUnitsDetailsDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsOkWithSuccessFalse()
    {
        var controller = Create(out var service);
        var updateDto = new UpdateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 };
        service.Setup(s => s.UpdateAsync(99, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubUnitsDetailsDto?)null);

        var result = await controller.Update(99, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SubUnitsDetailsDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("Record not found for Update ", apiResponse.Message);
    }

    [Fact]
    public async Task Update_ValidationException_Propagates()
    {
        var controller = Create(out var service);
        var updateDto = new UpdateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 };
        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation failed", new Dictionary<string, string>(), OperationType.Update));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Update(1, updateDto, CancellationToken.None));
    }

    #endregion
}
