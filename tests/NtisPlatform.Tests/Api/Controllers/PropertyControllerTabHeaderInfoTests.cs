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

public class PropertyControllerTabHeaderInfoTests
{
    private static PropertyController Create(out Mock<IPropertyOldDetailsService> service)
    {
        var propertyService = new Mock<IPropertyService>();
        service = new Mock<IPropertyOldDetailsService>();
        var logger = new Mock<ILogger<PropertyController>>();
        return PropertyControllerTestHelper.CreateController(propertyService, logger, oldDetailsService: service);
    }

    [Fact]
    public async Task GetTabHeaderInfo_ReturnsOk_WhenFound()
    {
        var controller = Create(out var service);
        var expectedDto = new PropertyTabHeaderInfoDto
        {
            PropertyId = 1,
            StatusName = "UNASSESSED",
            OldWardNo = "MM-17",
            OldPropertyNo = "500722",
            OldPartitionNo = null,
            Description = "Residential Property",
            Type = "RES",
            Category = "Category A",
            UPICId = "MM0100850000UPIC",
            OwnerName = "John Doe",
            Address = "123 Main St",
            TypeOfUse = "Residential"
        };
        service.Setup(s => s.GetTabHeaderInfoAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var result = await controller.GetTabHeaderInfo(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertyTabHeaderInfoDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Record fetched successfully", apiResponse.Message);
        Assert.Equal(expectedDto, apiResponse.Items);
    }

    [Fact]
    public async Task GetTabHeaderInfo_ReturnsNotFound_WhenMissing()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetTabHeaderInfoAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTabHeaderInfoDto?)null);

        var result = await controller.GetTabHeaderInfo(99, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertyTabHeaderInfoDto>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("Property with ID 99 not found", apiResponse.Message);
    }
}
