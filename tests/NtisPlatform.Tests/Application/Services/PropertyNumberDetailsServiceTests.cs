using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class PropertyNumberDetailsServiceTests
{
    private readonly Mock<ILogger<PropertyNumberDetailsService>> _loggerMock;
    private readonly Mock<IRepository<PropertyEntity, int>> _propertyRepositoryMock;
    private readonly Mock<IRepository<PropertyCategoryEntity, int>> _categoryRepositoryMock;
    private readonly Mock<IRepository<WardEntity, int>> _wardRepositoryMock;

    private readonly PropertyNumberDetailsService _service;

    public PropertyNumberDetailsServiceTests()
    {
        _loggerMock = new Mock<ILogger<PropertyNumberDetailsService>>();
        _propertyRepositoryMock = new Mock<IRepository<PropertyEntity, int>>();
        _categoryRepositoryMock = new Mock<IRepository<PropertyCategoryEntity, int>>();
        _wardRepositoryMock = new Mock<IRepository<WardEntity, int>>();

        _service = new PropertyNumberDetailsService(
            _loggerMock.Object,
            _propertyRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _wardRepositoryMock.Object
        );
    }

    private void SetupMocks(
        List<PropertyEntity> properties,
        List<PropertyCategoryEntity> categories,
        List<WardEntity> wards)
    {
        _propertyRepositoryMock.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _categoryRepositoryMock.Setup(r => r.GetQueryable()).Returns(categories.BuildMock());
        _wardRepositoryMock.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());
    }

    [Fact]
    public async Task GetAsync_PropertyNotFound_ReturnsNull()
    {
        // Arrange
        SetupMocks(new List<PropertyEntity>(), new List<PropertyCategoryEntity>(), new List<WardEntity>());

        // Act
        var result = await _service.GetAsync(1, "Current");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_CurrentFilter_ReturnsCurrentProperty()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 1, WardId = 1, CategoryId = 1, PropertyNo = "10", IsActive = true, MarkedForDeletion = false }
        };
        var categories = new List<PropertyCategoryEntity>
        {
            new PropertyCategoryEntity { Id = 1, PropertyCategoryName = "Individual", IsActive = true }
        };
        var wards = new List<WardEntity>
        {
            new WardEntity { Id = 1, WardNo = "W1", IsActive = true }
        };

        SetupMocks(properties, categories, wards);

        // Act
        var result = await _service.GetAsync(1, "Current");

        // Assert
        result.Should().NotBeNull();
        result!.PropertyNo.Should().Be("10");
        result.WardNo.Should().Be("W1");
        result.Category.Should().Be("Individual");
        result.PropertyType.Should().Be("Current");
    }

    [Fact]
    public async Task GetAsync_NextFilter_NormalProperty_ReturnsNextMainProperty()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 1, WardId = 1, CategoryId = 1, PropertyNo = "10", IsActive = true, MarkedForDeletion = false, PartitionNo = "" },
            new PropertyEntity { Id = 2, WardId = 1, CategoryId = 1, PropertyNo = "11", IsActive = true, MarkedForDeletion = false, PartitionNo = "" },
            new PropertyEntity { Id = 3, WardId = 1, CategoryId = 1, PropertyNo = "12", IsActive = true, MarkedForDeletion = false, PartitionNo = "" }
        };
        var categories = new List<PropertyCategoryEntity>
        {
            new PropertyCategoryEntity { Id = 1, PropertyCategoryName = "Individual", IsActive = true }
        };
        var wards = new List<WardEntity>
        {
            new WardEntity { Id = 1, WardNo = "W1", IsActive = true }
        };

        SetupMocks(properties, categories, wards);

        // Act
        var result = await _service.GetAsync(2, "Next");

        // Assert
        result.Should().NotBeNull();
        result!.PropertyNo.Should().Be("12");
        result.PropertyType.Should().Be("Next");
    }

    [Fact]
    public async Task GetAsync_PreviousFilter_NormalProperty_ReturnsPreviousMainProperty()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 1, WardId = 1, CategoryId = 1, PropertyNo = "10", IsActive = true, MarkedForDeletion = false, PartitionNo = "" },
            new PropertyEntity { Id = 2, WardId = 1, CategoryId = 1, PropertyNo = "11", IsActive = true, MarkedForDeletion = false, PartitionNo = "" },
            new PropertyEntity { Id = 3, WardId = 1, CategoryId = 1, PropertyNo = "12", IsActive = true, MarkedForDeletion = false, PartitionNo = "" }
        };
        var categories = new List<PropertyCategoryEntity>
        {
            new PropertyCategoryEntity { Id = 1, PropertyCategoryName = "Individual", IsActive = true }
        };
        var wards = new List<WardEntity>
        {
            new WardEntity { Id = 1, WardNo = "W1", IsActive = true }
        };

        SetupMocks(properties, categories, wards);

        // Act
        var result = await _service.GetAsync(2, "Previous");

        // Assert
        result.Should().NotBeNull();
        result!.PropertyNo.Should().Be("10");
        result.PropertyType.Should().Be("Previous");
    }

    [Fact]
    public async Task GetAsync_NextFilter_ApartmentMain_ReturnsFirstFlat()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 1, WardId = 1, CategoryId = 2, PropertyNo = "10", FlatOrShopNo = "", IsActive = true, MarkedForDeletion = false, PartitionNo = "" },
            new PropertyEntity { Id = 2, WardId = 1, CategoryId = 2, PropertyNo = "10", FlatOrShopNo = "1", IsActive = true, MarkedForDeletion = false, PartitionNo = "" },
            new PropertyEntity { Id = 3, WardId = 1, CategoryId = 2, PropertyNo = "10", FlatOrShopNo = "2", IsActive = true, MarkedForDeletion = false, PartitionNo = "" }
        };
        var categories = new List<PropertyCategoryEntity>
        {
            new PropertyCategoryEntity { Id = 2, PropertyCategoryName = "Apartment", IsActive = true }
        };
        var wards = new List<WardEntity>
        {
            new WardEntity { Id = 1, WardNo = "W1", IsActive = true }
        };

        SetupMocks(properties, categories, wards);

        // Act
        var result = await _service.GetAsync(1, "Next");

        // Assert
        result.Should().NotBeNull();
        result!.PropertyNo.Should().Be("10");
        result.FlatOrShopNo.Should().Be("1");
        result.PropertyType.Should().Be("Next");
    }

    [Fact]
    public async Task GetAsync_NextFilter_ApartmentFlat_ReturnsNextFlat()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 1, WardId = 1, CategoryId = 2, PropertyNo = "10", FlatOrShopNo = "", IsActive = true, MarkedForDeletion = false, PartitionNo = "" },
            new PropertyEntity { Id = 2, WardId = 1, CategoryId = 2, PropertyNo = "10", FlatOrShopNo = "1", IsActive = true, MarkedForDeletion = false, PartitionNo = "" },
            new PropertyEntity { Id = 3, WardId = 1, CategoryId = 2, PropertyNo = "10", FlatOrShopNo = "2", IsActive = true, MarkedForDeletion = false, PartitionNo = "" }
        };
        var categories = new List<PropertyCategoryEntity>
        {
            new PropertyCategoryEntity { Id = 2, PropertyCategoryName = "Apartment", IsActive = true }
        };
        var wards = new List<WardEntity>
        {
            new WardEntity { Id = 1, WardNo = "W1", IsActive = true }
        };

        SetupMocks(properties, categories, wards);

        // Act
        var result = await _service.GetAsync(2, "Next");

        // Assert
        result.Should().NotBeNull();
        result!.PropertyNo.Should().Be("10");
        result.FlatOrShopNo.Should().Be("2");
        result.PropertyType.Should().Be("Next");
    }

    [Fact]
    public async Task GetAsync_PreviousFilter_ApartmentFlat_ReturnsPreviousFlat()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 1, WardId = 1, CategoryId = 2, PropertyNo = "10", FlatOrShopNo = "", IsActive = true, MarkedForDeletion = false, PartitionNo = "" },
            new PropertyEntity { Id = 2, WardId = 1, CategoryId = 2, PropertyNo = "10", FlatOrShopNo = "1", IsActive = true, MarkedForDeletion = false, PartitionNo = "" },
            new PropertyEntity { Id = 3, WardId = 1, CategoryId = 2, PropertyNo = "10", FlatOrShopNo = "2", IsActive = true, MarkedForDeletion = false, PartitionNo = "" }
        };
        var categories = new List<PropertyCategoryEntity>
        {
            new PropertyCategoryEntity { Id = 2, PropertyCategoryName = "Apartment", IsActive = true }
        };
        var wards = new List<WardEntity>
        {
            new WardEntity { Id = 1, WardNo = "W1", IsActive = true }
        };

        SetupMocks(properties, categories, wards);

        // Act
        var result = await _service.GetAsync(3, "Previous");

        // Assert
        result.Should().NotBeNull();
        result!.PropertyNo.Should().Be("10");
        result.FlatOrShopNo.Should().Be("1");
        result.PropertyType.Should().Be("Previous");
    }
}
