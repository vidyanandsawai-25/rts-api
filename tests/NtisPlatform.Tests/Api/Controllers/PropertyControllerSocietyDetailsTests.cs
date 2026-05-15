using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerSocietyDetailsTests
{
    private static PropertyController Create(out Mock<IPropertyService> service)
    {
        service = new Mock<IPropertyService>();
        var logger = new Mock<ILogger<PropertyController>>();
        return new PropertyController(service.Object, logger.Object);
    }

    [Fact]
    public async Task GetSocietyDetails_ReturnsOk_WhenFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetSocietyDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertySocietyDetailsDto());

        var result = await controller.GetSocietyDetails(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetSocietyDetails_ReturnsNotFound_WhenMissing()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetSocietyDetailsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertySocietyDetailsDto?)null);

        var result = await controller.GetSocietyDetails(99, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetSocietyDetails_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetSocietyDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.GetSocietyDetails(1, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task UpdateSocietyDetails_ReturnsOk_WhenSuccess()
    {
        var controller = Create(out var service);
        var dto = new UpdatePropertySocietyDetailsDto();
        service.Setup(s => s.UpdateSocietyDetailsAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertySocietyDetailsDto());

        var result = await controller.UpdateSocietyDetails(1, dto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateSocietyDetails_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateSocietyDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertySocietyDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertySocietyDetailsDto?)null);

        var result = await controller.UpdateSocietyDetails(99, new UpdatePropertySocietyDetailsDto(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateSocietyDetails_ReturnsBadRequest_OnInvalidOperation()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateSocietyDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertySocietyDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("invalid"));

        var result = await controller.UpdateSocietyDetails(1, new UpdatePropertySocietyDetailsDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateSocietyDetails_Returns500_OnGenericException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateSocietyDetailsAsync(It.IsAny<int>(), It.IsAny<UpdatePropertySocietyDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var result = await controller.UpdateSocietyDetails(1, new UpdatePropertySocietyDetailsDto(), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }
}
