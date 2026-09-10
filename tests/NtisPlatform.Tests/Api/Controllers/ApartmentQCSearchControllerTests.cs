using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class ApartmentQCSearchControllerTests
{
    private static ApartmentQCController Create(out Mock<IApartmentQcSearchService> service)
    {
        service = new Mock<IApartmentQcSearchService>();
        return new ApartmentQCController(
            new Mock<IApartmentQCService>().Object,
            new Mock<IWingWiseDetailsService>().Object,
            new Mock<IRateableValueService>().Object,
            new Mock<ICapitalValueService>().Object,
            new Mock<IApartmentQcCertificateGridService>().Object,
            service.Object,
            new Mock<IApartmentQcTopSectionService>().Object,
            new Mock<IApartmentQcTopSectionBelowFlexService>().Object,
            new Mock<IApartmentTaxDetailsService>().Object,
            new Mock<IPropertyCertificateApplicationService>().Object,
            new Mock<NtisPlatform.Application.Interfaces.Master.ISocialAttributeService>().Object,
            NullLogger<ApartmentQCController>.Instance,
            new Mock<IGetApartmentDetailsWingWiseService>().Object);
    }

    [Fact]
    public async Task GetSuggestions_MissingWardId_ReturnsBadRequest()
    {
        var controller = Create(out var service);

        var result = await controller.GetSuggestions(
            new ApartmentQcSearchQueryParameters { WardId = 0 }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        service.Verify(s => s.GetSuggestionsAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSuggestions_ValidWardId_ReturnsOkWithSuggestions()
    {
        var controller = Create(out var service);
        var suggestions = new List<ApartmentQcSearchSuggestionDto>
        {
            new() { PropertyId = 1, WardId = 77, PropertyNo = "1", Category = ApartmentQcSearchCategory.Society, DisplayLabel = "1" }
        };
        service.Setup(s => s.GetSuggestionsAsync(77, "1", null, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestions);

        var result = await controller.GetSuggestions(
            new ApartmentQcSearchQueryParameters { WardId = 77, PropertyNo = "1", MaxResults = 20 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<ApartmentQcSearchSuggestionDto>>>(ok.Value);
        Assert.True(response.Success);
        Assert.Single(response.Items!);
    }
}
