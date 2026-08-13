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
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.DTOs.Asset_Management.InventoryAsset;
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

/// <summary>
/// Covers AssetMasterService.Inventory.cs (BuildInventoryDataAsync, GetAllocatedAssetIdsAsync). See
/// tests/NtisPlatform.Tests/AssetMaster-TestCoverage-Roadmap.md, Section C6. Note: IAssetInventoryService
/// does not exist anywhere in this codebase — this logic lives directly on AssetMasterService.
/// </summary>
public class AssetMasterServiceInventoryTests
{
    private static AssetMasterService CreateService(
        out Mock<IRepository<AssetMasterEntity, int>> assetRepository,
        out Mock<IRepository<InventoryBatchEntity, int>> inventoryBatchRepository,
        out Mock<IRepository<InventoryAssetDetailEntity, int>> inventoryAssetDetailRepository,
        out Mock<IRepository<InventoryItemCategoryEntity, int>> inventoryCategoryRepository,
        out Mock<IRepository<InventoryItemNameEntity, int>> inventoryNameRepository,
        out Mock<IRepository<InventoryItemModelEntity, int>> inventoryModelRepository,
        out Mock<IRepository<AssetConditionMasterEntity, int>> conditionRepository,
        out Mock<IRepository<OwningDepartmentEntity, int>> inventoryDepartmentRepository,
        out Mock<IInventoryDocumentApplicationService> inventoryDocumentApplicationService)
    {
        assetRepository = new Mock<IRepository<AssetMasterEntity, int>>();
        assetRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity>().BuildMockDbSet().Object);

