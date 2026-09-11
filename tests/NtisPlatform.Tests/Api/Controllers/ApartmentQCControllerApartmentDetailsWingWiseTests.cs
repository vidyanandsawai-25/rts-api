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

public class ApartmentQCControllerApartmentDetailsWingWiseTests
{
    private static ApartmentQCController Create(out Mock<IGetApartmentDetailsWingWiseService> service)
    {
        service = new Mock<IGetApartmentDetailsWingWiseService>();
        return new ApartmentQCController(
            new Mock<IApartmentQCService>().Object,
            new Mock<IWingWiseDetailsService>().Object,
            new Mock<IRateableValueService>().Object,
            new Mock<ICapitalValueService>().Object,
            new Mock<IApartmentQcCertificateGridService>().Object,
            new Mock<IApartmentQcSearchService>().Object,
            new Mock<IApartmentQcTopSectionService>().Object,
            new Mock<IApartmentQcTopSectionBelowFlexService>().Object,
            new Mock<IApartmentTaxDetailsService>().Object,
            new Mock<IPropertyCertificateApplicationService>().Object,
            new Mock<NtisPlatform.Application.Interfaces.Master.ISocialAttributeService>().Object,
            NullLogger<ApartmentQCController>.Instance,
            service.Object,
            new Mock<IApartmentDashboardService>().Object);
    }

    [Fact]
    public async Task GetApartmentDetailsWingWise_ReturnsOkWithPagedResult()
    {
        var controller = Create(out var service);
        var pagedResult = new PagedResult<ApartmentQCComparisonDto>(
            new List<ApartmentQCComparisonDto>
            {
                new()
                {
                    NewSurvey = new NewSurveyPropertyDto { Id = 101, PropertyNo = "PROP-101" },
                    OldSurvey = new OldSurveyPropertyDto { Id = 201, OldPropertyNo = "OLD-101" }
                }
            },
            totalCount: 1,
            pageNumber: 1,
            pageSize: 10);

        service.Setup(s => s.GetApartmentDetailsWingWiseAsync(It.IsAny<GetApartmentDetailsWingWiseQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new GetApartmentDetailsWingWiseQueryParameters { WingDetailId = 12, PageNumber = 1, PageSize = 10 };
        var result = await controller.GetApartmentDetailsWingWise(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PagedResult<ApartmentQCComparisonDto>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Record found successfully", apiResponse.Message);
        Assert.Equal(1, apiResponse.Items!.TotalCount);
        Assert.Single(apiResponse.Items.Items);
    }

    [Fact]
    public async Task GetApartmentDetailsWingWise_EmptyResult_ReturnsOkWithNoRecordsMessage()
    {
        var controller = Create(out var service);
        var pagedResult = new PagedResult<ApartmentQCComparisonDto>(
            new List<ApartmentQCComparisonDto>(),
            totalCount: 0,
            pageNumber: 1,
            pageSize: 10);

        service.Setup(s => s.GetApartmentDetailsWingWiseAsync(It.IsAny<GetApartmentDetailsWingWiseQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new GetApartmentDetailsWingWiseQueryParameters { WingDetailId = 99 };
        var result = await controller.GetApartmentDetailsWingWise(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PagedResult<ApartmentQCComparisonDto>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("No records found", apiResponse.Message);
        Assert.Equal(0, apiResponse.Items!.TotalCount);
    }
}
