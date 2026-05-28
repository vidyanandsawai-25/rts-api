using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using NtisPlatform.Tests.Helpers;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

/// <summary>
/// Comprehensive integration tests for PropertyRepository.GetFloorDetailsOldPagedAsync
/// Tests pagination, filtering, searching, sorting, and edge cases
/// </summary>
public class PropertyRepositoryGetFloorDetailsOldPagedTests
{
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private async Task<ApplicationDbContext> CreateContextWithTestData()
    {
        var context = CreateContext();

        // Create master data
        var floor1 = EntityTestHelpers.CreateFloorEntity(id: 1);
        floor1.FloorCode = "F1";
        floor1.Description = "Ground Floor";

        var floor2 = EntityTestHelpers.CreateFloorEntity(id: 2);
        floor2.FloorCode = "F2";
        floor2.Description = "First Floor";

        var subFloor1 = EntityTestHelpers.CreateSubFloorEntity(id: 1);
        subFloor1.SubFloorCode = "SF1";
        subFloor1.Description = "Sub Floor A";

        var subFloor2 = EntityTestHelpers.CreateSubFloorEntity(id: 2);
        subFloor2.SubFloorCode = "SF2";
        subFloor2.Description = "Sub Floor B";

        var constructionType1 = EntityTestHelpers.CreateConstructionTypeEntity(id: 1);
        constructionType1.ConstructionCode = "CT1";
        constructionType1.Description = "Concrete";

        var constructionType2 = EntityTestHelpers.CreateConstructionTypeEntity(id: 2);
        constructionType2.ConstructionCode = "CT2";
        constructionType2.Description = "Steel";

        var typeOfUse1 = EntityTestHelpers.CreateTypeOfUseEntity(id: 1);
        typeOfUse1.TypeOfUseCode = "TOU1";
        typeOfUse1.Description = "Residential";

        var typeOfUse2 = EntityTestHelpers.CreateTypeOfUseEntity(id: 2);
        typeOfUse2.TypeOfUseCode = "TOU2";
        typeOfUse2.Description = "Commercial";

        var subTypeOfUse1 = EntityTestHelpers.CreateSubTypeOfUseEntity(id: 1);
        subTypeOfUse1.Description = "Apartment";
        subTypeOfUse1.TypeOfUseId = 1;

        var subTypeOfUse2 = EntityTestHelpers.CreateSubTypeOfUseEntity(id: 2);
        subTypeOfUse2.Description = "Office";
        subTypeOfUse2.TypeOfUseId = 2;

        context.FloorEntity.AddRange(floor1, floor2);
        context.SubFloorEntity.AddRange(subFloor1, subFloor2);
        context.ConstructionTypeEntity.AddRange(constructionType1, constructionType2);
        context.TypeOfUse.AddRange(typeOfUse1, typeOfUse2);
        context.SubTypeOfUse.AddRange(subTypeOfUse1, subTypeOfUse2);

        // Create property with PropertyMastOldId
        var property = new PropertyEntity
        {
            Id = 1,
            PropertyNo = "PROP001",
            WardId = 1,
            TaxZoneId = 1,
            PropertyMastOldId = 100,
            IsActive = true
        };
        context.PropertyMast.Add(property);

        // Create multiple property details old for pagination testing
        var details = new[]
        {
            new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyMastOldId = 100,
                OldFloorId = 1,
                OldSubFloorId = 1,
                OldConstructionYear = "2020",
                OldAssessmentYear = "2021",
                OldConstructionTypeId = 1,
                OldTypeOfUseId = 1,
                OldSubTypeOfUseId = 1,
                OldCarpetAreaSqMeter = 100.50,
                OldCarpetAreaSqFeet = 1081.60,
                OldBuiltupAreaSqMeter = 120.00,
                OldBuiltupAreaSqFeet = 1291.67,
                IsActive = true,
                MarkedForDeletion = false
            },
            new PropertyDetailsOldEntity
            {
                Id = 2,
                PropertyMastOldId = 100,
                OldFloorId = 2,
                OldSubFloorId = 2,
                OldConstructionYear = "2019",
                OldAssessmentYear = "2020",
                OldConstructionTypeId = 2,
                OldTypeOfUseId = 2,
                OldSubTypeOfUseId = 2,
                OldCarpetAreaSqMeter = 150.00,
                OldCarpetAreaSqFeet = 1614.59,
                OldBuiltupAreaSqMeter = 180.00,
                OldBuiltupAreaSqFeet = 1937.51,
                IsActive = true,
                MarkedForDeletion = false
            },
            new PropertyDetailsOldEntity
            {
                Id = 3,
                PropertyMastOldId = 100,
                OldFloorId = 1,
                OldSubFloorId = 1,
                OldConstructionYear = "2018",
                OldAssessmentYear = "2019",
                OldConstructionTypeId = 1,
                OldTypeOfUseId = 1,
                OldSubTypeOfUseId = 1,
                OldCarpetAreaSqMeter = 90.00,
                OldCarpetAreaSqFeet = 968.75,
                OldBuiltupAreaSqMeter = 110.00,
                OldBuiltupAreaSqFeet = 1184.03,
                IsActive = true,
                MarkedForDeletion = false
            },
            new PropertyDetailsOldEntity
            {
                Id = 4,
                PropertyMastOldId = 100,
                OldFloorId = 2,
                OldSubFloorId = null,
                OldConstructionYear = "invalid_year",
                OldAssessmentYear = "2022",
                OldConstructionTypeId = 2,
                OldTypeOfUseId = 2,
                OldSubTypeOfUseId = null,
                OldCarpetAreaSqMeter = 200.00,
                OldCarpetAreaSqFeet = 2152.78,
                IsActive = true,
                MarkedForDeletion = false
            },
            new PropertyDetailsOldEntity
            {
                Id = 5,
                PropertyMastOldId = 100,
                OldFloorId = 1,
                OldSubFloorId = 2,
                OldConstructionYear = "2021",
                OldAssessmentYear = "",
                OldConstructionTypeId = 1,
                OldTypeOfUseId = 1,
                OldSubTypeOfUseId = 1,
                IsActive = true,
                MarkedForDeletion = false
            },
            // Inactive record - should be excluded
            new PropertyDetailsOldEntity
            {
                Id = 6,
                PropertyMastOldId = 100,
                OldFloorId = 1,
                IsActive = false,
                MarkedForDeletion = false
            },
            // Marked for deletion - should be excluded
            new PropertyDetailsOldEntity
            {
                Id = 7,
                PropertyMastOldId = 100,
                OldFloorId = 1,
                IsActive = true,
                MarkedForDeletion = true
            }
        };

