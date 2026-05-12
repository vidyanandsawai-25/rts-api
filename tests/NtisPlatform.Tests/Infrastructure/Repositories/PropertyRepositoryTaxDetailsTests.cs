using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

public class PropertyRepositoryTaxDetailsTests
{
    #region GetApartmentPropertyTaxDetailsAsync Tests


    [Fact]
    public async Task GetApartmentPropertyTaxDetailsAsync_NoMatchingProperties_ReturnsNull()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetApartmentPropertyTaxDetailsAsync(request);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsAsync_WithTransMastRVRecords_ReturnsOrderedTaxAmountList()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity { Id = 1, IsActive = true, MarkedForDeletion = false };
        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 2, IsActive = true };
        var tax2 = new TaxMasterEntity { Id = 2, TaxName = "Water Tax", TaxCode = "WATER", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var rv1 = new TransMastRVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastRVEntity { Id = 2, PropertyId = 1, TaxId = 2, FinanceYearId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false };

        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(tax1, tax2);
        context.YearMaster.Add(year);
        context.TransMastRV.AddRange(rv1, rv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetApartmentPropertyTaxDetailsAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyId);
        Assert.Equal(2, result.TaxAmounts.Count);
        Assert.Collection(result.TaxAmounts,
            item =>
            {
                Assert.Equal("Water Tax", item.TaxName);
                Assert.Equal(500m, item.TaxAmount);
                Assert.Equal(1, item.DisplayOrder);
            },
            item =>
            {
                Assert.Equal("Property Tax", item.TaxName);
                Assert.Equal(1000m, item.TaxAmount);
                Assert.Equal(2, item.DisplayOrder);
            });
    }

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsAsync_WithInactiveOrDeletedTransMastRV_ExcludesThem()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity { Id = 1, IsActive = true, MarkedForDeletion = false };
        var tax = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var rvActive = new TransMastRVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false };
        var rvInactive = new TransMastRVEntity { Id = 2, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, IsActive = false, MarkedForDeletion = false };
        var rvDeleted = new TransMastRVEntity { Id = 3, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 200m, IsActive = true, MarkedForDeletion = true };

        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastRV.AddRange(rvActive, rvInactive, rvDeleted);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetApartmentPropertyTaxDetailsAsync(request);

        Assert.NotNull(result);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Property Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(1000m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsAsync_AggregatesAcrossMultipleProperties()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property1 = new PropertyEntity { Id = 1, IsActive = true, MarkedForDeletion = false, WardId = 1 };
        var property2 = new PropertyEntity { Id = 2, IsActive = true, MarkedForDeletion = false, WardId = 1 };
        var tax = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var rv1 = new TransMastRVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastRVEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false };

        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastRV.AddRange(rv1, rv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { WardId = 1 };
        var result = await repository.GetApartmentPropertyTaxDetailsAsync(request);

        Assert.NotNull(result);
        Assert.Equal(2, result.PropertyCount);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Property Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(1500m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsAsync_RespectsTaxMasterIsActive()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity { Id = 1, IsActive = true, MarkedForDeletion = false };
        var taxActive = new TaxMasterEntity { Id = 1, TaxName = "Active Tax", TaxCode = "A", DisplayOrder = 1, IsActive = true };
        var taxInactive = new TaxMasterEntity { Id = 2, TaxName = "Inactive Tax", TaxCode = "I", DisplayOrder = 2, IsActive = false };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var rvActive = new TransMastRVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false };
        var rvInactive = new TransMastRVEntity { Id = 2, PropertyId = 1, TaxId = 2, FinanceYearId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false };

        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(taxActive, taxInactive);
        context.YearMaster.Add(year);
        context.TransMastRV.AddRange(rvActive, rvInactive);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetApartmentPropertyTaxDetailsAsync(request);

        Assert.NotNull(result);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Active Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(1000m, result.TaxAmounts[0].TaxAmount);
    }

    #endregion

    #region GetApartmentPropertyTaxDetailsCVAsync Tests

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsCVAsync_NoMatchingProperties_ReturnsNull()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetApartmentPropertyTaxDetailsCVAsync(request);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsCVAsync_WithTransMastCVRecords_ReturnsOrderedTaxAmountList()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity { Id = 1, IsActive = true, MarkedForDeletion = false };
        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 2, IsActive = true };
        var tax2 = new TaxMasterEntity { Id = 2, TaxName = "Education Cess", TaxCode = "EDU", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var cv1 = new TransMastCVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastCVEntity { Id = 2, PropertyId = 1, TaxId = 2, FinanceYearId = 1, TaxAmount = 750m, IsActive = true, MarkedForDeletion = false };

        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(tax1, tax2);
        context.YearMaster.Add(year);
        context.TransMastCV.AddRange(cv1, cv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetApartmentPropertyTaxDetailsCVAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyId);
        Assert.Equal(2, result.TaxAmounts.Count);
        Assert.Collection(result.TaxAmounts,
            item =>
            {
                Assert.Equal("Education Cess", item.TaxName);
                Assert.Equal(750m, item.TaxAmount);
                Assert.Equal(1, item.DisplayOrder);
            },
            item =>
            {
                Assert.Equal("Capital Value Tax", item.TaxName);
                Assert.Equal(2000m, item.TaxAmount);
                Assert.Equal(2, item.DisplayOrder);
            });
    }

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsCVAsync_WithInactiveOrDeletedTransMastCV_ExcludesThem()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity { Id = 1, IsActive = true, MarkedForDeletion = false };
        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var cvActive = new TransMastCVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, IsActive = true, MarkedForDeletion = false };
        var cvInactive = new TransMastCVEntity { Id = 2, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, IsActive = false, MarkedForDeletion = false };
        var cvDeleted = new TransMastCVEntity { Id = 3, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 200m, IsActive = true, MarkedForDeletion = true };

        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax1);
        context.YearMaster.Add(year);
        context.TransMastCV.AddRange(cvActive, cvInactive, cvDeleted);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetApartmentPropertyTaxDetailsCVAsync(request);

        Assert.NotNull(result);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Capital Value Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(2000m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsCVAsync_AggregatesAcrossMultipleProperties()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property1 = new PropertyEntity { Id = 1, IsActive = true, MarkedForDeletion = false, WardId = 1 };
        var property2 = new PropertyEntity { Id = 2, IsActive = true, MarkedForDeletion = false, WardId = 1 };
        var tax = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var cv1 = new TransMastCVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastCVEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false };

        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastCV.AddRange(cv1, cv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { WardId = 1 };
        var result = await repository.GetApartmentPropertyTaxDetailsCVAsync(request);

        Assert.NotNull(result);
        Assert.Equal(2, result.PropertyCount);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Capital Value Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(2500m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsCVAsync_RespectsTaxMasterIsActive()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity { Id = 1, IsActive = true, MarkedForDeletion = false };
        var taxActive = new TaxMasterEntity { Id = 1, TaxName = "Active Tax", TaxCode = "A", DisplayOrder = 1, IsActive = true };
        var taxInactive = new TaxMasterEntity { Id = 2, TaxName = "Inactive Tax", TaxCode = "I", DisplayOrder = 2, IsActive = false };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var cvActive = new TransMastCVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, IsActive = true, MarkedForDeletion = false };
        var cvInactive = new TransMastCVEntity { Id = 2, PropertyId = 1, TaxId = 2, FinanceYearId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false };

        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(taxActive, taxInactive);
        context.YearMaster.Add(year);
        context.TransMastCV.AddRange(cvActive, cvInactive);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetApartmentPropertyTaxDetailsCVAsync(request);

        Assert.NotNull(result);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Active Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(2000m, result.TaxAmounts[0].TaxAmount);
    }

    #endregion

    #region GetTaxDetailsAsync Tests

    [Fact]
    public async Task GetTaxDetailsAsync_PropertyDoesNotExist_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsAsync(999999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_PropertyIsInactive_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = false, // Inactive
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsAsync(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_PropertyMarkedForDeletion_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = true // Marked for deletion
        };

        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsAsync(1);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetTaxDetailsCVAsync Tests

    [Fact]
    public async Task GetTaxDetailsCVAsync_PropertyDoesNotExist_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsCVAsync(999999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsCVAsync_PropertyIsInactive_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = false, // Inactive
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsCVAsync(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsCVAsync_PropertyMarkedForDeletion_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = true // Marked for deletion
        };

        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsCVAsync(1);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
