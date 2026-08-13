using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Integration;

/// <summary>
/// Integration tests for InventoryItemCategory against a real <see cref="ApplicationDbContext"/>
/// (EF Core InMemory provider) instead of a mocked <c>IRepository</c>, per CLAUDE.md Section 17
/// ("Add at least one integration test against a real DB (in-memory or testcontainer) per aggregate").
///
/// What this catches that mock-based tests (<c>InventoryItemCategoryServiceTests</c>) can't: the
/// entity actually being mapped with all required columns (e.g. the seeded AssetCategoryEntity below
/// throws DbUpdateException if CreatedDate is omitted, since InMemory does enforce non-nullable
/// properties), and that the GetAllAsync join query (GroupJoin/DefaultIfEmpty) actually executes
/// against a real IQueryable/DbContext pipeline instead of MockQueryable's LINQ-to-Objects provider.
/// The Model_* tests below inspect <c>_context.Model</c> directly (provider-agnostic metadata), so
/// they verify OnModelCreating's declared index/FK configuration regardless of provider.
///
/// What this does NOT cover: EF Core's InMemory provider does not enforce relational constraints
/// (unique indexes, FK referential integrity) at the storage level and does not translate LINQ to
/// SQL -- so it cannot catch a real UNIQUE-constraint violation or a query that fails to translate
/// on a relational provider. Where that matters, prefer SQLite in-memory (see
/// DataEntryIntegrationTests) or a testcontainer against the real target RDBMS.
/// </summary>
[Trait("Category", "Integration")]
public class InventoryItemCategoryIntegrationTests : IAsyncLifetime
{
    private ApplicationDbContext? _context;
    private Repository<InventoryItemCategoryEntity, int>? _repository;
    private Repository<AssetCategoryEntity, int>? _assetCategoryRepository;
    private UnitOfWork? _unitOfWork;
    private IMapper? _mapper;
    private InventoryItemCategoryService? _service;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"InventoryItemCategoryIntegrationTests_{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new Repository<InventoryItemCategoryEntity, int>(_context);
        _assetCategoryRepository = new Repository<AssetCategoryEntity, int>(_context);
        _unitOfWork = new UnitOfWork(_context);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<InventoryItemCategoryMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        var referenceValidator = new Mock<IReferenceValidationService>();
        referenceValidator
            .Setup(x => x.ValidateReferencesAsync<InventoryItemCategoryEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());

        _service = new InventoryItemCategoryService(_repository, _unitOfWork, _mapper, referenceValidator.Object, _assetCategoryRepository);

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

    [Fact]
    public async Task CreateAsync_UniqueTypeCodeAndTypeName_PersistsAndIsRetrievable()
    {
        var dto = new CreateInventoryItemCategoryDto
        {
            AssetCategoryId = 1,
            TypeCode = "ELEC",
            TypeName = "Electronics",
            DisplayOrder = 1,
            DepreciationRate = 0.15m
        };

        var created = await _service!.CreateAsync(dto, CancellationToken.None);

        Assert.True(created.Id > 0);
        var fetched = await _service.GetByIdAsync(created.Id, CancellationToken.None);
        Assert.NotNull(fetched);
        Assert.Equal("ELEC", fetched!.TypeCode);
        Assert.Equal("Electronics", fetched.TypeName);
    }

    [Fact]
    public async Task CreateAsync_DuplicateTypeCodeOnActiveRow_ThrowsValidationExceptionBeforeTouchingDb()
    {
        await _service!.CreateAsync(
            new CreateInventoryItemCategoryDto { AssetCategoryId = 1, TypeCode = "DUPCODE", TypeName = "First", DisplayOrder = 1 },
            CancellationToken.None);

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(
                new CreateInventoryItemCategoryDto { AssetCategoryId = 1, TypeCode = "DUPCODE", TypeName = "Second", DisplayOrder = 2 },
                CancellationToken.None));

