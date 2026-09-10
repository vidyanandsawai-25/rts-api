using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyDashboard;
using NtisPlatform.Application.Interfaces;
using System.Reflection;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyDashboardControllerTests
{
    private readonly Mock<IPropertyDashboardService> _mockService;
    private readonly Mock<ILogger<PropertyDashboardController>> _mockLogger;
    private readonly PropertyDashboardController _controller;

    public PropertyDashboardControllerTests()
    {
        _mockService = new Mock<IPropertyDashboardService>();
        _mockLogger = new Mock<ILogger<PropertyDashboardController>>();
        _controller = new PropertyDashboardController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetPropertyDashboardDetailsAsync_WithValidParameters_ReturnsOkResultWithDashboardData()
    {
        // Arrange
        var queryParameters = new PropertyDashboardQueryParameters
        {
            UserId = 1,
            ZoneId = 2,
            WardId = 3
        };

        var expectedDto = new PropertyDashboardDto
        {
            TotalOldProperties = 150,
            GeoSequencingProperties = 120,
            PropertyAssessment = new PropertyAssessmentDto
            {
                AssessedProperties = 90,
                UnassessedProperties = 30
            },
            Apartment = new ApartmentDashboardDto
            {
                TotalProperty = 50,
                TotalBuilding = 5,
                TotalUnits = 45,
                TotalAmenities = 5,
                AssessedStatus = new AssessedStatusDto
                {
                    Assessed = 40,
                    Unassessed = 10
                }
            },
            Individual = new CategoryDashboardDto
            {
                TotalProperty = 40,
                MainProperty = 35,
                PartitionProperty = 5,
                AssessedStatus = new AssessedStatusDto
                {
                    Assessed = 30,
                    Unassessed = 10
                }
            },
            Industrial = new CategoryDashboardDto
            {
                TotalProperty = 20,
                MainProperty = 18,
                PartitionProperty = 2,
                AssessedStatus = new AssessedStatusDto
                {
                    Assessed = 15,
                    Unassessed = 5
                }
            },
            Plot = new CategoryDashboardDto
            {
                TotalProperty = 10,
                MainProperty = 10,
                PartitionProperty = 0,
                AssessedStatus = new AssessedStatusDto
                {
                    Assessed = 5,
                    Unassessed = 5
                }
            }
        };

        _mockService
            .Setup(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetPropertyDashboardDetailsAsync(queryParameters, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var data = okResult.Value.Should().BeOfType<PropertyDashboardDto>().Subject;
        data.Should().BeEquivalentTo(expectedDto);

        _mockService.Verify(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPropertyDashboardDetailsAsync_PassesCancellationToken_ToService()
    {
        // Arrange
        var queryParameters = new PropertyDashboardQueryParameters { UserId = 5 };
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var expectedDto = new PropertyDashboardDto();

        _mockService
            .Setup(s => s.GetAllAsync(queryParameters, token))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetPropertyDashboardDetailsAsync(queryParameters, token);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expectedDto);
        _mockService.Verify(s => s.GetAllAsync(queryParameters, token), Times.Once);
    }

    [Fact]
    public async Task GetPropertyDashboardDetailsAsync_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var queryParameters = new PropertyDashboardQueryParameters { UserId = 99 };

        _mockService
            .Setup(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database query failed"));

        // Act
        var act = () => _controller.GetPropertyDashboardDetailsAsync(queryParameters, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database query failed");
    }

    [Fact]
    public void Controller_HasRequiredAttributes()
    {
        // Assert ApiController attribute
        typeof(PropertyDashboardController)
            .GetCustomAttribute<ApiControllerAttribute>()
            .Should().NotBeNull("Controller should be decorated with [ApiController]");

        // Assert Authorize attribute
        typeof(PropertyDashboardController)
            .GetCustomAttribute<AuthorizeAttribute>()
            .Should().NotBeNull("Controller should be decorated with [Authorize]");

        // Assert Route attribute
        var routeAttr = typeof(PropertyDashboardController)
            .GetCustomAttribute<RouteAttribute>();
        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be("api/[controller]");
    }

    [Fact]
    public void GetPropertyDashboardDetailsAsync_HasHttpGetAttribute()
    {
        var method = typeof(PropertyDashboardController)
            .GetMethod(nameof(PropertyDashboardController.GetPropertyDashboardDetailsAsync));

        method.Should().NotBeNull();
        var httpGetAttr = method!.GetCustomAttribute<HttpGetAttribute>();
        httpGetAttr.Should().NotBeNull("GetPropertyDashboardDetailsAsync should be decorated with [HttpGet]");
    }
}
