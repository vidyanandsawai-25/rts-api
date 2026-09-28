using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NtisPlatform.Application.DTOs.PropertyNumberDetails;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerNumberDetailsTests
{
    [Fact]
    public async Task GetPropertyNumberDetailsAsync_ShouldReturnOk_WithData()
    {
        // Arrange
        var setup = PropertyControllerTestHelper.CreateControllerWithMocks();
        var controller = setup.Controller;
        var propertyId = 1;
        var filter = "Next";

        var expectedDto = new PropertyNumberDetailsDto
        {
            WardId = 1,
            WardNo = "W1",
            PropertyNo = "100",
            PartitionNo = "A",
            Category = "Apartment",
            FlatOrShopNo = "10",
            WingDetailId = null,
            PropertyType = "Next"
        };

        setup.NumberDetailsService
            .Setup(x => x.GetAsync(propertyId, filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await controller.GetPropertyNumberDetailsAsync(propertyId, filter, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PropertyNumberDetailsDto>(okResult.Value);
        
        dto.Should().BeEquivalentTo(expectedDto);
        setup.NumberDetailsService.Verify(x => x.GetAsync(propertyId, filter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPropertyNumberDetailsAsync_ShouldReturnOk_WhenServiceReturnsNull()
    {
        // Arrange
        var setup = PropertyControllerTestHelper.CreateControllerWithMocks();
        var controller = setup.Controller;
        var propertyId = 1;
        var filter = "Next";

        setup.NumberDetailsService
            .Setup(x => x.GetAsync(propertyId, filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyNumberDetailsDto?)null);

        // Act
        var result = await controller.GetPropertyNumberDetailsAsync(propertyId, filter, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Null(okResult.Value);
        setup.NumberDetailsService.Verify(x => x.GetAsync(propertyId, filter, It.IsAny<CancellationToken>()), Times.Once);
    }
}
