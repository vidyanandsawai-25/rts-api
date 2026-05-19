using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using NtisPlatform.Tests.Helpers;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

/// <summary>
/// Comprehensive tests for PropertyRepository.GetFloorDetailsOldByIdAsync to achieve 100% line coverage
/// </summary>
public class PropertyRepositoryGetFloorDetailsOldTests
{
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_WithValidData_ReturnsFloorDetails()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new PropertyRepository(context);

        // Create floor master data
        var floor = EntityTestHelpers.CreateFloorEntity(id: 1);
        floor.FloorCode = "F1";
        floor.Description = "First Floor";

        var subFloor = EntityTestHelpers.CreateSubFloorEntity(id: 1);
        subFloor.SubFloorCode = "SF1";
        subFloor.Description = "Sub Floor 1";

        var constructionType = EntityTestHelpers.CreateConstructionTypeEntity(id: 1);
        constructionType.ConstructionCode = "CT1";
        constructionType.Description = "Concrete";

        var typeOfUse = EntityTestHelpers.CreateTypeOfUseEntity(id: 1);
        typeOfUse.TypeOfUseCode = "TOU1";
        typeOfUse.Description = "Residential";

        var subTypeOfUse = EntityTestHelpers.CreateSubTypeOfUseEntity(id: 1);
        subTypeOfUse.Description = "Apartment";
        subTypeOfUse.TypeOfUseId = 1;

        context.FloorEntity.Add(floor);
        context.SubFloorEntity.Add(subFloor);
        context.ConstructionTypeEntity.Add(constructionType);
        context.TypeOfUse.Add(typeOfUse);
        context.SubTypeOfUse.Add(subTypeOfUse);

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

