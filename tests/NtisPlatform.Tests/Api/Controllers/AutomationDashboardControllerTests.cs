using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Tests.Api.Controllers;

public class AutomationDashboardControllerTests
{
    private static AutomationDashboardController CreateController(Mock<IAutomationDashboardService> service)
        => new(
            service.Object,
            Mock.Of<ILogger<AutomationDashboardController>>());

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
        var response = Assert.IsType<AutomationDashboardItemsResponse<IReadOnlyList<TrackStageStatusDto>>>(ok.Value);
        Assert.Equal(stages, response.Items);
    }
}
