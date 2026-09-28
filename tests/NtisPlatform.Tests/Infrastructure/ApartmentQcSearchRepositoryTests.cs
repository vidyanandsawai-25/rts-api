using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure;

public class ApartmentQcSearchRepositoryTests
{
    private const int ApartmentCategoryId = 1;

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void SeedApartmentCategory(ApplicationDbContext context)
        => context.PropertyCategoryMaster.Add(new PropertyCategoryEntity { Id = ApartmentCategoryId, PropertyCategoryName = "Apartment", IsActive = true, CreatedDate = DateTime.Now });

    private static PropertyEntity CreateProperty(int id, int wardId, string propertyNo, string? partitionNo, int? wingDetailId = null, int? categoryId = null, int? propertyTypeId = null)
        => new()
        {
            Id = id,
            WardId = wardId,
            TaxZoneId = 1,
            PropertyNo = propertyNo,
            PartitionNo = partitionNo,
            WingDetailId = wingDetailId,
            CategoryId = categoryId,
            PropertyTypeId = propertyTypeId,
            IsActive = true,
            MarkedForDeletion = false
        };

    [Fact]
    public async Task GetSuggestionsAsync_SocietyRepresentativeProperty_ResolvesSocietyCategoryWithWings()
    {
        using var context = CreateContext();
        context.WardMaster.Add(new WardEntity { Id = 77, WardNo = "77", Description = "Ward 77", ZoneId = 1, IsActive = true });
        SeedApartmentCategory(context);
        context.PropertyMast.Add(CreateProperty(1, 77, "1", null, categoryId: ApartmentCategoryId));
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 5, PropertyId = 1, SocietyName = "Sunrise CHS", IsActive = true, CreatedDate = DateTime.Now });
        context.WingDetailsMast.AddRange(
            new WingDetailsMastEntity { Id = 10, SocietyDetailsMastId = 5, WingName = "A Wing", IsActive = true, CreatedDate = DateTime.Now },
            new WingDetailsMastEntity { Id = 20, SocietyDetailsMastId = 5, WingName = "B Wing", IsActive = true, CreatedDate = DateTime.Now });
        await context.SaveChangesAsync();

        var repository = new ApartmentQcSearchRepository(context, new MemoryCache(new MemoryCacheOptions()));
        var result = await repository.GetSuggestionsAsync(wardId: 77, propertyNo: null, partitionNo: null, maxResults: 20);

        var suggestion = Assert.Single(result);
        Assert.Equal(ApartmentQcSearchCategory.Society, suggestion.Category);
        Assert.Equal(5, suggestion.SocietyDetailId);
        Assert.Equal("Sunrise CHS", suggestion.SocietyName);
        Assert.NotNull(suggestion.Wings);
        Assert.Equal(2, suggestion.Wings!.Count);
        Assert.Contains(suggestion.Wings, w => w.WingName == "A Wing");
        Assert.Contains(suggestion.Wings, w => w.WingName == "B Wing");
        Assert.Null(suggestion.WingDetailId);
    }

    [Fact]
    public async Task GetSuggestionsAsync_PropertyWithWingDetailId_ResolvesUnitCategoryWithSocietyAndWingLinkage()
    {
        // An apartment unit must carry SocietyDetailId + WingDetailId + PropertyId together --
        // its society is resolved via WingDetailsMast.SocietyDetailsMastId, since the unit's own
        // PropertyMast row is never the one SocietyDetailsMast.PropertyId points to (that's a
        // different, "representative" property row).
        using var context = CreateContext();
        context.WardMaster.Add(new WardEntity { Id = 77, WardNo = "77", Description = "Ward 77", ZoneId = 1, IsActive = true });
        SeedApartmentCategory(context);
        context.PropertyMast.AddRange(
            CreateProperty(1, 77, "1", null, categoryId: ApartmentCategoryId), // the society's representative property
            CreateProperty(2, 77, "1", "1-A", wingDetailId: 10, categoryId: ApartmentCategoryId)); // the apartment unit
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 5, PropertyId = 1, SocietyName = "Sunrise CHS", IsActive = true, CreatedDate = DateTime.Now });
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 10, SocietyDetailsMastId = 5, WingName = "A Wing", IsActive = true, CreatedDate = DateTime.Now });
        await context.SaveChangesAsync();

        var repository = new ApartmentQcSearchRepository(context, new MemoryCache(new MemoryCacheOptions()));
        var result = await repository.GetSuggestionsAsync(wardId: 77, propertyNo: null, partitionNo: null, maxResults: 20);

        var unit = Assert.Single(result, r => r.Category == ApartmentQcSearchCategory.Unit);
        Assert.Equal(2, unit.PropertyId);
        Assert.Equal(10, unit.WingDetailId);
        Assert.Equal(5, unit.SocietyDetailId);
        Assert.Equal("Sunrise CHS", unit.SocietyName);
        Assert.Null(unit.Wings);
        Assert.Equal("1-1-A", unit.DisplayLabel);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ApartmentUnitWithAmenityPropertyType_ResolvesAmenityCategory()
    {
        // An Apartment-category property with a PartitionNo whose PropertyType is classified
        // Amenity (PartType = "Amenity") is its own category, not a generic Unit -- regardless of
        // whether the description is in English or Marathi, since PartType is the
        // language-independent classification signal.
        using var context = CreateContext();
        context.WardMaster.Add(new WardEntity { Id = 77, WardNo = "77", Description = "Ward 77", ZoneId = 1, IsActive = true });
        SeedApartmentCategory(context);
        context.PropertyTypeMasters.Add(new PropertyTypeMasterEntity { Id = 140, PropertyDescription = "अॅमिनीटी", PartType = "Amenity", IsActive = true, CreatedDate = DateTime.Now });
        context.PropertyMast.Add(CreateProperty(1, 77, "1", "CLUB", categoryId: ApartmentCategoryId, propertyTypeId: 140));
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 5, PropertyId = 1, SocietyName = "Sunrise CHS", IsActive = true, CreatedDate = DateTime.Now });
        await context.SaveChangesAsync();

        var repository = new ApartmentQcSearchRepository(context, new MemoryCache(new MemoryCacheOptions()));
        var result = await repository.GetSuggestionsAsync(wardId: 77, propertyNo: null, partitionNo: null, maxResults: 20);

        var suggestion = Assert.Single(result);
        Assert.Equal(ApartmentQcSearchCategory.Amenity, suggestion.Category);
        Assert.Equal("apartment society amenity property", suggestion.CategoryLabel);
        Assert.Equal(5, suggestion.SocietyDetailId);
        Assert.Equal("Sunrise CHS", suggestion.SocietyName);
        Assert.Null(suggestion.Wings);
    }

    [Fact]
    public async Task GetSuggestionsAsync_AmenityPropertyWithoutDirectSocietyLink_ResolvesSocietyViaRepresentativePropertySamePropertyNo()
    {
        // Amenity properties (PartitionNo = "AM2") carry no direct SocietyDetailsMast.PropertyId link
        // on their own row. They resolve SocietyDetailId/SocietyName from the representative society
        // property (PartitionNo = null) under the same PropertyNo in the ward.
        using var context = CreateContext();
        context.WardMaster.Add(new WardEntity { Id = 77, WardNo = "77", Description = "Ward 77", ZoneId = 1, IsActive = true });
        SeedApartmentCategory(context);
        context.PropertyTypeMasters.Add(new PropertyTypeMasterEntity { Id = 140, PropertyDescription = "Amenity", PartType = "Amenity", IsActive = true, CreatedDate = DateTime.Now });
        context.PropertyMast.AddRange(
            CreateProperty(1, 77, "9", null, categoryId: ApartmentCategoryId), // representative society property
            CreateProperty(2, 77, "9", "AM2", categoryId: ApartmentCategoryId, propertyTypeId: 140) // amenity property
        );
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 1509001, PropertyId = 1, SocietyName = "Gokul CHS", IsActive = true, CreatedDate = DateTime.Now });
        await context.SaveChangesAsync();

        var repository = new ApartmentQcSearchRepository(context, new MemoryCache(new MemoryCacheOptions()));
        var result = await repository.GetSuggestionsAsync(wardId: 77, propertyNo: "9", partitionNo: "AM2", maxResults: 20);

        var amenity = Assert.Single(result, r => r.PartitionNo == "AM2");
        Assert.Equal(ApartmentQcSearchCategory.Amenity, amenity.Category);
        Assert.Equal(1509001, amenity.SocietyDetailId);
        Assert.Equal("Gokul CHS", amenity.SocietyName);
    }

    [Fact]
    public async Task GetSuggestionsAsync_UnitWithOwnStraySocietyRow_StillResolvesUnitNotSociety()
    {
        // Real production data: the "Save Society Details" screen creates a SocietyDetailsMast row
        // scoped to whichever single property the user is editing, with no cross-partition
        // de-duplication. So a genuine apartment unit (WingDetailId set) can ALSO carry its own
        // redundant SocietyDetailsMast row (its own PropertyId, not the building's representative
        // property's Id). WingDetailId must still win -- this unit is a Unit, not a second "Society".
        using var context = CreateContext();
        context.WardMaster.Add(new WardEntity { Id = 77, WardNo = "77", Description = "Ward 77", ZoneId = 1, IsActive = true });
        SeedApartmentCategory(context);
        context.PropertyMast.Add(CreateProperty(2, 77, "1", "1-A", wingDetailId: 10, categoryId: ApartmentCategoryId));
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 10, SocietyDetailsMastId = 5, WingName = "A Wing", IsActive = true, CreatedDate = DateTime.Now });
        context.SocietyDetailsMast.AddRange(
            new SocietyDetailsEntity { Id = 5, PropertyId = 1, SocietyName = "Sunrise CHS", IsActive = true, CreatedDate = DateTime.Now }, // the wing's real owning society
            new SocietyDetailsEntity { Id = 99, PropertyId = 2, SocietyName = "Sunrise CHS", IsActive = true, CreatedDate = DateTime.Now }); // stray row self-referencing the unit
        await context.SaveChangesAsync();

        var repository = new ApartmentQcSearchRepository(context, new MemoryCache(new MemoryCacheOptions()));
        var result = await repository.GetSuggestionsAsync(wardId: 77, propertyNo: null, partitionNo: null, maxResults: 20);

        var unit = Assert.Single(result);
        Assert.Equal(ApartmentQcSearchCategory.Unit, unit.Category);
        Assert.Equal(5, unit.SocietyDetailId); // resolved via the wing, not the stray self-referencing row
    }

    [Fact]
    public async Task GetSuggestionsAsync_NonApartmentCategoryWithStraySocietyLink_ResolvesIndividualProperty()
    {
        // A property whose PropertyCategoryMaster category is NOT Apartment must always be
        // IndividualProperty, even if it happens to carry a stray SocietyDetailsMast/WingDetailId
        // link from legacy data -- the category gate wins over any linkage.
        using var context = CreateContext();
        context.WardMaster.Add(new WardEntity { Id = 77, WardNo = "77", Description = "Ward 77", ZoneId = 1, IsActive = true });
        context.PropertyCategoryMaster.Add(new PropertyCategoryEntity { Id = 2, PropertyCategoryName = "Residential", IsActive = true, CreatedDate = DateTime.Now });
        context.PropertyMast.Add(CreateProperty(1, 77, "1", null, categoryId: 2));
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 5, PropertyId = 1, SocietyName = "Stray Link", IsActive = true, CreatedDate = DateTime.Now });
        await context.SaveChangesAsync();

        var repository = new ApartmentQcSearchRepository(context, new MemoryCache(new MemoryCacheOptions()));
        var result = await repository.GetSuggestionsAsync(wardId: 77, propertyNo: null, partitionNo: null, maxResults: 20);

        var suggestion = Assert.Single(result);
        Assert.Equal(ApartmentQcSearchCategory.IndividualProperty, suggestion.Category);
        Assert.Null(suggestion.SocietyDetailId);
        Assert.Null(suggestion.WingDetailId);
        Assert.Null(suggestion.Wings);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ApartmentCategoryWithoutWingOrSocietyLink_StillResolvesSociety()
    {
        // An Apartment-category property with no PartitionNo must NEVER fall back to
        // IndividualProperty, even when no SocietyDetailsMast row points to it yet (e.g. a
        // building's common-area/society-office property whose Society tab hasn't been filled in).
        // "Apartment category" and "IndividualProperty" are a strict partition.
        using var context = CreateContext();
        context.WardMaster.Add(new WardEntity { Id = 77, WardNo = "77", Description = "Ward 77", ZoneId = 1, IsActive = true });
        SeedApartmentCategory(context);
        context.PropertyMast.Add(CreateProperty(1, 77, "7", null, categoryId: ApartmentCategoryId));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcSearchRepository(context, new MemoryCache(new MemoryCacheOptions()));
        var result = await repository.GetSuggestionsAsync(wardId: 77, propertyNo: null, partitionNo: null, maxResults: 20);

        var suggestion = Assert.Single(result);
        Assert.Equal(ApartmentQcSearchCategory.Society, suggestion.Category);
        Assert.Null(suggestion.SocietyDetailId);
        Assert.Null(suggestion.SocietyName);
        Assert.NotNull(suggestion.Wings);
        Assert.Empty(suggestion.Wings!);
        Assert.Null(suggestion.WingDetailId);
    }

    [Fact]
    public async Task GetSuggestionsAsync_StandaloneProperty_ResolvesIndividualPropertyCategory()
    {
        using var context = CreateContext();
        context.WardMaster.Add(new WardEntity { Id = 77, WardNo = "77", Description = "Ward 77", ZoneId = 1, IsActive = true });
        context.PropertyMast.Add(CreateProperty(3, 77, "50", null));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcSearchRepository(context, new MemoryCache(new MemoryCacheOptions()));
        var result = await repository.GetSuggestionsAsync(wardId: 77, propertyNo: null, partitionNo: null, maxResults: 20);

        var suggestion = Assert.Single(result);
        Assert.Equal(ApartmentQcSearchCategory.IndividualProperty, suggestion.Category);
        Assert.Null(suggestion.SocietyDetailId);
        Assert.Null(suggestion.WingDetailId);
        Assert.Null(suggestion.Wings);
    }

    [Fact]
    public async Task GetSuggestionsAsync_FiltersByPartialPropertyNo()
    {
        using var context = CreateContext();
        context.WardMaster.Add(new WardEntity { Id = 77, WardNo = "77", Description = "Ward 77", ZoneId = 1, IsActive = true });
        context.PropertyMast.AddRange(
            CreateProperty(1, 77, "123", null),
            CreateProperty(2, 77, "456", null),
            CreateProperty(3, 77, "812", null));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcSearchRepository(context, new MemoryCache(new MemoryCacheOptions()));
        var result = await repository.GetSuggestionsAsync(wardId: 77, propertyNo: "1", partitionNo: null, maxResults: 20);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.PropertyNo == "123");
        Assert.Contains(result, r => r.PropertyNo == "812");
    }

    [Fact]
    public async Task GetSuggestionsAsync_DifferentWard_ReturnsNoResults()
    {
        using var context = CreateContext();
        context.WardMaster.Add(new WardEntity { Id = 77, WardNo = "77", Description = "Ward 77", ZoneId = 1, IsActive = true });
        context.PropertyMast.Add(CreateProperty(1, 77, "1", null));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcSearchRepository(context, new MemoryCache(new MemoryCacheOptions()));
        var result = await repository.GetSuggestionsAsync(wardId: 99, propertyNo: null, partitionNo: null, maxResults: 20);

        Assert.Empty(result);
    }
}
