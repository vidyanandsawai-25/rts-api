using AutoMapper;
using Moq;
using NtisPlatform.Application.DTOs.AutomationDashboard;
using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Tests.Application;

public class AutomationDashboardGridResponseTests
{
    [Fact]
    public async Task GetGeoSequencingGridDataAsync_ReturnsZoneNo()
    {
        var repository = new Mock<IGeoSequencingStageRepository>();
        repository.Setup(x => x.StageExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(x => x.ReadZonesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(int ZoneId, string ZoneName, string ZoneNo)> { (14, "Zone 14", "Z14") });
        repository.Setup(x => x.ReadStagePropertiesForZonesAsync(1, It.IsAny<List<int>>(), It.IsAny<CancellationToken>(), It.IsAny<PropertySearchRequestDto?>()))
            .ReturnsAsync(new List<GeoSequencingStagePropertyProjection>());
        repository.Setup(x => x.ReadPropertyUsesForZonesAsync(1, It.IsAny<List<int>>(), It.IsAny<CancellationToken>(), It.IsAny<PropertySearchRequestDto?>()))
            .ReturnsAsync(new List<GeoSequencingPropertyUseProjection>());
        repository.Setup(x => x.ReadRegisteredCountsByZoneAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>(), It.IsAny<PropertySearchRequestDto?>()))
            .ReturnsAsync(new Dictionary<int, int> { [14] = 10 });
        repository.Setup(x => x.ReadAssessmentStatusIdsByNameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());
        var service = new GeoSequencingStageService(repository.Object);

        var result = await service.GetGeoSequencingGridDataAsync(new PropertySearchRequestDto { WorkflowStageId = 1 }, CancellationToken.None);

        Assert.Equal("Z14", result.Zones.Single().ZoneNo);
    }

