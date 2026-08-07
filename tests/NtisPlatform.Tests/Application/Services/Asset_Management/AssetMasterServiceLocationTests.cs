using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

namespace NtisPlatform.Tests.Application.Services.Asset_Management;

/// <summary>
/// Covers <c>AssetMasterService.Location.cs</c> — the batched parent-fallback location-resolution
/// logic (<c>GetLocationInfoByAssetIdsAsync</c>, <c>ApplyLocation</c>, <c>EnrichLocationAsync</c>,
/// <c>PopulateFlatProperties</c>). All four methods are private, so they are exercised indirectly
/// through <see cref="AssetMasterService.GetByParentAssetIdAsync"/> (the full location-enrichment
/// path incl. parent-fallback batching, defined in AssetMasterService.SubAssets.cs) and
/// <see cref="AssetMasterService.GetByIdAsync"/> (which also calls <c>PopulateFlatProperties</c>).
///
/// <para>
/// <c>GetLocationInfoByAssetIdsAsync</c>'s main query is a chain of LEFT JOINs where each
/// subsequent join's key selector dereferences a property of the PREVIOUS (possibly-absent)
/// joined row, e.g. <c>join zone in ... on details.ZoneId equals zone.Id</c> where <c>details</c>
/// can be null. Real EF Core (backed by an actual relational provider) translates this whole
/// chain into one SQL statement where NULL propagates safely through cascading LEFT JOINs — it
/// never dereferences a null CLR object. <c>MockQueryable</c>, however, genuinely executes the
/// compiled query via LINQ-to-Objects (<c>Enumerable.GroupJoin</c>/<c>SelectMany</c>), so it
/// throws <see cref="NullReferenceException"/> the moment any queried asset lacks its own
/// AssetDetails row — exactly CLAUDE.md §17's warning that "mock-based tests might miss EF Core
/// query translation issues". Scenarios where every asset has its own AssetDetails row use the
/// plain <c>MockQueryable.Moq</c> convention (matches the rest of this test suite); scenarios that
/// require an asset to be MISSING its own AssetDetails row (the parent-fallback path itself, and
/// the "no data at all" edge case) use a real SQLite in-memory <see cref="DbContext"/> instead, so
/// the LEFT JOIN semantics are genuine and the test reflects real production behavior.
/// </para>
/// </summary>
public class AssetMasterServiceLocationTests
{
    #region Moq-based service factory (asset always has its own AssetDetails row)

