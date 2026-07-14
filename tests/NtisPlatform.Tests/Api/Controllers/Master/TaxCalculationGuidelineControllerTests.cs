using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master.TaxCalculationGuideline;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class TaxCalculationGuidelineControllerTests
{
    private static TaxCalculationGuidelineController Create(
        out Mock<ITaxCalculationGuidelineService> service,
        out Mock<IHardDeleteCleanupService> cleanup,
        out Mock<IReferenceValidationService> referenceValidation)
    {
        service = new Mock<ITaxCalculationGuidelineService>();
        cleanup = new Mock<IHardDeleteCleanupService>();
        referenceValidation = new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<TaxCalculationGuidelineController>>();

        return new TaxCalculationGuidelineController(
            service.Object,
            cleanup.Object,
            referenceValidation.Object,
            logger.Object);
    }

    [Fact]
    public async Task GetById_WhenRecordExists_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaxCalculationGuidelineDto { Id = 1, GuidelineCode = "G1", GuidelineName = "Guideline" });

        var result = await controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithValidPayload_ReturnsOkResponse()
    {
        var controller = Create(out var service, out _, out _);
        var createDto = new CreateTaxCalculationGuidelineDto
        {
            GuidelineCode = "G1",
            GuidelineName = "Guideline",
            DatePriority1 = "RETROSPECTIVE",
            DatePriority2 = "ELECTRIC_BILL",
            DatePriority3 = "CC",
            DatePriority4 = "OC",
            IgnoreCCToOCIfWithinType = "MONTHS",
            ElectricBillDateRule = "NO_TAX",
            NoDateRule = "DEFAULT_RETROSPECTIVE",
            FloorCertificatePriority = "PROPERTY_OVERRIDES_FLOOR",
            ProrationMethod = "FULL_YEAR",
            TaxPersistenceMode = "PROPERTY_AGGREGATED"
        };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaxCalculationGuidelineDto { Id = 1, GuidelineCode = "G1", GuidelineName = "Guideline" });

        var result = await controller.Create(createDto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TaxCalculationGuidelineDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.Items!.Id);
    }

    [Fact]
    public async Task Update_WhenRecordMissing_ReturnsOkWithFailureResponse()
    {
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdateTaxCalculationGuidelineDto
        {
            GuidelineCode = "G1",
            GuidelineName = "Guideline",
            DatePriority1 = "RETROSPECTIVE",
            DatePriority2 = "ELECTRIC_BILL",
            DatePriority3 = "CC",
            DatePriority4 = "OC",
            IgnoreCCToOCIfWithinType = "MONTHS",
            ElectricBillDateRule = "NO_TAX",
            NoDateRule = "DEFAULT_RETROSPECTIVE",
            FloorCertificatePriority = "PROPERTY_OVERRIDES_FLOOR",
            ProrationMethod = "FULL_YEAR",
            TaxPersistenceMode = "PROPERTY_AGGREGATED"
        };

        service.Setup(s => s.UpdateAsync(99, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxCalculationGuidelineDto?)null);

        var result = await controller.Update(99, updateDto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TaxCalculationGuidelineDto>>(ok.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Delete_WhenDeleteSucceeds_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }
}
