using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerCombinePropertyTests
{
    private static PropertyController CreateController()
    {
        var service = new Mock<IPropertyService>();
        var logger = new Mock<ILogger<PropertyController>>();
        return new PropertyController(service.Object, logger.Object);
    }

    [Fact]
    public async Task GetAllCombineProperties_ReturnsOk()
    {
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var query = new CombinePropertyQueryParameters();
        combineService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CombinePropertyDto>(new List<CombinePropertyDto>(), 0, 1, 10));

        var result = await controller.GetAllCombineProperties(combineService.Object, query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetPropertyCombineDetails_ReturnsOk()
    {
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var query = new PropertyCombineDetailsQueryParameters();
        combineService.Setup(s => s.GetPropertyCombineDetailsAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCombineDetailsDto>());

        var result = await controller.GetPropertyCombineDetails(combineService.Object, query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetPropertyCombineDetails_Returns500_OnException()
    {
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        combineService.Setup(s => s.GetPropertyCombineDetailsAsync(It.IsAny<PropertyCombineDetailsQueryParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.GetPropertyCombineDetails(combineService.Object, new PropertyCombineDetailsQueryParameters(), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task CombineProperties_ReturnsOk_OnSuccess()
    {
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var request = new CombinePropertiesRequestDto { MainPropertyId = 1 };
        combineService.Setup(s => s.CombinePropertiesAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CombinePropertiesResponseDto { Success = true, Message = "ok" });

        var result = await controller.CombineProperties(combineService.Object, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CombineProperties_ReturnsBadRequest_WhenServiceReportsFailure()
    {
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var request = new CombinePropertiesRequestDto { MainPropertyId = 1 };
        combineService.Setup(s => s.CombinePropertiesAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CombinePropertiesResponseDto { Success = false, Message = "validation failed" });

        var result = await controller.CombineProperties(combineService.Object, request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CombineProperties_Returns500_OnException()
    {
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        combineService.Setup(s => s.CombinePropertiesAsync(It.IsAny<CombinePropertiesRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.CombineProperties(combineService.Object, new CombinePropertiesRequestDto { MainPropertyId = 1 }, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }
}
