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
    public async Task TrackStageStatusAsync_MapsRepositoryCompletionsToResponseFlags()
    {
        var completions = new List<WorkflowStageCompletionProjection>
        {
            new() { WorkflowStageId = 1, StageName = "Geo Sequencing", DisplayOrder = 1, IsCompleted = true },
            new() { WorkflowStageId = 2, StageName = "Assessment", DisplayOrder = 2, IsCompleted = false }
        };
        var repository = new Mock<IAutomationDashboardRepository>();
        repository
            .Setup(x => x.ReadWorkflowStageCompletionsAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(completions);

        var service = new AutomationDashboardService(
            repository.Object,
            Mock.Of<IGeoSequencingStageService>(),
            Mock.Of<IInternalSurveyStageService>(),
            Mock.Of<IDataEntryStageService>(),
            Mock.Of<IAssessmentStageService>());

        var result = await service.TrackStageStatusAsync(25, CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, result.Select(x => x.WorkflowStageId));
        Assert.Equal(1, result[0].IsCompleted);
        Assert.Equal(0, result[1].IsCompleted);
        repository.Verify(x => x.ReadWorkflowStageCompletionsAsync(25, It.IsAny<CancellationToken>()), Times.Once);
    }
}
