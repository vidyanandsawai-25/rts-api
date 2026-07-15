using Microsoft.AspNetCore.Mvc;
using Moq;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerSplitTests
{
    [Fact]
    public async Task SplitProperty_ReturnsOk_WhenSuccess()
    {
        // Arrange
        var setup = PropertyControllerTestHelper.CreateControllerWithMocks();
        var controller = setup.Controller;
        var mockPropertyService = setup.PropertyService;
        
        var dto = new PropertySplitCreateDto 
        { 
            PropertyNo = "P123", 
            NoOfSplit = 2, 
            UserId = 1, 
            WardId = 1, 
            IsPartitionProperty = true, 
            PartitionNo = "A1", 
            IsMainPropertyDataAttach = false, 
            CreatedBy = 1, 
            IsActive = true 
        };

        var expectedResult = new PropertySplitResultDto
        {
            Skipped = new List<PropertySpiltResponseDto>(),
            Created = new List<PropertySpiltResponseDto>
            {
                new PropertySpiltResponseDto { GeneratedPropertyNo = "P123", GeneratedPartitionNo = "A1B" },
                new PropertySpiltResponseDto { GeneratedPropertyNo = "P123", GeneratedPartitionNo = "A1C" }
            }
        };

        mockPropertyService
            .Setup(s => s.SplitProperty(It.IsAny<PropertySplitCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await controller.SplitProperty(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResult = Assert.IsType<PropertySplitResultDto>(okResult.Value);
        Assert.Equal(2, actualResult.Created.Count);
        Assert.Equal("A1B", actualResult.Created[0].GeneratedPartitionNo);
        Assert.Equal("A1C", actualResult.Created[1].GeneratedPartitionNo);
    }
}
