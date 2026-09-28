using Moq;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class ApartmentQcSearchServiceTests
{
    [Fact]
    public async Task GetSuggestionsAsync_ClampsMaxResultsAboveCap()
    {
        var repository = new Mock<IApartmentQcSearchRepository>();
        repository.Setup(r => r.GetSuggestionsAsync(77, null, null, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ApartmentQcSearchSuggestionDto>());

        var service = new ApartmentQcSearchService(repository.Object);

        await service.GetSuggestionsAsync(wardId: 77, propertyNo: null, partitionNo: null, maxResults: 5000);

        repository.Verify(r => r.GetSuggestionsAsync(77, null, null, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ZeroOrNegativeMaxResults_DefaultsTo20()
    {
        var repository = new Mock<IApartmentQcSearchRepository>();
        repository.Setup(r => r.GetSuggestionsAsync(77, "1", null, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ApartmentQcSearchSuggestionDto>());

        var service = new ApartmentQcSearchService(repository.Object);

        await service.GetSuggestionsAsync(wardId: 77, propertyNo: "1", partitionNo: null, maxResults: 0);

        repository.Verify(r => r.GetSuggestionsAsync(77, "1", null, 20, It.IsAny<CancellationToken>()), Times.Once);
    }
}
