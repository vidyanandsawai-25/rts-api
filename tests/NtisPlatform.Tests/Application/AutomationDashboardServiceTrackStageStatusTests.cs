using Moq;
using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;

namespace NtisPlatform.Tests.Application;

public class AutomationDashboardServiceTrackStageStatusTests
{
    [Fact]
    public async Task TrackStageStatusAsync_MapsRepositoryCompletionsToResponseFlags()
    {
        var stages = new List<PropertyWorkflowStageMasterEntity>
        {
            new() { Id = 1, StageName = "Geo Sequencing", UserId = 10, DisplayOrder = 1, IsActive = true },
            new() { Id = 2, StageName = "Assessment", UserId = 20, DisplayOrder = 2, IsActive = true }
        };
        var repository = new Mock<IAutomationDashboardRepository>();
        repository
            .Setup(x => x.ReadWorkflowStagesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stages);
        repository
            .Setup(x => x.ReadCompletedWorkflowStageIdsAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { 1 });
        repository
            .Setup(x => x.ReadWorkflowStageOfficerNamesAsync(
                It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 1, 2 })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string?> { [1] = "Geo Officer", [2] = "Assessment Officer" });

        var service = new AutomationDashboardService(
            repository.Object,
            Mock.Of<IGeoSequencingStageService>(),
            Mock.Of<IInternalSurveyStageService>(),
            Mock.Of<IDataEntryStageService>(),
            Mock.Of<IAssessmentStageService>());

        var result = await service.TrackStageStatusAsync(25, CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, result.Select(x => x.Id));
        Assert.Equal(new int?[] { 10, 20 }, result.Select(x => x.UserId));
        Assert.Equal("Geo Officer", result[0].OfficerName);
        Assert.Equal("Assessment Officer", result[1].OfficerName);
        Assert.Equal(1, result[0].IsCompleted);
        Assert.Equal(0, result[1].IsCompleted);
        repository.Verify(x => x.ReadWorkflowStagesAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.ReadCompletedWorkflowStageIdsAsync(25, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.ReadWorkflowStageOfficerNamesAsync(
            It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 1, 2 })),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
