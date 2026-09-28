using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Property.ApartmentTaxDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class ApartmentTaxDetailsControllerTests
{
    private static ApartmentQCController Create(out Mock<IApartmentTaxDetailsService> service)
    {
        service = new Mock<IApartmentTaxDetailsService>();
        return new ApartmentQCController(
            new Mock<IApartmentQCService>().Object,
            new Mock<IWingWiseDetailsService>().Object,
            new Mock<IRateableValueService>().Object,
            new Mock<ICapitalValueService>().Object,
            new Mock<IApartmentQcCertificateGridService>().Object,
            new Mock<IApartmentQcSearchService>().Object,
            new Mock<IApartmentQcTopSectionService>().Object,
            new Mock<IApartmentQcTopSectionBelowFlexService>().Object,
            service.Object,
            new Mock<IPropertyCertificateApplicationService>().Object,
            new Mock<ISocialAttributeService>().Object,
            NullLogger<ApartmentQCController>.Instance,
            new Mock<IGetApartmentDetailsWingWiseService>().Object,
            new Mock<IApartmentDashboardService>().Object);
    }

    [Fact]
    public async Task GetTaxDetails_MissingPropertyNo_ReturnsBadRequest()
    {
        var controller = Create(out var service);

        var result = await controller.GetTaxDetails(wardId: 96, propertyNo: " ", wingMasterId: null, taxType: "RV", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        service.Verify(s => s.GetTaxDetailsAsync(It.IsAny<ApartmentTaxDetailsQueryParameters>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTaxDetails_InvalidTaxType_ReturnsBadRequest()
    {
        var controller = Create(out var service);

        var result = await controller.GetTaxDetails(wardId: 96, propertyNo: "20", wingMasterId: null, taxType: "bogus", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        service.Verify(s => s.GetTaxDetailsAsync(It.IsAny<ApartmentTaxDetailsQueryParameters>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTaxDetails_PropertyNotFound_ReturnsNotFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetTaxDetailsAsync(It.IsAny<ApartmentTaxDetailsQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApartmentTaxDetailsDto?)null);

        var result = await controller.GetTaxDetails(wardId: 96, propertyNo: "999", wingMasterId: null, taxType: "RV", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFound.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetTaxDetails_PropertyFound_ReturnsOkWithData()
    {
        var controller = Create(out var service);
        var dto = new ApartmentTaxDetailsDto
        {
            WardId = 96,
            PropertyNo = "20",
            PropertyCount = 2,
            CurrentTaxes = new List<CurrentTaxByTypeDto>
            {
                new() { TaxType = "RV", TaxHeads = new List<TaxHeadAmountDto> { new() { TaxId = 1, TaxName = "General Tax", TaxAmount = 250m } } }
            },
            Arrears = new List<TaxHeadAmountDto>()
        };
        service.Setup(s => s.GetTaxDetailsAsync(It.IsAny<ApartmentTaxDetailsQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await controller.GetTaxDetails(wardId: 96, propertyNo: "20", wingMasterId: null, taxType: "RV", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<ApartmentTaxDetailsDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Items!.PropertyCount);
        var rv = Assert.Single(response.Items.CurrentTaxes);
        Assert.Equal("RV", rv.TaxType);
    }

    [Fact]
    public async Task GetTaxDetails_ValidRequest_MapsQueryParametersCaseInsensitiveTaxType()
    {
        var controller = Create(out var service);
        ApartmentTaxDetailsQueryParameters? captured = null;
        service.Setup(s => s.GetTaxDetailsAsync(It.IsAny<ApartmentTaxDetailsQueryParameters>(), It.IsAny<CancellationToken>()))
            .Callback<ApartmentTaxDetailsQueryParameters, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(new ApartmentTaxDetailsDto { WardId = 96, PropertyNo = "20" });

        await controller.GetTaxDetails(wardId: 96, propertyNo: "20", wingMasterId: 5, taxType: "dual", CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(96, captured!.WardId);
        Assert.Equal("20", captured.PropertyNo);
        Assert.Equal(5, captured.WingMasterId);
        Assert.Equal(ApartmentTaxType.Dual, captured.TaxType);
    }
}
