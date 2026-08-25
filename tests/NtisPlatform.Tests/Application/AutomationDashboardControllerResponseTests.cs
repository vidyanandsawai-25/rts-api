using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Tests.Application;

public class AutomationDashboardControllerResponseTests
{
    [Fact]
    public async Task GetMainCards_ReturnsPayloadInsideItemsArray()
    {
        var service = new Mock<IAutomationDashboardService>();
        var expected = new MainCardsResponseDto
        {
            PreviouslyRegistered = new DashboardCardBreakdownDto { PropertyCount = 10, Demand = "1.25Cr" }
        };

        service
            .Setup(x => x.GetMainCardsAsync())
            .ReturnsAsync(expected);

        var controller = CreateController(service.Object);

        var actionResult = await controller.GetMainCards();
        var response = ExtractResponse<MainCardsResponseDto>(actionResult.Result);

        var item = Assert.Single(response.Items!);
        Assert.Equal(10, item.PreviouslyRegistered.PropertyCount);
        Assert.Equal("1.25Cr", item.PreviouslyRegistered.Demand);
    }

    [Fact]
    public async Task GetWorkflowCards_ReturnsListAsItemsArray()
    {
        var service = new Mock<IAutomationDashboardService>();
        service
            .Setup(x => x.GetWorkflowCardsAsync())
            .ReturnsAsync(new List<WorkflowStageCardDto>
            {
                new() { Id = 1, StageName = "GeoSequencing" },
                new() { Id = 2, StageName = "InternalSurvey" }
            });

        var controller = CreateController(service.Object);

        var actionResult = await controller.GetWorkflowCards();
        var response = ExtractResponse<WorkflowStageCardDto>(actionResult.Result);

        Assert.Equal(2, response.Items!.Count);
    }

    [Fact]
    public async Task SendToApprove_ReturnsMessageWithoutDataItems()
    {
        var service = new Mock<IAutomationDashboardService>();
        service
            .Setup(x => x.SendToApproveAsync(It.IsAny<SendToApproveRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendToApproveResponseDto());

        var controller = CreateController(service.Object);

        var actionResult = await controller.SendToApprove(
            new SendToApproveRequestDto { PropertyIds = new List<int> { 1, 2 }, UserId = 7 },
            CancellationToken.None);
        var response = ExtractResponse<object>(actionResult.Result);

        Assert.Empty(response.Items!);
    }

    private static AutomationDashboardController CreateController(IAutomationDashboardService service)
        => new(
            service,
            Mock.Of<ILogger<AutomationDashboardController>>());

    private static AutomationDashboardItemsResponse<IReadOnlyList<T>> ExtractResponse<T>(IActionResult? actionResult)
    {
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        return Assert.IsType<AutomationDashboardItemsResponse<IReadOnlyList<T>>>(okResult.Value);
    }
}
