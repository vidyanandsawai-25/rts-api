using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerKycDetailsTests
{
    // KYC endpoints delegate to IPropertyKycService (per-tab service).
    private static PropertyController Create(out Mock<IPropertyKycService> service)
    {
        var propertyService = new Mock<IPropertyService>();
        service = new Mock<IPropertyKycService>();
        var logger = new Mock<ILogger<PropertyController>>();
        return PropertyControllerTestHelper.CreateController(propertyService, logger, kycService: service);
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

}
