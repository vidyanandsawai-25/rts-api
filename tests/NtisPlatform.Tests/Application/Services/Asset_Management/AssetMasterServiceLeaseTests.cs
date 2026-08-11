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

/// <summary>
/// Covers AssetMasterService.Lease.cs: GetShopWiseDetailsByParentAssetIdAsync,
/// ActivateLeaseRentDetailsAsync, GetRenterDetailsAsync, and the private
/// GetShopNumber/GetShopName/GetStatusFromOccupancy helpers (exercised indirectly, since they are
/// private static).
/// </summary>
public class AssetMasterServiceLeaseTests
{
    private static IMapper CreateMapper()
    {
        var mapperConfig = new MapperConfiguration(
            cfg => cfg.AddProfile<AssetMasterMappingProfile>(),
            NullLoggerFactory.Instance);
        return mapperConfig.CreateMapper();
    }

    /// <summary>
    /// Builds an AssetMasterService with only the dependencies exercised by the Lease.cs methods
    /// exposed as out-params. Every other constructor dependency (there are ~30 total) is
    /// defaulted to a bare Mock&lt;T&gt;.Object. Queryable-backed repositories default to an empty
    /// MockQueryable-backed set unless a test overrides the setup.
    /// </summary>
    private static AssetMasterService CreateService(
        out Mock<IRepository<AssetMasterEntity, int>> repository,
        out Mock<IRepository<AssetLeaseRentDetailsEntity, int>> leaseRentDetailsRepository,
        out Mock<IRepository<SubUnitsDetailsEntity, int>> floorDetailsRepository,
        out Mock<IUnitOfWork> unitOfWork)
    {
        repository = new Mock<IRepository<AssetMasterEntity, int>>();
        leaseRentDetailsRepository = new Mock<IRepository<AssetLeaseRentDetailsEntity, int>>();
        floorDetailsRepository = new Mock<IRepository<SubUnitsDetailsEntity, int>>();
        unitOfWork = new Mock<IUnitOfWork>();

        repository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity>().BuildMockDbSet().Object);
        leaseRentDetailsRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetLeaseRentDetailsEntity>().BuildMockDbSet().Object);
        floorDetailsRepository.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity>().BuildMockDbSet().Object);

        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

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
            new Mock<IRepository<AssetDetailsEntity, int>>().Object,
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

    [Fact]
    public async Task GetShopWiseDetailsByParentAssetIdAsync_ComputesAgreementPeriodString()
    {
        var service = CreateService(out var repository, out var leaseRentDetailsRepository, out _, out _);

        var asset = new AssetMasterEntity
        {
            Id = 1,
            ParentAssetId = 100,
            AssetNo = "AST-100-1",
            AssetName = "Shop 5",
            IsActive = true,
            MarkedForDeletion = false
        };
        repository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { asset }.BuildMockDbSet().Object);

        var fromDate = new DateTime(2023, 1, 1);
        var toDate = new DateTime(2025, 1, 1); // ~2 years later
        var lease = new AssetLeaseRentDetailsEntity
        {
            Id = 10,
            AssetId = 1,
            TenantName = "Acme Traders",
            TenantMobile = "9999999999",
            TotalAreaSqFt = 500,
            RentAmount = 15000,
            LeaseStartDate = fromDate,
            LeaseEndDate = toDate,
            IsActive = true,
            MarkedForDeletion = false
        };
        leaseRentDetailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetLeaseRentDetailsEntity> { lease }.BuildMockDbSet().Object);

        var result = await service.GetShopWiseDetailsByParentAssetIdAsync(100, CancellationToken.None);

        var shop = Assert.Single(result);
        Assert.Equal("AST-100-1", shop.AssetId);
        Assert.Equal("Acme Traders", shop.Occupier);
        Assert.Equal(500, shop.Area);
        Assert.Equal(15000, shop.AnnualRent);
        Assert.NotNull(shop.AgreementPeriod);
        Assert.Contains("2023-01-01", shop.AgreementPeriod);
        Assert.Contains("2 years", shop.AgreementPeriod);
    }

    [Fact]
    public async Task GetShopWiseDetailsByParentAssetIdAsync_WithNoLeaseRent_AgreementPeriodIsNA()
    {
        var service = CreateService(out var repository, out var leaseRentDetailsRepository, out _, out _);

        var asset = new AssetMasterEntity
        {
            Id = 1,
            ParentAssetId = 100,
            AssetNo = "AST-100-1",
            AssetName = "Shop 5",
            IsActive = true,
            MarkedForDeletion = false
        };
        repository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { asset }.BuildMockDbSet().Object);
        leaseRentDetailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetLeaseRentDetailsEntity>().BuildMockDbSet().Object);

        var result = await service.GetShopWiseDetailsByParentAssetIdAsync(100, CancellationToken.None);

        var shop = Assert.Single(result);
        Assert.Equal("N/A", shop.AgreementPeriod);
        Assert.Equal("Vacant", shop.Occupier);
        Assert.Equal("N/A", shop.Contact);
        Assert.Equal(0m, shop.AnnualRent);
    }

    [Theory]
    [InlineData("Occupied", "Paid")]
    [InlineData("Vacant", "Vacant")]
    [InlineData("Leased", "Paid")]
    [InlineData("Rented", "Paid")]
    [InlineData("SomethingElse", "SomethingElse")]
    [InlineData(null, "Unknown")]
    [InlineData("", "Unknown")]
    public async Task GetShopWiseDetailsByParentAssetIdAsync_MapsStatusFromOccupancy(string? occupancyStatus, string expectedStatus)
    {
        var service = CreateService(out var repository, out var leaseRentDetailsRepository, out _, out _);

        var asset = new AssetMasterEntity
        {
            Id = 1,
            ParentAssetId = 100,
            AssetNo = "AST-100-1",
            AssetName = "Shop 5",
            OccupancyStatus = occupancyStatus,
            IsActive = true,
            MarkedForDeletion = false
        };
        repository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { asset }.BuildMockDbSet().Object);
        leaseRentDetailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetLeaseRentDetailsEntity>().BuildMockDbSet().Object);

        var result = await service.GetShopWiseDetailsByParentAssetIdAsync(100, CancellationToken.None);

        var shop = Assert.Single(result);
        Assert.Equal(expectedStatus, shop.Status);
    }

    [Theory]
    [InlineData("Shop 12", "12")] // numeric token present -> returned as-is
    [InlineData("Unit 12B", "Unit 12B")] // "12B" is not purely numeric -> falls back to the full asset name
    [InlineData("Shop A", "Shop A")] // no numeric token at all -> falls back to the full asset name
    public async Task GetShopWiseDetailsByParentAssetIdAsync_ShopNumberResolvesNumericTokenOrFallsBackToFullName(
        string assetName, string expectedShopNo)
    {
        var service = CreateService(out var repository, out var leaseRentDetailsRepository, out _, out _);

        var asset = new AssetMasterEntity
        {
            Id = 1,
            ParentAssetId = 100,
            AssetNo = "AST-100-1",
            AssetName = assetName,
            IsActive = true,
            MarkedForDeletion = false
        };
        repository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { asset }.BuildMockDbSet().Object);
        leaseRentDetailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetLeaseRentDetailsEntity>().BuildMockDbSet().Object);

        var result = await service.GetShopWiseDetailsByParentAssetIdAsync(100, CancellationToken.None);

        var shop = Assert.Single(result);
        Assert.Equal(expectedShopNo, shop.ShopNo);
    }

    [Fact]
    public async Task GetShopWiseDetailsByParentAssetIdAsync_ShopNumberFallsBackToNA_WhenAssetNameIsEmpty()
    {
        var service = CreateService(out var repository, out var leaseRentDetailsRepository, out _, out _);

        var asset = new AssetMasterEntity
        {
            Id = 1,
            ParentAssetId = 100,
            AssetNo = "AST-100-1",
            AssetName = string.Empty,
            IsActive = true,
            MarkedForDeletion = false
        };
        repository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { asset }.BuildMockDbSet().Object);
        leaseRentDetailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetLeaseRentDetailsEntity>().BuildMockDbSet().Object);

        var result = await service.GetShopWiseDetailsByParentAssetIdAsync(100, CancellationToken.None);

        var shop = Assert.Single(result);
        Assert.Equal("N/A", shop.ShopNo);
    }

    [Fact]
    public async Task GetShopWiseDetailsByParentAssetIdAsync_ShopNameUsesTenantName_WhenPresent_OtherwiseAssetName()
    {
        var service = CreateService(out var repository, out var leaseRentDetailsRepository, out _, out _);

        var assetWithTenant = new AssetMasterEntity
        {
            Id = 1,
            ParentAssetId = 100,
            AssetNo = "AST-100-1",
            AssetName = "Shop 1",
            IsActive = true,
            MarkedForDeletion = false
        };
        var assetWithoutTenant = new AssetMasterEntity
        {
            Id = 2,
            ParentAssetId = 100,
            AssetNo = "AST-100-2",
            AssetName = "Shop 2",
            IsActive = true,
            MarkedForDeletion = false
        };
        repository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetMasterEntity> { assetWithTenant, assetWithoutTenant }.BuildMockDbSet().Object);

        var leaseWithTenant = new AssetLeaseRentDetailsEntity
        {
            Id = 10,
            AssetId = 1,
            TenantName = "Acme Corp",
            LeaseStartDate = DateTime.UtcNow,
            IsActive = true,
            MarkedForDeletion = false
        };
        leaseRentDetailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetLeaseRentDetailsEntity> { leaseWithTenant }.BuildMockDbSet().Object);

        var result = await service.GetShopWiseDetailsByParentAssetIdAsync(100, CancellationToken.None);

        var shop1 = result.Single(r => r.AssetId == "AST-100-1");
        Assert.Equal("Acme Corp", shop1.ShopName);

        // Asset 2 has no lease row at all, so LeaseRent is null and TenantName resolves to null ->
        // GetShopName falls back to the asset's own name.
        var shop2 = result.Single(r => r.AssetId == "AST-100-2");
        Assert.Equal("Shop 2", shop2.ShopName);
    }

    [Fact]
    public async Task ActivateLeaseRentDetailsAsync_SetsIsActiveTrue_ForGivenAssetIds_DoesNotItselfCallSaveChanges()
    {
        var service = CreateService(out _, out var leaseRentDetailsRepository, out _, out var unitOfWork);

        var matching1 = new AssetLeaseRentDetailsEntity { Id = 1, AssetId = 10, IsActive = false, MarkedForDeletion = false };
        var matching2 = new AssetLeaseRentDetailsEntity { Id = 2, AssetId = 20, IsActive = false, MarkedForDeletion = false };
        var unrelated = new AssetLeaseRentDetailsEntity { Id = 3, AssetId = 30, IsActive = false, MarkedForDeletion = false };
        var markedForDeletion = new AssetLeaseRentDetailsEntity { Id = 4, AssetId = 10, IsActive = false, MarkedForDeletion = true };

        leaseRentDetailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetLeaseRentDetailsEntity> { matching1, matching2, unrelated, markedForDeletion }.BuildMockDbSet().Object);

        var now = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);

        await service.ActivateLeaseRentDetailsAsync(new List<int> { 10, 20 }, now, CancellationToken.None);

        Assert.True(matching1.IsActive);
        Assert.Equal(now, matching1.UpdatedDate);
        Assert.True(matching2.IsActive);
        Assert.Equal(now, matching2.UpdatedDate);

        // Not in the requested asset id list -> left untouched.
        Assert.False(unrelated.IsActive);
        // Marked for deletion -> excluded even though its AssetId matches.
        Assert.False(markedForDeletion.IsActive);

        // Per the source comment, activation here does not itself persist - the caller is
        // responsible for calling SaveChangesAsync.
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRenterDetailsAsync_ReturnsOnlyActiveNonDeletedLeaseRentRows_ForGivenSubAssetIds()
    {
        var service = CreateService(out _, out var leaseRentDetailsRepository, out _, out _);

        var asset = new AssetMasterEntity { Id = 10, AssetNo = "AST-010", AssetName = "Shop 10" };

        var activeIncluded = new AssetLeaseRentDetailsEntity
        {
            Id = 1,
            AssetId = 10,
            TenantName = "Valid Tenant",
            TenantMobile = "9000000000",
            IsActive = true,
            MarkedForDeletion = false,
            Asset = asset
        };
        var inactiveExcluded = new AssetLeaseRentDetailsEntity
        {
            Id = 2,
            AssetId = 10,
            TenantName = "Inactive Tenant",
            IsActive = false,
            MarkedForDeletion = false
        };
        var markedForDeletionExcluded = new AssetLeaseRentDetailsEntity
        {
            Id = 3,
            AssetId = 10,
            TenantName = "Deleted Tenant",
            IsActive = true,
            MarkedForDeletion = true
        };
        var notRequestedExcluded = new AssetLeaseRentDetailsEntity
        {
            Id = 4,
            AssetId = 999,
            TenantName = "Other Asset Tenant",
            IsActive = true,
            MarkedForDeletion = false
        };

        leaseRentDetailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetLeaseRentDetailsEntity>
            {
                activeIncluded, inactiveExcluded, markedForDeletionExcluded, notRequestedExcluded
            }.BuildMockDbSet().Object);

        var result = await service.GetRenterDetailsAsync(new List<int> { 10 }, CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(1, dto.Id);
        Assert.Equal(10, dto.AssetId);
        Assert.Equal("Valid Tenant", dto.TenantName);
        Assert.Equal("AST-010", dto.Names.AssetNo);
        Assert.Equal("Shop 10", dto.Names.AssetName);
    }
}
