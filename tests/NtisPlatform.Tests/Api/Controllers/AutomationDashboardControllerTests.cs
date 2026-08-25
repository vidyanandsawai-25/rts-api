using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Master.PropertyWorkflowStageMaster;
using NtisPlatform.Application.Interfaces.AutomationDashboard;

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
        var stages = new List<PropertyWorkflowStageMasterDto>
        {
            new() { Id = 1, StageName = "GeoSequencing", OfficerName = "Geo Officer", DisplayOrder = 1, IsCompleted = 1 },
            new() { Id = 2, StageName = "InternalSurvey", OfficerName = "Survey Officer", DisplayOrder = 2, IsCompleted = 0 }
        };
        var service = new Mock<IAutomationDashboardService>();
        service
            .Setup(x => x.TrackStageStatusAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stages);
        var controller = CreateController(service);

        var result = await controller.TrackStageStatus(10, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AutomationDashboardItemsResponse<IReadOnlyList<PropertyWorkflowStageMasterDto>>>(ok.Value);
        Assert.Equal(stages, response.Items);
    }
}
