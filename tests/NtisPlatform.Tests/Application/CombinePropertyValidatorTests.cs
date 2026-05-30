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
    private readonly Mock<IRepository<PropertyCategoryEntity, int>> _mockCategoryRepository;
    private readonly Mock<ILogger<CombinePropertyValidator>> _mockLogger;
    private readonly CombinePropertyValidator _validator;

    public CombinePropertyValidatorTests()
    {
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockCategoryRepository = new Mock<IRepository<PropertyCategoryEntity, int>>();
        _mockLogger = new Mock<ILogger<CombinePropertyValidator>>();
        _validator = new CombinePropertyValidator(
            _mockRepository.Object,
            _mockCategoryRepository.Object,
            _mockLogger.Object);
    }

    /// <summary>
    /// Helper method to setup default category repository mocks for non-apartment properties.
    /// This is needed because the validator always runs category validation.
    /// </summary>
    private void SetupNonApartmentCategoryMocks(List<PropertyEntity> combineProperties)
    {
        // Assign category IDs to properties that don't have one
        int categoryIdCounter = 100;
        foreach (var prop in combineProperties)
        {
            if (!prop.CategoryId.HasValue)
            {
                prop.CategoryId = categoryIdCounter++;
            }
        }

        // Setup category for combined properties (non-apartment)
        var categories = combineProperties
            .Where(p => p.CategoryId.HasValue)
            .Select(p => new PropertyCategoryEntity { Id = p.CategoryId!.Value, PropertyCategoryName = "Residential" })
            .ToList();

        _mockCategoryRepository.Setup(r => r.GetQueryable())
            .Returns(categories.BuildMock());
    }

    /// <summary>
    /// Helper method to setup default properties with matching zones and property numbers for successful validation.
    /// </summary>
    private static void SetupMatchingPropertyDetails(PropertyEntity mainProperty, List<PropertyEntity> combineProperties)
    {
        // Set zone, ward, and assign category IDs
        mainProperty.TaxZoneId = 1;
        mainProperty.WardId = 1;
        mainProperty.CategoryId = 1;

        int propNoBase = int.TryParse(mainProperty.PropertyNo, out int basePropNo) ? basePropNo : 100;

        foreach (var prop in combineProperties)
        {
            prop.TaxZoneId = 1;
            prop.WardId = 1;
            // Ensure property numbers are within ±2 range
            if (!int.TryParse(prop.PropertyNo, out _))
            {
                prop.PropertyNo = (propNoBase + 1).ToString();
            }
        }
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
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("SourcePropertyId not found.", result.ErrorMessage);
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

        SetupMatchingPropertyDetails(mainProperty, combineProperties);

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        SetupNonApartmentCategoryMocks(combineProperties);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

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
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

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
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

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

        SetupMatchingPropertyDetails(mainProperty, combineProperties);

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        SetupNonApartmentCategoryMocks(combineProperties);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

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

        SetupMatchingPropertyDetails(mainProperty, combineProperties);

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        SetupNonApartmentCategoryMocks(combineProperties);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

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

        SetupMatchingPropertyDetails(mainProperty, combineProperties);

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        SetupNonApartmentCategoryMocks(combineProperties);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

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
            new() { Id = 4, PropertyNo = "122", OwnerName = "John Doe", IsActive = true }
        };

        SetupMatchingPropertyDetails(mainProperty, combineProperties);

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        SetupNonApartmentCategoryMocks(combineProperties);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

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

        SetupMatchingPropertyDetails(mainProperty, combineProperties);

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        SetupNonApartmentCategoryMocks(combineProperties);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

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

        SetupMatchingPropertyDetails(mainProperty, combineProperties);

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        SetupNonApartmentCategoryMocks(combineProperties);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

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
            new() { Id = 3, PropertyNo = "125", OwnerName = "John Doe", IsActive = false }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        // Inactive properties are filtered out at the query level (IsActive filter),
        // so the count doesn't match and returns "not found" error
        Assert.False(result.IsValid);
        Assert.Equal("One or more CombinedPropertyIds not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_MainPropertyInactive_ReturnsFailure()
    {
        // Arrange
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "123",
            OwnerName = "John Doe",
            IsActive = false,
            MarkedForDeletion = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Property cannot be combined because it is inactive or locked.", result.ErrorMessage);
        Assert.Empty(result.ValidProperties);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_CombinedPropertyMarkedForDeletion_ReturnsFailure()
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
            new() { Id = 2, PropertyNo = "124", OwnerName = "John Doe", IsActive = true, MarkedForDeletion = true }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Property cannot be combined because it is inactive or locked.", result.ErrorMessage);
        Assert.Empty(result.ValidProperties);
    }

    #endregion
}