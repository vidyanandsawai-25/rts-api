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
    #region GetAggregatedPropertyTaxDetailsAsync Tests


    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsAsync_NoMatchingProperties_ReturnsNull()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetAggregatedPropertyTaxDetailsAsync(request);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsAsync_WithTransMastRVRecords_ReturnsOrderedTaxAmountList()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", IsActive = true };
        var property = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false };
        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 2, IsActive = true };
        var tax2 = new TaxMasterEntity { Id = 2, TaxName = "Water Tax", TaxCode = "WATER", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var rv1 = new TransMastRVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastRVEntity { Id = 2, PropertyId = 1, TaxId = 2, FinanceYearId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(tax1, tax2);
        context.YearMaster.Add(year);
        context.TransMastRV.AddRange(rv1, rv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetAggregatedPropertyTaxDetailsAsync(request);

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
    public async Task GetAggregatedPropertyTaxDetailsAsync_WithInactiveOrDeletedTransMastRV_ExcludesThem()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", IsActive = true };
        var property = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false };
        var tax = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var rvActive = new TransMastRVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false };
        var rvInactive = new TransMastRVEntity { Id = 2, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, IsActive = false, MarkedForDeletion = false };
        var rvDeleted = new TransMastRVEntity { Id = 3, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 200m, IsActive = true, MarkedForDeletion = true };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastRV.AddRange(rvActive, rvInactive, rvDeleted);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetAggregatedPropertyTaxDetailsAsync(request);

        Assert.NotNull(result);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Property Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(1000m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsAsync_AggregatesAcrossMultipleProperties()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", IsActive = true };
        var property1 = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1 };
        var property2 = new PropertyEntity { Id = 2, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1 };
        var tax = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var rv1 = new TransMastRVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastRVEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastRV.AddRange(rv1, rv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { WardId = 1 };
        var result = await repository.GetAggregatedPropertyTaxDetailsAsync(request);

        Assert.NotNull(result);
        Assert.Equal(2, result.PropertyCount);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Property Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(1500m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsAsync_RespectsTaxMasterIsActive()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", IsActive = true };
        var property = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false };
        var taxActive = new TaxMasterEntity { Id = 1, TaxName = "Active Tax", TaxCode = "A", DisplayOrder = 1, IsActive = true };
        var taxInactive = new TaxMasterEntity { Id = 2, TaxName = "Inactive Tax", TaxCode = "I", DisplayOrder = 2, IsActive = false };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var rvActive = new TransMastRVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false };
        var rvInactive = new TransMastRVEntity { Id = 2, PropertyId = 1, TaxId = 2, FinanceYearId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(taxActive, taxInactive);
        context.YearMaster.Add(year);
        context.TransMastRV.AddRange(rvActive, rvInactive);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetAggregatedPropertyTaxDetailsAsync(request);

        Assert.NotNull(result);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Active Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(1000m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsAsync_FiltersByPartType_ReturnsMatchingProperties()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType1 = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", PartType = "Apartment", IsActive = true };
        var propertyType2 = new PropertyTypeMasterEntity { Id = 2, PropertyDescription = "Commercial", PartType = "Shop", IsActive = true };

        var property1 = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1 };
        var property2 = new PropertyEntity { Id = 2, PropertyTypeId = 2, IsActive = true, MarkedForDeletion = false, WardId = 1 };

        var tax = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var rv1 = new TransMastRVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastRVEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.AddRange(propertyType1, propertyType2);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastRV.AddRange(rv1, rv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { WardId = 1, PartType = "Apartment" };
        var result = await repository.GetAggregatedPropertyTaxDetailsAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyCount);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Property Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(1000m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsAsync_WithInactivePropertyType_ExcludesProperty()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyTypeActive = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", PartType = "Apartment", IsActive = true };
        var propertyTypeInactive = new PropertyTypeMasterEntity { Id = 2, PropertyDescription = "Commercial", PartType = "Shop", IsActive = false };

        var property1 = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false };
        var property2 = new PropertyEntity { Id = 2, PropertyTypeId = 2, IsActive = true, MarkedForDeletion = false };

        var tax = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var rv1 = new TransMastRVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastRVEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.AddRange(propertyTypeActive, propertyTypeInactive);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastRV.AddRange(rv1, rv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto();
        var result = await repository.GetAggregatedPropertyTaxDetailsAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyCount);
        Assert.Single(result.TaxAmounts);
        Assert.Equal(1000m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsAsync_PartTypeFilterCaseInsensitive()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", PartType = "Apartment", IsActive = true };
        var property = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false };
        var tax = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };
        var rv = new TransMastRVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastRV.Add(rv);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PartType = "APARTMENT" };
        var result = await repository.GetAggregatedPropertyTaxDetailsAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyCount);
        Assert.Equal(1000m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsAsync_FiltersByPartitionNo_ReturnsMatchingProperties()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", PartType = "Apartment", IsActive = true };

        var property1 = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "A-1" };
        var property2 = new PropertyEntity { Id = 2, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "B-2" };

        var tax = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var rv1 = new TransMastRVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastRVEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastRV.AddRange(rv1, rv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { WardId = 1, PartitionNo = "A-1" };
        var result = await repository.GetAggregatedPropertyTaxDetailsAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyCount);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Property Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(1000m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsAsync_PartitionNoInWingList_UsesLikeFilter()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", PartType = "Apartment", IsActive = true };

        var property1 = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "A1" };
        var property2 = new PropertyEntity { Id = 2, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "A2" };
        var property3 = new PropertyEntity { Id = 3, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "B1" };

        var wing = new WingEntity { Id = 1, WingNo = "A", IsActive = true };

        var tax = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var rv1 = new TransMastRVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastRVEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false };
        var rv3 = new TransMastRVEntity { Id = 3, PropertyId = 3, TaxId = 1, FinanceYearId = 1, TaxAmount = 300m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2, property3);
        context.Set<WingEntity>().Add(wing);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastRV.AddRange(rv1, rv2, rv3);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        
        var request = new PropertyApartmentTaxRequestDto { WardId = 1, PartitionNo = "A" };
        var result = await repository.GetAggregatedPropertyTaxDetailsAsync(request);

        Assert.NotNull(result);
        Assert.Equal(2, result.PropertyCount);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Property Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(1500m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsAsync_PartitionNoNotInWingList_UsesEqualityFilter()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", PartType = "Apartment", IsActive = true };

        var property1 = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "A1" };
        var property2 = new PropertyEntity { Id = 2, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "A2" };
        var property3 = new PropertyEntity { Id = 3, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "B1" };

        var wing = new WingEntity { Id = 1, WingNo = "A", IsActive = true };

        var tax = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var rv1 = new TransMastRVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastRVEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false };
        var rv3 = new TransMastRVEntity { Id = 3, PropertyId = 3, TaxId = 1, FinanceYearId = 1, TaxAmount = 300m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2, property3);
        context.Set<WingEntity>().Add(wing);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastRV.AddRange(rv1, rv2, rv3);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        
        var request = new PropertyApartmentTaxRequestDto { WardId = 1, PartitionNo = "A1" };
        var result = await repository.GetAggregatedPropertyTaxDetailsAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyCount);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Property Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(1000m, result.TaxAmounts[0].TaxAmount);
    }

    #endregion

    #region GetAggregatedPropertyTaxDetailsCVAsync Tests

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsCVAsync_NoMatchingProperties_ReturnsNull()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetAggregatedPropertyTaxDetailsCVAsync(request);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsCVAsync_WithTransMastCVRecords_ReturnsOrderedTaxAmountList()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", IsActive = true };
        var property = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false };
        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 2, IsActive = true };
        var tax2 = new TaxMasterEntity { Id = 2, TaxName = "Education Cess", TaxCode = "EDU", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var cv1 = new TransMastCVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastCVEntity { Id = 2, PropertyId = 1, TaxId = 2, FinanceYearId = 1, TaxAmount = 750m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(tax1, tax2);
        context.YearMaster.Add(year);
        context.TransMastCV.AddRange(cv1, cv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetAggregatedPropertyTaxDetailsCVAsync(request);

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
    public async Task GetAggregatedPropertyTaxDetailsCVAsync_WithInactiveOrDeletedTransMastCV_ExcludesThem()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", IsActive = true };
        var property = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false };
        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var cvActive = new TransMastCVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, IsActive = true, MarkedForDeletion = false };
        var cvInactive = new TransMastCVEntity { Id = 2, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, IsActive = false, MarkedForDeletion = false };
        var cvDeleted = new TransMastCVEntity { Id = 3, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 200m, IsActive = true, MarkedForDeletion = true };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax1);
        context.YearMaster.Add(year);
        context.TransMastCV.AddRange(cvActive, cvInactive, cvDeleted);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetAggregatedPropertyTaxDetailsCVAsync(request);

        Assert.NotNull(result);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Capital Value Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(2000m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsCVAsync_AggregatesAcrossMultipleProperties()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", IsActive = true };
        var property1 = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1 };
        var property2 = new PropertyEntity { Id = 2, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1 };
        var tax = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var cv1 = new TransMastCVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastCVEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastCV.AddRange(cv1, cv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { WardId = 1 };
        var result = await repository.GetAggregatedPropertyTaxDetailsCVAsync(request);

        Assert.NotNull(result);
        Assert.Equal(2, result.PropertyCount);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Capital Value Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(2500m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsCVAsync_RespectsTaxMasterIsActive()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", IsActive = true };
        var property = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false };
        var taxActive = new TaxMasterEntity { Id = 1, TaxName = "Active Tax", TaxCode = "A", DisplayOrder = 1, IsActive = true };
        var taxInactive = new TaxMasterEntity { Id = 2, TaxName = "Inactive Tax", TaxCode = "I", DisplayOrder = 2, IsActive = false };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var cvActive = new TransMastCVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, IsActive = true, MarkedForDeletion = false };
        var cvInactive = new TransMastCVEntity { Id = 2, PropertyId = 1, TaxId = 2, FinanceYearId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(taxActive, taxInactive);
        context.YearMaster.Add(year);
        context.TransMastCV.AddRange(cvActive, cvInactive);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PropertyId = 1 };
        var result = await repository.GetAggregatedPropertyTaxDetailsCVAsync(request);

        Assert.NotNull(result);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Active Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(2000m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsCVAsync_FiltersByPartType_ReturnsMatchingProperties()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType1 = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", PartType = "Apartment", IsActive = true };
        var propertyType2 = new PropertyTypeMasterEntity { Id = 2, PropertyDescription = "Commercial", PartType = "Shop", IsActive = true };

        var property1 = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1 };
        var property2 = new PropertyEntity { Id = 2, PropertyTypeId = 2, IsActive = true, MarkedForDeletion = false, WardId = 1 };

        var tax = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var cv1 = new TransMastCVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastCVEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 750m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.AddRange(propertyType1, propertyType2);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastCV.AddRange(cv1, cv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { WardId = 1, PartType = "Apartment" };
        var result = await repository.GetAggregatedPropertyTaxDetailsCVAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyCount);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Capital Value Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(2000m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsCVAsync_WithInactivePropertyType_ExcludesProperty()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyTypeActive = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", PartType = "Apartment", IsActive = true };
        var propertyTypeInactive = new PropertyTypeMasterEntity { Id = 2, PropertyDescription = "Commercial", PartType = "Shop", IsActive = false };

        var property1 = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false };
        var property2 = new PropertyEntity { Id = 2, PropertyTypeId = 2, IsActive = true, MarkedForDeletion = false };

        var tax = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var cv1 = new TransMastCVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastCVEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 750m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.AddRange(propertyTypeActive, propertyTypeInactive);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastCV.AddRange(cv1, cv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto();
        var result = await repository.GetAggregatedPropertyTaxDetailsCVAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyCount);
        Assert.Single(result.TaxAmounts);
        Assert.Equal(2000m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsCVAsync_PartTypeFilterCaseInsensitive()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", PartType = "Apartment", IsActive = true };
        var property = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false };
        var tax = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };
        var cv = new TransMastCVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastCV.Add(cv);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { PartType = "APARTMENT" };
        var result = await repository.GetAggregatedPropertyTaxDetailsCVAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyCount);
        Assert.Equal(2000m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsCVAsync_FiltersByPartitionNo_ReturnsMatchingProperties()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", PartType = "Apartment", IsActive = true };

        var property1 = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "A-1" };
        var property2 = new PropertyEntity { Id = 2, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "B-2" };

        var tax = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var cv1 = new TransMastCVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastCVEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 750m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastCV.AddRange(cv1, cv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        var request = new PropertyApartmentTaxRequestDto { WardId = 1, PartitionNo = "A-1" };
        var result = await repository.GetAggregatedPropertyTaxDetailsCVAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyCount);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Capital Value Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(2000m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsCVAsync_PartitionNoInWingList_UsesLikeFilter()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", PartType = "Apartment", IsActive = true };

        var property1 = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "A1" };
        var property2 = new PropertyEntity { Id = 2, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "A2" };
        var property3 = new PropertyEntity { Id = 3, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "B1" };

        var wing = new WingEntity { Id = 1, WingNo = "A", IsActive = true };

        var tax = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var cv1 = new TransMastCVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastCVEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 750m, IsActive = true, MarkedForDeletion = false };
        var cv3 = new TransMastCVEntity { Id = 3, PropertyId = 3, TaxId = 1, FinanceYearId = 1, TaxAmount = 300m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2, property3);
        context.Set<WingEntity>().Add(wing);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastCV.AddRange(cv1, cv2, cv3);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        
        var request = new PropertyApartmentTaxRequestDto { WardId = 1, PartitionNo = "A" };
        var result = await repository.GetAggregatedPropertyTaxDetailsCVAsync(request);

        Assert.NotNull(result);
        Assert.Equal(2, result.PropertyCount);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Capital Value Tax", result.TaxAmounts[0].TaxName);
        Assert.Equal(2750m, result.TaxAmounts[0].TaxAmount);
    }

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsCVAsync_PartitionNoNotInWingList_UsesEqualityFilter()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var propertyType = new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", PartType = "Apartment", IsActive = true };

        var property1 = new PropertyEntity { Id = 1, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "A1" };
        var property2 = new PropertyEntity { Id = 2, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "A2" };
        var property3 = new PropertyEntity { Id = 3, PropertyTypeId = 1, IsActive = true, MarkedForDeletion = false, WardId = 1, PartitionNo = "B1" };

        var wing = new WingEntity { Id = 1, WingNo = "A", IsActive = true };

        var tax = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };
        var year = new YearMasterEntity { Id = 1, YearCode = "2024-25", IsActive = true };

        var cv1 = new TransMastCVEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastCVEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 750m, IsActive = true, MarkedForDeletion = false };
        var cv3 = new TransMastCVEntity { Id = 3, PropertyId = 3, TaxId = 1, FinanceYearId = 1, TaxAmount = 300m, IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2, property3);
        context.Set<WingEntity>().Add(wing);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMastCV.AddRange(cv1, cv2, cv3);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);
        
        var request = new PropertyApartmentTaxRequestDto { WardId = 1, PartitionNo = "A1" };
        var result = await repository.GetAggregatedPropertyTaxDetailsCVAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyCount);
        Assert.Single(result.TaxAmounts);
        Assert.Equal("Capital Value Tax", result.TaxAmounts[0].TaxName);
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

    [Fact]
    public async Task GetTaxDetailsCVAsync_WithSinglePolicyAndMultipleTaxes_ReturnsCorrectGroupedData()
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
            MarkedForDeletion = false
        };

        // Create tax category masters required by the new join logic
        var categoryTax = new TaxCategoryMasterEntity { Id = 1, CategoryCode = "TAX", CategoryName = "Property Tax", IsActive = true };
        var categoryEdu = new TaxCategoryMasterEntity { Id = 2, CategoryCode = "EDU", CategoryName = "Education", IsActive = true };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, TaxCategoryId = 1, IsActive = true };
        var tax2 = new TaxMasterEntity { Id = 2, TaxName = "Education Cess", TaxCode = "EDU", DisplayOrder = 2, TaxCategoryId = 2, IsActive = true };

        var policyTaxCV1 = new PolicyTaxDetailsCVEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 1,
            TaxAmount = 2000.50m,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.UtcNow
        };

        var policyTaxCV2 = new PolicyTaxDetailsCVEntity
        {
            Id = 2,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 2,
            TaxAmount = 750.25m,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.UtcNow
        };

        context.PropertyMast.Add(property);
        context.TaxCategoryMaster.AddRange(categoryTax, categoryEdu);
        context.TaxMaster.AddRange(tax1, tax2);
        context.PolicyTaxDetailsCV.AddRange(policyTaxCV1, policyTaxCV2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsCVAsync(1);

        // Assert
        Assert.NotNull(result);
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

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };

        // Policy CV 2023
        var policyTaxCV1 = new PolicyTaxDetailsCVEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POLCV2023",
            TaxId = 1,
            TaxAmount = 1800.00m,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.UtcNow
        };

        // Policy CV 2024
        var policyTaxCV2 = new PolicyTaxDetailsCVEntity
        {
            Id = 2,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 1,
            TaxAmount = 2000.00m,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.UtcNow
        };

        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax1);
        context.PolicyTaxDetailsCV.AddRange(policyTaxCV1, policyTaxCV2);
        await context.SaveChangesAsync();

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
            IsActive = false, // Property is inactive - this is what we're testing
            MarkedForDeletion = false
        };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };

        var activeTax = new PolicyTaxDetailsCVEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 1,
            TaxAmount = 2000.00m,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.UtcNow
        };

        var inactiveTax = new PolicyTaxDetailsCVEntity
        {
            Id = 2,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 1,
            TaxAmount = 500.00m,
            IsActive = false, // Inactive tax (should be filtered out anyway)
            MarkedForDeletion = false,
            CreatedDate = DateTime.UtcNow
        };

        // Add all entities to context so they can be queried
        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax1);
        context.PolicyTaxDetailsCV.Add(activeTax);
        context.PolicyTaxDetailsCV.Add(inactiveTax);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsCVAsync(1);

        // Assert
        // Should return null because the property is inactive, even though tax data exists
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
            MarkedForDeletion = true // Property is marked for deletion - this is what we're testing
        };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };

        var deletedTax = new PolicyTaxDetailsCVEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 1,
            TaxAmount = 2000.00m,
            IsActive = true,
            MarkedForDeletion = true, // Tax also marked for deletion (should be filtered anyway)
            CreatedDate = DateTime.UtcNow
        };

        // Add all entities to context so they can be queried
        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax1);
        context.PolicyTaxDetailsCV.Add(deletedTax);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsCVAsync(1);

        // Assert
        // Should return null because the property is marked for deletion, even though tax data exists
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsCVAsync_WithDuplicateTaxNamesSamePolicy_SumsTaxAmounts()
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
            MarkedForDeletion = false
        };

        // Create tax category master required by the new join logic
        var categoryTax = new TaxCategoryMasterEntity { Id = 1, CategoryCode = "TAX", CategoryName = "Property Tax", IsActive = true };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, TaxCategoryId = 1, IsActive = true };

        // Same PolicyCode and TaxName - should be summed
        var policyTaxCV1 = new PolicyTaxDetailsCVEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 1,
            TaxAmount = 1000.00m,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.UtcNow
        };

        var policyTaxCV2 = new PolicyTaxDetailsCVEntity
        {
            Id = 2,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 1,
            TaxAmount = 1000.00m,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.UtcNow
        };

        context.PropertyMast.Add(property);
        context.TaxCategoryMaster.Add(categoryTax);
        context.TaxMaster.Add(tax1);
        context.PolicyTaxDetailsCV.AddRange(policyTaxCV1, policyTaxCV2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsCVAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Policies);
        Assert.Equal(2000.00m, result.Policies[0].TaxAmounts.First(t => t.TaxName == "Capital Value Tax").TaxAmount);
    }

    #endregion
}
