using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services.Handlers;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Handlers;

public class PropertyPhotoDocumentBindingHandlerTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void ReferenceTableName_ReturnsPropertyPhoto()
    {
        var photoService = new Mock<IPropertyPhotoService>();
        var context = GetInMemoryDbContext();
        var logger = new Mock<ILogger<PropertyPhotoDocumentBindingHandler>>();

        var handler = new PropertyPhotoDocumentBindingHandler(photoService.Object, context, logger.Object);

        Assert.Equal("PropertyPhoto", handler.ReferenceTableName);
    }

    [Fact]
    public void Handles_ReturnsTrue_ForPropertyPhoto()
    {
        var photoService = new Mock<IPropertyPhotoService>();
        var context = GetInMemoryDbContext();
        var logger = new Mock<ILogger<PropertyPhotoDocumentBindingHandler>>();

        var handler = new PropertyPhotoDocumentBindingHandler(photoService.Object, context, logger.Object);

        Assert.True(handler.Handles("PropertyPhoto"));
        Assert.True(handler.Handles("propertyphoto"));
        Assert.False(handler.Handles("OtherTable"));
    }

    [Fact]
    public async Task OnAfterUploadAsync_NewWingPhoto_ResolvesWingAndSocietyDetails()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var photoService = new Mock<IPropertyPhotoService>();
        var logger = new Mock<ILogger<PropertyPhotoDocumentBindingHandler>>();

        var wing = new WingDetailsMastEntity
        {
            SocietyDetailsMastId = 21,
            WingMasterId = 1,
            IsActive = true
        };
        context.Set<WingDetailsMastEntity>().Add(wing);
        await context.SaveChangesAsync();

        var property = new PropertyEntity
        {
            PropertyNo = "85",
            WingDetailId = wing.Id,
            IsActive = true
        };
        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var photoType = new PropertyPhotoTypeEntity
        {
            PhotoTypeCode = "WING_BUILDING",
            PhotoScope = "WING",
            IsActive = true
        };
        context.PropertyPhotoTypes.Add(photoType);
        await context.SaveChangesAsync();

        var document = new DocumentEntity
        {
            DocumentGuid = Guid.NewGuid(),
            DocumentType = "WING_BUILDING",
            FileName = "wing_photo.jpg",
            OriginalFileName = "wing_photo.jpg",
            FileExtension = ".jpg",
            MimeType = "image/jpeg",
            StoragePath = "Uploads/wing_photo.jpg",
            IsActive = true
        };
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var binding = new DocumentBindingEntity
        {
            DocumentId = document.Id,
            ReferenceTableName = "PropertyPhoto",
            ReferencePropertyName = "PropertyId",
            ReferenceTableId = property.Id,
            BindingPurpose = "Wing Photo",
            IsActive = true
        };
        context.DocumentBindings.Add(binding);
        await context.SaveChangesAsync();

        var handler = new PropertyPhotoDocumentBindingHandler(photoService.Object, context, logger.Object);

        // Act
        await handler.OnAfterUploadAsync(
            document.Id, // documentId
            binding.Id, // bindingId
            property.Id, // referenceTableId (PropertyId)
            42, // uploadedBy
            CancellationToken.None);

        // Assert
        // Wing-scoped photos are saved with PropertyId = null (only WingDetailId + SocietyDetailId
        // identify the row) -- per the Society/Wing/Property scope save rules.
        var createdPhoto = await context.PropertyPhotos.FirstOrDefaultAsync(p => p.WingDetailId == wing.Id);
        Assert.NotNull(createdPhoto);
        Assert.Equal("W", createdPhoto.EntityType);
        Assert.Null(createdPhoto.PropertyId);
        Assert.Equal(wing.Id, createdPhoto.WingDetailId);
        Assert.Equal(21, createdPhoto.SocietyDetailId);
        Assert.Equal(binding.Id, createdPhoto.DocumentBindingId);
    }

    [Fact]
    public async Task OnAfterUploadAsync_NewSocietyPhoto_UsesSocietyReferenceForGalleryLookup()
    {
        var context = GetInMemoryDbContext();
        var photoService = new Mock<IPropertyPhotoService>();
        var logger = new Mock<ILogger<PropertyPhotoDocumentBindingHandler>>();

        var society = new SocietyDetailsEntity { SocietyName = "Green Meadows", IsActive = true };
        context.SocietyDetailsMast.Add(society);

        var photoType = new PropertyPhotoTypeEntity
        {
            PhotoTypeCode = "SOCIETY_PLACE",
            PhotoScope = "SOCIETY",
            IsActive = true
        };
        context.PropertyPhotoTypes.Add(photoType);

        var document = new DocumentEntity
        {
            DocumentGuid = Guid.NewGuid(),
            DocumentType = "SOCIETY_PLACE",
            FileName = "society_photo.jpg",
            OriginalFileName = "society_photo.jpg",
            FileExtension = ".jpg",
            MimeType = "image/jpeg",
            StoragePath = "Uploads/society_photo.jpg",
            IsActive = true
        };
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var binding = new DocumentBindingEntity
        {
            DocumentId = document.Id,
            ReferenceTableName = "PropertyPhoto",
            ReferencePropertyName = "SocietyDetailId",
            ReferenceTableId = society.Id,
            BindingPurpose = "Society Place Photo",
            IsActive = true
        };
        context.DocumentBindings.Add(binding);
        await context.SaveChangesAsync();

        var handler = new PropertyPhotoDocumentBindingHandler(photoService.Object, context, logger.Object);

        await handler.OnAfterUploadAsync(document.Id, binding.Id, society.Id, 42, CancellationToken.None);

        var createdPhoto = await context.PropertyPhotos.SingleAsync(p => p.DocumentBindingId == binding.Id);
        Assert.Equal("S", createdPhoto.EntityType);
        Assert.Equal(society.Id, createdPhoto.SocietyDetailId);
        Assert.Null(createdPhoto.WingDetailId);
        Assert.Null(createdPhoto.PropertyId);
    }

    [Fact]
    public async Task OnAfterUploadAsync_ReplaceExistingPhoto_ResolvesWingAndSocietyDetails()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var photoService = new Mock<IPropertyPhotoService>();
        var logger = new Mock<ILogger<PropertyPhotoDocumentBindingHandler>>();

        var wing = new WingDetailsMastEntity
        {
            SocietyDetailsMastId = 21,
            WingMasterId = 1,
            IsActive = true
        };
        context.Set<WingDetailsMastEntity>().Add(wing);
        await context.SaveChangesAsync();

        var property = new PropertyEntity
        {
            PropertyNo = "85",
            WingDetailId = wing.Id,
            IsActive = true
        };
        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var photoType = new PropertyPhotoTypeEntity
        {
            PhotoTypeCode = "WING_BUILDING",
            PhotoScope = "WING",
            IsActive = true
        };
        context.PropertyPhotoTypes.Add(photoType);
        await context.SaveChangesAsync();

        var existingPhoto = PropertyPhotoEntity.CreateWithDetails(
            propertyId: property.Id,
            photoTypeId: photoType.Id,
            entityType: "P", // legacy
            societyDetailId: null,
            wingDetailId: null,
            documentBindingId: null,
            displayOrder: 1,
            remarks: "Old Wing Photo"
        );
        context.PropertyPhotos.Add(existingPhoto);
        await context.SaveChangesAsync();

        var document = new DocumentEntity
        {
            DocumentGuid = Guid.NewGuid(),
            DocumentType = "WING_BUILDING",
            FileName = "replaced_photo.jpg",
            OriginalFileName = "replaced_photo.jpg",
            FileExtension = ".jpg",
            MimeType = "image/jpeg",
            StoragePath = "Uploads/replaced_photo.jpg",
            IsActive = true
        };
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var binding = new DocumentBindingEntity
        {
            DocumentId = document.Id,
            ReferenceTableName = "PropertyPhoto",
            ReferencePropertyName = "PropertyPhotoId",
            ReferenceTableId = existingPhoto.Id,
            BindingPurpose = "Replaced Wing Photo",
            IsActive = true
        };
        context.DocumentBindings.Add(binding);
        await context.SaveChangesAsync();

        var handler = new PropertyPhotoDocumentBindingHandler(photoService.Object, context, logger.Object);

        // Act
        await handler.OnAfterUploadAsync(
            document.Id, // documentId
            binding.Id, // bindingId
            existingPhoto.Id, // referenceTableId (PropertyPhotoId)
            42, // uploadedBy
            CancellationToken.None);

        // Assert
        // Reclassifying to Wing scope on replace must also null PropertyId, same as a brand new
        // Wing-scope upload -- otherwise a legacy row could keep a stale PropertyId forever.
        var updatedPhoto = await context.PropertyPhotos.FirstOrDefaultAsync(p => p.Id == existingPhoto.Id);
        Assert.NotNull(updatedPhoto);
        Assert.Equal("W", updatedPhoto.EntityType);
        Assert.Equal(wing.Id, updatedPhoto.WingDetailId);
        Assert.Equal(21, updatedPhoto.SocietyDetailId);
        Assert.Null(updatedPhoto.PropertyId);

        // Verify UpdateDocumentBindingAsync was called
        photoService.Verify(s => s.UpdateDocumentBindingAsync(existingPhoto.Id, binding.Id, 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async Task<(DocumentEntity Document, DocumentBindingEntity Binding)> SeedPropertyPlanUploadAsync(
        ApplicationDbContext context, int propertyId, PropertyPhotoTypeEntity photoType, string fileSuffix)
    {
        var document = new DocumentEntity
        {
            DocumentGuid = Guid.NewGuid(),
            DocumentType = photoType.PhotoTypeCode,
            FileName = $"plan_{fileSuffix}.jpg",
            OriginalFileName = $"plan_{fileSuffix}.jpg",
            FileExtension = ".jpg",
            MimeType = "image/jpeg",
            StoragePath = $"Uploads/plan_{fileSuffix}.jpg",
            IsActive = true
        };
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var binding = new DocumentBindingEntity
        {
            DocumentId = document.Id,
            ReferenceTableName = "PropertyPhoto",
            ReferencePropertyName = "PropertyId",
            ReferenceTableId = propertyId,
            BindingPurpose = "Plan",
            IsActive = true
        };
        context.DocumentBindings.Add(binding);
        await context.SaveChangesAsync();

        return (document, binding);
    }

    [Fact]
    public async Task OnAfterUploadAsync_PropertyPlan_NonApartmentProperty_BindsToPropertyIdOnlyWithNullSocietyAndType()
    {
        var context = GetInMemoryDbContext();
        var photoService = new Mock<IPropertyPhotoService>();
        var logger = new Mock<ILogger<PropertyPhotoDocumentBindingHandler>>();

        // CategoryId = 2 (Individual), i.e. not Apartment (1)
        var property = new PropertyEntity { PropertyNo = "901", CategoryId = 2, Type = "STANDALONE", IsActive = true };
        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var photoType = new PropertyPhotoTypeEntity { PhotoTypeCode = "PROPERTY_PLAN", PhotoScope = "PROPERTY", IsActive = true };
        context.PropertyPhotoTypes.Add(photoType);
        await context.SaveChangesAsync();

        var (document, binding) = await SeedPropertyPlanUploadAsync(context, property.Id, photoType, "individual");

        var handler = new PropertyPhotoDocumentBindingHandler(photoService.Object, context, logger.Object);

        await handler.OnAfterUploadAsync(document.Id, binding.Id, property.Id, 42, CancellationToken.None);

        var createdPhoto = await context.PropertyPhotos.FirstOrDefaultAsync(p => p.PhotoTypeId == photoType.Id);
        Assert.NotNull(createdPhoto);
        Assert.Equal("P", createdPhoto.EntityType);
        Assert.Equal(property.Id, createdPhoto.PropertyId);
        Assert.Null(createdPhoto.SocietyDetailId);
        Assert.Null(createdPhoto.Type);
        Assert.Equal(binding.Id, createdPhoto.DocumentBindingId);
    }

    [Fact]
    public async Task OnAfterUploadAsync_PropertyPlan_ApartmentAmenity_BindsToPropertyIdAndSocietyIdWithNullType()
    {
        var context = GetInMemoryDbContext();
        var photoService = new Mock<IPropertyPhotoService>();
        var logger = new Mock<ILogger<PropertyPhotoDocumentBindingHandler>>();

        // CategoryId = 1 (Apartment), PropertyTypeId = 140 (Amenity)
        var amenityProperty = new PropertyEntity { PropertyNo = "902", CategoryId = 1, PropertyTypeId = 140, Type = "CLUBHOUSE", IsActive = true };
        context.PropertyMast.Add(amenityProperty);
        await context.SaveChangesAsync();

        var society = new SocietyDetailsEntity { PropertyId = amenityProperty.Id, SocietyName = "Test Society", IsActive = true };
        context.SocietyDetailsMast.Add(society);
        await context.SaveChangesAsync();

        var photoType = new PropertyPhotoTypeEntity { PhotoTypeCode = "PROPERTY_PLAN", PhotoScope = "PROPERTY", IsActive = true };
        context.PropertyPhotoTypes.Add(photoType);
        await context.SaveChangesAsync();

        var (document, binding) = await SeedPropertyPlanUploadAsync(context, amenityProperty.Id, photoType, "amenity");

        var handler = new PropertyPhotoDocumentBindingHandler(photoService.Object, context, logger.Object);

        await handler.OnAfterUploadAsync(document.Id, binding.Id, amenityProperty.Id, 42, CancellationToken.None);

        var createdPhoto = await context.PropertyPhotos.FirstOrDefaultAsync(p => p.PhotoTypeId == photoType.Id);
        Assert.NotNull(createdPhoto);
        Assert.Equal("P", createdPhoto.EntityType);
        Assert.Equal(amenityProperty.Id, createdPhoto.PropertyId);
        Assert.Equal(society.Id, createdPhoto.SocietyDetailId);
        Assert.Null(createdPhoto.Type);
        Assert.Equal(binding.Id, createdPhoto.DocumentBindingId);
    }

    [Fact]
    public async Task OnAfterUploadAsync_PropertyPlan_ApartmentNormalUnit_BindsToSocietyAndTypeWithNullPropertyId()
    {
        var context = GetInMemoryDbContext();
        var photoService = new Mock<IPropertyPhotoService>();
        var logger = new Mock<ILogger<PropertyPhotoDocumentBindingHandler>>();

        var society = new SocietyDetailsEntity { SocietyName = "Green Meadows", IsActive = true };
        context.SocietyDetailsMast.Add(society);
        await context.SaveChangesAsync();

        var wing = new WingDetailsMastEntity { SocietyDetailsMastId = society.Id, WingMasterId = 1, IsActive = true };
        context.Set<WingDetailsMastEntity>().Add(wing);
        await context.SaveChangesAsync();

        // CategoryId = 1 (Apartment), PropertyTypeId != 140 (a normal residential unit)
        var unit = new PropertyEntity { PropertyNo = "A-101", CategoryId = 1, PropertyTypeId = 5, Type = "2BHK", WingDetailId = wing.Id, FlatOrShopNo = "A-101", IsActive = true };
        context.PropertyMast.Add(unit);
        await context.SaveChangesAsync();

        var photoType = new PropertyPhotoTypeEntity { PhotoTypeCode = "PROPERTY_PLAN", PhotoScope = "PROPERTY", IsActive = true };
        context.PropertyPhotoTypes.Add(photoType);
        await context.SaveChangesAsync();

        var (document, binding) = await SeedPropertyPlanUploadAsync(context, unit.Id, photoType, "unit-a101");

        var handler = new PropertyPhotoDocumentBindingHandler(photoService.Object, context, logger.Object);

        await handler.OnAfterUploadAsync(document.Id, binding.Id, unit.Id, 42, CancellationToken.None);

        var createdPhoto = await context.PropertyPhotos.FirstOrDefaultAsync(p => p.PhotoTypeId == photoType.Id);
        Assert.NotNull(createdPhoto);
        Assert.Equal("S", createdPhoto.EntityType);
        Assert.Null(createdPhoto.PropertyId);
        Assert.Equal(society.Id, createdPhoto.SocietyDetailId);
        Assert.Equal("2BHK", createdPhoto.Type);
        Assert.Equal(binding.Id, createdPhoto.DocumentBindingId);
    }

    [Fact]
    public async Task OnAfterUploadAsync_PropertyPlan_SecondUnitSameSocietyAndType_ReusesExistingSharedPhotoInsteadOfDuplicating()
    {
        var context = GetInMemoryDbContext();
        var photoService = new Mock<IPropertyPhotoService>();
        var logger = new Mock<ILogger<PropertyPhotoDocumentBindingHandler>>();

        var society = new SocietyDetailsEntity { SocietyName = "Green Meadows", IsActive = true };
        context.SocietyDetailsMast.Add(society);
        await context.SaveChangesAsync();

        var wing = new WingDetailsMastEntity { SocietyDetailsMastId = society.Id, WingMasterId = 1, IsActive = true };
        context.Set<WingDetailsMastEntity>().Add(wing);
        await context.SaveChangesAsync();

        var unit1 = new PropertyEntity { PropertyNo = "A-101", CategoryId = 1, PropertyTypeId = 5, Type = "2BHK", WingDetailId = wing.Id, FlatOrShopNo = "A-101", IsActive = true };
        var unit2 = new PropertyEntity { PropertyNo = "A-102", CategoryId = 1, PropertyTypeId = 5, Type = "2BHK", WingDetailId = wing.Id, FlatOrShopNo = "A-102", IsActive = true };
        context.PropertyMast.AddRange(unit1, unit2);
        await context.SaveChangesAsync();

        var photoType = new PropertyPhotoTypeEntity { PhotoTypeCode = "PROPERTY_PLAN", PhotoScope = "PROPERTY", IsActive = true };
        context.PropertyPhotoTypes.Add(photoType);
        await context.SaveChangesAsync();

        var handler = new PropertyPhotoDocumentBindingHandler(photoService.Object, context, logger.Object);

        var (document1, binding1) = await SeedPropertyPlanUploadAsync(context, unit1.Id, photoType, "unit-a101");
        await handler.OnAfterUploadAsync(document1.Id, binding1.Id, unit1.Id, 42, CancellationToken.None);

        var firstPhoto = await context.PropertyPhotos.SingleAsync(p => p.PhotoTypeId == photoType.Id);

        var (document2, binding2) = await SeedPropertyPlanUploadAsync(context, unit2.Id, photoType, "unit-a102");
        await handler.OnAfterUploadAsync(document2.Id, binding2.Id, unit2.Id, 42, CancellationToken.None);

        // Still exactly one PropertyPhoto row for this (SocietyDetailId, PhotoTypeId, Type) combo --
        // the second unit's upload reused it rather than creating a duplicate.
        var allPlanPhotos = await context.PropertyPhotos.Where(p => p.PhotoTypeId == photoType.Id).ToListAsync();
        var sharedPhoto = Assert.Single(allPlanPhotos);
        Assert.Equal(firstPhoto.Id, sharedPhoto.Id);
        Assert.Equal(binding2.Id, sharedPhoto.DocumentBindingId);

        var reloadedBinding2 = await context.DocumentBindings.FirstAsync(b => b.Id == binding2.Id);
        Assert.Equal(sharedPhoto.Id, reloadedBinding2.ReferenceTableId);
        Assert.Equal("Id", reloadedBinding2.ReferencePropertyName);
    }
}
