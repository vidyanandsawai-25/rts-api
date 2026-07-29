using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Unit tests for PropertyController.Merge endpoints
/// </summary>
public class PropertyControllerMergeTests
{
    private readonly Mock<IPropertyService> _mockPropertyService;
    private readonly Mock<IPropertyBasicDetailsService> _mockPropertyBasicDetailsService;
    private readonly Mock<IPropertyKycService> _mockPropertyKycService;
    private readonly Mock<IPropertySocietyService> _mockPropertySocietyService;
    private readonly Mock<IPropertyDiscountService> _mockPropertyDiscountService;
    private readonly Mock<IPropertyOldDetailsService> _mockPropertyOldDetailsService;
    private readonly Mock<IPropertySearchService> _mockPropertySearchService;
    private readonly Mock<ILogger<PropertyController>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
    private readonly Mock<FileValidationHelper> _mockFileValidationHelper;
    private readonly Mock<IPropertyWorkflowDetailsService> _mockPropertyWorkflowDetailsService;
    private readonly PropertyController _controller;

    public PropertyControllerMergeTests()
    {
        _mockPropertyService = new Mock<IPropertyService>();
        _mockPropertyBasicDetailsService = new Mock<IPropertyBasicDetailsService>();
        _mockPropertyKycService = new Mock<IPropertyKycService>();
        _mockPropertySocietyService = new Mock<IPropertySocietyService>();
        _mockPropertyDiscountService = new Mock<IPropertyDiscountService>();
        _mockPropertyOldDetailsService = new Mock<IPropertyOldDetailsService>();
        _mockPropertySearchService = new Mock<IPropertySearchService>();
        _mockLogger = new Mock<ILogger<PropertyController>>();
        _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(s => s.GetChildren()).Returns(Array.Empty<IConfigurationSection>());
        mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockConfigSection.Object);
        _mockFileValidationHelper = new Mock<FileValidationHelper>(mockConfiguration.Object);
        _mockPropertyWorkflowDetailsService = new Mock<IPropertyWorkflowDetailsService>();

