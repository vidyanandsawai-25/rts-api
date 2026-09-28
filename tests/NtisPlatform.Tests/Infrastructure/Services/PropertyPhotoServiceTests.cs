using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

/// <summary>
/// Covers the shared PROPERTY_PLAN resolution in GetLatestByPropertyIdAsync: the plan row for a
/// non-Amenity apartment unit carries no PropertyId, so it must be matched by
/// (SocietyDetailId, Type) instead -- and that match must NOT leak across units of a different
/// Type in the same society.
/// </summary>
public class PropertyPhotoServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static PropertyPhotoService BuildService(ApplicationDbContext context)
        => new(context, new Mock<IUnitOfWork>().Object);

    [Fact]
    public async Task GetLatestByPropertyIdAsync_ApartmentUnit_ResolvesSharedPlanByMatchingSocietyAndType()
    {
        var context = GetInMemoryDbContext();

        var society = new SocietyDetailsEntity { SocietyName = "Green Meadows", IsActive = true };
        context.SocietyDetailsMast.Add(society);
        await context.SaveChangesAsync();

        var wing = new WingDetailsMastEntity { SocietyDetailsMastId = society.Id, WingMasterId = 1, IsActive = true };
        context.Set<WingDetailsMastEntity>().Add(wing);
        await context.SaveChangesAsync();

        var unit = new PropertyEntity { PropertyNo = "A-101", CategoryId = 1, PropertyTypeId = 5, Type = "2BHK", WingDetailId = wing.Id, FlatOrShopNo = "A-101", IsActive = true };
        context.PropertyMast.Add(unit);
        await context.SaveChangesAsync();

        var photoType = new PropertyPhotoTypeEntity { PhotoTypeCode = "PROPERTY_PLAN", PhotoScope = "PROPERTY", IsActive = true };
        context.PropertyPhotoTypes.Add(photoType);
        await context.SaveChangesAsync();

        var sharedPlan = PropertyPhotoEntity.CreateWithDetails(
            propertyId: null,
            photoTypeId: photoType.Id,
            entityType: "S",
            societyDetailId: society.Id,
            wingDetailId: null,
            documentBindingId: null,
            displayOrder: 1,
            remarks: "2BHK plan",
            type: "2BHK");
        context.PropertyPhotos.Add(sharedPlan);
        await context.SaveChangesAsync();

        var service = BuildService(context);

        var results = await service.GetLatestByPropertyIdAsync(unit.Id, CancellationToken.None);

        var found = Assert.Single(results);
        Assert.Equal(sharedPlan.Id, found.Id);
    }

    [Fact]
    public async Task GetLatestByPropertyIdAsync_ApartmentUnit_DoesNotResolvePlanOfDifferentTypeInSameSociety()
    {
        var context = GetInMemoryDbContext();

        var society = new SocietyDetailsEntity { SocietyName = "Green Meadows", IsActive = true };
        context.SocietyDetailsMast.Add(society);
        await context.SaveChangesAsync();

        var wing = new WingDetailsMastEntity { SocietyDetailsMastId = society.Id, WingMasterId = 1, IsActive = true };
        context.Set<WingDetailsMastEntity>().Add(wing);
        await context.SaveChangesAsync();

        // This unit is 1BHK, but the only shared plan on file is for 2BHK units.
        var unit = new PropertyEntity { PropertyNo = "A-201", CategoryId = 1, PropertyTypeId = 5, Type = "1BHK", WingDetailId = wing.Id, FlatOrShopNo = "A-201", IsActive = true };
        context.PropertyMast.Add(unit);
        await context.SaveChangesAsync();

        var photoType = new PropertyPhotoTypeEntity { PhotoTypeCode = "PROPERTY_PLAN", PhotoScope = "PROPERTY", IsActive = true };
        context.PropertyPhotoTypes.Add(photoType);
        await context.SaveChangesAsync();

        var otherTypePlan = PropertyPhotoEntity.CreateWithDetails(
            propertyId: null,
            photoTypeId: photoType.Id,
            entityType: "S",
            societyDetailId: society.Id,
            wingDetailId: null,
            documentBindingId: null,
            displayOrder: 1,
            remarks: "2BHK plan",
            type: "2BHK");
        context.PropertyPhotos.Add(otherTypePlan);
        await context.SaveChangesAsync();

        var service = BuildService(context);

        var results = await service.GetLatestByPropertyIdAsync(unit.Id, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetLatestByPropertyIdAsync_ApartmentAmenity_ResolvesItsOwnPlanByPropertyId()
    {
        var context = GetInMemoryDbContext();

        var amenityProperty = new PropertyEntity { PropertyNo = "CLUB-1", CategoryId = 1, PropertyTypeId = 140, Type = "CLUBHOUSE", IsActive = true };
        context.PropertyMast.Add(amenityProperty);
        await context.SaveChangesAsync();

        var society = new SocietyDetailsEntity { PropertyId = amenityProperty.Id, SocietyName = "Green Meadows", IsActive = true };
        context.SocietyDetailsMast.Add(society);
        await context.SaveChangesAsync();

        var photoType = new PropertyPhotoTypeEntity { PhotoTypeCode = "PROPERTY_PLAN", PhotoScope = "PROPERTY", IsActive = true };
        context.PropertyPhotoTypes.Add(photoType);
        await context.SaveChangesAsync();

        var amenityPlan = PropertyPhotoEntity.CreateWithDetails(
            propertyId: amenityProperty.Id,
            photoTypeId: photoType.Id,
            entityType: "P",
            societyDetailId: society.Id,
            wingDetailId: null,
            documentBindingId: null,
            displayOrder: 1,
            remarks: "Clubhouse plan",
            type: null);
        context.PropertyPhotos.Add(amenityPlan);
        await context.SaveChangesAsync();

        var service = BuildService(context);

        var results = await service.GetLatestByPropertyIdAsync(amenityProperty.Id, CancellationToken.None);

        var found = Assert.Single(results);
        Assert.Equal(amenityPlan.Id, found.Id);
    }

    [Fact]
    public async Task GetLatestByPropertyIdAsync_NonApartmentProperty_ResolvesItsOwnPlanByPropertyId()
    {
        var context = GetInMemoryDbContext();

        var property = new PropertyEntity { PropertyNo = "IND-1", CategoryId = 2, Type = "STANDALONE", IsActive = true };
        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var photoType = new PropertyPhotoTypeEntity { PhotoTypeCode = "PROPERTY_PLAN", PhotoScope = "PROPERTY", IsActive = true };
        context.PropertyPhotoTypes.Add(photoType);
        await context.SaveChangesAsync();

        var plan = PropertyPhotoEntity.CreateWithDetails(
            propertyId: property.Id,
            photoTypeId: photoType.Id,
            entityType: "P",
            societyDetailId: null,
            wingDetailId: null,
            documentBindingId: null,
            displayOrder: 1,
            remarks: "Standalone plan",
            type: null);
        context.PropertyPhotos.Add(plan);
        await context.SaveChangesAsync();

        var service = BuildService(context);

        var results = await service.GetLatestByPropertyIdAsync(property.Id, CancellationToken.None);

        var found = Assert.Single(results);
        Assert.Equal(plan.Id, found.Id);
    }
}
