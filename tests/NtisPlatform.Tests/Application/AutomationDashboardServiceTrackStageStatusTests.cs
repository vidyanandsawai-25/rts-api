using AutoMapper;
using Moq;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Tests.Application;

public class AutomationDashboardServiceTrackStageStatusTests
{
    [Fact]
    public async Task TrackStageStatusAsync_DelegatesToDashboardRepository()
    {
        var expected = new List<TrackStageStatusDto>
        {
            new() { WorkflowStageId = 1, StageName = "Geo Sequencing", DisplayOrder = 1, IsCompleted = 1 },
            new() { WorkflowStageId = 2, StageName = "Assessment", DisplayOrder = 2, IsCompleted = 0 }
        };
        var repository = new Mock<IAutomationDashboardRepository>();
        repository
            .Setup(x => x.TrackStageStatusAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new AutomationDashboardService(
            repository.Object,
            Mock.Of<IGeoSequencingStageService>(),
            Mock.Of<IInternalSurveyStageService>(),
            Mock.Of<IDataEntryStageService>(),
            Mock.Of<IAssessmentStageService>());

        var result = await service.TrackStageStatusAsync(25, CancellationToken.None);

        Assert.Same(expected, result);
        repository.Verify(x => x.TrackStageStatusAsync(25, It.IsAny<CancellationToken>()), Times.Once);
    }
}
