using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories.AutomationDashboard;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

public class DataEntryStageRepositoryTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ReadDataEntryQueries_ApplyPropertyTypeIdFilterBeforeProjection()
    {
        using var context = CreateContext();
        SeedCommonData(context);
        await context.SaveChangesAsync();
        var repository = new DataEntryStageRepository(context);
        var request = new DashboardGridQueryParameters { PropertyTypeId = 10 };

        var stageProperties = await repository.ReadStagePropertiesForZonesAsync(3, new List<int> { 14 }, CancellationToken.None, request);
        var zoneTotals = await repository.ReadZoneTotalsAsync(new List<int> { 14 }, CancellationToken.None, request);
        var completedPhotos = await repository.ReadCompletedPhotosAsync(3, new List<int> { 14 }, 1, 2, CancellationToken.None, request);
        var propertyTypes = await repository.ReadPropertyTypesAsync(3, new List<int> { 14 }, CancellationToken.None, request);
        var propertyUses = await repository.ReadPropertyUsesAsync(3, new List<int> { 14 }, CancellationToken.None, request);
        var statusCounts = await repository.ReadAssessmentStatusCountsAsync(3, new List<int> { 14 }, CancellationToken.None, request);

        Assert.Equal(new[] { 100 }, stageProperties.Select(x => x.PropertyId));
        var total = Assert.Single(zoneTotals);
        Assert.Equal(1, total.Value.UnitCount);
        Assert.Equal(new[] { 100 }, completedPhotos.Select(x => x.PropertyId).Distinct());
        Assert.Equal(new[] { 100 }, propertyTypes.Select(x => x.PropertyId));
        Assert.Equal(new[] { 100 }, propertyUses.Select(x => x.PropertyId));
        Assert.Single(statusCounts);
    }

    [Fact]
    public async Task ReadStagePropertiesForZonesAsync_AppliesPropertyTypeCategoryFilter()
    {
        using var context = CreateContext();
        SeedCommonData(context);
        await context.SaveChangesAsync();
        var repository = new DataEntryStageRepository(context);

        var result = await repository.ReadStagePropertiesForZonesAsync(
            3,
            new List<int> { 14 },
            CancellationToken.None,
            new DashboardGridQueryParameters { PropertyTypeCategoryId = 3 });

        Assert.Equal(new[] { 101 }, result.Select(x => x.PropertyId));
    }

    private static void SeedCommonData(ApplicationDbContext context)
    {
        var createdDate = new DateTime(2026, 1, 1);
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
        context.PropertyAssessmentStatuses.Add(new PropertyAssessmentStatusEntity
        {
            Id = 1,
            StatusName = "Assessed",
            IsActive = true,
            CreatedDate = createdDate
        });
        context.PropertyTypeMasters.AddRange(
            new PropertyTypeMasterEntity
            {
                Id = 10,
                PropertyDescription = "Residential",
                Type = "R",
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertyTypeMasterEntity
            {
                Id = 20,
                PropertyDescription = "Mixed",
                Type = "R-C",
                IsActive = true,
                CreatedDate = createdDate
            });
        context.TypeOfUse.Add(new TypeOfUseEntity
        {
            Id = 1,
            Type = "R",
            TypeOfUseCode = "RES",
            Description = "Residential",
            IsActive = true,
            CreatedDate = createdDate
        });
        context.PropertyMast.AddRange(
            CreateProperty(100, 21, "115", 10, createdDate),
            CreateProperty(101, 21, "116", 20, createdDate));
        context.PropertyWorkflowDetails.AddRange(
            CreateWorkflowDetail(200, 100, 3, createdDate),
            CreateWorkflowDetail(201, 101, 3, createdDate));
        context.PropertyDetails.AddRange(
            CreatePropertyDetail(300, 100, createdDate),
            CreatePropertyDetail(301, 101, createdDate));
        context.PropertyPhotos.AddRange(
            PropertyPhotoEntity.Create(100, 1),
            PropertyPhotoEntity.Create(101, 1));
    }

    private static PropertyEntity CreateProperty(
        int id,
        int wardId,
        string propertyNo,
        int propertyTypeId,
        DateTime createdDate)
        => new()
        {
            Id = id,
            WardId = wardId,
            PropertyNo = propertyNo,
            PropertyTypeId = propertyTypeId,
            PropertyAssessmentStatusId = 1,
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
