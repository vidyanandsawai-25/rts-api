using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Tests for PropertyController tax details endpoints.
/// Verifies 404 behavior when tax details are not found and successful responses when tax details exist.
/// </summary>
public class PropertyControllerTaxDetailsTests
{
    private readonly Mock<IPropertyService> _mockPropertyService;
    private readonly Mock<ILogger<PropertyController>> _mockLogger;
    private readonly PropertyController _controller;

    public PropertyControllerTaxDetailsTests()
    {
        _mockPropertyService = new Mock<IPropertyService>();
        _mockLogger = new Mock<ILogger<PropertyController>>();
        _controller = new PropertyController(_mockPropertyService.Object, _mockLogger.Object);
    }

    #region GetTaxDetails Tests

    [Fact]
    public async Task GetTaxDetails_PropertyNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockPropertyService
            .Setup(s => s.GetTaxDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTaxDetailsDto?)null);

        // Act
        var result = await _controller.GetTaxDetails(999, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyTaxDetailsDto>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("No tax details found", response.Message);
    }

    [Fact]
    public async Task GetTaxDetails_NoTaxDetails_ReturnsNotFound()
    {
        // Arrange - Property exists but all tax details are filtered out
        _mockPropertyService
            .Setup(s => s.GetTaxDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTaxDetailsDto?)null);

        // Act
        var result = await _controller.GetTaxDetails(1, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyTaxDetailsDto>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("No tax details found", response.Message);
    }

    [Fact]
    public async Task GetTaxDetails_WithPolicies_ReturnsOk()
    {
        // Arrange
        var dto = new PropertyTaxDetailsDto
        {
            PropertyId = 1,
            Policies = new List<PolicyTaxDetail>
            {
                new PolicyTaxDetail
                {
                    PolicyCode = "POL2024",
                    TaxAmounts = new Dictionary<string, decimal?>
                    {
                        { "Property Tax", 1000.00m },
                        { "Water Tax", 500.00m }
                    }
                }
            }
        };

        _mockPropertyService
            .Setup(s => s.GetTaxDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetTaxDetails(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyTaxDetailsDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Single(response.Items.Policies);
        Assert.Equal("POL2024", response.Items.Policies[0].PolicyCode);
    }

    [Fact]
    public async Task GetTaxDetails_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockPropertyService
            .Setup(s => s.GetTaxDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetTaxDetails(1, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
        var response = Assert.IsType<ApiResponse<PropertyTaxDetailsDto>>(statusResult.Value);
        Assert.False(response.Success);
        Assert.Contains("error occurred", response.Message);
    }

    #endregion

    #region GetTaxDetailsCV Tests

    [Fact]
    public async Task GetTaxDetailsCV_PropertyNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockPropertyService
            .Setup(s => s.GetTaxDetailsCVAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTaxDetailsCVDto?)null);

        // Act
        var result = await _controller.GetTaxDetailsCV(999, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyTaxDetailsCVDto>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("No CV tax details found", response.Message);
    }

    [Fact]
    public async Task GetTaxDetailsCV_NoTaxDetails_ReturnsNotFound()
    {
        // Arrange - Property exists but all CV tax details are filtered out
        _mockPropertyService
            .Setup(s => s.GetTaxDetailsCVAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTaxDetailsCVDto?)null);

        // Act
        var result = await _controller.GetTaxDetailsCV(1, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyTaxDetailsCVDto>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("No CV tax details found", response.Message);
    }

    [Fact]
    public async Task GetTaxDetailsCV_WithPolicies_ReturnsOk()
    {
        // Arrange
        var dto = new PropertyTaxDetailsCVDto
        {
            PropertyId = 1,
            Policies = new List<PolicyTaxDetail>
            {
                new PolicyTaxDetail
                {
                    PolicyCode = "POLCV2024",
                    TaxAmounts = new Dictionary<string, decimal?>
                    {
                        { "Capital Value Tax", 2000.00m },
                        { "Education Cess", 750.00m }
                    }
                }
            }
        };

        _mockPropertyService
            .Setup(s => s.GetTaxDetailsCVAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetTaxDetailsCV(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyTaxDetailsCVDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Single(response.Items.Policies);
        Assert.Equal("POLCV2024", response.Items.Policies[0].PolicyCode);
    }

    [Fact]
    public async Task GetTaxDetailsCV_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockPropertyService
            .Setup(s => s.GetTaxDetailsCVAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetTaxDetailsCV(1, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
        var response = Assert.IsType<ApiResponse<PropertyTaxDetailsCVDto>>(statusResult.Value);
        Assert.False(response.Success);
        Assert.Contains("error occurred", response.Message);
    }

    #endregion
}
