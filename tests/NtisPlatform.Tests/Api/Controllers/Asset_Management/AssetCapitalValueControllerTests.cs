using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Asset_Management;
using NtisPlatform.Application.DTOs.AssetCapitalValue;
using NtisPlatform.Application.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Asset_Management;

/// <summary>
/// Covers <see cref="AssetCapitalValueController"/> — HTTP wiring only, so every test mocks
/// <see cref="IAssetCapitalValueService"/> and asserts the controller (a) returns the right
/// <see cref="IActionResult"/> shape, (b) forwards the request/id and CancellationToken unchanged,
/// and (c) does not swallow exceptions (CLAUDE.md: controllers must not try/catch — the global
/// exception handler maps service exceptions to status codes).
/// </summary>
public class AssetCapitalValueControllerTests
{
    private readonly Mock<IAssetCapitalValueService> _mockService;
    private readonly Mock<ILogger<AssetCapitalValueController>> _mockLogger;
    private readonly AssetCapitalValueController _controller;

    public AssetCapitalValueControllerTests()
    {
        _mockService = new Mock<IAssetCapitalValueService>();
        _mockLogger = new Mock<ILogger<AssetCapitalValueController>>();
        _controller = new AssetCapitalValueController(_mockService.Object, _mockLogger.Object);
    }

