using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerBasicDetailsTests
{
    private static PropertyController Create(out Mock<IPropertyService> service)
    {
        service = new Mock<IPropertyService>();
        var logger = new Mock<ILogger<PropertyController>>();
        return new PropertyController(service.Object, logger.Object);
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
    public async Task GetBasicDetails_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetBasicDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.GetBasicDetails(1, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
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

    [Fact]
    public async Task UpdateBasicDetails_ReturnsBadRequest_OnInvalidOperation()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateBasicDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertyBasicDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("FK"));

        var result = await controller.UpdateBasicDetails(1, new UpdatePropertyBasicDetailsDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBasicDetails_Returns500_OnGenericException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateBasicDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertyBasicDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var result = await controller.UpdateBasicDetails(1, new UpdatePropertyBasicDetailsDto(), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }
}
