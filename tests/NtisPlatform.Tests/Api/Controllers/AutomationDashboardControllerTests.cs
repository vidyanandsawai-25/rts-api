using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Tests.Api.Controllers;

public class AutomationDashboardControllerTests
{
    private static AutomationDashboardController CreateController(Mock<IAutomationDashboardService> service)
        => new(Mock.Of<ILogger<AutomationDashboardController>>(), service.Object);

    [Fact]
    public async Task TrackStageStatus_WithInvalidPropertyId_ReturnsBadRequest()
    {
        var service = new Mock<IAutomationDashboardService>();
        var controller = CreateController(service);

        var result = await controller.TrackStageStatus(0, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("PropertyId parameter is required", response.Message);
        service.Verify(
            x => x.TrackStageStatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TrackStageStatus_WithValidPropertyId_ReturnsWorkflowStages()
    {
        var stages = new List<TrackStageStatusDto>
        {
            new() { WorkflowStageId = 1, StageName = "GeoSequencing", DisplayOrder = 1, IsCompleted = 1 },
            new() { WorkflowStageId = 2, StageName = "InternalSurvey", DisplayOrder = 2, IsCompleted = 0 }
        };
        var service = new Mock<IAutomationDashboardService>();
        service
            .Setup(x => x.TrackStageStatusAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stages);
        var controller = CreateController(service);

        var result = await controller.TrackStageStatus(10, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<List<TrackStageStatusDto>>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal(stages, response.Items);
        Assert.Equal("Workflow stage status retrieved successfully", response.Message);
    }
}
