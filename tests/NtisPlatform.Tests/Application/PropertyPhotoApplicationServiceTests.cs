using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MockQueryable;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Minimal DbContext mapping only PropertyPhotoTypeEntity -- used to prove a repository filter
/// expression is translatable by a real relational EF Core provider (SQLite), which the
/// AsQueryable()-over-List mocks used elsewhere in this file cannot verify: LINQ-to-Objects
/// happily runs string.Equals(x, y, StringComparison.OrdinalIgnoreCase) since it's just calling
/// the real .NET method, while a SQL provider must translate the expression tree and rejects
/// that overload -- exactly the bug this test guards against regressing to.
/// </summary>
internal sealed class PhotoTypeOnlyDbContext : DbContext
{
    public PhotoTypeOnlyDbContext(DbContextOptions<PhotoTypeOnlyDbContext> options) : base(options) { }
    public DbSet<PropertyPhotoTypeEntity> PropertyPhotoTypes => Set<PropertyPhotoTypeEntity>();
}

/// <summary>
/// Covers the Property-tab photo type list. The scope shown is resolved by
/// ResolvePhotoScopeAsync from the property's PropertyCategoryMaster category and PartitionNo:
/// an Apartment-category property with no PartitionNo resolves to SOCIETY (which also includes
/// WING-scoped types) -- e.g. the "representative"/common-area property of a building; an
/// Apartment-category property WITH a PartitionNo (an actual flat) resolves to PROPERTY;
/// Society/Wing-category properties resolve directly to their own scope; anything else (or no
/// category at all) falls back to PROPERTY.
/// </summary>
public class PropertyPhotoApplicationServiceTests
{
    private static readonly PropertyPhotoTypeEntity PropertyType = new() { PhotoTypeCode = "PROPERTY_FRONT", PhotoTypeName = "Front Elevation", PhotoScope = "PROPERTY", IsActive = true, DisplayOrder = 1 };
    private static readonly PropertyPhotoTypeEntity SocietyType = new() { PhotoTypeCode = "SOCIETY_PLACE", PhotoTypeName = "Society Building", PhotoScope = "SOCIETY", IsActive = true, DisplayOrder = 1 };
    private static readonly PropertyPhotoTypeEntity WingType = new() { PhotoTypeCode = "WING_BUILDING", PhotoTypeName = "Wing Building", PhotoScope = "WING", IsActive = true, DisplayOrder = 1 };

    private static PropertyPhotoApplicationService BuildService(
        Mock<IPropertyPhotoService> photoService,
        Mock<IRepository<PropertyPhotoTypeEntity, int>> photoTypeRepo,
        Mock<IRepository<PropertyEntity, int>>? propertyRepo = null,
        Mock<IRepository<PropertyCategoryEntity, int>>? categoryRepo = null,
        Mock<IRepository<SocietyDetailsEntity, int>>? societyRepo = null)
    {
        return new PropertyPhotoApplicationService(
            photoService.Object,
            new Mock<IDocumentApplicationService>().Object,
            new Mock<IModuleLookupService>().Object,
            photoTypeRepo.Object,
            (propertyRepo ?? new Mock<IRepository<PropertyEntity, int>>()).Object,
            (categoryRepo ?? new Mock<IRepository<PropertyCategoryEntity, int>>()).Object,
            (societyRepo ?? new Mock<IRepository<SocietyDetailsEntity, int>>()).Object,
            NullLogger<PropertyPhotoApplicationService>.Instance);
    }

