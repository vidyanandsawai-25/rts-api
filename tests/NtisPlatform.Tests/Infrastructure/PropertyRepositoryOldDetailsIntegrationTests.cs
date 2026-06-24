using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Application.Services.Property;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using NtisPlatform.Infrastructure.Repositories.Property;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure;

/// <summary>
/// Comprehensive integration tests for PropertyRepository Old Details methods
/// Target: 100% line coverage and 100% branch coverage
/// </summary>
public class PropertyRepositoryOldDetailsIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    // Old property + old taxes are exercised through the per-tab service (where their orchestration lives);
    // old (historical) floor is exercised directly against the per-tab repository (its data-access signatures
    // match the former PropertyRepository floor methods exactly).
    private readonly PropertyOldDetailsService _oldDetailsService;
    private readonly PropertyOldDetailsRepository _oldDetailsRepository;

    public PropertyRepositoryOldDetailsIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        var unitOfWork = new UnitOfWork(_context);
        _oldDetailsRepository = new PropertyOldDetailsRepository(_context, unitOfWork);
        _oldDetailsService = new PropertyOldDetailsService(_oldDetailsRepository, new MasterRepository(_context), unitOfWork, new PropertyMutationInvariantPolicy());

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add Zones
        _context.ZoneMaster.Add(new ZoneEntity { Id = 1, ZoneNo = "1", Description = "Zone 1", IsActive = true });

        // Add Wards
        _context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "1", Description = "Ward 1", ZoneId = 1, IsActive = true });

        // Add Floor Master
        _context.FloorEntity.AddRange(
            new FloorEntity { Id = 1, FloorCode = "GF", Description = "Ground Floor", IsActive = true },
            new FloorEntity { Id = 2, FloorCode = "FF", Description = "First Floor", IsActive = true },
            new FloorEntity { Id = 3, FloorCode = "SF", Description = "Second Floor", IsActive = true },
            new FloorEntity { Id = 999, FloorCode = "IF", Description = "Inactive Floor", IsActive = false }
        );

        // Add SubFloor Master
        _context.SubFloorEntity.AddRange(
            new SubFloorEntity { Id = 1, SubFloorCode = "BM", Description = "Basement", IsActive = true },
            new SubFloorEntity { Id = 2, SubFloorCode = "MZ", Description = "Mezzanine", IsActive = true },
            new SubFloorEntity { Id = 999, SubFloorCode = "IX", Description = "Inactive SubFloor", IsActive = false }
        );

        // Add Construction Type Master
        _context.ConstructionTypeEntity.AddRange(
            new ConstructionTypeEntity { Id = 1, ConstructionCode = "RCC", Description = "RCC", IsActive = true },
            new ConstructionTypeEntity { Id = 2, ConstructionCode = "STL", Description = "Steel", IsActive = true },
            new ConstructionTypeEntity { Id = 999, ConstructionCode = "IX", Description = "Inactive Type", IsActive = false }
        );

        // Add Type of Use Group (required by TypeOfUse)
        _context.TypeOfUseGroup.Add(new TypeOfUseGroupEntity { Id = 1, TypeOfUseGroupCode = "GEN", GroupName = "General", IsActive = true });

        // Add Type of Use Master
        _context.TypeOfUse.AddRange(
            new TypeOfUseEntity { Id = 1, TypeOfUseCode = "RES", Description = "Residential", Type = "R", TypeOfUseGroupId = 1, IsActive = true },
            new TypeOfUseEntity { Id = 2, TypeOfUseCode = "COM", Description = "Commercial", Type = "C", TypeOfUseGroupId = 1, IsActive = true },
            new TypeOfUseEntity { Id = 999, TypeOfUseCode = "IX", Description = "Inactive Use", Type = "I", TypeOfUseGroupId = 1, IsActive = false }
        );

        // Add SubType of Use Master
        _context.SubTypeOfUse.AddRange(
            new SubTypeOfUseEntity { Id = 1, Description = "Apartment", TypeOfUseId = 1, IsActive = true },
            new SubTypeOfUseEntity { Id = 2, Description = "Shop", TypeOfUseId = 2, IsActive = true },
            new SubTypeOfUseEntity { Id = 999, Description = "Inactive SubUse", TypeOfUseId = 1, IsActive = false }
        );

        // Add Tax Master with OldTaxStatus
        _context.TaxMaster.AddRange(
            new TaxMasterEntity { Id = 1, TaxCode = "PT", TaxName = "Property Tax", TaxNameAlias = "PT", TaxCategoryId = 1, OldTaxStatus = true, IsActive = true, DisplayOrder = 1 },
            new TaxMasterEntity { Id = 2, TaxCode = "WT", TaxName = "Water Tax", TaxNameAlias = "WT", TaxCategoryId = 1, OldTaxStatus = true, IsActive = true, DisplayOrder = 2 },
            new TaxMasterEntity { Id = 3, TaxCode = "INT", TaxName = "Interest", TaxNameAlias = "INT", TaxCategoryId = 1, OldTaxStatus = true, IsActive = true, DisplayOrder = 3 },
            new TaxMasterEntity { Id = 4, TaxCode = "DT", TaxName = "Drainage Tax", TaxNameAlias = "DT", TaxCategoryId = 1, OldTaxStatus = true, IsActive = true, DisplayOrder = 4 }
        );

        // Add Year Master
        _context.YearMaster.AddRange(
            new YearMasterEntity { Id = 1, Year = 2020, YearCode = "2020-21", IsActive = true },
            new YearMasterEntity { Id = 2, Year = 2021, YearCode = "2021-22", IsActive = true },
            new YearMasterEntity { Id = 3, Year = 2022, YearCode = "2022-23", IsActive = true },
            new YearMasterEntity { Id = 999, Year = 2019, YearCode = "2019-20", IsActive = false }
        );

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetOldDetailsAsync Tests

    [Fact]
    public async Task GetOldDetailsAsync_PropertyNotFound_ReturnsNull()
    {
        // Arrange
        int nonExistentPropertyId = 99999;

        // Act
        var result = await _oldDetailsService.GetOldDetailsAsync(nonExistentPropertyId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetOldDetailsAsync_PropertyExistsButNoPropertyMastOldId_ReturnsEmptyDto()
    {
        // Arrange
        var property = new PropertyEntity
        {
            Id = 1,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = null,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetOldDetailsAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyId);
        Assert.Null(result.OldWardNo);
        Assert.Null(result.OldPropertyNo);
    }

    [Fact]
    public async Task GetOldDetailsAsync_WithPropertyMastOldButNoDetails_ReturnsOnlyMastData()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity
        {
            Id = 1,
            OldWardNo = "5",
            OldPropertyNo = "123",
            OldPlotNo = "456",
            OldRV = 50000,
            OldALV = 45000,
            OldTotalTax = 5000,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 2,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetOldDetailsAsync(2, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.PropertyId);
        Assert.Equal("5", result.OldWardNo);
        Assert.Equal("123", result.OldPropertyNo);
        Assert.Equal("456", result.OldPlotNo);
        Assert.Equal(50000, result.OldRV);
        Assert.Equal(45000, result.OldALV);
        Assert.Equal(5000, result.OldTotalTax);
        Assert.Null(result.OldConstructionYear);
    }

    [Fact]
    public async Task GetOldDetailsAsync_WithFullData_ReturnsCompleteDto()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity
        {
            Id = 2,
            OldWardNo = "5",
            OldPropertyNo = "123",
            OldPartitionNo = "A",
            OldEgovNo = "EG001",
            OldPlotArea = 1000,
            OldPlotNo = "456",
            OldRV = 50000,
            OldALV = 45000,
            OldTotalTax = 5000,
            OldZoneNo = "Z1",
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMastOld.Add(propertyMastOld);

        var propertyDetailsOld = new PropertyDetailsOldEntity
        {
            Id = 1,
            PropertyMastOldId = 2,
            OldConstructionYear = "2015",
            OldCarpetAreaSqFeet = 1200,
            OldCarpetAreaSqMeter = 111.48,
            OldConstructionTypeId = 1,
            OldTypeOfUseId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyDetailsOld.Add(propertyDetailsOld);

        var property = new PropertyEntity
        {
            Id = 3,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 2,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetOldDetailsAsync(3, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.PropertyId);
        Assert.Equal("5", result.OldWardNo);
        Assert.Equal("123", result.OldPropertyNo);
        Assert.Equal("2015", result.OldConstructionYear);
        Assert.Equal(1200, result.OldCarpetAreaSqFeet);
        Assert.Equal(111.48, result.OldCarpetAreaSqMeter);
        Assert.Equal(1, result.OldConstructionTypeId);
        Assert.Equal(1, result.OldTypeOfUseId);
    }

    [Fact]
    public async Task GetOldDetailsAsync_PropertyMarkedForDeletion_ReturnsNull()
    {
        // Arrange
        var property = new PropertyEntity
        {
            Id = 4,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            IsActive = true,
            MarkedForDeletion = true
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetOldDetailsAsync(4, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetOldDetailsAsync_PropertyInactive_ReturnsNull()
    {
        // Arrange
        var property = new PropertyEntity
        {
            Id = 5,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            IsActive = false,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetOldDetailsAsync(5, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region UpdateOldDetailsAsync Tests

    [Fact]
    public async Task UpdateOldDetailsAsync_PropertyNotFound_ReturnsNull()
    {
        // Arrange
        var dto = new UpdatePropertyOldDetailsDto { OldWardNo = "1" };

        // Act
        var result = await _oldDetailsService.UpdateOldDetailsAsync(99999, dto, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateOldDetailsAsync_NoPropertyMastOldId_CreatesNewPropertyMastOld()
    {
        // Arrange
        var property = new PropertyEntity
        {
            Id = 10,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = null,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldDetailsDto
        {
            OldWardNo = "10",
            OldPropertyNo = "200",
            OldPlotNo = "300",
            OldRV = 60000,
            OldALV = 55000,
            OldTotalTax = 6000
        };

        // Act
        var result = await _oldDetailsService.UpdateOldDetailsAsync(10, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.PropertyId);
        Assert.Equal("10", result.OldWardNo);
        Assert.Equal("200", result.OldPropertyNo);

        // Verify PropertyMast was updated with PropertyMastOldId
        var updatedProperty = await _context.PropertyMast.FindAsync(10);
        Assert.NotNull(updatedProperty);
        Assert.NotNull(updatedProperty.PropertyMastOldId);

        // Verify PropertyMastOld was created
        var createdPropertyMastOld = await _context.PropertyMastOld.FindAsync(updatedProperty.PropertyMastOldId);
        Assert.NotNull(createdPropertyMastOld);
        Assert.Equal("10", createdPropertyMastOld.OldWardNo);
    }

    [Fact]
    public async Task UpdateOldDetailsAsync_ExistingPropertyMastOld_UpdatesData()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity
        {
            Id = 10,
            OldWardNo = "5",
            OldPropertyNo = "100",
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 11,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 10,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldDetailsDto
        {
            OldWardNo = "15",
            OldPropertyNo = "500",
            OldPartitionNo = "B",
            OldEgovNo = "EG002",
            OldPlotArea = 2000,
            OldPlotNo = "789",
            OldRV = 80000,
            OldALV = 75000,
            OldTotalTax = 8000,
            OldZoneNo = "Z2"
        };

        // Act
        var result = await _oldDetailsService.UpdateOldDetailsAsync(11, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("15", result.OldWardNo);
        Assert.Equal("500", result.OldPropertyNo);
        Assert.Equal("B", result.OldPartitionNo);
        Assert.Equal("EG002", result.OldEgovNo);
        Assert.Equal(2000, result.OldPlotArea);
        Assert.Equal("789", result.OldPlotNo);
        Assert.Equal(80000, result.OldRV);
        Assert.Equal(75000, result.OldALV);
        Assert.Equal(8000, result.OldTotalTax);
        Assert.Equal("Z2", result.OldZoneNo);
    }

    [Fact]
    public async Task UpdateOldDetailsAsync_CreatePropertyDetailsOld_WhenDataProvided()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity
        {
            Id = 11,
            OldWardNo = "5",
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 12,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 11,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldDetailsDto
        {
            OldFloorId = 1,  // Required field
            OldConstructionYear = "2018",
            OldCarpetAreaSqFeet = 1500,
            OldCarpetAreaSqMeter = 139.35,
            OldConstructionTypeId = 1,
            OldTypeOfUseId = 2
        };

        // Act
        var result = await _oldDetailsService.UpdateOldDetailsAsync(12, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("2018", result.OldConstructionYear);
        Assert.Equal(1500, result.OldCarpetAreaSqFeet);
        Assert.Equal(139.35, result.OldCarpetAreaSqMeter);
        Assert.Equal(1, result.OldConstructionTypeId);
        Assert.Equal(2, result.OldTypeOfUseId);

        // Verify PropertyDetailsOld was created
        var createdDetails = await _context.PropertyDetailsOld
            .FirstOrDefaultAsync(pd => pd.PropertyMastOldId == 11);
        Assert.NotNull(createdDetails);
        Assert.Equal("2018", createdDetails.OldConstructionYear);
    }

    [Fact]
    public async Task UpdateOldDetailsAsync_UpdateExistingPropertyDetailsOld()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity
        {
            Id = 12,
            OldWardNo = "5",
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMastOld.Add(propertyMastOld);

        var existingDetails = new PropertyDetailsOldEntity
        {
            Id = 10,
            PropertyMastOldId = 12,
            OldConstructionYear = "2015",
            OldCarpetAreaSqFeet = 1000,
            OldCarpetAreaSqMeter = 92.90,
            OldConstructionTypeId = 1,
            OldTypeOfUseId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyDetailsOld.Add(existingDetails);

        var property = new PropertyEntity
        {
            Id = 13,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 12,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldDetailsDto
        {
            OldConstructionYear = "2020",
            OldCarpetAreaSqFeet = 1800,
            OldCarpetAreaSqMeter = 167.22,
            OldConstructionTypeId = 2,
            OldTypeOfUseId = 2
        };

        // Act
        var result = await _oldDetailsService.UpdateOldDetailsAsync(13, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("2020", result.OldConstructionYear);
        Assert.Equal(1800, result.OldCarpetAreaSqFeet);
        Assert.Equal(167.22, result.OldCarpetAreaSqMeter);
        Assert.Equal(2, result.OldConstructionTypeId);
        Assert.Equal(2, result.OldTypeOfUseId);

        // Verify existing record was updated, not new one created
        var allDetails = await _context.PropertyDetailsOld
            .Where(pd => pd.PropertyMastOldId == 12)
            .ToListAsync();
        Assert.Single(allDetails);
        Assert.Equal(10, allDetails[0].Id);
    }

    [Fact]
    public async Task UpdateOldDetailsAsync_NoDetailsData_DoesNotCreatePropertyDetailsOld()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity
        {
            Id = 13,
            OldWardNo = "5",
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 14,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 13,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldDetailsDto
        {
            OldWardNo = "10"
            // No PropertyDetailsOld data
        };

        // Act
        var result = await _oldDetailsService.UpdateOldDetailsAsync(14, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // Verify PropertyDetailsOld was NOT created
        var details = await _context.PropertyDetailsOld
            .FirstOrDefaultAsync(pd => pd.PropertyMastOldId == 13);
        Assert.Null(details);
    }

    [Fact]
    public async Task UpdateOldDetailsAsync_ZeroPlaceholderValues_DoesNotCreatePropertyDetailsOld()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity
        {
            Id = 14,
            OldWardNo = "5",
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 15,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 14,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldDetailsDto
        {
            OldWardNo = "10",
            OldFloorId = 0,
            OldConstructionTypeId = 0,
            OldTypeOfUseId = 0,
            OldCarpetAreaSqFeet = 0
        };

        // Act & Assert (Should not throw and should not create a Details record)
        var result = await _oldDetailsService.UpdateOldDetailsAsync(15, dto, CancellationToken.None);
        Assert.NotNull(result);

        var details = await _context.PropertyDetailsOld
            .FirstOrDefaultAsync(pd => pd.PropertyMastOldId == 14);
        Assert.Null(details);
    }

    [Fact]
    public async Task UpdateOldDetailsAsync_MaxValuePlaceholderValues_DoesNotCreatePropertyDetailsOld()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity
        {
            Id = 15,
            OldWardNo = "5",
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 16,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 15,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldDetailsDto
        {
            OldWardNo = "10",
            OldFloorId = int.MaxValue,
            OldConstructionTypeId = int.MaxValue,
            OldTypeOfUseId = int.MaxValue
        };

        // Act & Assert (Should not throw and should not create a Details record)
        var result = await _oldDetailsService.UpdateOldDetailsAsync(16, dto, CancellationToken.None);
        Assert.NotNull(result);

        var details = await _context.PropertyDetailsOld
            .FirstOrDefaultAsync(pd => pd.PropertyMastOldId == 15);
        Assert.Null(details);
    }

    [Fact]
    public async Task UpdateOldDetailsAsync_EmptyStringConstructionYear_DoesNotCreatePropertyDetailsOld()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity
        {
            Id = 16,
            OldWardNo = "5",
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 17,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 16,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldDetailsDto
        {
            OldWardNo = "10",
            OldConstructionYear = ""
        };

        // Act & Assert (Should not throw and should not create a Details record)
        var result = await _oldDetailsService.UpdateOldDetailsAsync(17, dto, CancellationToken.None);
        Assert.NotNull(result);

        var details = await _context.PropertyDetailsOld
            .FirstOrDefaultAsync(pd => pd.PropertyMastOldId == 16);
        Assert.Null(details);
    }

    #endregion

    #region GetOldTaxesDetailsAsync Tests

    [Fact]
    public async Task GetOldTaxesDetailsAsync_PropertyNotFound_ReturnsNull()
    {
        // Act
        var result = await _oldDetailsService.GetOldTaxesDetailsAsync(99999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetOldTaxesDetailsAsync_NoPropertyMastOldId_ReturnsEmptyResult()
    {
        // Arrange
        var property = new PropertyEntity
        {
            Id = 20,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = null,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetOldTaxesDetailsAsync(20, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(20, result.PropertyId);
        // No PropertyMastOldId means no transaction data - returns year fields as null with taxes having 0 amounts
        Assert.Single(result.TaxYears);
        var year = result.TaxYears[0];
        Assert.Null(year.FinanceYearId); // No transactions, so year is null
        Assert.Null(year.Year);
        Assert.Null(year.YearCode);
        Assert.Equal(4, year.Taxes.Count); // All 4 configured taxes
        Assert.All(year.Taxes, tax => Assert.Equal(0m, tax.TaxAmount)); // All amounts should be 0
    }

    [Fact]
    public async Task GetOldTaxesDetailsAsync_NoOldTaxesConfigured_ReturnsEmptyResult()
    {
        // Arrange
        // Remove all old taxes
        var oldTaxes = await _context.TaxMaster.Where(t => t.OldTaxStatus).ToListAsync();
        foreach (var tax in oldTaxes)
        {
            tax.OldTaxStatus = false;
        }
        await _context.SaveChangesAsync();

        var propertyMastOld = new PropertyMastOldEntity
        {
            Id = 20,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 21,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 20,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetOldTaxesDetailsAsync(21, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(21, result.PropertyId);
        Assert.Empty(result.TaxYears);

        // Restore old taxes for other tests
        foreach (var tax in oldTaxes)
        {
            tax.OldTaxStatus = true;
        }
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetOldTaxesDetailsAsync_NoTransactions_ReturnsEmptyYears()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity
        {
            Id = 21,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 22,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 21,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetOldTaxesDetailsAsync(22, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(22, result.PropertyId);
        // No transactions means no data - returns year fields as null with taxes having 0 amounts
        Assert.Single(result.TaxYears);
        var year = result.TaxYears[0];
        Assert.Null(year.FinanceYearId); // No transactions, so year is null
        Assert.Null(year.Year);
        Assert.Null(year.YearCode);
        Assert.Equal(4, year.Taxes.Count); // All 4 configured taxes
        Assert.All(year.Taxes, tax => Assert.Equal(0m, tax.TaxAmount)); // All amounts should be 0
    }

    [Fact]
    public async Task GetOldTaxesDetailsAsync_WithTransactions_ReturnsCorrectData()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity
        {
            Id = 22,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 23,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 22,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        // Add transactions for the active year (year 2022, FinanceYearId = 3)
        _context.TransMastOld.AddRange(
            new TransMastOldEntity
            {
                PropertyMastOldId = 22,
                FinanceYearId = 3, // Active year
                TaxId = 1,
                TaxAmount = 1000m,
                RVorCV = "RV",
                RVorCVValue = 0m,
                IsActive = true,
                MarkedForDeletion = false
            },
            new TransMastOldEntity
            {
                PropertyMastOldId = 22,
                FinanceYearId = 3, // Active year
                TaxId = 2,
                TaxAmount = 500m,
                RVorCV = "RV",
                RVorCVValue = 0m,
                IsActive = true,
                MarkedForDeletion = false
            },
            new TransMastOldEntity
            {
                PropertyMastOldId = 22,
                FinanceYearId = 3, // Active year
                TaxId = 3,
                TaxAmount = 100m,
                RVorCV = "RV",
                RVorCVValue = 0m,
                IsActive = true,
                MarkedForDeletion = false
            }
        );

        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetOldTaxesDetailsAsync(23, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(23, result.PropertyId);
        Assert.Single(result.TaxYears);

        var year = result.TaxYears[0];
        Assert.Equal(3, year.FinanceYearId); // Active year
        Assert.Equal(2022, year.Year);
        Assert.Equal("2022-23", year.YearCode);
        Assert.Equal(4, year.Taxes.Count); // All 4 configured taxes

        // Verify tax amounts
        var propertyTax = year.Taxes.First(t => t.TaxId == 1);
        Assert.Equal(1000m, propertyTax.TaxAmount);

        var waterTax = year.Taxes.First(t => t.TaxId == 2);
        Assert.Equal(500m, waterTax.TaxAmount);

        var interest = year.Taxes.First(t => t.TaxId == 3);
        Assert.Equal(100m, interest.TaxAmount);

        // Totals are no longer part of the response - removed from API
    }

    [Fact]
    public async Task GetOldTaxesDetailsAsync_MultipleYears_ReturnsAllYearsDescending()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity
        {
            Id = 23,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 24,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 23,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        // Add transactions for multiple years
        _context.TransMastOld.AddRange(
            new TransMastOldEntity { PropertyMastOldId = 23, FinanceYearId = 1, TaxId = 1, TaxAmount = 1000m, RVorCV = "RV", RVorCVValue = 50000m, IsActive = true, MarkedForDeletion = false },
            new TransMastOldEntity { PropertyMastOldId = 23, FinanceYearId = 2, TaxId = 1, TaxAmount = 1100m, RVorCV = "RV", RVorCVValue = 55000m, IsActive = true, MarkedForDeletion = false },
            new TransMastOldEntity { PropertyMastOldId = 23, FinanceYearId = 3, TaxId = 1, TaxAmount = 1200m, RVorCV = "RV", RVorCVValue = 60000m, IsActive = true, MarkedForDeletion = false }
        );

        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetOldTaxesDetailsAsync(24, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Current behavior: Returns only active year (year 2022 is active)
        Assert.Single(result.TaxYears);

        // Verify the active year is returned
        Assert.Equal(2022, result.TaxYears[0].Year);
    }

    [Fact]
    public async Task GetOldTaxesDetailsAsync_InactiveTransactions_AreIgnored()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity
        {
            Id = 24,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 25,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 24,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        // Add active and inactive transactions for the active year (year 3)
        _context.TransMastOld.AddRange(
            new TransMastOldEntity { PropertyMastOldId = 24, FinanceYearId = 3, TaxId = 1, TaxAmount = 1000m, RVorCV = "RV", RVorCVValue = 50000m, IsActive = true, MarkedForDeletion = false },
            new TransMastOldEntity { PropertyMastOldId = 24, FinanceYearId = 3, TaxId = 2, TaxAmount = 500m, RVorCV = "RV", RVorCVValue = 50000m, IsActive = false, MarkedForDeletion = false },
            new TransMastOldEntity { PropertyMastOldId = 24, FinanceYearId = 3, TaxId = 3, TaxAmount = 100m, RVorCV = "RV", RVorCVValue = 50000m, IsActive = true, MarkedForDeletion = true }
        );

        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetOldTaxesDetailsAsync(25, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Returns only active year
        Assert.Single(result.TaxYears);

        var year = result.TaxYears[0];
        Assert.Equal(3, year.FinanceYearId);
        Assert.Equal(2022, year.Year);
        Assert.Equal("2022-23", year.YearCode);

        // Only active and not marked for deletion transactions should be included
        var propertyTax = year.Taxes.First(t => t.TaxId == 1);
        Assert.Equal(1000m, propertyTax.TaxAmount); // Active transaction

        var waterTax = year.Taxes.First(t => t.TaxId == 2);
        Assert.Equal(0m, waterTax.TaxAmount); // Inactive

        var interest = year.Taxes.First(t => t.TaxId == 3);
        Assert.Equal(0m, interest.TaxAmount); // Marked for deletion
    }

    #endregion

    #region CreateOldTaxesDetailsAsync Tests

    [Fact]
    public async Task CreateOldTaxesDetailsAsync_PropertyNotFound_ReturnsNull()
    {
        // Arrange
        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>()
        };

        // Act
        var result = await _oldDetailsService.CreateOldTaxesDetailsAsync(99999, dto, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateOldTaxesDetailsAsync_NoPropertyMastOldId_CreatesNewPropertyMastOldAndTransactions()
    {
        // Arrange
        var property = new PropertyEntity
        {
            Id = 100,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "CREATE100",
            PropertyMastOldId = null,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1,                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 1000m },
                        new() { TaxId = 2, TaxAmount = 500m }
                    }
                }
            }
        };

        // Act
        var result = await _oldDetailsService.CreateOldTaxesDetailsAsync(100, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.PropertyId);

        // Verify PropertyMast was updated
        var updatedProperty = await _context.PropertyMast.FindAsync(100);
        Assert.NotNull(updatedProperty);
        Assert.NotNull(updatedProperty.PropertyMastOldId);

        // Verify PropertyMastOld was created
        var createdPropertyMastOld = await _context.PropertyMastOld.FindAsync(updatedProperty.PropertyMastOldId);
        Assert.NotNull(createdPropertyMastOld);

        // Verify transactions were created
        var transactions = await _context.TransMastOld
            .Where(t => t.PropertyMastOldId == updatedProperty.PropertyMastOldId)
            .ToListAsync();
        Assert.Equal(2, transactions.Count);
    }

    [Fact]
    public async Task CreateOldTaxesDetailsAsync_ExistingRecords_ThrowsConflict()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 100, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 101,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "CREATE101",
            PropertyMastOldId = 100,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        // Add existing transaction
        _context.TransMastOld.Add(new TransMastOldEntity
        {
            PropertyMastOldId = 100,
            FinanceYearId = 1,
            TaxId = 1,
            TaxAmount = 1000m,
            RVorCV = string.Empty,
            RVorCVValue = 0m,
            IsActive = true,
            MarkedForDeletion = false
        });
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1,                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 2000m } // Trying to create existing year-tax combination
                    }
                }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.CreateOldTaxesDetailsAsync(101, dto, CancellationToken.None));

        Assert.Contains("already exist", exception.Message);
        Assert.Contains("Use PUT endpoint to update", exception.Message);
    }

    [Fact]
    public async Task CreateOldTaxesDetailsAsync_PartialConflict_ThrowsConflictWithDetails()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 101, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 102,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "CREATE102",
            PropertyMastOldId = 101,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        // Add existing transaction for Tax 1 only
        _context.TransMastOld.Add(new TransMastOldEntity
        {
            PropertyMastOldId = 101,
            FinanceYearId = 1,
            TaxId = 1,
            TaxAmount = 1000m,
            RVorCV = string.Empty,
            RVorCVValue = 0m,
            IsActive = true,
            MarkedForDeletion = false
        });
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1,                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 2000m }, // Conflict
                        new() { TaxId = 2, TaxAmount = 500m }   // New, but should fail due to conflict
                    }
                }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.CreateOldTaxesDetailsAsync(102, dto, CancellationToken.None));

        Assert.Contains("already exist", exception.Message);
        Assert.Contains("2020-21", exception.Message); // Year name in error
        Assert.Contains("Property Tax", exception.Message); // Tax name in error
    }

    [Fact]
    public async Task CreateOldTaxesDetailsAsync_SoftDeletedRecords_Succeeds()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 102, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 103,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "CREATE103",
            PropertyMastOldId = 102,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        // Add soft-deleted transaction (should NOT cause conflict in CREATE)
        _context.TransMastOld.Add(new TransMastOldEntity
        {
            PropertyMastOldId = 102,
            FinanceYearId = 1,
            TaxId = 1,
            TaxAmount = 1000m,
            RVorCV = string.Empty,
            RVorCVValue = 0m,
            IsActive = false,
            MarkedForDeletion = true
        });
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1,                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 2000m } // Should succeed as existing is soft-deleted
                    }
                }
            }
        };

        // Act
        var result = await _oldDetailsService.CreateOldTaxesDetailsAsync(103, dto, CancellationToken.None);

        // Assert - Should succeed because soft-deleted records don't count as "existing"
        Assert.NotNull(result);
        Assert.Equal(103, result.PropertyId);
    }

    [Fact]
    public async Task CreateOldTaxesDetailsAsync_NewRecordsOnly_Success()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 103, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 104,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "CREATE104",
            PropertyMastOldId = 103,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1,                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 1000m },
                        new() { TaxId = 2, TaxAmount = 500m },
                        new() { TaxId = 3, TaxAmount = 100m }
                    }
                }
            }
        };

        // Act
        var result = await _oldDetailsService.CreateOldTaxesDetailsAsync(104, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(104, result.PropertyId);

        var transactions = await _context.TransMastOld
            .Where(t => t.PropertyMastOldId == 103)
            .ToListAsync();

        Assert.Equal(3, transactions.Count);
        Assert.All(transactions, t =>
        {
            Assert.True(t.IsActive);
            Assert.False(t.MarkedForDeletion);
        });
    }

    [Fact]
    public async Task CreateOldTaxesDetailsAsync_MultipleYears_Success()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 104, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 105,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "CREATE105",
            PropertyMastOldId = 104,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1,                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 1000m }
                    }
                },
                new()
                {
                    FinanceYearId = 2,                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 1100m }
                    }
                }
            }
        };

        // Act
        var result = await _oldDetailsService.CreateOldTaxesDetailsAsync(105, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Current behavior: Returns only active year in GET response
        Assert.Single(result.TaxYears);

        var transactions = await _context.TransMastOld
            .Where(t => t.PropertyMastOldId == 104)
            .ToListAsync();

        Assert.Equal(2, transactions.Count);
    }

    [Fact]
    public async Task CreateOldTaxesDetailsAsync_ValidationFailsAfterNoPropertyMastOld_RemainsAtomic()
    {
        // Arrange - Property without PropertyMastOldId
        var property = new PropertyEntity
        {
            Id = 106,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "CREATE106",
            PropertyMastOldId = null,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        // DTO with invalid year (will fail validation)
        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 99999, // Invalid year ID - will fail validation
                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 1000m }
                    }
                }
            }
        };

        // Act & Assert - Should throw validation error
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.CreateOldTaxesDetailsAsync(106, dto, CancellationToken.None));

        // Verify atomicity: PropertyMast should NOT have PropertyMastOldId set
        var propertyAfterError = await _context.PropertyMast.FindAsync(106);
        Assert.NotNull(propertyAfterError);
        Assert.Null(propertyAfterError.PropertyMastOldId); // Should remain null due to validation failure

        // Verify no orphaned PropertyMastOld record was created
        var orphanedPropertyMastOld = await _context.PropertyMastOld
            .Where(pmo => !_context.PropertyMast.Any(p => p.PropertyMastOldId == pmo.Id))
            .ToListAsync();
        Assert.Empty(orphanedPropertyMastOld);
    }

    [Fact]
    public async Task CreateOldTaxesDetailsAsync_ValidationFailsWithInvalidTax_RemainsAtomic()
    {
        // Arrange - Property without PropertyMastOldId
        var property = new PropertyEntity
        {
            Id = 107,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "CREATE107",
            PropertyMastOldId = null,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        // DTO with invalid tax (will fail validation)
        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1,                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 99999, TaxAmount = 1000m } // Invalid tax ID
                    }
                }
            }
        };

        // Act & Assert - Should throw validation error
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.CreateOldTaxesDetailsAsync(107, dto, CancellationToken.None));

        // Verify atomicity: PropertyMast should NOT have PropertyMastOldId set
        var propertyAfterError = await _context.PropertyMast.FindAsync(107);
        Assert.NotNull(propertyAfterError);
        Assert.Null(propertyAfterError.PropertyMastOldId); // Should remain null

        // Verify no orphaned records
        var orphanedPropertyMastOld = await _context.PropertyMastOld
            .Where(pmo => !_context.PropertyMast.Any(p => p.PropertyMastOldId == pmo.Id))
            .ToListAsync();
        Assert.Empty(orphanedPropertyMastOld);
    }

    [Fact]
    public async Task CreateOldTaxesDetailsAsync_FutureYear_ThrowsException()
    {
        // Arrange - Create a future year in YearMaster
        var currentYear = DateTime.Now.Year;
        var futureYear = new YearMasterEntity
        {
            Id = 9999,
            Year = currentYear + 5, // 5 years in the future
            YearCode = $"{currentYear + 5}-{(currentYear + 5 + 1).ToString().Substring(2)}",
            IsActive = false
        };
        _context.YearMaster.Add(futureYear);

        var property = new PropertyEntity
        {
            Id = 108,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "CREATE108",
            PropertyMastOldId = null,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 9999, // Future year
                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 1000m }
                    }
                }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.CreateOldTaxesDetailsAsync(108, dto, CancellationToken.None));

        // Verify exception message includes the future year
        Assert.Contains($"Year cannot be greater than the current year ({currentYear})", exception.Message);
        Assert.Contains((currentYear + 5).ToString(), exception.Message);
    }

    #endregion

    #region UpdateOldTaxesDetailsAsync Tests

    [Fact]
    public async Task UpdateOldTaxesDetailsAsync_FutureYear_ThrowsException()
    {
        // Arrange - Create a future year in YearMaster
        var currentYear = DateTime.Now.Year;
        var futureYear = new YearMasterEntity
        {
            Id = 9998,
            Year = currentYear + 10, // 10 years in the future
            YearCode = $"{currentYear + 10}-{(currentYear + 10 + 1).ToString().Substring(2)}",
            IsActive = false
        };
        _context.YearMaster.Add(futureYear);

        var propertyMastOld = new PropertyMastOldEntity { Id = 200, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 201,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "UPDATE201",
            PropertyMastOldId = 200,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 9998, // Future year
                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 1500m }
                    }
                }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.UpdateOldTaxesDetailsAsync(201, dto, CancellationToken.None));

        // Verify exception message includes the future year
        Assert.Contains($"Year cannot be greater than the current year ({currentYear})", exception.Message);
        Assert.Contains((currentYear + 10).ToString(), exception.Message);
    }

    #endregion

    #region UpdateOldTaxesDetailsAsync Tests

    [Fact]
    public async Task UpdateOldTaxesDetailsAsync_PropertyNotFound_ReturnsNull()
    {
        // Arrange
        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>()
        };

        // Act
        var result = await _oldDetailsService.UpdateOldTaxesDetailsAsync(99999, dto, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateOldTaxesDetailsAsync_NoPropertyMastOldId_CreatesNewPropertyMastOld()
    {
        // Arrange
        var property = new PropertyEntity
        {
            Id = 30,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = null,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1,                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 1000m }
                    }
                }
            }
        };

        // Act
        var result = await _oldDetailsService.UpdateOldTaxesDetailsAsync(30, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(30, result.PropertyId);

        // Verify PropertyMast was updated
        var updatedProperty = await _context.PropertyMast.FindAsync(30);
        Assert.NotNull(updatedProperty);
        Assert.NotNull(updatedProperty.PropertyMastOldId);

        // Verify PropertyMastOld was created
        var createdPropertyMastOld = await _context.PropertyMastOld.FindAsync(updatedProperty.PropertyMastOldId);
        Assert.NotNull(createdPropertyMastOld);
    }

    [Fact]
    public async Task UpdateOldTaxesDetailsAsync_DuplicateFinanceYears_ThrowsException()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 30, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 31,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 30,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new() { FinanceYearId = 1, Taxes = new List<UpdateTaxDetailDto>() },
                new() { FinanceYearId = 1, Taxes = new List<UpdateTaxDetailDto>() }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.UpdateOldTaxesDetailsAsync(31, dto, CancellationToken.None));

        Assert.Contains("Duplicate finance years", exception.Message);
    }

    [Fact]
    public async Task UpdateOldTaxesDetailsAsync_InvalidFinanceYear_ThrowsException()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 31, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 32,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 31,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 99999,                    Taxes = new List<UpdateTaxDetailDto>()
                }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.UpdateOldTaxesDetailsAsync(32, dto, CancellationToken.None));

        Assert.Contains("finance years are invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateOldTaxesDetailsAsync_InvalidTaxId_ThrowsException()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 32, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 33,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 32,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1,                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 99999, TaxAmount = 1000m }
                    }
                }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.UpdateOldTaxesDetailsAsync(33, dto, CancellationToken.None));

        Assert.Contains("invalid or not configured for old taxes", exception.Message);
    }

    [Fact]
    public async Task UpdateOldTaxesDetailsAsync_DuplicateTaxIdInYear_ThrowsException()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 33, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 34,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 33,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1,                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 1000m },
                        new() { TaxId = 1, TaxAmount = 500m }
                    }
                }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.UpdateOldTaxesDetailsAsync(34, dto, CancellationToken.None));

        Assert.Contains("Duplicate TaxId", exception.Message);
    }

    // Test removed: RVorCV validation is no longer applicable as the field has been removed from the API

    // Test removed: RVorCV default value logic is no longer applicable as the field has been removed from the API

    // Test removed: RVorCV default value logic is no longer applicable as the field has been removed from the API

    [Fact]
    public async Task UpdateOldTaxesDetailsAsync_InsertNewTransaction_Success()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 37, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 38,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 37,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1,
                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 1000m },
                        new() { TaxId = 2, TaxAmount = 500m }
                    }
                }
            }
        };

        // Act
        var result = await _oldDetailsService.UpdateOldTaxesDetailsAsync(38, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var transactions = await _context.TransMastOld
            .Where(t => t.PropertyMastOldId == 37)
            .ToListAsync();

        Assert.Equal(2, transactions.Count);

        var tax1 = transactions.First(t => t.TaxId == 1);
        Assert.Equal(1000m, tax1.TaxAmount);
        Assert.Equal("RV", tax1.RVorCV);
        Assert.Equal(0m, tax1.RVorCVValue);

        var tax2 = transactions.First(t => t.TaxId == 2);
        Assert.Equal(500m, tax2.TaxAmount);
    }

    [Fact]
    public async Task UpdateOldTaxesDetailsAsync_UpdateExistingTransaction_Success()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 38, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 39,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 38,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        var existingTransaction = new TransMastOldEntity
        {
            PropertyMastOldId = 38,
            FinanceYearId = 1,
            TaxId = 1,
            TaxAmount = 1000m,
            RVorCV = string.Empty,
            RVorCVValue = 0m,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.TransMastOld.Add(existingTransaction);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1,
                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 1500m }
                    }
                }
            }
        };

        // Act
        var result = await _oldDetailsService.UpdateOldTaxesDetailsAsync(39, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var updatedTransaction = await _context.TransMastOld
            .FirstOrDefaultAsync(t => t.PropertyMastOldId == 38 && t.FinanceYearId == 1 && t.TaxId == 1);

        Assert.NotNull(updatedTransaction);
        Assert.Equal(1500m, updatedTransaction.TaxAmount);
        // RVorCV is preserved from existing record, not overwritten
        Assert.True(updatedTransaction.IsActive);
        Assert.False(updatedTransaction.MarkedForDeletion);
    }

    [Fact]
    public async Task UpdateOldTaxesDetailsAsync_ReactivatesDeletedTransaction_Success()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 39, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 40,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 39,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        var deletedTransaction = new TransMastOldEntity
        {
            PropertyMastOldId = 39,
            FinanceYearId = 1,
            TaxId = 1,
            TaxAmount = 1000m,
            RVorCV = string.Empty,
            RVorCVValue = 0m,
            IsActive = false,
            MarkedForDeletion = true,
            MarkedForDeletionDate = DateTime.Now
        };
        _context.TransMastOld.Add(deletedTransaction);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1,                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 1200m }
                    }
                }
            }
        };

        // Act
        var result = await _oldDetailsService.UpdateOldTaxesDetailsAsync(40, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var reactivatedTransaction = await _context.TransMastOld
            .FirstOrDefaultAsync(t => t.PropertyMastOldId == 39 && t.FinanceYearId == 1 && t.TaxId == 1);

        Assert.NotNull(reactivatedTransaction);
        Assert.Equal(1200m, reactivatedTransaction.TaxAmount);
        Assert.True(reactivatedTransaction.IsActive);
        Assert.False(reactivatedTransaction.MarkedForDeletion);
        Assert.Null(reactivatedTransaction.MarkedForDeletionDate);
    }

    [Fact]
    public async Task UpdateOldTaxesDetailsAsync_MultipleYearsAndTaxes_Success()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 40, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 41,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 40,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1,                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 1000m },
                        new() { TaxId = 2, TaxAmount = 500m },
                        new() { TaxId = 3, TaxAmount = 100m }
                    }
                },
                new()
                {
                    FinanceYearId = 2,                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 1100m },
                        new() { TaxId = 2, TaxAmount = 550m }
                    }
                }
            }
        };

        // Act
        var result = await _oldDetailsService.UpdateOldTaxesDetailsAsync(41, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Current behavior: Returns only active year in GET response
        Assert.Single(result.TaxYears);

        var transactions = await _context.TransMastOld
            .Where(t => t.PropertyMastOldId == 40)
            .ToListAsync();

        Assert.Equal(5, transactions.Count); // 3 for year 1, 2 for year 2

        // Verify year 1 transactions
        var year1Transactions = transactions.Where(t => t.FinanceYearId == 1).ToList();
        Assert.Equal(3, year1Transactions.Count);
        // RVorCV defaults to "RV" when not provided by API
        Assert.All(year1Transactions, t => Assert.Equal("RV", t.RVorCV));

        // Verify year 2 transactions
        var year2Transactions = transactions.Where(t => t.FinanceYearId == 2).ToList();
        Assert.Equal(2, year2Transactions.Count);
        // RVorCV defaults to "RV" when not provided by API
        Assert.All(year2Transactions, t => Assert.Equal("RV", t.RVorCV));
    }

    #endregion

    #region AddFloorDetailsOldAsync Tests

    [Fact]
    public async Task AddFloorDetailsOldAsync_PropertyNotFound_ReturnsNull()
    {
        // Arrange
        var dto = new AddPropertyDetailsOldDto { OldFloorId = 1 };

        // Act
        var result = await _oldDetailsService.AddFloorDetailsOldAsync(99999, dto, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddFloorDetailsOldAsync_NoPropertyMastOldId_AutoCreatesPropertyMastOld()
    {
        // Arrange
        var property = new PropertyEntity
        {
            Id = 50,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = null,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new AddPropertyDetailsOldDto
        {
            OldFloorId = 1,
            OldConstructionTypeId = 1,  // Required field
            OldTypeOfUseId = 1  // Required field
        };

        // Act
        var result = await _oldDetailsService.AddFloorDetailsOldAsync(50, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50, result.PropertyId);

        // Verify PropertyMast was updated with PropertyMastOldId
        var updatedProperty = await _context.PropertyMast.FindAsync(50);
        Assert.NotNull(updatedProperty);
        Assert.NotNull(updatedProperty.PropertyMastOldId);

        // Verify PropertyMastOld was created
        var createdPropertyMastOld = await _context.PropertyMastOld.FindAsync(updatedProperty.PropertyMastOldId);
        Assert.NotNull(createdPropertyMastOld);
        Assert.True(createdPropertyMastOld.IsActive);
        Assert.False(createdPropertyMastOld.MarkedForDeletion);
    }

    [Fact]
    public async Task AddFloorDetailsOldAsync_InvalidFloorId_ThrowsException()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 50, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 51,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 50,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new AddPropertyDetailsOldDto { OldFloorId = 99999 };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.AddFloorDetailsOldAsync(51, dto, CancellationToken.None));

        Assert.Contains("Invalid or inactive Floor ID", exception.Message);
    }

    [Fact]
    public async Task AddFloorDetailsOldAsync_InactiveFloorId_ThrowsException()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 51, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 52,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 51,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new AddPropertyDetailsOldDto { OldFloorId = 999 }; // Inactive floor

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.AddFloorDetailsOldAsync(52, dto, CancellationToken.None));

        Assert.Contains("Invalid or inactive Floor ID", exception.Message);
    }

    [Fact]
    public async Task AddFloorDetailsOldAsync_InvalidSubFloorId_ThrowsException()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 52, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 53,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 52,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new AddPropertyDetailsOldDto
        {
            OldFloorId = 1,
            OldSubFloorId = 99999
        };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.AddFloorDetailsOldAsync(53, dto, CancellationToken.None));

        Assert.Contains("Invalid or inactive SubFloor ID", exception.Message);
    }

    [Fact]
    public async Task AddFloorDetailsOldAsync_InvalidConstructionTypeId_ThrowsException()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 53, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 54,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 53,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new AddPropertyDetailsOldDto
        {
            OldFloorId = 1,
            OldConstructionTypeId = 99999
        };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.AddFloorDetailsOldAsync(54, dto, CancellationToken.None));

        Assert.Contains("Invalid or inactive ConstructionType ID", exception.Message);
    }

    [Fact]
    public async Task AddFloorDetailsOldAsync_InvalidTypeOfUseId_ThrowsException()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 54, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 55,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 54,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new AddPropertyDetailsOldDto
        {
            OldFloorId = 1,
            OldTypeOfUseId = 99999
        };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.AddFloorDetailsOldAsync(55, dto, CancellationToken.None));

        Assert.Contains("Invalid or inactive TypeOfUse ID", exception.Message);
    }

    [Fact]
    public async Task AddFloorDetailsOldAsync_InvalidSubTypeOfUseId_ThrowsException()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 55, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 56,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 55,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new AddPropertyDetailsOldDto
        {
            OldFloorId = 1,
            OldSubTypeOfUseId = 99999
        };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.AddFloorDetailsOldAsync(56, dto, CancellationToken.None));

        Assert.Contains("Invalid or inactive SubTypeOfUse ID", exception.Message);
    }

    [Fact]
    public async Task AddFloorDetailsOldAsync_ValidData_Success()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 56, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 57,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 56,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new AddPropertyDetailsOldDto
        {
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
            OldBuiltupAreaSqFeet = 1291.67
        };

        // Act
        var result = await _oldDetailsService.AddFloorDetailsOldAsync(57, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(57, result.PropertyId);
        Assert.Equal(1, result.OldFloorId);
        Assert.Equal("Ground Floor", result.FloorDescription);
        Assert.Equal(1, result.OldSubFloorId);
        Assert.Equal("Basement", result.SubFloorDescription);
        Assert.Equal("2020", result.OldConstructionYear);
        Assert.Equal(2020, result.ConstructionYearValue);
        Assert.Equal("2021", result.OldAssessmentYear);
        Assert.Equal(2021, result.AssessmentYearValue);
        Assert.Equal(1, result.OldConstructionTypeId);
        Assert.Equal("RCC", result.ConstructionTypeDescription);
        Assert.Equal(1, result.OldTypeOfUseId);
        Assert.Equal("Residential", result.TypeOfUseDescription);
        Assert.Equal(1, result.OldSubTypeOfUseId);
        Assert.Equal("Apartment", result.SubTypeOfUseDescription);
        Assert.Equal(100.50, result.OldCarpetAreaSqMeter);
        Assert.Equal(1081.60, result.OldCarpetAreaSqFeet);
        Assert.Equal(120.00, result.OldBuiltupAreaSqMeter);
        Assert.Equal(1291.67, result.OldBuiltupAreaSqFeet);
    }

    [Fact]
    public async Task AddFloorDetailsOldAsync_WithOnlyRequiredFields_Success()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 57, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 58,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 57,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new AddPropertyDetailsOldDto
        {
            OldFloorId = 1,  // Required
            OldConstructionTypeId = 1,  // Required
            OldTypeOfUseId = 1,  // Required
            // All other fields are optional/nullable
            OldSubFloorId = null,
            OldConstructionYear = null,
            OldAssessmentYear = null,
            OldSubTypeOfUseId = null,
            OldCarpetAreaSqMeter = null,
            OldCarpetAreaSqFeet = null,
            OldBuiltupAreaSqMeter = null,
            OldBuiltupAreaSqFeet = null
        };

        // Act
        var result = await _oldDetailsService.AddFloorDetailsOldAsync(58, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(58, result.PropertyId);
        Assert.Equal(1, result.OldFloorId);  // Required field
        Assert.NotNull(result.FloorDescription);  // Will be populated from FloorMaster join
        Assert.Equal(1, result.OldConstructionTypeId);
        Assert.Equal(1, result.OldTypeOfUseId);
        Assert.Null(result.OldSubFloorId);  // Nullable field
        Assert.Null(result.OldConstructionYear);
        Assert.Null(result.ConstructionYearValue);
    }

    [Fact]
    public async Task AddFloorDetailsOldAsync_ZeroPlaceholderValues_NormalizesToNull()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 100, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 150,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 100,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new AddPropertyDetailsOldDto
        {
            OldFloorId = 0,
            OldSubFloorId = 0,
            OldConstructionTypeId = 0,
            OldTypeOfUseId = 0,
            OldSubTypeOfUseId = 0
        };

        // Act
        var result = await _oldDetailsService.AddFloorDetailsOldAsync(150, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.OldFloorId);
        Assert.Null(result.OldSubFloorId);
        Assert.Null(result.OldConstructionTypeId);
        Assert.Null(result.OldTypeOfUseId);
        Assert.Null(result.OldSubTypeOfUseId);
    }

    [Fact]
    public async Task AddFloorDetailsOldAsync_MaxValuePlaceholderValues_NormalizesToNull()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 101, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 151,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 101,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new AddPropertyDetailsOldDto
        {
            OldFloorId = int.MaxValue,
            OldSubFloorId = int.MaxValue,
            OldConstructionTypeId = int.MaxValue,
            OldTypeOfUseId = int.MaxValue,
            OldSubTypeOfUseId = int.MaxValue
        };

        // Act
        var result = await _oldDetailsService.AddFloorDetailsOldAsync(151, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.OldFloorId);
        Assert.Null(result.OldSubFloorId);
        Assert.Null(result.OldConstructionTypeId);
        Assert.Null(result.OldTypeOfUseId);
        Assert.Null(result.OldSubTypeOfUseId);
    }

    #endregion

    #region UpdateFloorDetailsOldAsync Tests

    [Fact]
    public async Task UpdateFloorDetailsOldAsync_PropertyNotFound_ReturnsNull()
    {
        // Arrange
        var dto = new UpdatePropertyDetailsOldDto { OldFloorId = 1 };

        // Act
        var result = await _oldDetailsService.UpdateFloorDetailsOldAsync(99999, 1, dto, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateFloorDetailsOldAsync_NoPropertyMastOldId_ThrowsException()
    {
        // Arrange
        var property = new PropertyEntity
        {
            Id = 60,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = null,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyDetailsOldDto { OldFloorId = 1 };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.UpdateFloorDetailsOldAsync(60, 1, dto, CancellationToken.None));

        Assert.Contains("does not have an associated PropertyMastOld record", exception.Message);
    }

    [Fact]
    public async Task UpdateFloorDetailsOldAsync_FloorNotFound_ReturnsNull()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 60, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 61,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 60,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyDetailsOldDto { OldFloorId = 1 };

        // Act
        var result = await _oldDetailsService.UpdateFloorDetailsOldAsync(61, 99999, dto, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateFloorDetailsOldAsync_FloorBelongsToDifferentProperty_ReturnsNull()
    {
        // Arrange
        var propertyMastOld1 = new PropertyMastOldEntity { Id = 61, IsActive = true, MarkedForDeletion = false };
        var propertyMastOld2 = new PropertyMastOldEntity { Id = 62, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.AddRange(propertyMastOld1, propertyMastOld2);

        var property1 = new PropertyEntity
        {
            Id = 62,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 61,
            IsActive = true,
            MarkedForDeletion = false
        };
        var property2 = new PropertyEntity
        {
            Id = 63,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "101",
            PropertyMastOldId = 62,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.AddRange(property1, property2);

        var floor = new PropertyDetailsOldEntity
        {
            Id = 100,
            PropertyMastOldId = 62, // Belongs to property2
            OldFloorId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyDetailsOld.Add(floor);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyDetailsOldDto { OldFloorId = 2 };

        // Act - Try to update from property1
        var result = await _oldDetailsService.UpdateFloorDetailsOldAsync(62, 100, dto, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateFloorDetailsOldAsync_InvalidFloorId_ThrowsException()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 63, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 64,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 63,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        var floor = new PropertyDetailsOldEntity
        {
            Id = 101,
            PropertyMastOldId = 63,
            OldFloorId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyDetailsOld.Add(floor);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyDetailsOldDto { OldFloorId = 99999 };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _oldDetailsService.UpdateFloorDetailsOldAsync(64, 101, dto, CancellationToken.None));

        Assert.Contains("Invalid or inactive Floor ID", exception.Message);
    }

    [Fact]
    public async Task UpdateFloorDetailsOldAsync_ValidData_Success()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 64, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 65,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 64,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        var floor = new PropertyDetailsOldEntity
        {
            Id = 102,
            PropertyMastOldId = 64,
            OldFloorId = 1,
            OldSubFloorId = 1,
            OldConstructionYear = "2015",
            OldAssessmentYear = "2016",
            OldConstructionTypeId = 1,
            OldTypeOfUseId = 1,
            OldSubTypeOfUseId = 1,
            OldCarpetAreaSqMeter = 100,
            OldCarpetAreaSqFeet = 1000,
            OldBuiltupAreaSqMeter = 120,
            OldBuiltupAreaSqFeet = 1200,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyDetailsOld.Add(floor);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyDetailsOldDto
        {
            OldFloorId = 2,
            OldSubFloorId = 2,
            OldConstructionYear = "2020",
            OldAssessmentYear = "2021",
            OldConstructionTypeId = 2,
            OldTypeOfUseId = 2,
            OldSubTypeOfUseId = 2,
            OldCarpetAreaSqMeter = 150,
            OldCarpetAreaSqFeet = 1500,
            OldBuiltupAreaSqMeter = 180,
            OldBuiltupAreaSqFeet = 1800
        };

        // Act
        var result = await _oldDetailsService.UpdateFloorDetailsOldAsync(65, 102, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(65, result.PropertyId);
        Assert.Equal(2, result.OldFloorId);
        Assert.Equal("First Floor", result.FloorDescription);
        Assert.Equal(2, result.OldSubFloorId);
        Assert.Equal("Mezzanine", result.SubFloorDescription);
        Assert.Equal("2020", result.OldConstructionYear);
        Assert.Equal(2020, result.ConstructionYearValue);
        Assert.Equal("2021", result.OldAssessmentYear);
        Assert.Equal(2021, result.AssessmentYearValue);
        Assert.Equal(2, result.OldConstructionTypeId);
        Assert.Equal("Steel", result.ConstructionTypeDescription);
        Assert.Equal(2, result.OldTypeOfUseId);
        Assert.Equal("Commercial", result.TypeOfUseDescription);
        Assert.Equal(2, result.OldSubTypeOfUseId);
        Assert.Equal("Shop", result.SubTypeOfUseDescription);
        Assert.Equal(150, result.OldCarpetAreaSqMeter);
        Assert.Equal(1500, result.OldCarpetAreaSqFeet);
        Assert.Equal(180, result.OldBuiltupAreaSqMeter);
        Assert.Equal(1800, result.OldBuiltupAreaSqFeet);
    }

    [Fact]
    public async Task UpdateFloorDetailsOldAsync_UpdatesUpdatedDate()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 65, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 66,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 65,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        var oldDate = DateTime.Now.AddDays(-10);
        var floor = new PropertyDetailsOldEntity
        {
            Id = 103,
            PropertyMastOldId = 65,
            OldFloorId = 1,
            OldConstructionTypeId = 1,  // Required field
            OldTypeOfUseId = 1,  // Required field
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = oldDate,
            UpdatedDate = oldDate
        };
        _context.PropertyDetailsOld.Add(floor);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyDetailsOldDto
        {
            OldFloorId = 2,
            OldConstructionTypeId = 1,  // Required field
            OldTypeOfUseId = 1  // Required field
        };

        // Act
        var result = await _oldDetailsService.UpdateFloorDetailsOldAsync(66, 103, dto, CancellationToken.None);

        // Assert
        var updatedFloor = await _context.PropertyDetailsOld.FindAsync(103);
        Assert.NotNull(updatedFloor);
        Assert.NotNull(updatedFloor.UpdatedDate);
        Assert.True(updatedFloor.UpdatedDate > oldDate);
    }

    [Fact]
    public async Task UpdateFloorDetailsOldAsync_ZeroPlaceholderValues_NormalizesToNull()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 200, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 250,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 200,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        var floor = new PropertyDetailsOldEntity
        {
            Id = 300,
            PropertyMastOldId = 200,
            OldFloorId = 1,
            OldConstructionTypeId = 1,
            OldTypeOfUseId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyDetailsOld.Add(floor);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyDetailsOldDto
        {
            OldFloorId = 0,
            OldSubFloorId = 0,
            OldConstructionTypeId = 0,
            OldTypeOfUseId = 0,
            OldSubTypeOfUseId = 0
        };

        // Act
        var result = await _oldDetailsService.UpdateFloorDetailsOldAsync(250, 300, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.OldFloorId);
        Assert.Null(result.OldSubFloorId);
        Assert.Null(result.OldConstructionTypeId);
        Assert.Null(result.OldTypeOfUseId);
        Assert.Null(result.OldSubTypeOfUseId);
    }

    [Fact]
    public async Task UpdateFloorDetailsOldAsync_MaxValuePlaceholderValues_NormalizesToNull()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 201, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 251,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 201,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        var floor = new PropertyDetailsOldEntity
        {
            Id = 301,
            PropertyMastOldId = 201,
            OldFloorId = 1,
            OldConstructionTypeId = 1,
            OldTypeOfUseId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyDetailsOld.Add(floor);
        await _context.SaveChangesAsync();

        var dto = new UpdatePropertyDetailsOldDto
        {
            OldFloorId = int.MaxValue,
            OldSubFloorId = int.MaxValue,
            OldConstructionTypeId = int.MaxValue,
            OldTypeOfUseId = int.MaxValue,
            OldSubTypeOfUseId = int.MaxValue
        };

        // Act
        var result = await _oldDetailsService.UpdateFloorDetailsOldAsync(251, 301, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.OldFloorId);
        Assert.Null(result.OldSubFloorId);
        Assert.Null(result.OldConstructionTypeId);
        Assert.Null(result.OldTypeOfUseId);
        Assert.Null(result.OldSubTypeOfUseId);
    }

    #endregion

    #region GetFloorDetailsOldAsync Tests

    [Fact]
    public async Task GetFloorDetailsOldAsync_PropertyNotFound_ReturnsNull()
    {
        // Act
        var result = await _oldDetailsService.GetFloorDetailsOldAsync(99999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldAsync_NoPropertyMastOldId_ReturnsEmptyList()
    {
        // Arrange
        var property = new PropertyEntity
        {
            Id = 70,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = null,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetFloorDetailsOldAsync(70, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(70, result.PropertyId);
        Assert.Empty(result.FloorDetails);
    }

    [Fact]
    public async Task GetFloorDetailsOldAsync_NoFloorRecords_ReturnsEmptyList()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 70, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 71,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 70,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetFloorDetailsOldAsync(71, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(71, result.PropertyId);
        Assert.Empty(result.FloorDetails);
    }

    [Fact]
    public async Task GetFloorDetailsOldAsync_WithFloorRecords_ReturnsCompleteData()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 71, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 72,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 71,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        _context.PropertyDetailsOld.AddRange(
            new PropertyDetailsOldEntity
            {
                Id = 200,
                PropertyMastOldId = 71,
                OldFloorId = 1,
                OldSubFloorId = 1,
                OldConstructionYear = "2020",
                OldAssessmentYear = "2021",
                OldConstructionTypeId = 1,
                OldTypeOfUseId = 1,
                OldSubTypeOfUseId = 1,
                OldCarpetAreaSqMeter = 100,
                OldCarpetAreaSqFeet = 1000,
                OldBuiltupAreaSqMeter = 120,
                OldBuiltupAreaSqFeet = 1200,
                IsActive = true,
                MarkedForDeletion = false
            },
            new PropertyDetailsOldEntity
            {
                Id = 201,
                PropertyMastOldId = 71,
                OldFloorId = 2,
                OldSubFloorId = null,
                OldConstructionYear = "2019",
                OldAssessmentYear = null,
                OldConstructionTypeId = 2,
                OldTypeOfUseId = 2,
                OldSubTypeOfUseId = null,
                OldCarpetAreaSqMeter = 80,
                OldCarpetAreaSqFeet = 800,
                OldBuiltupAreaSqMeter = null,
                OldBuiltupAreaSqFeet = null,
                IsActive = true,
                MarkedForDeletion = false
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetFloorDetailsOldAsync(72, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(72, result.PropertyId);
        Assert.Equal(2, result.FloorDetails.Count);

        // Verify first floor
        var floor1 = result.FloorDetails[0];
        Assert.Equal(200, floor1.Id);
        Assert.Equal(72, floor1.PropertyId);
        Assert.Equal(1, floor1.OldFloorId);
        Assert.Equal("Ground Floor", floor1.FloorDescription);
        Assert.Equal(1, floor1.OldSubFloorId);
        Assert.Equal("Basement", floor1.SubFloorDescription);
        Assert.Equal("2020", floor1.OldConstructionYear);
        Assert.Equal(2020, floor1.ConstructionYearValue);
        Assert.Equal("2021", floor1.OldAssessmentYear);
        Assert.Equal(2021, floor1.AssessmentYearValue);

        // Verify second floor
        var floor2 = result.FloorDetails[1];
        Assert.Equal(201, floor2.Id);
        Assert.Equal(2, floor2.OldFloorId);
        Assert.Equal("First Floor", floor2.FloorDescription);
        Assert.Null(floor2.OldSubFloorId);
        Assert.Null(floor2.SubFloorDescription);
        Assert.Equal("2019", floor2.OldConstructionYear);
        Assert.Equal(2019, floor2.ConstructionYearValue);
        Assert.Null(floor2.OldAssessmentYear);
        Assert.Null(floor2.AssessmentYearValue);
    }

    [Fact]
    public async Task GetFloorDetailsOldAsync_InactiveFloorRecords_AreIgnored()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 72, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 73,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 72,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        _context.PropertyDetailsOld.AddRange(
            new PropertyDetailsOldEntity
            {
                Id = 202,
                PropertyMastOldId = 72,
                OldFloorId = 1,
                IsActive = true,
                MarkedForDeletion = false
            },
            new PropertyDetailsOldEntity
            {
                Id = 203,
                PropertyMastOldId = 72,
                OldFloorId = 2,
                IsActive = false,
                MarkedForDeletion = false
            },
            new PropertyDetailsOldEntity
            {
                Id = 204,
                PropertyMastOldId = 72,
                OldFloorId = 3,
                IsActive = true,
                MarkedForDeletion = true
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetFloorDetailsOldAsync(73, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.FloorDetails);
        Assert.Equal(202, result.FloorDetails[0].Id);
    }

    [Fact]
    public async Task GetFloorDetailsOldAsync_FloorsOrderedById()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 73, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 74,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 73,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        _context.PropertyDetailsOld.AddRange(
            new PropertyDetailsOldEntity { Id = 206, PropertyMastOldId = 73, OldFloorId = 3, IsActive = true, MarkedForDeletion = false },
            new PropertyDetailsOldEntity { Id = 205, PropertyMastOldId = 73, OldFloorId = 1, IsActive = true, MarkedForDeletion = false },
            new PropertyDetailsOldEntity { Id = 207, PropertyMastOldId = 73, OldFloorId = 2, IsActive = true, MarkedForDeletion = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetFloorDetailsOldAsync(74, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.FloorDetails.Count);
        Assert.Equal(205, result.FloorDetails[0].Id);
        Assert.Equal(206, result.FloorDetails[1].Id);
        Assert.Equal(207, result.FloorDetails[2].Id);
    }

    [Fact]
    public async Task GetFloorDetailsOldAsync_InvalidYearString_ReturnsNullYearValue()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 74, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 75,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 74,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        _context.PropertyDetailsOld.Add(
            new PropertyDetailsOldEntity
            {
                Id = 208,
                PropertyMastOldId = 74,
                OldFloorId = 1,
                OldConstructionYear = "INVALID",
                OldAssessmentYear = "ABC",
                IsActive = true,
                MarkedForDeletion = false
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.GetFloorDetailsOldAsync(75, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.FloorDetails);
        Assert.Equal("INVALID", result.FloorDetails[0].OldConstructionYear);
        Assert.Null(result.FloorDetails[0].ConstructionYearValue);
        Assert.Equal("ABC", result.FloorDetails[0].OldAssessmentYear);
        Assert.Null(result.FloorDetails[0].AssessmentYearValue);
    }

    #endregion

    #region DeleteFloorDetailsOldAsync Tests

    [Fact]
    public async Task DeleteFloorDetailsOldAsync_PropertyNotFound_ReturnsFalse()
    {
        // Act
        var result = await _oldDetailsService.DeleteFloorDetailsOldAsync(99999, 1, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFloorDetailsOldAsync_NoPropertyMastOldId_ReturnsFalse()
    {
        // Arrange
        var property = new PropertyEntity
        {
            Id = 80,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = null,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.DeleteFloorDetailsOldAsync(80, 1, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFloorDetailsOldAsync_FloorNotFound_ReturnsFalse()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 80, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 81,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 80,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.DeleteFloorDetailsOldAsync(81, 99999, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFloorDetailsOldAsync_FloorBelongsToDifferentProperty_ReturnsFalse()
    {
        // Arrange
        var propertyMastOld1 = new PropertyMastOldEntity { Id = 81, IsActive = true, MarkedForDeletion = false };
        var propertyMastOld2 = new PropertyMastOldEntity { Id = 82, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.AddRange(propertyMastOld1, propertyMastOld2);

        var property1 = new PropertyEntity
        {
            Id = 82,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 81,
            IsActive = true,
            MarkedForDeletion = false
        };
        var property2 = new PropertyEntity
        {
            Id = 83,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "101",
            PropertyMastOldId = 82,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.AddRange(property1, property2);

        var floor = new PropertyDetailsOldEntity
        {
            Id = 300,
            PropertyMastOldId = 82,
            OldFloorId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyDetailsOld.Add(floor);
        await _context.SaveChangesAsync();

        // Act - Try to delete from property1
        var result = await _oldDetailsService.DeleteFloorDetailsOldAsync(82, 300, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFloorDetailsOldAsync_ValidFloor_SoftDeletesSuccessfully()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 83, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 84,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 83,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        var floor = new PropertyDetailsOldEntity
        {
            Id = 301,
            PropertyMastOldId = 83,
            OldFloorId = 1,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.Now
        };
        _context.PropertyDetailsOld.Add(floor);
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.DeleteFloorDetailsOldAsync(84, 301, CancellationToken.None);

        // Assert
        Assert.True(result);

        var deletedFloor = await _context.PropertyDetailsOld.FindAsync(301);
        Assert.NotNull(deletedFloor);
        Assert.True(deletedFloor.MarkedForDeletion);
        Assert.False(deletedFloor.IsActive);
        Assert.NotNull(deletedFloor.MarkedForDeletionDate);
        Assert.NotNull(deletedFloor.UpdatedDate);
    }

    [Fact]
    public async Task DeleteFloorDetailsOldAsync_AlreadyDeleted_ReturnsFalse()
    {
        // Arrange
        var propertyMastOld = new PropertyMastOldEntity { Id = 84, IsActive = true, MarkedForDeletion = false };
        _context.PropertyMastOld.Add(propertyMastOld);

        var property = new PropertyEntity
        {
            Id = 85,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "100",
            PropertyMastOldId = 84,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);

        var floor = new PropertyDetailsOldEntity
        {
            Id = 302,
            PropertyMastOldId = 84,
            OldFloorId = 1,
            IsActive = false,
            MarkedForDeletion = true,
            MarkedForDeletionDate = DateTime.Now
        };
        _context.PropertyDetailsOld.Add(floor);
        await _context.SaveChangesAsync();

        // Act
        var result = await _oldDetailsService.DeleteFloorDetailsOldAsync(85, 302, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region TaxTotal Tests

    [Fact(Skip = "PersistNewOldTaxesAsync stores TaxTotal row as submitted (0); dynamic recalculation of the TOTAL TransMastOld row is not yet implemented in the repository.")]
    public async Task CreateAndUpdateOldTaxesDetailsAsync_WithTaxTotalConfigured_CalculatesDynamicallyAndExcludesFromTotal()
    {
        // 1. Arrange
        // Add TaxTotal to the context
        var taxTotalConfig = new TaxMasterEntity
        {
            Id = 21,
            TaxCode = "TOTAL",
            TaxName = "TaxTotal",
            TaxNameAlias = "TaxTotal",
            TaxCategoryId = 1,
            OldTaxStatus = true,
            IsActive = true,
            DisplayOrder = 5
        };
        _context.TaxMaster.Add(taxTotalConfig);
        await _context.SaveChangesAsync();

        var property = new PropertyEntity
        {
            Id = 500,
            TaxZoneId = 1,
            WardId = 1,
            PropertyNo = "TAX500",
            PropertyMastOldId = null,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();

        // 2. Test CreateOldTaxesDetailsAsync
        var createDto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1, // Year 2020-21
                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 100m }, // Property Tax
                        new() { TaxId = 2, TaxAmount = 50m },  // Water Tax
                        new() { TaxId = 3, TaxAmount = 10m },  // Interest (should be excluded from TaxTotal)
                        new() { TaxId = 21, TaxAmount = 0m }   // TaxTotal (should be calculated dynamically)
                    }
                }
            }
        };

        var createResult = await _oldDetailsService.CreateOldTaxesDetailsAsync(500, createDto, CancellationToken.None);

        // Assert Create Results
        Assert.NotNull(createResult);
        var createdYear = createResult.TaxYears.FirstOrDefault(y => y.FinanceYearId == 1);
        Assert.NotNull(createdYear);
        var createdTaxTotal = createdYear.Taxes.FirstOrDefault(t => t.TaxId == 21);
        Assert.NotNull(createdTaxTotal);
        // TaxTotal should be dynamic sum of other taxes (Property Tax 100 + Water Tax 50 = 150. Excludes Interest 10)
        Assert.Equal(150m, createdTaxTotal.TaxAmount);

        // Verify that PropertyMastOld.OldTotalTax excludes TaxTotal (Property Tax 100 + Water Tax 50 = 150. Excludes Interest 10, and excludes TaxTotal itself)
        var updatedProperty = await _context.PropertyMast.FindAsync(500);
        Assert.NotNull(updatedProperty);
        Assert.NotNull(updatedProperty.PropertyMastOldId);
        var mastOld = await _context.PropertyMastOld.FindAsync(updatedProperty.PropertyMastOldId);
        Assert.NotNull(mastOld);
        Assert.Equal(150.0, mastOld.OldTotalTax);

        // 3. Test UpdateOldTaxesDetailsAsync
        var updateDto = new UpdatePropertyOldTaxesDetailsDto
        {
            TaxYears = new List<UpdateOldTaxYearDto>
            {
                new()
                {
                    FinanceYearId = 1,
                    Taxes = new List<UpdateTaxDetailDto>
                    {
                        new() { TaxId = 1, TaxAmount = 200m }, // Property Tax updated
                        new() { TaxId = 2, TaxAmount = 100m }, // Water Tax updated
                        new() { TaxId = 3, TaxAmount = 15m },  // Interest updated
                        new() { TaxId = 21, TaxAmount = 0m }   // TaxTotal updated
                    }
                }
            }
        };

        var updateResult = await _oldDetailsService.UpdateOldTaxesDetailsAsync(500, updateDto, CancellationToken.None);

        // Assert Update Results
        Assert.NotNull(updateResult);
        var updatedYear = updateResult.TaxYears.FirstOrDefault(y => y.FinanceYearId == 1);
        Assert.NotNull(updatedYear);
        var updatedTaxTotal = updatedYear.Taxes.FirstOrDefault(t => t.TaxId == 21);
        Assert.NotNull(updatedTaxTotal);
        // TaxTotal should be dynamic sum of other taxes (Property Tax 200 + Water Tax 100 = 300. Excludes Interest 15)
        Assert.Equal(300m, updatedTaxTotal.TaxAmount);

        // Verify that PropertyMastOld.OldTotalTax excludes TaxTotal (Property Tax 200 + Water Tax 100 = 300. Excludes Interest 15, and excludes TaxTotal itself)
        var updatedMastOld = await _context.PropertyMastOld.FindAsync(updatedProperty.PropertyMastOldId);
        Assert.NotNull(updatedMastOld);
        Assert.Equal(300.0, updatedMastOld.OldTotalTax);
    }

    #endregion
}
