using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Services.AutomationDashboard;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Tests.Application;

public class AutomationDashboardGridResponseTests
{
    [Fact]
    public async Task GetGeoSequencingGridDataAsync_ReturnsZoneNo()
    {
        var repository = new Mock<IGeoSequencingStageRepository>();
        repository.Setup(x => x.ReadZonesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(int ZoneId, string ZoneName, string ZoneNo)> { (14, "Zone 14", "Z14") });
        repository.Setup(x => x.ReadStagePropertiesForZonesAsync(1, It.IsAny<List<int>>(), It.IsAny<CancellationToken>(), It.IsAny<DashboardGridQueryParameters?>()))
            .ReturnsAsync(new List<GeoSequencingStagePropertyProjection>());
        repository.Setup(x => x.ReadPropertyUsesForZonesAsync(1, It.IsAny<List<int>>(), It.IsAny<CancellationToken>(), It.IsAny<DashboardGridQueryParameters?>()))
            .ReturnsAsync(new List<GeoSequencingPropertyUseProjection>());
        repository.Setup(x => x.ReadRegisteredCountsByZoneAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>(), It.IsAny<DashboardGridQueryParameters?>()))
            .ReturnsAsync(new Dictionary<int, int> { [14] = 10 });
        repository.Setup(x => x.ReadAssessmentStatusIdsByNameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());

        var logger = new Mock<ILogger<GeoSequencingStageService>>();
        var service = new GeoSequencingStageService(repository.Object, logger.Object);

        var result = await service.GetGeoSequencingGridDataAsync(new DashboardGridQueryParameters { WorkflowStageId = 1 }, CancellationToken.None);

