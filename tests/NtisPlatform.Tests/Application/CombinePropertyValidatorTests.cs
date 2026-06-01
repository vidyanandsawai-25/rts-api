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

    #region Multi-Unit Apartment Validation Tests

    /// <summary>
    /// Helper method to setup apartment category mocks.
    /// </summary>
    private void SetupApartmentCategoryMocks(PropertyEntity mainProperty, List<PropertyEntity> combineProperties)
    {
        // Ensure main property has category
        if (!mainProperty.CategoryId.HasValue)
        {
            mainProperty.CategoryId = 1;
        }

        // Assign category IDs to properties that don't have one
        foreach (var prop in combineProperties)
        {
            if (!prop.CategoryId.HasValue)
            {
                prop.CategoryId = mainProperty.CategoryId;
            }
        }

        // Setup apartment category
        var categories = new List<PropertyCategoryEntity>
        {
            new() { Id = mainProperty.CategoryId.Value, PropertyCategoryName = "Apartment" }
        };

        // Add categories for combined properties if different
        foreach (var prop in combineProperties.Where(p => p.CategoryId.HasValue && p.CategoryId != mainProperty.CategoryId))
        {
            categories.Add(new PropertyCategoryEntity { Id = prop.CategoryId!.Value, PropertyCategoryName = "Apartment" });
        }

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(mainProperty.CategoryId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories.First());

        _mockCategoryRepository.Setup(r => r.GetQueryable())
            .Returns(categories.BuildMock());
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_MultiUnitApartment_WithSameSocietyDetailId_ReturnsSuccess()
    {
        // Arrange - Multi-unit apartment scenario with wings (multiple properties with same PropertyNo and partitions)
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2, 3 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "100",
            PartitionNo = "A1",
            OwnerName = "John Doe",
            IsActive = true,
            TaxZoneId = 1,
            WardId = 10,
            CategoryId = 1,
            SocietyDetailId = 5 // Same wing
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "100", PartitionNo = "A2", OwnerName = "John Doe", IsActive = true, TaxZoneId = 1, WardId = 10, CategoryId = 1, SocietyDetailId = 5 },
            new() { Id = 3, PropertyNo = "100", PartitionNo = "A3", OwnerName = "John Doe", IsActive = true, TaxZoneId = 1, WardId = 10, CategoryId = 1, SocietyDetailId = 5 }
        };

        // All properties in the building (to simulate multi-unit detection)
        var allBuildingProperties = new List<PropertyEntity> { mainProperty };
        allBuildingProperties.AddRange(combineProperties);

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(allBuildingProperties.BuildMock());

        SetupApartmentCategoryMocks(mainProperty, combineProperties);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.ValidProperties.Count);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_MultiUnitApartment_DifferentSocietyDetailId_ReturnsFailure()
    {
        // Arrange - Multi-unit apartment with different wings (different SocietyDetailId)
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "100",
            PartitionNo = "A1",
            OwnerName = "John Doe",
            IsActive = true,
            TaxZoneId = 1,
            WardId = 10,
            CategoryId = 1,
            SocietyDetailId = 5 // Wing A
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "100", PartitionNo = "B1", OwnerName = "John Doe", IsActive = true, TaxZoneId = 1, WardId = 10, CategoryId = 1, SocietyDetailId = 6 } // Wing B - different
        };

        // All properties in the building (multiple properties = multi-unit detection)
        var allBuildingProperties = new List<PropertyEntity> { mainProperty };
        allBuildingProperties.AddRange(combineProperties);

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(allBuildingProperties.BuildMock());

        SetupApartmentCategoryMocks(mainProperty, combineProperties);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("All properties must be from the same Wing.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_MultiUnitApartment_NoSocietyDetailIdOnMain_ReturnsFailure()
    {
        // Arrange - Multi-unit apartment where source property has no SocietyDetailId
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "100",
            PartitionNo = "A1",
            OwnerName = "John Doe",
            IsActive = true,
            TaxZoneId = 1,
            WardId = 10,
            CategoryId = 1,
            SocietyDetailId = null // No society detail - should fail
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "100", PartitionNo = "A2", OwnerName = "John Doe", IsActive = true, TaxZoneId = 1, WardId = 10, CategoryId = 1, SocietyDetailId = 5 }
        };

        // All properties in the building (multiple properties = multi-unit detection)
        var allBuildingProperties = new List<PropertyEntity> { mainProperty };
        allBuildingProperties.AddRange(combineProperties);

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(allBuildingProperties.BuildMock());

        SetupApartmentCategoryMocks(mainProperty, combineProperties);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Source property's society details not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_MultiUnitApartment_DifferentPropertyNo_ReturnsFailure()
    {
        // Arrange - Multi-unit apartment with different PropertyNo
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "100",
            PartitionNo = "A1",
            OwnerName = "John Doe",
            IsActive = true,
            TaxZoneId = 1,
            WardId = 10,
            CategoryId = 1,
            SocietyDetailId = 5
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "101", PartitionNo = "A2", OwnerName = "John Doe", IsActive = true, TaxZoneId = 1, WardId = 10, CategoryId = 1, SocietyDetailId = 5 } // Different PropertyNo
        };

        // Additional property to trigger multi-unit detection
        var additionalProperty = new PropertyEntity { Id = 3, PropertyNo = "100", PartitionNo = "A3", IsActive = true, TaxZoneId = 1, WardId = 10, CategoryId = 1, SocietyDetailId = 5 };

        var allBuildingProperties = new List<PropertyEntity> { mainProperty, additionalProperty };
        allBuildingProperties.AddRange(combineProperties);

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(allBuildingProperties.BuildMock());

        SetupApartmentCategoryMocks(mainProperty, combineProperties);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("All properties must be from the same Zone, Ward, and PropertyNo.", result.ErrorMessage);
    }

    #endregion

    #region Standalone Apartment Validation Tests

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_StandaloneApartment_SameZoneAndWard_ReturnsSuccess()
    {
        // Arrange - Standalone apartment scenario (single property, no other partitions exist)
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "100",
            PartitionNo = null, // No partition - standalone
            OwnerName = "John Doe",
            IsActive = true,
            TaxZoneId = 1,
            WardId = 10,
            CategoryId = 1,
            SocietyDetailId = null // No society detail - standalone
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "101", PartitionNo = null, OwnerName = "John Doe", IsActive = true, TaxZoneId = 1, WardId = 10, CategoryId = 1, SocietyDetailId = null }
        };

        // Only these two properties exist - no multi-unit scenario
        var allProperties = new List<PropertyEntity> { mainProperty };
        allProperties.AddRange(combineProperties);

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(allProperties.BuildMock());

        SetupApartmentCategoryMocks(mainProperty, combineProperties);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Single(result.ValidProperties);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_StandaloneApartment_DifferentWard_ReturnsFailure()
    {
        // Arrange - Standalone apartment with different ward
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "100",
            PartitionNo = null,
            OwnerName = "John Doe",
            IsActive = true,
            TaxZoneId = 1,
            WardId = 10, // Ward 10
            CategoryId = 1,
            SocietyDetailId = null
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "101", PartitionNo = null, OwnerName = "John Doe", IsActive = true, TaxZoneId = 1, WardId = 20, CategoryId = 1, SocietyDetailId = null } // Ward 20 - different
        };

        var allProperties = new List<PropertyEntity> { mainProperty };
        allProperties.AddRange(combineProperties);

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(allProperties.BuildMock());

        SetupApartmentCategoryMocks(mainProperty, combineProperties);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("All properties must be from the same Zone and Ward.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_StandaloneApartment_DifferentZone_ReturnsFailure()
    {
        // Arrange - Standalone apartment with different zone
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "100",
            PartitionNo = null,
            OwnerName = "John Doe",
            IsActive = true,
            TaxZoneId = 1, // Zone 1
            WardId = 10,
            CategoryId = 1,
            SocietyDetailId = null
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "101", PartitionNo = null, OwnerName = "John Doe", IsActive = true, TaxZoneId = 2, WardId = 10, CategoryId = 1, SocietyDetailId = null } // Zone 2 - different
        };

        var allProperties = new List<PropertyEntity> { mainProperty };
        allProperties.AddRange(combineProperties);

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(allProperties.BuildMock());

        SetupApartmentCategoryMocks(mainProperty, combineProperties);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("All properties must be from the same Zone and Ward.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_StandaloneApartment_DifferentPropertyNo_ReturnsSuccess()
    {
        // Arrange - Standalone apartments can have different PropertyNo (only Zone+Ward validation)
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            PropertyNo = "100",
            PartitionNo = null,
            OwnerName = "John Doe",
            IsActive = true,
            TaxZoneId = 1,
            WardId = 10,
            CategoryId = 1,
            SocietyDetailId = null
        };

        var combineProperties = new List<PropertyEntity>
        {
            new() { Id = 2, PropertyNo = "200", PartitionNo = null, OwnerName = "John Doe", IsActive = true, TaxZoneId = 1, WardId = 10, CategoryId = 1, SocietyDetailId = null } // Different PropertyNo - allowed for standalone
        };

        var allProperties = new List<PropertyEntity> { mainProperty };
        allProperties.AddRange(combineProperties);

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(allProperties.BuildMock());

        SetupApartmentCategoryMocks(mainProperty, combineProperties);

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Single(result.ValidProperties);
    }

    #endregion
}