using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.RetrospectiveTax;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RuleLibrary;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.RetrospectiveTax;

public class RuleLibraryControllerTests
{
    private readonly Mock<IRuleLibraryService> _mockService;
    private readonly Mock<ILogger<RuleLibraryController>> _mockLogger;
    private readonly RuleLibraryController _controller;

    public RuleLibraryControllerTests()
    {
        _mockService = new Mock<IRuleLibraryService>();
        _mockLogger = new Mock<ILogger<RuleLibraryController>>();

        _controller = new RuleLibraryController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetLibrary_ServiceSucceeds_ReturnsOkObjectResult()
    {
        var query = new RuleLibraryQueryParameters();
        var libraryDto = new RuleLibraryDto
        {
            CommonTaxation = new RuleLibraryCommonTaxationDto { RateModeCode = "CURRENT_YEAR_FOR_ALL_YEARS" },
            Rules = new PagedResult<RuleLibraryRowDto>(new List<RuleLibraryRowDto> { new() { Id = 1, RuleCode = "THA-01" } }, 1, 1, 10)
        };

        _mockService.Setup(s => s.GetLibraryAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(libraryDto);

        var result = await _controller.GetLibrary(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetLibraryAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLibrary_ServiceThrows_ReturnsStatusCode500()
    {
        var query = new RuleLibraryQueryParameters();

        _mockService.Setup(s => s.GetLibraryAsync(query, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _controller.GetLibrary(query, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }
}