    #region Constructor / Cross-cutting

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var controller = new AssetCapitalValueController(_mockService.Object, _mockLogger.Object);
        Assert.NotNull(controller);
    }

    [Fact]
    public void Controller_HasAuthorizeAttribute()
    {
        // CLAUDE.md Section 17: assert [Authorize] is present so unauthenticated requests 401.
        var attributes = typeof(AssetCapitalValueController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);
        Assert.NotEmpty(attributes);
    }

    [Fact]
    public void Controller_HasNoAllowAnonymousAttribute()
    {
        var attributes = typeof(AssetCapitalValueController).GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true);
        Assert.Empty(attributes);
    }

    #endregion

    #region CalculateUnitCV Tests

    [Fact]
    public async Task CalculateUnitCV_WithValidRequest_ReturnsOkWithSummary()
    {
        // Arrange
        var request = new CalculateAssetCVRequestDto { AssetId = 101, SubUnitsDetailsId = 0, IncludeChildAssets = false };
        var expected = new AssetCVSummaryDto
        {
            AssetId = 101,
            AssetNo = "A-101",
            TotalCapitalValue = 55000m,
            FloorDetailsCount = 1,
            CalculatedFloorDetailsCount = 1
        };

        _mockService.Setup(s => s.CalculateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.CalculateUnitCV(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<AssetCVSummaryDto>(okResult.Value);
        Assert.Equal(101, dto.AssetId);
        Assert.Equal(55000m, dto.TotalCapitalValue);
        _mockService.Verify(s => s.CalculateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalculateUnitCV_PropagatesCancellationToken()
    {
        var request = new CalculateAssetCVRequestDto { AssetId = 1 };
        using var cts = new CancellationTokenSource();
        _mockService.Setup(s => s.CalculateAsync(request, cts.Token))
            .ReturnsAsync(new AssetCVSummaryDto { AssetId = 1 });

        await _controller.CalculateUnitCV(request, cts.Token);

        _mockService.Verify(s => s.CalculateAsync(request, cts.Token), Times.Once);
    }

    [Fact]
    public async Task CalculateUnitCV_WhenAssetNotFound_PropagatesException()
    {
        // The service throws InvalidOperationException when the asset doesn't exist (see
        // AssetCapitalValueService.CalculateAsync) — the controller has no try/catch, so this
        // must bubble up uncaught for the global exception handler to map to a response.
        var request = new CalculateAssetCVRequestDto { AssetId = 999 };
        _mockService.Setup(s => s.CalculateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Asset with ID 999 not found"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.CalculateUnitCV(request, CancellationToken.None));
    }

    [Fact]
    public async Task CalculateUnitCV_CallsServiceExactlyOnce()
    {
        var request = new CalculateAssetCVRequestDto { AssetId = 1 };
        _mockService.Setup(s => s.CalculateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetCVSummaryDto { AssetId = 1 });

        await _controller.CalculateUnitCV(request, CancellationToken.None);

        _mockService.Verify(s => s.CalculateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockService.VerifyNoOtherCalls();
    }

    #endregion

    #region CalculateBuildingCV Tests

    [Fact]
    public async Task CalculateBuildingCV_WithValidRequest_ReturnsOkWithSummary()
    {
        var request = new CalculateBuildingCVRequestDto { BuildingAssetId = 55 };
        var expected = new BuildingCVSummaryDto
        {
            BuildingAssetId = 55,
            TotalBuildingCapitalValue = 250000m,
            TotalChildAssets = 3,
            CalculatedChildAssets = 3,
            IsFullyCalculated = true
        };

        _mockService.Setup(s => s.CalculateBuildingCVAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.CalculateBuildingCV(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<BuildingCVSummaryDto>(okResult.Value);
        Assert.Equal(55, dto.BuildingAssetId);
        Assert.Equal(250000m, dto.TotalBuildingCapitalValue);
        Assert.True(dto.IsFullyCalculated);
    }

    [Fact]
    public async Task CalculateBuildingCV_WhenBuildingNotFound_PropagatesException()
    {
        var request = new CalculateBuildingCVRequestDto { BuildingAssetId = 404 };
        _mockService.Setup(s => s.CalculateBuildingCVAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Building asset with ID 404 not found"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.CalculateBuildingCV(request, CancellationToken.None));
    }

    [Fact]
    public async Task CalculateBuildingCV_CallsServiceExactlyOnce()
    {
        var request = new CalculateBuildingCVRequestDto { BuildingAssetId = 1 };
        _mockService.Setup(s => s.CalculateBuildingCVAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BuildingCVSummaryDto { BuildingAssetId = 1 });

        await _controller.CalculateBuildingCV(request, CancellationToken.None);

        _mockService.Verify(s => s.CalculateBuildingCVAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockService.VerifyNoOtherCalls();
    }

    #endregion

    #region CalculatePlotCV Tests

    [Fact]
    public async Task CalculatePlotCV_WithValidRequest_ReturnsOkWithSummary()
    {
        var request = new CalculatePlotCVRequestDto { AssetId = 77 };
        var expected = new PlotCVSummaryDto
        {
            AssetId = 77,
            TotalPlots = 1,
            CalculatedPlots = 1,
            TotalCapitalValue = 12000m
        };

        _mockService.Setup(s => s.CalculatePlotCVAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.CalculatePlotCV(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PlotCVSummaryDto>(okResult.Value);
        Assert.Equal(77, dto.AssetId);
        Assert.Equal(12000m, dto.TotalCapitalValue);
        Assert.True(dto.IsFullyCalculated);
    }

    [Fact]
    public async Task CalculatePlotCV_WithNoLandArea_ReturnsOkWithUncalculatedSummary()
    {
        // Service returns (not throws) an uncalculated summary when LandAreaSqMeter isn't set.
        var request = new CalculatePlotCVRequestDto { AssetId = 88 };
        var expected = new PlotCVSummaryDto { AssetId = 88, TotalPlots = 1, CalculatedPlots = 0 };
        expected.PlotDetails.Add(new PlotCVDetailDto
        {
            PlotId = 88,
            IsCalculated = false,
            CalculationMessage = "No land area found for this plot. Enter Total Plot Area on Basic Info."
        });

        _mockService.Setup(s => s.CalculatePlotCVAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.CalculatePlotCV(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PlotCVSummaryDto>(okResult.Value);
        Assert.False(dto.IsFullyCalculated);
        Assert.False(dto.PlotDetails.Single().IsCalculated);
    }

    [Fact]
    public async Task CalculatePlotCV_WhenAssetNotFound_PropagatesException()
    {
        var request = new CalculatePlotCVRequestDto { AssetId = 999 };
        _mockService.Setup(s => s.CalculatePlotCVAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Asset with ID 999 not found"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.CalculatePlotCV(request, CancellationToken.None));
    }

    [Fact]
    public async Task CalculatePlotCV_CallsServiceExactlyOnce()
    {
        var request = new CalculatePlotCVRequestDto { AssetId = 1 };
        _mockService.Setup(s => s.CalculatePlotCVAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlotCVSummaryDto { AssetId = 1 });

        await _controller.CalculatePlotCV(request, CancellationToken.None);

        _mockService.Verify(s => s.CalculatePlotCVAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockService.VerifyNoOtherCalls();
    }

    #endregion

    #region CalculateMovableAssetCV Tests

    [Fact]
    public async Task CalculateMovableAssetCV_WithValidRequest_ReturnsOkWithResult()
    {
        var request = new CalculateMovableAssetCVRequestDto
        {
            AssetId = 33,
            ValuationMethod = MovableAssetValuationMethod.DepreciatedValue,
            ConditionFactor = 0.9m
        };
        var expected = new MovableAssetCVResultDto
        {
            AssetId = 33,
            CapitalValue = 45000m,
            IsCalculated = true,
            ValuationMethod = MovableAssetValuationMethod.DepreciatedValue
        };

        _mockService.Setup(s => s.CalculateMovableAssetCVAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.CalculateMovableAssetCV(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<MovableAssetCVResultDto>(okResult.Value);
        Assert.Equal(33, dto.AssetId);
        Assert.True(dto.IsCalculated);
        Assert.Equal(45000m, dto.CapitalValue);
    }

    [Fact]
    public async Task CalculateMovableAssetCV_WhenAssetNotFound_PropagatesException()
    {
        var request = new CalculateMovableAssetCVRequestDto { AssetId = 999 };
        _mockService.Setup(s => s.CalculateMovableAssetCVAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Asset with ID 999 not found"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.CalculateMovableAssetCV(request, CancellationToken.None));
    }

    [Fact]
    public async Task CalculateMovableAssetCV_CallsServiceExactlyOnce()
    {
        var request = new CalculateMovableAssetCVRequestDto { AssetId = 1 };
        _mockService.Setup(s => s.CalculateMovableAssetCVAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovableAssetCVResultDto { AssetId = 1 });

        await _controller.CalculateMovableAssetCV(request, CancellationToken.None);

        _mockService.Verify(s => s.CalculateMovableAssetCVAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockService.VerifyNoOtherCalls();
    }

    #endregion

    #region GetParentAssetValuation Tests

    [Fact]
    public async Task GetParentAssetValuation_WithExistingParent_ReturnsOkWithValuation()
    {
        const long parentAssetId = 10;
        var expected = new ParentAssetValuationDto
        {
            ParentAssetId = parentAssetId,
            BaseValue = 10000m,
            SubUnitsCapitalValue = 20000m,
            InventoryCapitalValue = 5000m,
            TotalCapitalValue = 35000m
        };

        _mockService.Setup(s => s.GetParentAssetValuationAsync(parentAssetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetParentAssetValuation(parentAssetId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ParentAssetValuationDto>(okResult.Value);
        Assert.Equal(parentAssetId, dto.ParentAssetId);
        Assert.Equal(35000m, dto.TotalCapitalValue);
    }

    [Fact]
    public async Task GetParentAssetValuation_WithNonExistingParent_ReturnsNotFound()
    {
        const long parentAssetId = 999;
        _mockService.Setup(s => s.GetParentAssetValuationAsync(parentAssetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParentAssetValuationDto?)null);

        var result = await _controller.GetParentAssetValuation(parentAssetId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetParentAssetValuation_WithZeroId_ReturnsNotFoundWhenServiceReturnsNull()
    {
        const long parentAssetId = 0;
        _mockService.Setup(s => s.GetParentAssetValuationAsync(parentAssetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParentAssetValuationDto?)null);

        var result = await _controller.GetParentAssetValuation(parentAssetId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetParentAssetValuation_CallsServiceExactlyOnce()
    {
        const long parentAssetId = 1;
        _mockService.Setup(s => s.GetParentAssetValuationAsync(parentAssetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParentAssetValuationDto { ParentAssetId = parentAssetId });

        await _controller.GetParentAssetValuation(parentAssetId, CancellationToken.None);

        _mockService.Verify(s => s.GetParentAssetValuationAsync(parentAssetId, It.IsAny<CancellationToken>()), Times.Once);
        _mockService.VerifyNoOtherCalls();
    }

    #endregion
}
