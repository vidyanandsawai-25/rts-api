using MockQueryable;
using Moq;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;
using Microsoft.Extensions.Logging;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Test cases for Non-Apartment properties with partitions (numeric and alphabetic)
/// </summary>
public class CombinePropertyValidatorPartitionTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IRepository<PropertyCategoryEntity, int>> _mockCategoryRepository;
    private readonly Mock<ILogger<CombinePropertyValidator>> _mockLogger;
    private readonly CombinePropertyValidator _validator;

    public CombinePropertyValidatorPartitionTests()
    {
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockCategoryRepository = new Mock<IRepository<PropertyCategoryEntity, int>>();
        _mockLogger = new Mock<ILogger<CombinePropertyValidator>>();
        _validator = new CombinePropertyValidator(_mockRepository.Object, _mockCategoryRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_NonApartmentWithNumericPartitions_SamePropertyNo_ReturnsSuccess()
    {
        // Arrange - Non-apartment properties with same PropertyNo and PURELY numeric partitions (1, 2, 3)
        // This tests the fix for numeric-only partitions where each digit was incorrectly treated as a prefix
        var mainPropertyId = 550726;
        var combinePropertyIds = new List<int> { 550727, 550728 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            CategoryId = 2, // Individual (Non-apartment)
            TaxZoneId = 1,
            WardId = 79,
            PropertyNo = "85",
            PartitionNo = "1", // Purely numeric
            OwnerName = "John Doe",
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new()
            {
                Id = 550727,
                CategoryId = 2,
                TaxZoneId = 1,
                WardId = 79,
                PropertyNo = "85",
                PartitionNo = "2", // Purely numeric
                OwnerName = "John Doe",
                IsActive = true
            },
            new()
            {
                Id = 550728,
                CategoryId = 2,
                TaxZoneId = 1,
                WardId = 79,
                PropertyNo = "85",
                PartitionNo = "3", // Purely numeric
                OwnerName = "John Doe",
                IsActive = true
            }
        };

        var nonApartmentCategory = new PropertyCategoryEntity
        {
            Id = 2,
            PropertyCategoryName = "Individual",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nonApartmentCategory);

        _mockCategoryRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyCategoryEntity> { nonApartmentCategory }.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.True(result.IsValid, $"Expected validation to succeed but got error: {result.ErrorMessage}");
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.ValidProperties.Count);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_NonApartmentWithAlphabeticPartitions_SamePropertyNo_ReturnsSuccess()
    {
        // Arrange - Non-apartment properties with same PropertyNo and alphabetic partitions (A1, A2, A3)
        var mainPropertyId = 552371;
        var combinePropertyIds = new List<int> { 552372, 552373 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            CategoryId = 5,
            TaxZoneId = 1,
            WardId = 79,
            PropertyNo = "85",
            PartitionNo = "A1",
            OwnerName = "Jane Smith",
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new()
            {
                Id = 552372,
                CategoryId = 5,
                TaxZoneId = 1,
                WardId = 79,
                PropertyNo = "85",
                PartitionNo = "A2",
                OwnerName = "Jane Smith",
                IsActive = true
            },
            new()
            {
                Id = 552373,
                CategoryId = 5,
                TaxZoneId = 1,
                WardId = 79,
                PropertyNo = "85",
                PartitionNo = "A3",
                OwnerName = "Jane Smith",
                IsActive = true
            }
        };

        var nonApartmentCategory = new PropertyCategoryEntity
        {
            Id = 5,
            PropertyCategoryName = "Residential",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nonApartmentCategory);

        _mockCategoryRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyCategoryEntity> { nonApartmentCategory }.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.ValidProperties.Count);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_NonApartmentMixedPrefixes_SamePropertyNo_ReturnsSuccess()
    {
        // Arrange - Non-apartment properties with same PropertyNo and MIXED partition prefixes (A1, B2, C3)
        // This should now SUCCEED since we removed all partition format validation for non-apartments
        var mainPropertyId = 552371;
        var combinePropertyIds = new List<int> { 552372, 552373 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            CategoryId = 5,
            TaxZoneId = 1,
            WardId = 79,
            PropertyNo = "85",
            PartitionNo = "A1",
            OwnerName = "John Doe",
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new()
            {
                Id = 552372,
                CategoryId = 5,
                TaxZoneId = 1,
                WardId = 79,
                PropertyNo = "85",
                PartitionNo = "B2", // Different prefix - now allowed!
                OwnerName = "John Doe",
                IsActive = true
            },
            new()
            {
                Id = 552373,
                CategoryId = 5,
                TaxZoneId = 1,
                WardId = 79,
                PropertyNo = "85",
                PartitionNo = "C3", // Another different prefix - now allowed!
                OwnerName = "John Doe",
                IsActive = true
            }
        };

        var nonApartmentCategory = new PropertyCategoryEntity
        {
            Id = 5,
            PropertyCategoryName = "Residential",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nonApartmentCategory);

        _mockCategoryRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyCategoryEntity> { nonApartmentCategory }.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert - Should now SUCCEED with no partition format restrictions
        Assert.True(result.IsValid, $"Expected validation to succeed but got error: {result.ErrorMessage}");
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.ValidProperties.Count);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_NonApartmentWithAndWithoutPartitions_SamePropertyNo_ReturnsSuccess()
    {
        // Arrange - Non-apartment: Mix of empty partition and partitions with same PropertyNo
        var mainPropertyId = 552371;
        var combinePropertyIds = new List<int> { 552372, 552373 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            CategoryId = 5,
            TaxZoneId = 1,
            WardId = 79,
            PropertyNo = "85",
            PartitionNo = "", // Empty partition
            OwnerName = "John Doe",
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new()
            {
                Id = 552372,
                CategoryId = 5,
                TaxZoneId = 1,
                WardId = 79,
                PropertyNo = "85",
                PartitionNo = "1",
                OwnerName = "John Doe",
                IsActive = true
            },
            new()
            {
                Id = 552373,
                CategoryId = 5,
                TaxZoneId = 1,
                WardId = 79,
                PropertyNo = "85",
                PartitionNo = "2",
                OwnerName = "John Doe",
                IsActive = true
            }
        };

        var nonApartmentCategory = new PropertyCategoryEntity
        {
            Id = 5,
            PropertyCategoryName = "Residential",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nonApartmentCategory);

        _mockCategoryRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyCategoryEntity> { nonApartmentCategory }.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.ValidProperties.Count);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_NonApartmentDifferentPropertyNoWithinRange_ReturnsSuccess()
    {
        // Arrange - Non-apartment properties with different PropertyNo but within ±2 range (existing behavior)
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            CategoryId = 5,
            TaxZoneId = 1,
            WardId = 79,
            PropertyNo = "85",
            PartitionNo = "",
            OwnerName = "John Doe",
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new()
            {
                Id = 2,
                CategoryId = 5,
                TaxZoneId = 1,
                WardId = 79,
                PropertyNo = "86", // Within ±2 range
                PartitionNo = "",
                OwnerName = "John Doe",
                IsActive = true
            }
        };

        var nonApartmentCategory = new PropertyCategoryEntity
        {
            Id = 5,
            PropertyCategoryName = "Residential",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nonApartmentCategory);

        _mockCategoryRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyCategoryEntity> { nonApartmentCategory }.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_MixedNumericAndAlphanumeric_SamePropertyNo_ReturnsSuccess()
    {
        // Arrange - Non-apartment properties with mixed numeric (1) and alphanumeric (A2) partitions
        // This should now SUCCEED since we removed all partition format validation for non-apartments
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            CategoryId = 5,
            TaxZoneId = 1,
            WardId = 79,
            PropertyNo = "85",
            PartitionNo = "1", // Numeric
            OwnerName = "John Doe",
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new()
            {
                Id = 2,
                CategoryId = 5,
                TaxZoneId = 1,
                WardId = 79,
                PropertyNo = "85",
                PartitionNo = "A2", // Alphanumeric - mixed with numeric is now allowed!
                OwnerName = "John Doe",
                IsActive = true
            }
        };

        var nonApartmentCategory = new PropertyCategoryEntity
        {
            Id = 5,
            PropertyCategoryName = "Residential",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nonApartmentCategory);

        _mockCategoryRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyCategoryEntity> { nonApartmentCategory }.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert - Should now SUCCEED with no partition format restrictions
        Assert.True(result.IsValid, $"Expected validation to succeed but got error: {result.ErrorMessage}");
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidatePropertiesForCombinationAsync_LargeNumericPartitions_SamePropertyNo_ReturnsSuccess()
    {
        // Arrange - Non-apartment properties with multi-digit numeric partitions (10, 20, 30)
        var mainPropertyId = 1;
        var combinePropertyIds = new List<int> { 2, 3 };

        var mainProperty = new PropertyEntity
        {
            Id = mainPropertyId,
            CategoryId = 5,
            TaxZoneId = 1,
            WardId = 79,
            PropertyNo = "85",
            PartitionNo = "10", // Multi-digit numeric
            OwnerName = "John Doe",
            IsActive = true
        };

        var combineProperties = new List<PropertyEntity>
        {
            new()
            {
                Id = 2,
                CategoryId = 5,
                TaxZoneId = 1,
                WardId = 79,
                PropertyNo = "85",
                PartitionNo = "20",
                OwnerName = "John Doe",
                IsActive = true
            },
            new()
            {
                Id = 3,
                CategoryId = 5,
                TaxZoneId = 1,
                WardId = 79,
                PropertyNo = "85",
                PartitionNo = "30",
                OwnerName = "John Doe",
                IsActive = true
            }
        };

        var nonApartmentCategory = new PropertyCategoryEntity
        {
            Id = 5,
            PropertyCategoryName = "Residential",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(combineProperties.BuildMock());

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nonApartmentCategory);

        _mockCategoryRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyCategoryEntity> { nonApartmentCategory }.BuildMock());

        // Act
        var result = await _validator.ValidatePropertiesForCombinationAsync(mainPropertyId, combinePropertyIds, false, default);

        // Assert
        Assert.True(result.IsValid, $"Expected validation to succeed for multi-digit numeric partitions but got error: {result.ErrorMessage}");
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.ValidProperties.Count);
    }
}
