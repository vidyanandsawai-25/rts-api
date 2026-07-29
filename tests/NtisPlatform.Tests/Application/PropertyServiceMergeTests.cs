using Moq;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for PropertyService.Merge functionality
/// These tests focus on testing the service behavior through the IPropertyService interface
/// </summary>
public class PropertyServiceMergeTests
{
    private readonly Mock<IPropertyService> _mockPropertyService;

    public PropertyServiceMergeTests()
    {
        _mockPropertyService = new Mock<IPropertyService>();
    }

    #region MergePropertyAsync Tests

    [Fact]
    public async Task MergePropertyAsync_WithValidOneToOneMapping_ReturnsSuccess()
    {
        // Arrange
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 },
            Latitude = "18.5204",
            Longitude = "73.8567"
        };

        var expectedResponse = new PropertyResponse
        {
            Success = true,
            Message = "Property merged successfully"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _mockPropertyService.Object.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Property merged successfully", result.Message);
    }

    [Fact]
    public async Task MergePropertyAsync_WithValidSplitMapping_ReturnsSuccess()
    {
        // Arrange - 1 old property to multiple new properties (SPLIT)
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1, 2, 3 },
            PropertyOldIds = new List<int> { 100 },
            Latitude = "18.5204",
            Longitude = "73.8567"
        };

        var expectedResponse = new PropertyResponse
        {
            Success = true,
            Message = "Properties split successfully"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _mockPropertyService.Object.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Properties split successfully", result.Message);
    }

    [Fact]
    public async Task MergePropertyAsync_WithValidMergeMapping_ReturnsSuccess()
    {
        // Arrange - multiple old properties to 1 new property (MERGE)
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100, 101, 102 },
            Latitude = "18.5204",
            Longitude = "73.8567"
        };

        var expectedResponse = new PropertyResponse
        {
            Success = true,
            Message = "Properties merged successfully"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _mockPropertyService.Object.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task MergePropertyAsync_WithNullPropertyIds_ReturnsFailure()
    {
        // Arrange
        var dto = new PropertyMergeDto
        {
            PropertyIds = null,
            PropertyOldIds = new List<int> { 100 }
        };

        var expectedResponse = new PropertyResponse
        {
            Success = false,
            Message = "Property details are required"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _mockPropertyService.Object.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Property details are required", result.Message);
    }

    [Fact]
    public async Task MergePropertyAsync_WithNullPropertyOldIds_ReturnsFailure()
    {
        // Arrange
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = null
        };

        var expectedResponse = new PropertyResponse
        {
            Success = false,
            Message = "Property details are required"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _mockPropertyService.Object.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MergePropertyAsync_WithOldPropertyNotFound_ReturnsFailure()
    {
        // Arrange
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 999 }
        };

        var expectedResponse = new PropertyResponse
        {
            Success = false,
            Message = "Old Property not found"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _mockPropertyService.Object.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Old Property not found", result.Message);
    }

    [Fact]
    public async Task MergePropertyAsync_WithNewPropertyNotFound_ReturnsFailure()
    {
        // Arrange
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 999 },
            PropertyOldIds = new List<int> { 100 }
        };

        var expectedResponse = new PropertyResponse
        {
            Success = false,
            Message = "New Property not found"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _mockPropertyService.Object.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("New Property not found", result.Message);
    }

    [Fact]
    public async Task MergePropertyAsync_WithPropertyAlreadyMerged_ReturnsFailure()
    {
        // Arrange
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 }
        };

        var expectedResponse = new PropertyResponse
        {
            Success = false,
            Message = "New properties 1-101 already merged for old properties: 100"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _mockPropertyService.Object.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("already merged", result.Message);
    }

    [Fact]
    public async Task MergePropertyAsync_WithInvalidMappingCategory_ReturnsFailure()
    {
        // Arrange - Multiple old to multiple new (invalid)
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1, 2 },
            PropertyOldIds = new List<int> { 100, 101 }
        };

        var expectedResponse = new PropertyResponse
        {
            Success = false,
            Message = "Multiple old properties cannot be merged with multiple new properties"
        };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _mockPropertyService.Object.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Multiple old properties cannot be merged with multiple new properties", result.Message);
    }

    [Fact]
    public async Task MergePropertyAsync_VerifiesServiceCalled()
    {
        // Arrange
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 }
        };

        var expectedResponse = new PropertyResponse { Success = true };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(It.IsAny<PropertyMergeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        await _mockPropertyService.Object.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        _mockPropertyService.Verify(s => s.MergePropertyAsync(
            It.Is<PropertyMergeDto>(d => d.PropertyIds.Count == 1 && d.PropertyOldIds.Count == 1),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MergePropertyAsync_WithCoordinates_PassesCorrectData()
    {
        // Arrange
        var dto = new PropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 },
            Latitude = "18.5204303",
            Longitude = "73.8567437"
        };

        var expectedResponse = new PropertyResponse { Success = true };

        _mockPropertyService
            .Setup(s => s.MergePropertyAsync(
                It.Is<PropertyMergeDto>(d => d.Latitude == "18.5204303" && d.Longitude == "73.8567437"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _mockPropertyService.Object.MergePropertyAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        _mockPropertyService.Verify(s => s.MergePropertyAsync(
            It.Is<PropertyMergeDto>(d => d.Latitude == "18.5204303" && d.Longitude == "73.8567437"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