        Assert.Contains("InventoryItemCategory_TypeCode_Duplicate", ex.Errors.Values);
    }

    /// <summary>
    /// The exact scenario the live DB's plain (unfiltered) UQ_InventoryItemCategoryMaster_TypeCode
    /// constraint forces: a row that's only marked-for-deletion (not yet purged by the nightly
    /// HardDeleteCleanupService) still occupies its TypeCode. Recreating with that code must fail
    /// cleanly at the application layer, not with a raw DB unique-constraint error.
    /// </summary>
    [Fact]
    public async Task CreateAsync_TypeCodeHeldByMarkedForDeletionRow_ThrowsValidationException()
    {
        var original = await _service!.CreateAsync(
            new CreateInventoryItemCategoryDto { AssetCategoryId = 1, TypeCode = "OLDCODE", TypeName = "Old Category", DisplayOrder = 1 },
            CancellationToken.None);

        // Simulate the soft-delete / mark-for-deletion step directly (not yet purged).
        var trackedEntity = await _context!.Set<InventoryItemCategoryEntity>().FindAsync(original.Id);
        trackedEntity!.IsActive = false;
        trackedEntity.MarkedForDeletion = true;
        trackedEntity.MarkedForDeletionDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(
                new CreateInventoryItemCategoryDto { AssetCategoryId = 1, TypeCode = "OLDCODE", TypeName = "New Category", DisplayOrder = 2 },
                CancellationToken.None));

        Assert.Contains("InventoryItemCategory_TypeCode_Duplicate", ex.Errors.Values);
    }

    /// <summary>
    /// Verifies the EF model itself declares TypeCode as unique (matching the live DB's
    /// UQ_InventoryItemCategoryMaster_TypeCode) -- independent of the app-level pre-check above,
    /// this confirms ApplicationDbContext.OnModelCreating wasn't left silently out of sync with the
    /// deployed schema. Bypasses the service/repository so the app-level duplicate check can't mask
    /// a missing/incorrect index.
    /// </summary>
    [Fact]
    public async Task Model_TypeCodeIndex_IsConfiguredAsUnique()
    {
        var entityType = _context!.Model.FindEntityType(typeof(InventoryItemCategoryEntity))!;
        var index = entityType.GetIndexes().Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(InventoryItemCategoryEntity.TypeCode));

        Assert.True(index.IsUnique);
    }

    /// <summary>
    /// AssetCategoryId is int NOT NULL on the live DB with FK_InventoryItemCategoryMaster_AssetCategory
    /// pointing at AMS.AssetCategoryMaster.Id -- this was the actual root cause of the originally
    /// reported "An error occurred while creating the record" 500: the entity/DTOs never modeled this
    /// column at all, so every insert failed a raw NOT NULL constraint. Verifies both that the value
    /// round-trips through Create/GetById and that the model declares the FK correctly.
    /// </summary>
    [Fact]
    public async Task CreateAsync_PersistsAssetCategoryId()
    {
        var dto = new CreateInventoryItemCategoryDto
        {
            AssetCategoryId = 7,
            TypeCode = "TOOLS2",
            TypeName = "Hand Tools",
            DisplayOrder = 1
        };

        var created = await _service!.CreateAsync(dto, CancellationToken.None);
        Assert.Equal(7, created.AssetCategoryId);

        var fetched = await _service.GetByIdAsync(created.Id, CancellationToken.None);
        Assert.Equal(7, fetched!.AssetCategoryId);
    }

    /// <summary>
    /// GetAllAsync joins against AssetCategoryMaster (AssetCategoryEntity) in SQL to resolve
    /// AssetCategoryName -- exercised here against the real EF model/provider rather than a
    /// mocked repository, per CLAUDE.md Section 17 ("at least one integration test... per
    /// aggregate" and Section 7's preference for SQL joins over in-memory lookups).
    /// </summary>
    [Fact]
    public async Task GetAllAsync_PopulatesAssetCategoryName_FromReferencedAssetCategory()
    {
        _context!.Set<AssetCategoryEntity>().Add(new AssetCategoryEntity
        {
            Id = 5,
            CategoryCode = "ELEC",
            CategoryName = "Electronics Category",
            CreatedDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _service!.CreateAsync(
            new CreateInventoryItemCategoryDto { AssetCategoryId = 5, TypeCode = "ELEC1", TypeName = "Electronics Item", DisplayOrder = 1 },
            CancellationToken.None);

        var result = await _service.GetAllAsync(new InventoryItemCategoryQueryParameters { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(5, item.AssetCategoryId);
        Assert.Equal("Electronics Category", item.AssetCategoryName);
    }

    /// <summary>
    /// When AssetCategoryId doesn't resolve to any row (orphaned FK), the left join must not
    /// drop the InventoryItemCategory row -- AssetCategoryName is simply left null.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_UnresolvableAssetCategoryId_ReturnsNullAssetCategoryName()
    {
        await _service!.CreateAsync(
            new CreateInventoryItemCategoryDto { AssetCategoryId = 12345, TypeCode = "ORPH1", TypeName = "Orphaned Item", DisplayOrder = 1 },
            CancellationToken.None);

        var result = await _service.GetAllAsync(new InventoryItemCategoryQueryParameters { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(12345, item.AssetCategoryId);
        Assert.Null(item.AssetCategoryName);
    }

    [Fact]
    public void Model_AssetCategoryIdForeignKey_PointsAtAssetCategoryMaster()
    {
        var entityType = _context!.Model.FindEntityType(typeof(InventoryItemCategoryEntity))!;
        var fk = entityType.GetForeignKeys().Single(f => f.Properties.Count == 1 && f.Properties[0].Name == nameof(InventoryItemCategoryEntity.AssetCategoryId));

        Assert.Equal(typeof(NtisPlatform.Core.Entities.Master.AssetCategoryEntity), fk.PrincipalEntityType.ClrType);
        Assert.False(fk.Properties[0].IsNullable);
    }
}
