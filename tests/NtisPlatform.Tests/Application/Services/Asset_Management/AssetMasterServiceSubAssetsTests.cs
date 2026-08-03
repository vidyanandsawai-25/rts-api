using AutoMapper;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application.Services.Asset_Management;

/// <summary>
/// Covers <c>AssetMasterService.SubAssets.cs</c> — <see cref="AssetMasterService.GetByParentAssetIdAsync"/>,
/// <see cref="AssetMasterService.GetAssetFloorAndOtherDetailsAsync"/> (including the private
/// <c>MapInventoryData</c> helper, exercised indirectly), and
/// <see cref="AssetMasterService.GetSubAssetsGroupedByParentAsync"/>. These methods internally call
/// into <c>AssetMasterService.Location.cs</c> (<c>EnrichLocationAsync</c>/<c>ApplyLocation</c>/
/// <c>GetLocationInfoByAssetIdsAsync</c>) and <c>AssetMasterService.Inventory.cs</c>
/// (<c>GetAllocatedAssetIdsAsync</c>/<c>BuildInventoryDataAsync</c>) — those have their own dedicated
/// test files, so here every location/inventory-adjacent repository is just defaulted to an empty
/// queryable unless a specific test needs rows to exercise the interaction points in this file.
/// </summary>
public class AssetMasterServiceSubAssetsTests
{
    private static AssetMasterService CreateService(
        out Mock<IRepository<AssetMasterEntity, int>> repository,
        out Mock<IRepository<SubUnitsDetailsEntity, int>> floorDetailsRepository,
        out Mock<IRepository<AssetRoomWiseSubmissionDetailsEntity, int>> roomWiseSubmissionRepository,
        out Mock<IRepository<AssetTypeOfUseMasterEntity, int>> amsTypeOfUseRepository,
        out Mock<IRepository<AssetSubTypeOfUseEntity, int>> amsSubTypeOfUseRepository,
        out Mock<IRepository<InventoryBatchEntity, int>> inventoryBatchRepository,
        out Mock<IRepository<InventoryAssetDetailEntity, int>> inventoryAssetDetailRepository,
        out Mock<IInventoryDocumentApplicationService> inventoryDocumentApplicationService,
        out Mock<IRepository<AssetLeaseRentDetailsEntity, int>> leaseRentDetailsRepository,
        out Mock<IRepository<AssetDetailsEntity, int>> detailsRepository)
    {
        repository = new Mock<IRepository<AssetMasterEntity, int>>();
        floorDetailsRepository = new Mock<IRepository<SubUnitsDetailsEntity, int>>();
        roomWiseSubmissionRepository = new Mock<IRepository<AssetRoomWiseSubmissionDetailsEntity, int>>();
        amsTypeOfUseRepository = new Mock<IRepository<AssetTypeOfUseMasterEntity, int>>();
        amsSubTypeOfUseRepository = new Mock<IRepository<AssetSubTypeOfUseEntity, int>>();
        inventoryBatchRepository = new Mock<IRepository<InventoryBatchEntity, int>>();
        inventoryAssetDetailRepository = new Mock<IRepository<InventoryAssetDetailEntity, int>>();
        inventoryDocumentApplicationService = new Mock<IInventoryDocumentApplicationService>();
        leaseRentDetailsRepository = new Mock<IRepository<AssetLeaseRentDetailsEntity, int>>();
        detailsRepository = new Mock<IRepository<AssetDetailsEntity, int>>();

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var mapperConfig = new MapperConfiguration(
            cfg => cfg.AddProfile<AssetMasterMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();

        var referenceValidator = new Mock<IReferenceValidationService>();
        var fieldValueRepository = new Mock<IRepository<AssetFieldValueEntity, int>>();
        var assetCategoryRepository = new Mock<IRepository<AssetCategoryEntity, int>>();
        var assetTypeRepository = new Mock<IRepository<AssetTypeEntity, int>>();
        var ulbRepository = new Mock<IRepository<ULBMasterEntity, int>>();
        var assetDocumentRepository = new Mock<IRepository<AssetDocumentEntity, int>>();
        var assetPhotoRepository = new Mock<IRepository<AssetPhotoEntity, int>>();
        var assetPhotoApplicationService = new Mock<IAssetPhotoApplicationService>();
        var documentApplicationService = new Mock<IDocumentApplicationService>();
        var zoneRepository = new Mock<IRepository<ZoneEntity, int>>();
        var wardRepository = new Mock<IRepository<WardEntity, int>>();
        var moujaRepository = new Mock<IRepository<MoujaEntity, int>>();
        var subZoneRepository = new Mock<IRepository<SubZoneDetailsForCVEntity, int>>();
        var departmentRepository = new Mock<IRepository<OwningDepartmentEntity, int>>();
        var organizationRepository = new Mock<IRepository<AssetOrganizationMasterEntity, int>>();
        var conditionRepository = new Mock<IRepository<AssetConditionMasterEntity, int>>();
        var deptMasterRepository = new Mock<IRepository<DepartmentMasterEntity, int>>();
        var moduleMasterRepository = new Mock<IRepository<ModuleMasterEntity, int>>();
        var designationRepository = new Mock<IRepository<AssetDesignationEntity, int>>();
        var logger = new Mock<ILogger<AssetMasterService>>();
        var inventoryCategoryRepository = new Mock<IRepository<InventoryItemCategoryEntity, int>>();
        var inventoryNameRepository = new Mock<IRepository<InventoryItemNameEntity, int>>();
        var inventoryModelRepository = new Mock<IRepository<InventoryItemModelEntity, int>>();
        var inventoryDepartmentRepository = new Mock<IRepository<OwningDepartmentEntity, int>>();

        // Every repository the batched location/inventory queries touch must return SOME queryable
        // (even if empty) or the LINQ joins/subqueries in the methods under test throw. Default
        // everything to empty first; individual tests override specific repos with real rows.
        SetupRows(repository);
        SetupRows(floorDetailsRepository);
        SetupRows(roomWiseSubmissionRepository);
        SetupRows(amsTypeOfUseRepository);
        SetupRows(amsSubTypeOfUseRepository);
        SetupRows(inventoryBatchRepository);
        SetupRows(inventoryAssetDetailRepository);
        SetupRows(leaseRentDetailsRepository);
        SetupRows(fieldValueRepository);
        SetupRows(assetCategoryRepository);
        SetupRows(assetTypeRepository);
        SetupRows(ulbRepository);
        SetupRows(detailsRepository);
        SetupRows(assetDocumentRepository);
        SetupRows(assetPhotoRepository);
        SetupRows(zoneRepository);
        SetupRows(wardRepository);
        SetupRows(moujaRepository);
        SetupRows(subZoneRepository);
        SetupRows(departmentRepository);
        SetupRows(organizationRepository);
        SetupRows(conditionRepository);
        SetupRows(deptMasterRepository);
        SetupRows(moduleMasterRepository);
        SetupRows(designationRepository);
        SetupRows(inventoryCategoryRepository);
        SetupRows(inventoryNameRepository);
        SetupRows(inventoryModelRepository);
        SetupRows(inventoryDepartmentRepository);

        inventoryDocumentApplicationService
            .Setup(s => s.GetDocumentsByInventoryBatchesAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, List<InventoryDocumentDto>>());

        return new AssetMasterService(
            repository.Object,
            unitOfWork.Object,
            mapper,
            referenceValidator.Object,
            fieldValueRepository.Object,
            floorDetailsRepository.Object,
            roomWiseSubmissionRepository.Object,
            assetCategoryRepository.Object,
            assetTypeRepository.Object,
            ulbRepository.Object,
            detailsRepository.Object,
            assetDocumentRepository.Object,
            assetPhotoRepository.Object,
            assetPhotoApplicationService.Object,
            documentApplicationService.Object,
            zoneRepository.Object,
            wardRepository.Object,
            moujaRepository.Object,
            subZoneRepository.Object,
            departmentRepository.Object,
            organizationRepository.Object,
            conditionRepository.Object,
            deptMasterRepository.Object,
            moduleMasterRepository.Object,
            designationRepository.Object,
            amsTypeOfUseRepository.Object,
            amsSubTypeOfUseRepository.Object,
            logger.Object,
            inventoryBatchRepository.Object,
            inventoryAssetDetailRepository.Object,
            inventoryCategoryRepository.Object,
            inventoryNameRepository.Object,
            inventoryModelRepository.Object,
            inventoryDepartmentRepository.Object,
            inventoryDocumentApplicationService.Object,
            leaseRentDetailsRepository.Object);
    }

    private static void SetupRows<T>(Mock<IRepository<T, int>> repoMock, params T[] rows) where T : class
    {
        var mockQuery = rows.ToList().BuildMockDbSet();
        repoMock.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);
    }

