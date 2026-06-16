using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class TaxZoningServiceTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _propertyRepo = new();
    private readonly Mock<IRepository<WardEntity, int>> _wardRepo = new();
    private readonly Mock<IRepository<TaxZoneEntity, int>> _taxZoneRepo = new();
    private readonly Mock<ILogger<TaxZoningService>> _logger = new();

    private TaxZoningService CreateService()
    {
        return new TaxZoningService(
            _propertyRepo.Object,
            _wardRepo.Object,
            _taxZoneRepo.Object,
            _logger.Object);
    }

    private void SetupRepositories(
        List<PropertyEntity>? properties = null,
        List<WardEntity>? wards = null,
        List<TaxZoneEntity>? taxZones = null)
    {
        properties ??= [];
        wards ??= [];
        taxZones ??= [];

        var propertyQueryable = properties.BuildMock();
        var wardQueryable = wards.BuildMock();
        var taxZoneQueryable = taxZones.BuildMock();

        _propertyRepo.Setup(r => r.GetQueryable()).Returns(propertyQueryable);
        _wardRepo.Setup(r => r.GetQueryable()).Returns(wardQueryable);
        _taxZoneRepo.Setup(r => r.GetQueryable()).Returns(taxZoneQueryable);
    }

    #region GetAllPropertyNo Tests

    [Fact]
    public async Task GetAllPropertyNo_WithNoFilters_ReturnsAllActiveProperties()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, TaxZoneId = 10, PropertyNo = "A1", IsActive = true },
            new() { Id = 2, WardId = 1, TaxZoneId = 10, PropertyNo = "A2", IsActive = true },
            new() { Id = 3, WardId = 2, TaxZoneId = 20, PropertyNo = "B1", IsActive = false }
        };
        var wards = new List<WardEntity>
        {
            new() { Id = 1, WardNo = "W1", ZoneId = 1 },
            new() { Id = 2, WardNo = "W2", ZoneId = 2 }
        };
        var taxZones = new List<TaxZoneEntity>
        {
            new() { Id = 10, TaxZoneNo = "TZ10" },
            new() { Id = 20, TaxZoneNo = "TZ20" }
        };

        SetupRepositories(properties, wards, taxZones);
        var service = CreateService();
        var query = new TaxZoningQueryParameters { IsActive = true };

        // Act
        var result = await service.GetAllPropertyNo(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, item => Assert.True(item.WardId == 1));
    }

    [Fact]
    public async Task GetAllPropertyNo_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var properties = Enumerable.Range(1, 25).Select(i => new PropertyEntity
        {
            Id = i,
            WardId = 1,
            TaxZoneId = 10,
            PropertyNo = $"P{i:D3}",
            IsActive = true
        }).ToList();
        var wards = new List<WardEntity> { new() { Id = 1, WardNo = "W1", ZoneId = 1 } };
        var taxZones = new List<TaxZoneEntity> { new() { Id = 10, TaxZoneNo = "TZ10" } };

        SetupRepositories(properties, wards, taxZones);
        var service = CreateService();
        var query = new TaxZoningQueryParameters
        {
            IsActive = true,
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllPropertyNo(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetAllPropertyNo_WithSortByPropertyNo_ReturnsSortedResults()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, TaxZoneId = 10, PropertyNo = "C1", IsActive = true },
            new() { Id = 2, WardId = 1, TaxZoneId = 10, PropertyNo = "A1", IsActive = true },
            new() { Id = 3, WardId = 1, TaxZoneId = 10, PropertyNo = "B1", IsActive = true }
        };
        var wards = new List<WardEntity> { new() { Id = 1, WardNo = "W1", ZoneId = 1 } };
        var taxZones = new List<TaxZoneEntity> { new() { Id = 10, TaxZoneNo = "TZ10" } };

        SetupRepositories(properties, wards, taxZones);
        var service = CreateService();
        var query = new TaxZoningQueryParameters
        {
            IsActive = true,
            SortBy = "propertyno",
            SortOrder = "ASC"
        };

        // Act
        var result = await service.GetAllPropertyNo(query);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Equal("A1", items[0].PropertyNo);
        Assert.Equal("B1", items[1].PropertyNo);
        Assert.Equal("C1", items[2].PropertyNo);
    }    

    [Fact]
    public async Task GetAllPropertyNo_WithSortByPropertyNo_ReturnsNaturalSortedResults()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, TaxZoneId = 10, PropertyNo = "10", IsActive = true },
            new() { Id = 2, WardId = 1, TaxZoneId = 10, PropertyNo = "2", IsActive = true },
            new() { Id = 3, WardId = 1, TaxZoneId = 10, PropertyNo = "1", IsActive = true },
            new() { Id = 4, WardId = 1, TaxZoneId = 10, PropertyNo = "A10", IsActive = true },
            new() { Id = 5, WardId = 1, TaxZoneId = 10, PropertyNo = "A2", IsActive = true },
            new() { Id = 6, WardId = 1, TaxZoneId = 10, PropertyNo = "A1", IsActive = true }
        };
        var wards = new List<WardEntity> { new() { Id = 1, WardNo = "W1", ZoneId = 1 } };
        var taxZones = new List<TaxZoneEntity> { new() { Id = 10, TaxZoneNo = "TZ10" } };

        SetupRepositories(properties, wards, taxZones);
        var service = CreateService();
        var query = new TaxZoningQueryParameters
        {
            IsActive = true,
            SortBy = "propertyno",
            SortOrder = "ASC"
        };

        // Act
        var result = await service.GetAllPropertyNo(query);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Equal(6, items.Count);
        Assert.Equal("1", items[0].PropertyNo);
        Assert.Equal("2", items[1].PropertyNo);
        Assert.Equal("10", items[2].PropertyNo);
        Assert.Equal("A1", items[3].PropertyNo);
        Assert.Equal("A2", items[4].PropertyNo);
        Assert.Equal("A10", items[5].PropertyNo);
    }

    [Fact]
    public async Task GetAllPropertyNo_WithPageSizeMinusOne_ReturnsAllResults()
    {
        // Arrange
        var properties = Enumerable.Range(1, 50).Select(i => new PropertyEntity
        {
            Id = i,
            WardId = 1,
            TaxZoneId = 10,
            PropertyNo = $"P{i:D3}",
            IsActive = true
        }).ToList();
        var wards = new List<WardEntity> { new() { Id = 1, WardNo = "W1", ZoneId = 1 } };
        var taxZones = new List<TaxZoneEntity> { new() { Id = 10, TaxZoneNo = "TZ10" } };

        SetupRepositories(properties, wards, taxZones);
        var service = CreateService();
        var query = new TaxZoningQueryParameters
        {
            IsActive = true,
            PageSize = -1
        };

        // Act
        var result = await service.GetAllPropertyNo(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50, result.Items.Count());
        Assert.Equal(50, result.TotalCount);
    }

    [Fact]
    public async Task GetAllPropertyNo_ExcludesEmptyPropertyNo()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, TaxZoneId = 10, PropertyNo = "A1", IsActive = true },
            new() { Id = 2, WardId = 1, TaxZoneId = 10, PropertyNo = "", IsActive = true },
            new() { Id = 3, WardId = 1, TaxZoneId = 10, PropertyNo = "   ", IsActive = true },
            new() { Id = 4, WardId = 1, TaxZoneId = 10, PropertyNo = null, IsActive = true }
        };
        var wards = new List<WardEntity> { new() { Id = 1, WardNo = "W1", ZoneId = 1 } };
        var taxZones = new List<TaxZoneEntity> { new() { Id = 10, TaxZoneNo = "TZ10" } };

        SetupRepositories(properties, wards, taxZones);
        var service = CreateService();
        var query = new TaxZoningQueryParameters { IsActive = true };

        // Act
        var result = await service.GetAllPropertyNo(query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("A1", result.Items.First().PropertyNo);
    }

    #endregion

    #region GetFromToPropertyNo Tests

    [Fact]
    public async Task GetFromToPropertyNo_GroupsByWardAndReturnsFromTo()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, TaxZoneId = 10, PropertyNo = "A1", IsActive = true },
            new() { Id = 2, WardId = 1, TaxZoneId = 10, PropertyNo = "A5", IsActive = true },
            new() { Id = 3, WardId = 1, TaxZoneId = 10, PropertyNo = "A3", IsActive = true },
            new() { Id = 4, WardId = 2, TaxZoneId = 20, PropertyNo = "B1", IsActive = true }
        };
        var wards = new List<WardEntity>
        {
            new() { Id = 1, WardNo = "W1", ZoneId = 1 },
            new() { Id = 2, WardNo = "W2", ZoneId = 2 }
        };
        var taxZones = new List<TaxZoneEntity>
        {
            new() { Id = 10, TaxZoneNo = "TZ10" },
            new() { Id = 20, TaxZoneNo = "TZ20" }
        };

        SetupRepositories(properties, wards, taxZones);
        var service = CreateService();
        var query = new TaxZoningQueryParameters { IsActive = true };

        // Act
        var result = await service.GetFromToPropertyNo(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count()); // 2 wards

        var ward1Group = result.Items.First(x => x.WardId == 1);
        Assert.Equal("A1", ward1Group.FromProperty);
        Assert.Equal("A5", ward1Group.ToProperty);

        var ward2Group = result.Items.First(x => x.WardId == 2);
        Assert.Equal("B1", ward2Group.FromProperty);
        Assert.Equal("B1", ward2Group.ToProperty);
    }

    [Fact]
    public async Task GetFromToPropertyNo_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var properties = Enumerable.Range(1, 15).SelectMany(w => new[]
        {
            new PropertyEntity { Id = w * 10, WardId = w, TaxZoneId = 10, PropertyNo = $"P{w}A", IsActive = true },
            new PropertyEntity { Id = w * 10 + 1, WardId = w, TaxZoneId = 10, PropertyNo = $"P{w}B", IsActive = true }
        }).ToList();
        var wards = Enumerable.Range(1, 15).Select(w => new WardEntity { Id = w, WardNo = $"W{w}", ZoneId = 1 }).ToList();
        var taxZones = new List<TaxZoneEntity> { new() { Id = 10, TaxZoneNo = "TZ10" } };

        SetupRepositories(properties, wards, taxZones);
        var service = CreateService();
        var query = new TaxZoningQueryParameters
        {
            IsActive = true,
            PageNumber = 2,
            PageSize = 5
        };

        // Act
        var result = await service.GetFromToPropertyNo(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Items.Count());
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
    }

    [Fact]
    public async Task GetFromToPropertyNo_SortsByWardByDefault()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 3, TaxZoneId = 10, PropertyNo = "C1", IsActive = true },
            new() { Id = 2, WardId = 1, TaxZoneId = 10, PropertyNo = "A1", IsActive = true },
            new() { Id = 3, WardId = 2, TaxZoneId = 10, PropertyNo = "B1", IsActive = true }
        };
        var wards = new List<WardEntity>
        {
            new() { Id = 1, WardNo = "W1", ZoneId = 1 },
            new() { Id = 2, WardNo = "W2", ZoneId = 1 },
            new() { Id = 3, WardNo = "W3", ZoneId = 1 }
        };
        var taxZones = new List<TaxZoneEntity> { new() { Id = 10, TaxZoneNo = "TZ10" } };

        SetupRepositories(properties, wards, taxZones);
        var service = CreateService();
        var query = new TaxZoningQueryParameters { IsActive = true };

        // Act
        var result = await service.GetFromToPropertyNo(query);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Equal(1, items[0].WardId);
        Assert.Equal(2, items[1].WardId);
        Assert.Equal(3, items[2].WardId);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithPropertyNoList_ReturnsExpectedDto()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, TaxZoneId = 10, PropertyNo = "A1", IsActive = true },
            new() { Id = 2, WardId = 1, TaxZoneId = 10, PropertyNo = "A2", IsActive = true },
            new() { Id = 3, WardId = 1, TaxZoneId = 10, PropertyNo = "A3", IsActive = true }
        };

        SetupRepositories(properties);
        var service = CreateService();
        var dto = new UpdateTaxZoningDto
        {
            WardId = 1,
            TaxZoneId = 99,
            PropertyNo = "A1,A2"
        };

        // Act
        var result = await service.UpdateAsync(dto);

        // Assert - MockQueryable.EntityFrameworkCore supports ExecuteUpdateAsync
        // The result should contain the expected DTO structure
        Assert.NotNull(result);
        Assert.Equal(1, result.WardId);
        Assert.Equal("A1", result.PropertyNo);  // First property in the list
        Assert.Equal(99, result.TaxZoneId);
    }

    [Fact]
    public async Task UpdateAsync_WithRangeFromTo_RequiresBothValues()
    {
        // Arrange
        SetupRepositories();
        var service = CreateService();
        var dto = new UpdateTaxZoningDto
        {
            WardId = 1,
            TaxZoneId = 99,
            FromProperty = "A1",
            ToProperty = null
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_WithFromToContainingCommas_ThrowsArgumentException()
    {
        // Arrange
        SetupRepositories();
        var service = CreateService();
        var dto = new UpdateTaxZoningDto
        {
            WardId = 1,
            TaxZoneId = 99,
            FromProperty = "A1,A2",
            ToProperty = "A5"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(dto));
        Assert.Contains("no commas", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_WithOnlyWardId_ReturnsExpectedDto()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, TaxZoneId = 10, PropertyNo = "A1", IsActive = true },
            new() { Id = 2, WardId = 1, TaxZoneId = 10, PropertyNo = "A2", IsActive = true }
        };

        SetupRepositories(properties);
        var service = CreateService();
        var dto = new UpdateTaxZoningDto
        {
            WardId = 1,
            TaxZoneId = 99
        };

        // Act
        var result = await service.UpdateAsync(dto);

        // Assert - MockQueryable.EntityFrameworkCore supports ExecuteUpdateAsync
        Assert.NotNull(result);
        Assert.Equal(1, result.WardId);
        Assert.Equal(99, result.TaxZoneId);
    }

    [Fact]
    public async Task UpdateAsync_LogsStartOfOperation()
    {
        // Arrange
        SetupRepositories();
        var service = CreateService();
        var dto = new UpdateTaxZoningDto
        {
            WardId = 1,
            TaxZoneId = 99
        };

        // Act
        await service.UpdateAsync(dto);

        // Assert - Verify logging was called
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("UpdateAsync started")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetAllPropertyNo_WithNoMatchingRecords_ReturnsEmptyResult()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, TaxZoneId = 10, PropertyNo = "A1", IsActive = false }
        };
        var wards = new List<WardEntity> { new() { Id = 1, WardNo = "W1", ZoneId = 1 } };
        var taxZones = new List<TaxZoneEntity> { new() { Id = 10, TaxZoneNo = "TZ10" } };

        SetupRepositories(properties, wards, taxZones);
        var service = CreateService();
        var query = new TaxZoningQueryParameters { IsActive = true };

        // Act
        var result = await service.GetAllPropertyNo(query);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetAllPropertyNo_WithMissingWardLookup_HandlesGracefully()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 999, TaxZoneId = 10, PropertyNo = "A1", IsActive = true }
        };
        var wards = new List<WardEntity>(); // No ward with Id = 999
        var taxZones = new List<TaxZoneEntity> { new() { Id = 10, TaxZoneNo = "TZ10" } };

        SetupRepositories(properties, wards, taxZones);
        var service = CreateService();
        var query = new TaxZoningQueryParameters { IsActive = true };

        // Act
        var result = await service.GetAllPropertyNo(query);

        // Assert - With INNER JOIN, missing ward means no results returned
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetAllPropertyNo_WithMissingTaxZoneLookup_HandlesGracefully()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, TaxZoneId = 999, PropertyNo = "A1", IsActive = true }
        };
        var wards = new List<WardEntity> { new() { Id = 1, WardNo = "W1", ZoneId = 1 } };
        var taxZones = new List<TaxZoneEntity>(); // No tax zone with Id = 999

        SetupRepositories(properties, wards, taxZones);
        var service = CreateService();
        var query = new TaxZoningQueryParameters { IsActive = true };

        // Act
        var result = await service.GetAllPropertyNo(query);

        // Assert - With INNER JOIN, missing tax zone means no results returned
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetFromToPropertyNo_WithEmptyPropertyNos_HandlesGracefully()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, TaxZoneId = 10, PropertyNo = "", IsActive = true },
            new() { Id = 2, WardId = 1, TaxZoneId = 10, PropertyNo = "   ", IsActive = true }
        };
        var wards = new List<WardEntity> { new() { Id = 1, WardNo = "W1", ZoneId = 1 } };
        var taxZones = new List<TaxZoneEntity> { new() { Id = 10, TaxZoneNo = "TZ10" } };

        SetupRepositories(properties, wards, taxZones);
        var service = CreateService();
        var query = new TaxZoningQueryParameters { IsActive = true };

        // Act
        var result = await service.GetFromToPropertyNo(query);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllPropertyNo_DescendingSort_ReturnsSortedDescending()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, TaxZoneId = 10, PropertyNo = "A1", IsActive = true },
            new() { Id = 2, WardId = 1, TaxZoneId = 10, PropertyNo = "B1", IsActive = true },
            new() { Id = 3, WardId = 1, TaxZoneId = 10, PropertyNo = "C1", IsActive = true }
        };
        var wards = new List<WardEntity> { new() { Id = 1, WardNo = "W1", ZoneId = 1 } };
        var taxZones = new List<TaxZoneEntity> { new() { Id = 10, TaxZoneNo = "TZ10" } };

        SetupRepositories(properties, wards, taxZones);
        var service = CreateService();
        var query = new TaxZoningQueryParameters
        {
            IsActive = true,
            SortBy = "propertyno",
            SortOrder = "DESC"
        };

        // Act
        var result = await service.GetAllPropertyNo(query);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Equal("C1", items[0].PropertyNo);
        Assert.Equal("B1", items[1].PropertyNo);
        Assert.Equal("A1", items[2].PropertyNo);
    }

    #endregion
}
