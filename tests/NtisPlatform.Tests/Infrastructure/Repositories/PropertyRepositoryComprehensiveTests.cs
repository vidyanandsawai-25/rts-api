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

namespace NtisPlatform.Tests.Infrastructure.Repositories;

/// <summary>
/// Additional comprehensive tests for PropertyRepository to achieve 100% code coverage
/// </summary>
public class PropertyRepositoryComprehensiveTests
{
    /// <summary>
    /// Composes the Basic Details use-case service over the in-memory context (feature repository +
    /// master repository + unit of work) so these ported tests exercise the same behaviour the
    /// repository previously implemented directly.
    /// </summary>
    private static PropertyBasicDetailsService CreateBasicDetailsService(ApplicationDbContext context)
        => new(new PropertyBasicDetailsRepository(context), new MasterRepository(context), new UnitOfWork(context), new PropertyMutationInvariantPolicy());

    private static PropertySocietyService CreateSocietyService(ApplicationDbContext context)
        => new(new PropertySocietyRepository(context), new MasterRepository(context), new UnitOfWork(context), new PropertyMutationInvariantPolicy());

    [Fact]
    public async Task GetBasicDetailsAsync_WithAllMasterData_ReturnsCompleteDto()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using var context = new ApplicationDbContext(options);

        var zone = new ZoneEntity { Id = 1, ZoneNo = "Z1", Description = "Zone 1", IsActive = true };
        var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 1, Description = "Ward 79", IsActive = true };
        var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "Tax Zone 10", IsActive = true };
        var category = new PropertyCategoryEntity { Id = 1, PropertyCategoryName = "Residential", IsActive = true };
        var propertyType = new PropertyTypeMasterEntity { Id = 2, PropertyDescription = "Apartment", IsActive = true };
        var mouja = new MoujaEntity { Id = 3, MoujaName = "Mouja 1", IsActive = true };

        var property = new PropertyEntity
        {
            Id = 549357,
            WardId = 79,
            TaxZoneId = 10,
            CategoryId = 1,
            PropertyTypeId = 2,
            PropertyNo = "22",
            PartitionNo = "1",
            FlatOrShopNo = "101",
            PlotNo = "P123",
            CSN = "CSN456",
            UPICId = "UPIC123",
            SubZoneNo = "SZ01",
            MoujaId = 3,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.ZoneMaster.Add(zone);
        context.WardMaster.Add(ward);
        context.TaxZoneMaster.Add(taxZone);
        context.PropertyCategoryMaster.Add(category);
        context.PropertyTypeMasters.Add(propertyType);
        context.MoujaEntity.Add(mouja);
        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var service = CreateBasicDetailsService(context);
        var result = await service.GetBasicDetailsAsync(549357);

        Assert.NotNull(result);
        Assert.Equal(549357, result.PropertyId);
        Assert.Equal(79, result.WardId);
        Assert.Equal("W79", result.WardNo);
        Assert.Equal(1, result.ZoneId);
        Assert.Equal("Zone 1", result.Division);
        Assert.Equal(10, result.TaxZoneId);
        Assert.Equal("TZ10", result.TaxZoneNo);
        Assert.Equal(1, result.CategoryId);
        Assert.Equal("Residential", result.CategoryName);
        Assert.Equal(2, result.PropertyTypeId);
        Assert.Equal("Apartment", result.PropertyDescription);
        Assert.Equal("22", result.PropertyNo);
        Assert.Equal("1", result.PartitionNo);
        Assert.Equal("101", result.FlatOrShopNo);
        Assert.Equal("P123", result.PlotNo);
        Assert.Equal("CSN456", result.SurveyNo);
        Assert.Equal("UPIC123", result.UPICId);
        Assert.Equal("SZ01", result.SubZoneNo);
        Assert.Equal(3, result.MoujaId);
        Assert.Equal("Mouja 1", result.MoujaName);
    }

    [Fact]
    public async Task GetBasicDetailsAsync_WithMappedOldProperties_ReturnsSummedOldAreas()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 549357,
            IsActive = true,
            MarkedForDeletion = false
        };

        var mapDetail1 = new PropertyMapDetailEntity
        {
            Id = 1,
            PropertyMapId = 1,
            PropertySide = "NEW",
            PropertyNoNew = "22",
            PropertyIdNew = 549357,
            PropertyIdOld = 100,
            IsActive = true,
            IsCurrent = true,
            Status = "ACTIVE",
            CreatedDate = DateTime.UtcNow
        };

        var mapDetail2 = new PropertyMapDetailEntity
        {
            Id = 2,
            PropertyMapId = 1,
            PropertySide = "NEW",
            PropertyNoNew = "22",
            PropertyIdNew = 549357,
            PropertyIdOld = 101,
            IsActive = true,
            IsCurrent = true,
            Status = "ACTIVE",
            CreatedDate = DateTime.UtcNow
        };

        var oldDetails1 = new PropertyDetailsOldEntity
        {
            Id = 10,
            PropertyMastOldId = 100,
            OldCarpetAreaSqFeet = 100,
            OldCarpetAreaSqMeter = 9.29,
            OldBuiltupAreaSqFeet = 120,
            OldBuiltupAreaSqMeter = 11.15,
            IsActive = true,
            MarkedForDeletion = false
        };

        var oldDetails2 = new PropertyDetailsOldEntity
        {
            Id = 11,
            PropertyMastOldId = 101,
            OldCarpetAreaSqFeet = 200,
            OldCarpetAreaSqMeter = 18.58,
            OldBuiltupAreaSqFeet = 240,
            OldBuiltupAreaSqMeter = 22.3,
            IsActive = true,
            MarkedForDeletion = false
        };

        var oldDetailsInactive = new PropertyDetailsOldEntity
        {
            Id = 12,
            PropertyMastOldId = 100,
            OldCarpetAreaSqFeet = 999,
            OldCarpetAreaSqMeter = 999,
            OldBuiltupAreaSqFeet = 999,
            OldBuiltupAreaSqMeter = 999,
            IsActive = false,
            MarkedForDeletion = false
        };

        var oldDetailsDeleted = new PropertyDetailsOldEntity
        {
            Id = 13,
            PropertyMastOldId = 101,
            OldCarpetAreaSqFeet = 999,
            OldCarpetAreaSqMeter = 999,
            OldBuiltupAreaSqFeet = 999,
            OldBuiltupAreaSqMeter = 999,
            IsActive = true,
            MarkedForDeletion = true
        };

        context.PropertyMast.Add(property);
        context.PropertyMapDetails.AddRange(mapDetail1, mapDetail2);
        context.PropertyDetailsOld.Add(oldDetails1);
        context.PropertyDetailsOld.Add(oldDetails2);
        context.PropertyDetailsOld.Add(oldDetailsInactive);
        context.PropertyDetailsOld.Add(oldDetailsDeleted);
        await context.SaveChangesAsync();

        var service = CreateBasicDetailsService(context);
        var result = await service.GetBasicDetailsAsync(549357);

        Assert.NotNull(result);
        Assert.Equal(300, result.OldCarpetAreaSqFeet);
        Assert.Equal(27.87, result.OldCarpetAreaSqMeter);
        Assert.Equal(360, result.OldBuiltupAreaSqFeet);
        Assert.Equal(33.45, result.OldBuiltupAreaSqMeter);
    }

    [Fact]
    public async Task GetBasicDetailsAsync_WithSocietyAndWing_ReturnsWingNoFromWingMaster()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using var context = new ApplicationDbContext(options);

        var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 1, IsActive = true };
        var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
        var wing = new WingEntity { Id = 1, WingNo = "AssessmentWing", IsActive = true };

        var property = new PropertyEntity
        {
            Id = 549357,
            WardId = 79,
            TaxZoneId = 10,
            SocietyDetailId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var society = new SocietyDetailsEntity
        {
            Id = 1,
            PropertyId = 549357,
            WingId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var assessment = new PropertyAssessmentEntity
        {
            Id = 1,
            PropertyId = 549357,
            NoOfResidentialToilets = 2,
            NoOfCommercialToilets = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.WardMaster.Add(ward);
        context.TaxZoneMaster.Add(taxZone);
        context.Set<WingEntity>().Add(wing);
        context.PropertyMast.Add(property);
        context.SocietyDetailsMast.Add(society);
        context.PropertyMastDetails.Add(assessment);
        await context.SaveChangesAsync();

        var service = CreateBasicDetailsService(context);
        var result = await service.GetBasicDetailsAsync(549357);

        Assert.NotNull(result);
        Assert.Equal("AssessmentWing", result.WingNo);
        Assert.Equal(2, result.NoOfResidentialToilets);
        Assert.Equal(1, result.NoOfCommercialToilets);
    }

    [Fact]
    public async Task GetBasicDetailsAsync_WithMultipleConstructionYears_ReturnsEarliestValidYear()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 549357,
            IsActive = true,
            MarkedForDeletion = false
        };

        var detail1 = new PropertyDetailsEntity { Id = 1, PropertyId = 549357, ConstructionYear = "2020", IsActive = true, MarkedForDeletion = false };
        var detail2 = new PropertyDetailsEntity { Id = 2, PropertyId = 549357, ConstructionYear = "2015", IsActive = true, MarkedForDeletion = false };
        var detail3 = new PropertyDetailsEntity { Id = 3, PropertyId = 549357, ConstructionYear = "2018", IsActive = true, MarkedForDeletion = false };
        var detailInvalid = new PropertyDetailsEntity { Id = 4, PropertyId = 549357, ConstructionYear = "Invalid", IsActive = true, MarkedForDeletion = false };
        var detailEmpty = new PropertyDetailsEntity { Id = 5, PropertyId = 549357, ConstructionYear = "", IsActive = true, MarkedForDeletion = false };

        context.PropertyMast.Add(property);
        context.PropertyDetails.AddRange(detail1, detail2, detail3, detailInvalid, detailEmpty);
        await context.SaveChangesAsync();

        var service = CreateBasicDetailsService(context);
        var result = await service.GetBasicDetailsAsync(549357);

        Assert.NotNull(result);
        Assert.Equal("2015", result.ConstructionYear);
    }

    [Fact]
    public async Task UpdateBasicDetailsAsync_WithWingNo_CreatesSocietyAndLinksWingNo()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using var context = new ApplicationDbContext(options);

        var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 1, IsActive = true };
        var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
        var wing = new WingEntity { Id = 1, WingNo = "A", IsActive = true };

        var property = new PropertyEntity
        {
            Id = 549357,
            WardId = 79,
            TaxZoneId = 10,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.WardMaster.Add(ward);
        context.TaxZoneMaster.Add(taxZone);
        context.Set<WingEntity>().Add(wing);
        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var service = CreateBasicDetailsService(context);
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            WingNo = "A"
        };

        var result = await service.UpdateBasicDetailsAsync(549357, dto);

        Assert.NotNull(result);
        Assert.Equal("A", result.WingNo);

        var society = await context.SocietyDetailsMast.FirstOrDefaultAsync(s => s.PropertyId == 549357);
        Assert.NotNull(society);
        Assert.Equal(1, society.WingId);
    }

    [Fact]
    public async Task UpdateBasicDetailsAsync_ExistingSociety_UpdatesWingNoViaSociety()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using var context = new ApplicationDbContext(options);

        var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 1, IsActive = true };
        var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
        var oldWing = new WingEntity { Id = 1, WingNo = "OldWing", IsActive = true };
        var newWing = new WingEntity { Id = 2, WingNo = "UpdatedWing", IsActive = true };

        var property = new PropertyEntity
        {
            Id = 549357,
            WardId = 79,
            TaxZoneId = 10,
            SocietyDetailId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var society = new SocietyDetailsEntity
        {
            Id = 1,
            PropertyId = 549357,
            WingId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.WardMaster.Add(ward);
        context.TaxZoneMaster.Add(taxZone);
        context.Set<WingEntity>().Add(oldWing);
        context.Set<WingEntity>().Add(newWing);
        context.PropertyMast.Add(property);
        context.SocietyDetailsMast.Add(society);
        await context.SaveChangesAsync();

        var service = CreateBasicDetailsService(context);
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            WingNo = "UpdatedWing"
        };

        var result = await service.UpdateBasicDetailsAsync(549357, dto);

        Assert.NotNull(result);
        Assert.Equal("UpdatedWing", result.WingNo);

        var updatedSociety = await context.SocietyDetailsMast.FindAsync(1);
        Assert.NotNull(updatedSociety);
        Assert.Equal(2, updatedSociety.WingId);
    }

    [Fact]
    public async Task GetSocietyDetailsAsync_InactiveSociety_ReturnsEmptyDto()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using var context = new ApplicationDbContext(options);

        var society = new SocietyDetailsEntity
        {
            Id = 100,
            PropertyId = 549357,
            SocietyName = "ABC Society",
            IsActive = false,
            MarkedForDeletion = false
        };

        var property = new PropertyEntity
        {
            Id = 549357,
            WardId = 79,
            TaxZoneId = 10,
            SocietyDetailId = 100,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.SocietyDetailsMast.Add(society);
        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var service = CreateSocietyService(context);
        var result = await service.GetSocietyDetailsAsync(549357);

        Assert.NotNull(result);
        Assert.Equal(549357, result.PropertyId);
        Assert.Null(result.SocietyDetailId);
    }

    [Fact]
    public async Task GetSocietyDetailsAsync_MarkedForDeletionSociety_ReturnsEmptyDto()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using var context = new ApplicationDbContext(options);

        var society = new SocietyDetailsEntity
        {
            Id = 100,
            PropertyId = 549357,
            SocietyName = "ABC Society",
            IsActive = true,
            MarkedForDeletion = true
        };

        var property = new PropertyEntity
        {
            Id = 549357,
            WardId = 79,
            TaxZoneId = 10,
            SocietyDetailId = 100,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.SocietyDetailsMast.Add(society);
        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var service = CreateSocietyService(context);
        var result = await service.GetSocietyDetailsAsync(549357);

        Assert.NotNull(result);
        Assert.Equal(549357, result.PropertyId);
        Assert.Null(result.SocietyDetailId);
    }
}
