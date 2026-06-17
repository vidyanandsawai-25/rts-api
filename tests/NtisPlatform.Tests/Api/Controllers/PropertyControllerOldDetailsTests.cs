using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerOldDetailsTests
{
    // Old Details endpoints delegate to IPropertyOldDetailsService (per-tab service).
    private static PropertyController Create(out Mock<IPropertyOldDetailsService> service)
    {
        var propertyService = new Mock<IPropertyService>();
        service = new Mock<IPropertyOldDetailsService>();
        var logger = new Mock<ILogger<PropertyController>>();
        return PropertyControllerTestHelper.CreateController(propertyService, logger, oldDetailsService: service);
    }

    [Fact]
    public async Task GetOldDetails_ReturnsOk_WhenFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetOldDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyOldDetailsDto());

        var result = await controller.GetOldDetails(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetOldDetails_ReturnsNotFound_WhenMissing()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetOldDetailsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyOldDetailsDto?)null);

        var result = await controller.GetOldDetails(99, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateOldDetails_ReturnsOk_WhenSuccess()
    {
        var controller = Create(out var service);
        var dto = new UpdatePropertyOldDetailsDto();
        service.Setup(s => s.UpdateOldDetailsAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyOldDetailsDto());

        var result = await controller.UpdateOldDetails(1, dto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateOldDetails_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateOldDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertyOldDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyOldDetailsDto?)null);

        var result = await controller.UpdateOldDetails(99, new UpdatePropertyOldDetailsDto(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

}
