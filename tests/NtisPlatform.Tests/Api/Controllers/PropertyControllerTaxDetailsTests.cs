using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;
using NtisPlatform.Application.DTOs.Property;
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
                    TaxAmounts = new List<TaxAmountDetail>
                    {
                        new TaxAmountDetail { TaxName = "Property Tax", TaxAmount = 1000.00m },
                        new TaxAmountDetail { TaxName = "Water Tax", TaxAmount = 500.00m }
                    },
                    TaxTotal = 1500.00m
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
        Assert.Equal(1500.00m, response.Items.Policies[0].TaxTotal);
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
                    TaxAmounts = new List<TaxAmountDetail>
                    {
                        new TaxAmountDetail { TaxName = "Capital Value Tax", TaxAmount = 2000.00m },
                        new TaxAmountDetail { TaxName = "Education Cess", TaxAmount = 750.00m }
                    },
                    TaxTotal = 2750.00m
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

    #region GetApartmentPropertyTaxDetailsRV Tests

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsRV_NoTaxDetails_ReturnsNotFound()
    {
        // Arrange
        _mockPropertyService
            .Setup(s => s.GetAggregatedPropertyTaxDetailsAsync(It.IsAny<PropertyApartmentTaxRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTaxApartmentDetailsDto?)null);

        // Act
        var result = await _controller.GetApartmentPropertyTaxDetailsRV(new PropertyQueryParameters(), CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyTaxApartmentDetailsDto>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("No tax details found", response.Message);
    }

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsRV_WithData_ReturnsOk()
    {
        // Arrange
        var dto = new PropertyTaxApartmentDetailsDto
        {
            PropertyCount = 2,
            TaxAmounts = new List<TaxAmountDto> { new TaxAmountDto { TaxName = "Property Tax", TaxAmount = 1000, DisplayOrder = 1 } }
        };
        _mockPropertyService
            .Setup(s => s.GetAggregatedPropertyTaxDetailsAsync(It.IsAny<PropertyApartmentTaxRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetApartmentPropertyTaxDetailsRV(new PropertyQueryParameters(), CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyTaxApartmentDetailsDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(2, response.Items.PropertyCount);
        Assert.Single(response.Items.TaxAmounts);
    }

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsRV_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockPropertyService
            .Setup(s => s.GetAggregatedPropertyTaxDetailsAsync(It.IsAny<PropertyApartmentTaxRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetApartmentPropertyTaxDetailsRV(new PropertyQueryParameters(), CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
        var response = Assert.IsType<ApiResponse<PropertyTaxApartmentDetailsDto>>(statusResult.Value);
        Assert.False(response.Success);
        Assert.Contains("error occurred", response.Message);
    }

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsRV_MapsQueryParamsToDto()
    {
        // Arrange
        var query = new PropertyQueryParameters { WardId = 5, PropertyNo = "P123", PartType = "A", Type = "Flat", Id = 42 };
        PropertyApartmentTaxRequestDto? capturedDto = null;
        _mockPropertyService
            .Setup(s => s.GetAggregatedPropertyTaxDetailsAsync(It.IsAny<PropertyApartmentTaxRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<PropertyApartmentTaxRequestDto, CancellationToken>((dto, ct) => capturedDto = dto)
            .ReturnsAsync(new PropertyTaxApartmentDetailsDto());

        // Act
        await _controller.GetApartmentPropertyTaxDetailsRV(query, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedDto);
        Assert.Equal(5, capturedDto.WardId);
        Assert.Equal("P123", capturedDto.PropertyNo);
        Assert.Equal("A", capturedDto.PartType);
        Assert.Equal("Flat", capturedDto.Type);
        Assert.Equal(42, capturedDto.PropertyId);
    }

    #endregion

    #region GetApartmentPropertyTaxDetailsCV Tests

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsCV_NoTaxDetails_ReturnsNotFound()
    {
        // Arrange
        _mockPropertyService
            .Setup(s => s.GetAggregatedPropertyTaxDetailsCVAsync(It.IsAny<PropertyApartmentTaxRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTaxApartmentDetailsCVDto?)null);

        // Act
        var result = await _controller.GetApartmentPropertyTaxDetailsCV(new PropertyQueryParameters(), CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyTaxApartmentDetailsCVDto>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("No CV tax details found", response.Message);
    }

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsCV_WithData_ReturnsOk()
    {
        // Arrange
        var dto = new PropertyTaxApartmentDetailsCVDto
        {
            PropertyCount = 3,
            TaxAmounts = new List<TaxAmountDto> { new TaxAmountDto { TaxName = "CV Tax", TaxAmount = 2000, DisplayOrder = 1 } }
        };
        _mockPropertyService
            .Setup(s => s.GetAggregatedPropertyTaxDetailsCVAsync(It.IsAny<PropertyApartmentTaxRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetApartmentPropertyTaxDetailsCV(new PropertyQueryParameters(), CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyTaxApartmentDetailsCVDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(3, response.Items.PropertyCount);
        Assert.Single(response.Items.TaxAmounts);
    }

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsCV_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockPropertyService
            .Setup(s => s.GetAggregatedPropertyTaxDetailsCVAsync(It.IsAny<PropertyApartmentTaxRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetApartmentPropertyTaxDetailsCV(new PropertyQueryParameters(), CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
        var response = Assert.IsType<ApiResponse<PropertyTaxApartmentDetailsCVDto>>(statusResult.Value);
        Assert.False(response.Success);
        Assert.Contains("error occurred", response.Message);
    }

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsCV_MapsQueryParamsToDto()
    {
        // Arrange
        var query = new PropertyQueryParameters { WardId = 7, PropertyNo = "P999", PartType = "B", Type = "Shop", Id = 99 };
        PropertyApartmentTaxRequestDto? capturedDto = null;
        _mockPropertyService
            .Setup(s => s.GetAggregatedPropertyTaxDetailsCVAsync(It.IsAny<PropertyApartmentTaxRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<PropertyApartmentTaxRequestDto, CancellationToken>((dto, ct) => capturedDto = dto)
            .ReturnsAsync(new PropertyTaxApartmentDetailsCVDto());

        // Act
        await _controller.GetApartmentPropertyTaxDetailsCV(query, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedDto);
        Assert.Equal(7, capturedDto.WardId);
        Assert.Equal("P999", capturedDto.PropertyNo);
        Assert.Equal("B", capturedDto.PartType);
        Assert.Equal("Shop", capturedDto.Type);
        Assert.Equal(99, capturedDto.PropertyId);
    }

    #endregion
}