        _controller = new PropertyController(
            _mockPropertyService.Object,
            _mockPropertyBasicDetailsService.Object,
            _mockPropertyKycService.Object,
            _mockPropertySocietyService.Object,
            _mockPropertyDiscountService.Object,
            _mockPropertyOldDetailsService.Object,
            _mockPropertySearchService.Object,
            _mockLogger.Object,
            _mockWebHostEnvironment.Object,
            _mockFileValidationHelper.Object,
            _mockPropertyWorkflowDetailsService.Object
        );
    }

    #region MergePropertyAsync Tests

    [Fact]
    public async Task MergePropertyAsync_WithValidOneToOneMapping_ReturnsOkResult()
    {
        // Arrange
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 },
            Latitude = "18.5204",
            Longitude = "73.8567"
        };

        var serviceResponse = new PropertyResponse
        {
            Success = true,
            Message = "Property merged successfully"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyMergeDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Property merged successfully", response.Message);
        _mockPropertyService.Verify(s => s.MergePropertyAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MergePropertyAsync_WithValidSplitMapping_ReturnsOkResult()
    {
        // Arrange - 1 old property to multiple new properties (SPLIT)
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1, 2, 3 },
            PropertyOldIds = new List<int> { 100 },
            Latitude = "18.5204",
            Longitude = "73.8567"
        };

        var serviceResponse = new PropertyResponse
        {
            Success = true,
            Message = "Properties split successfully"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyMergeDto>>(okResult.Value);
        Assert.True(response.Success);
        _mockPropertyService.Verify(s => s.MergePropertyAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MergePropertyAsync_WithValidMergeMapping_ReturnsOkResult()
    {
        // Arrange - multiple old properties to 1 new property (MERGE)
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100, 101, 102 },
            Latitude = "18.5204",
            Longitude = "73.8567"
        };

        var serviceResponse = new PropertyResponse
        {
            Success = true,
            Message = "Properties merged successfully"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyMergeDto>>(okResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task MergePropertyAsync_WithNullPropertyIds_ReturnsBadRequest()
    {
        // Arrange
        var dto = new PropertyMergeDto
        {
            PropertyIds = null,
            PropertyOldIds = new List<int> { 100 }
        };

        var serviceResponse = new PropertyResponse
        {
            Success = false,
            Message = "Property details are required"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyMergeDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Property details are required", response.Message);
    }

    [Fact]
    public async Task MergePropertyAsync_WithNullPropertyOldIds_ReturnsBadRequest()
    {
        // Arrange
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = null
        };

        var serviceResponse = new PropertyResponse
        {
            Success = false,
            Message = "Property details are required"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyMergeDto>>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task MergePropertyAsync_WithOldPropertyNotFound_ReturnsFailureResponse()
    {
        // Arrange
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 999 }
        };

        var serviceResponse = new PropertyResponse
        {
            Success = false,
            Message = "Old Property not found"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyMergeDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Old Property not found", response.Message);
    }

    [Fact]
    public async Task MergePropertyAsync_WithNewPropertyNotFound_ReturnsFailureResponse()
    {
        // Arrange
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 999 },
            PropertyOldIds = new List<int> { 100 }
        };

        var serviceResponse = new PropertyResponse
        {
            Success = false,
            Message = "New Property not found"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyMergeDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("New Property not found", response.Message);
    }

    [Fact]
    public async Task MergePropertyAsync_WithPropertyAlreadyMerged_ReturnsFailureResponse()
    {
        // Arrange
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 }
        };

        var serviceResponse = new PropertyResponse
        {
            Success = false,
            Message = "New properties 1-101 already merged for old properties: 100"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyMergeDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("already merged", response.Message);
    }

    [Fact]
    public async Task MergePropertyAsync_WithInvalidMappingCategory_ReturnsFailureResponse()
    {
        // Arrange - Multiple old to multiple new (invalid)
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1, 2 },
            PropertyOldIds = new List<int> { 100, 101 }
        };

        var serviceResponse = new PropertyResponse
        {
            Success = false,
            Message = "Multiple old properties cannot be merged with multiple new properties"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyMergeDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Multiple old properties cannot be merged with multiple new properties", response.Message);
    }

    [Fact]
    public async Task MergePropertyAsync_WithException_Returns500Response()
    {
        // Arrange
        // The controller catches all exceptions internally and returns a 500 ObjectResult.
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 }
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act
        var result = await _controller.MergePropertyAsync(dto, CancellationToken.None);

        // Assert - controller handles exceptions and returns 500 with error ApiResponse
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        var response = Assert.IsType<ApiResponse<PropertyMergeDto>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("An error occurred while merging properties", response.Message);
    }

    [Fact]
    public async Task MergePropertyAsync_WithCoordinates_StoresLocationData()
    {
        // Arrange
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 },
            Latitude = "18.5204303",
            Longitude = "73.8567437"
        };

        var serviceResponse = new PropertyResponse
        {
            Success = true,
            Message = "Property merged successfully with location data"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.Is<PropertyMergeDto>(d => 
                d.Latitude == "18.5204303" && d.Longitude == "73.8567437"), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyMergeDto>>(okResult.Value);
        Assert.True(response.Success);
        _mockPropertyService.Verify(s => s.MergePropertyAsync(
            It.Is<PropertyMergeDto>(d => d.Latitude == "18.5204303" && d.Longitude == "73.8567437"), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // NOTE: GetUnMergePropertyDetailsAsync and GetUnMergeOldPropertyDetailsAsync 
    // tests are commented out as these methods need to be added to PropertyController.Merge.cs first

    // Uncomment these tests once the controller methods are implemented:

    /*
    #region GetUnMergePropertyDetailsAsync Tests

    [Fact]
    public async Task GetUnMergePropertyDetailsAsync_WithValidRequest_ReturnsOkWithData()
    {
        // Arrange
        var request = new UnMergePropertydetailDto
        {
            PropertyId = 1,
            WingName = "A"
        };

        var expectedResponse = new List<PropertyUnMergeResponseDto>
        {
            new PropertyUnMergeResponseDto
            {
                PropertyId = 1,
                WardNo = "1",
                PropertyNo = "101",
                PartitionNo = "1",
                OwnerName = "John Doe"
            }
        };

        _mockPropertyService
            .Setup(s => s.GetUnMergePropertyDetailsAsync(It.IsAny<UnMergePropertydetailDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetUnMergePropertyDetailsAsync(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<List<PropertyUnMergeResponseDto>>(okResult.Value);
        Assert.Single(response);
        Assert.Equal(1, response[0].PropertyId);
        Assert.Equal("John Doe", response[0].OwnerName);
    }

    #endregion

    #region GetUnMergeOldPropertyDetailsAsync Tests

    [Fact]
    public async Task GetUnMergeOldPropertyDetailsAsync_WithValidRequest_ReturnsOkWithData()
    {
        // Arrange
        var request = new UnMergePropertydetailDto
        {
            PropertyId = 1
        };

        var expectedResponse = new List<OldPropertyUnMergeResponseDto>
        {
            new OldPropertyUnMergeResponseDto
            {
                PropertyOldId = 100,
                OldWardNo = "1",
                OldPropertyNo = "100",
                OldOwnerName = "Previous Owner"
            }
        };

        _mockPropertyService
            .Setup(s => s.GetUnMergeOldPropertyDetailsAsync(It.IsAny<UnMergePropertydetailDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetUnMergeOldPropertyDetailsAsync(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<List<OldPropertyUnMergeResponseDto>>(okResult.Value);
        Assert.Single(response);
        Assert.Equal(100, response[0].PropertyOldId);
        Assert.Equal("Previous Owner", response[0].OldOwnerName);
    }

    #endregion
    */

    #region Integration Scenarios - MergePropertyAsync Only

    [Fact]
    public async Task MergeProperty_WithValidData_Succeeds()
    {
        // Arrange
        var mergeDto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 }
        };

        var serviceResponse = new PropertyResponse { Success = true, Message = "Merged successfully" };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var mergeResult = await _controller.MergePropertyAsync(mergeDto, CancellationToken.None);

        // Assert
        var mergeOkResult = Assert.IsType<OkObjectResult>(mergeResult);
        var mergeResponseData = Assert.IsType<ApiResponse<PropertyMergeDto>>(mergeOkResult.Value);
        Assert.True(mergeResponseData.Success);
        Assert.Equal("Merged successfully", mergeResponseData.Message);

        _mockPropertyService.Verify(s => s.MergePropertyAsync(
            It.Is<PropertyMergeDto>(d => d.PropertyIds.Count == 1 && d.PropertyOldIds.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