        inventoryBatchRepository = new Mock<IRepository<InventoryBatchEntity, int>>();
        inventoryBatchRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryBatchEntity>().BuildMockDbSet().Object);

        inventoryAssetDetailRepository = new Mock<IRepository<InventoryAssetDetailEntity, int>>();
        inventoryAssetDetailRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryAssetDetailEntity>().BuildMockDbSet().Object);

        inventoryCategoryRepository = new Mock<IRepository<InventoryItemCategoryEntity, int>>();
        inventoryCategoryRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemCategoryEntity>().BuildMockDbSet().Object);

        inventoryNameRepository = new Mock<IRepository<InventoryItemNameEntity, int>>();
        inventoryNameRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemNameEntity>().BuildMockDbSet().Object);

        inventoryModelRepository = new Mock<IRepository<InventoryItemModelEntity, int>>();
        inventoryModelRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemModelEntity>().BuildMockDbSet().Object);

        conditionRepository = new Mock<IRepository<AssetConditionMasterEntity, int>>();
        conditionRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetConditionMasterEntity>().BuildMockDbSet().Object);

        inventoryDepartmentRepository = new Mock<IRepository<OwningDepartmentEntity, int>>();
        inventoryDepartmentRepository.Setup(r => r.GetQueryable()).Returns(new List<OwningDepartmentEntity>().BuildMockDbSet().Object);

        inventoryDocumentApplicationService = new Mock<IInventoryDocumentApplicationService>();
        inventoryDocumentApplicationService
            .Setup(s => s.GetDocumentsByInventoryBatchesAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, List<InventoryDocumentDto>>());

        var mapperConfig = new MapperConfiguration(
            cfg => cfg.AddProfile<AssetMasterMappingProfile>(),
            NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();

        return new AssetMasterService(
            repository: assetRepository.Object,
            unitOfWork: new Mock<IUnitOfWork>().Object,
            mapper: mapper,
            referenceValidator: new Mock<IReferenceValidationService>().Object,
            fieldValueRepository: new Mock<IRepository<AssetFieldValueEntity, int>>().Object,
            floorDetailsRepository: new Mock<IRepository<SubUnitsDetailsEntity, int>>().Object,
            roomWiseSubmissionRepository: new Mock<IRepository<AssetRoomWiseSubmissionDetailsEntity, int>>().Object,
            assetCategoryRepository: new Mock<IRepository<AssetCategoryEntity, int>>().Object,
            assetTypeRepository: new Mock<IRepository<AssetTypeEntity, int>>().Object,
            ulbRepository: new Mock<IRepository<ULBMasterEntity, int>>().Object,
            detailsRepository: new Mock<IRepository<AssetDetailsEntity, int>>().Object,
            assetDocumentRepository: new Mock<IRepository<AssetDocumentEntity, int>>().Object,
            assetPhotoRepository: new Mock<IRepository<AssetPhotoEntity, int>>().Object,
            assetPhotoApplicationService: new Mock<IAssetPhotoApplicationService>().Object,
            documentApplicationService: new Mock<IDocumentApplicationService>().Object,
            zoneRepository: new Mock<IRepository<ZoneEntity, int>>().Object,
            wardRepository: new Mock<IRepository<WardEntity, int>>().Object,
            moujaRepository: new Mock<IRepository<MoujaEntity, int>>().Object,
            subZoneRepository: new Mock<IRepository<SubZoneDetailsForCVEntity, int>>().Object,
            departmentRepository: new Mock<IRepository<OwningDepartmentEntity, int>>().Object,
            organizationRepository: new Mock<IRepository<AssetOrganizationMasterEntity, int>>().Object,
            conditionRepository: conditionRepository.Object,
            deptMasterRepository: new Mock<IRepository<DepartmentMasterEntity, int>>().Object,
            moduleMasterRepository: new Mock<IRepository<ModuleMasterEntity, int>>().Object,
            designationRepository: new Mock<IRepository<AssetDesignationEntity, int>>().Object,
            amsTypeOfUseRepository: new Mock<IRepository<AssetTypeOfUseMasterEntity, int>>().Object,
            amsSubTypeOfUseRepository: new Mock<IRepository<AssetSubTypeOfUseEntity, int>>().Object,
            logger: new Mock<ILogger<AssetMasterService>>().Object,
            inventoryBatchRepository: inventoryBatchRepository.Object,
            inventoryAssetDetailRepository: inventoryAssetDetailRepository.Object,
            inventoryCategoryRepository: inventoryCategoryRepository.Object,
            inventoryNameRepository: inventoryNameRepository.Object,
            inventoryModelRepository: inventoryModelRepository.Object,
            inventoryDepartmentRepository: inventoryDepartmentRepository.Object,
            inventoryDocumentApplicationService: inventoryDocumentApplicationService.Object,
            leaseRentDetailsRepository: new Mock<IRepository<AssetLeaseRentDetailsEntity, int>>().Object);
    }

    [Fact]
    public async Task BuildInventoryDataAsync_AggregatesBatchesUnitsLookupsAndDocuments()
    {
        var service = CreateService(
            out var assetRepository,
            out var inventoryBatchRepository,
            out var inventoryAssetDetailRepository,
            out var inventoryCategoryRepository,
            out var inventoryNameRepository,
            out var inventoryModelRepository,
            out var conditionRepository,
            out var inventoryDepartmentRepository,
            out var inventoryDocumentApplicationService);

        const int parentAssetId = 100;

        assetRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity>
        {
            new AssetMasterEntity { Id = parentAssetId, AssetName = "Building A" }
        }.BuildMockDbSet().Object);

        var batch = new InventoryBatchEntity
        {
            Id = 1,
            ParentAssetId = parentAssetId,
            IsActive = true,
            MarkedForDeletion = false,
            InventoryItemCategoryId = 10,
            InventoryItemNameId = 20,
            InventoryItemModelId = 30,
            ConditionId = 40,
            OwningDepartmentId = 50,
            Quantity = 2,
            UnitValue = 100m,
            TotalBatchValue = 200m,
            TotalBatchCV = null, // forces fallback: sum of unit capital values
            InvoiceNumber = "INV-1",
            PurchaseDate = new DateTime(2024, 1, 1),
            CreatedDate = new DateTime(2024, 1, 2)
        };
        inventoryBatchRepository.Setup(r => r.GetQueryable())
            .Returns(new List<InventoryBatchEntity> { batch }.BuildMockDbSet().Object);

        var unit = new InventoryAssetDetailEntity
        {
            Id = 1,
            BatchId = 1,
            AssetId = 501,
            UnitNumber = 1,
            IsActive = true,
            MarkedForDeletion = false,
            InventoryItemConditionId = 40,
            UnitPurchaseValue = 100m,
            UnitCapitalValue = 90m
        };
        inventoryAssetDetailRepository.Setup(r => r.GetQueryable())
            .Returns(new List<InventoryAssetDetailEntity> { unit }.BuildMockDbSet().Object);

        inventoryCategoryRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemCategoryEntity>
        {
            new InventoryItemCategoryEntity { Id = 10, TypeName = "Furniture" }
        }.BuildMockDbSet().Object);

        inventoryNameRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemNameEntity>
        {
            new InventoryItemNameEntity { Id = 20, SubTypeName = "Chair" }
        }.BuildMockDbSet().Object);

        inventoryModelRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemModelEntity>
        {
            new InventoryItemModelEntity { Id = 30, ModelName = "ModelX" }
        }.BuildMockDbSet().Object);

        conditionRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetConditionMasterEntity>
        {
            new AssetConditionMasterEntity { Id = 40, ConditionName = "Good" }
        }.BuildMockDbSet().Object);

        inventoryDepartmentRepository.Setup(r => r.GetQueryable()).Returns(new List<OwningDepartmentEntity>
        {
            new OwningDepartmentEntity { Id = 50, OwningDepartmentName = "IT Department" }
        }.BuildMockDbSet().Object);

        var docs = new List<InventoryDocumentDto>
        {
            new InventoryDocumentDto { InventoryDocumentId = 1, InventoryBatchId = 1, DocumentTypeName = "Invoice" }
        };
        inventoryDocumentApplicationService
            .Setup(s => s.GetDocumentsByInventoryBatchesAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, List<InventoryDocumentDto>> { { 1, docs } });

        var result = await service.BuildInventoryDataAsync(parentAssetId, CancellationToken.None);

        Assert.Equal(parentAssetId, result.ParentAssetId);
        Assert.Equal("Building A", result.ParentAssetName);
        Assert.Equal(1, result.TotalBatches);
        Assert.Equal(2, result.TotalUnits);

        var batchDto = Assert.Single(result.Batches);
        Assert.Equal(1, batchDto.BatchId);
        Assert.Equal(parentAssetId, batchDto.ParentAssetId);
        Assert.Equal("Furniture", batchDto.Names.InventoryType);
        Assert.Equal("Chair", batchDto.Names.ItemName);
        Assert.Equal("ModelX", batchDto.Names.ModelBrand);
        Assert.Equal("Good", batchDto.Names.Condition);
        Assert.Equal("IT Department", batchDto.Names.OwningDepartment);
        // TotalBatchCV on the entity is null, so the service falls back to the sum of unit capital values.
        Assert.Equal(90m, batchDto.TotalBatchCV);

        var unitDto = Assert.Single(batchDto.Units);
        Assert.Equal(501, unitDto.AssetId);
        Assert.Equal(1, unitDto.UnitNumber);
        Assert.Equal("Good", unitDto.Condition);
        Assert.Equal(90m, unitDto.UnitCapitalValue);

        var docDto = Assert.Single(batchDto.Documents);
        Assert.Equal("Invoice", docDto.DocumentTypeName);
    }

    [Fact]
    public async Task BuildInventoryDataAsync_WithNoBatches_ReturnsEmptyResponse()
    {
        var service = CreateService(
            out _, out var inventoryBatchRepository, out _, out _, out _, out _, out _, out _, out _);

        inventoryBatchRepository.Setup(r => r.GetQueryable())
            .Returns(new List<InventoryBatchEntity>().BuildMockDbSet().Object);

        var result = await service.BuildInventoryDataAsync(999, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, result.TotalBatches);
        Assert.Empty(result.Batches);
        Assert.Equal(0, result.TotalUnits);
        Assert.Equal(0m, result.TotalPurchaseValue);
        Assert.Equal(0m, result.TotalCapitalValue);
    }

    [Fact]
    public async Task GetAllocatedAssetIdsAsync_ReturnsAssetIdsAlreadyLinkedToInventoryUnits()
    {
        var service = CreateService(
            out var assetRepository, out _, out var inventoryAssetDetailRepository,
            out _, out _, out _, out _, out _, out _);

        const int parentAssetId = 200;

        assetRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity>
        {
            new AssetMasterEntity { Id = 501, ParentAssetId = parentAssetId },
            new AssetMasterEntity { Id = 502, ParentAssetId = parentAssetId },
            new AssetMasterEntity { Id = 503, ParentAssetId = 999 } // different parent — must be excluded
        }.BuildMockDbSet().Object);

        inventoryAssetDetailRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryAssetDetailEntity>
        {
            new InventoryAssetDetailEntity { Id = 1, BatchId = 1, AssetId = 501 },
            new InventoryAssetDetailEntity { Id = 2, BatchId = 1, AssetId = 502 },
            new InventoryAssetDetailEntity { Id = 3, BatchId = 1, AssetId = 503 }
        }.BuildMockDbSet().Object);

        var result = await service.GetAllocatedAssetIdsAsync(parentAssetId, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(501, result);
        Assert.Contains(502, result);
        Assert.DoesNotContain(503, result);
    }
}
