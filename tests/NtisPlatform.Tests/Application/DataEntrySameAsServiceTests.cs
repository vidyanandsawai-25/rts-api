using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.DataEntrySameAs;
using NtisPlatform.Application.Services;
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
}
