using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.RateableValue;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Covers the standalone "Recalculate RV" endpoint's Occupation Tax follow-up: unlike the
/// certificate-change pipeline (which already runs RV-then-Occupation-Tax in strict order via
/// PropertyCertificateChangedEventHandler), this endpoint previously left CC/OC/Electric-Bill
/// amounts stale after an RV recalculation since nothing else re-triggered them.
/// </summary>
public class RateableValueControllerTests
{
    private static RateableValueController Create(
        out Mock<IRateableValueService> rvService,
        out Mock<IOccupationTaxService> occupationTaxService,
        int? userId = 42,
        bool hasCertificates = true)
    {
        rvService = new Mock<IRateableValueService>();
        occupationTaxService = new Mock<IOccupationTaxService>();
        var logger = new Mock<ILogger<RateableValueController>>();

        var certificateRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        var certificates = hasCertificates
            ? new List<PropertyCertificateEntity> { PropertyCertificateEntity.Create(propertyId: 549441, certificateTypeId: 1) }
            : new List<PropertyCertificateEntity>();
        certificateRepo.Setup(r => r.GetQueryable()).Returns(certificates.BuildMockDbSet().Object);

        var controller = new RateableValueController(rvService.Object, occupationTaxService.Object, certificateRepo.Object, logger.Object);

        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
            }, "TestAuth"));
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task Calculate_OnSuccess_AppliesOccupationTaxWithRequestingUserId()
    {
        const int propertyId = 549441;
        const int userId = 42;

        var controller = Create(out var rvService, out var occupationTaxService, userId);
        rvService.Setup(s => s.CalculateAndSaveAsync(propertyId))
            .ReturnsAsync(new RateableValueResponseDto { PropertyId = propertyId });
        occupationTaxService.Setup(s => s.ApplyAsync(propertyId, userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await controller.Calculate(propertyId);

        Assert.IsType<OkObjectResult>(result.Result);
        occupationTaxService.Verify(s => s.ApplyAsync(propertyId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Calculate_OccupationTaxApplyThrows_StillReturnsOkWithRvResult()
    {
        // A failure in the Occupation Tax follow-up must not fail the RV response that already
        // succeeded and was already persisted.
        const int propertyId = 549441;

        var controller = Create(out var rvService, out var occupationTaxService);
        var rvResult = new RateableValueResponseDto { PropertyId = propertyId, TotalRateableValue = 12345m };
        rvService.Setup(s => s.CalculateAndSaveAsync(propertyId)).ReturnsAsync(rvResult);
        occupationTaxService.Setup(s => s.ApplyAsync(propertyId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.Calculate(propertyId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(rvResult, ok.Value);
    }

    [Fact]
    public async Task Calculate_NoCertificatesForProperty_SkipsOccupationTaxApply()
    {
        const int propertyId = 549441;

        var controller = Create(out var rvService, out var occupationTaxService, hasCertificates: false);
        rvService.Setup(s => s.CalculateAndSaveAsync(propertyId))
            .ReturnsAsync(new RateableValueResponseDto { PropertyId = propertyId });

        var result = await controller.Calculate(propertyId);

        Assert.IsType<OkObjectResult>(result.Result);
        occupationTaxService.Verify(s => s.ApplyAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Calculate_RvCalculationRejected_ReturnsNotFound_AndNeverCallsOccupationTax()
    {
        const int propertyId = 549441;

        var controller = Create(out var rvService, out var occupationTaxService);
        rvService.Setup(s => s.CalculateAndSaveAsync(propertyId))
            .ThrowsAsync(new InvalidOperationException("no property details"));

        var result = await controller.Calculate(propertyId);

        Assert.IsType<NotFoundObjectResult>(result.Result);
        occupationTaxService.Verify(s => s.ApplyAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