        // Create property details old
        var propertyDetailsOld = new PropertyDetailsOldEntity
        {
            Id = 500,
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
        };
        context.PropertyDetailsOld.Add(propertyDetailsOld);

        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetFloorDetailsOldByIdAsync(1, 500, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(500, result.Id);
        Assert.Equal(1, result.PropertyId);
        Assert.Equal(1, result.OldFloorId);
        Assert.Equal("First Floor", result.FloorDescription);
        Assert.Equal(1, result.OldSubFloorId);
        Assert.Equal("Sub Floor 1", result.SubFloorDescription);
        Assert.Equal("2020", result.OldConstructionYear);
        Assert.Equal(2020, result.ConstructionYearValue);
        Assert.Equal("2021", result.OldAssessmentYear);
        Assert.Equal(2021, result.AssessmentYearValue);
        Assert.Equal(1, result.OldConstructionTypeId);
        Assert.Equal("Concrete", result.ConstructionTypeDescription);
        Assert.Equal(1, result.OldTypeOfUseId);
        Assert.Equal("Residential", result.TypeOfUseDescription);
        Assert.Equal(1, result.OldSubTypeOfUseId);
        Assert.Equal("Apartment", result.SubTypeOfUseDescription);
        Assert.Equal(100.50, result.OldCarpetAreaSqMeter);
        Assert.Equal(1081.60, result.OldCarpetAreaSqFeet);
        Assert.Equal(120.00, result.OldBuiltupAreaSqMeter);
        Assert.Equal(1291.67, result.OldBuiltupAreaSqFeet);
        Assert.False(result.MarkedForDeletion);
        Assert.Null(result.MarkedForDeletionDate);
    }

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_WithNonExistentProperty_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetFloorDetailsOldByIdAsync(9999, 500, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_WithInactiveProperty_ReturnsNull()
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

        // Act
        var result = await repository.GetFloorDetailsOldByIdAsync(1, 500, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_WithPropertyMarkedForDeletion_ReturnsNull()
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

        // Act
        var result = await repository.GetFloorDetailsOldByIdAsync(1, 500, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_WithNullPropertyMastOldId_ReturnsNull()
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

        // Act
        var result = await repository.GetFloorDetailsOldByIdAsync(1, 500, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_WithNonExistentFloorId_ReturnsNull()
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

        // Act
        var result = await repository.GetFloorDetailsOldByIdAsync(1, 9999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_WithInactiveFloorDetails_ReturnsNull()
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

        var propertyDetailsOld = new PropertyDetailsOldEntity
        {
            Id = 500,
            PropertyMastOldId = 100,
            IsActive = false,
            MarkedForDeletion = false
        };
        context.PropertyDetailsOld.Add(propertyDetailsOld);

        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetFloorDetailsOldByIdAsync(1, 500, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_WithMarkedForDeletionFloorDetails_ReturnsNull()
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

        var propertyDetailsOld = new PropertyDetailsOldEntity
        {
            Id = 500,
            PropertyMastOldId = 100,
            IsActive = true,
            MarkedForDeletion = true
        };
        context.PropertyDetailsOld.Add(propertyDetailsOld);

        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetFloorDetailsOldByIdAsync(1, 500, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_WithNullOptionalFields_ReturnsDataWithNulls()
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

        var propertyDetailsOld = new PropertyDetailsOldEntity
        {
            Id = 500,
            PropertyMastOldId = 100,
            OldFloorId = 1,  // Required field - use default value
            OldSubFloorId = null,
            OldConstructionTypeId = 1,  // Required field - use default value
            OldTypeOfUseId = 1,  // Required field - use default value
            OldSubTypeOfUseId = null,
            IsActive = true,
            MarkedForDeletion = false
        };
        context.PropertyDetailsOld.Add(propertyDetailsOld);

        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetFloorDetailsOldByIdAsync(1, 500, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(500, result.Id);
        // OldFloorId, OldConstructionTypeId, OldTypeOfUseId are now non-nullable (int) so they have default values
        Assert.Equal(1, result.OldFloorId);  // Required field - defaults to 1 from test data
        Assert.Null(result.FloorDescription);
        Assert.Null(result.OldSubFloorId);  // This is still nullable
        Assert.Null(result.SubFloorDescription);
        Assert.Equal(1, result.OldConstructionTypeId);  // Required field - defaults to 1 from test data
        Assert.Null(result.ConstructionTypeDescription);
        Assert.Equal(1, result.OldTypeOfUseId);  // Required field - defaults to 1 from test data
        Assert.Null(result.TypeOfUseDescription);
        Assert.Null(result.OldSubTypeOfUseId);  // This is still nullable
        Assert.Null(result.SubTypeOfUseDescription);
    }

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_WithInvalidConstructionYear_ParsesAsNull()
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

        var propertyDetailsOld = new PropertyDetailsOldEntity
        {
            Id = 500,
            PropertyMastOldId = 100,
            OldConstructionYear = "invalid",
            OldAssessmentYear = "2021",
            IsActive = true,
            MarkedForDeletion = false
        };
        context.PropertyDetailsOld.Add(propertyDetailsOld);

        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetFloorDetailsOldByIdAsync(1, 500, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("invalid", result.OldConstructionYear);
        Assert.Null(result.ConstructionYearValue);
        Assert.Equal("2021", result.OldAssessmentYear);
        Assert.Equal(2021, result.AssessmentYearValue);
    }

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_WithInvalidAssessmentYear_ParsesAsNull()
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

        var propertyDetailsOld = new PropertyDetailsOldEntity
        {
            Id = 500,
            PropertyMastOldId = 100,
            OldConstructionYear = "2020",
            OldAssessmentYear = "not-a-year",
            IsActive = true,
            MarkedForDeletion = false
        };
        context.PropertyDetailsOld.Add(propertyDetailsOld);

        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetFloorDetailsOldByIdAsync(1, 500, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("2020", result.OldConstructionYear);
        Assert.Equal(2020, result.ConstructionYearValue);
        Assert.Equal("not-a-year", result.OldAssessmentYear);
        Assert.Null(result.AssessmentYearValue);
    }

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_WithEmptyStringYears_ParsesAsNull()
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

        var propertyDetailsOld = new PropertyDetailsOldEntity
        {
            Id = 500,
            PropertyMastOldId = 100,
            OldConstructionYear = "",
            OldAssessmentYear = "",
            IsActive = true,
            MarkedForDeletion = false
        };
        context.PropertyDetailsOld.Add(propertyDetailsOld);

        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetFloorDetailsOldByIdAsync(1, 500, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ConstructionYearValue);
        Assert.Null(result.AssessmentYearValue);
    }

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_WithInactiveJoinedEntities_ReturnsNullDescriptions()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new PropertyRepository(context);

        // Create inactive floor master data
        var floor = new FloorEntity { Id = 1, FloorCode = "F1", Description = "First Floor", IsActive = false };
        context.FloorEntity.Add(floor);

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

        var propertyDetailsOld = new PropertyDetailsOldEntity
        {
            Id = 500,
            PropertyMastOldId = 100,
            OldFloorId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        context.PropertyDetailsOld.Add(propertyDetailsOld);

        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetFloorDetailsOldByIdAsync(1, 500, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.OldFloorId);
        Assert.Null(result.FloorDescription); // Inactive floor should not be included
    }

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_WithMismatchedPropertyMastOldId_ReturnsNull()
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

        // Create floor details with different PropertyMastOldId
        var propertyDetailsOld = new PropertyDetailsOldEntity
        {
            Id = 500,
            PropertyMastOldId = 999, // Different from property's PropertyMastOldId
            IsActive = true,
            MarkedForDeletion = false
        };
        context.PropertyDetailsOld.Add(propertyDetailsOld);

        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetFloorDetailsOldByIdAsync(1, 500, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
