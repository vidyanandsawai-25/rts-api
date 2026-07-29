using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories.AutomationDashboard;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

public class AutomationDashboardRepositoryTrackStageStatusTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task TrackStageStatusAsync_ReturnsActiveStagesWithCompletionFlags()
    {
        using var context = CreateContext();
        var createdDate = new DateTime(2026, 1, 1);
        context.PropertyWorkflowStageMaster.AddRange(
            new PropertyWorkflowStageMasterEntity
            {
                Id = 2,
                StageName = "Assessment",
                DisplayOrder = 2,
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertyWorkflowStageMasterEntity
            {
                Id = 1,
                StageName = "Geo Sequencing",
                DisplayOrder = 1,
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertyWorkflowStageMasterEntity
            {
                Id = 3,
                StageName = "Inactive",
                DisplayOrder = 3,
                IsActive = false,
                CreatedDate = createdDate
            });
        context.PropertyWorkflowDetails.AddRange(
            new PropertyWorkflowDetailsEntity
            {
                Id = 10,
                PropertyId = 100,
                WorkflowStageId = 1,
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertyWorkflowDetailsEntity
            {
                Id = 11,
                PropertyId = 100,
                WorkflowStageId = 3,
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertyWorkflowDetailsEntity
            {
                Id = 12,
                PropertyId = 100,
                WorkflowStageId = 2,
                IsActive = false,
                CreatedDate = createdDate
            },
            new PropertyWorkflowDetailsEntity
            {
                Id = 13,
                PropertyId = 200,
                WorkflowStageId = 2,
                IsActive = true,
                CreatedDate = createdDate
            });
        await context.SaveChangesAsync();
        var repository = new AutomationDashboardRepository(context);

        var result = await repository.TrackStageStatusAsync(100, CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, result.Select(x => x.WorkflowStageId));
        Assert.Equal(1, result[0].IsCompleted);
        Assert.Equal(0, result[1].IsCompleted);
    }
}