    private static AssetMasterService CreateService(
        out Mock<IRepository<AssetMasterEntity, int>> repository,
        out Mock<IRepository<AssetDetailsEntity, int>> detailsRepository,
        out Mock<IRepository<ZoneEntity, int>> zoneRepository,
        out Mock<IRepository<WardEntity, int>> wardRepository,
        out Mock<IRepository<MoujaEntity, int>> moujaRepository,
        out Mock<IRepository<SubZoneDetailsForCVEntity, int>> subZoneRepository,
        out Mock<IRepository<AssetOrganizationMasterEntity, int>> organizationRepository,
        out Mock<IRepository<OwningDepartmentEntity, int>> departmentRepository,
        out Mock<IRepository<AssetConditionMasterEntity, int>> conditionRepository)
    {
        repository = new Mock<IRepository<AssetMasterEntity, int>>();
        detailsRepository = new Mock<IRepository<AssetDetailsEntity, int>>();
        zoneRepository = new Mock<IRepository<ZoneEntity, int>>();
        wardRepository = new Mock<IRepository<WardEntity, int>>();
        moujaRepository = new Mock<IRepository<MoujaEntity, int>>();
        subZoneRepository = new Mock<IRepository<SubZoneDetailsForCVEntity, int>>();
        organizationRepository = new Mock<IRepository<AssetOrganizationMasterEntity, int>>();
        departmentRepository = new Mock<IRepository<OwningDepartmentEntity, int>>();
        conditionRepository = new Mock<IRepository<AssetConditionMasterEntity, int>>();

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var mapperConfig = new MapperConfiguration(
            cfg => cfg.AddProfile<AssetMasterMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();

        var referenceValidator = new Mock<IReferenceValidationService>();
        var fieldValueRepository = new Mock<IRepository<AssetFieldValueEntity, int>>();
        var floorDetailsRepository = new Mock<IRepository<SubUnitsDetailsEntity, int>>();
        var roomWiseSubmissionRepository = new Mock<IRepository<AssetRoomWiseSubmissionDetailsEntity, int>>();
        var assetCategoryRepository = new Mock<IRepository<AssetCategoryEntity, int>>();
        var assetTypeRepository = new Mock<IRepository<AssetTypeEntity, int>>();
        var ulbRepository = new Mock<IRepository<ULBMasterEntity, int>>();
        var assetDocumentRepository = new Mock<IRepository<AssetDocumentEntity, int>>();
        var assetPhotoRepository = new Mock<IRepository<AssetPhotoEntity, int>>();
        var assetPhotoApplicationService = new Mock<IAssetPhotoApplicationService>();
        var documentApplicationService = new Mock<IDocumentApplicationService>();
        var deptMasterRepository = new Mock<IRepository<DepartmentMasterEntity, int>>();
        var moduleMasterRepository = new Mock<IRepository<ModuleMasterEntity, int>>();
        var designationRepository = new Mock<IRepository<AssetDesignationEntity, int>>();
        var amsTypeOfUseRepository = new Mock<IRepository<AssetTypeOfUseMasterEntity, int>>();
        var amsSubTypeOfUseRepository = new Mock<IRepository<AssetSubTypeOfUseEntity, int>>();
        var logger = new Mock<ILogger<AssetMasterService>>();
        var inventoryBatchRepository = new Mock<IRepository<InventoryBatchEntity, int>>();
        var inventoryAssetDetailRepository = new Mock<IRepository<InventoryAssetDetailEntity, int>>();
        var inventoryCategoryRepository = new Mock<IRepository<InventoryItemCategoryEntity, int>>();
        var inventoryNameRepository = new Mock<IRepository<InventoryItemNameEntity, int>>();
        var inventoryModelRepository = new Mock<IRepository<InventoryItemModelEntity, int>>();
        var inventoryDepartmentRepository = new Mock<IRepository<OwningDepartmentEntity, int>>();
        var inventoryDocumentApplicationService = new Mock<IInventoryDocumentApplicationService>();
        var leaseRentDetailsRepository = new Mock<IRepository<AssetLeaseRentDetailsEntity, int>>();

        // Every repository the batched location-join queries against must return SOME queryable
        // (even if empty) or the LINQ joins inside GetLocationInfoByAssetIdsAsync throw. Default
        // everything to empty first; individual tests override specific repos with real rows.
        SetupRows(repository);
        SetupRows(detailsRepository);
        SetupRows(zoneRepository);
        SetupRows(wardRepository);
        SetupRows(moujaRepository);
        SetupRows(subZoneRepository);
        SetupRows(organizationRepository);
        SetupRows(departmentRepository);
        SetupRows(conditionRepository);
        SetupRows(fieldValueRepository);
        SetupRows(floorDetailsRepository);
        SetupRows(roomWiseSubmissionRepository);
        SetupRows(assetCategoryRepository);
        SetupRows(assetTypeRepository);
        SetupRows(ulbRepository);
        SetupRows(assetDocumentRepository);
        SetupRows(assetPhotoRepository);
        SetupRows(deptMasterRepository);
        SetupRows(moduleMasterRepository);
        SetupRows(designationRepository);
        SetupRows(amsTypeOfUseRepository);
        SetupRows(amsSubTypeOfUseRepository);
        SetupRows(inventoryBatchRepository);
        SetupRows(inventoryAssetDetailRepository);
        SetupRows(inventoryCategoryRepository);
        SetupRows(inventoryNameRepository);
        SetupRows(inventoryModelRepository);
        SetupRows(inventoryDepartmentRepository);
        SetupRows(leaseRentDetailsRepository);

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

    #endregion

    #region SQLite-backed service factory (asset MISSING its own AssetDetails row)

    /// <summary>
    /// Minimal, self-contained EF model covering only the tables
    /// <c>GetLocationInfoByAssetIdsAsync</c> and <c>GetByParentAssetIdAsync</c>/<c>GetByIdAsync</c>'s
    /// own projections touch. Deliberately NOT the real <c>ApplicationDbContext</c> — that context's
    /// full ~100-entity model uses SQL-Server-only schema features that fail against SQLite
    /// (see the existing precedent/comment in UnitOfWorkNestedTransactionTests.cs), so this test
    /// gets its own tiny, portable model instead.
    /// </summary>
    private sealed class LocationTestDbContext(DbContextOptions<LocationTestDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AssetMasterEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.HasOne(x => x.AssetCategory).WithMany().HasForeignKey(x => x.AssetCategoryId);
                b.HasOne(x => x.AssetType).WithMany().HasForeignKey(x => x.AssetTypeId);
                b.HasOne(x => x.ParentAsset).WithMany().HasForeignKey(x => x.ParentAssetId);
                b.HasOne(x => x.Details).WithOne(d => d.Asset).HasForeignKey<AssetDetailsEntity>(d => d.AssetId);
                b.HasMany(x => x.FieldValues).WithOne(fv => fv.Asset).HasForeignKey(fv => fv.AssetId);
                b.Ignore(x => x.InventoryBatch);
                b.Ignore(x => x.SubUnitsDetails);
            });

            modelBuilder.Entity<AssetCategoryEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });
            modelBuilder.Entity<AssetTypeEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });
            modelBuilder.Entity<AssetFieldValueEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });
            modelBuilder.Entity<AssetDetailsEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });

            // GetByIdAsync's own unitFloorStats/floorRows queries JOIN _repository against
            // _floorDetailsRepository -- if the latter isn't also a real, translatable EF source,
            // EF Core refuses to translate the join at all (throws InvalidOperationException)
            // rather than silently falling back to client evaluation. Kept in-model (always empty
            // in these tests) purely so that join stays server-translatable.
            modelBuilder.Entity<SubUnitsDetailsEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId);
                b.Ignore(x => x.Floor);
                b.Ignore(x => x.SubFloor);
                b.Ignore(x => x.ConstructionType);
                b.Ignore(x => x.TypeOfUse);
                b.Ignore(x => x.SubTypeOfUse);
            });

            // Zone/Ward/Mouja/SubZone/Organization/Department are joined ad-hoc by scalar id in
            // Location.cs's manual LINQ `join` clauses, NOT via EF navigation/Include -- they need
            // no relationship configuration to each other, only their own key and a cut-off for any
            // collection navigation that would otherwise pull in unrelated entity types transitively.
            modelBuilder.Entity<ZoneEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.Ignore(x => x.Ward);
            });
            modelBuilder.Entity<WardEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.Ignore(x => x.Zone);
                b.Ignore(x => x.BlockMaster);
                b.Ignore(x => x.RateSectionDetails);
                b.Ignore(x => x.Property);
            });
            modelBuilder.Entity<MoujaEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.Ignore(x => x.Property);
                b.Ignore(x => x.CSNDetails);
                b.Ignore(x => x.SubZoneDetails);
            });
            modelBuilder.Entity<SubZoneDetailsForCVEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.Ignore(x => x.Mouja);
            });
            modelBuilder.Entity<AssetOrganizationMasterEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });
            modelBuilder.Entity<OwningDepartmentEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });

            modelBuilder.Ignore<DynamicTaxRuleEntity>();
            modelBuilder.Ignore<TaxConditionRuleEntity>();
        }
    }

    /// <summary>
    /// Thin <see cref="IRepository{T,TKey}"/> adapter over a shared <see cref="LocationTestDbContext"/>
    /// DbSet. Only <see cref="GetQueryable"/> is ever called by the code under test; everything else
    /// intentionally throws so a test would fail loudly if the production code path changes to use one
    /// of them. Tracks how many times <see cref="GetQueryable"/> was invoked, standing in for
    /// <c>Mock.Verify</c> (which isn't available on a real, non-mocked adapter).
    /// </summary>
    private sealed class EfQueryOnlyRepository<T>(LocationTestDbContext context) : IRepository<T, int> where T : class
    {
        public int GetQueryableCallCount { get; private set; }

        public IQueryable<T> GetQueryable()
        {
            GetQueryableCallCount++;
            return context.Set<T>();
        }

        public Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(T entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(T entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task HardDeleteAsync(T entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private static (LocationTestDbContext Context, SqliteConnection Connection) CreateSqliteContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<LocationTestDbContext>().UseSqlite(connection).Options;
        var context = new LocationTestDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }

    /// <summary>
    /// Builds an <see cref="AssetMasterService"/> whose repository/detailsRepository/zone/ward/
    /// mouja/subZone/organization/department repos are ALL backed by the SAME real EF Core (SQLite)
    /// context, so the location-join query gets genuine, single-provider SQL translation. Every
    /// other constructor dependency is an unused empty Moq, matching <see cref="CreateService"/>.
    /// </summary>
    private static AssetMasterService CreateSqliteBackedService(
        LocationTestDbContext context,
        out EfQueryOnlyRepository<AssetMasterEntity> repository,
        out EfQueryOnlyRepository<AssetDetailsEntity> detailsRepository,
        out EfQueryOnlyRepository<ZoneEntity> zoneRepository,
        out EfQueryOnlyRepository<WardEntity> wardRepository,
        out EfQueryOnlyRepository<MoujaEntity> moujaRepository,
        out EfQueryOnlyRepository<SubZoneDetailsForCVEntity> subZoneRepository,
        out EfQueryOnlyRepository<AssetOrganizationMasterEntity> organizationRepository,
        out EfQueryOnlyRepository<OwningDepartmentEntity> departmentRepository)
    {
        repository = new EfQueryOnlyRepository<AssetMasterEntity>(context);
        detailsRepository = new EfQueryOnlyRepository<AssetDetailsEntity>(context);
        zoneRepository = new EfQueryOnlyRepository<ZoneEntity>(context);
        wardRepository = new EfQueryOnlyRepository<WardEntity>(context);
        moujaRepository = new EfQueryOnlyRepository<MoujaEntity>(context);
        subZoneRepository = new EfQueryOnlyRepository<SubZoneDetailsForCVEntity>(context);
        organizationRepository = new EfQueryOnlyRepository<AssetOrganizationMasterEntity>(context);
        departmentRepository = new EfQueryOnlyRepository<OwningDepartmentEntity>(context);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var mapperConfig = new MapperConfiguration(
            cfg => cfg.AddProfile<AssetMasterMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();

        var referenceValidator = new Mock<IReferenceValidationService>();
        var fieldValueRepository = new Mock<IRepository<AssetFieldValueEntity, int>>();
        // Real EF-backed (not Moq): GetByIdAsync's unitFloorStats/floorRows queries JOIN this
        // against _repository, which EF Core refuses to translate unless BOTH sides are genuine
        // (same-provider) queryables. Always empty in these tests -- see model comment above.
        var floorDetailsRepository = new EfQueryOnlyRepository<SubUnitsDetailsEntity>(context);
        var roomWiseSubmissionRepository = new Mock<IRepository<AssetRoomWiseSubmissionDetailsEntity, int>>();
        var assetCategoryRepository = new Mock<IRepository<AssetCategoryEntity, int>>();
        var assetTypeRepository = new Mock<IRepository<AssetTypeEntity, int>>();
        var ulbRepository = new Mock<IRepository<ULBMasterEntity, int>>();
        var assetDocumentRepository = new Mock<IRepository<AssetDocumentEntity, int>>();
        var assetPhotoRepository = new Mock<IRepository<AssetPhotoEntity, int>>();
        var assetPhotoApplicationService = new Mock<IAssetPhotoApplicationService>();
        var documentApplicationService = new Mock<IDocumentApplicationService>();
        var deptMasterRepository = new Mock<IRepository<DepartmentMasterEntity, int>>();
        var moduleMasterRepository = new Mock<IRepository<ModuleMasterEntity, int>>();
        var designationRepository = new Mock<IRepository<AssetDesignationEntity, int>>();
        var amsTypeOfUseRepository = new Mock<IRepository<AssetTypeOfUseMasterEntity, int>>();
        var amsSubTypeOfUseRepository = new Mock<IRepository<AssetSubTypeOfUseEntity, int>>();
        var logger = new Mock<ILogger<AssetMasterService>>();
        var conditionRepository = new Mock<IRepository<AssetConditionMasterEntity, int>>();
        var inventoryBatchRepository = new Mock<IRepository<InventoryBatchEntity, int>>();
        var inventoryAssetDetailRepository = new Mock<IRepository<InventoryAssetDetailEntity, int>>();
        var inventoryCategoryRepository = new Mock<IRepository<InventoryItemCategoryEntity, int>>();
        var inventoryNameRepository = new Mock<IRepository<InventoryItemNameEntity, int>>();
        var inventoryModelRepository = new Mock<IRepository<InventoryItemModelEntity, int>>();
        var inventoryDepartmentRepository = new Mock<IRepository<OwningDepartmentEntity, int>>();
        var inventoryDocumentApplicationService = new Mock<IInventoryDocumentApplicationService>();
        var leaseRentDetailsRepository = new Mock<IRepository<AssetLeaseRentDetailsEntity, int>>();

        SetupRows(fieldValueRepository);
        SetupRows(roomWiseSubmissionRepository);
        SetupRows(assetCategoryRepository);
        SetupRows(assetTypeRepository);
        SetupRows(ulbRepository);
        SetupRows(assetDocumentRepository);
        SetupRows(assetPhotoRepository);
        SetupRows(deptMasterRepository);
        SetupRows(moduleMasterRepository);
        SetupRows(designationRepository);
        SetupRows(amsTypeOfUseRepository);
        SetupRows(amsSubTypeOfUseRepository);
        SetupRows(conditionRepository);
        SetupRows(inventoryBatchRepository);
        SetupRows(inventoryAssetDetailRepository);
        SetupRows(inventoryCategoryRepository);
        SetupRows(inventoryNameRepository);
        SetupRows(inventoryModelRepository);
        SetupRows(inventoryDepartmentRepository);
        SetupRows(leaseRentDetailsRepository);

        return new AssetMasterService(
            repository,
            unitOfWork.Object,
            mapper,
            referenceValidator.Object,
            fieldValueRepository.Object,
            floorDetailsRepository,
            roomWiseSubmissionRepository.Object,
            assetCategoryRepository.Object,
            assetTypeRepository.Object,
            ulbRepository.Object,
            detailsRepository,
            assetDocumentRepository.Object,
            assetPhotoRepository.Object,
            assetPhotoApplicationService.Object,
            documentApplicationService.Object,
            zoneRepository,
            wardRepository,
            moujaRepository,
            subZoneRepository,
            departmentRepository,
            organizationRepository,
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

    /// <summary>Seeds a valid AssetCategory/AssetType pair (id=1) every SQLite-backed asset can point at.</summary>
    private static void SeedCategoryAndType(LocationTestDbContext context)
    {
        context.Set<AssetCategoryEntity>().Add(new AssetCategoryEntity { Id = 1, CategoryCode = "CAT1", CategoryName = "Test Category" });
        context.Set<AssetTypeEntity>().Add(new AssetTypeEntity { Id = 1, TypeCode = "TYP1", TypeName = "Test Type", CodeFormat = "N" });
    }

    #endregion

    #region GetLocationInfoByAssetIdsAsync

    [Fact]
    public async Task GetLocationInfoByAssetIdsAsync_ResolvesZoneWardMoujaSubZoneOrgDepartment_InOneBatchedQuery()
    {
        var service = CreateService(
            out var repository, out var detailsRepository, out var zoneRepository, out var wardRepository,
            out var moujaRepository, out var subZoneRepository, out var organizationRepository,
            out var departmentRepository, out var conditionRepository);

        var child = new AssetMasterEntity
        {
            Id = 2,
            ParentAssetId = 1,
            AssetName = "Shop 1",
            IsActive = true,
            DepartmentId = 60,
            FieldValues = new List<AssetFieldValueEntity>()
        };
        SetupRows(repository, child);

        SetupRows(detailsRepository, new AssetDetailsEntity
        {
            AssetId = 2,
            ZoneId = 10,
            WardId = 20,
            MoujaId = 30,
            SubZoneId = 40,
            OrganizationId = 50,
            Address = "123 Market Road"
        });

        SetupRows(zoneRepository, new ZoneEntity { Id = 10, ZoneNo = "Z1", Description = "Zone One" });
        SetupRows(wardRepository, new WardEntity { Id = 20, WardNo = "W1", Description = "Ward One", ZoneId = 10 });
        SetupRows(moujaRepository, new MoujaEntity { Id = 30, MoujaNo = "M1", MoujaName = "Mouja One" });
        SetupRows(subZoneRepository, new SubZoneDetailsForCVEntity { Id = 40, MoujaId = 30, SubZoneNo = "SZ1", SubZoneName = "SubZone One" });
        SetupRows(organizationRepository, new AssetOrganizationMasterEntity { Id = 50, OrganizationCode = "OC1", OrganizationName = "Org One" });
        SetupRows(departmentRepository, new OwningDepartmentEntity { Id = 60, OwningDepartmentName = "Public Works" });

        var result = await service.GetByParentAssetIdAsync(1, 0, CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(10, dto.Details.ZoneId);
        Assert.Equal(20, dto.Details.WardId);
        Assert.Equal(30, dto.Details.MoujaId);
        Assert.Equal(40, dto.Details.SubZoneId);
        Assert.Equal(50, dto.Details.OrganizationId);
        Assert.Equal("123 Market Road", dto.Details.Address);
        Assert.Equal("Zone One", dto.Names.ZoneName);
        Assert.Equal("Z1", dto.Names.ZoneNo);
        Assert.Equal("Ward One", dto.Names.WardName);
        Assert.Equal("W1", dto.Names.WardNo);
        Assert.Equal("Mouja One", dto.Names.MoujaName);
        Assert.Equal("SZ1", dto.Names.SubZoneNo);
        Assert.Equal("Org One", dto.Names.OrganizationName);
        Assert.Equal("Public Works", dto.Names.DepartmentName);
        Assert.Equal(60, dto.DepartmentId);
    }

    [Fact]
    public async Task GetLocationInfoByAssetIdsAsync_ChildAssetWithoutOwnDetails_InheritsParentLocation()
    {
        var (context, connection) = CreateSqliteContext();
        using var _connection = connection;
        await using var _context = context;

        SeedCategoryAndType(context);
        // Stub parent row -- GetByParentAssetIdAsync's own child query doesn't need it to exist,
        // but AssetDetailsEntity.AssetId is configured as a real FK, so it must satisfy that.
        context.Set<AssetMasterEntity>().Add(new AssetMasterEntity { Id = 1, AssetName = "Parent", AssetCategoryId = 1, AssetTypeId = 1, IsActive = true });
        context.Set<AssetMasterEntity>().Add(new AssetMasterEntity { Id = 2, ParentAssetId = 1, AssetName = "Room 101", AssetCategoryId = 1, AssetTypeId = 1, IsActive = true });

        // Only the PARENT (AssetId = 1) has an AssetDetails row -- the child has none of its own.
        context.Set<AssetDetailsEntity>().Add(new AssetDetailsEntity
        {
            Id = 1,
            AssetId = 1,
            ZoneId = 10,
            WardId = 20,
            MoujaId = 30,
            SubZoneId = 40,
            OrganizationId = 50,
            Address = "Parent Building Address"
        });
        context.Set<ZoneEntity>().Add(new ZoneEntity { Id = 10, ZoneNo = "Z1", Description = "Zone Parent" });
        context.Set<WardEntity>().Add(new WardEntity { Id = 20, WardNo = "W1", Description = "Ward Parent", ZoneId = 10 });
        context.Set<MoujaEntity>().Add(new MoujaEntity { Id = 30, MoujaNo = "M1", MoujaName = "Mouja Parent" });
        context.Set<SubZoneDetailsForCVEntity>().Add(new SubZoneDetailsForCVEntity { Id = 40, MoujaId = 30, SubZoneNo = "SZ1", SubZoneName = "SubZone Parent" });
        context.Set<AssetOrganizationMasterEntity>().Add(new AssetOrganizationMasterEntity { Id = 50, OrganizationCode = "OC1", OrganizationName = "Org Parent" });
        context.SaveChanges();

        var service = CreateSqliteBackedService(context,
            out _, out _, out _, out _, out _, out _, out _, out _);

        var result = await service.GetByParentAssetIdAsync(1, 0, CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(10, dto.Details.ZoneId);
        Assert.Equal(20, dto.Details.WardId);
        Assert.Equal(30, dto.Details.MoujaId);
        Assert.Equal(40, dto.Details.SubZoneId);
        Assert.Equal(50, dto.Details.OrganizationId);
        Assert.Equal("Zone Parent", dto.Names.ZoneName);
        Assert.Equal("Ward Parent", dto.Names.WardName);
        Assert.Equal("Mouja Parent", dto.Names.MoujaName);
        Assert.Equal("SZ1", dto.Names.SubZoneNo);
        Assert.Equal("Org Parent", dto.Names.OrganizationName);
        Assert.Equal("Parent Building Address", dto.Details.Address);
    }

    [Fact]
    public async Task GetLocationInfoByAssetIdsAsync_ChildAssetWithOwnDetails_DoesNotFallBackToParent()
    {
        var service = CreateService(
            out var repository, out var detailsRepository, out var zoneRepository, out var wardRepository,
            out var moujaRepository, out var subZoneRepository, out var organizationRepository,
            out var departmentRepository, out var conditionRepository);

        var child = new AssetMasterEntity
        {
            Id = 2,
            ParentAssetId = 1,
            AssetName = "Shop 5",
            IsActive = true,
            FieldValues = new List<AssetFieldValueEntity>()
        };
        SetupRows(repository, child);

        // Both the parent (Id=1) and the child (Id=2) have their OWN AssetDetails rows, pointing
        // at DIFFERENT zones -- proves the fallback only kicks in when the child's row is missing.
        SetupRows(detailsRepository,
            new AssetDetailsEntity { AssetId = 1, ZoneId = 10, WardId = 20, MoujaId = 30, SubZoneId = 40, OrganizationId = 50 },
            new AssetDetailsEntity { AssetId = 2, ZoneId = 99, WardId = 98, MoujaId = 97, SubZoneId = 96, OrganizationId = 95 });

        SetupRows(zoneRepository,
            new ZoneEntity { Id = 10, ZoneNo = "Z1", Description = "Zone Parent" },
            new ZoneEntity { Id = 99, ZoneNo = "Z9", Description = "Zone Child" });
        SetupRows(wardRepository,
            new WardEntity { Id = 20, WardNo = "W1", Description = "Ward Parent", ZoneId = 10 },
            new WardEntity { Id = 98, WardNo = "W9", Description = "Ward Child", ZoneId = 99 });
        SetupRows(moujaRepository,
            new MoujaEntity { Id = 30, MoujaNo = "M1", MoujaName = "Mouja Parent" },
            new MoujaEntity { Id = 97, MoujaNo = "M9", MoujaName = "Mouja Child" });
        SetupRows(subZoneRepository,
            new SubZoneDetailsForCVEntity { Id = 40, MoujaId = 30, SubZoneNo = "SZ1", SubZoneName = "SubZone Parent" },
            new SubZoneDetailsForCVEntity { Id = 96, MoujaId = 97, SubZoneNo = "SZ9", SubZoneName = "SubZone Child" });
        SetupRows(organizationRepository,
            new AssetOrganizationMasterEntity { Id = 50, OrganizationCode = "OC1", OrganizationName = "Org Parent" },
            new AssetOrganizationMasterEntity { Id = 95, OrganizationCode = "OC9", OrganizationName = "Org Child" });

        var result = await service.GetByParentAssetIdAsync(1, 0, CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(99, dto.Details.ZoneId);
        Assert.Equal(98, dto.Details.WardId);
        Assert.Equal(95, dto.Details.OrganizationId);
        Assert.Equal("Zone Child", dto.Names.ZoneName);
        Assert.Equal("Ward Child", dto.Names.WardName);
        Assert.Equal("Mouja Child", dto.Names.MoujaName);
        Assert.Equal("Org Child", dto.Names.OrganizationName);
        Assert.NotEqual("Zone Parent", dto.Names.ZoneName);
    }

    [Fact]
    public async Task GetLocationInfoByAssetIdsAsync_AssetWithNoDetailsAndNoParentFallback_AllLocationFieldsNullNoException()
    {
        var (context, connection) = CreateSqliteContext();
        using var _connection = connection;
        await using var _context = context;

        SeedCategoryAndType(context);
        context.Set<AssetMasterEntity>().Add(new AssetMasterEntity { Id = 1, AssetName = "Parent", AssetCategoryId = 1, AssetTypeId = 1, IsActive = true });
        context.Set<AssetMasterEntity>().Add(new AssetMasterEntity { Id = 2, ParentAssetId = 1, AssetName = "Orphan Unit", AssetCategoryId = 1, AssetTypeId = 1, IsActive = true });
        // No AssetDetails rows at all -- neither the child's own, nor the parent's to fall back to.
        context.SaveChanges();

        var service = CreateSqliteBackedService(context,
            out _, out _, out _, out _, out _, out _, out _, out _);

        var result = await service.GetByParentAssetIdAsync(1, 0, CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Null(dto.Details.ZoneId);
        Assert.Null(dto.Details.WardId);
        Assert.Null(dto.Details.MoujaId);
        Assert.Null(dto.Details.SubZoneId);
        Assert.Equal(0, dto.Details.OrganizationId);
        Assert.Null(dto.Names.ZoneName);
        Assert.Null(dto.Names.WardName);
        Assert.Null(dto.Names.MoujaName);
        Assert.Null(dto.Names.OrganizationName);
        Assert.Null(dto.Names.DepartmentName);
    }

    #endregion

    #region EnrichLocationAsync

    [Fact]
    public async Task EnrichLocationAsync_BatchEnrichesEntireDtoList_SingleRoundTripRegardlessOfAssetCount()
    {
        var (context, connection) = CreateSqliteContext();
        using var _connection = connection;
        await using var _context = context;

        SeedCategoryAndType(context);
        context.Set<AssetMasterEntity>().Add(new AssetMasterEntity { Id = 1, AssetName = "Parent", AssetCategoryId = 1, AssetTypeId = 1, IsActive = true });
        context.Set<AssetMasterEntity>().Add(new AssetMasterEntity { Id = 2, ParentAssetId = 1, AssetName = "A", AssetCategoryId = 1, AssetTypeId = 1, IsActive = true });
        context.Set<AssetMasterEntity>().Add(new AssetMasterEntity { Id = 3, ParentAssetId = 1, AssetName = "B", AssetCategoryId = 1, AssetTypeId = 1, IsActive = true });
        context.Set<AssetMasterEntity>().Add(new AssetMasterEntity { Id = 4, ParentAssetId = 1, AssetName = "C", AssetCategoryId = 1, AssetTypeId = 1, IsActive = true });

        // A (Id=2) and B (Id=3) have NO own AssetDetails row (both need the parent fallback);
        // C (Id=4) has its own.
        context.Set<AssetDetailsEntity>().Add(new AssetDetailsEntity { Id = 1, AssetId = 1, ZoneId = 10, WardId = 20, MoujaId = 30, SubZoneId = 40, OrganizationId = 50 });
        context.Set<AssetDetailsEntity>().Add(new AssetDetailsEntity { Id = 4, AssetId = 4, ZoneId = 99, WardId = 98, MoujaId = 97, SubZoneId = 96, OrganizationId = 95 });

        context.Set<ZoneEntity>().Add(new ZoneEntity { Id = 10, ZoneNo = "Z1", Description = "Zone Parent" });
        context.Set<ZoneEntity>().Add(new ZoneEntity { Id = 99, ZoneNo = "Z9", Description = "Zone C" });
        context.Set<WardEntity>().Add(new WardEntity { Id = 20, WardNo = "W1", Description = "Ward Parent", ZoneId = 10 });
        context.Set<WardEntity>().Add(new WardEntity { Id = 98, WardNo = "W9", Description = "Ward C", ZoneId = 99 });
        context.Set<MoujaEntity>().Add(new MoujaEntity { Id = 30, MoujaNo = "M1", MoujaName = "Mouja Parent" });
        context.Set<MoujaEntity>().Add(new MoujaEntity { Id = 97, MoujaNo = "M9", MoujaName = "Mouja C" });
        context.Set<SubZoneDetailsForCVEntity>().Add(new SubZoneDetailsForCVEntity { Id = 40, MoujaId = 30, SubZoneNo = "SZ1", SubZoneName = "SubZone Parent" });
        context.Set<SubZoneDetailsForCVEntity>().Add(new SubZoneDetailsForCVEntity { Id = 96, MoujaId = 97, SubZoneNo = "SZ9", SubZoneName = "SubZone C" });
        context.Set<AssetOrganizationMasterEntity>().Add(new AssetOrganizationMasterEntity { Id = 50, OrganizationCode = "OC1", OrganizationName = "Org Parent" });
        context.Set<AssetOrganizationMasterEntity>().Add(new AssetOrganizationMasterEntity { Id = 95, OrganizationCode = "OC9", OrganizationName = "Org C" });
        context.SaveChanges();

        var service = CreateSqliteBackedService(context,
            out _, out _, out var zoneRepository, out _, out _, out _, out _, out _);

        var result = await service.GetByParentAssetIdAsync(1, 0, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal("Zone Parent", result.Single(a => a.Id == 2).Names.ZoneName);
        Assert.Equal("Zone Parent", result.Single(a => a.Id == 3).Names.ZoneName);
        Assert.Equal("Zone C", result.Single(a => a.Id == 4).Names.ZoneName);

        // The whole point of the batching in GetLocationInfoByAssetIdsAsync: ONE query resolves
        // the main per-asset join for all 3 assets, and (at most) ONE more resolves the
        // parent-fallback join -- shared by BOTH assets that needed it. Never once per asset
        // (which is what the pre-batching code did: up to 6 sequential queries per child).
        Assert.Equal(2, zoneRepository.GetQueryableCallCount);
    }

    [Fact]
    public async Task EnrichLocationAsync_WithNoChildAssets_ReturnsEmptyList_WithoutQueryingLocationRepositories()
    {
        var service = CreateService(
            out var repository, out var detailsRepository, out var zoneRepository, out var wardRepository,
            out var moujaRepository, out var subZoneRepository, out var organizationRepository,
            out var departmentRepository, out var conditionRepository);

        // No assets at all under parent 1.
        SetupRows(repository);

        var result = await service.GetByParentAssetIdAsync(1, 0, CancellationToken.None);

        Assert.Empty(result);
        // EnrichLocationAsync's `if (dtos.Count == 0) return;` guard should short-circuit before
        // GetLocationInfoByAssetIdsAsync ever touches the location-resolution repositories.
        zoneRepository.Verify(r => r.GetQueryable(), Times.Never);
        detailsRepository.Verify(r => r.GetQueryable(), Times.Never);
    }

    #endregion

    #region PopulateFlatProperties

    [Fact]
    public async Task PopulateFlatProperties_MirrorsNamesAndDetailsOntoFlatDtoFields()
    {
        var service = CreateService(
            out var repository, out var detailsRepository, out var zoneRepository, out var wardRepository,
            out var moujaRepository, out var subZoneRepository, out var organizationRepository,
            out var departmentRepository, out var conditionRepository);

        var asset = new AssetMasterEntity
        {
            Id = 1,
            AssetName = "Main Building",
            IsActive = true,
            DepartmentId = 60,
            AssetConditionId = 7,
            FieldValues = new List<AssetFieldValueEntity>(),
            AssetCategory = new AssetCategoryEntity { CategoryName = "Building" },
            AssetType = new AssetTypeEntity { TypeName = "Office" }
        };
        SetupRows(repository, asset);

        SetupRows(detailsRepository, new AssetDetailsEntity
        {
            AssetId = 1,
            ZoneId = 10,
            WardId = 20,
            MoujaId = 30,
            SubZoneId = 40,
            OrganizationId = 50,
            Address = "1 Civic Plaza"
        });

        SetupRows(zoneRepository, new ZoneEntity { Id = 10, ZoneNo = "Z1", Description = "Zone One" });
        SetupRows(wardRepository, new WardEntity { Id = 20, WardNo = "W1", Description = "Ward One", ZoneId = 10 });
        SetupRows(moujaRepository, new MoujaEntity { Id = 30, MoujaNo = "M1", MoujaName = "Mouja One" });
        SetupRows(subZoneRepository, new SubZoneDetailsForCVEntity { Id = 40, MoujaId = 30, SubZoneNo = "SZ1", SubZoneName = "SubZone One" });
        SetupRows(organizationRepository, new AssetOrganizationMasterEntity { Id = 50, OrganizationCode = "OC1", OrganizationName = "Org One" });
        SetupRows(departmentRepository, new OwningDepartmentEntity { Id = 60, OwningDepartmentName = "Public Works" });
        SetupRows(conditionRepository, new AssetConditionMasterEntity { Id = 7, ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good" });

        var dto = await service.GetByIdAsync(1, CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("Building", dto!.AssetCategoryName);
        Assert.Equal("Ward One", dto.WardName);
        Assert.Equal("W1", dto.WardNo);
        Assert.Equal("Zone One", dto.ZoneName);
        Assert.Equal("Z1", dto.ZoneNo);
        Assert.Equal("Mouja One", dto.MoujaName);
        Assert.Equal("SZ1", dto.SubZoneNo);
        Assert.Equal("Good", dto.AssetCondition);
        Assert.Equal("1 Civic Plaza", dto.Address);
        Assert.Equal("Public Works", dto.DepartmentName);
    }

    [Fact]
    public async Task PopulateFlatProperties_WhenNoLocationOrConditionResolved_LeavesFlatFieldsNull()
    {
        var (context, connection) = CreateSqliteContext();
        using var _connection = connection;
        await using var _context = context;

        SeedCategoryAndType(context);
        context.Set<AssetMasterEntity>().Add(new AssetMasterEntity { Id = 1, AssetName = "Bare Asset", AssetCategoryId = 1, AssetTypeId = 1, IsActive = true });
        // No AssetDetails row, no AssetConditionId -- nothing else seeded.
        context.SaveChanges();

        var service = CreateSqliteBackedService(context,
            out _, out _, out _, out _, out _, out _, out _, out _);

        var dto = await service.GetByIdAsync(1, CancellationToken.None);

        Assert.NotNull(dto);
        // AssetCategoryName still resolves (id=1 category/type were seeded so the JOIN in the
        // outer GetByIdAsync projection matches) -- only the LOCATION/condition fields, which have
        // nothing to resolve against, should be null.
        Assert.Equal("Test Category", dto!.AssetCategoryName);
        Assert.Null(dto.WardName);
        Assert.Null(dto.ZoneName);
        Assert.Null(dto.MoujaName);
        Assert.Null(dto.SubZoneNo);
        Assert.Null(dto.AssetCondition);
        Assert.Null(dto.Address);
    }

    #endregion
}