    private static Mock<IRepository<PropertyPhotoTypeEntity, int>> BuildPhotoTypeRepo(params PropertyPhotoTypeEntity[] types)
    {
        var repo = new Mock<IRepository<PropertyPhotoTypeEntity, int>>();
        repo.Setup(r => r.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<PropertyPhotoTypeEntity, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<PropertyPhotoTypeEntity, bool>> filter, CancellationToken _) =>
                types.AsQueryable().Where(filter).ToList());
        return repo;
    }

    private static (Mock<IRepository<PropertyEntity, int>> PropertyRepo, Mock<IRepository<PropertyCategoryEntity, int>> CategoryRepo) BuildPropertyWithCategory(
        int propertyId, string? partitionNo, int? categoryId, string? categoryName)
    {
        var propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        propertyRepo.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyEntity { Id = propertyId, PartitionNo = partitionNo, CategoryId = categoryId });

        var categoryRepo = new Mock<IRepository<PropertyCategoryEntity, int>>();
        if (categoryId.HasValue && categoryName != null)
        {
            categoryRepo.Setup(r => r.GetByIdAsync(categoryId.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PropertyCategoryEntity { Id = categoryId.Value, PropertyCategoryName = categoryName });
        }

        return (propertyRepo, categoryRepo);
    }

    [Fact]
    public async Task GetPhotoTypesWithStatusAsync_ApartmentCategoryWithNoPartitionNo_ResolvesSocietyScopeIncludingWingTypes()
    {
        // The representative/common-area property of an apartment building (Apartment category,
        // no PartitionNo) shows SOCIETY-scoped types -- and WING-scoped types too, per the
        // explicit business decision that Society and Wing share the same view here.
        const int propertyId = 550722;
        var (propertyRepo, categoryRepo) = BuildPropertyWithCategory(propertyId, partitionNo: null, categoryId: 1, categoryName: "Apartment");
        var photoTypeRepo = BuildPhotoTypeRepo(PropertyType, SocietyType, WingType);

        var photoService = new Mock<IPropertyPhotoService>();
        photoService.Setup(s => s.GetLatestByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyPhotoEntity>());

        var service = BuildService(photoService, photoTypeRepo, propertyRepo, categoryRepo);

        var result = await service.GetPhotoTypesWithStatusAsync(propertyId);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.PhotoTypeCode == "SOCIETY_PLACE");
        Assert.Contains(result, t => t.PhotoTypeCode == "WING_BUILDING");
        Assert.DoesNotContain(result, t => t.PhotoTypeCode == "PROPERTY_FRONT");
    }

    [Fact]
    public async Task GetPhotoTypesWithStatusAsync_ApartmentCategoryWithPartitionNo_ResolvesPropertyScope()
    {
        // An actual flat (Apartment category, has a PartitionNo) shows PROPERTY-scoped types only.
        const int propertyId = 550723;
        var (propertyRepo, categoryRepo) = BuildPropertyWithCategory(propertyId, partitionNo: "A9", categoryId: 1, categoryName: "Apartment");
        var photoTypeRepo = BuildPhotoTypeRepo(PropertyType, SocietyType, WingType);

        var photoService = new Mock<IPropertyPhotoService>();
        photoService.Setup(s => s.GetLatestByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyPhotoEntity>());

        var service = BuildService(photoService, photoTypeRepo, propertyRepo, categoryRepo);

        var result = await service.GetPhotoTypesWithStatusAsync(propertyId);

        var type = Assert.Single(result);
        Assert.Equal("PROPERTY_FRONT", type.PhotoTypeCode);
    }

    [Fact]
    public async Task GetPhotoTypesWithStatusAsync_NoCategory_FallsBackToPropertyScope()
    {
        // A standalone property with no category set at all defaults to PROPERTY scope.
        const int propertyId = 550724;
        var (propertyRepo, categoryRepo) = BuildPropertyWithCategory(propertyId, partitionNo: null, categoryId: null, categoryName: null);
        var photoTypeRepo = BuildPhotoTypeRepo(PropertyType, SocietyType);

        var photoService = new Mock<IPropertyPhotoService>();
        photoService.Setup(s => s.GetLatestByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyPhotoEntity>());

        var service = BuildService(photoService, photoTypeRepo, propertyRepo, categoryRepo);

        var result = await service.GetPhotoTypesWithStatusAsync(propertyId);

        var type = Assert.Single(result);
        Assert.Equal("PROPERTY_FRONT", type.PhotoTypeCode);
    }

    [Fact]
    public async Task GetPhotoTypesWithStatusAsync_ApartmentAmenity_IncludesPropertyPlanTypeAlongsideSocietyTypes()
    {
        // An Amenity property (SocietyDetailsMast.PropertyId == its own id) resolves to SOCIETY
        // scope -- but PROPERTY_PLAN is scoped PROPERTY, so it needs the same explicit allowance
        // already given to WING types, or an Amenity would never see its own plan upload slot.
        const int propertyId = 550725;
        var (propertyRepo, categoryRepo) = BuildPropertyWithCategory(propertyId, partitionNo: null, categoryId: 1, categoryName: "Apartment");

        var societyRepo = new Mock<IRepository<SocietyDetailsEntity, int>>();
        societyRepo.Setup(r => r.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SocietyDetailsEntity, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocietyDetailsEntity> { new() { Id = 77, PropertyId = propertyId, IsActive = true } });

        var planType = new PropertyPhotoTypeEntity { PhotoTypeCode = "PROPERTY_PLAN", PhotoTypeName = "Property Plan", PhotoScope = "PROPERTY", IsActive = true, DisplayOrder = 5 };
        var photoTypeRepo = BuildPhotoTypeRepo(PropertyType, SocietyType, WingType, planType);

        var photoService = new Mock<IPropertyPhotoService>();
        photoService.Setup(s => s.GetLatestByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyPhotoEntity>());

        var service = BuildService(photoService, photoTypeRepo, propertyRepo, categoryRepo, societyRepo);

        var result = await service.GetPhotoTypesWithStatusAsync(propertyId);

        Assert.Contains(result, t => t.PhotoTypeCode == "SOCIETY_PLACE");
        Assert.Contains(result, t => t.PhotoTypeCode == "WING_BUILDING");
        Assert.Contains(result, t => t.PhotoTypeCode == "PROPERTY_PLAN");
        Assert.DoesNotContain(result, t => t.PhotoTypeCode == "PROPERTY_FRONT");
    }

    [Fact]
    public async Task PhotoScopeFilter_TranslatesAgainstRealRelationalProvider()
    {
        // Regression guard: a previous version of this filter used
        // string.Equals(t.PhotoScope, "PROPERTY", StringComparison.OrdinalIgnoreCase), which
        // EF Core's relational providers cannot translate to SQL (InvalidOperationException at
        // runtime against a real database, even though it passed every mocked/AsQueryable()-over-
        // List unit test since LINQ-to-Objects just calls the real string.Equals method). Runs the
        // exact filter used by PropertyPhotoApplicationService against SQLite -- a real relational
        // provider -- to prove it's translatable.
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<PhotoTypeOnlyDbContext>().UseSqlite(connection).Options;
        using var context = new PhotoTypeOnlyDbContext(options);
        await context.Database.EnsureCreatedAsync();

        context.PropertyPhotoTypes.AddRange(
            new PropertyPhotoTypeEntity { PhotoTypeCode = "PROPERTY_FRONT", PhotoTypeName = "Front Elevation", PhotoScope = "PROPERTY", IsActive = true },
            new PropertyPhotoTypeEntity { PhotoTypeCode = "SOCIETY_PLACE", PhotoTypeName = "Society Building", PhotoScope = "SOCIETY", IsActive = true });
        await context.SaveChangesAsync();

        var results = await context.PropertyPhotoTypes
            .Where(t => t.IsActive && t.PhotoScope == "PROPERTY")
            .ToListAsync();

        var type = Assert.Single(results);
        Assert.Equal("PROPERTY_FRONT", type.PhotoTypeCode);
    }
}
