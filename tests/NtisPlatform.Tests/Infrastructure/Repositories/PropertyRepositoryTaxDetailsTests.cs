using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Application.Interfaces;
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
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var rv1 = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastEntity { Id = 2, PropertyId = 1, TaxId = 2, FinanceYearId = 1, TaxAmount = 500m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(tax1, tax2);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(rv1, rv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var rvActive = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };
        var rvInactive = new TransMastEntity { Id = 2, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, CalculationType = "RV", IsActive = false, MarkedForDeletion = false };
        var rvDeleted = new TransMastEntity { Id = 3, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 200m, CalculationType = "RV", IsActive = true, MarkedForDeletion = true };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(rvActive, rvInactive, rvDeleted);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var rv1 = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(rv1, rv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var rvActive = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };
        var rvInactive = new TransMastEntity { Id = 2, PropertyId = 1, TaxId = 2, FinanceYearId = 1, TaxAmount = 500m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(taxActive, taxInactive);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(rvActive, rvInactive);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var rv1 = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.AddRange(propertyType1, propertyType2);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(rv1, rv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var rv1 = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.AddRange(propertyTypeActive, propertyTypeInactive);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(rv1, rv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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
        var rv = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMast.Add(rv);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var rv1 = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(rv1, rv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var rv1 = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };
        var rv3 = new TransMastEntity { Id = 3, PropertyId = 3, TaxId = 1, FinanceYearId = 1, TaxAmount = 300m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2, property3);
        context.Set<WingEntity>().Add(wing);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(rv1, rv2, rv3);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
        
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

        var rv1 = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 1000m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };
        var rv2 = new TransMastEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };
        var rv3 = new TransMastEntity { Id = 3, PropertyId = 3, TaxId = 1, FinanceYearId = 1, TaxAmount = 300m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2, property3);
        context.Set<WingEntity>().Add(wing);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(rv1, rv2, rv3);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
        
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
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var cv1 = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastEntity { Id = 2, PropertyId = 1, TaxId = 2, FinanceYearId = 1, TaxAmount = 750m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(tax1, tax2);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(cv1, cv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var cvActive = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };
        var cvInactive = new TransMastEntity { Id = 2, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, CalculationType = "CV", IsActive = false, MarkedForDeletion = false };
        var cvDeleted = new TransMastEntity { Id = 3, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 200m, CalculationType = "CV", IsActive = true, MarkedForDeletion = true };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax1);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(cvActive, cvInactive, cvDeleted);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var cv1 = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 500m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(cv1, cv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var cvActive = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };
        var cvInactive = new TransMastEntity { Id = 2, PropertyId = 1, TaxId = 2, FinanceYearId = 1, TaxAmount = 500m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(taxActive, taxInactive);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(cvActive, cvInactive);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var cv1 = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 750m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.AddRange(propertyType1, propertyType2);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(cv1, cv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var cv1 = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 750m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.AddRange(propertyTypeActive, propertyTypeInactive);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(cv1, cv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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
        var cv = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMast.Add(cv);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var cv1 = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 750m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(cv1, cv2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
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

        var cv1 = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 750m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };
        var cv3 = new TransMastEntity { Id = 3, PropertyId = 3, TaxId = 1, FinanceYearId = 1, TaxAmount = 300m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2, property3);
        context.Set<WingEntity>().Add(wing);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(cv1, cv2, cv3);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
        
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

        var cv1 = new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 1, TaxAmount = 2000m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };
        var cv2 = new TransMastEntity { Id = 2, PropertyId = 2, TaxId = 1, FinanceYearId = 1, TaxAmount = 750m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };
        var cv3 = new TransMastEntity { Id = 3, PropertyId = 3, TaxId = 1, FinanceYearId = 1, TaxAmount = 300m, CalculationType = "CV", IsActive = true, MarkedForDeletion = false };

        context.PropertyTypeMasters.Add(propertyType);
        context.PropertyMast.AddRange(property1, property2, property3);
        context.Set<WingEntity>().Add(wing);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year);
        context.TransMast.AddRange(cv1, cv2, cv3);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
        
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
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

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

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

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

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetTaxDetailsAsync(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_CurrentYearTransMastExists_ReturnsTransMastAmountNotRawPolicyAmount()
    {
        // A certificate save (CC/OC/Electric Bill) runs OccupationTaxApplicationService, which
        // writes its prorated/retrospective-adjusted amount into TransMast for the CURRENT
        // finance year -- NOT back into PolicyTaxDetails. Before this fix, GetTaxDetailsAsync only
        // ever read the raw annual PolicyTaxDetails amount, so the Tax Details UI panel never
        // reflected a certificate date change even though the pipeline ran successfully.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity { Id = 1, WardId = 1, TaxZoneId = 1, IsActive = true, MarkedForDeletion = false };
        var categoryTax = new TaxCategoryMasterEntity { Id = 1, CategoryCode = "TAX", CategoryName = "Property Tax", IsActive = true };
        var tax = new TaxMasterEntity { Id = 1, TaxName = "General Tax", TaxCode = "GEN", DisplayOrder = 1, TaxCategoryId = 1, IsActive = true };
        var year2026 = new YearMasterEntity { Id = 10, Year = 2026, YearCode = "2026-27", IsActive = true };

        var nettaxPolicy = new PolicyCodeMasterEntity { Id = 1, PolicyCode = "NETTAX", PolicyName = "Net Tax", PolicyType = "NORMAL", IsActive = true };

        // Raw annual RV amount (what the RV engine computed, unchanged by occupation timing).
        var policyTax = new PolicyTaxDetailsEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCodeId = 1,
            TaxId = 1,
            TaxAmount = 10_000m,
            IsActive = true,
            MarkedForDeletion = false
        };

        // OccupationTaxApplicationService's prorated/adjusted amount for the current finance year.
        var transMast = new TransMastEntity
        {
            Id = 1,
            PropertyId = 1,
            TaxId = 1,
            FinanceYearId = 10,
            CalculationType = "RV",
            TaxAmount = 4_110m,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        context.TaxCategoryMaster.Add(categoryTax);
        context.TaxMaster.Add(tax);
        context.YearMaster.Add(year2026);
        context.PolicyCodeMaster.Add(nettaxPolicy);
        context.PolicyTaxDetails.Add(policyTax);
        context.TransMast.Add(transMast);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        var result = await repository.GetTaxDetailsAsync(1);

        Assert.NotNull(result);
        var policy = Assert.Single(result!.Policies);
        var taxAmount = Assert.Single(policy.TaxAmounts);
        Assert.Equal("General Tax", taxAmount.TaxName);
        Assert.Equal(4_110m, taxAmount.TaxAmount); // TransMast override, not the raw 10,000 PolicyTaxDetails amount
        Assert.Equal(4_110m, policy.TaxTotal);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_TaxPendingDetailsRetroExists_AttachesPendingYearsToOcGroupOnly()
    {
        // Year-wise retro/arrears breakdown from TaxPendingDetailsRetro should surface as
        // PolicyTaxDetail.PendingYears on the certificate-tax family group (OC/CC/Electric-Bill)
        // it belongs to -- purely additive display data, never on the unrelated NETTAX group, and
        // never by reintroducing those retro years into the main current-year TaxAmounts.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity { Id = 1, WardId = 1, TaxZoneId = 1, IsActive = true, MarkedForDeletion = false };
        var categoryTax = new TaxCategoryMasterEntity { Id = 1, CategoryCode = "TAX", CategoryName = "Property Tax", IsActive = true };
        var tax = new TaxMasterEntity { Id = 1, TaxName = "General Tax", TaxCode = "GEN", DisplayOrder = 1, TaxCategoryId = 1, IsActive = true };
        var year2024 = new YearMasterEntity { Id = 8, Year = 2024, YearCode = "2024-25", IsActive = true };
        var year2025 = new YearMasterEntity { Id = 9, Year = 2025, YearCode = "2025-26", IsActive = true };
        var year2026 = new YearMasterEntity { Id = 10, Year = 2026, YearCode = "2026-27", IsActive = true };

        var nettaxPolicy = new PolicyCodeMasterEntity { Id = 1, PolicyCode = "NETTAX", PolicyName = "Net Tax", PolicyType = "NORMAL", IsActive = true };
        var ocPolicy = new PolicyCodeMasterEntity { Id = 2, PolicyCode = "PARTIAL_OC", PolicyName = "Partial OC", PolicyType = "NORMAL", IsActive = true };

        var nettaxRow = new PolicyTaxDetailsEntity { Id = 1, PropertyId = 1, PolicyCodeId = 1, TaxId = 1, TaxAmount = 10_000m, IsActive = true, MarkedForDeletion = false };
        var ocRow = new PolicyTaxDetailsEntity { Id = 2, PropertyId = 1, PolicyCodeId = 2, TaxId = 1, TaxAmount = 4_110m, IsActive = true, MarkedForDeletion = false };

        var retro2024 = new TaxPendingDetailsRetroEntity { PropertyId = 1, PendingYearId = 8, TaxId = 1, PendingAmount = 3_540m, IsActive = true, MarkedForDeletion = false };
        var retro2025 = new TaxPendingDetailsRetroEntity { PropertyId = 1, PendingYearId = 9, TaxId = 1, PendingAmount = 3_560m, IsActive = true, MarkedForDeletion = false };

        context.PropertyMast.Add(property);
        context.TaxCategoryMaster.Add(categoryTax);
        context.TaxMaster.Add(tax);
        context.YearMaster.AddRange(year2024, year2025, year2026);
        context.PolicyCodeMaster.AddRange(nettaxPolicy, ocPolicy);
        context.PolicyTaxDetails.AddRange(nettaxRow, ocRow);
        context.TaxPendingDetailsRetro.AddRange(retro2024, retro2025);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        var result = await repository.GetTaxDetailsAsync(1);

        Assert.NotNull(result);
        var ocGroup = result!.Policies.Single(p => p.PolicyCode == "PARTIAL_OC");
        var nettaxGroup = result.Policies.Single(p => p.PolicyCode == "NETTAX");

        Assert.Empty(nettaxGroup.PendingYears); // unrelated group never gets pending years attached

        Assert.Equal(2, ocGroup.PendingYears.Count);
        Assert.Equal("2024-25", ocGroup.PendingYears[0].YearCode);
        Assert.Equal(3_540m, ocGroup.PendingYears[0].TaxTotal);
        Assert.Equal("2025-26", ocGroup.PendingYears[1].YearCode);
        Assert.Equal(3_560m, ocGroup.PendingYears[1].TaxTotal);

        // Current-year TaxAmounts are untouched by this -- still just the 4,110 current figure.
        Assert.Equal(4_110m, ocGroup.TaxTotal);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_NoTransMastForCurrentYear_FallsBackToRawPolicyAmount()
    {
        // The certificate-change pipeline has never run for this property (no TransMast rows at
        // all) -- must fall back to the raw PolicyTaxDetails amount unchanged, not show zero/null.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity { Id = 1, WardId = 1, TaxZoneId = 1, IsActive = true, MarkedForDeletion = false };
        var categoryTax = new TaxCategoryMasterEntity { Id = 1, CategoryCode = "TAX", CategoryName = "Property Tax", IsActive = true };
        var tax = new TaxMasterEntity { Id = 1, TaxName = "General Tax", TaxCode = "GEN", DisplayOrder = 1, TaxCategoryId = 1, IsActive = true };

        var nettaxPolicy = new PolicyCodeMasterEntity { Id = 1, PolicyCode = "NETTAX", PolicyName = "Net Tax", PolicyType = "NORMAL", IsActive = true };

        var policyTax = new PolicyTaxDetailsEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCodeId = 1,
            TaxId = 1,
            TaxAmount = 10_000m,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        context.TaxCategoryMaster.Add(categoryTax);
        context.TaxMaster.Add(tax);
        context.PolicyCodeMaster.Add(nettaxPolicy);
        context.PolicyTaxDetails.Add(policyTax);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        var result = await repository.GetTaxDetailsAsync(1);

        Assert.NotNull(result);
        var policy = Assert.Single(result!.Policies);
        var taxAmount = Assert.Single(policy.TaxAmounts);
        Assert.Equal(10_000m, taxAmount.TaxAmount);
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

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

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

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

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

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

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

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

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

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetTaxDetailsCVAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Policies);
        Assert.Equal(2000.00m, result.Policies[0].TaxAmounts.First(t => t.TaxName == "Capital Value Tax").TaxAmount);
    }

    #endregion
}
