using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
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
/// Covers AssetMasterService.Excel.cs (ExportToExcelAsync). Delegates to GetAllAsync
/// (AssetMasterService.Crud.cs's GetAllInternalAsync), so the service under test here is exercised
/// through the real query pipeline rather than a stubbed-out GetAllAsync — only the repositories
/// that pipeline touches are mocked.
/// (ExportToExcelForUserAsync/GetAllForUserAsync were removed 2026-08-03 -- they never actually
/// scoped by currentUserId; see AssetMaster-TestCoverage-Roadmap.md.)
/// </summary>
public class AssetMasterServiceExcelTests
{
    private static readonly string[] ExpectedHeaders =
    {
        "Asset ID",
        "Asset Name",
        "Asset Category",
        "Asset Type",
        "Owning Department",
        "Capital Value",
        "Ownership Type",
        "Condition",
        "Life (Yrs)",
        "Address"
    };

    private static IMapper CreateMapper()
    {
        var mapperConfig = new MapperConfiguration(
            cfg => cfg.AddProfile<AssetMasterMappingProfile>(),
            NullLoggerFactory.Instance);
        return mapperConfig.CreateMapper();
    }

    /// <summary>
    /// Builds an AssetMasterService with only the dependencies exercised by GetAllInternalAsync
    /// (the query pipeline ExportToExcelAsync delegates to) exposed as
    /// out-params. Every other constructor dependency (there are ~30 total) is defaulted to a bare
    /// Mock&lt;T&gt;.Object. All queryable-backed repositories default to an empty
    /// MockQueryable-backed set unless a test overrides the setup, so GetAllInternalAsync's
    /// enrichment joins (EnrichLocationAsync's Zone/Ward/Mouja/SubZone/Organization/Department
    /// lookups, the capital-value summation, the condition-name lookup) run over zero rows
    /// without null-refing.
    /// </summary>
    private static AssetMasterService CreateService(
        out Mock<IRepository<AssetMasterEntity, int>> repository,
        out Mock<IRepository<AssetDetailsEntity, int>> detailsRepository,
        out Mock<IRepository<SubUnitsDetailsEntity, int>> floorDetailsRepository,
        out Mock<IRepository<AssetPhotoEntity, int>> assetPhotoRepository,
        out Mock<IRepository<AssetConditionMasterEntity, int>> conditionRepository,
        out Mock<IRepository<OwningDepartmentEntity, int>> departmentRepository)
    {
        repository = new Mock<IRepository<AssetMasterEntity, int>>();
        detailsRepository = new Mock<IRepository<AssetDetailsEntity, int>>();
        floorDetailsRepository = new Mock<IRepository<SubUnitsDetailsEntity, int>>();
        assetPhotoRepository = new Mock<IRepository<AssetPhotoEntity, int>>();
        conditionRepository = new Mock<IRepository<AssetConditionMasterEntity, int>>();
        departmentRepository = new Mock<IRepository<OwningDepartmentEntity, int>>();

        // Defaults: empty queryables for every repository GetAllInternalAsync/EnrichLocationAsync
        // touches, so the pipeline runs over zero rows unless a test overrides a specific setup.
        repository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity>().BuildMockDbSet().Object);
        detailsRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetDetailsEntity>().BuildMockDbSet().Object);
        floorDetailsRepository.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity>().BuildMockDbSet().Object);
        assetPhotoRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetPhotoEntity>().BuildMockDbSet().Object);
        conditionRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetConditionMasterEntity>().BuildMockDbSet().Object);
        departmentRepository.Setup(r => r.GetQueryable()).Returns(new List<OwningDepartmentEntity>().BuildMockDbSet().Object);

        var zoneRepository = new Mock<IRepository<ZoneEntity, int>>();
        zoneRepository.Setup(r => r.GetQueryable()).Returns(new List<ZoneEntity>().BuildMockDbSet().Object);
        var wardRepository = new Mock<IRepository<WardEntity, int>>();
        wardRepository.Setup(r => r.GetQueryable()).Returns(new List<WardEntity>().BuildMockDbSet().Object);
        var moujaRepository = new Mock<IRepository<MoujaEntity, int>>();
        moujaRepository.Setup(r => r.GetQueryable()).Returns(new List<MoujaEntity>().BuildMockDbSet().Object);
        var subZoneRepository = new Mock<IRepository<SubZoneDetailsForCVEntity, int>>();
        subZoneRepository.Setup(r => r.GetQueryable()).Returns(new List<SubZoneDetailsForCVEntity>().BuildMockDbSet().Object);
        var organizationRepository = new Mock<IRepository<AssetOrganizationMasterEntity, int>>();
        organizationRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetOrganizationMasterEntity>().BuildMockDbSet().Object);

        var unitOfWork = new Mock<IUnitOfWork>();
        var mapper = CreateMapper();
        var logger = new Mock<ILogger<AssetMasterService>>();

        return new AssetMasterService(
            repository.Object,
            unitOfWork.Object,
            mapper,
            new Mock<IReferenceValidationService>().Object,
            new Mock<IRepository<AssetFieldValueEntity, int>>().Object,
            floorDetailsRepository.Object,
            new Mock<IRepository<AssetRoomWiseSubmissionDetailsEntity, int>>().Object,
            new Mock<IRepository<AssetCategoryEntity, int>>().Object,
            new Mock<IRepository<AssetTypeEntity, int>>().Object,
            new Mock<IRepository<ULBMasterEntity, int>>().Object,
            detailsRepository.Object,
            new Mock<IRepository<AssetDocumentEntity, int>>().Object,
            assetPhotoRepository.Object,
            new Mock<IAssetPhotoApplicationService>().Object,
            new Mock<IDocumentApplicationService>().Object,
            zoneRepository.Object,
            wardRepository.Object,
            moujaRepository.Object,
            subZoneRepository.Object,
            departmentRepository.Object,
            organizationRepository.Object,
            conditionRepository.Object,
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
            new Mock<IRepository<AssetLeaseRentDetailsEntity, int>>().Object);
    }

    [Fact]
    public async Task ExportToExcelAsync_ProducesWorkbookWithExpectedColumnHeaders()
    {
        var service = CreateService(out _, out _, out _, out _, out _, out _);

        var queryParameters = new AssetMasterQueryParameters();

        var bytes = await service.ExportToExcelAsync(queryParameters, CancellationToken.None);

        Assert.NotEmpty(bytes);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheet(1);
        var headerRow = worksheet.Row(1);

        for (var i = 0; i < ExpectedHeaders.Length; i++)
        {
            Assert.Equal(ExpectedHeaders[i], headerRow.Cell(i + 1).GetString());
        }

        // No assets were returned, so the sheet should contain the header row only.
        var usedRange = worksheet.RangeUsed()!;
        Assert.Equal(1, usedRange.RowCount());
        Assert.Equal(ExpectedHeaders.Length, usedRange.ColumnCount());
    }

    [Fact]
    public async Task ExportToExcelAsync_WritesOneRowPerAsset_WithCorrectValues()
    {
        var service = CreateService(
            out var repository,
            out var detailsRepository,
            out var floorDetailsRepository,
            out _,
            out var conditionRepository,
            out var departmentRepository);

        var category = new AssetCategoryEntity { Id = 10, CategoryName = "Commercial Building" };
        var assetType = new AssetTypeEntity { Id = 20, TypeName = "Shop" };

        var asset1 = new AssetMasterEntity
        {
            Id = 1,
            AssetNo = "AST-0001",
            AssetName = "Municipal Market",
            AssetCategoryId = 10,
            AssetTypeId = 20,
            DepartmentId = 5,
            OwnershipType = "Freehold",
            AssetConditionId = 7,
            IsActive = true,
            MarkedForDeletion = false,
            AssetCategory = category,
            AssetType = assetType
        };

        repository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetMasterEntity> { asset1 }.BuildMockDbSet().Object);

        conditionRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetConditionMasterEntity>
            {
                new AssetConditionMasterEntity { Id = 7, ConditionName = "Good" }
            }.BuildMockDbSet().Object);

        departmentRepository.Setup(r => r.GetQueryable())
            .Returns(new List<OwningDepartmentEntity>
            {
                new OwningDepartmentEntity { Id = 5, OwningDepartmentName = "Public Works" }
            }.BuildMockDbSet().Object);

        detailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetDetailsEntity>
            {
                new AssetDetailsEntity { AssetId = 1, OrganizationId = 1, Address = "12 Market Road" }
            }.BuildMockDbSet().Object);

        // A single floor-details row on the asset itself (not a child) so CapitalValue sums to a
        // known value; Asset must be wired up because the production query dereferences
        // fd.Asset!.ParentAssetId unconditionally in its projection.
        var floorRow = new SubUnitsDetailsEntity
        {
            Id = 100,
            AssetId = 1,
            CapitalValue = 250000m,
            IsActive = true,
            MarkedForDeletion = false,
            Asset = asset1
        };
        floorDetailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<SubUnitsDetailsEntity> { floorRow }.BuildMockDbSet().Object);

        var queryParameters = new AssetMasterQueryParameters();

        var bytes = await service.ExportToExcelAsync(queryParameters, CancellationToken.None);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheet(1);

        Assert.Equal("AST-0001", worksheet.Cell(2, 1).GetString());
        Assert.Equal("Municipal Market", worksheet.Cell(2, 2).GetString());
        Assert.Equal("Commercial Building", worksheet.Cell(2, 3).GetString());
        Assert.Equal("Shop", worksheet.Cell(2, 4).GetString());
        Assert.Equal("Public Works", worksheet.Cell(2, 5).GetString());
        Assert.Equal(250000m, worksheet.Cell(2, 6).GetValue<decimal>());
        Assert.Equal("Freehold", worksheet.Cell(2, 7).GetString());
        Assert.Equal("Good", worksheet.Cell(2, 8).GetString());
        // AssetLife is derived from child sub-units' construction years; this asset has none.
        Assert.Equal("-", worksheet.Cell(2, 9).GetString());
        Assert.Equal("12 Market Road", worksheet.Cell(2, 10).GetString());
    }

    [Fact]
    public async Task ExportToExcelAsync_ForcesPageSizeNegativeOne_ReturnsAllMatchingRows()
    {
        var service = CreateService(out var repository, out var detailsRepository, out _, out _, out _, out _);

        var assets = Enumerable.Range(1, 15)
            .Select(i => new AssetMasterEntity
            {
                Id = i,
                AssetNo = $"AST-{i:D4}",
                AssetName = $"Asset {i}",
                IsActive = true,
                MarkedForDeletion = false
            })
            .ToList();
        repository.Setup(r => r.GetQueryable()).Returns(assets.BuildMockDbSet().Object);

        // GetLocationInfoByAssetIdsAsync's join dereferences details.ZoneId/WardId/... straight
        // after `detailsGroup.DefaultIfEmpty()`. Against a real EF Core SQL provider that's a safe
        // LEFT JOIN (NULL propagates in SQL); against MockQueryable's LINQ-to-Objects provider a
        // genuinely-missing AssetDetails row would NullReferenceException instead. So every asset
        // here needs a matching AssetDetailsEntity row purely to keep the in-memory join safe.
        var details = assets.Select(a => new AssetDetailsEntity { AssetId = a.Id, OrganizationId = 1 }).ToList();
        detailsRepository.Setup(r => r.GetQueryable()).Returns(details.BuildMockDbSet().Object);

        // Caller-supplied PageSize is well below the number of matching rows; the export path
        // must force PageSize to -1 internally so ALL rows come back, not just a page's worth.
        var queryParameters = new AssetMasterQueryParameters { PageSize = 10, PageNumber = 1 };

        var bytes = await service.ExportToExcelAsync(queryParameters, CancellationToken.None);

        Assert.Equal(-1, queryParameters.PageSize);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheet(1);
        var usedRange = worksheet.RangeUsed()!;
        // Header row + all 15 assets - not capped at the originally-requested PageSize of 10.
        Assert.Equal(16, usedRange.RowCount());
    }
}
