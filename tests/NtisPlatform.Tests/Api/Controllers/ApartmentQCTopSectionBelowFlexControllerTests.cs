using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class ApartmentQCTopSectionBelowFlexControllerTests
{
    private static ApartmentQCController Create(out Mock<IApartmentQcTopSectionBelowFlexService> service)
    {
        service = new Mock<IApartmentQcTopSectionBelowFlexService>();
        return new ApartmentQCController(
            new Mock<IApartmentQCService>().Object,
            new Mock<IWingWiseDetailsService>().Object,
            new Mock<IRateableValueService>().Object,
            new Mock<ICapitalValueService>().Object,
            new Mock<IApartmentQcCertificateGridService>().Object,
            new Mock<IApartmentQcSearchService>().Object,
            new Mock<IApartmentQcTopSectionService>().Object,
            service.Object,
            new Mock<IApartmentTaxDetailsService>().Object,
            new Mock<IPropertyCertificateApplicationService>().Object,
            new Mock<NtisPlatform.Application.Interfaces.Master.ISocialAttributeService>().Object,
            NullLogger<ApartmentQCController>.Instance,
            new Mock<IGetApartmentDetailsWingWiseService>().Object);
    }

    [Fact]
    public async Task GetTopSectionBelowFlex_NoIdentifier_ReturnsBadRequest()
    {
        var controller = Create(out var service);

        var result = await controller.GetTopSectionBelowFlex(
            propertyId: null, upic: null, ward: null, propertyNo: null,
            partitionNo: null, wingDetailsId: null, societyId: null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        service.Verify(s => s.GetBelowFlexAsync(It.IsAny<ApartmentQcTopSectionQueryParameters>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTopSectionBelowFlex_PropertyNotFound_ReturnsNotFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetBelowFlexAsync(It.IsAny<ApartmentQcTopSectionQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApartmentQcBelowFlexDto?)null);

        var result = await controller.GetTopSectionBelowFlex(
            propertyId: 999, upic: null, ward: null, propertyNo: null,
            partitionNo: null, wingDetailsId: null, societyId: null, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFound.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetTopSectionBelowFlex_PropertyFound_ReturnsOkWithData()
    {
        var controller = Create(out var service);
        var dto = new ApartmentQcBelowFlexDto
        {
            PropertyId = 5,
            WorkflowStages = new List<WorkflowStageStatusDto>
            {
                new() { StageId = 1, StageName = "GIS Verification", IsCompleted = true }
            },
            CertificateTypes = new List<CertificateTypeStatusDto>
            {
                new() { CertificateTypeId = 1, CertificateTypeCode = "CC", CertificateTypeName = "Completion Certificate", IsIssued = false }
            }
        };
        service.Setup(s => s.GetBelowFlexAsync(It.IsAny<ApartmentQcTopSectionQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await controller.GetTopSectionBelowFlex(
            propertyId: 5, upic: null, ward: null, propertyNo: null,
            partitionNo: null, wingDetailsId: null, societyId: null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<ApartmentQcBelowFlexDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal(5, response.Items!.PropertyId);
        Assert.Single(response.Items.WorkflowStages);
        Assert.Single(response.Items.CertificateTypes);
    }

    [Fact]
    public async Task GetTopSectionBelowFlex_ByUpic_ResolvesViaQueryParameters()
    {
        var controller = Create(out var service);
        ApartmentQcTopSectionQueryParameters? captured = null;
        service.Setup(s => s.GetBelowFlexAsync(It.IsAny<ApartmentQcTopSectionQueryParameters>(), It.IsAny<CancellationToken>()))
            .Callback<ApartmentQcTopSectionQueryParameters, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(new ApartmentQcBelowFlexDto { PropertyId = 7 });

        await controller.GetTopSectionBelowFlex(
            propertyId: null, upic: "UPIC123", ward: null, propertyNo: null,
            partitionNo: null, wingDetailsId: null, societyId: null, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("UPIC123", captured!.Upic);
    }
}
