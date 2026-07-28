using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.DataEntrySameAs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for <see cref="DataEntrySameAsService"/>, focused on the FilterType parsing/dispatch
/// contract — in particular the comma-separated multi-mode support (e.g. "PARKING,PROPERTYWISE"),
/// where each listed mode acts independently within the one transaction.
///
/// The copy path is exercised without touching <c>ExecuteUpdateAsync</c> (unsupported by MockQueryable)
/// by giving destinations no existing PropertyDetails, so the soft-delete step early-returns while the
/// copy step still runs.
/// </summary>
public class DataEntrySameAsServiceTests
{
    private const int SourcePropertyId = 1;
    private const int DestinationPropertyId = 2;
    private const int ParkingTypeOfUseId = 99;
    private const int NonParkingTypeOfUseId = 1;
    private const int UpdatedBy = 42;

    private readonly Mock<IRepository<PropertyEntity, int>> _propertyRepo = new();
    private readonly Mock<IRepository<PropertyDetailsEntity, int>> _propertyDetailsRepo = new();
    private readonly Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>> _roomSubmissionRepo = new();
    private readonly Mock<IRepository<RoomWiseMinusDataEntity, int>> _roomMinusRepo = new();
    private readonly Mock<IRepository<ParkingTypeMasterEntity, int>> _parkingTypeRepo = new();
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _societyRepo = new();
    private readonly Mock<IRepository<WingEntity, int>> _wingRepo = new();
    private readonly Mock<IRepository<BuildingPlanTypeEntity, int>> _buildingPlanTypeRepo = new();
    private readonly Mock<IRepository<WardEntity, int>> _wardRepo = new();
    private readonly Mock<IRepository<ZoneEntity, int>> _zoneRepo = new();
    private readonly Mock<IRepository<PropertyTypeMasterEntity, int>> _propertyTypeRepo = new();
    private readonly Mock<IRepository<PropertyCategoryEntity, int>> _propertyCategoryRepo = new();
    private readonly Mock<IRepository<TypeOfUseEntity, int>> _typeOfUseRepo = new();
    private readonly Mock<IRepository<TypeOfUseCategoryEntity, int>> _typeOfUseCategoryRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<DataEntrySameAsService>> _logger = new();