        context.PropertyDetailsOld.AddRange(details);
        await context.SaveChangesAsync();

        return context;
    }

    #region Basic Pagination Tests

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_WithValidData_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 3
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount); // 5 active records (excluding inactive and marked for deletion)
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(2, result.TotalPages); // Ceiling(5/3) = 2
        Assert.False(result.HasPrevious);
        Assert.True(result.HasNext);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_SecondPage_ReturnsCorrectRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 2,
            PageSize = 3
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(3, result.PageSize);
        Assert.True(result.HasPrevious);
        Assert.False(result.HasNext);
        Assert.Equal(2, result.Items.Count); // 2 remaining records on page 2
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_PageBeyondTotal_ReturnsEmptyItems()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 10,
            PageSize = 3
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_LargePageSize_ReturnsAllRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(5, result.Items.Count);
        Assert.False(result.HasNext);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_UnpagedMode_ReturnsAllRecordsWithNormalizedMetadata()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = -1 // Unpaged mode
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount); // 5 active records
        Assert.Equal(5, result.Items.Count); // All records returned
        Assert.Equal(1, result.PageNumber); // Normalized to 1
        Assert.Equal(5, result.PageSize); // Normalized to totalCount
        Assert.Equal(1, result.TotalPages); // 5/5 = 1 (no division by -1)
        Assert.False(result.HasPrevious);
        Assert.False(result.HasNext);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_UnpagedModeWithNoRecords_ReturnsNormalizedMetadata()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new PropertyRepository(context);

        var property = new PropertyEntity
        {
            Id = 1,
            PropertyNo = "PROP001",
            WardId = 1,
            TaxZoneId = 1,
            PropertyMastOldId = 100,
            IsActive = true
        };
        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = -1 // Unpaged mode
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
        Assert.Equal(1, result.PageNumber); // Normalized to 1
        Assert.Equal(1, result.PageSize); // Normalized to Math.Max(1, 0) = 1
        Assert.Equal(0, result.TotalPages); // 0/1 = 0 (no error)
        Assert.False(result.HasPrevious);
        Assert.False(result.HasNext);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_UnpagedModeWithNullPropertyMastOldId_ReturnsNormalizedMetadata()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new PropertyRepository(context);

        var property = new PropertyEntity
        {
            Id = 1,
            PropertyNo = "PROP001",
            WardId = 1,
            TaxZoneId = 1,
            PropertyMastOldId = null,
            IsActive = true
        };
        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = -1 // Unpaged mode
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
        Assert.Equal(1, result.PageNumber); // Normalized to 1
        Assert.Equal(1, result.PageSize); // Normalized to 1 for unpaged mode
        Assert.Equal(0, result.TotalPages); // No error
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_UnpagedModeWithFilters_ReturnsFilteredRecordsWithNormalizedMetadata()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = -1, // Unpaged mode
            OldFloorId = 1
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount); // 3 records with OldFloorId = 1
        Assert.Equal(3, result.Items.Count); // All matching records returned
        Assert.Equal(1, result.PageNumber); // Normalized to 1
        Assert.Equal(3, result.PageSize); // Normalized to totalCount
        Assert.Equal(1, result.TotalPages); // 3/3 = 1
        Assert.All(result.Items, item => Assert.Equal(1, item.OldFloorId));
    }

    #endregion

    #region Filter Tests

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_FilterByOldFloorId_ReturnsMatchingRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            OldFloorId = 1
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount); // 3 records with OldFloorId = 1
        Assert.All(result.Items, item => Assert.Equal(1, item.OldFloorId));
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_FilterByOldSubFloorId_ReturnsMatchingRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            OldSubFloorId = 1
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount); // 2 records with OldSubFloorId = 1
        Assert.All(result.Items, item => Assert.Equal(1, item.OldSubFloorId));
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_FilterByOldConstructionTypeId_ReturnsMatchingRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            OldConstructionTypeId = 2
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount); // 2 records with OldConstructionTypeId = 2
        Assert.All(result.Items, item => Assert.Equal(2, item.OldConstructionTypeId));
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_FilterByOldTypeOfUseId_ReturnsMatchingRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            OldTypeOfUseId = 1
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount); // 3 records with OldTypeOfUseId = 1
        Assert.All(result.Items, item => Assert.Equal(1, item.OldTypeOfUseId));
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_FilterByOldSubTypeOfUseId_ReturnsMatchingRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            OldSubTypeOfUseId = 1
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount); // 3 records with OldSubTypeOfUseId = 1
        Assert.All(result.Items, item => Assert.Equal(1, item.OldSubTypeOfUseId));
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_FilterByOldConstructionYear_ReturnsMatchingRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            OldConstructionYear = "2020"
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal("2020", item.OldConstructionYear));
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_FilterByOldAssessmentYear_ReturnsMatchingRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            OldAssessmentYear = "2021"
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal("2021", item.OldAssessmentYear));
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_MultipleFilters_ReturnsIntersection()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            OldFloorId = 1,
            OldConstructionTypeId = 1
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Items, item =>
        {
            Assert.Equal(1, item.OldFloorId);
            Assert.Equal(1, item.OldConstructionTypeId);
        });
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_SearchByFloorDescription_ReturnsMatchingRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Ground"
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0);
        Assert.All(result.Items, item =>
            Assert.Contains("Ground", item.FloorDescription ?? "", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_SearchByConstructionTypeDescription_ReturnsMatchingRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Concrete"
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0);
        Assert.All(result.Items, item =>
            Assert.Contains("Concrete", item.ConstructionTypeDescription ?? "", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_SearchCaseInsensitive_ReturnsMatchingRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "RESIDENTIAL"
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_SearchNoMatch_ReturnsEmptyResults()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "NonExistentTerm12345"
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_SortById_Ascending_ReturnsSortedRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "Id",
            SortOrder = "asc"
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var ids = result.Items.Select(x => x.Id).ToList();
        Assert.Equal(ids.OrderBy(x => x).ToList(), ids);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_SortById_Descending_ReturnsSortedRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "Id",
            SortOrder = "desc"
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var ids = result.Items.Select(x => x.Id).ToList();
        Assert.Equal(ids.OrderByDescending(x => x).ToList(), ids);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_SortByOldFloorId_ReturnsSortedRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "OldFloorId",
            SortOrder = "asc"
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var floorIds = result.Items.Select(x => x.OldFloorId).ToList();
        Assert.Equal(floorIds.OrderBy(x => x).ToList(), floorIds);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_SortByOldConstructionYear_ReturnsSortedRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "OldConstructionYear",
            SortOrder = "asc"
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Verify results are sorted (string comparison)
        var years = result.Items.Select(x => x.OldConstructionYear).ToList();
        Assert.Equal(years.OrderBy(x => x).ToList(), years);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_NoSortSpecified_DefaultsToIdSort()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var ids = result.Items.Select(x => x.Id).ToList();
        Assert.Equal(ids.OrderBy(x => x).ToList(), ids);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_InvalidSortField_DefaultsToIdSort()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "InvalidField"
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var ids = result.Items.Select(x => x.Id).ToList();
        Assert.Equal(ids.OrderBy(x => x).ToList(), ids);
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_PropertyNotFound_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(9999, query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_InactiveProperty_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new PropertyRepository(context);

        var property = new PropertyEntity
        {
            Id = 1,
            PropertyNo = "PROP001",
            WardId = 1,
            TaxZoneId = 1,
            PropertyMastOldId = 100,
            IsActive = false
        };
        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_PropertyMarkedForDeletion_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new PropertyRepository(context);

        var property = new PropertyEntity
        {
            Id = 1,
            PropertyNo = "PROP001",
            WardId = 1,
            TaxZoneId = 1,
            PropertyMastOldId = 100,
            IsActive = true
        };
        property.MarkedForDeletion = true;
        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_NullPropertyMastOldId_ReturnsEmptyResult()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new PropertyRepository(context);

        var property = new PropertyEntity
        {
            Id = 1,
            PropertyNo = "PROP001",
            WardId = 1,
            TaxZoneId = 1,
            PropertyMastOldId = null,
            IsActive = true
        };
        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_NoFloorDetailsOld_ReturnsEmptyResult()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new PropertyRepository(context);

        var property = new PropertyEntity
        {
            Id = 1,
            PropertyNo = "PROP001",
            WardId = 1,
            TaxZoneId = 1,
            PropertyMastOldId = 100,
            IsActive = true
        };
        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_ExcludesInactiveRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(result.Items, item => item.Id == 6); // Inactive record
        Assert.All(result.Items, item => Assert.True(item.MarkedForDeletion == false));
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_ExcludesMarkedForDeletionRecords()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(result.Items, item => item.Id == 7); // Marked for deletion record
    }

    #endregion

    #region Year Parsing Tests

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_ValidYears_ParsesCorrectly()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            OldConstructionYear = "2020"
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var firstItem = result.Items.First();
        Assert.Equal("2020", firstItem.OldConstructionYear);
        Assert.Equal(2020, firstItem.ConstructionYearValue);
        Assert.Equal("2021", firstItem.OldAssessmentYear);
        Assert.Equal(2021, firstItem.AssessmentYearValue);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_InvalidConstructionYear_ParsesAsNull()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            OldConstructionYear = "invalid_year"
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var item = result.Items.First();
        Assert.Equal("invalid_year", item.OldConstructionYear);
        Assert.Null(item.ConstructionYearValue);
        Assert.Equal(2022, item.AssessmentYearValue); // Valid assessment year
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_EmptyAssessmentYear_ParsesAsNull()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            OldConstructionYear = "2021"
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var item = result.Items.First();
        Assert.Equal("2021", item.OldConstructionYear);
        Assert.Equal(2021, item.ConstructionYearValue);
        Assert.Null(item.AssessmentYearValue);
    }

    #endregion

    #region Joined Data Tests

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_WithJoinedData_ReturnsDescriptions()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 1
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var item = result.Items.First();
        Assert.NotNull(item.FloorDescription);
        Assert.NotNull(item.SubFloorDescription);
        Assert.NotNull(item.ConstructionTypeDescription);
        Assert.NotNull(item.TypeOfUseDescription);
        Assert.NotNull(item.SubTypeOfUseDescription);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_WithNullableJoins_HandlesNullGracefully()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 10,
            OldConstructionYear = "invalid_year" // This record has null SubFloorId and SubTypeOfUseId
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var item = result.Items.First();
        Assert.Null(item.OldSubFloorId);
        Assert.Null(item.SubFloorDescription);
        Assert.Null(item.OldSubTypeOfUseId);
        Assert.Null(item.SubTypeOfUseDescription);
    }

    #endregion

    #region Combined Filter, Search, Sort, and Pagination Tests

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_CombinedFiltersSearchSortPaging_WorksCorrectly()
    {
        // Arrange
        using var context = await CreateContextWithTestData();
        var repository = new PropertyRepository(context);
        var query = new FloorDetailsOldQuery
        {
            PageNumber = 1,
            PageSize = 2,
            OldFloorId = 1,
            SearchTerm = "Apartment",
            SortBy = "OldConstructionYear",
            SortOrder = "desc"
        };

        // Act
        var result = await repository.GetFloorDetailsOldPagedAsync(1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0);
        Assert.True(result.Items.Count <= 2);
        Assert.All(result.Items, item =>
        {
            Assert.Equal(1, item.OldFloorId);
            Assert.Contains("Apartment", item.SubTypeOfUseDescription ?? "", StringComparison.OrdinalIgnoreCase);
        });
    }

    #endregion
}
