using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertySocialDetails;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Tests for PropertyController Discount Details endpoints
/// </summary>
public class PropertyControllerDiscountDetailsTests
{
    private readonly Mock<IPropertyService> _mockPropertyService;
    private readonly Mock<IPropertyDiscountService> _mockDiscountService;
    private readonly Mock<ILogger<PropertyController>> _mockLogger;
    private readonly PropertyController _controller;

    public PropertyControllerDiscountDetailsTests()
    {
        _mockPropertyService = new Mock<IPropertyService>();
        _mockDiscountService = new Mock<IPropertyDiscountService>();
        _mockLogger = new Mock<ILogger<PropertyController>>();

        // Create controller with all dependencies
        var mockEnvironment = new Mock<IWebHostEnvironment>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var fileValidationHelper = new FileValidationHelper(configuration);

        _controller = new PropertyController(
            _mockPropertyService.Object,
            new Mock<IPropertyBasicDetailsService>().Object,
            new Mock<IPropertyKycService>().Object,
            new Mock<IPropertySocietyService>().Object,
            _mockDiscountService.Object,
            new Mock<IPropertyOldDetailsService>().Object,
            new Mock<IPropertySearchService>().Object,
            _mockLogger.Object,
            mockEnvironment.Object,
            fileValidationHelper,
            new Mock<IPropertyWorkflowDetailsService>().Object);

        // Set up HttpContext with authenticated user
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        }, "TestAuth"));
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    #region GetDiscountDetails Tests

    [Fact]
    public async Task GetDiscountDetails_ReturnsOk_WhenPropertyFound()
    {
        // Arrange
        var propertyId = 1;
        var expectedResponse = new PropertyDiscountInfoResponseDto
        {
            PropertyId = propertyId,
            DiscountAttributes = new List<DiscountAttributeDto>
            {
                new() { Id = 1, SocialAttributeCode = "SOLAR", SocialAttributeName = "Solar Panel" }
            }
        };

        _mockDiscountService.Setup(s => s.GetDiscountDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetDiscountDetails(propertyId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertyDiscountInfoResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Discount information retrieved successfully", apiResponse.Message);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(propertyId, apiResponse.Items.PropertyId);
    }

    [Fact]
    public async Task GetDiscountDetails_ReturnsNotFound_WhenPropertyDoesNotExist()
    {
        // Arrange
        var propertyId = 999;
        _mockDiscountService.Setup(s => s.GetDiscountDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDiscountInfoResponseDto?)null);

        // Act
        var result = await _controller.GetDiscountDetails(propertyId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertyDiscountInfoResponseDto>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("not found", apiResponse.Message);
    }

    #endregion

    #region UpdateDiscountDetails Tests

    [Fact]
    public async Task UpdateDiscountDetails_ReturnsOk_WhenUpdateSuccessful()
    {
        // Arrange
        var propertyId = 1;
        var updateDto = new UpsertPropertyDiscountInfoDto
        {
            PropertyId = propertyId,
            UpdatedBy = 1,
            DiscountAttributes = new List<DiscountAttributeItemDto>
            {
                new() { SocialAttributeId = 1, BitValue = true }
            }
        };

        var expectedResponse = new PropertyDiscountInfoResponseDto
        {
            PropertyId = propertyId,
            DiscountAttributes = new List<DiscountAttributeDto>()
        };

        _mockDiscountService.Setup(s => s.UpdateDiscountDetailsAsync(propertyId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdateDiscountDetails(propertyId, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertyDiscountInfoResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Discount information updated successfully", apiResponse.Message);
    }

    [Fact]
    public async Task UpdateDiscountDetails_ReturnsBadRequest_WhenPropertyIdMismatch()
    {
        // Arrange
        var propertyId = 1;
        var updateDto = new UpsertPropertyDiscountInfoDto
        {
            PropertyId = 2, // Different ID
            UpdatedBy = 1
        };

        // Act
        var result = await _controller.UpdateDiscountDetails(propertyId, updateDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertyDiscountInfoResponseDto>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("does not match", apiResponse.Message);
    }

    [Fact]
    public async Task UpdateDiscountDetails_ReturnsNotFound_WhenPropertyDoesNotExist()
    {
        // Arrange
        var propertyId = 999;
        var updateDto = new UpsertPropertyDiscountInfoDto
        {
            PropertyId = propertyId,
            UpdatedBy = 1
        };

        _mockDiscountService.Setup(s => s.UpdateDiscountDetailsAsync(propertyId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDiscountInfoResponseDto?)null);

        // Act
        var result = await _controller.UpdateDiscountDetails(propertyId, updateDto, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertyDiscountInfoResponseDto>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
    }

    #endregion

    #region Helper Methods

    private static IFormFile CreateMockFormFile(string fileName, string contentType)
    {
        var content = "Test file content"u8.ToArray();
        var stream = new MemoryStream(content);
        var mockFile = new Mock<IFormFile>();

        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        mockFile.Setup(f => f.Length).Returns(content.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

        return mockFile.Object;
    }

    #endregion
}
