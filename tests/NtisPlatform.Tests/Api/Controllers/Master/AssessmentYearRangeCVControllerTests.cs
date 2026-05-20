using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class AssessmentYearRangeCVControllerTests
{
    private static AssessmentYearRangeCVController Create(
        out Mock<IAssessmentYearRangeCVService> service,
        out Mock<IHardDeleteCleanupService> cleanup)
    {
        service = new Mock<IAssessmentYearRangeCVService>();
        cleanup = new Mock<IHardDeleteCleanupService>();
        var logger = new Mock<ILogger<AssessmentYearRangeCVController>>();
        var mockReferenceValidationService = new Mock<IReferenceValidationService>();
        return new AssessmentYearRangeCVController(service.Object, cleanup.Object, mockReferenceValidationService.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service, out _);
        var query = new AssessmentYearRangeCVQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AssessmentYearRangeCVDto>(new List<AssessmentYearRangeCVDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
