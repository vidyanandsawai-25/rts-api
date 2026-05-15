using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerKycDetailsTests
{
    private static PropertyController Create(out Mock<IPropertyService> service)
    {
        service = new Mock<IPropertyService>();
        var logger = new Mock<ILogger<PropertyController>>();
        return new PropertyController(service.Object, logger.Object);
    }

    [Fact]
    public async Task GetKycDetails_ReturnsOk_WhenFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetKycDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyKycDetailsDto());

        var result = await controller.GetKycDetails(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetKycDetails_ReturnsNotFound_WhenMissing()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetKycDetailsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyKycDetailsDto?)null);

        var result = await controller.GetKycDetails(99, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetKycDetails_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetKycDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.GetKycDetails(1, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task UpdateKycDetails_ReturnsOk_WhenSuccess()
    {
        var controller = Create(out var service);
        var dto = new UpdatePropertyKycDetailsDto();
        service.Setup(s => s.UpdateKycDetailsAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyKycDetailsDto());

        var result = await controller.UpdateKycDetails(1, dto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateKycDetails_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateKycDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertyKycDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyKycDetailsDto?)null);

        var result = await controller.UpdateKycDetails(99, new UpdatePropertyKycDetailsDto(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateKycDetails_ReturnsBadRequest_OnInvalidOperation()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateKycDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertyKycDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("invalid"));

        var result = await controller.UpdateKycDetails(1, new UpdatePropertyKycDetailsDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateKycDetails_Returns500_OnGenericException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateKycDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertyKycDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var result = await controller.UpdateKycDetails(1, new UpdatePropertyKycDetailsDto(), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }
}
