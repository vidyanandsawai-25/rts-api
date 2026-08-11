using Moq;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Tests.Application;

public class GeoSequencingStageServiceWardWiseSummaryTests
{
    [Fact]
    public async Task GetGeoSequencingWardWiseSummaryAsync_PrioritizesWardsWithCountsBeforePaging()
    {
        var repository = new Mock<IGeoSequencingStageRepository>();
        var wards = Enumerable.Range(1, 12)
            .Select(id => (WardId: id, WardNo: $"W{id:00}"))
            .ToList();

        repository
            .Setup(x => x.StageExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository
            .Setup(x => x.ReadZoneAsync(14, It.IsAny<CancellationToken>()))
            .ReturnsAsync((14, "Zone 14", "Z14"));
        repository
            .Setup(x => x.ReadWardsInZoneAsync(14, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wards);
        repository
            .Setup(x => x.ReadStagePropertiesForZonesAsync(
                1,
                It.Is<List<int>>(ids => ids.SequenceEqual(new[] { 14 })),
                It.IsAny<CancellationToken>(),
                null))
            .ReturnsAsync(new List<GeoSequencingStagePropertyProjection>
            {
                new()
                {
                    PropertyId = 120,
                    WardId = 12,
                    ZoneId = 14,
                    PartitionNo = "",
                    PropertyCategoryName = "House",
                    AssessmentStatusId = 1
                }
            });
        repository
            .Setup(x => x.ReadPropertyUsesForZonesAsync(
                1,
                It.Is<List<int>>(ids => ids.SequenceEqual(new[] { 14 })),
                It.IsAny<CancellationToken>(),
                null))
            .ReturnsAsync(new List<GeoSequencingPropertyUseProjection>());
        repository
            .Setup(x => x.ReadRegisteredCountsByWardAsync(
                It.Is<List<int>>(ids => ids.SequenceEqual(wards.Select(w => w.WardId))),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int> { [12] = 1 });
        repository
            .Setup(x => x.ReadAssessmentStatusIdsByNameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { ["ASSESSED"] = 1 });
        var service = new GeoSequencingStageService(repository.Object);

        var result = await service.GetGeoSequencingWardWiseSummaryAsync(
            14,
            1,
            pageNumber: 1,
            pageSize: 10,
            CancellationToken.None);

        Assert.Equal(12, result.TotalCount);
        Assert.Equal(1, result.TotalRow.RegisteredProperties);
        Assert.Equal("W12", result.WardData.First().WardNo);
        Assert.Equal(1, result.WardData.First().RegisteredProperties);
        Assert.Equal(1, result.WardData.First().GeoSequencedProperties.StructureCount);
    }
}
