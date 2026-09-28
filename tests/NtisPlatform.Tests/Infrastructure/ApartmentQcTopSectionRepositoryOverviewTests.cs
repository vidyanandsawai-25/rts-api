using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure;

/// <summary>
/// Covers the overview-strip fields added to <c>ApartmentQcTopSectionRepository.GetPropertyAsync</c>:
/// society name, bilingual owner/occupier names, owner category, and the society's main-building
/// photo. The pre-existing overview/info/performance fields are covered elsewhere (or not at all
/// yet, per the sibling test gap) — this file is scoped to the new additions only.
/// </summary>
public class ApartmentQcTopSectionRepositoryOverviewTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static PropertyEntity SeedProperty(ApplicationDbContext context, int id = 100)
    {
        var property = new PropertyEntity
        {
            Id = id,
            WardId = 1,
            TaxZoneId = 1,
            PropertyNo = "2024-000123",
            OwnerName = "मातोश्री बिल्डर्स",
            OwnerNameEnglish = "MATOSHREE BUILDERS",
            OccupierName = "मातोश्री बिल्डर्स",
            OccupierNameEnglish = "MATOSHREE BUILDERS",
            IsActive = true,
            MarkedForDeletion = false
        };
        context.PropertyMast.Add(property);
        return property;
    }

    [Fact]
    public async Task GetPropertyAsync_ReturnsSocietyNameAndBilingualOwnerOccupierFields()
    {
        using var context = CreateContext();
        SeedProperty(context);
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity
        {
            Id = 1,
            PropertyId = 100,
            SocietyName = "मातोश्री बिल्डर्स सोसायटी",
            SocietyNameEnglish = "MATOSHREE BUILDERS SOCIETY",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var repository = new ApartmentQcTopSectionRepository(context);
        var result = await repository.GetPropertyAsync(new ApartmentQcTopSectionQueryParameters { PropertyId = 100 });

        Assert.NotNull(result);
        Assert.Equal("MATOSHREE BUILDERS SOCIETY", result!.SocietyNameEnglish);
        Assert.Equal("मातोश्री बिल्डर्स सोसायटी", result.SocietyName);
        Assert.Equal("MATOSHREE BUILDERS", result.OwnerNameEnglish);
        Assert.Equal("मातोश्री बिल्डर्स", result.OwnerName);
        Assert.Equal("MATOSHREE BUILDERS", result.OccupierNameEnglish);
        Assert.Equal("मातोश्री बिल्डर्स", result.OccupierName);
    }

    [Fact]
    public async Task GetPropertyAsync_NoSociety_LeavesSocietyFieldsNull()
    {
        using var context = CreateContext();
        SeedProperty(context);
        await context.SaveChangesAsync();

        var repository = new ApartmentQcTopSectionRepository(context);
        var result = await repository.GetPropertyAsync(new ApartmentQcTopSectionQueryParameters { PropertyId = 100 });

        Assert.NotNull(result);
        Assert.Null(result!.SocietyName);
        Assert.Null(result.SocietyNameEnglish);
        Assert.Null(result.SocietyBuildingPhotoGuid);
    }

    [Fact]
    public async Task GetPropertyAsync_ResolvesOwnerCategoryFromOwnerTypeMaster()
    {
        using var context = CreateContext();
        SeedProperty(context);
        context.OwnerTypeMaster.Add(new OwnerTypeMasterEntity { Id = 1, OwnerType = "Company / Industrial", IsActive = true });
        context.PropertyMastDetails.Add(new PropertyAssessmentEntity
        {
            Id = 1,
            PropertyId = 100,
            OwnerTypeId = 1,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.Now
        });
        await context.SaveChangesAsync();

        var repository = new ApartmentQcTopSectionRepository(context);
        var result = await repository.GetPropertyAsync(new ApartmentQcTopSectionQueryParameters { PropertyId = 100 });

        Assert.NotNull(result);
        Assert.Equal("Company / Industrial", result!.OwnerCategory);
    }

    [Fact]
    public async Task GetPropertyAsync_ResolvesSocietyBuildingPhotoGuid_FromSocietyPlacePhotoType()
    {
        using var context = CreateContext();
        SeedProperty(context);
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 1, PropertyId = 100, IsActive = true });
        context.PropertyPhotoTypes.Add(new PropertyPhotoTypeEntity
        {
            Id = 5,
            PhotoTypeCode = "SOCIETY_PLACE",
            PhotoTypeName = "Society Place Photo",
            PhotoScope = "SOCIETY",
            IsActive = true
        });
        var document = new DocumentEntity { Id = 1, DocumentGuid = Guid.NewGuid(), IsActive = true };
        context.Documents.Add(document);
        var binding = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1, departmentId: 1, moduleId: 1,
            referenceTableName: "PropertyPhoto", referenceTableId: 1, referencePropertyName: "DocumentBindingId");
        binding.Id = 1;
        context.DocumentBindings.Add(binding);
        var photo = PropertyPhotoEntity.CreateWithDetails(
            propertyId: 100, photoTypeId: 5, entityType: "S",
            societyDetailId: 1, wingDetailId: null, documentBindingId: 1);
        context.PropertyPhotos.Add(photo);
        await context.SaveChangesAsync();

        var repository = new ApartmentQcTopSectionRepository(context);
        var result = await repository.GetPropertyAsync(new ApartmentQcTopSectionQueryParameters { PropertyId = 100 });

        Assert.NotNull(result);
        Assert.Equal(document.DocumentGuid, result!.SocietyBuildingPhotoGuid);
    }

    [Fact]
    public async Task GetPropertyAsync_IgnoresWingScopedPhoto_ForSocietyBuildingPhoto()
    {
        using var context = CreateContext();
        SeedProperty(context);
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 1, PropertyId = 100, IsActive = true });
        context.PropertyPhotoTypes.Add(new PropertyPhotoTypeEntity
        {
            Id = 8,
            PhotoTypeCode = "WING_BUILDING",
            PhotoTypeName = "Wing Building Photo",
            PhotoScope = "WING",
            IsActive = true
        });
        var document = new DocumentEntity { Id = 1, DocumentGuid = Guid.NewGuid(), IsActive = true };
        context.Documents.Add(document);
        var binding = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1, departmentId: 1, moduleId: 1,
            referenceTableName: "PropertyPhoto", referenceTableId: 1, referencePropertyName: "DocumentBindingId");
        binding.Id = 1;
        context.DocumentBindings.Add(binding);
        var wingPhoto = PropertyPhotoEntity.CreateWithDetails(
            propertyId: 100, photoTypeId: 8, entityType: "W",
            societyDetailId: null, wingDetailId: 1, documentBindingId: 1);
        context.PropertyPhotos.Add(wingPhoto);
        await context.SaveChangesAsync();

        var repository = new ApartmentQcTopSectionRepository(context);
        var result = await repository.GetPropertyAsync(new ApartmentQcTopSectionQueryParameters { PropertyId = 100 });

        Assert.NotNull(result);
        Assert.Null(result!.SocietyBuildingPhotoGuid);
    }
}
