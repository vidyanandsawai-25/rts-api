using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerBasicDetailsTests
{
    // Basic Details endpoints now delegate to IPropertyBasicDetailsService (per-tab service),
    // so the controller tests drive that mock rather than the aggregate IPropertyService.
    private static PropertyController Create(out Mock<IPropertyBasicDetailsService> service)
    {
        var propertyService = new Mock<IPropertyService>();
        service = new Mock<IPropertyBasicDetailsService>();
        var logger = new Mock<ILogger<PropertyController>>();
        return PropertyControllerTestHelper.CreateController(propertyService, logger, service);
    }

    [Fact]
    public async Task GetBasicDetails_ReturnsOk_WhenFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetBasicDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyBasicDetailsDto());

        var result = await controller.GetBasicDetails(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetBasicDetails_ReturnsNotFound_WhenMissing()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetBasicDetailsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyBasicDetailsDto?)null);

        var result = await controller.GetBasicDetails(99, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBasicDetails_ReturnsOk_WhenSuccess()
    {
        var controller = Create(out var service);
        var dto = new UpdatePropertyBasicDetailsDto();
        service.Setup(s => s.UpdateBasicDetailsAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyBasicDetailsDto());

        var result = await controller.UpdateBasicDetails(1, dto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBasicDetails_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateBasicDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertyBasicDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyBasicDetailsDto?)null);

        var result = await controller.UpdateBasicDetails(99, new UpdatePropertyBasicDetailsDto(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

}
