using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Models;
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

    [Fact]
    public async Task GetWorkflowCardsAsync_UsesGridStructureAndUnitCounting()
    {
        using var context = CreateContext();
        var createdDate = new DateTime(2026, 1, 1);
        context.PropertyWorkflowStageMaster.Add(new PropertyWorkflowStageMasterEntity
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            IsActive = true,
            CreatedDate = createdDate
        });
        context.ZoneMaster.AddRange(
            new ZoneEntity
            {
                Id = 1,
                ZoneNo = "Z1",
                Description = "Zone 1",
                IsActive = true,
                CreatedDate = createdDate
            },
            new ZoneEntity
            {
                Id = 2,
                ZoneNo = "Z2",
                Description = "Inactive Zone",
                IsActive = false,
                CreatedDate = createdDate
            });
        context.WardMaster.AddRange(
            new WardEntity
            {
                Id = 1,
                WardNo = "W1",
                ZoneId = 1,
                IsActive = true,
                CreatedDate = createdDate
            },
            new WardEntity
            {
                Id = 2,
                WardNo = "W2",
                ZoneId = 1,
                IsActive = false,
                CreatedDate = createdDate
            },
            new WardEntity
            {
                Id = 3,
                WardNo = "W3",
                ZoneId = 2,
                IsActive = true,
                CreatedDate = createdDate
            });
        context.PropertyMast.AddRange(
            new PropertyEntity
            {
                Id = 100,
                WardId = 1,
                PropertyNo = "3",
                PartitionNo = "",
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = createdDate
            },
            new PropertyEntity
            {
                Id = 101,
                WardId = 1,
                PropertyNo = "3",
                PartitionNo = "1",
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = createdDate
            },
            new PropertyEntity
            {
                Id = 102,
                WardId = 1,
                PropertyNo = "3",
                PartitionNo = "2",
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = createdDate
            },
            new PropertyEntity
            {
                Id = 103,
                WardId = 2,
                PropertyNo = "4",
                PartitionNo = "",
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = createdDate
            },
            new PropertyEntity
            {
                Id = 104,
                WardId = 3,
                PropertyNo = "5",
                PartitionNo = "",
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = createdDate
            });
        context.PropertyWorkflowDetails.AddRange(
            new PropertyWorkflowDetailsEntity
            {
                Id = 20,
                PropertyId = 100,
                WorkflowStageId = 1,
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertyWorkflowDetailsEntity
            {
                Id = 21,
                PropertyId = 101,
                WorkflowStageId = 1,
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertyWorkflowDetailsEntity
            {
                Id = 22,
                PropertyId = 102,
                WorkflowStageId = 1,
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertyWorkflowDetailsEntity
            {
                Id = 23,
                PropertyId = 103,
                WorkflowStageId = 1,
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertyWorkflowDetailsEntity
            {
                Id = 24,
                PropertyId = 104,
                WorkflowStageId = 1,
                IsActive = true,
                CreatedDate = createdDate
            });
        await context.SaveChangesAsync();
        var repository = new AutomationDashboardRepository(context);

        var result = await repository.GetWorkflowCardsAsync(null, CancellationToken.None);

        var card = Assert.Single(result);
        Assert.Equal(1, card.StructureCount);
        Assert.Equal(3, card.UnitCount);
    }

    [Fact]
    public async Task GetPendingAssessmentPropsAsync_AppliesFiltersBeforePaging()
    {
        using var context = CreateContext();
        var createdDate = new DateTime(2026, 1, 1);
        context.PropertyWorkflowStageMaster.Add(new PropertyWorkflowStageMasterEntity
        {
            Id = 4,
            StageName = "Assessment",
            DisplayOrder = 4,
            IsActive = true,
            CreatedDate = createdDate
        });
        context.ZoneMaster.AddRange(
            new ZoneEntity
            {
                Id = 14,
                ZoneNo = "Z14",
                Description = "Zone 14",
                IsActive = true,
                CreatedDate = createdDate
            },
            new ZoneEntity
            {
                Id = 15,
                ZoneNo = "Z15",
                Description = "Zone 15",
                IsActive = true,
                CreatedDate = createdDate
            });
        context.WardMaster.AddRange(
            new WardEntity
            {
                Id = 21,
                WardNo = "D11",
                ZoneId = 14,
                IsActive = true,
                CreatedDate = createdDate
            },
            new WardEntity
            {
                Id = 22,
                WardNo = "D12",
                ZoneId = 15,
                IsActive = true,
                CreatedDate = createdDate
            });
        context.PropertyAssessmentStatuses.AddRange(
            new PropertyAssessmentStatusEntity
            {
                Id = 1,
                StatusName = "Assessed",
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertyAssessmentStatusEntity
            {
                Id = 2,
                StatusName = "Unassessed",
                IsActive = true,
                CreatedDate = createdDate
            });
        context.PropertyTypeMasters.AddRange(
            new PropertyTypeMasterEntity
            {
                Id = 10,
                PropertyDescription = "Mixed Shop",
                Type = "R-C",
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertyTypeMasterEntity
            {
                Id = 11,
                PropertyDescription = "Residential House",
                Type = "R",
                IsActive = true,
                CreatedDate = createdDate
            });
        context.PropertyMast.AddRange(
            CreatePendingAssessmentProperty(100, 21, "115", "Target Owner", 1, 10, createdDate),
            CreatePendingAssessmentProperty(101, 21, "116", "Target Signed", 1, 10, createdDate),
            CreatePendingAssessmentProperty(102, 21, "117", "Target Wrong Description", 1, 11, createdDate),
            CreatePendingAssessmentProperty(103, 21, "118", "Target Wrong Status", 2, 10, createdDate),
            CreatePendingAssessmentProperty(104, 22, "119", "Target Wrong Zone", 1, 10, createdDate));
        context.PropertyWorkflowDetails.AddRange(
            CreateWorkflowDetail(200, 100, 4, createdDate),
            CreateWorkflowDetail(201, 101, 4, createdDate),
            CreateWorkflowDetail(202, 102, 4, createdDate),
            CreateWorkflowDetail(203, 103, 4, createdDate),
            CreateWorkflowDetail(204, 104, 4, createdDate));
        context.PropertySignatureDetails.Add(new PropertySignatureDetailsEntity
        {
            Id = 300,
            PropertyId = 101,
            UserId = 1,
            SignAuthorityId = 1,
            IsActive = true,
            CreatedDate = createdDate
        });
        await context.SaveChangesAsync();
        var repository = new AutomationDashboardRepository(context);

        var result = await repository.GetPendingAssessmentPropsAsync(
            new SubGridFilterRequestDto
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "Target",
                SurveyTypeId = 1,
                ZoneNo = "Z14",
                WardNo = "D11",
                PropertyTypeId = 10
            },
            CancellationToken.None);

        var property = Assert.Single(result.Properties);
        Assert.Equal(100, property.Id);
        Assert.Equal(21, property.WardId);
        Assert.Equal("D11", property.WardNo);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(14, result.ZoneId);
        Assert.Equal("Z14", result.ZoneNo);
        Assert.Equal(21, result.WardId);
        Assert.Equal("D11", result.WardNo);
    }

    private static PropertyEntity CreatePendingAssessmentProperty(
        int id,
        int wardId,
        string propertyNo,
        string ownerName,
        int assessmentStatusId,
        int propertyTypeId,
        DateTime createdDate)
        => new()
        {
            Id = id,
            WardId = wardId,
            PropertyNo = propertyNo,
            OwnerName = ownerName,
            PropertyAssessmentStatusId = assessmentStatusId,
            PropertyTypeId = propertyTypeId,
            PartitionNo = "",
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = createdDate
        };

    private static PropertyWorkflowDetailsEntity CreateWorkflowDetail(
        int id,
        int propertyId,
        int workflowStageId,
        DateTime createdDate)
        => new()
        {
            Id = id,
            PropertyId = propertyId,
            WorkflowStageId = workflowStageId,
            IsActive = true,
            CreatedDate = createdDate
        };
}
