using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories.AutomationDashboard;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

public class AssessmentStageRepositoryTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetStagePropertiesAsync_LoadsRentedFlagSeparatelyForTotalGrid()
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
        context.ZoneMaster.Add(new ZoneEntity
        {
            Id = 14,
            ZoneNo = "Z14",
            Description = "Zone 14",
            IsActive = true,
            CreatedDate = createdDate
        });
        context.WardMaster.Add(new WardEntity
        {
            Id = 21,
            WardNo = "D11",
            ZoneId = 14,
            IsActive = true,
            CreatedDate = createdDate
        });
        context.PropertyMast.AddRange(
            CreateProperty(100, 21, "115", createdDate),
            CreateProperty(101, 21, "116", createdDate));
        context.PropertyWorkflowDetails.AddRange(
            CreateWorkflowDetail(200, 100, 4, createdDate),
            CreateWorkflowDetail(201, 101, 4, createdDate));
        context.PropertyDetails.AddRange(
            CreatePropertyDetail(300, 100, createdDate),
            CreatePropertyDetail(301, 101, createdDate));
        context.RenterMast.Add(new RenterMastEntity
        {
            Id = 400,
            PropertyDetailsId = 300,
            TaxLiability = " Renter ",
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = createdDate
        });
        await context.SaveChangesAsync();
        var repository = new AssessmentStageRepository(context);

        var result = await repository.GetStagePropertiesAsync(4, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.True(result.Single(x => x.PropertyId == 100).IsRented);
        Assert.False(result.Single(x => x.PropertyId == 101).IsRented);
        Assert.All(result, x =>
        {
            Assert.Equal(14, x.ZoneId);
            Assert.Equal("Z14", x.ZoneNo);
        });
    }

    private static PropertyEntity CreateProperty(
        int id,
        int wardId,
        string propertyNo,
        DateTime createdDate)
        => new()
        {
            Id = id,
            WardId = wardId,
            PropertyNo = propertyNo,
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

    private static PropertyDetailsEntity CreatePropertyDetail(
        int id,
        int propertyId,
        DateTime createdDate)
        => new()
        {
            Id = id,
            PropertyId = propertyId,
            TypeOfUseId = 1,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = createdDate
        };
}
