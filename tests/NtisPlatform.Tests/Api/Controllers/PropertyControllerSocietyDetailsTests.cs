using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerSocietyDetailsTests
{
    // Society endpoints delegate to IPropertySocietyService (per-tab service).
    private static PropertyController Create(out Mock<IPropertySocietyService> service)
    {
        var propertyService = new Mock<IPropertyService>();
        service = new Mock<IPropertySocietyService>();
        var logger = new Mock<ILogger<PropertyController>>();
        return PropertyControllerTestHelper.CreateController(propertyService, logger, societyService: service);
    }

    // Amenity / wing-list endpoints stay on the aggregate IPropertyService (authored elsewhere, not refactored).
    private static PropertyController CreateForAggregate(out Mock<IPropertyService> service)
    {
        service = new Mock<IPropertyService>();
        var societyService = new Mock<IPropertySocietyService>();
        var logger = new Mock<ILogger<PropertyController>>();
        return PropertyControllerTestHelper.CreateController(service, logger, societyService: societyService);
    }

    #region GetSocietyDetails Tests

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

    #endregion

    #region UpdateSocietyDetails Tests

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

    #endregion

    #region GetSocietyAmenityDetailsAsync Tests

    [Fact]
    public async Task GetSocietyAmenityDetailsAsync_ReturnsOk_WhenFound()
    {
        var controller = CreateForAggregate(out var service);
        var expectedList = new List<SocietyAminityDetailsDto>
        {
            new() { SocietyDetailId = 1, PropertyId = 1, wingId = 1, WingNo = "A", WingName = "Wing A" },
            new() { SocietyDetailId = 1, PropertyId = 1, wingId = 2, WingNo = "B", WingName = "Wing B" }
        };
        service.Setup(s => s.GetSocietyAmenityDetailsAsync(1,true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        var result = await controller.GetSocietyAmenityDetailsAsync(1, true, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<SocietyAminityDetailsDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record fetched successfully", response.Message);
        Assert.Equal(2, response.Items?.Count);
    }

    [Fact]
    public async Task GetSocietyAmenityDetailsAsync_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateForAggregate(out var service);
        service.Setup(s => s.GetSocietyAmenityDetailsAsync(99, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<SocietyAminityDetailsDto>?)null);

        var result = await controller.GetSocietyAmenityDetailsAsync(99, true, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<SocietyAminityDetailsDto>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("99", response.Message);
    }

    [Fact]
    public async Task GetSocietyAmenityDetailsAsync_ReturnsOk_WithEmptyList()
    {
        var controller = CreateForAggregate(out var service);
        service.Setup(s => s.GetSocietyAmenityDetailsAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await controller.GetSocietyAmenityDetailsAsync(1, true, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<SocietyAminityDetailsDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Empty(response.Items!);
    }

    #endregion

    #region GetSocietyWingListAsync Tests

    [Fact]
    public async Task GetSocietyWingListAsync_ReturnsOk_WhenFound()
    {
        var controller = CreateForAggregate(out var service);
        var expectedList = new List<PropertySocietyDetailsDto>
        {
            new() { PropertyId = 1, WingId = 1, WingName = "Wing A" },
            new() { PropertyId = 1, WingId = 2, WingName = "Wing B" }
        };
        service.Setup(s => s.GetSocietyWingListAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        var result = await controller.GetSocietyWingListAsync(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<PropertySocietyDetailsDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record fetched successfully", response.Message);
        Assert.Equal(2, response.Items?.Count);
    }

    [Fact]
    public async Task GetSocietyWingListAsync_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateForAggregate(out var service);
        service.Setup(s => s.GetSocietyWingListAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<PropertySocietyDetailsDto>?)null);

        var result = await controller.GetSocietyWingListAsync(99, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertySocietyDetailsDto>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("99", response.Message);
    }

    [Fact]
    public async Task GetSocietyWingListAsync_ReturnsOk_WithEmptyList()
    {
        var controller = CreateForAggregate(out var service);
        service.Setup(s => s.GetSocietyWingListAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await controller.GetSocietyWingListAsync(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<PropertySocietyDetailsDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Empty(response.Items!);
    }

    [Fact]
    public async Task GetSocietyWingListAsync_PassesCancellationToken()
    {
        var controller = CreateForAggregate(out var service);
        var cts = new CancellationTokenSource();
        service.Setup(s => s.GetSocietyWingListAsync(1, cts.Token))
            .ReturnsAsync([]);

        await controller.GetSocietyWingListAsync(1, cts.Token);

        service.Verify(s => s.GetSocietyWingListAsync(1, cts.Token), Times.Once);
    }

    #endregion
}
