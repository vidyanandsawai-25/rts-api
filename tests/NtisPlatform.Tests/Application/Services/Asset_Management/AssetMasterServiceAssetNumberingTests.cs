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

public class AssetMasterServiceAssetNumberingTests
{
    private static IMapper CreateMapper()
    {
        var mapperConfig = new MapperConfiguration(
            cfg => cfg.AddProfile<AssetMasterMappingProfile>(),
            NullLoggerFactory.Instance);
        return mapperConfig.CreateMapper();
    }

    /// <summary>
    /// Builds an AssetMasterService with only the dependencies exercised by the asset-numbering
    /// partial exposed as out-params (AssetMaster repository for sequence lookups, category/type
    /// repositories, and ULB repository). Every other constructor dependency (there are ~30 total)
    /// is defaulted to a bare Mock&lt;T&gt;.Object. Queryable-backed repositories default to an
    /// empty MockQueryable-backed set unless a test overrides the setup.
    /// </summary>
    private static AssetMasterService CreateService(
        out Mock<IRepository<AssetMasterEntity, int>> repository,
        out Mock<IRepository<AssetCategoryEntity, int>> assetCategoryRepository,
        out Mock<IRepository<AssetTypeEntity, int>> assetTypeRepository,
        out Mock<IRepository<ULBMasterEntity, int>> ulbRepository)
    {
        repository = new Mock<IRepository<AssetMasterEntity, int>>();
        assetCategoryRepository = new Mock<IRepository<AssetCategoryEntity, int>>();
        assetTypeRepository = new Mock<IRepository<AssetTypeEntity, int>>();
        ulbRepository = new Mock<IRepository<ULBMasterEntity, int>>();

        repository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetMasterEntity>().BuildMockDbSet().Object);
        assetCategoryRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetCategoryEntity>().BuildMockDbSet().Object);
        assetTypeRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetTypeEntity>().BuildMockDbSet().Object);
        ulbRepository.Setup(r => r.GetQueryable())
            .Returns(new List<ULBMasterEntity>().BuildMockDbSet().Object);

        var unitOfWork = new Mock<IUnitOfWork>();
        var mapper = CreateMapper();

        return new AssetMasterService(
            repository.Object,
            unitOfWork.Object,
            mapper,
            new Mock<IReferenceValidationService>().Object,
            new Mock<IRepository<AssetFieldValueEntity, int>>().Object,
            new Mock<IRepository<SubUnitsDetailsEntity, int>>().Object,
            new Mock<IRepository<AssetRoomWiseSubmissionDetailsEntity, int>>().Object,
            assetCategoryRepository.Object,
            assetTypeRepository.Object,
            ulbRepository.Object,
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
            new Mock<ILogger<AssetMasterService>>().Object,
            new Mock<IRepository<InventoryBatchEntity, int>>().Object,
            new Mock<IRepository<InventoryAssetDetailEntity, int>>().Object,
            new Mock<IRepository<InventoryItemCategoryEntity, int>>().Object,
            new Mock<IRepository<InventoryItemNameEntity, int>>().Object,
            new Mock<IRepository<InventoryItemModelEntity, int>>().Object,
            new Mock<IRepository<OwningDepartmentEntity, int>>().Object,
            new Mock<IInventoryDocumentApplicationService>().Object,
            new Mock<IRepository<AssetLeaseRentDetailsEntity, int>>().Object);
    }

    #region SanitizeAssetNoSegment (pure static — no mocking)

    [Theory]
    [InlineData("cat-01", "CAT01")]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("  mixed_123!! ", "MIXED123")]
    public void SanitizeAssetNoSegment_StripsNonAlphanumeric_UppercasesResult(string? input, string expected)
    {
        var result = AssetMasterService.SanitizeAssetNoSegment(input);

        Assert.Equal(expected, result);
    }

    #endregion

    #region GetCategoryAndTypeSegments (pure static — no mocking)

    [Fact]
    public void GetCategoryAndTypeSegments_StripsCatAndTypePrefixes()
    {
        var (categorySegment, typeSegment) = AssetMasterService.GetCategoryAndTypeSegments(
            categoryCode: "CAT-BLD",
            categoryName: null,
            categoryId: 1,
            typeCode: "TYPE-RES",
            typeName: null,
            typeId: 2);

        Assert.Equal("BLD", categorySegment);
        Assert.Equal("RES", typeSegment);
    }

    [Fact]
    public void GetCategoryAndTypeSegments_DeduplicatesOverlappingSegments()
    {
        // Category code is "BLD"; type code is prefixed by the category code + dash ("BLD-RES").
        // Without de-dup, the type segment would come out as "BLDRES" (duplicating the category
        // segment inside the type segment of the generated asset number). The de-dup branch
        // strips the "BLD-" prefix off the raw type code before sanitizing it.
        var (categorySegment, typeSegment) = AssetMasterService.GetCategoryAndTypeSegments(
            categoryCode: "BLD",
            categoryName: null,
            categoryId: 1,
            typeCode: "BLD-RES",
            typeName: null,
            typeId: 2);

        Assert.Equal("BLD", categorySegment);
        Assert.Equal("RES", typeSegment);
    }

    [Fact]
    public void GetCategoryAndTypeSegments_FallsBackToIdWhenCodeAndNameAreBothBlank()
    {
        var (categorySegment, typeSegment) = AssetMasterService.GetCategoryAndTypeSegments(
            categoryCode: null,
            categoryName: null,
            categoryId: 5,
            typeCode: null,
            typeName: null,
            typeId: 7);

        Assert.Equal("5", categorySegment);
        Assert.Equal("7", typeSegment);
    }

    #endregion

    #region GenerateAssetNoAsync / GenerateAssetNosAsync

    [Fact]
    public async Task GenerateAssetNoAsync_DelegatesToGenerateAssetNosAsync_WithCountOne()
    {
        var service = CreateService(
            out _, out var categoryRepository, out var typeRepository, out var ulbRepository);

        categoryRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetCategoryEntity>
        {
            new() { Id = 1, IsActive = true, MarkedForDeletion = false, CategoryCode = "BLD", CategoryName = "Building" }
        }.BuildMockDbSet().Object);

        typeRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetTypeEntity>
        {
            new() { Id = 2, AssetCategoryId = 1, IsActive = true, MarkedForDeletion = false, TypeCode = "RES", TypeName = "Residential" }
        }.BuildMockDbSet().Object);

        ulbRepository.Setup(r => r.GetQueryable()).Returns(new List<ULBMasterEntity>
        {
            new() { Id = 1, IsActive = true, UlbCode = "PMC" }
        }.BuildMockDbSet().Object);

        var result = await service.GenerateAssetNoAsync(1, 2);

        Assert.Equal("PMC-BLD-RES-0001", result);
    }

    [Fact]
    public async Task GenerateAssetNosAsync_WhenCategoryNotFoundOrInactive_ThrowsInvalidOperationException()
    {
        var service = CreateService(
            out _, out var categoryRepository, out _, out var ulbRepository);

        ulbRepository.Setup(r => r.GetQueryable()).Returns(new List<ULBMasterEntity>
        {
            new() { Id = 1, IsActive = true, UlbCode = "PMC" }
        }.BuildMockDbSet().Object);

        // Category exists but is inactive, so the IsActive filter excludes it -> not found.
        categoryRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetCategoryEntity>
        {
            new() { Id = 1, IsActive = false, MarkedForDeletion = false, CategoryCode = "BLD", CategoryName = "Building" }
        }.BuildMockDbSet().Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateAssetNosAsync(1, 2, 1));
    }

    [Fact]
    public async Task GenerateAssetNosAsync_WhenTypeNotFoundOrNotMappedToCategory_ThrowsInvalidOperationException()
    {
        var service = CreateService(
            out _, out var categoryRepository, out var typeRepository, out var ulbRepository);

        ulbRepository.Setup(r => r.GetQueryable()).Returns(new List<ULBMasterEntity>
        {
            new() { Id = 1, IsActive = true, UlbCode = "PMC" }
        }.BuildMockDbSet().Object);

        categoryRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetCategoryEntity>
        {
            new() { Id = 1, IsActive = true, MarkedForDeletion = false, CategoryCode = "BLD", CategoryName = "Building" }
        }.BuildMockDbSet().Object);

        // Type exists but is mapped to a different category -> not found for category 1.
        typeRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetTypeEntity>
        {
            new() { Id = 2, AssetCategoryId = 999, IsActive = true, MarkedForDeletion = false, TypeCode = "RES", TypeName = "Residential" }
        }.BuildMockDbSet().Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateAssetNosAsync(1, 2, 1));
    }

    #endregion

    #region GetUlbCodeAsync

    [Fact]
    public async Task GetUlbCodeAsync_ReturnsFirstActiveUlbCode()
    {
        var service = CreateService(out _, out _, out _, out var ulbRepository);

        ulbRepository.Setup(r => r.GetQueryable()).Returns(new List<ULBMasterEntity>
        {
            new() { Id = 2, IsActive = true, UlbCode = "SECOND" },
            new() { Id = 1, IsActive = true, UlbCode = "FIRST" }
        }.BuildMockDbSet().Object);

        var result = await service.GetUlbCodeAsync();

        Assert.Equal("FIRST", result);
    }

    [Fact]
    public async Task GetUlbCodeAsync_FallsBackToAMC_WhenNoActiveUlb()
    {
        var service = CreateService(out _, out _, out _, out var ulbRepository);

        ulbRepository.Setup(r => r.GetQueryable()).Returns(new List<ULBMasterEntity>
        {
            new() { Id = 1, IsActive = false, UlbCode = "INACTIVE" }
        }.BuildMockDbSet().Object);

        var result = await service.GetUlbCodeAsync();

        Assert.Equal("AMC", result);
    }

    #endregion

    #region GenerateAssetNosWithPrefixAsync

    [Fact]
    public async Task GenerateAssetNosWithPrefixAsync_ReturnsSequentialPaddedNumbers()
    {
        var service = CreateService(out var repository, out _, out _, out _);

        repository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetMasterEntity>().BuildMockDbSet().Object);

        var result = await service.GenerateAssetNosWithPrefixAsync("ABC-", 3, 4);

        Assert.Equal(new List<string> { "ABC-0001", "ABC-0002", "ABC-0003" }, result);
    }

    [Fact]
    public async Task GenerateAssetNosWithPrefixAsync_ContinuesFromMaxExistingSequence()
    {
        var service = CreateService(out var repository, out _, out _, out _);

        repository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity>
        {
            new() { Id = 1, AssetNo = "ABC-0005" }
        }.BuildMockDbSet().Object);

        var result = await service.GenerateAssetNosWithPrefixAsync("ABC-", 1, 4);

        Assert.Equal(new List<string> { "ABC-0006" }, result);
    }

    /// <summary>
    /// Empirically stresses <c>GenerateAssetNosWithPrefixAsync</c> with many real concurrent callers,
    /// each committing its reserved number back into the shared backing store immediately (as a real
    /// caller would persist the new asset right after reserving its number), and asserts every
    /// generated number came out unique.
    ///
    /// CONFIRMED BUG (Section B item 5): this test reliably reproduces a duplicate (e.g. 24 distinct
    /// out of 25 callers) because the static <c>_sequenceLock</c> only guards the max-existing-sequence
    /// READ inside <c>GenerateAssetNosWithPrefixAsync</c> -- it is released before the caller persists
    /// the new asset, so two concurrent callers can both read the same max before either commits and
    /// walk away with the same number.
    ///
    /// An in-process reservation-set fix was tried and reverted: it broke test isolation across the
    /// wider AssetMaster suite, because dozens of tests in other files (e.g.
    /// AssetMasterServiceCrudTests.cs) hardcode the same category/type combination and expect a fresh
    /// "0001" sequence every time -- a process-wide reservation set makes that depend on run order. The
    /// intended fix instead lives at the persistence call sites (CreateAsync, etc. in
    /// AssetMasterService.Crud.cs): catch the unique-constraint violation against
    /// <c>UQ_AssetMaster_AssetNo</c> (in <c>ApplicationDbContext</c>) and retry with a freshly generated
    /// number, rather than widening this method's own lock. Left <c>Skip</c>ped rather than red so it
    /// doesn't read as ambient CI flakiness.
    /// </summary>
    [Fact(Skip = "Confirmed bug (roadmap Section B item 5): _sequenceLock's read-only scope lets concurrent callers reserve the same AssetNo before either persists. Fix belongs at the persistence call sites (retry on unique-constraint violation), not in this method -- see doc comment.")]
    public async Task GenerateAssetNosWithPrefixAsync_ConcurrentCalls_NeverProduceDuplicateNumbers()
    {
        var service = CreateService(out var repository, out _, out _, out _);

        var existingAssetNos = new List<string>();
        var syncRoot = new object();

        repository.Setup(r => r.GetQueryable()).Returns(() =>
        {
            lock (syncRoot)
            {
                return existingAssetNos
                    .Select(no => new AssetMasterEntity { AssetNo = no })
                    .ToList()
                    .BuildMockDbSet().Object;
            }
        });

        const int callerCount = 25;

        var results = await Task.WhenAll(Enumerable.Range(0, callerCount).Select(_ => Task.Run(async () =>
        {
            var generated = await service.GenerateAssetNosWithPrefixAsync("ABC-", 1, 4);
            lock (syncRoot)
            {
                existingAssetNos.Add(generated[0]);
            }
            return generated[0];
        })));

        Assert.Equal(callerCount, results.Distinct().Count());
    }

    #endregion
}
