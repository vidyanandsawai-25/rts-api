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
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Asset_Management;

public class AssetPhotoControllerTests
{
    private static AssetPhotoController Create(
        out Mock<IAssetPhotoApplicationService> service,
        int? userId = 42)
    {
        service = new Mock<IAssetPhotoApplicationService>();
        var logger = new Mock<ILogger<AssetPhotoController>>();

        var controller = new AssetPhotoController(service.Object, logger.Object);

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

    #region GetPhotosByAsset

    [Fact]
    public async Task GetPhotosByAsset_ReturnsBadRequest_WhenAssetIdInvalid()
    {
        var controller = Create(out _);

        var result = await controller.GetPhotosByAsset(0, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetPhotosByAsset_ReturnsOk_WhenAssetIdValid()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetPhotosByAssetAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetPhotoDto>());

        var result = await controller.GetPhotosByAsset(123, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetPhotosByAsset_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetPhotosByAssetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("error"));

        var result = await controller.GetPhotosByAsset(123, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    #endregion

    #region GetGroupedPhotosByAsset

    [Fact]
    public async Task GetGroupedPhotosByAsset_ReturnsBadRequest_WhenAssetIdInvalid()
    {
        var controller = Create(out _);

        var result = await controller.GetGroupedPhotosByAsset(0, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetGroupedPhotosByAsset_ReturnsOk_WhenAssetIdValid()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetGroupedPhotosByAssetAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetPhotoGalleryDto { AssetId = 123 });

        var result = await controller.GetGroupedPhotosByAsset(123, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetGroupedPhotosByAsset_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetGroupedPhotosByAssetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("error"));

        var result = await controller.GetGroupedPhotosByAsset(123, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    #endregion

    #region GetPhotoTypesWithStatus

    [Fact]
    public async Task GetPhotoTypesWithStatus_ReturnsBadRequest_WhenAssetIdInvalid()
    {
        var controller = Create(out _);

        var result = await controller.GetPhotoTypesWithStatus(0, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetPhotoTypesWithStatus_ReturnsOk_WhenAssetIdValid()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetPhotoTypesWithStatusAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetPhotoTypeWithStatusDto>());

        var result = await controller.GetPhotoTypesWithStatus(123, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetPhotoTypesWithStatus_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetPhotoTypesWithStatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("error"));

        var result = await controller.GetPhotoTypesWithStatus(123, CancellationToken.None);

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

        var result = await controller.BulkSaveAll(new AssetPhotoBulkSaveDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task BulkSaveAll_ReturnsUnauthorized_WhenUserClaimMissing()
    {
        var controller = Create(out _, userId: null);

        var result = await controller.BulkSaveAll(new AssetPhotoBulkSaveDto { AssetId = 1 }, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task BulkSaveAll_ReturnsOk_WhenSuccessful()
    {
        var controller = Create(out var service);
        service.Setup(s => s.BulkSaveAllAsync(It.IsAny<AssetPhotoBulkSaveDto>(), 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetPhotoBulkSaveResponseDto
            {
                AssetId = 123,
                EnabledCount = 1,
                DisabledCount = 0,
                Errors = new List<string>()
            });

        var result = await controller.BulkSaveAll(new AssetPhotoBulkSaveDto { AssetId = 123 }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task BulkSaveAll_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.BulkSaveAllAsync(It.IsAny<AssetPhotoBulkSaveDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("error"));

        var result = await controller.BulkSaveAll(new AssetPhotoBulkSaveDto { AssetId = 123 }, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    #endregion
}