    private DataEntrySameAsService CreateService(
        List<PropertyEntity>? properties = null,
        List<PropertyDetailsEntity>? propertyDetails = null,
        List<ParkingTypeMasterEntity>? parkingTypes = null)
    {
        properties ??= new List<PropertyEntity>
        {
            new() { Id = SourcePropertyId, WardId = 10, PropertyNo = "P1", PartitionNo = "A", Type = "1" },
            new() { Id = DestinationPropertyId, WardId = 10, PropertyNo = "P1", PartitionNo = "B" }
        };
        parkingTypes ??= new List<ParkingTypeMasterEntity> { new() { TypeOfUseId = ParkingTypeOfUseId } };
        propertyDetails ??= new List<PropertyDetailsEntity>();

        _propertyRepo.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _propertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(propertyDetails.BuildMock());
        _roomSubmissionRepo.Setup(r => r.GetQueryable()).Returns(new List<RoomWiseSubmissionDetailsEntity>().BuildMock());
        _roomMinusRepo.Setup(r => r.GetQueryable()).Returns(new List<RoomWiseMinusDataEntity>().BuildMock());
        _parkingTypeRepo.Setup(r => r.GetQueryable()).Returns(parkingTypes.BuildMock());
        _societyRepo.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity>().BuildMock());
        _wingRepo.Setup(r => r.GetQueryable()).Returns(new List<WingEntity>().BuildMock());
        _buildingPlanTypeRepo.Setup(r => r.GetQueryable()).Returns(new List<BuildingPlanTypeEntity>().BuildMock());
        _wardRepo.Setup(r => r.GetQueryable()).Returns(new List<WardEntity>().BuildMock());
        _zoneRepo.Setup(r => r.GetQueryable()).Returns(new List<ZoneEntity>().BuildMock());
        _propertyTypeRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyTypeMasterEntity>().BuildMock());
        _propertyCategoryRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCategoryEntity>().BuildMock());
        _typeOfUseRepo.Setup(r => r.GetQueryable()).Returns(new List<TypeOfUseEntity>().BuildMock());
        _typeOfUseCategoryRepo.Setup(r => r.GetQueryable()).Returns(new List<TypeOfUseCategoryEntity>().BuildMock());

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _propertyDetailsRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PropertyDetailsEntity>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _roomSubmissionRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<RoomWiseSubmissionDetailsEntity>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _roomMinusRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<RoomWiseMinusDataEntity>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        return new DataEntrySameAsService(
            _propertyRepo.Object,
            _propertyDetailsRepo.Object,
            _roomSubmissionRepo.Object,
            _roomMinusRepo.Object,
            _parkingTypeRepo.Object,
            _societyRepo.Object,
            _wingRepo.Object,
            _buildingPlanTypeRepo.Object,
            _wardRepo.Object,
            _zoneRepo.Object,
            _propertyTypeRepo.Object,
            _propertyCategoryRepo.Object,
            _typeOfUseRepo.Object,
            _typeOfUseCategoryRepo.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    /// <summary>One active source PropertyDetail per requested TypeOfUse, all owned by the source property.</summary>
    private static List<PropertyDetailsEntity> SourceDetails(params int[] typeOfUseIds)
        => typeOfUseIds
            .Select((t, i) => new PropertyDetailsEntity
            {
                Id = 1000 + i,
                PropertyId = SourcePropertyId,
                TypeOfUseId = t,
                IsActive = true,
                MarkedForDeletion = false
            })
            .ToList();

    private static DataEntrySameAsRequestDto Request(string filterType, int type = 0) => new()
    {
        SourcePropertyId = SourcePropertyId,
        DestinationPropertyIds = new List<int> { DestinationPropertyId },
        FilterType = filterType,
        Type = type
    };

    // ── FilterType validation (runs before any DB access) ──────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",")]
    public async Task ExecuteAsync_EmptyOrBlankFilterType_Throws(string filterType)
    {
        var service = CreateService();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExecuteAsync(Request(filterType), UpdatedBy));

        Assert.Contains("FilterType", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidFilterType_ThrowsAndNamesTheOffendingValue()
    {
        var service = CreateService();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExecuteAsync(Request("BOGUS"), UpdatedBy));

        Assert.Contains("BOGUS", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_MixOfValidAndInvalidFilterTypes_ThrowsNamingOnlyTheInvalidOne()
    {
        var service = CreateService();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExecuteAsync(Request("PARKING,BOGUS"), UpdatedBy));

        Assert.Contains("BOGUS", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_FilterTypeIsCaseInsensitiveAndTrimmed()
    {
        var service = CreateService(propertyDetails: SourceDetails(ParkingTypeOfUseId));

        var result = await service.ExecuteAsync(Request("  parking  "), UpdatedBy);

        Assert.Equal(1, result.PropertyDetailsCopied);
    }

    // ── Multi-mode dispatch: the reason for this change ───────────────────────

    [Fact]
    public async Task ExecuteAsync_ParkingOnly_CopiesOnlyParkingRows()
    {
        var service = CreateService(
            propertyDetails: SourceDetails(ParkingTypeOfUseId, NonParkingTypeOfUseId));

        var result = await service.ExecuteAsync(Request("PARKING"), UpdatedBy);

        Assert.Equal(1, result.PropertyDetailsCopied);
    }

    [Fact]
    public async Task ExecuteAsync_PropertywiseOnly_CopiesOnlyNonParkingRows()
    {
        var service = CreateService(
            propertyDetails: SourceDetails(ParkingTypeOfUseId, NonParkingTypeOfUseId));

        var result = await service.ExecuteAsync(Request("PROPERTYWISE"), UpdatedBy);

        Assert.Equal(1, result.PropertyDetailsCopied);
    }

    [Fact]
    public async Task ExecuteAsync_ParkingAndPropertywise_CopiesBothParkingAndNonParkingRows()
    {
        var service = CreateService(
            propertyDetails: SourceDetails(ParkingTypeOfUseId, NonParkingTypeOfUseId));

        var result = await service.ExecuteAsync(Request("PARKING,PROPERTYWISE"), UpdatedBy);

        // Both modes ran and their counts accumulated (parking row + non-parking row).
        Assert.Equal(2, result.PropertyDetailsCopied);
        Assert.Equal(1, result.ProcessedDestinations);
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateFilterTypes_AreDedupedAndRunOnce()
    {
        var service = CreateService(
            propertyDetails: SourceDetails(ParkingTypeOfUseId, NonParkingTypeOfUseId));

        var result = await service.ExecuteAsync(Request("PARKING,PARKING"), UpdatedBy);

        // Deduped to a single PARKING pass — the parking row is copied once, not twice.
        Assert.Equal(1, result.PropertyDetailsCopied);
    }

    [Fact]
    public async Task ExecuteAsync_MultiMode_RunsInsideASingleTransaction()
    {
        var service = CreateService(
            propertyDetails: SourceDetails(ParkingTypeOfUseId, NonParkingTypeOfUseId));

        await service.ExecuteAsync(Request("PARKING,PROPERTYWISE"), UpdatedBy);

        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Guard rails unchanged by this feature ─────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SourceNotFound_Throws()
    {
        // Only the destination exists in the property set.
        var service = CreateService(properties: new List<PropertyEntity>
        {
            new() { Id = DestinationPropertyId, WardId = 10, PropertyNo = "P1", PartitionNo = "B" }
        });

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExecuteAsync(Request("PARKING"), UpdatedBy));

        Assert.Contains("Source property", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_NoValidDestinations_Throws()
    {
        var service = CreateService();
        var request = Request("PARKING");
        // Destination equals the source, so it is dropped as a self-reference.
        request.DestinationPropertyIds = new List<int> { SourcePropertyId };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExecuteAsync(request, UpdatedBy));
    }

    // ── TYPEWISE self-type-change: source listed as its own destination ───────
    //
    // NOTE: the *successful* self-change path stamps the source Type via ExecuteUpdateAsync, which
    // MockQueryable cannot execute (see the class header). Only the validation/rejection branches —
    // which throw before the transaction — are unit-testable here; the DB write is verified manually.

    [Fact]
    public async Task ExecuteAsync_TypewiseSelfChange_WithoutType_Throws()
    {
        var service = CreateService();
        // Source is its own (and only) destination, but no new Type (1-99) supplied.
        var request = Request("TYPEWISE", type: 0);
        request.DestinationPropertyIds = new List<int> { SourcePropertyId };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExecuteAsync(request, UpdatedBy));

        Assert.Contains("Type between 1 and 99", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_SelfReferenceCombinedWithTypewise_StillRejected()
    {
        var service = CreateService();
        // TYPEWISE is not the sole filter, so the self-change relaxation does not apply.
        var request = Request("TYPEWISE,PARKING", type: 5);
        request.DestinationPropertyIds = new List<int> { SourcePropertyId };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExecuteAsync(request, UpdatedBy));

        Assert.Contains("No valid destination properties supplied", ex.Message);
    }

    // ── GetSiblingPropertiesAsync: unmatched-wing rows must not duplicate ──────

    [Fact]
    public async Task GetSiblingPropertiesAsync_DropsUnmatchedWingRows_ReturnsNoDuplicate()
    {
        // A property with two society rows: one whose WingId matches a WingMaster (WingNo differs from
        // the partition, so it is kept) and one whose WingId matches nothing (unmatched left join → null
        // wing number). Raw SQL drops the unmatched row (`PartitionNo != NULL` is UNKNOWN); the service
        // must do the same instead of emitting a duplicate. (Pre-fix, `pm.PartitionNo != wm.WingNo`
        // relied on EF's C# null-semantics and kept the unmatched row.)
        const int propertyId = 579097;
        var properties = new List<PropertyEntity>
        {
            new() { Id = propertyId, WardId = 18, PropertyNo = "1", PartitionNo = "A", Type = "4" }
        };
        var society = new List<SocietyDetailsEntity>
        {
            new() { PropertyId = propertyId, WingId = 5, WingName = "GG" },    // matches WingMaster 5
            new() { PropertyId = propertyId, WingId = 999, WingName = null }   // no matching WingMaster
        };
        var wings = new List<WingEntity> { new() { Id = 5, WingNo = "B" } };   // WingNo "B" != partition "A"
        var details = new List<PropertyDetailsEntity>
        {
            new() { Id = 1, PropertyId = propertyId, IsActive = true, MarkedForDeletion = false,
                    CarpetAreaSqMeter = 38.72, CarpetAreaSqFeet = 416.78 }
        };

        var service = CreateService(properties: properties, propertyDetails: details);
        _societyRepo.Setup(r => r.GetQueryable()).Returns(society.BuildMock());
        _wingRepo.Setup(r => r.GetQueryable()).Returns(wings.BuildMock());

        var result = await service.GetSiblingPropertiesAsync(
            new DataEntrySameAsQueryParameters { WardId = 18, PropertyNo = "1", PartitionNo = "A" });

        var row = Assert.Single(result);
        Assert.Equal(propertyId, row.PropertyId);
        Assert.Equal("GG", row.WingName);
        Assert.Equal(38.72, row.CarpetAreaSqMeter);
    }

    // ── GetPropertyUnitsAsync: excludes amenity/wing rows; totals + parking split ──

    [Fact]
    public async Task GetPropertyUnitsAsync_ExcludesAmenityAndWingRows_SplitsTotalAndParkingAreas()
    {
        const int residentialTypeId = 1;
        const int amenityTypeId = 2;
        const int parkingTypeOfUseId = 20;      // maps to the PARKING category
        const int residentialTypeOfUseId = 10;  // maps to a non-parking category

        var wards = new List<WardEntity> { new() { Id = 18, WardNo = "18", ZoneId = 3 } };
        var zones = new List<ZoneEntity> { new() { Id = 3, ZoneNo = "Z3" } };
        var propertyTypes = new List<PropertyTypeMasterEntity>
        {
            new() { Id = residentialTypeId, PartType = PartTypeConstants.Residential },
            new() { Id = amenityTypeId, PartType = PartTypeConstants.Amenity }
        };
        var categories = new List<PropertyCategoryEntity>
        {
            new() { Id = 7, PropertyCategoryName = "Residential" },
            new() { Id = 8, PropertyCategoryName = PropertyConstants.Categories.Apartment }
        };
        var wings = new List<WingEntity> { new() { Id = 5, WingNo = "GG" } };
        var typeOfUses = new List<TypeOfUseEntity>
        {
            new() { Id = residentialTypeOfUseId, TypeOfUseCategoryId = 100 },
            new() { Id = parkingTypeOfUseId, TypeOfUseCategoryId = 200 }
        };
        var typeOfUseCategories = new List<TypeOfUseCategoryEntity>
        {
            new() { Id = 100, TypeOfUseCategoryCode = "RES" },
            new() { Id = 200, TypeOfUseCategoryCode = TypeOfUseConstants.Parking }
        };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 100, WardId = 18, PropertyNo = "1", PartitionNo = "A1", TaxZoneId = 50,
                    PropertyTypeId = residentialTypeId, CategoryId = 7, Type = "4", FlatOrShopNo = "101" },
            new() { Id = 101, WardId = 18, PropertyNo = "1", PartitionNo = "A2", TaxZoneId = 50,
                    PropertyTypeId = amenityTypeId, CategoryId = 7 },                 // amenity -> excluded
            new() { Id = 102, WardId = 18, PropertyNo = "1", PartitionNo = "GG", TaxZoneId = 50,
                    PropertyTypeId = residentialTypeId, CategoryId = 7 },              // wing -> excluded
            new() { Id = 103, WardId = 18, PropertyNo = "1", PartitionNo = "", TaxZoneId = 50,
                    PropertyTypeId = residentialTypeId, CategoryId = 8 },              // Apartment + blank partition -> excluded
            new() { Id = 104, WardId = 18, PropertyNo = "1", PartitionNo = "A3", TaxZoneId = 50,
                    PropertyTypeId = residentialTypeId, CategoryId = 8 }               // Apartment + non-blank partition -> included
        };
        var details = new List<PropertyDetailsEntity>
        {
            // property 100: one non-parking detail + one parking detail
            new() { Id = 1, PropertyId = 100, TypeOfUseId = residentialTypeOfUseId, IsActive = true, MarkedForDeletion = false,
                    CarpetAreaSqMeter = 10.5, CarpetAreaSqFeet = 110, BuiltupAreaSqMeter = 12, BuiltupAreaSqFeet = 120 },
            new() { Id = 2, PropertyId = 100, TypeOfUseId = parkingTypeOfUseId, IsActive = true, MarkedForDeletion = false,
                    CarpetAreaSqMeter = 4.5, CarpetAreaSqFeet = 40, BuiltupAreaSqMeter = 5, BuiltupAreaSqFeet = 50 },
            // excluded properties' details (should never reach the result)
            new() { Id = 3, PropertyId = 101, TypeOfUseId = residentialTypeOfUseId, IsActive = true, MarkedForDeletion = false, CarpetAreaSqMeter = 99 },
            new() { Id = 4, PropertyId = 102, TypeOfUseId = residentialTypeOfUseId, IsActive = true, MarkedForDeletion = false, CarpetAreaSqMeter = 99 },
            new() { Id = 5, PropertyId = 103, TypeOfUseId = residentialTypeOfUseId, IsActive = true, MarkedForDeletion = false, CarpetAreaSqMeter = 99 },
            new() { Id = 6, PropertyId = 104, TypeOfUseId = residentialTypeOfUseId, IsActive = true, MarkedForDeletion = false, CarpetAreaSqMeter = 7 }
        };

        var service = CreateService(properties: properties, propertyDetails: details);
        _wardRepo.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());
        _zoneRepo.Setup(r => r.GetQueryable()).Returns(zones.BuildMock());
        _propertyTypeRepo.Setup(r => r.GetQueryable()).Returns(propertyTypes.BuildMock());
        _propertyCategoryRepo.Setup(r => r.GetQueryable()).Returns(categories.BuildMock());
        _wingRepo.Setup(r => r.GetQueryable()).Returns(wings.BuildMock());
        _typeOfUseRepo.Setup(r => r.GetQueryable()).Returns(typeOfUses.BuildMock());
        _typeOfUseCategoryRepo.Setup(r => r.GetQueryable()).Returns(typeOfUseCategories.BuildMock());

        var result = await service.GetPropertyUnitsAsync(
            new DataEntrySameAsUnitsQueryParameters { WardId = 18, PropertyNo = "1" });

        // amenity, wing, and blank-partition Apartment rows all dropped
        Assert.Equal(2, result.Count);
        var row = result.Single(r => r.PropertyId == 100);
        Assert.Equal(PartTypeConstants.Residential, row.PartType);
        Assert.Equal("Z3", row.ZoneNo);
        // Totals across both active details.
        Assert.Equal(15.0, row.TotalCarpetAreaSqMeter);   // 10.5 + 4.5
        Assert.Equal(150.0, row.TotalCarpetAreaSqFeet);   // 110 + 40
        Assert.Equal(17.0, row.TotalBuiltupAreaSqMeter);  // 12 + 5
        Assert.Equal(170.0, row.TotalBuiltupAreaSqFeet);  // 120 + 50
        // Parking slice = the parking detail only.
        Assert.Equal(4.5, row.ParkingCarpetAreaSqMeter);
        Assert.Equal(40.0, row.ParkingCarpetAreaSqFeet);
        Assert.Equal(5.0, row.ParkingBuiltupAreaSqMeter);
        Assert.Equal(50.0, row.ParkingBuiltupAreaSqFeet);

        // Apartment category with a non-blank partition is still included.
        var apartmentRow = result.Single(r => r.PropertyId == 104);
        Assert.Equal("A3", apartmentRow.PartitionNo);
        Assert.Equal(7.0, apartmentRow.TotalCarpetAreaSqMeter);
    }
}
