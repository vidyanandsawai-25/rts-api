using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Master.MultilingualDetail;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class MultilingualTranslationControllerTests
{
    private static MultilingualTranslationController Create(out Mock<IMultilingualTranslation> service)
    {
        service = new Mock<IMultilingualTranslation>();
        var logger = new Mock<ILogger<MultilingualTranslationController>>();
        return new MultilingualTranslationController(service.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service);
        var query = new MultilingualTranslationQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<MultilingualTranslationDtos>(new List<MultilingualTranslationDtos>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task BulkUpdate_ReturnsOk()
    {
        var controller = Create(out var service);
        var items = new[] { new BulkUpdateItem<int, UpdateMultilingualTranslationDtos>(1, new UpdateMultilingualTranslationDtos()) };
        service.Setup(s => s.BulkUpdateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkResult<MultilingualTranslationDtos>(1, 0, new List<MultilingualTranslationDtos>()));

        var result = await controller.BulkUpdate(items, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetResources_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "ValidationMessages", "UIStrings" });

        var result = await controller.GetResources(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public void GetAutoTranslationConfig_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.IsAutoTranslationEnabled()).Returns(true);

        var result = controller.GetAutoTranslationConfig();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        service.Verify(s => s.IsAutoTranslationEnabled(), Times.Once);
    }
}
