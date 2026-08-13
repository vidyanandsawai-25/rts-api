using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Asset_Management;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Asset_Management.ManageSubUnits;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Tests.Api.Controllers.Asset_Management;

/// <summary>
/// Tests for <see cref="ManageSubUnitsController"/> covering all 7 hand-written endpoints:
/// BulkGenerateChildAssets, BulkGenerateAcrossFloors, CreateChildAsset, GetAllSubUnitsByParentId,
/// GetSubUnitDetailsById, GetSubUnitsCompleteDetailsByParentId, and
/// GetSubUnitLeaseRentBySubUnitDetailsId. The service is fully mocked; only controller-level
/// response-shaping/error-handling logic is exercised here.
/// </summary>
public class ManageSubUnitsControllerTests
{
    private static ManageSubUnitsController Create(
        out Mock<IManageSubUnitsService> service,
        out Mock<ILogger<ManageSubUnitsController>> logger)
    {
        logger = new Mock<ILogger<ManageSubUnitsController>>();
        service = new Mock<IManageSubUnitsService>();
        return new ManageSubUnitsController(logger.Object, service.Object);
    }

    #region Controller Contract Tests

    [Fact]
    public void Controller_HasExpectedRoutePrefix()
    {
        var attribute = typeof(ManageSubUnitsController)
            .GetCustomAttributes(typeof(RouteAttribute), false)
            .FirstOrDefault() as RouteAttribute;

        Assert.NotNull(attribute);
        Assert.Equal("api/[controller]", attribute!.Template);
    }

    [Fact]
    public void Controller_RequiresAuthorization()
    {
        var attributes = typeof(ManageSubUnitsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), false);