    #region GetByParentAssetIdAsync

    [Fact]
    public async Task GetByParentAssetIdAsync_ReturnsChildAssets_ForGivenParent()
    {
        var service = CreateService(out var repository, out _, out _, out _, out _, out _, out _, out _, out _, out var detailsRepository);

        var parent = new AssetMasterEntity { Id = 1, AssetName = "Parent Building", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        var child1 = new AssetMasterEntity { Id = 2, ParentAssetId = 1, AssetNo = "A-1", AssetName = "Shop 1", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        var child2 = new AssetMasterEntity { Id = 3, ParentAssetId = 1, AssetNo = "A-2", AssetName = "Shop 2", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        var unrelated = new AssetMasterEntity { Id = 4, ParentAssetId = 99, AssetNo = "A-3", AssetName = "Unrelated", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        SetupRows(repository, parent, child1, child2, unrelated);

        // EnrichLocationAsync's batched join (Location.cs) dereferences the joined AssetDetails row
        // directly for fields like ZoneId in a couple of places without a null-check on the row
        // itself (only on the downstream zone/ward/... lookups) -- under MockQueryable's in-memory
        // LINQ execution (unlike a real translated SQL LEFT JOIN) that throws a NullReferenceException
        // if an asset has no matching AssetDetails row at all. Giving every enriched asset id a
        // (mostly empty) AssetDetails row sidesteps that without asserting anything about location
        // data itself -- that behavior is covered by AssetMasterServiceLocationTests.cs.
        SetupRows(detailsRepository, new AssetDetailsEntity { AssetId = child1.Id }, new AssetDetailsEntity { AssetId = child2.Id });

        var result = await service.GetByParentAssetIdAsync(1, floorDetailsId: 0, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Id == 2 && r.AssetName == "Shop 1");
        Assert.Contains(result, r => r.Id == 3 && r.AssetName == "Shop 2");
        Assert.DoesNotContain(result, r => r.Id == 4);
    }

    // Section B item 3 of the test coverage roadmap: `floorDetailsId` is accepted by
    // GetByParentAssetIdAsync but is never referenced anywhere in the method body (confirmed by
    // reading AssetMasterService.SubAssets.cs — no field/property/query in the method touches it).
    // This test locks in that dead-parameter behavior: calling with wildly different floorDetailsId
    // values must produce identical results. If Section B item 3 is ever resolved (the parameter
    // either removed from the signature or wired up to actually filter), replace this test with one
    // that asserts the real filtering behavior instead of the current no-op.
    [Fact]
    public async Task GetByParentAssetIdAsync_BehaviorIsIdenticalRegardlessOfFloorDetailsIdValue()
    {
        var service = CreateService(out var repository, out _, out _, out _, out _, out _, out _, out _, out _, out var detailsRepository);

        var child = new AssetMasterEntity { Id = 2, ParentAssetId = 1, AssetNo = "A-1", AssetName = "Shop 1", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        SetupRows(repository, child);
        // See comment in GetByParentAssetIdAsync_ReturnsChildAssets_ForGivenParent above: without a
        // matching AssetDetails row, EnrichLocationAsync's batched join throws under MockQueryable.
        SetupRows(detailsRepository, new AssetDetailsEntity { AssetId = child.Id });

        var resultWithFloorDetailsId1 = await service.GetByParentAssetIdAsync(1, floorDetailsId: 1, CancellationToken.None);
        var resultWithFloorDetailsId999 = await service.GetByParentAssetIdAsync(1, floorDetailsId: 999, CancellationToken.None);

        Assert.Single(resultWithFloorDetailsId1);
        Assert.Single(resultWithFloorDetailsId999);
        Assert.Equal(resultWithFloorDetailsId1[0].Id, resultWithFloorDetailsId999[0].Id);
        Assert.Equal(resultWithFloorDetailsId1[0].AssetNo, resultWithFloorDetailsId999[0].AssetNo);
        Assert.Equal(resultWithFloorDetailsId1[0].AssetName, resultWithFloorDetailsId999[0].AssetName);
    }

    #endregion

    #region GetAssetFloorAndOtherDetailsAsync

    [Fact]
    public async Task GetAssetFloorAndOtherDetailsAsync_WhenParentNotFound_ReturnsNull()
    {
        var service = CreateService(out var repository, out _, out _, out _, out _, out _, out _, out _, out _, out _);
        SetupRows(repository); // no asset with Id == 123 at all

        var result = await service.GetAssetFloorAndOtherDetailsAsync(123, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAssetFloorAndOtherDetailsAsync_GroupsFloorSummaryByFloorId()
    {
        var service = CreateService(out var repository, out var floorDetailsRepository, out _, out _, out _, out _, out _, out _, out _, out _);

        var parent = new AssetMasterEntity { Id = 1, AssetName = "Parent Building", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        var child = new AssetMasterEntity { Id = 2, ParentAssetId = 1, AssetName = "Shop 1", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        SetupRows(repository, parent, child);

        // Two SubUnitsDetails rows sharing the same FloorId (100) but with different
        // CarpetAreaSqMeter values -- the grouped floor summary must sum them into one entry.
        var groundFloor = new FloorEntity { Id = 100, Description = "Ground Floor", SequenceNo = 1 };
        var floorDetail1 = new SubUnitsDetailsEntity
        {
            Id = 10,
            AssetId = child.Id,
            FloorId = 100,
            IsActive = true,
            MarkedForDeletion = false,
            CarpetAreaSqMeter = 10m,
            Floor = groundFloor
        };
        var floorDetail2 = new SubUnitsDetailsEntity
        {
            Id = 11,
            AssetId = child.Id,
            FloorId = 100,
            IsActive = true,
            MarkedForDeletion = false,
            CarpetAreaSqMeter = 20m,
            Floor = groundFloor
        };
        SetupRows(floorDetailsRepository, floorDetail1, floorDetail2);

        var result = await service.GetAssetFloorAndOtherDetailsAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result!.FloorSummary);
        var floorSummaryEntry = Assert.Single(result.FloorSummary!.FloorDetails);
        Assert.Equal("Ground Floor", floorSummaryEntry.FloorName);
        Assert.Equal(30m, floorSummaryEntry.CarpetAreaSqMeter);
        Assert.Equal(1, result.FloorSummary.TotalFloors);
    }

    // MapInventoryData is `private static`, so it's exercised indirectly through
    // GetAssetFloorAndOtherDetailsAsync, which always calls BuildInventoryDataAsync (Inventory.cs)
    // then feeds the result through MapInventoryData before returning.
    [Fact]
    public async Task MapInventoryData_FiltersDocumentsToInventoryImageAndInvoiceRemarksOnly()
    {
        var service = CreateService(
            out var repository, out _, out _, out _, out _,
            out var inventoryBatchRepository, out _, out var inventoryDocumentApplicationService, out _, out _);

        var parent = new AssetMasterEntity { Id = 1, AssetName = "Parent Building", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        SetupRows(repository, parent);

        var batch = new InventoryBatchEntity
        {
            Id = 500,
            ParentAssetId = 1,
            IsActive = true,
            MarkedForDeletion = false,
            PurchaseDate = DateTime.UtcNow,
            Quantity = 1,
            UnitValue = 100m,
            TotalBatchValue = 100m
        };
        SetupRows(inventoryBatchRepository, batch);

        inventoryDocumentApplicationService
            .Setup(s => s.GetDocumentsByInventoryBatchesAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, List<InventoryDocumentDto>>
            {
                [500] = new List<InventoryDocumentDto>
                {
                    new InventoryDocumentDto { InventoryDocumentId = 1, InventoryBatchId = 500, DocumentTypeCode = "IMG", DocumentTypeName = "Image", Remarks = "Inventory Image" },
                    new InventoryDocumentDto { InventoryDocumentId = 2, InventoryBatchId = 500, DocumentTypeCode = "INV", DocumentTypeName = "Invoice", Remarks = "Inventory Invoice" },
                    new InventoryDocumentDto { InventoryDocumentId = 3, InventoryBatchId = 500, DocumentTypeCode = "OTH", DocumentTypeName = "Other", Remarks = "Random Note" }
                }
            });

        var result = await service.GetAssetFloorAndOtherDetailsAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result!.InventoryData);
        var mappedBatch = Assert.Single(result.InventoryData!.Batches);
        Assert.Equal(2, mappedBatch.Documents.Count);
        Assert.Contains(mappedBatch.Documents, d => d.Remarks == "Inventory Image");
        Assert.Contains(mappedBatch.Documents, d => d.Remarks == "Inventory Invoice");
        Assert.DoesNotContain(mappedBatch.Documents, d => d.Remarks == "Random Note");
    }

    #endregion

    #region GetSubAssetsGroupedByParentAsync

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetSubAssetsGroupedByParentAsync_WithZeroOrNegativeParentAssetId_ThrowsArgumentOutOfRangeException(int parentAssetId)
    {
        var service = CreateService(out _, out _, out _, out _, out _, out _, out _, out _, out _, out _);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetSubAssetsGroupedByParentAsync(parentAssetId, CancellationToken.None));
    }

    // IMPORTANT: this is deliberately different from GetAssetFloorAndOtherDetailsAsync's
    // "not found" behavior tested above (which returns a null DTO). GetSubAssetsGroupedByParentAsync
    // instead returns a non-null SubAssetGroupedResponseDto with ParentAsset == null,
    // TotalSubAssets == 0, and an empty SubAssets list. These are sibling methods in the same file
    // covering the same "missing/inactive parent" scenario with two different result shapes -- easy
    // to conflate, so this test asserts the actual (non-null, empty) shape explicitly.
    [Fact]
    public async Task GetSubAssetsGroupedByParentAsync_WhenParentNotActiveOrNotFound_ReturnsEmptyResult_DoesNotThrowOrReturnNull()
    {
        // Case 1: no asset with this Id exists at all.
        var serviceForMissingParent = CreateService(out var repositoryForMissingParent, out _, out _, out _, out _, out _, out _, out _, out _, out _);
        SetupRows(repositoryForMissingParent);

        var resultForMissingParent = await serviceForMissingParent.GetSubAssetsGroupedByParentAsync(42, CancellationToken.None);

        Assert.NotNull(resultForMissingParent);
        Assert.Null(resultForMissingParent.ParentAsset);
        Assert.Equal(0, resultForMissingParent.TotalSubAssets);
        Assert.Empty(resultForMissingParent.SubAssets);

        // Case 2: the asset exists but IsActive == false -- the query's `a.IsActive` filter excludes
        // it just like a missing row would, producing the identical empty (never null) result shape.
        var serviceForInactiveParent = CreateService(out var repositoryForInactiveParent, out _, out _, out _, out _, out _, out _, out _, out _, out _);
        var inactiveParent = new AssetMasterEntity { Id = 42, AssetName = "Inactive Parent", IsActive = false, FieldValues = new List<AssetFieldValueEntity>() };
        SetupRows(repositoryForInactiveParent, inactiveParent);

        var resultForInactiveParent = await serviceForInactiveParent.GetSubAssetsGroupedByParentAsync(42, CancellationToken.None);

        Assert.NotNull(resultForInactiveParent);
        Assert.Null(resultForInactiveParent.ParentAsset);
        Assert.Equal(0, resultForInactiveParent.TotalSubAssets);
        Assert.Empty(resultForInactiveParent.SubAssets);
    }

    [Fact]
    public async Task GetSubAssetsGroupedByParentAsync_ExcludesAssetsAlreadyAllocatedToInventory()
    {
        var service = CreateService(
            out var repository, out _, out _, out _, out _,
            out _, out var inventoryAssetDetailRepository, out _, out _, out var detailsRepository);

        var parent = new AssetMasterEntity { Id = 1, AssetName = "Parent Building", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        var child1 = new AssetMasterEntity { Id = 2, ParentAssetId = 1, AssetNo = "A-1", AssetName = "Shop 1", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        var child2 = new AssetMasterEntity { Id = 3, ParentAssetId = 1, AssetNo = "A-2", AssetName = "Shop 2 (Allocated)", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        SetupRows(repository, parent, child1, child2);

        // GetSubAssetsGroupedByParentAsync resolves location context for the parent AND for every
        // remaining (non-allocated) sub-asset via GetLocationInfoByAssetIdsAsync (Location.cs). Under
        // MockQueryable's in-memory join execution that throws if an asset has no matching
        // AssetDetails row at all (see comment in the GetByParentAssetIdAsync tests above) -- give the
        // parent and child1 one each. child2 is allocated and excluded before location resolution, so
        // it doesn't need one.
        SetupRows(detailsRepository, new AssetDetailsEntity { AssetId = parent.Id }, new AssetDetailsEntity { AssetId = child1.Id });

        // child2 (Id = 3) already has an inventory unit pointing at it -- GetAllocatedAssetIdsAsync
        // (Inventory.cs) should surface its id, and GetSubAssetsGroupedByParentAsync must exclude it.
        var allocatedUnit = new InventoryAssetDetailEntity { Id = 900, AssetId = child2.Id, BatchId = 500, UnitNumber = 1 };
        SetupRows(inventoryAssetDetailRepository, allocatedUnit);

        var result = await service.GetSubAssetsGroupedByParentAsync(1, CancellationToken.None);

        Assert.NotNull(result.ParentAsset);
        Assert.Equal(1, result.TotalSubAssets);
        var remainingSubAsset = Assert.Single(result.SubAssets);
        Assert.Equal(child1.Id, remainingSubAsset.Id);
        Assert.DoesNotContain(result.SubAssets, sa => sa.Id == child2.Id);
    }

    #endregion
}