    [Fact]
    public async Task GetInternalSurveyGridDataAsync_ReturnsZoneNo()
    {
        var repository = new Mock<IInternalSurveyStageRepository>();
        repository.Setup(x => x.StageExistsAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(x => x.ReadZonesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(int ZoneId, string ZoneName, string ZoneNo)> { (14, "Zone 14", "Z14") });
        repository.Setup(x => x.ReadGeoSequencingStageIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        repository.Setup(x => x.ReadAssessedAndUnassessedStatusIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((1, 2));
        repository.Setup(x => x.ReadPropertyPhotoTypeIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(5);
        repository.Setup(x => x.ReadStagePropertiesForZonesAsync(It.IsAny<int>(), It.IsAny<List<int>>(), true, It.IsAny<CancellationToken>(), It.IsAny<PropertySearchRequestDto?>()))
            .ReturnsAsync(new List<InternalSurveyStagePropertyProjection>());
        repository.Setup(x => x.ReadPropertyUsesForStageInZonesAsync(2, It.IsAny<List<int>>(), true, It.IsAny<CancellationToken>(), It.IsAny<PropertySearchRequestDto?>()))
            .ReturnsAsync(new List<InternalSurveyPropertyUseSourceProjection>());
        repository.Setup(x => x.ReadPhotoCountsByZoneAsync(2, It.IsAny<List<int>>(), 5, It.IsAny<CancellationToken>(), It.IsAny<PropertySearchRequestDto?>()))
            .ReturnsAsync(new List<InternalSurveyPhotoCountProjection>());
        var service = new InternalSurveyStageService(repository.Object);

        var result = await service.GetInternalSurveyGridDataAsync(new PropertySearchRequestDto { WorkflowStageId = 2 }, CancellationToken.None);

        Assert.Equal("Z14", result.DivisionData.Single().ZoneNo);
    }

    [Fact]
    public async Task GetDataEntryGridDataAsync_ReturnsZoneNo()
    {
        var repository = new Mock<IDataEntryStageRepository>();
        repository.Setup(x => x.GetDataEntryGridSnapshotAsync(3, null, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync(new DataEntryGridSnapshotProjection
            {
                WorkflowStageExists = true,
                InternalSurveyStageId = 2,
                AssessmentStageId = 4,
                PropertyPhotoTypeId = 1,
                PlanPhotoTypeId = 2,
                Zones = new List<(int ZoneId, string ZoneName, string ZoneNo)> { (14, "Zone 14", "Z14") },
                StageProperties = new List<DataEntryStagePropertyProjection>
                {
                    new() { PropertyId = 100, WorkflowStageId = 3, ZoneId = 14, PartitionNo = "" }
                },
                ZoneTotals = new List<DataEntryZoneCountProjection>
                {
                    new() { ZoneId = 14, StructureCount = 1, UnitCount = 1 }
                }
            });
        var service = new DataEntryStageService(repository.Object);

        var result = await service.GetDataEntryGridDataAsync(new PropertySearchRequestDto { WorkflowStageId = 3 }, CancellationToken.None);

        Assert.Equal("Z14", result.DivisionData.Single().ZoneNo);
    }

    [Fact]
    public async Task GetAssessmentGridDataAsync_ReturnsZoneNo()
    {
        var repository = new Mock<IAssessmentStageRepository>();
        repository.Setup(x => x.AssessmentWorkflowStageExistsAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(x => x.GetAssessmentStatusIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { ["Assessed"] = 1, ["Unassessed"] = 2 });
        repository.Setup(x => x.GetStagePropertiesAsync(4, It.IsAny<CancellationToken>(), It.IsAny<PropertySearchRequestDto?>()))
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
        var service = new AssessmentStageService(repository.Object);

        var result = await service.GetAssessmentGridDataAsync(new PropertySearchRequestDto { WorkflowStageId = 4 }, "Total", CancellationToken.None);

        Assert.Equal("Z14", result.ZoneData.Single().ZoneNo);
    }

    [Fact]
    public async Task GetSubGridDataAsync_MapsWingNameAndDoesNotReturnQcChecklist()
    {
        var repository = new Mock<IAutomationDashboardRepository>();
        repository.Setup(x => x.GetSubGridDataAsync(It.IsAny<SubGridFilterRequestDto>(), It.IsAny<CancellationToken>()))
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
    public async Task GetWardSubGridDataAsync_PassesWardIdWithoutZoneId()
    {
        SubGridFilterRequestDto? capturedQuery = null;
        var repository = new Mock<IAutomationDashboardRepository>();
        repository.Setup(x => x.GetSubGridDataAsync(It.IsAny<SubGridFilterRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<SubGridFilterRequestDto, CancellationToken>((query, _) => capturedQuery = query)
            .ReturnsAsync(new SubGridDataProjection { ZoneNo = "Z14" });
        var service = CreateAutomationDashboardService(repository.Object);

        var result = await service.GetWardSubGridDataAsync(new WardSubGridQueryParameters { WardId = 21, WorkflowStageId = 1 }, CancellationToken.None);

        Assert.NotNull(capturedQuery);
        Assert.Equal(21, capturedQuery!.WardId);
        Assert.Null(capturedQuery.ZoneId);
        Assert.Equal("Z14", result.ZoneNo);
    }

    [Fact]
    public async Task GetPendingAssessmentPropsAsync_PassesFilterParameters()
    {
        SubGridFilterRequestDto? capturedQuery = null;
        var repository = new Mock<IAutomationDashboardRepository>();
        repository.Setup(x => x.GetPendingAssessmentPropsAsync(It.IsAny<SubGridFilterRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<SubGridFilterRequestDto, CancellationToken>((query, _) => capturedQuery = query)
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
            Mock.Of<IDataEntryStageRepository>(),
            Mock.Of<IGeoSequencingStageService>(),
            Mock.Of<IInternalSurveyStageService>(),
            Mock.Of<IDataEntryStageService>(),
            Mock.Of<IAssessmentStageService>(),
            Mock.Of<IMapper>());
}