        Assert.NotEmpty(attributes);
    }

    #endregion

    #region BulkGenerateChildAssets Tests

    [Fact]
    public async Task BulkGenerateChildAssets_NoErrors_ReturnsOkWithSuccessTrue()
    {
        var controller = Create(out var service, out _);
        var dto = new BulkGenerateChildAssetsDto { ParentAssetId = 1, Type = "Flat", Count = 3 };
        var response = new BulkGenerateChildAssetsResponseDto
        {
            TotalGenerated = 3,
            GeneratedAssets = new List<GeneratedAssetDto>
            {
                new() { AssetId = 10, AssetNo = "A-1", AssetName = "Flat Unit" },
                new() { AssetId = 11, AssetNo = "A-2", AssetName = "Flat Unit" },
                new() { AssetId = 12, AssetNo = "A-3", AssetName = "Flat Unit" }
            }
        };
        service.Setup(s => s.BulkGenerateChildAssetsAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.BulkGenerateChildAssets(dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkGenerateChildAssetsResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("Successfully generated 3", apiResponse.Message);
        Assert.Equal(3, apiResponse.Items!.TotalGenerated);
    }

    [Fact]
    public async Task BulkGenerateChildAssets_PartialErrors_ReturnsOkWithSuccessTrueAndPartialMessage()
    {
        var controller = Create(out var service, out _);
        var dto = new BulkGenerateChildAssetsDto { ParentAssetId = 1, Type = "Flat", Count = 2 };
        var response = new BulkGenerateChildAssetsResponseDto
        {
            TotalGenerated = 1,
            GeneratedAssets = new List<GeneratedAssetDto> { new() { AssetId = 10, AssetNo = "A-1", AssetName = "Flat Unit" } },
            Errors = new List<string> { "Something went wrong for unit 2" }
        };
        service.Setup(s => s.BulkGenerateChildAssetsAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.BulkGenerateChildAssets(dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkGenerateChildAssetsResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("Partially completed", apiResponse.Message);
    }

    [Fact]
    public async Task BulkGenerateChildAssets_AllFail_ReturnsOkWithSuccessFalse()
    {
        var controller = Create(out var service, out _);
        var dto = new BulkGenerateChildAssetsDto { ParentAssetId = 999, Type = "Flat", Count = 2 };
        var response = new BulkGenerateChildAssetsResponseDto
        {
            TotalGenerated = 0,
            Errors = new List<string> { "Parent asset with Id 999 not found" }
        };
        service.Setup(s => s.BulkGenerateChildAssetsAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.BulkGenerateChildAssets(dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkGenerateChildAssetsResponseDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("Bulk generation failed. See errors for details.", apiResponse.Message);
    }

    #endregion

    #region BulkGenerateAcrossFloors Tests

    [Fact]
    public async Task BulkGenerateAcrossFloors_NoErrors_ReturnsOkWithSuccessTrue()
    {
        var controller = Create(out var service, out _);
        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            FloorIds = new List<int> { 1, 2 },
            UnitsPerFloor = 2
        };
        var response = new BulkGenerateAcrossFloorsResponseDto
        {
            TotalGenerated = 4,
            GeneratedAssets = Enumerable.Range(1, 4)
                .Select(i => new GeneratedAssetDto { AssetId = i, AssetNo = $"A-{i}", AssetName = "Shop Unit" })
                .ToList()
        };
        service.Setup(s => s.BulkGenerateAcrossFloorsAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.BulkGenerateAcrossFloors(dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkGenerateAcrossFloorsResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(4, apiResponse.Items!.TotalGenerated);
    }

    [Fact]
    public async Task BulkGenerateAcrossFloors_PartialErrors_ReturnsOkWithPartialMessage()
    {
        var controller = Create(out var service, out _);
        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            FloorIds = new List<int> { 1 },
            UnitsPerFloor = 2
        };
        var response = new BulkGenerateAcrossFloorsResponseDto
        {
            TotalGenerated = 1,
            GeneratedAssets = new List<GeneratedAssetDto> { new() { AssetId = 1, AssetNo = "A-1", AssetName = "Shop Unit" } },
            Errors = new List<string> { "Failed on unit 2" }
        };
        service.Setup(s => s.BulkGenerateAcrossFloorsAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.BulkGenerateAcrossFloors(dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkGenerateAcrossFloorsResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("Partially completed", apiResponse.Message);
    }

    [Fact]
    public async Task BulkGenerateAcrossFloors_AllFail_ReturnsOkWithSuccessFalse()
    {
        var controller = Create(out var service, out _);
        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            FloorIds = new List<int>(),
            UnitsPerFloor = 2
        };
        var response = new BulkGenerateAcrossFloorsResponseDto
        {
            TotalGenerated = 0,
            Errors = new List<string> { "At least one FloorId is required." }
        };
        service.Setup(s => s.BulkGenerateAcrossFloorsAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.BulkGenerateAcrossFloors(dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkGenerateAcrossFloorsResponseDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("Generation failed. See errors for details.", apiResponse.Message);
    }

    #endregion

    #region CreateChildAsset Tests

    [Fact]
    public async Task CreateChildAsset_BlankAssessmentYear_DefaultsToCurrentYearBeforeCallingService()
    {
        var controller = Create(out var service, out _);
        var dto = new CreateChildAssetDto { ParentAssetId = 1, AssetId = 10, AssessmentYear = "  " };
        CreateChildAssetDto? capturedDto = null;
        service.Setup(s => s.CreateChildAssetAsync(It.IsAny<CreateChildAssetDto>(), It.IsAny<CancellationToken>()))
            .Callback<CreateChildAssetDto, CancellationToken>((d, _) => capturedDto = d)
            .ReturnsAsync(new CreateChildAssetResponseDto { Success = true, Message = "ok", AssetId = 10, AssetNo = "A-1" });

        await controller.CreateChildAsset(dto, CancellationToken.None);

        Assert.NotNull(capturedDto);
        Assert.Equal(DateTime.UtcNow.Year.ToString(), capturedDto!.AssessmentYear);
    }

    [Fact]
    public async Task CreateChildAsset_NonBlankAssessmentYear_IsNotOverwritten()
    {
        var controller = Create(out var service, out _);
        var dto = new CreateChildAssetDto { ParentAssetId = 1, AssetId = 10, AssessmentYear = "2023" };
        CreateChildAssetDto? capturedDto = null;
        service.Setup(s => s.CreateChildAssetAsync(It.IsAny<CreateChildAssetDto>(), It.IsAny<CancellationToken>()))
            .Callback<CreateChildAssetDto, CancellationToken>((d, _) => capturedDto = d)
            .ReturnsAsync(new CreateChildAssetResponseDto { Success = true, Message = "ok" });

        await controller.CreateChildAsset(dto, CancellationToken.None);

        Assert.Equal("2023", capturedDto!.AssessmentYear);
    }

    [Fact]
    public async Task CreateChildAsset_ServiceSucceeds_ReturnsOkWithApiResponse()
    {
        var controller = Create(out var service, out _);
        var dto = new CreateChildAssetDto { ParentAssetId = 1, AssetId = 10 };
        var response = new CreateChildAssetResponseDto
        {
            Success = true,
            Message = "created",
            AssetId = 10,
            AssetNo = "A-1"
        };
        service.Setup(s => s.CreateChildAssetAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await controller.CreateChildAsset(dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<CreateChildAssetResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(10, apiResponse.Items!.AssetId);
    }

    [Fact]
    public async Task CreateChildAsset_ServiceFails_ReturnsBadRequestWithApiResponse()
    {
        var controller = Create(out var service, out _);
        var dto = new CreateChildAssetDto { ParentAssetId = 999, AssetId = 10 };
        var response = new CreateChildAssetResponseDto
        {
            Success = false,
            Message = "Parent asset with Id 999 not found",
            Errors = { "Parent asset with Id 999 not found" }
        };
        service.Setup(s => s.CreateChildAssetAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var result = await controller.CreateChildAsset(dto, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<CreateChildAssetResponseDto>>(badRequest.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("Parent asset with Id 999 not found", apiResponse.Message);
    }

    #endregion

    #region GetAllSubUnitsByParentId Tests

    [Fact]
    public async Task GetAllSubUnitsByParentId_Success_ReturnsOkWithCountMessage()
    {
        var controller = Create(out var service, out _);
        var list = new List<SubUnitListDto>
        {
            new() { Id = 1, AssetNo = "A-1", AssetName = "Flat 101" },
            new() { Id = 2, AssetNo = "A-2", AssetName = "Flat 102" }
        };
        service.Setup(s => s.GetAllSubUnitsByParentIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var result = await controller.GetAllSubUnitsByParentId(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<List<SubUnitListDto>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("Retrieved 2 sub-units", apiResponse.Message);
        Assert.Equal(2, apiResponse.Items!.Count);
    }

    [Fact]
    public async Task GetAllSubUnitsByParentId_ServiceThrows_Returns500()
    {
        var controller = Create(out var service, out _);
        service.Setup(s => s.GetAllSubUnitsByParentIdAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var result = await controller.GetAllSubUnitsByParentId(1, CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<List<SubUnitListDto>>>(serverError.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("An error occurred while retrieving sub-units", apiResponse.Message);
    }

    #endregion

    #region GetSubUnitDetailsById Tests

    [Fact]
    public async Task GetSubUnitDetailsById_Success_ReturnsOkWithDetail()
    {
        var controller = Create(out var service, out _);
        var detail = new SubAssetDetailDto { Id = 5, AssetNo = "A-5", AssetName = "Flat 105" };
        service.Setup(s => s.GetSubUnitDetailsByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(detail);

        var result = await controller.GetSubUnitDetailsById(5, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SubAssetDetailDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(5, apiResponse.Items!.Id);
    }

    [Fact]
    public async Task GetSubUnitDetailsById_NotFound_Returns404WithMessage()
    {
        var controller = Create(out var service, out _);
        service.Setup(s => s.GetSubUnitDetailsByIdAsync(999, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Sub-unit with AssetId 999 not found, deleted, or it is inventory data."));

        var result = await controller.GetSubUnitDetailsById(999, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(notFound.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("999", apiResponse.Message);
    }

    [Fact]
    public async Task GetSubUnitDetailsById_ServiceThrowsGenericException_Returns500()
    {
        var controller = Create(out var service, out _);
        service.Setup(s => s.GetSubUnitDetailsByIdAsync(5, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.GetSubUnitDetailsById(5, CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<object>>(serverError.Value);
        Assert.False(apiResponse.Success);
    }

    #endregion

    #region GetSubUnitsCompleteDetailsByParentId Tests

    [Fact]
    public async Task GetSubUnitsCompleteDetailsByParentId_Success_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var list = new List<SubUnitCompleteDetailDto> { new() { Id = 1, AssetNo = "A-1", AssetName = "Flat 101" } };
        service.Setup(s => s.GetSubUnitsCompleteDetailsByParentIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var result = await controller.GetSubUnitsCompleteDetailsByParentId(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<List<SubUnitCompleteDetailDto>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Single(apiResponse.Items!);
    }

    [Fact]
    public async Task GetSubUnitsCompleteDetailsByParentId_ServiceThrows_Returns500()
    {
        var controller = Create(out var service, out _);
        service.Setup(s => s.GetSubUnitsCompleteDetailsByParentIdAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.GetSubUnitsCompleteDetailsByParentId(1, CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<List<SubUnitCompleteDetailDto>>>(serverError.Value);
        Assert.False(apiResponse.Success);
    }

    #endregion

    #region GetSubUnitLeaseRentBySubUnitDetailsId Tests

    [Fact]
    public async Task GetSubUnitLeaseRentBySubUnitDetailsId_Success_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var detail = new SubUnitLeaseRentDetailDto { SubUnitDetailsId = 1, AssetId = 5, AssetNo = "A-5", AssetName = "Flat 105" };
        service.Setup(s => s.GetSubUnitLeaseRentBySubUnitDetailsIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(detail);

        var result = await controller.GetSubUnitLeaseRentBySubUnitDetailsId(5, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SubUnitLeaseRentDetailDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(5, apiResponse.Items!.AssetId);
    }

    [Fact]
    public async Task GetSubUnitLeaseRentBySubUnitDetailsId_NotFound_Returns404()
    {
        var controller = Create(out var service, out _);
        service.Setup(s => s.GetSubUnitLeaseRentBySubUnitDetailsIdAsync(999, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Asset with Id 999 not found."));

        var result = await controller.GetSubUnitLeaseRentBySubUnitDetailsId(999, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<SubUnitLeaseRentDetailDto>>(notFound.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("999", apiResponse.Message);
    }

    [Fact]
    public async Task GetSubUnitLeaseRentBySubUnitDetailsId_UnhandledException_Propagates()
    {
        // This action has no catch-all for generic exceptions — verify it is not
        // silently swallowed and instead propagates to the global exception handler.
        var controller = Create(out var service, out _);
        service.Setup(s => s.GetSubUnitLeaseRentBySubUnitDetailsIdAsync(5, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.GetSubUnitLeaseRentBySubUnitDetailsId(5, CancellationToken.None));
    }

    #endregion
}
