using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.Asset_Management;

/// <summary>
/// Integration tests for <see cref="SubUnitsDetailsService"/> against a real
/// <see cref="ApplicationDbContext"/> (EF Core InMemory provider), per CLAUDE.md Section 17
/// ("Add at least one integration test against a real DB ... per aggregate"). This service is a
/// poor fit for mock-heavy tests: GetAllAsync/GetByIdAsync/GetByAssetIdAsync all use
/// Include()+Select() projections and GroupBy over _repository.GetQueryable(), which a mocked
/// IQueryable can't exercise faithfully.
///
/// These tests also lock in the CalculateAndUpdateCapitalValueAsync removal: CreateAsync (with
/// RoomDetails) and CreateDirectRoomsAsync used to auto-recalculate CapitalValue as a side effect
/// via an ICVCalculationService dependency that was never implemented or registered in DI (dead,
/// unreachable code). Both call sites were removed together with the method; the tests below
/// assert CapitalValue stays null after those operations so the removal doesn't silently regress
/// back to depending on that dead service.
/// </summary>
[Trait("Category", "Integration")]
public class SubUnitsDetailsServiceIntegrationTests : IAsyncLifetime
{
    private ApplicationDbContext? _context;
    private Repository<SubUnitsDetailsEntity, int>? _repository;
    private Repository<AssetMasterEntity, int>? _assetRepository;
    private Repository<AssetRoomWiseSubmissionDetailsEntity, int>? _roomWiseRepository;
    private Repository<AssetLeaseRentDetailsEntity, int>? _leaseRentRepository;
    private Repository<AssetRoomWiseMinusDataEntity, int>? _minusRepository;
    private Repository<AssetTypeOfUseMasterEntity, int>? _amsTypeOfUseRepository;
    private Repository<AssetSubTypeOfUseEntity, int>? _amsSubTypeOfUseRepository;
    private UnitOfWork? _unitOfWork;
    private IMapper? _mapper;
    private Mock<IReferenceValidationService>? _referenceValidator;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"SubUnitsDetailsServiceIntegrationTests_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .EnableSensitiveDataLogging()
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new Repository<SubUnitsDetailsEntity, int>(_context);
        _assetRepository = new Repository<AssetMasterEntity, int>(_context);
        _roomWiseRepository = new Repository<AssetRoomWiseSubmissionDetailsEntity, int>(_context);
        _leaseRentRepository = new Repository<AssetLeaseRentDetailsEntity, int>(_context);
        _minusRepository = new Repository<AssetRoomWiseMinusDataEntity, int>(_context);
        _amsTypeOfUseRepository = new Repository<AssetTypeOfUseMasterEntity, int>(_context);
        _amsSubTypeOfUseRepository = new Repository<AssetSubTypeOfUseEntity, int>(_context);
        _unitOfWork = new UnitOfWork(_context);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<SubUnitsDetailsMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _referenceValidator = new Mock<IReferenceValidationService>();
        _referenceValidator
            .Setup(x => x.ValidateReferencesAsync<SubUnitsDetailsEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }

    private SubUnitsDetailsService CreateService() => new(
        _repository!, _unitOfWork!, _mapper!, _referenceValidator!.Object,
        _assetRepository!, _roomWiseRepository!, _leaseRentRepository!, _minusRepository!,
        _amsTypeOfUseRepository!, _amsSubTypeOfUseRepository!,
        new Mock<ILogger<SubUnitsDetailsService>>().Object);

    private async Task<AssetMasterEntity> SeedAssetAsync(int id, int? parentAssetId = null)
    {
        var asset = new AssetMasterEntity
        {
            Id = id,
            AssetNo = $"A-{id}",
            AssetName = $"Asset {id}",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            ParentAssetId = parentAssetId,
            IsActive = true,
            CreatedDate = DateTime.Now
        };
        _context!.Set<AssetMasterEntity>().Add(asset);
        await _context.SaveChangesAsync();
        return asset;
    }

    #region CreateAsync — Parent Building Placeholder Logic

