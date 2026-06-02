using Microsoft.AspNetCore.Mvc;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.CapitalValue;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class CapitalValueControllerTests
{
    private readonly Mock<ICapitalValueService> _mockService;
    private readonly CapitalValueController _controller;

    public CapitalValueControllerTests()
    {
        _mockService = new Mock<ICapitalValueService>();
        _controller = new CapitalValueController(_mockService.Object);
    }

    [Fact]
    public async Task Get_ReturnsOk_WithData()
    {
        var mockData = new List<CapitalValueDto>
        {
            new CapitalValueDto { PropertyId = 1, PropertyDetailsId = 10, CapitalValue = 1000000 }
        };

        _mockService.Setup(x => x.GetAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockData);

        var result = await _controller.Get(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<CapitalValueDto>>(ok.Value);

        Assert.Single(data);
        Assert.Equal(1000000, data[0].CapitalValue);
    }

    [Fact]
    public async Task Get_PropertyNotFound_ThrowsException()
    {
        _mockService.Setup(x => x.GetAsync(999, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PropertyDetailsNotFoundException(999));

        await Assert.ThrowsAsync<PropertyDetailsNotFoundException>(() =>
            _controller.Get(999, CancellationToken.None));
    }
}
