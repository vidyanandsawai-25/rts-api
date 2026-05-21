using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for CombinePropertyValidator
/// Tests validation logic for property combination operations
/// </summary>
public class CombinePropertyValidatorTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<ILogger<CombinePropertyValidator>> _mockLogger;
    private readonly CombinePropertyValidator _validator;

    public CombinePropertyValidatorTests()
    {
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockLogger = new Mock<ILogger<CombinePropertyValidator>>();
        _validator = new CombinePropertyValidator(_mockRepository.Object, _mockLogger.Object);
    }

    #region Main Property Validation Tests

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_MainPropertyNotFound_ReturnsFailure()
    {
        // Arrange
        var mainPropertyId = 999;
        var combinePropertyIds = new List<int> { 2, 3 };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyEntity?)null);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, default);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("MainPropertyId not found.", result.ErrorMessage);
        Assert.Empty(result.ValidProperties);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_MainPropertyExists_ContinuesValidation()
    {
        // Arrange
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2, 3 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "123",
            OwnerName = "John Doe",
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "124", OwnerName = "John Doe", IsActive = true },
            new() { Id = 3, PropertyNo = "125", OwnerName = "John Doe", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, default);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.ValidProperties.Count);
    }

    #endregion

    #region Combine Properties Validation Tests

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_CombinePropertiesNotFound_ReturnsFailure()
    {
        // Arrange
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 999, 998 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "123",
            OwnerName = "John Doe",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity>().BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, default);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("One or more CombinedPropertyIds not found.", result.ErrorMessage);
        Assert.Empty(result.ValidProperties);
    }

    #endregion

    #region Owner Name Validation Tests

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_OwnerNameMismatch_ReturnsFailure()
    {
        // Arrange
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2, 3 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "123",
            OwnerName = "John Doe",
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "124", OwnerName = "Jane Smith", IsActive = true }, // Different owner
            new() { Id = 3, PropertyNo = "125", OwnerName = "John Doe", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, default);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Owner name must match for all properties.", result.ErrorMessage);
        Assert.Empty(result.ValidProperties);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_OwnerNameMatchesCaseInsensitive_ReturnsSuccess()
    {
        // Arrange
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2, 3 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "123",
            OwnerName = "JOHN DOE",
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "124", OwnerName = "john doe", IsActive = true },
            new() { Id = 3, PropertyNo = "125", OwnerName = "John Doe", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, default);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.ValidProperties.Count);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_OwnerNameWithWhitespace_ReturnsSuccess()
    {
        // Arrange
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "123",
            OwnerName = "  John Doe  ",
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "124", OwnerName = "John Doe", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, default);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Single(result.ValidProperties);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_NullOwnerNames_ReturnsSuccess()
    {
        // Arrange
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "123",
            OwnerName = null,
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "124", OwnerName = null, IsActive = true }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, default);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Single(result.ValidProperties);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_ValidProperties_ReturnsSuccess()
    {
        // Arrange
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2, 3, 4 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "123",
            OwnerName = "John Doe",
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "124", OwnerName = "John Doe", IsActive = true },
            new() { Id = 3, PropertyNo = "125", OwnerName = "John Doe", IsActive = true },
            new() { Id = 4, PropertyNo = "126", OwnerName = "John Doe", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, default);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(3, result.ValidProperties.Count);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_SinglePropertyCombination_ReturnsSuccess()
    {
        // Arrange
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "123",
            OwnerName = "John Doe",
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "124", OwnerName = "John Doe", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, default);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Single(result.ValidProperties);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_EmptyOwnerNames_ReturnsSuccess()
    {
        // Arrange
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "123",
            OwnerName = "",
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "124", OwnerName = "", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, default);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Single(result.ValidProperties);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_InactiveProperties_NotReturned()
    {
        // Arrange
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2, 3 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "123",
            OwnerName = "John Doe",
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "124", OwnerName = "John Doe", IsActive = true },
            // Property 3 is inactive and won't be in the query result
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, default);

        // Assert - Should fail because not all requested properties were found
        Assert.False(result.IsValid);
        Assert.Equal("One or more CombinedPropertyIds not found.", result.ErrorMessage);
    }

    #endregion
}