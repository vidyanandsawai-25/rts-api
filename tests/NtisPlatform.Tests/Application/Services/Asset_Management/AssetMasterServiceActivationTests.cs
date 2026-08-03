using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.Asset_Management;

public class AssetMasterServiceActivationTests
{
    private static IMapper CreateMapper()
    {
        var mapperConfig = new MapperConfiguration(
            cfg => cfg.AddProfile<AssetMasterMappingProfile>(),
            NullLoggerFactory.Instance);
        return mapperConfig.CreateMapper();
    }

    /// <summary>
    /// Builds an AssetMasterService with only the dependencies exercised by
    /// ActivateAssetAndFieldValuesAsync exposed as out-params. Every other constructor
    /// dependency (there are ~30 total) is defaulted to a bare Mock&lt;T&gt;.Object.
    /// All queryable-backed repositories default to an empty MockQueryable-backed set
    /// unless a test overrides the setup.
    /// </summary>
    private static AssetMasterService CreateService(
        out Mock<IRepository<AssetMasterEntity, int>> repository,
        out Mock<IUnitOfWork> unitOfWork,
        out Mock<IRepository<AssetFieldValueEntity, int>> fieldValueRepository,
        out Mock<IRepository<AssetDetailsEntity, int>> detailsRepository,
        out Mock<IRepository<SubUnitsDetailsEntity, int>> floorDetailsRepository,
        out Mock<IRepository<AssetRoomWiseSubmissionDetailsEntity, int>> roomWiseSubmissionRepository,
        out Mock<IRepository<AssetLeaseRentDetailsEntity, int>> leaseRentDetailsRepository,
        out Mock<ILogger<AssetMasterService>> logger)
    {
        repository = new Mock<IRepository<AssetMasterEntity, int>>();
        unitOfWork = new Mock<IUnitOfWork>();
        fieldValueRepository = new Mock<IRepository<AssetFieldValueEntity, int>>();
        detailsRepository = new Mock<IRepository<AssetDetailsEntity, int>>();
        floorDetailsRepository = new Mock<IRepository<SubUnitsDetailsEntity, int>>();
        roomWiseSubmissionRepository = new Mock<IRepository<AssetRoomWiseSubmissionDetailsEntity, int>>();
        leaseRentDetailsRepository = new Mock<IRepository<AssetLeaseRentDetailsEntity, int>>();
        logger = new Mock<ILogger<AssetMasterService>>();

        // Defaults: empty queryables for every repository the method touches, so the
        // cascade steps run over zero rows unless a test overrides a specific setup.
        fieldValueRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetFieldValueEntity>().BuildMockDbSet().Object);
        detailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetDetailsEntity>().BuildMockDbSet().Object);
        floorDetailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<SubUnitsDetailsEntity>().BuildMockDbSet().Object);
        roomWiseSubmissionRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetRoomWiseSubmissionDetailsEntity>().BuildMockDbSet().Object);
        leaseRentDetailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetLeaseRentDetailsEntity>().BuildMockDbSet().Object);

        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var mapper = CreateMapper();

        return new AssetMasterService(
            repository.Object,
            unitOfWork.Object,
            mapper,
            new Mock<IReferenceValidationService>().Object,
            fieldValueRepository.Object,
            floorDetailsRepository.Object,
            roomWiseSubmissionRepository.Object,
            new Mock<IRepository<AssetCategoryEntity, int>>().Object,
            new Mock<IRepository<AssetTypeEntity, int>>().Object,
            new Mock<IRepository<ULBMasterEntity, int>>().Object,
            detailsRepository.Object,
            new Mock<IRepository<AssetDocumentEntity, int>>().Object,
            new Mock<IRepository<AssetPhotoEntity, int>>().Object,
            new Mock<IAssetPhotoApplicationService>().Object,
            new Mock<IDocumentApplicationService>().Object,
            new Mock<IRepository<ZoneEntity, int>>().Object,
            new Mock<IRepository<WardEntity, int>>().Object,
            new Mock<IRepository<MoujaEntity, int>>().Object,
            new Mock<IRepository<SubZoneDetailsForCVEntity, int>>().Object,
            new Mock<IRepository<OwningDepartmentEntity, int>>().Object,
            new Mock<IRepository<AssetOrganizationMasterEntity, int>>().Object,
            new Mock<IRepository<AssetConditionMasterEntity, int>>().Object,
            new Mock<IRepository<DepartmentMasterEntity, int>>().Object,
            new Mock<IRepository<ModuleMasterEntity, int>>().Object,
            new Mock<IRepository<AssetDesignationEntity, int>>().Object,
            new Mock<IRepository<AssetTypeOfUseMasterEntity, int>>().Object,
            new Mock<IRepository<AssetSubTypeOfUseEntity, int>>().Object,
            logger.Object,
            new Mock<IRepository<InventoryBatchEntity, int>>().Object,
            new Mock<IRepository<InventoryAssetDetailEntity, int>>().Object,
            new Mock<IRepository<InventoryItemCategoryEntity, int>>().Object,
            new Mock<IRepository<InventoryItemNameEntity, int>>().Object,
            new Mock<IRepository<InventoryItemModelEntity, int>>().Object,
            new Mock<IRepository<OwningDepartmentEntity, int>>().Object,
            new Mock<IInventoryDocumentApplicationService>().Object,
            leaseRentDetailsRepository.Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ActivateAssetAndFieldValuesAsync_WithZeroOrNegativeId_ThrowsArgumentOutOfRangeException(int assetId)
    {
        var service = CreateService(out _, out _, out _, out _, out _, out _, out _, out _);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.ActivateAssetAndFieldValuesAsync(assetId));
    }

    [Fact]
    public async Task ActivateAssetAndFieldValuesAsync_WhenAssetNotFound_ReturnsFalse()
    {
        var service = CreateService(
            out var repository, out var unitOfWork, out _, out _, out _, out _, out _, out _);

        repository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetMasterEntity>().BuildMockDbSet().Object);

        var result = await service.ActivateAssetAndFieldValuesAsync(1);

        Assert.False(result);
        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ActivateAssetAndFieldValuesAsync_ActivatesAssetFieldValuesAndDetails()
    {
        var service = CreateService(
            out var repository, out var unitOfWork, out var fieldValueRepository,
            out var detailsRepository, out _, out _, out _, out _);

        var asset = new AssetMasterEntity { Id = 1, IsActive = false, MarkedForDeletion = false };
        repository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetMasterEntity> { asset }.BuildMockDbSet().Object);

        var fieldValue1 = new AssetFieldValueEntity { Id = 10, AssetId = 1, IsActive = false, MarkedForDeletion = false };
        var fieldValue2 = new AssetFieldValueEntity { Id = 11, AssetId = 1, IsActive = false, MarkedForDeletion = false };
        fieldValueRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetFieldValueEntity> { fieldValue1, fieldValue2 }.BuildMockDbSet().Object);

        var details = new AssetDetailsEntity { Id = 1, AssetId = 1, IsActive = false, MarkedForDeletion = false };
        detailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetDetailsEntity> { details }.BuildMockDbSet().Object);

        var result = await service.ActivateAssetAndFieldValuesAsync(1);

        Assert.True(result);
        // The asset and field-value entities come from the mocked queryable's in-memory
        // object references (BuildMockDbSet() does not clone), so mutating them in the
        // service is directly observable on these same instances here.
        Assert.True(asset.IsActive);
        Assert.True(fieldValue1.IsActive);
        Assert.True(fieldValue2.IsActive);
        Assert.True(details.IsActive);

        detailsRepository.Verify(
            r => r.UpdateAsync(details, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ActivateAssetAndFieldValuesAsync_CascadesToLeaseRentDetailsFloorsAndRooms()
    {
        var service = CreateService(
            out var repository, out var unitOfWork, out _, out _,
            out var floorDetailsRepository, out var roomWiseSubmissionRepository,
            out var leaseRentDetailsRepository, out _);

        var asset = new AssetMasterEntity { Id = 1, IsActive = false, MarkedForDeletion = false };
        repository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetMasterEntity> { asset }.BuildMockDbSet().Object);

        var lease = new AssetLeaseRentDetailsEntity { Id = 1, AssetId = 1, IsActive = false, MarkedForDeletion = false };
        leaseRentDetailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetLeaseRentDetailsEntity> { lease }.BuildMockDbSet().Object);

        // One floor eligible for activation, one marked for deletion. The production query fetches
        // BOTH (no MarkedForDeletion filter in the query itself) and relies on the in-loop
        // `if (!floor.MarkedForDeletion)` guard to skip the deleted one.
        var floor = new SubUnitsDetailsEntity { Id = 100, AssetId = 1, IsActive = false, MarkedForDeletion = false };
        var deletedFloor = new SubUnitsDetailsEntity { Id = 101, AssetId = 1, IsActive = false, MarkedForDeletion = true };
        floorDetailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<SubUnitsDetailsEntity> { floor, deletedFloor }.BuildMockDbSet().Object);

        // Rooms under the eligible floor: one activatable room with a mix of activatable/deleted
        // minus-data rows, plus a room marked for deletion (should stay inactive).
        var activatableMinus = new AssetRoomWiseMinusDataEntity { Id = 200, RoomWiseSubmissionId = 300, IsActive = false, MarkedForDeletion = false };
        var deletedMinus = new AssetRoomWiseMinusDataEntity { Id = 201, RoomWiseSubmissionId = 300, IsActive = false, MarkedForDeletion = true };
        var room = new AssetRoomWiseSubmissionDetailsEntity
        {
            Id = 300,
            AssetId = 1,
            SubUnitsDetailsId = 100,
            IsActive = false,
            MarkedForDeletion = false,
            RoomMinusData = new List<AssetRoomWiseMinusDataEntity> { activatableMinus, deletedMinus }
        };
        var deletedRoom = new AssetRoomWiseSubmissionDetailsEntity
        {
            Id = 301,
            AssetId = 1,
            SubUnitsDetailsId = 100,
            IsActive = false,
            MarkedForDeletion = true
        };
        roomWiseSubmissionRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetRoomWiseSubmissionDetailsEntity> { room, deletedRoom }.BuildMockDbSet().Object);

        var result = await service.ActivateAssetAndFieldValuesAsync(1);

        Assert.True(result);
        Assert.True(lease.IsActive);
        Assert.True(floor.IsActive);
        Assert.False(deletedFloor.IsActive);
        Assert.True(room.IsActive);
        Assert.False(deletedRoom.IsActive);
        Assert.True(activatableMinus.IsActive);
        Assert.False(deletedMinus.IsActive);
    }

    [Fact]
    public async Task ActivateAssetAndFieldValuesAsync_ActivatesChildAssetsWhereParentAssetIdMatches()
    {
        var service = CreateService(
            out var repository, out var unitOfWork, out _, out _, out _, out _, out _, out _);

        var parent = new AssetMasterEntity { Id = 1, IsActive = false, MarkedForDeletion = false };
        var child1 = new AssetMasterEntity { Id = 2, ParentAssetId = 1, IsActive = false, MarkedForDeletion = false };
        var child2 = new AssetMasterEntity { Id = 3, ParentAssetId = 1, IsActive = false, MarkedForDeletion = false };
        var unrelated = new AssetMasterEntity { Id = 4, ParentAssetId = 99, IsActive = false, MarkedForDeletion = false };

        repository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetMasterEntity> { parent, child1, child2, unrelated }.BuildMockDbSet().Object);

        var result = await service.ActivateAssetAndFieldValuesAsync(1);

        Assert.True(result);
        Assert.True(parent.IsActive);
        Assert.True(child1.IsActive);
        Assert.True(child2.IsActive);
        Assert.False(unrelated.IsActive);
    }

    [Fact]
    public async Task ActivateAssetAndFieldValuesAsync_CommitsTransaction_OnSuccess()
    {
        var service = CreateService(
            out var repository, out var unitOfWork, out _, out _, out _, out _, out _, out _);

        var asset = new AssetMasterEntity { Id = 1, IsActive = false, MarkedForDeletion = false };
        repository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetMasterEntity> { asset }.BuildMockDbSet().Object);

        var result = await service.ActivateAssetAndFieldValuesAsync(1);

        Assert.True(result);
        unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ActivateAssetAndFieldValuesAsync_RollsBackTransaction_WhenSaveChangesThrows()
    {
        var service = CreateService(
            out var repository, out var unitOfWork, out _, out _, out _, out _, out _, out _);

        var asset = new AssetMasterEntity { Id = 1, IsActive = false, MarkedForDeletion = false };
        repository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetMasterEntity> { asset }.BuildMockDbSet().Object);

        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ActivateAssetAndFieldValuesAsync(1));

        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