        Assert.Equal("Z14", result.Zones.Single().ZoneNo);
    }

    [Fact]
    public async Task GetInternalSurveyGridDataAsync_ReturnsZoneNo()
    {
        var repository = new Mock<IInternalSurveyStageRepository>();
        repository.Setup(x => x.ReadZonesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(int ZoneId, string ZoneName, string ZoneNo)> { (14, "Zone 14", "Z14") });
        repository.Setup(x => x.ReadGeoSequencingStageIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        repository.Setup(x => x.ReadAssessedAndUnassessedStatusIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((1, 2));
        repository.Setup(x => x.ReadPropertyPhotoTypeIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(5);
        repository.Setup(x => x.ReadStagePropertiesForZonesAsync(It.IsAny<int>(), It.IsAny<List<int>>(), true, It.IsAny<CancellationToken>(), It.IsAny<DashboardGridQueryParameters?>()))
            .ReturnsAsync(new List<InternalSurveyStagePropertyProjection>());
        repository.Setup(x => x.ReadPropertyUsesForStageInZonesAsync(2, It.IsAny<List<int>>(), true, It.IsAny<CancellationToken>(), It.IsAny<DashboardGridQueryParameters?>()))
            .ReturnsAsync(new List<InternalSurveyPropertyUseSourceProjection>());
        repository.Setup(x => x.ReadPhotoCountsByZoneAsync(2, It.IsAny<List<int>>(), 5, It.IsAny<CancellationToken>(), It.IsAny<DashboardGridQueryParameters?>()))
            .ReturnsAsync(new List<InternalSurveyPhotoCountProjection>());

        var logger = new Mock<ILogger<InternalSurveyStageService>>();
        var service = new InternalSurveyStageService(repository.Object, logger.Object);

        var result = await service.GetInternalSurveyGridDataAsync(new DashboardGridQueryParameters { WorkflowStageId = 2 }, CancellationToken.None);

        Assert.Equal("Z14", result.DivisionData.Single().ZoneNo);
    }

    [Fact]
    public async Task GetDataEntryGridDataAsync_ReturnsZoneNo()
    {
        var repository = new Mock<IDataEntryStageRepository>();
        var logger = new Mock<ILogger<DataEntryStageService>>();

        repository.Setup(x => x.ReadZonesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(int ZoneId, string ZoneName, string ZoneNo)> { (14, "Zone 14", "Z14") });
        repository.Setup(x => x.ReadInternalSurveyStageIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        repository.Setup(x => x.ReadAssessmentStageIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(4);
        repository.Setup(x => x.ReadPropertyPhotoTypeIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        repository.Setup(x => x.ReadPlanPhotoTypeIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        repository.Setup(x => x.ReadStagePropertiesForZonesAsync(It.IsAny<int>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>(), It.IsAny<DashboardGridQueryParameters>()))
            .ReturnsAsync(new List<DataEntryStagePropertyProjection>
            {
                new() { PropertyId = 100, WorkflowStageId = 3, ZoneId = 14, PartitionNo = "" }
            });
        repository.Setup(x => x.ReadZoneTotalsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>(), It.IsAny<DashboardGridQueryParameters>()))
            .ReturnsAsync(new Dictionary<int, (int StructureCount, int UnitCount)> { [14] = (1, 1) });
        repository.Setup(x => x.ReadCompletedPhotosAsync(It.IsAny<int>(), It.IsAny<List<int>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<DashboardGridQueryParameters>()))
            .ReturnsAsync(new List<DataEntryCompletedPhotoProjection>());
        repository.Setup(x => x.ReadPropertyTypesAsync(It.IsAny<int>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>(), It.IsAny<DashboardGridQueryParameters>()))
            .ReturnsAsync(new List<DataEntryPropertyTypeSourceProjection>());
        repository.Setup(x => x.ReadPropertyUsesAsync(It.IsAny<int>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>(), It.IsAny<DashboardGridQueryParameters>()))
            .ReturnsAsync(new List<DataEntryPropertyUseSourceProjection>());
        repository.Setup(x => x.ReadAssessmentStatusIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());
        repository.Setup(x => x.ReadAssessmentStatusCountsAsync(It.IsAny<int>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>(), It.IsAny<DashboardGridQueryParameters>()))
            .ReturnsAsync(new List<DataEntryAssessmentStatusCountProjection>());

        var service = new DataEntryStageService(repository.Object, logger.Object);

        var result = await service.GetDataEntryGridDataAsync(new DashboardGridQueryParameters { WorkflowStageId = 3 }, CancellationToken.None);

        Assert.Equal("Z14", result.DivisionData.Single().ZoneNo);
    }

    [Fact]
    public async Task GetAssessmentGridDataAsync_ReturnsZoneNo()
    {
        var repository = new Mock<IAssessmentStageRepository>();
        var logger = new Mock<ILogger<AssessmentStageService>>();

        repository.Setup(x => x.GetAssessmentStatusIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { ["Assessed"] = 1, ["Unassessed"] = 2 });
        repository.Setup(x => x.GetStagePropertiesAsync(4, It.IsAny<CancellationToken>(), It.IsAny<AssessmentGridQueryParameters?>()))
            .ReturnsAsync(new List<AssessmentStagePropertyProjection>
            {
                new() { PropertyId = 100, ZoneId = 14, ZoneName = "Zone 14", ZoneNo = "Z14", PartitionNo = "", AssessmentStatusId = 1 }
            });
        repository.Setup(x => x.GetOldDemandByZoneAsync(It.IsAny<IEnumerable<AssessmentStagePropertyProjection>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, decimal>());
        repository.Setup(x => x.GetCurrentDemandByZoneAsync(It.IsAny<IEnumerable<AssessmentStagePropertyProjection>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, decimal>());
        repository.Setup(x => x.GetRetroDemandByZoneAsync(It.IsAny<IEnumerable<AssessmentStagePropertyProjection>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, decimal>());

        var service = new AssessmentStageService(repository.Object, logger.Object);

        var result = await service.GetAssessmentGridDataAsync(new AssessmentGridQueryParameters { WorkflowStageId = 4, Type = "Total" }, CancellationToken.None);

        Assert.Equal("Z14", result.ZoneData.Single().ZoneNo);
    }

    [Fact]
    public async Task GetSubGridDataAsync_MapsWingNameAndDoesNotReturnQcChecklist()
    {
        var repository = new Mock<IAutomationDashboardRepository>();
        repository.Setup(x => x.GetSubGridDataAsync(It.IsAny<SubGridQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubGridDataProjection
            {
                WorkflowStageId = 1,
                WorkflowStageName = "GeoSequencing",
                ZoneId = 14,
                ZoneName = "Zone 14",
                ZoneNo = "Z14",
                TotalCount = 1,
                Properties = new List<SubGridPropertyProjection>
                {
                    new()
                    {
                        Id = 100,
                        WardId = 21,
                        WardNo = "D18",
                        PropertyNo = "3",
                        PartitionNo = "",
                        WingName = "A Wing",
                        CategoryName = "Residential",
                        TypeDescription = "House",
                        TypeName = "R"
                    }
                }
            });
        var service = CreateAutomationDashboardService(repository.Object);

        var result = await service.GetSubGridDataAsync(new SubGridQueryParameters { ZoneId = 14, WorkflowStageId = 1 }, CancellationToken.None);

        var property = result.Properties.Single();
        Assert.Equal("Z14", result.ZoneNo);
        Assert.Equal(21, property.WardId);
        Assert.Equal("D18", property.WardNo);
        Assert.Equal("A Wing", property.WingName);
        Assert.IsNotType<PendingAssessmentSubGridPropertyDetailsDto>(property);
    }

    [Fact]
    public async Task GetSubGridDataAsync_PassesPropertyNoAndOwnerNameFilters()
    {
        SubGridQueryParameters? capturedQuery = null;
        var repository = new Mock<IAutomationDashboardRepository>();
        repository.Setup(x => x.GetSubGridDataAsync(It.IsAny<SubGridQueryParameters>(), It.IsAny<CancellationToken>()))
            .Callback<SubGridQueryParameters, CancellationToken>((query, _) => capturedQuery = query)
            .ReturnsAsync(new SubGridDataProjection());
        var service = CreateAutomationDashboardService(repository.Object);

        await service.GetSubGridDataAsync(
            new SubGridQueryParameters
            {
                ZoneId = 14,
                WorkflowStageId = 1,
                PropertyNo = "D18-3",
                OwnerName = "Patil"
            },
            CancellationToken.None);

        Assert.NotNull(capturedQuery);
        Assert.Equal("D18-3", capturedQuery!.PropertyNo);
        Assert.Equal("Patil", capturedQuery.OwnerName);
    }

    [Fact]
    public async Task GetWardSubGridDataAsync_PassesWardIdWithoutZoneId()
    {
        WardSubGridQueryParameters? capturedQuery = null;
        var repository = new Mock<IAutomationDashboardRepository>();
        repository.Setup(x => x.GetSubGridDataAsync(It.IsAny<WardSubGridQueryParameters>(), It.IsAny<CancellationToken>()))
            .Callback<WardSubGridQueryParameters, CancellationToken>((query, _) => capturedQuery = query)
            .ReturnsAsync(new SubGridDataProjection { ZoneNo = "Z14" });
        var service = CreateAutomationDashboardService(repository.Object);

        var result = await service.GetWardSubGridDataAsync(new WardSubGridQueryParameters { WardId = 21, WorkflowStageId = 1 }, CancellationToken.None);

        Assert.NotNull(capturedQuery);
        Assert.Equal(21, capturedQuery!.WardId);
        Assert.Equal("Z14", result.ZoneNo);
    }

    [Fact]
    public async Task GetPendingAssessmentPropsAsync_PassesFilterParameters()
    {
        PendingAssessmentQueryParameters? capturedQuery = null;
        var repository = new Mock<IAutomationDashboardRepository>();
        repository.Setup(x => x.GetPendingAssessmentPropsAsync(It.IsAny<PendingAssessmentQueryParameters>(), It.IsAny<CancellationToken>()))
            .Callback<PendingAssessmentQueryParameters, CancellationToken>((query, _) => capturedQuery = query)
            .ReturnsAsync(new SubGridDataProjection { ZoneNo = "Z14" });
        var service = CreateAutomationDashboardService(repository.Object);

        var result = await service.GetPendingAssessmentPropsAsync(
            new PendingAssessmentQueryParameters
            {
                PageNumber = 2,
                PageSize = 25,
                SearchTerm = "D11-115",
                SurveyTypeId = 1,
                ZoneNo = "Z14",
                WardNo = "D11",
                PropertyTypeId = 3
            },
            CancellationToken.None);

        Assert.NotNull(capturedQuery);
        Assert.Equal(2, capturedQuery!.PageNumber);
        Assert.Equal(25, capturedQuery.PageSize);
        Assert.Equal("D11-115", capturedQuery.SearchTerm);
        Assert.Equal(1, capturedQuery.SurveyTypeId);
        Assert.Equal("Z14", capturedQuery.ZoneNo);
        Assert.Equal("D11", capturedQuery.WardNo);
        Assert.Equal(3, capturedQuery.PropertyTypeId);
        Assert.Equal("Z14", result.ZoneNo);
    }

    private static AutomationDashboardService CreateAutomationDashboardService(IAutomationDashboardRepository repository)
        => new(
            repository,
            Mock.Of<IGeoSequencingStageService>(),
            Mock.Of<IInternalSurveyStageService>(),
            Mock.Of<IDataEntryStageService>(),
            Mock.Of<IAssessmentStageService>());
}



