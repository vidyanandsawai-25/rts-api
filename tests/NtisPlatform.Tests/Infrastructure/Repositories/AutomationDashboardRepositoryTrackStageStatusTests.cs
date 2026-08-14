using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;
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
    public async Task ReadPreviouslyRegisteredBreakdownAsync_CalculatesPreviouslyRegisteredFromPropertyMastOld()
    {
        using var context = CreateContext();
        var createdDate = new DateTime(2026, 1, 1);
        context.PropertyMastOld.AddRange(
            new PropertyMastOldEntity
            {
                Id = 1,
                OldPropertyNo = "100",
                OldPartitionNo = null,
                OldTotalTax = 1000,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = createdDate
            },
            new PropertyMastOldEntity
            {
                Id = 2,
                OldPropertyNo = "100",
                OldPartitionNo = "1",
                OldTotalTax = 250,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = createdDate
            },
            new PropertyMastOldEntity
            {
                Id = 3,
                OldPropertyNo = "101",
                OldPartitionNo = "",
                OldTotalTax = 500,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = createdDate
            },
            new PropertyMastOldEntity
            {
                Id = 4,
                OldPropertyNo = "102",
                OldPartitionNo = "1",
                OldTotalTax = 700,
                IsActive = false,
                MarkedForDeletion = false,
                CreatedDate = createdDate
            },
            new PropertyMastOldEntity
            {
                Id = 5,
                OldPropertyNo = "103",
                OldPartitionNo = null,
                OldTotalTax = 900,
                IsActive = true,
                MarkedForDeletion = true,
                CreatedDate = createdDate
            });
        await context.SaveChangesAsync();
        var repository = new AutomationDashboardRepository(context);

        var result = await repository.ReadPreviouslyRegisteredBreakdownAsync(CancellationToken.None);

        Assert.Equal(3, result.PropertyCount);
        Assert.Equal(2, result.StructureCount);
        Assert.Equal(3, result.UnitCount);
        Assert.Equal(1750m, result.Demand);
    }

    [Fact]
    public async Task ReadAcdApprovedPropertyBreakdownAsync_CalculatesAdditionalRevenueFromAcdApprovedSignatures()
    {
        using var context = CreateContext();
        var createdDate = new DateTime(2026, 1, 1);
        context.PropertyMast.AddRange(
            CreateSubGridProperty(100, 21, "1", "", "Approved Main", createdDate),
            CreateSubGridProperty(101, 21, "1", "1", "Approved Partition", createdDate),
            CreateSubGridProperty(102, 21, "2", "", "Wrong Status", createdDate),
            CreateSubGridProperty(103, 21, "3", "", "Inactive Signature", createdDate),
            CreateSubGridProperty(104, 21, "4", "", "Inactive Property", createdDate, isActive: false));
        context.PropertySignatureDetails.AddRange(
            new PropertySignatureDetailsEntity
            {
                Id = 1,
                PropertyId = 100,
                UserId = 1,
                SignAuthorityId = 1,
                SignStatus = "ApprovedByACD",
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertySignatureDetailsEntity
            {
                Id = 2,
                PropertyId = 100,
                UserId = 2,
                SignAuthorityId = 2,
                SignStatus = "ApprovedByACD",
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertySignatureDetailsEntity
            {
                Id = 3,
                PropertyId = 101,
                UserId = 1,
                SignAuthorityId = 1,
                SignStatus = "ApprovedByACD",
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertySignatureDetailsEntity
            {
                Id = 4,
                PropertyId = 102,
                UserId = 1,
                SignAuthorityId = 1,
                SignStatus = "PendingToACD",
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertySignatureDetailsEntity
            {
                Id = 5,
                PropertyId = 103,
                UserId = 1,
                SignAuthorityId = 1,
                SignStatus = "ApprovedByACD",
                IsActive = false,
                CreatedDate = createdDate
            },
            new PropertySignatureDetailsEntity
            {
                Id = 6,
                PropertyId = 104,
                UserId = 1,
                SignAuthorityId = 1,
                SignStatus = "ApprovedByACD",
                IsActive = true,
                CreatedDate = createdDate
            });
        context.TaxMaster.AddRange(
            new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "TaxTotal",
                TaxName = "TaxTotal",
                TaxCategoryId = 1,
                IsActive = true,
                CreatedDate = createdDate
            },
            new TaxMasterEntity
            {
                Id = 2,
                TaxCode = "GENERAL",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                IsActive = true,
                CreatedDate = createdDate
            });
        context.TransMast.AddRange(
            CreateTransMast(1, 100, 1, 1000m, createdDate),
            CreateTransMast(2, 100, 2, 50m, createdDate),
            CreateTransMast(3, 101, 1, 250m, createdDate),
            CreateTransMast(4, 102, 1, 700m, createdDate),
            CreateTransMast(5, 104, 1, 900m, createdDate));
        await context.SaveChangesAsync();
        var repository = new AutomationDashboardRepository(context);

        var result = await repository.ReadAcdApprovedPropertyBreakdownAsync(cancellationToken: CancellationToken.None);

        Assert.Equal(2, result.PropertyCount);
        Assert.Equal(1, result.StructureCount);
        Assert.Equal(2, result.UnitCount);
        Assert.Equal(1250m, result.Demand);
    }

    [Fact]
    public async Task ReadWorkflowStageCompletionsAsync_ReturnsActiveStagesWithCompletionFlags()
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

        var result = await repository.ReadWorkflowStageCompletionsAsync(100, CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, result.Select(x => x.WorkflowStageId));
        Assert.True(result[0].IsCompleted);
        Assert.False(result[1].IsCompleted);
    }

    [Fact]
    public async Task ReadWorkflowStageCountsAsync_UsesGridStructureAndUnitCounting()
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

        var stages = await repository.ReadWorkflowStagesAsync(cancellationToken: CancellationToken.None);
        var result = await repository.ReadWorkflowStageCountsAsync(
            stages.Select(s => s.WorkflowStageId),
            null,
            CancellationToken.None);

        var card = Assert.Single(result.Values);
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
            new PendingAssessmentQueryParameters
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

    [Fact]
    public async Task GetSubGridDataAsync_FiltersFormattedWardPropertyAndPartitionNumber()
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
        context.ZoneMaster.Add(new ZoneEntity
        {
            Id = 14,
            ZoneNo = "MM",
            Description = "MM",
            IsActive = true,
            CreatedDate = createdDate
        });
        context.WardMaster.AddRange(
            new WardEntity
            {
                Id = 21,
                WardNo = "MMMAJOR4",
                ZoneId = 14,
                IsActive = true,
                CreatedDate = createdDate
            },
            new WardEntity
            {
                Id = 22,
                WardNo = "MMMAJOR5",
                ZoneId = 14,
                IsActive = true,
                CreatedDate = createdDate
            });
        context.PropertyMast.AddRange(
            CreateSubGridProperty(100, 21, "8", "", "Main Owner", createdDate),
            CreateSubGridProperty(101, 21, "8", "1", "Partition Owner", createdDate),
            CreateSubGridProperty(102, 21, "8", "2", "Second Partition Owner", createdDate),
            CreateSubGridProperty(103, 22, "8", "", "Other Ward Owner", createdDate));
        context.PropertyWorkflowDetails.AddRange(
            CreateWorkflowDetail(200, 100, 1, createdDate),
            CreateWorkflowDetail(201, 101, 1, createdDate),
            CreateWorkflowDetail(202, 102, 1, createdDate),
            CreateWorkflowDetail(203, 103, 1, createdDate));
        await context.SaveChangesAsync();
        var repository = new AutomationDashboardRepository(context);

        var mainResult = await repository.GetSubGridDataAsync(
            new SubGridQueryParameters
            {
                WorkflowStageId = 1,
                PropertyNo = "MMMAJOR4-8",
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);
        var partitionResult = await repository.GetSubGridDataAsync(
            new SubGridQueryParameters
            {
                WorkflowStageId = 1,
                PropertyNo = "MMMAJOR4-8-1",
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        var mainProperty = Assert.Single(mainResult.Properties);
        Assert.Equal(100, mainProperty.Id);
        Assert.Equal(1, mainResult.TotalCount);

        var partitionProperty = Assert.Single(partitionResult.Properties);
        Assert.Equal(101, partitionProperty.Id);
        Assert.Equal("1", partitionProperty.PartitionNo);
        Assert.Equal(1, partitionResult.TotalCount);
    }

    [Fact]
    public async Task GetSubGridDataAsync_FiltersStructureAndUnitRows()
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
        context.ZoneMaster.Add(new ZoneEntity
        {
            Id = 14,
            ZoneNo = "MM",
            Description = "MM",
            IsActive = true,
            CreatedDate = createdDate
        });
        context.WardMaster.Add(new WardEntity
        {
            Id = 21,
            WardNo = "MMMAJOR4",
            ZoneId = 14,
            IsActive = true,
            CreatedDate = createdDate
        });
        context.PropertyMast.AddRange(
            CreateSubGridProperty(100, 21, "8", "", "Main Owner", createdDate),
            CreateSubGridProperty(101, 21, "8", "1", "Partition Owner", createdDate),
            CreateSubGridProperty(102, 21, "8", "2", "Second Partition Owner", createdDate));
        context.PropertyWorkflowDetails.AddRange(
            CreateWorkflowDetail(200, 100, 1, createdDate),
            CreateWorkflowDetail(201, 101, 1, createdDate),
            CreateWorkflowDetail(202, 102, 1, createdDate));
        await context.SaveChangesAsync();
        var repository = new AutomationDashboardRepository(context);

        var structureResult = await repository.GetSubGridDataAsync(
            new SubGridQueryParameters
            {
                ZoneId = 14,
                WorkflowStageId = 1,
                Structure = true,
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);
        var unitResult = await repository.GetSubGridDataAsync(
            new SubGridQueryParameters
            {
                ZoneId = 14,
                WorkflowStageId = 1,
                Unit = true,
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);
        var allResult = await repository.GetSubGridDataAsync(
            new SubGridQueryParameters
            {
                ZoneId = 14,
                WorkflowStageId = 1,
                Structure = true,
                Unit = true,
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        Assert.Equal(new[] { 100 }, structureResult.Properties.Select(p => p.Id).ToArray());
        Assert.Equal(1, structureResult.TotalCount);
        Assert.Equal(new[] { 100, 101, 102 }, unitResult.Properties.Select(p => p.Id).ToArray());
        Assert.Equal(3, unitResult.TotalCount);
        Assert.Equal(new[] { 100, 101, 102 }, allResult.Properties.Select(p => p.Id).ToArray());
        Assert.Equal(3, allResult.TotalCount);
    }

    public static TheoryData<string, int[]> DataEntrySubGridMetricFilterCases => new()
    {
        { nameof(SubGridQueryParameters.PendingStructure), new[] { 402 } },
        { nameof(SubGridQueryParameters.PendingUnit), new[] { 402, 403 } },
        { nameof(SubGridQueryParameters.CompletedStructure), new[] { 400 } },
        { nameof(SubGridQueryParameters.CompletedUnit), new[] { 400, 401 } }
    };

    [Theory]
    [MemberData(nameof(DataEntrySubGridMetricFilterCases))]
    public async Task GetSubGridDataAsync_FiltersDataEntryCompletedAndPendingMetricRows(
        string filterName,
        int[] expectedPropertyIds)
    {
        using var context = CreateContext();
        var createdDate = new DateTime(2026, 1, 1);
        context.PropertyWorkflowStageMaster.Add(new PropertyWorkflowStageMasterEntity
        {
            Id = 2,
            StageName = "DataEntry",
            DisplayOrder = 2,
            IsActive = true,
            CreatedDate = createdDate
        });
        context.ZoneMaster.Add(new ZoneEntity
        {
            Id = 14,
            ZoneNo = "MM",
            Description = "MM",
            IsActive = true,
            CreatedDate = createdDate
        });
        context.WardMaster.Add(new WardEntity
        {
            Id = 21,
            WardNo = "D1",
            ZoneId = 14,
            IsActive = true,
            CreatedDate = createdDate
        });
        context.PropertyMast.AddRange(
            CreateSubGridProperty(400, 21, "1", "", "Completed Structure", createdDate),
            CreateSubGridProperty(401, 21, "1", "1", "Completed Unit", createdDate),
            CreateSubGridProperty(402, 21, "2", "", "Pending Structure", createdDate),
            CreateSubGridProperty(403, 21, "2", "1", "Pending Unit", createdDate));
        context.PropertyWorkflowDetails.AddRange(
            CreateWorkflowDetail(900, 400, 2, createdDate),
            CreateWorkflowDetail(901, 401, 2, createdDate));
        await context.SaveChangesAsync();
        var repository = new AutomationDashboardRepository(context);
        var query = new SubGridQueryParameters
        {
            ZoneId = 14,
            WorkflowStageId = 2,
            PageNumber = 1,
            PageSize = 10
        };
        typeof(SubGridQueryParameters).GetProperty(filterName)!.SetValue(query, true);

        var result = await repository.GetSubGridDataAsync(query, CancellationToken.None);

        Assert.Equal(expectedPropertyIds, result.Properties.Select(p => p.Id).ToArray());
        Assert.Equal(expectedPropertyIds.Length, result.TotalCount);
    }

    public static TheoryData<string, int[]> SubGridGlobalSearchCases => new()
    {
        { "rahul", new[] { 301 } },
        { "98765", new[] { 302 } },
        { "D1-31", new[] { 301 } },
        { "sagar niwas", new[] { 301 } },
        { "commercial", new[] { 302 } },
        { "wing-a", new[] { 301 } },
        { "builder prime", new[] { 301 } },
        { "office", new[] { 302 } }
    };

    [Theory]
    [MemberData(nameof(SubGridGlobalSearchCases))]
    public async Task GetSubGridDataAsync_GlobalSearchFiltersAcrossReturnedColumns(
        string search,
        int[] expectedPropertyIds)
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
        context.ZoneMaster.Add(new ZoneEntity
        {
            Id = 14,
            ZoneNo = "MM",
            Description = "MM",
            IsActive = true,
            CreatedDate = createdDate
        });
        context.WardMaster.Add(new WardEntity
        {
            Id = 21,
            WardNo = "D1",
            ZoneId = 14,
            IsActive = true,
            CreatedDate = createdDate
        });
        context.PropertyCategoryMaster.Add(new PropertyCategoryEntity
        {
            Id = 1,
            PropertyCategoryName = "Individual",
            IsActive = true,
            CreatedDate = createdDate
        });
        context.PropertyTypeMasters.AddRange(
            CreatePropertyType(11, "Residential", "R", createdDate),
            CreatePropertyType(12, "Commercial", "C", createdDate));
        context.TypeOfUse.AddRange(
            CreateTypeOfUse(901, "R", "R", "Residential", createdDate),
            CreateTypeOfUse(902, "C", "C", "Office", createdDate));
        context.WingEntity.Add(new WingEntity
        {
            Id = 501,
            WingNo = "Wing-A",
            IsActive = true,
            CreatedDate = createdDate
        });
        context.PropertyMast.AddRange(
            CreateSubGridProperty(301, 21, "31", "", "Rahul Patil", createdDate, 11),
            CreateSubGridProperty(302, 21, "32", "", "Other Owner", createdDate, 12));
        var firstProperty = context.PropertyMast.Local.Single(p => p.Id == 301);
        firstProperty.CategoryId = 1;
        firstProperty.Address = "Opp Sagar Niwas";
        firstProperty.MobileNo = "11111";
        firstProperty.FlatOrShopName = "Flat 101";
        var secondProperty = context.PropertyMast.Local.Single(p => p.Id == 302);
        secondProperty.Address = "Market Road";
        secondProperty.MobileNo = "9876543210";
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity
        {
            Id = 601,
            PropertyId = 301,
            WingId = 501,
            BuilderName = "Builder Prime",
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = createdDate
        });
        context.PropertyDetails.AddRange(
            CreatePropertyDetail(701, 301, 901, createdDate),
            CreatePropertyDetail(702, 302, 902, createdDate));
        context.PropertyWorkflowDetails.AddRange(
            CreateWorkflowDetail(801, 301, 1, createdDate),
            CreateWorkflowDetail(802, 302, 1, createdDate));
        await context.SaveChangesAsync();
        var repository = new AutomationDashboardRepository(context);

        var result = await repository.GetSubGridDataAsync(
            new SubGridQueryParameters
            {
                ZoneId = 14,
                WorkflowStageId = 1,
                Search = search,
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        Assert.Equal(expectedPropertyIds, result.Properties.Select(p => p.Id).ToArray());
        Assert.Equal(expectedPropertyIds.Length, result.TotalCount);
    }

    [Theory]
    [InlineData("Rahul", 301)]
    [InlineData("9876543210", 302)]
    [InlineData("5551234", 303)]
    [InlineData("7778888", 304)]
    [InlineData("Sagar Niwas", 305)]
    [InlineData("Mahadev", 305)]
    [InlineData("SHRI. PRAFULL M. SHAH,SHRI. PRAFULL M. SHAH", 306)]
    public async Task GetSubGridDataAsync_GlobalSearchWorksWithoutZoneId(string search, int expectedPropertyId)
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
                Id = 14,
                ZoneNo = "MM",
                Description = "MM",
                IsActive = true,
                CreatedDate = createdDate
            },
            new ZoneEntity
            {
                Id = 15,
                ZoneNo = "NK",
                Description = "Naupada Kopri",
                IsActive = true,
                CreatedDate = createdDate
            });
        context.WardMaster.AddRange(
            new WardEntity
            {
                Id = 21,
                WardNo = "D1",
                ZoneId = 14,
                IsActive = true,
                CreatedDate = createdDate
            },
            new WardEntity
            {
                Id = 22,
                WardNo = "D2",
                ZoneId = 15,
                IsActive = true,
                CreatedDate = createdDate
            });
        context.PropertyMast.AddRange(
            CreateSubGridProperty(301, 21, "31", "", "Rahul Patil", createdDate, 11),
            CreateSubGridProperty(302, 22, "32", "", "Other Owner", createdDate, 12),
            CreateSubGridProperty(303, 22, "33", "", "Alternate Owner", createdDate, 12),
            CreateSubGridProperty(304, 22, "34", "", "Occupier Owner", createdDate, 12),
            CreateSubGridProperty(305, 22, "35", "", "Address Owner", createdDate, 12),
            CreateSubGridProperty(306, 22, "36", "", "Prafull Owner", createdDate, 12));
        var secondProperty = context.PropertyMast.Local.Single(p => p.Id == 302);
        secondProperty.MobileNo = "98765-43210";
        var thirdProperty = context.PropertyMast.Local.Single(p => p.Id == 303);
        thirdProperty.AlternateMobileNo = "555 1234";
        var fourthProperty = context.PropertyMast.Local.Single(p => p.Id == 304);
        fourthProperty.OccupierMobileNo = "(777) 8888";
        var fifthProperty = context.PropertyMast.Local.Single(p => p.Id == 305);
        fifthProperty.Address = "Opp Sagar Niwas";
        fifthProperty.OccupierName = "Mahadev Patil";
        var sixthProperty = context.PropertyMast.Local.Single(p => p.Id == 306);
        sixthProperty.OccupierName = "SHRI. PRAFULL M. SHAH,SHRI. PRAFULL M. SHAH";
        context.PropertyWorkflowDetails.AddRange(
            CreateWorkflowDetail(801, 301, 1, createdDate),
            CreateWorkflowDetail(802, 302, 1, createdDate),
            CreateWorkflowDetail(803, 303, 1, createdDate),
            CreateWorkflowDetail(804, 304, 1, createdDate),
            CreateWorkflowDetail(805, 305, 1, createdDate),
            CreateWorkflowDetail(806, 306, 1, createdDate));
        await context.SaveChangesAsync();
        var repository = new AutomationDashboardRepository(context);

        var result = await repository.GetSubGridDataAsync(
            new SubGridQueryParameters
            {
                WorkflowStageId = 1,
                Search = search,
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        var property = Assert.Single(result.Properties);
        Assert.Equal(expectedPropertyId, property.Id);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(property.WardId, result.WardId);
        Assert.Equal(property.WardNo, result.WardNo);
        Assert.True(result.ZoneId > 0);
    }

    [Fact]
    public async Task GetSubGridDataAsync_GlobalSearchByZoneNoWorksWithoutZoneId()
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
                Id = 14,
                ZoneNo = "MM",
                Description = "MM",
                IsActive = true,
                CreatedDate = createdDate
            },
            new ZoneEntity
            {
                Id = 15,
                ZoneNo = "NK",
                Description = "Naupada Kopri",
                IsActive = true,
                CreatedDate = createdDate
            });
        context.WardMaster.AddRange(
            new WardEntity
            {
                Id = 21,
                WardNo = "D1",
                ZoneId = 14,
                IsActive = true,
                CreatedDate = createdDate
            },
            new WardEntity
            {
                Id = 22,
                WardNo = "D2",
                ZoneId = 15,
                IsActive = true,
                CreatedDate = createdDate
            });
        context.PropertyMast.AddRange(
            CreateSubGridProperty(301, 21, "31", "", "MM Owner", createdDate, 11),
            CreateSubGridProperty(302, 22, "32", "", "NK Owner One", createdDate, 12),
            CreateSubGridProperty(303, 22, "33", "", "NK Owner Two", createdDate, 12));
        context.PropertyWorkflowDetails.AddRange(
            CreateWorkflowDetail(801, 301, 1, createdDate),
            CreateWorkflowDetail(802, 302, 1, createdDate),
            CreateWorkflowDetail(803, 303, 1, createdDate));
        await context.SaveChangesAsync();
        var repository = new AutomationDashboardRepository(context);

        var result = await repository.GetSubGridDataAsync(
            new SubGridQueryParameters
            {
                WorkflowStageId = 1,
                Search = "NK",
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        Assert.Equal(new[] { 302, 303 }, result.Properties.Select(p => p.Id).ToArray());
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(15, result.ZoneId);
        Assert.Equal("NK", result.ZoneNo);
        Assert.Equal(22, result.WardId);
        Assert.Equal("D2", result.WardNo);
    }

    [Fact]
    public async Task GetSubGridDataAsync_SearchFormattedPropertyNoUsesFastPropertyFilter()
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
        context.ZoneMaster.Add(new ZoneEntity
        {
            Id = 14,
            ZoneNo = "UK",
            Description = "UK",
            IsActive = true,
            CreatedDate = createdDate
        });
        context.WardMaster.Add(new WardEntity
        {
            Id = 21,
            WardNo = "UK2",
            ZoneId = 14,
            IsActive = true,
            CreatedDate = createdDate
        });
        context.PropertyMast.AddRange(
            CreateSubGridProperty(301, 21, "39", "", "Main Owner", createdDate, 11),
            CreateSubGridProperty(302, 21, "39", "1", "Partition Owner", createdDate, 11),
            CreateSubGridProperty(303, 21, "40", "", "Other Owner", createdDate, 11));
        context.PropertyWorkflowDetails.AddRange(
            CreateWorkflowDetail(801, 301, 1, createdDate),
            CreateWorkflowDetail(802, 302, 1, createdDate),
            CreateWorkflowDetail(803, 303, 1, createdDate));
        await context.SaveChangesAsync();
        var repository = new AutomationDashboardRepository(context);

        var result = await repository.GetSubGridDataAsync(
            new SubGridQueryParameters
            {
                WorkflowStageId = 1,
                Search = "UK2-39",
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        Assert.Equal(new[] { 301, 302 }, result.Properties.Select(p => p.Id).ToArray());
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(14, result.ZoneId);
        Assert.Equal("UK", result.ZoneNo);
        Assert.Equal(21, result.WardId);
        Assert.Equal("UK2", result.WardNo);
    }

    public static TheoryData<int, int[]> PropertyTypeCategoryFilterCases => new()
    {
        { 1, new[] { 101, 106 } },
        { 2, new[] { 102 } },
        { 3, new[] { 104 } },
        { 4, new[] { 106 } },
        { 5, new[] { 103, 105 } },
        { 6, new[] { 107 } }
    };

    [Theory]
    [MemberData(nameof(PropertyTypeCategoryFilterCases))]
    public async Task GetSubGridDataAsync_FiltersPropertyTypeCategoryFromConfiguredBusinessRules(
        int propertyTypeCategoryId,
        int[] expectedPropertyIds)
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
        context.ZoneMaster.Add(new ZoneEntity
        {
            Id = 15,
            ZoneNo = "Z15",
            Description = "Zone 15",
            IsActive = true,
            CreatedDate = createdDate
        });
        context.WardMaster.Add(new WardEntity
        {
            Id = 21,
            WardNo = "W15",
            ZoneId = 15,
            IsActive = true,
            CreatedDate = createdDate
        });
        context.PropertyTypeMasters.AddRange(
            CreatePropertyType(11, "Residential", "R", createdDate),
            CreatePropertyType(12, "Commercial", "C", createdDate),
            CreatePropertyType(13, "Industrial", "I", createdDate),
            CreatePropertyType(14, "Mixed", "R-C", createdDate),
            CreatePropertyType(15, "Public Utility", "N", createdDate));
        context.TypeOfUse.AddRange(
            CreateTypeOfUse(901, "R", "R", "Residential", createdDate),
            CreateTypeOfUse(902, "C", "C", "Commercial", createdDate),
            CreateTypeOfUse(903, "I", "I", "Industrial", createdDate),
            CreateTypeOfUse(904, "N", "N", "Public Utility", createdDate),
            CreateTypeOfUse(905, "UC", "R", "Under Construction", createdDate));
        context.PropertyMast.AddRange(
            CreateSubGridProperty(101, 21, "1", "", "Residential Owner", createdDate, 11),
            CreateSubGridProperty(102, 21, "2", "", "Commercial Owner", createdDate, 12),
            CreateSubGridProperty(103, 21, "3", "", "Industrial Owner", createdDate, 13),
            CreateSubGridProperty(104, 21, "4", "", "Mixed Owner", createdDate, 14),
            CreateSubGridProperty(105, 21, "5", "", "Public Utility Owner", createdDate, 15),
            CreateSubGridProperty(106, 21, "6", "", "Open Plot Owner", createdDate, 11, isOpenPlot: true),
            CreateSubGridProperty(107, 21, "7", "", "Under Construction Owner", createdDate, 11));
        context.PropertyDetails.AddRange(
            CreatePropertyDetail(901, 101, 901, createdDate),
            CreatePropertyDetail(902, 102, 902, createdDate),
            CreatePropertyDetail(903, 103, 903, createdDate),
            CreatePropertyDetail(904, 105, 904, createdDate),
            CreatePropertyDetail(905, 107, 905, createdDate));
        context.PropertyWorkflowDetails.AddRange(
            CreateWorkflowDetail(201, 101, 1, createdDate),
            CreateWorkflowDetail(202, 102, 1, createdDate),
            CreateWorkflowDetail(203, 103, 1, createdDate),
            CreateWorkflowDetail(204, 104, 1, createdDate),
            CreateWorkflowDetail(205, 105, 1, createdDate),
            CreateWorkflowDetail(206, 106, 1, createdDate),
            CreateWorkflowDetail(207, 107, 1, createdDate));
        await context.SaveChangesAsync();
        var repository = new AutomationDashboardRepository(context);

        var result = await repository.GetSubGridDataAsync(
            new SubGridQueryParameters
            {
                ZoneId = 15,
                WorkflowStageId = 1,
                PropertyTypeCategoryId = propertyTypeCategoryId,
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        Assert.Equal(expectedPropertyIds, result.Properties.Select(p => p.Id).ToArray());
        Assert.Equal(expectedPropertyIds.Length, result.TotalCount);
    }

    private static PropertyTypeMasterEntity CreatePropertyType(
        int id,
        string description,
        string type,
        DateTime createdDate)
        => new()
        {
            Id = id,
            PropertyDescription = description,
            Type = type,
            PropertyTypeCategoryId = null,
            IsActive = true,
            CreatedDate = createdDate
        };

    private static TypeOfUseEntity CreateTypeOfUse(
        int id,
        string code,
        string type,
        string description,
        DateTime createdDate)
        => new()
        {
            Id = id,
            TypeOfUseCode = code,
            Description = description,
            Type = type,
            TypeOfUseGroupId = 1,
            IsActive = true,
            CreatedDate = createdDate
        };

    private static PropertyDetailsEntity CreatePropertyDetail(
        int id,
        int propertyId,
        int typeOfUseId,
        DateTime createdDate)
        => new()
        {
            Id = id,
            PropertyId = propertyId,
            TypeOfUseId = typeOfUseId,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = createdDate
        };

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

    private static PropertyEntity CreateSubGridProperty(
        int id,
        int wardId,
        string propertyNo,
        string partitionNo,
        string ownerName,
        DateTime createdDate,
        int? propertyTypeId = null,
        bool isOpenPlot = false,
        bool isActive = true)
        => new()
        {
            Id = id,
            WardId = wardId,
            PropertyNo = propertyNo,
            PartitionNo = partitionNo,
            OwnerName = ownerName,
            PropertyTypeId = propertyTypeId,
            OpenPlot = isOpenPlot,
            IsActive = isActive,
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

    private static TransMastEntity CreateTransMast(
        int id,
        int propertyId,
        int taxId,
        decimal taxAmount,
        DateTime createdDate)
        => new()
        {
            Id = id,
            PropertyId = propertyId,
            FinanceYearId = 1,
            CalculationType = "RV",
            CalculationValue = 0,
            TaxId = taxId,
            TaxAmount = taxAmount,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = createdDate
        };
}
