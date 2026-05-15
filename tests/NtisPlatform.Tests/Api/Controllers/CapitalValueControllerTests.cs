using Microsoft.AspNetCore.Mvc;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.CapitalValue;
using NtisPlatform.Application.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class CapitalValueControllerTests
{
    [Fact]
    public async Task Get_ReturnsOk_WithServiceResult()
    {
        var service = new Mock<ICapitalValueService>();
        service.Setup(s => s.GetAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CapitalValueDto>());
        var controller = new CapitalValueController(service.Object);

        var result = await controller.Get(7, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.GetAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }
}
