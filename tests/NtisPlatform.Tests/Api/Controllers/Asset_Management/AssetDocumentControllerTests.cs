using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Asset_Management;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDocument;
using NtisPlatform.Application.Interfaces.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Asset_Management;

public class AssetDocumentControllerTests
{
    private static AssetDocumentController Create(
        out Mock<IAssetDocumentApplicationService> service,
        int? userId = 42)
    {
        service = new Mock<IAssetDocumentApplicationService>();
        var logger = new Mock<ILogger<AssetDocumentController>>();

        var controller = new AssetDocumentController(service.Object, logger.Object);

        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
            }, "TestAuth"));
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    #region GetDocumentsByAsset

    [Fact]
    public async Task GetDocumentsByAsset_ReturnsBadRequest_WhenAssetIdInvalid()
    {
        var controller = Create(out _);

        var result = await controller.GetDocumentsByAsset(0, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetDocumentsByAsset_ReturnsOk_WhenAssetIdValid()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetDocumentsByAssetAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentDto>());

        var result = await controller.GetDocumentsByAsset(123, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetDocumentsByAsset_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetDocumentsByAssetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("error"));

        var result = await controller.GetDocumentsByAsset(123, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    #endregion

    #region GetGroupedDocumentsByAsset

    [Fact]
    public async Task GetGroupedDocumentsByAsset_ReturnsBadRequest_WhenAssetIdInvalid()
    {
        var controller = Create(out _);

        var result = await controller.GetGroupedDocumentsByAsset(0, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetGroupedDocumentsByAsset_ReturnsOk_WhenAssetIdValid()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetGroupedDocumentsByAssetAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDocumentGalleryDto { AssetId = 123 });

        var result = await controller.GetGroupedDocumentsByAsset(123, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetGroupedDocumentsByAsset_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetGroupedDocumentsByAssetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("error"));

        var result = await controller.GetGroupedDocumentsByAsset(123, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    #endregion

    #region GetDocumentTypesWithStatus

    [Fact]
    public async Task GetDocumentTypesWithStatus_ReturnsBadRequest_WhenAssetIdInvalid()
    {
        var controller = Create(out _);

        var result = await controller.GetDocumentTypesWithStatus(0, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetDocumentTypesWithStatus_ReturnsOk_WhenAssetIdValid()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetDocumentTypesWithStatusAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentTypeWithStatusDto>());

        var result = await controller.GetDocumentTypesWithStatus(123, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetDocumentTypesWithStatus_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetDocumentTypesWithStatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("error"));

        var result = await controller.GetDocumentTypesWithStatus(123, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    #endregion

    #region BulkSaveAll

    [Fact]
    public async Task BulkSaveAll_ReturnsBadRequest_WhenModelStateInvalid()
    {
        var controller = Create(out _);
        controller.ModelState.AddModelError("AssetId", "AssetId required");

        var result = await controller.BulkSaveAll(new AssetDocumentBulkSaveDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task BulkSaveAll_ReturnsUnauthorized_WhenUserClaimMissing()
    {
        var controller = Create(out _, userId: null);

        var result = await controller.BulkSaveAll(new AssetDocumentBulkSaveDto { AssetId = 1 }, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task BulkSaveAll_ReturnsOk_WhenSuccessful()
    {
        var controller = Create(out var service);
        service.Setup(s => s.BulkSaveAllAsync(It.IsAny<AssetDocumentBulkSaveDto>(), 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDocumentBulkSaveResponseDto
            {
                AssetId = 123,
                EnabledCount = 1,
                DisabledCount = 0,
                Errors = new List<string>()
            });

        var result = await controller.BulkSaveAll(new AssetDocumentBulkSaveDto { AssetId = 123 }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task BulkSaveAll_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.BulkSaveAllAsync(It.IsAny<AssetDocumentBulkSaveDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("error"));

        var result = await controller.BulkSaveAll(new AssetDocumentBulkSaveDto { AssetId = 123 }, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    #endregion
}