    [Fact]
    public async Task CreateAsync_NonParentAsset_PersistsAndIsRetrievable()
    {
        await SeedAssetAsync(10);
        var service = CreateService();
        var dto = new CreateSubUnitsDetailsDto
        {
            AssetId = 10,
            FloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            CarpetAreaSqMeter = 50m,
            IsActive = true
        };

        var created = await service.CreateAsync(dto, CancellationToken.None);

        Assert.True(created.Id > 0);
        Assert.Equal(10, created.AssetId);

        var fetched = await service.GetByIdAsync(created.Id, CancellationToken.None);
        Assert.NotNull(fetched);
        Assert.Equal(50m, fetched!.CarpetAreaSqMeter);
    }

    [Fact]
    public async Task CreateAsync_ParentBuildingWithNoExistingChildDetail_ReturnsPlaceholderIdZero_DoesNotPersist()
    {
        await SeedAssetAsync(1);
        await SeedAssetAsync(2, parentAssetId: 1);
        var service = CreateService();
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 };

        var result = await service.CreateAsync(dto, CancellationToken.None);

        Assert.Equal(0, result.Id);
        Assert.Equal(1, result.AssetId);
        Assert.Equal(1, result.FloorId);
        Assert.True(result.IsActive);
        Assert.Equal(0, await _context!.Set<SubUnitsDetailsEntity>().CountAsync());
    }

    [Fact]
    public async Task CreateAsync_ParentBuildingWithExistingChildDetail_ReturnsExistingRowMappedToParentAssetId()
    {
        await SeedAssetAsync(1);
        await SeedAssetAsync(2, parentAssetId: 1);
        var existingChildDetail = new SubUnitsDetailsEntity
        {
            AssetId = 2,
            FloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            IsActive = true,
            CreatedDate = DateTime.Now
        };
        _context!.Set<SubUnitsDetailsEntity>().Add(existingChildDetail);
        await _context.SaveChangesAsync();

        var service = CreateService();
        var dto = new CreateSubUnitsDetailsDto { AssetId = 1, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 };

        var result = await service.CreateAsync(dto, CancellationToken.None);

        Assert.Equal(existingChildDetail.Id, result.Id);
        Assert.Equal(1, result.AssetId); // mapped back to the parent asset id, not the child's
    }

    #endregion

    #region CreateAsync — Room Details + CV Auto-Calc Removal Regression

    [Fact]
    public async Task CreateAsync_WithRoomDetailsAndOffsets_PersistsRoomWiseAndOffsetRows_WithoutCalculatingCapitalValue()
    {
        await SeedAssetAsync(10);
        var service = CreateService();
        var dto = new CreateSubUnitsDetailsDto
        {
            AssetId = 10,
            FloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            IsActive = true,
            CarpetAreaSqFeet = 500m,
            RoomDetails = new List<RoomDetailDto>
            {
                new()
                {
                    LengthMtr = 5,
                    WidthMtr = 4,
                    RoomNo = "1",
                    RoomType = "Shop",
                    Offsets = new List<RoomOffsetDto> { new() { Length = 1, Width = 1, AreaSqM = 1, Shape = "Rectangle" } }
                }
            }
        };

        var created = await service.CreateAsync(dto, CancellationToken.None);

        var room = await _context!.Set<AssetRoomWiseSubmissionDetailsEntity>()
            .SingleAsync(r => r.SubUnitsDetailsId == created.Id);
        Assert.Equal(10, room.AssetId);

        var offset = await _context.Set<AssetRoomWiseMinusDataEntity>()
            .SingleAsync(m => m.RoomWiseSubmissionId == room.Id);
        Assert.Equal(1, offset.AreaSqMtr);

        // Regression guard: CalculateAndUpdateCapitalValueAsync (and its ICVCalculationService
        // dependency) was removed. CreateAsync must no longer attempt any CV auto-calculation.
        var persisted = await _context.Set<SubUnitsDetailsEntity>().AsNoTracking().SingleAsync(f => f.Id == created.Id);
        Assert.Null(persisted.CapitalValue);
        Assert.Null(persisted.BaseValue);
        Assert.Null(created.CapitalValue);
    }

    #endregion

    #region GetByIdAsync / GetAllAsync — Names + SubAssetCount Projection

    [Fact]
    public async Task GetByIdAsync_ResolvesNamesAndComputesSubAssetCountExcludingInactiveRooms()
    {
        var asset = await SeedAssetAsync(10);
        var floor = new NtisPlatform.Core.Entities.FloorEntity { Description = "Ground Floor", CreatedDate = DateTime.Now };
        var constructionType = new ConstructionTypeEntity { Description = "RCC", CreatedDate = DateTime.Now };
        _context!.Set<NtisPlatform.Core.Entities.FloorEntity>().Add(floor);
        _context.Set<ConstructionTypeEntity>().Add(constructionType);
        await _context.SaveChangesAsync();

        var detail = new SubUnitsDetailsEntity
        {
            AssetId = asset.Id,
            FloorId = floor.Id,
            ConstructionTypeId = constructionType.Id,
            TypeOfUseId = 1,
            IsActive = true,
            CreatedDate = DateTime.Now
        };
        _context.Set<SubUnitsDetailsEntity>().Add(detail);
        await _context.SaveChangesAsync();

        _context.Set<AssetRoomWiseSubmissionDetailsEntity>().AddRange(
            new AssetRoomWiseSubmissionDetailsEntity { SubUnitsDetailsId = detail.Id, IsActive = true, MarkedForDeletion = false, CreatedDate = DateTime.Now },
            new AssetRoomWiseSubmissionDetailsEntity { SubUnitsDetailsId = detail.Id, IsActive = true, MarkedForDeletion = false, CreatedDate = DateTime.Now },
            new AssetRoomWiseSubmissionDetailsEntity { SubUnitsDetailsId = detail.Id, IsActive = false, MarkedForDeletion = true, CreatedDate = DateTime.Now });
        await _context.SaveChangesAsync();

        var service = CreateService();
        var fetched = await service.GetByIdAsync(detail.Id, CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal("Ground Floor", fetched!.Names.FloorName);
        Assert.Equal("RCC", fetched.Names.ConstructionTypeName);
        Assert.Equal("Asset 10", fetched.Names.AssetName);
        Assert.Equal(2, fetched.SubAssetCount);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult_WithCorrectTotalCount()
    {
        await SeedAssetAsync(10);
        var service = CreateService();
        await service.CreateAsync(new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true }, CancellationToken.None);
        await service.CreateAsync(new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 2, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true }, CancellationToken.None);

        var result = await service.GetAllAsync(new SubUnitsDetailsQueryParameters { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
    }

    #endregion

    #region GetByAssetIdAsync — Cross-Child Aggregation

    [Fact]
    public async Task GetByAssetIdAsync_AggregatesAcrossChildAssets_AndMapsBackToParentAssetId()
    {
        await SeedAssetAsync(1);
        await SeedAssetAsync(2, parentAssetId: 1);
        await SeedAssetAsync(3, parentAssetId: 1);

        _context!.Set<SubUnitsDetailsEntity>().AddRange(
            new SubUnitsDetailsEntity { AssetId = 2, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, CarpetAreaSqMeter = 40m, CapitalValue = 1000m, BaseValue = 800m, CreatedDate = DateTime.Now },
            new SubUnitsDetailsEntity { AssetId = 3, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, CarpetAreaSqMeter = 60m, CapitalValue = 2000m, BaseValue = 1600m, CreatedDate = DateTime.Now });
        await _context.SaveChangesAsync();

        var service = CreateService();
        var summary = await service.GetByAssetIdAsync(1, CancellationToken.None);

        Assert.Equal(1, summary.TotalFloors);
        var floorDetail = Assert.Single(summary.FloorDetails);
        Assert.Equal(1, floorDetail.AssetId); // mapped back to the parent, not the child rows
        Assert.Equal(100m, floorDetail.CarpetAreaSqMeter);
        Assert.Equal(2, floorDetail.SubAssetCount);
        Assert.Equal(3000m, summary.TotalCapitalValue);
        Assert.Equal(2400m, summary.TotalBaseValue);
    }

    #endregion

    #region UpdateAsync — Deactivation Guarded By IReferenceValidationService

    [Fact]
    public async Task UpdateAsync_DeactivationBlockedWhenReferenced_ThrowsValidationExceptionAndDoesNotPersist()
    {
        await SeedAssetAsync(10);
        var service = CreateService();
        var created = await service.CreateAsync(
            new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true },
            CancellationToken.None);

        _referenceValidator!
            .Setup(x => x.ValidateReferencesAsync<SubUnitsDetailsEntity>(created.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("SubUnitsDetails_Referenced"));

        var updateDto = new UpdateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = false };

        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(created.Id, updateDto, CancellationToken.None));

        var persisted = await _context!.Set<SubUnitsDetailsEntity>().AsNoTracking().SingleAsync(f => f.Id == created.Id);
        Assert.True(persisted.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_DeactivationAllowedWhenNotReferenced_PersistsIsActiveFalse()
    {
        await SeedAssetAsync(10);
        var service = CreateService();
        var created = await service.CreateAsync(
            new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true },
            CancellationToken.None);

        var updateDto = new UpdateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = false };
        var updated = await service.UpdateAsync(created.Id, updateDto, CancellationToken.None);

        Assert.NotNull(updated);
        Assert.False(updated!.IsActive);
        var persisted = await _context!.Set<SubUnitsDetailsEntity>().AsNoTracking().SingleAsync(f => f.Id == created.Id);
        Assert.False(persisted.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ReturnsNull()
    {
        var service = CreateService();
        var updateDto = new UpdateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 };

        var result = await service.UpdateAsync(999, updateDto, CancellationToken.None);

        Assert.Null(result);
    }

    #endregion

    #region DeleteAsync — Soft Delete Guarded By IReferenceValidationService

    [Fact]
    public async Task DeleteAsync_Referenced_ThrowsValidationExceptionAndRowRemainsActive()
    {
        await SeedAssetAsync(10);
        var service = CreateService();
        var created = await service.CreateAsync(
            new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true },
            CancellationToken.None);

        _referenceValidator!
            .Setup(x => x.ValidateReferencesAsync<SubUnitsDetailsEntity>(created.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("SubUnitsDetails_Referenced"));

        await Assert.ThrowsAsync<ValidationException>(() => service.DeleteAsync(created.Id, CancellationToken.None));

        var persisted = await _context!.Set<SubUnitsDetailsEntity>().AsNoTracking().SingleAsync(f => f.Id == created.Id);
        Assert.True(persisted.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_NotReferenced_SoftDeletes_RowStillExistsWithIsActiveFalse()
    {
        await SeedAssetAsync(10);
        var service = CreateService();
        var created = await service.CreateAsync(
            new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true },
            CancellationToken.None);

        var result = await service.DeleteAsync(created.Id, CancellationToken.None);

        Assert.True(result);
        var persisted = await _context!.Set<SubUnitsDetailsEntity>().AsNoTracking().SingleOrDefaultAsync(f => f.Id == created.Id);
        Assert.NotNull(persisted); // soft delete: the row is not physically removed
        Assert.False(persisted!.IsActive);
    }

    #endregion

    #region CreateDirectRoomsAsync

    [Fact]
    public async Task CreateDirectRoomsAsync_ParentAssetNotFound_Throws()
    {
        var service = CreateService();
        var dto = new DirectRoomRegistrationDto
        {
            ParentAssetId = 999,
            FloorId = 1,
            PropertyGroups = new List<PropertyGroupDto>
            {
                new() { ConstructionYear = "2020", ConstructionTypeId = 1, TypeOfUseId = 1, Rooms = new List<RoomDetailDto> { new() { RoomNo = "1" } } }
            }
        };

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateDirectRoomsAsync(dto, currentUserId: 1, CancellationToken.None));
        Assert.Equal("Parent asset with Id 999 not found.", ex.Message);
    }

    [Fact]
    public async Task CreateDirectRoomsAsync_ReplacesExistingFloorData_CreatesNewRows_WithoutCalculatingCapitalValue()
    {
        var parent = await SeedAssetAsync(1);

        var oldFloorDetail = new SubUnitsDetailsEntity { AssetId = parent.Id, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, CreatedDate = DateTime.Now };
        _context!.Set<SubUnitsDetailsEntity>().Add(oldFloorDetail);
        await _context.SaveChangesAsync();

        var oldRoom = new AssetRoomWiseSubmissionDetailsEntity { SubUnitsDetailsId = oldFloorDetail.Id, AssetId = parent.Id, IsActive = true, CreatedDate = DateTime.Now };
        _context.Set<AssetRoomWiseSubmissionDetailsEntity>().Add(oldRoom);
        var oldLease = new AssetLeaseRentDetailsEntity
        {
            AssetId = parent.Id,
            FloorDetailsId = oldFloorDetail.Id,
            TenantName = "Old Tenant",
            TenantMobile = "9999999999",
            LeaseStartDate = DateTime.Now,
            SecurityDeposit = 0,
            IsActive = true,
            CreatedDate = DateTime.Now
        };
        _context.Set<AssetLeaseRentDetailsEntity>().Add(oldLease);
        await _context.SaveChangesAsync();

        var service = CreateService();
        var dto = new DirectRoomRegistrationDto
        {
            ParentAssetId = parent.Id,
            FloorId = 1,
            PropertyGroups = new List<PropertyGroupDto>
            {
                new()
                {
                    ConstructionYear = "2021",
                    ConstructionTypeId = 1,
                    TypeOfUseId = 1,
                    Rooms = new List<RoomDetailDto> { new() { LengthMtr = 4, WidthMtr = 3, RoomNo = "1", RoomType = "Shop" } }
                }
            },
            RentInformation = new RentInformationDto { LeaseRentType = "Rent", RentAmount = 5000m }
        };

        var result = await service.CreateDirectRoomsAsync(dto, currentUserId: 42, CancellationToken.None);

        Assert.True(result);

        // Old floor detail: soft-deleted (SubUnitsDetailsEntity is not IHardDeletable, so
        // Repository.DeleteAsync only flips IsActive — the row itself remains).
        var oldFloorAfter = await _context.Set<SubUnitsDetailsEntity>().AsNoTracking().SingleAsync(f => f.Id == oldFloorDetail.Id);
        Assert.False(oldFloorAfter.IsActive);

        // Old room: IHardDeletable — soft-deleted with MarkedForDeletion set.
        var oldRoomAfter = await _context.Set<AssetRoomWiseSubmissionDetailsEntity>().AsNoTracking().SingleAsync(r => r.Id == oldRoom.Id);
        Assert.False(oldRoomAfter.IsActive);
        Assert.True(oldRoomAfter.MarkedForDeletion);

        // New floor detail created for the same AssetId/FloorId.
        var newFloorDetails = await _context.Set<SubUnitsDetailsEntity>().AsNoTracking()
            .Where(f => f.AssetId == parent.Id && f.FloorId == 1 && f.IsActive)
            .ToListAsync();
        var newFloorDetail = Assert.Single(newFloorDetails);
        Assert.Equal("2021", newFloorDetail.ConstructionYear);

        // Regression guard: no CV auto-calculation on the new row.
        Assert.Null(newFloorDetail.CapitalValue);
        Assert.Null(newFloorDetail.BaseValue);

        var newRoom = await _context.Set<AssetRoomWiseSubmissionDetailsEntity>().AsNoTracking()
            .SingleAsync(r => r.SubUnitsDetailsId == newFloorDetail.Id);
        Assert.Equal("1", newRoom.RoomNo);

        var newLease = await _context.Set<AssetLeaseRentDetailsEntity>().AsNoTracking()
            .SingleAsync(l => l.AssetId == parent.Id && l.RentAmount == 5000m);
        Assert.Equal("Rent", newLease.LeaseType);
        // Regression guard: the lease must link to the newly created floor detail, not be orphaned.
        Assert.Equal(newFloorDetail.Id, newLease.FloorDetailsId);
    }

    #endregion
}
