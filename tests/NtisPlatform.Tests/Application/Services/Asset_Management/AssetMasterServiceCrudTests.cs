using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management.AssetFieldValue;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.Asset_Management;

/// <summary>
/// Covers <c>AssetMasterService.Crud.cs</c>: <c>CreateAsync</c>, <c>UpdateAsync</c>,
/// <c>DeleteAsync</c>, <c>GetByIdAsync</c>/<c>GetByIdForUserAsync</c>,
/// <c>GetAllAsync</c> (via <c>GetAllInternalAsync</c>), plus the
/// <c>ValidateForDeactivationAsync</c>/<c>ValidateForDeleteAsync</c> hooks reached through
/// <c>UpdateAsync</c>/<c>DeleteAsync</c>. (<c>GetAllForUserAsync</c>/<c>ExportToExcelForUserAsync</c>
/// were removed 2026-08-03 -- they never actually scoped by <c>currentUserId</c>; see the roadmap.)
///
/// IMPORTANT documented gap (see also AssetMaster-TestCoverage-Roadmap.md Section B item 1, and the
/// dedicated test below): <see cref="AssetMasterMappingProfile"/> Ignore()s <c>IsActive</c> on the
/// <c>UpdateAssetMasterDto -&gt; AssetMasterEntity</c> map, so <c>UpdateAsync</c>'s
/// <c>_mapper.Map(updateDto, entity)</c> call never changes <c>entity.IsActive</c>. That means
/// <c>ValidateForDeactivationAsync</c>'s guard (<c>currentEntity.IsActive &amp;&amp; !updatedEntity.IsActive</c>)
/// can never observe a true-&gt;false transition through this code path. See
/// <see cref="UpdateAsync_SettingIsActiveFalseOnDto_DoesNotChangeEntityIsActive_BecauseMappingIgnoresIt"/>.
/// </summary>
public class AssetMasterServiceCrudTests
{
    #region Mock wiring

    /// <summary>
    /// Holds one Mock&lt;T&gt; per <see cref="AssetMasterService"/> constructor dependency (~36 total).
    /// Kept as a plain container (rather than 36 "out" parameters) so <see cref="CreateService"/> and
    /// <see cref="CreateServiceWithCreateValidation"/> can both build either concrete service type from
    /// the exact same set of mocks without duplicating the whole dependency list twice.
    /// </summary>
    private sealed class ServiceMocks
    {
        public readonly Mock<IRepository<AssetMasterEntity, int>> Repository = new();
        public readonly Mock<IUnitOfWork> UnitOfWork = new();
        public readonly Mock<IReferenceValidationService> ReferenceValidator = new();
        public readonly Mock<IRepository<AssetFieldValueEntity, int>> FieldValueRepository = new();
        public readonly Mock<IRepository<SubUnitsDetailsEntity, int>> FloorDetailsRepository = new();
        public readonly Mock<IRepository<AssetRoomWiseSubmissionDetailsEntity, int>> RoomWiseSubmissionRepository = new();
        public readonly Mock<IRepository<AssetCategoryEntity, int>> AssetCategoryRepository = new();
        public readonly Mock<IRepository<AssetTypeEntity, int>> AssetTypeRepository = new();
        public readonly Mock<IRepository<ULBMasterEntity, int>> UlbRepository = new();
        public readonly Mock<IRepository<AssetDetailsEntity, int>> DetailsRepository = new();
        public readonly Mock<IRepository<AssetDocumentEntity, int>> AssetDocumentRepository = new();
        public readonly Mock<IRepository<AssetPhotoEntity, int>> AssetPhotoRepository = new();
        public readonly Mock<IAssetPhotoApplicationService> AssetPhotoApplicationService = new();
        public readonly Mock<IDocumentApplicationService> DocumentApplicationService = new();
        public readonly Mock<IRepository<ZoneEntity, int>> ZoneRepository = new();
        public readonly Mock<IRepository<WardEntity, int>> WardRepository = new();
        public readonly Mock<IRepository<MoujaEntity, int>> MoujaRepository = new();
        public readonly Mock<IRepository<SubZoneDetailsForCVEntity, int>> SubZoneRepository = new();
        public readonly Mock<IRepository<OwningDepartmentEntity, int>> DepartmentRepository = new();
        public readonly Mock<IRepository<AssetOrganizationMasterEntity, int>> OrganizationRepository = new();
        public readonly Mock<IRepository<AssetConditionMasterEntity, int>> ConditionRepository = new();
        public readonly Mock<IRepository<DepartmentMasterEntity, int>> DeptMasterRepository = new();
        public readonly Mock<IRepository<ModuleMasterEntity, int>> ModuleMasterRepository = new();
        public readonly Mock<IRepository<AssetDesignationEntity, int>> DesignationRepository = new();
        public readonly Mock<IRepository<AssetTypeOfUseMasterEntity, int>> AmsTypeOfUseRepository = new();
        public readonly Mock<IRepository<AssetSubTypeOfUseEntity, int>> AmsSubTypeOfUseRepository = new();
        public readonly Mock<ILogger<AssetMasterService>> Logger = new();
        public readonly Mock<IRepository<InventoryBatchEntity, int>> InventoryBatchRepository = new();
        public readonly Mock<IRepository<InventoryAssetDetailEntity, int>> InventoryAssetDetailRepository = new();
        public readonly Mock<IRepository<InventoryItemCategoryEntity, int>> InventoryCategoryRepository = new();
        public readonly Mock<IRepository<InventoryItemNameEntity, int>> InventoryNameRepository = new();
        public readonly Mock<IRepository<InventoryItemModelEntity, int>> InventoryModelRepository = new();
        public readonly Mock<IRepository<OwningDepartmentEntity, int>> InventoryDepartmentRepository = new();
        public readonly Mock<IInventoryDocumentApplicationService> InventoryDocumentApplicationService = new();
        public readonly Mock<IRepository<AssetLeaseRentDetailsEntity, int>> LeaseRentDetailsRepository = new();
        public IMapper Mapper = null!;
    }

    /// <summary>
    /// Test-only subclass exposing a hook into the otherwise-unoverridden <c>ValidateForCreateAsync</c>
    /// (AssetMasterService.Crud.cs never overrides it, so the base <c>BaseCommonCrudService</c> default
    /// always returns Success — the only way to exercise CreateAsync's rollback-on-validation-failure
    /// path is to override the hook ourselves).
    /// </summary>
    private sealed class TestableAssetMasterService : AssetMasterService
    {
        private readonly Func<ValidationResult> _createValidationOverride;

        public TestableAssetMasterService(
            Func<ValidationResult> createValidationOverride,
            IRepository<AssetMasterEntity, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IReferenceValidationService referenceValidator,
            IRepository<AssetFieldValueEntity, int> fieldValueRepository,
            IRepository<SubUnitsDetailsEntity, int> floorDetailsRepository,
            IRepository<AssetRoomWiseSubmissionDetailsEntity, int> roomWiseSubmissionRepository,
            IRepository<AssetCategoryEntity, int> assetCategoryRepository,
            IRepository<AssetTypeEntity, int> assetTypeRepository,
            IRepository<ULBMasterEntity, int> ulbRepository,
            IRepository<AssetDetailsEntity, int> detailsRepository,
            IRepository<AssetDocumentEntity, int> assetDocumentRepository,
            IRepository<AssetPhotoEntity, int> assetPhotoRepository,
            IAssetPhotoApplicationService assetPhotoApplicationService,
            IDocumentApplicationService documentApplicationService,
            IRepository<ZoneEntity, int> zoneRepository,
            IRepository<WardEntity, int> wardRepository,
            IRepository<MoujaEntity, int> moujaRepository,
            IRepository<SubZoneDetailsForCVEntity, int> subZoneRepository,
            IRepository<OwningDepartmentEntity, int> departmentRepository,
            IRepository<AssetOrganizationMasterEntity, int> organizationRepository,
            IRepository<AssetConditionMasterEntity, int> conditionRepository,
            IRepository<DepartmentMasterEntity, int> deptMasterRepository,
            IRepository<ModuleMasterEntity, int> moduleMasterRepository,
            IRepository<AssetDesignationEntity, int> designationRepository,
            IRepository<AssetTypeOfUseMasterEntity, int> amsTypeOfUseRepository,
            IRepository<AssetSubTypeOfUseEntity, int> amsSubTypeOfUseRepository,
            ILogger<AssetMasterService> logger,
            IRepository<InventoryBatchEntity, int> inventoryBatchRepository,
            IRepository<InventoryAssetDetailEntity, int> inventoryAssetDetailRepository,
            IRepository<InventoryItemCategoryEntity, int> inventoryCategoryRepository,
            IRepository<InventoryItemNameEntity, int> inventoryNameRepository,
            IRepository<InventoryItemModelEntity, int> inventoryModelRepository,
            IRepository<OwningDepartmentEntity, int> inventoryDepartmentRepository,
            IInventoryDocumentApplicationService inventoryDocumentApplicationService,
            IRepository<AssetLeaseRentDetailsEntity, int> leaseRentDetailsRepository)
            : base(repository, unitOfWork, mapper, referenceValidator, fieldValueRepository, floorDetailsRepository,
                  roomWiseSubmissionRepository, assetCategoryRepository, assetTypeRepository, ulbRepository,
                  detailsRepository, assetDocumentRepository, assetPhotoRepository, assetPhotoApplicationService,
                  documentApplicationService, zoneRepository, wardRepository, moujaRepository, subZoneRepository,
                  departmentRepository, organizationRepository, conditionRepository, deptMasterRepository,
                  moduleMasterRepository, designationRepository, amsTypeOfUseRepository, amsSubTypeOfUseRepository,
                  logger, inventoryBatchRepository, inventoryAssetDetailRepository, inventoryCategoryRepository,
                  inventoryNameRepository, inventoryModelRepository, inventoryDepartmentRepository,
                  inventoryDocumentApplicationService, leaseRentDetailsRepository)
        {
            _createValidationOverride = createValidationOverride;
        }

        protected override Task<ValidationResult> ValidateForCreateAsync(AssetMasterEntity entity, CancellationToken cancellationToken = default)
            => Task.FromResult(_createValidationOverride());
    }

    private static IMapper CreateMapper()
    {
        var mapperConfig = new MapperConfiguration(
            cfg => cfg.AddProfile<AssetMasterMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        return mapperConfig.CreateMapper();
    }

    private static void SetupRows<T>(Mock<IRepository<T, int>> repoMock, params T[] rows) where T : class
    {
        var mockQuery = rows.ToList().BuildMockDbSet();
        repoMock.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);
    }

    /// <summary>
    /// Builds every mock with a safe default: every queryable-backed repository returns an empty
    /// MockQueryable-backed set unless a test overrides it, so unrelated LINQ joins inside
    /// GetAllInternalAsync/GetByIdAsync don't blow up on an unconfigured mock.
    ///
    /// The AssetMaster repository is the one exception: CreateAsync ends with an internal
    /// GetByIdAsync(entity.Id, ...) reload, so whatever AddAsync "persists" must still be visible to
    /// that reload's GetQueryable() call. Its default is therefore a growing in-memory list (fed by
    /// AddAsync's callback) rather than a fixed empty snapshot. Tests that pre-populate the repository
    /// (Update/Delete/GetById/GetAll) simply override this default via their own
    /// repository.Setup(r =&gt; r.GetQueryable())... call, which replaces this Setup.
    /// </summary>
    private static ServiceMocks NewMocks()
    {
        var m = new ServiceMocks();

        var createdRows = new List<AssetMasterEntity>();
        m.Repository.Setup(r => r.GetQueryable()).Returns(() => createdRows.ToList().BuildMockDbSet().Object);
        m.Repository.Setup(r => r.AddAsync(It.IsAny<AssetMasterEntity>(), It.IsAny<CancellationToken>()))
            .Callback<AssetMasterEntity, CancellationToken>((e, _) =>
            {
                e.Id = 501;
                // Real EF would return an empty (not null) navigation collection here; the mapping
                // profile Ignore()s FieldValues on create, so the mapped entity's collection is still
                // null at this point -- set it explicitly to avoid an NRE in GetByIdAsync's reload
                // select (`a.FieldValues!.Where(...)`).
                e.FieldValues ??= new List<AssetFieldValueEntity>();
                createdRows.Add(e);
            })
            .ReturnsAsync((AssetMasterEntity e, CancellationToken _) => e);

        // GetLocationInfoByAssetIdsAsync (AssetMasterService.Location.cs) executes its join chain as
        // literal LINQ-to-Objects here (MockQueryable), not SQL translation -- `details.ZoneId` on a
        // null `details` (the result of a missing-row DefaultIfEmpty()) throws instead of the safe
        // null-propagation real SQL would give it. Real EF would return a blank-but-non-null details
        // row here in place of "no row at all" the moment CreateAsync/UpdateAsync insert one, so
        // DetailsRepository defaults to the same growing-list-fed-by-AddAsync pattern as Repository:
        // both CreateAsync's BuildDetails and UpdateAsync's "no existing details -> create one" branch
        // always insert a row keyed to the asset's own id, which keeps the final GetByIdAsync reload's
        // join chain safe. Tests that read pre-existing assets directly (GetByIdAsync/GetAllAsync) seed
        // their own blank AssetDetailsEntity rows explicitly for the same reason.
        var createdDetailsRows = new List<AssetDetailsEntity>();
        m.DetailsRepository.Setup(r => r.GetQueryable()).Returns(() => createdDetailsRows.ToList().BuildMockDbSet().Object);
        m.DetailsRepository.Setup(r => r.AddAsync(It.IsAny<AssetDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Callback<AssetDetailsEntity, CancellationToken>((d, _) =>
            {
                if (d.Id == 0) d.Id = 601;
                createdDetailsRows.Add(d);
            })
            .ReturnsAsync((AssetDetailsEntity d, CancellationToken _) => d);

        SetupRows(m.FieldValueRepository);
        SetupRows(m.FloorDetailsRepository);
        SetupRows(m.RoomWiseSubmissionRepository);
        SetupRows(m.AssetCategoryRepository);
        SetupRows(m.AssetTypeRepository);
        SetupRows(m.UlbRepository);
        SetupRows(m.AssetDocumentRepository);
        SetupRows(m.AssetPhotoRepository);
        SetupRows(m.ZoneRepository);
        SetupRows(m.WardRepository);
        SetupRows(m.MoujaRepository);
        SetupRows(m.SubZoneRepository);
        SetupRows(m.DepartmentRepository);
        SetupRows(m.OrganizationRepository);
        SetupRows(m.ConditionRepository);
        SetupRows(m.DeptMasterRepository);
        SetupRows(m.ModuleMasterRepository);
        SetupRows(m.DesignationRepository);
        SetupRows(m.AmsTypeOfUseRepository);
        SetupRows(m.AmsSubTypeOfUseRepository);
        SetupRows(m.InventoryBatchRepository);
        SetupRows(m.InventoryAssetDetailRepository);
        SetupRows(m.InventoryCategoryRepository);
        SetupRows(m.InventoryNameRepository);
        SetupRows(m.InventoryModelRepository);
        SetupRows(m.InventoryDepartmentRepository);
        SetupRows(m.LeaseRentDetailsRepository);

        m.Mapper = CreateMapper();
        return m;
    }

    private static AssetMasterService BuildService(ServiceMocks m) => new(
        m.Repository.Object, m.UnitOfWork.Object, m.Mapper, m.ReferenceValidator.Object,
        m.FieldValueRepository.Object, m.FloorDetailsRepository.Object, m.RoomWiseSubmissionRepository.Object,
        m.AssetCategoryRepository.Object, m.AssetTypeRepository.Object, m.UlbRepository.Object,
        m.DetailsRepository.Object, m.AssetDocumentRepository.Object, m.AssetPhotoRepository.Object,
        m.AssetPhotoApplicationService.Object, m.DocumentApplicationService.Object,
        m.ZoneRepository.Object, m.WardRepository.Object, m.MoujaRepository.Object, m.SubZoneRepository.Object,
        m.DepartmentRepository.Object, m.OrganizationRepository.Object, m.ConditionRepository.Object,
        m.DeptMasterRepository.Object, m.ModuleMasterRepository.Object, m.DesignationRepository.Object,
        m.AmsTypeOfUseRepository.Object, m.AmsSubTypeOfUseRepository.Object, m.Logger.Object,
        m.InventoryBatchRepository.Object, m.InventoryAssetDetailRepository.Object, m.InventoryCategoryRepository.Object,
        m.InventoryNameRepository.Object, m.InventoryModelRepository.Object, m.InventoryDepartmentRepository.Object,
        m.InventoryDocumentApplicationService.Object, m.LeaseRentDetailsRepository.Object);

    private static TestableAssetMasterService BuildTestableService(ServiceMocks m, Func<ValidationResult> createValidationOverride) => new(
        createValidationOverride,
        m.Repository.Object, m.UnitOfWork.Object, m.Mapper, m.ReferenceValidator.Object,
        m.FieldValueRepository.Object, m.FloorDetailsRepository.Object, m.RoomWiseSubmissionRepository.Object,
        m.AssetCategoryRepository.Object, m.AssetTypeRepository.Object, m.UlbRepository.Object,
        m.DetailsRepository.Object, m.AssetDocumentRepository.Object, m.AssetPhotoRepository.Object,
        m.AssetPhotoApplicationService.Object, m.DocumentApplicationService.Object,
        m.ZoneRepository.Object, m.WardRepository.Object, m.MoujaRepository.Object, m.SubZoneRepository.Object,
        m.DepartmentRepository.Object, m.OrganizationRepository.Object, m.ConditionRepository.Object,
        m.DeptMasterRepository.Object, m.ModuleMasterRepository.Object, m.DesignationRepository.Object,
        m.AmsTypeOfUseRepository.Object, m.AmsSubTypeOfUseRepository.Object, m.Logger.Object,
        m.InventoryBatchRepository.Object, m.InventoryAssetDetailRepository.Object, m.InventoryCategoryRepository.Object,
        m.InventoryNameRepository.Object, m.InventoryModelRepository.Object, m.InventoryDepartmentRepository.Object,
        m.InventoryDocumentApplicationService.Object, m.LeaseRentDetailsRepository.Object);

    private static AssetMasterService CreateService(
        out Mock<IRepository<AssetMasterEntity, int>> repository,
        out Mock<IUnitOfWork> unitOfWork,
        out Mock<IReferenceValidationService> referenceValidator,
        out Mock<IRepository<AssetFieldValueEntity, int>> fieldValueRepository,
        out Mock<IRepository<AssetDetailsEntity, int>> detailsRepository,
        out Mock<IRepository<SubUnitsDetailsEntity, int>> floorDetailsRepository,
        out Mock<IRepository<AssetCategoryEntity, int>> assetCategoryRepository,
        out Mock<IRepository<AssetTypeEntity, int>> assetTypeRepository,
        out Mock<ILogger<AssetMasterService>> logger)
    {
        var m = NewMocks();
        repository = m.Repository;
        unitOfWork = m.UnitOfWork;
        referenceValidator = m.ReferenceValidator;
        fieldValueRepository = m.FieldValueRepository;
        detailsRepository = m.DetailsRepository;
        floorDetailsRepository = m.FloorDetailsRepository;
        assetCategoryRepository = m.AssetCategoryRepository;
        assetTypeRepository = m.AssetTypeRepository;
        logger = m.Logger;
        return BuildService(m);
    }

    private static TestableAssetMasterService CreateServiceWithCreateValidation(
        Func<ValidationResult> createValidationOverride,
        out Mock<IRepository<AssetMasterEntity, int>> repository,
        out Mock<IUnitOfWork> unitOfWork,
        out Mock<IRepository<AssetCategoryEntity, int>> assetCategoryRepository,
        out Mock<IRepository<AssetTypeEntity, int>> assetTypeRepository)
    {
        var m = NewMocks();
        repository = m.Repository;
        unitOfWork = m.UnitOfWork;
        assetCategoryRepository = m.AssetCategoryRepository;
        assetTypeRepository = m.AssetTypeRepository;
        return BuildTestableService(m, createValidationOverride);
    }

    private static void SetupCategoryAndType(
        Mock<IRepository<AssetCategoryEntity, int>> categoryRepo,
        Mock<IRepository<AssetTypeEntity, int>> typeRepo,
        int categoryId, string categoryCode,
        int typeId, string typeCode)
    {
        SetupRows(categoryRepo, new AssetCategoryEntity
        {
            Id = categoryId,
            CategoryCode = categoryCode,
            CategoryName = categoryCode,
            IsActive = true
        });
        SetupRows(typeRepo, new AssetTypeEntity
        {
            Id = typeId,
            AssetCategoryId = categoryId,
            TypeCode = typeCode,
            TypeName = typeCode,
            IsActive = true
        });
    }

    private static CreateAssetMasterDto NewCreateDto(int categoryId = 1, int typeId = 1) => new()
    {
        OrganizationId = 1,
        AssetName = "Test Asset",
        AssetCategoryId = categoryId,
        AssetTypeId = typeId,
        CreatedBy = 42
    };

    private static UpdateAssetMasterDto NewUpdateDto(int categoryId = 1, int typeId = 1, string assetNo = "AMC-BLDG-OFC-0001") => new()
    {
        OrganizationId = 1,
        AssetName = "Updated Asset",
        AssetCategoryId = categoryId,
        AssetTypeId = typeId,
        AssetNo = assetNo,
        IsActive = true,
        UpdatedBy = 42
    };

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WithValidDto_GeneratesAssetNoAndPersists()
    {
        var service = CreateService(out var repository, out var unitOfWork, out _, out _, out _, out _,
            out var assetCategoryRepository, out var assetTypeRepository, out _);
        SetupCategoryAndType(assetCategoryRepository, assetTypeRepository, 1, "BLDG", 1, "OFC");

        var dto = NewCreateDto();

        var result = await service.CreateAsync(dto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("AMC-BLDG-OFC-0001", result.AssetNo);
        repository.Verify(r => r.AddAsync(It.IsAny<AssetMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Regression test for the fix to roadmap Section B item 5: GenerateAssetNoAsync's sequence lock
    /// only guards the max-sequence read, not persistence, so a concurrent create can already have
    /// committed the number this one just reserved. CreateAsync should catch the resulting
    /// UQ_AssetMaster_AssetNo unique-index violation, regenerate a fresh number, and retry -- not
    /// surface the race as a client-facing failure.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenAssetNoUniqueConstraintViolated_RetriesWithFreshNumber_Succeeds()
    {
        var service = CreateService(out var repository, out var unitOfWork, out _, out _, out _, out _,
            out var assetCategoryRepository, out var assetTypeRepository, out _);
        SetupCategoryAndType(assetCategoryRepository, assetTypeRepository, 1, "BLDG", 1, "OFC");

        var saveAttempts = 0;
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                saveAttempts++;
                if (saveAttempts == 1)
                {
                    throw new DbUpdateException(
                        "An error occurred while saving the entity changes.",
                        new Exception("Cannot insert duplicate key row in object 'AMS.AssetMaster' with unique index 'UQ_AssetMaster_AssetNo'."));
                }
                return 1;
            });

        var dto = NewCreateDto();

        var result = await service.CreateAsync(dto, CancellationToken.None);

        Assert.NotNull(result);
        // The exact regenerated suffix depends on what the mocked repository considers "existing" at
        // retry time (this test double reflects the AddAsync'd entity immediately, unlike a real DB
        // that never actually persisted the rejected row) -- the behavior under test is that CreateAsync
        // recovers and returns a valid AssetNo, not which specific sequence number it lands on.
        Assert.StartsWith("AMC-BLDG-OFC-", result.AssetNo);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
        unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ParsesFieldValuesJson_ArrayForm()
    {
        var service = CreateService(out _, out _, out _, out var fieldValueRepository, out _, out _,
            out var assetCategoryRepository, out var assetTypeRepository, out _);
        SetupCategoryAndType(assetCategoryRepository, assetTypeRepository, 1, "BLDG", 1, "OFC");

        var dto = NewCreateDto();
        dto.FieldValuesJson = "[{\"fieldDefinitionId\":1,\"fieldName\":\"Color\",\"fieldValue\":\"Red\"}," +
                               "{\"fieldDefinitionId\":2,\"fieldName\":\"Size\",\"fieldValue\":\"Large\"}]";

        var result = await service.CreateAsync(dto, CancellationToken.None);

        Assert.NotNull(result);
        fieldValueRepository.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<AssetFieldValueEntity>>(list =>
                list.Count() == 2 &&
                list.Any(f => f.FieldName == "Color" && f.FieldValue == "Red") &&
                list.Any(f => f.FieldName == "Size" && f.FieldValue == "Large")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ParsesFieldValuesJson_SingleObjectForm()
    {
        var service = CreateService(out _, out _, out _, out var fieldValueRepository, out _, out _,
            out var assetCategoryRepository, out var assetTypeRepository, out _);
        SetupCategoryAndType(assetCategoryRepository, assetTypeRepository, 1, "BLDG", 1, "OFC");

        var dto = NewCreateDto();
        dto.FieldValuesJson = "{\"fieldDefinitionId\":1,\"fieldName\":\"Color\",\"fieldValue\":\"Red\"}";

        var result = await service.CreateAsync(dto, CancellationToken.None);

        Assert.NotNull(result);
        fieldValueRepository.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<AssetFieldValueEntity>>(list =>
                list.Count() == 1 && list.Single().FieldName == "Color" && list.Single().FieldValue == "Red"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ParsesFieldValuesJson_MalformedJson_LogsAndContinuesWithEmptyList()
    {
        var service = CreateService(out _, out _, out _, out var fieldValueRepository, out _, out _,
            out var assetCategoryRepository, out var assetTypeRepository, out var logger);
        SetupCategoryAndType(assetCategoryRepository, assetTypeRepository, 1, "BLDG", 1, "OFC");

        var dto = NewCreateDto();
        dto.FieldValuesJson = "{not valid json";

        var result = await service.CreateAsync(dto, CancellationToken.None);

        // Malformed JSON must not fail the whole create -- it's caught, logged, and the asset is
        // still created with an empty field-value list.
        Assert.NotNull(result);
        fieldValueRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<AssetFieldValueEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenValidationFails_RollsBackTransaction_ThrowsValidationException()
    {
        var service = CreateServiceWithCreateValidation(
            () => ValidationResult.Failure("AssetName", "AMS_AssetMaster_AssetName_Duplicate"),
            out var repository, out var unitOfWork, out var assetCategoryRepository, out var assetTypeRepository);
        SetupCategoryAndType(assetCategoryRepository, assetTypeRepository, 1, "BLDG", 1, "OFC");

        var dto = NewCreateDto();

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto, CancellationToken.None));

        // CreateAsync calls RollbackTransactionAsync explicitly inline right before throwing the
        // ValidationException for a failed validationResult, and then AGAIN in the outer
        // `catch (ValidationException)` block (shouldRollbackTransaction is still true at that point,
        // since it's only flipped to false after a successful CommitTransactionAsync). Real, slightly
        // wasteful double-rollback -- asserted here as-is, not "fixed" by the test.
        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.AddAsync(It.IsAny<AssetMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenPhotoFilesCountDoesNotMatchMetadataCount_ThrowsValidationException()
    {
        var service = CreateService(out _, out var unitOfWork, out _, out _, out _, out _,
            out var assetCategoryRepository, out var assetTypeRepository, out _);
        SetupCategoryAndType(assetCategoryRepository, assetTypeRepository, 1, "BLDG", 1, "OFC");

        var dto = NewCreateDto();
        dto.PhotoFiles = new List<IFormFile> { Mock.Of<IFormFile>(), Mock.Of<IFormFile>() };
        dto.PhotoMetadataJson = "[{\"photoTypeId\":1}]"; // only 1 metadata entry for 2 files

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto, CancellationToken.None));

        Assert.Contains("PhotoFiles", ex.Errors.Keys);
        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithMalformedPhotoMetadataJson_ThrowsValidationException()
    {
        var service = CreateService(out _, out var unitOfWork, out _, out _, out _, out _,
            out var assetCategoryRepository, out var assetTypeRepository, out _);
        SetupCategoryAndType(assetCategoryRepository, assetTypeRepository, 1, "BLDG", 1, "OFC");

        var dto = NewCreateDto();
        dto.PhotoFiles = new List<IFormFile> { Mock.Of<IFormFile>() };
        dto.PhotoMetadataJson = "{not valid json";

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto, CancellationToken.None));

        Assert.Contains("PhotoMetadataJson", ex.Errors.Keys);
        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SetsIsActiveFalseRegardlessOfDtoValue()
    {
        var service = CreateService(out _, out _, out _, out _, out _, out _,
            out var assetCategoryRepository, out var assetTypeRepository, out _);
        SetupCategoryAndType(assetCategoryRepository, assetTypeRepository, 1, "BLDG", 1, "OFC");

        var dto = NewCreateDto();
        dto.IsActive = true; // caller asks for an already-active asset...

        var result = await service.CreateAsync(dto, CancellationToken.None);

        // ...but CreateAsync does `entity.IsActive = false;` unconditionally right after mapping,
        // regardless of what the DTO requested.
        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_DiffPatchesFieldValues_ByIdThenNormalizedName()
    {
        var service = CreateService(out var repository, out _, out _, out var fieldValueRepository,
            out _, out _, out _, out _, out _);

        var existing = new AssetMasterEntity
        {
            Id = 5,
            AssetName = "Old Name",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            AssetNo = "AMC-BLDG-OFC-0001",
            IsActive = true,
            FieldValues = new List<AssetFieldValueEntity>()
        };
        repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        repository.Setup(r => r.GetQueryable()).Returns(() => new List<AssetMasterEntity> { existing }.BuildMockDbSet().Object);

        var byIdMatch = new AssetFieldValueEntity { Id = 10, AssetId = 5, FieldDefinitionId = 1, FieldName = "Area", FieldValue = "100", IsActive = true };
        var byNameMatch = new AssetFieldValueEntity { Id = 11, AssetId = 5, FieldDefinitionId = 2, FieldName = "Height", FieldValue = "5", IsActive = true };
        fieldValueRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetFieldValueEntity> { byIdMatch, byNameMatch }.BuildMockDbSet().Object);

        var updateDto = NewUpdateDto();
        updateDto.FieldValues = new List<UpdateAssetFieldValueDto>
        {
            new() { Id = 10, FieldDefinitionId = 1, FieldName = "Area", FieldValue = "150" },              // matched by Id
            new() { Id = null, FieldDefinitionId = 2, FieldName = "  height ", FieldValue = "6" },          // matched by normalized (trim+lower) name
            new() { Id = null, FieldDefinitionId = 3, FieldName = "NewField", FieldValue = "X" }            // no match -> new row
        };

        var result = await service.UpdateAsync(5, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        fieldValueRepository.Verify(r => r.UpdateAsync(
            It.Is<AssetFieldValueEntity>(f => f.Id == 10 && f.FieldValue == "150"), It.IsAny<CancellationToken>()), Times.Once);
        fieldValueRepository.Verify(r => r.UpdateAsync(
            It.Is<AssetFieldValueEntity>(f => f.Id == 11 && f.FieldValue == "6"), It.IsAny<CancellationToken>()), Times.Once);
        fieldValueRepository.Verify(r => r.AddAsync(
            It.Is<AssetFieldValueEntity>(f => f.FieldName == "NewField" && f.FieldValue == "X"), It.IsAny<CancellationToken>()), Times.Once);

        // The whole point of the diff-patch: existing rows are patched via UpdateAsync, never
        // deleted-and-recreated.
        fieldValueRepository.Verify(r => r.DeleteAsync(It.IsAny<AssetFieldValueEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        fieldValueRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RegeneratesAssetNo_WhenCategoryOrTypeChanges()
    {
        var service = CreateService(out var repository, out _, out _, out _, out _, out _,
            out var assetCategoryRepository, out var assetTypeRepository, out _);

        var existing = new AssetMasterEntity
        {
            Id = 5,
            AssetName = "Old Name",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            AssetNo = "AMC-BLDG-OFC-0001",
            IsActive = true,
            FieldValues = new List<AssetFieldValueEntity>()
        };
        repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        repository.Setup(r => r.GetQueryable()).Returns(() => new List<AssetMasterEntity> { existing }.BuildMockDbSet().Object);

        SetupCategoryAndType(assetCategoryRepository, assetTypeRepository, 2, "LAND", 2, "PLOT");

        // AssetCategoryId/AssetTypeId change from (1,1) to (2,2) -- AssetNo string itself is left
        // stale/unchanged on the DTO to prove the regen is triggered by the category/type change,
        // not merely because the AssetNo field happens to be blank.
        var updateDto = NewUpdateDto(categoryId: 2, typeId: 2, assetNo: "AMC-BLDG-OFC-0001");

        var result = await service.UpdateAsync(5, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("AMC-LAND-PLOT-0001", result!.AssetNo);
        Assert.NotEqual("AMC-BLDG-OFC-0001", result.AssetNo);
    }

    /// <summary>
    /// Regression test for the fix to roadmap Section B item 5, on the UpdateAsync side: a
    /// category/type change that regenerates AssetNo can race a concurrent request the same way
    /// CreateAsync can. UpdateAsync should catch the resulting UQ_AssetMaster_AssetNo violation,
    /// regenerate again, and retry -- not surface the race as a client-facing failure.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenAssetNoUniqueConstraintViolated_RetriesWithFreshNumber_Succeeds()
    {
        var service = CreateService(out var repository, out var unitOfWork, out _, out _, out _, out _,
            out var assetCategoryRepository, out var assetTypeRepository, out _);

        var existing = new AssetMasterEntity
        {
            Id = 5,
            AssetName = "Old Name",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            AssetNo = "AMC-BLDG-OFC-0001",
            IsActive = true,
            FieldValues = new List<AssetFieldValueEntity>()
        };
        repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        repository.Setup(r => r.GetQueryable()).Returns(() => new List<AssetMasterEntity> { existing }.BuildMockDbSet().Object);

        SetupCategoryAndType(assetCategoryRepository, assetTypeRepository, 2, "LAND", 2, "PLOT");

        var saveAttempts = 0;
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                saveAttempts++;
                if (saveAttempts == 1)
                {
                    throw new DbUpdateException(
                        "An error occurred while saving the entity changes.",
                        new Exception("Cannot insert duplicate key row in object 'AMS.AssetMaster' with unique index 'UQ_AssetMaster_AssetNo'."));
                }
                return 1;
            });

        var updateDto = NewUpdateDto(categoryId: 2, typeId: 2, assetNo: "AMC-BLDG-OFC-0001");

        var result = await service.UpdateAsync(5, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.StartsWith("AMC-LAND-PLOT-", result!.AssetNo);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
        unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotRegenerate_WhenUnchanged()
    {
        var service = CreateService(out var repository, out _, out _, out _, out _, out _,
            out var assetCategoryRepository, out var assetTypeRepository, out _);

        var existing = new AssetMasterEntity
        {
            Id = 5,
            AssetName = "Old Name",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            AssetNo = "AMC-BLDG-OFC-0001",
            IsActive = true,
            FieldValues = new List<AssetFieldValueEntity>()
        };
        repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        repository.Setup(r => r.GetQueryable()).Returns(() => new List<AssetMasterEntity> { existing }.BuildMockDbSet().Object);

        var updateDto = NewUpdateDto(categoryId: 1, typeId: 1, assetNo: "AMC-BLDG-OFC-0001");

        var result = await service.UpdateAsync(5, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("AMC-BLDG-OFC-0001", result!.AssetNo);
        // Category/Type unchanged and AssetNo non-blank -> GenerateAssetNoAsync must never run.
        assetCategoryRepository.Verify(r => r.GetQueryable(), Times.Never);
        assetTypeRepository.Verify(r => r.GetQueryable(), Times.Never);
    }

    /// <summary>
    /// Documents a real, non-obvious gap rather than asserting the (currently unreachable)
    /// deactivation-blocking behavior as if it worked correctly. See
    /// <c>AssetMasterMappingProfile.cs</c> (~line 88): <c>CreateMap&lt;UpdateAssetMasterDto, AssetMasterEntity&gt;()</c>
    /// explicitly <c>.ForMember(d =&gt; d.IsActive, o =&gt; o.Ignore())</c>s <c>IsActive</c>. Because of
    /// that, <c>UpdateAsync</c>'s <c>_mapper.Map(updateDto, entity)</c> call NEVER changes
    /// <c>entity.IsActive</c> -- it stays whatever it already was when loaded from the repository.
    /// <c>ValidateForDeactivationAsync</c>'s guard (<c>currentEntity.IsActive &amp;&amp; !updatedEntity.IsActive</c>)
    /// therefore can never see a true-&gt;false transition through this code path, so the
    /// reference-validator gate that's supposed to block deactivating a referenced asset can never
    /// fire via Update -- regardless of what the caller sends for <c>IsActive</c>. This needs a
    /// product decision (map IsActive explicitly and rely on the guard, or move the deactivation entry
    /// point elsewhere) -- this test only proves what the code does today.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_SettingIsActiveFalseOnDto_DoesNotChangeEntityIsActive_BecauseMappingIgnoresIt()
    {
        var service = CreateService(out var repository, out _, out var referenceValidator, out _, out _, out _, out _, out _, out _);

        var existing = new AssetMasterEntity
        {
            Id = 5,
            AssetName = "Active Asset",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            AssetNo = "AMC-BLDG-OFC-0001",
            IsActive = true,
            FieldValues = new List<AssetFieldValueEntity>()
        };
        repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        repository.Setup(r => r.GetQueryable()).Returns(() => new List<AssetMasterEntity> { existing }.BuildMockDbSet().Object);

        var updateDto = NewUpdateDto();
        updateDto.IsActive = false; // caller asks to deactivate...

        var result = await service.UpdateAsync(5, updateDto, CancellationToken.None);

        // ...but the reloaded entity is STILL active, because the mapping never touched IsActive.
        Assert.NotNull(result);
        Assert.True(result!.IsActive);

        // And because updatedEntity.IsActive never actually flips to false, the reference-validator
        // gate is never even invoked.
        referenceValidator.Verify(
            v => v.ValidateReferencesAsync<AssetMasterEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_CascadesToFieldValuesAndDetails_SoftDeletesAll()
    {
        var service = CreateService(out var repository, out var unitOfWork, out var referenceValidator,
            out var fieldValueRepository, out var detailsRepository, out _, out _, out _, out _);

        var entity = new AssetMasterEntity { Id = 5, AssetName = "To Delete", IsActive = true };
        repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        referenceValidator.Setup(v => v.ValidateReferencesAsync<AssetMasterEntity>(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        var fv1 = new AssetFieldValueEntity { Id = 1, AssetId = 5, FieldName = "A", IsActive = true };
        var fv2 = new AssetFieldValueEntity { Id = 2, AssetId = 5, FieldName = "B", IsActive = true };
        fieldValueRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetFieldValueEntity> { fv1, fv2 }.BuildMockDbSet().Object);

        var details = new AssetDetailsEntity { Id = 1, AssetId = 5, IsActive = true };
        detailsRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetDetailsEntity> { details }.BuildMockDbSet().Object);

        var result = await service.DeleteAsync(5, CancellationToken.None);

        Assert.True(result);
        fieldValueRepository.Verify(r => r.UpdateAsync(
            It.Is<AssetFieldValueEntity>(f => f.Id == 1 && !f.IsActive && f.MarkedForDeletion), It.IsAny<CancellationToken>()), Times.Once);
        fieldValueRepository.Verify(r => r.UpdateAsync(
            It.Is<AssetFieldValueEntity>(f => f.Id == 2 && !f.IsActive && f.MarkedForDeletion), It.IsAny<CancellationToken>()), Times.Once);
        detailsRepository.Verify(r => r.UpdateAsync(
            It.Is<AssetDetailsEntity>(d => !d.IsActive && d.MarkedForDeletion), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.DeleteAsync(
            It.Is<AssetMasterEntity>(e => e.MarkedForDeletion), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenReferenced_BlockedByReferenceValidator()
    {
        var service = CreateService(out var repository, out var unitOfWork, out var referenceValidator,
            out _, out _, out _, out _, out _, out _);

        var entity = new AssetMasterEntity { Id = 5, AssetName = "Referenced Asset", IsActive = true };
        repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        referenceValidator.Setup(v => v.ValidateReferencesAsync<AssetMasterEntity>(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Id", "Cannot delete - record is referenced by other entities"));

        await Assert.ThrowsAsync<ValidationException>(() => service.DeleteAsync(5, CancellationToken.None));

        repository.Verify(r => r.DeleteAsync(It.IsAny<AssetMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ComputesUnitAndFloorStats_ViaJoin()
    {
        var service = CreateService(out var repository, out _, out _, out _, out var detailsRepository, out var floorDetailsRepository, out _, out _, out _);

        var parent = new AssetMasterEntity { Id = 1, AssetName = "Building", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        var child2 = new AssetMasterEntity { Id = 2, ParentAssetId = 1, AssetName = "Unit 2", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        var child3 = new AssetMasterEntity { Id = 3, ParentAssetId = 1, AssetName = "Unit 3", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        repository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { parent, child2, child3 }.BuildMockDbSet().Object);

        // Blank (but present) AssetDetails rows -- see the comment on NewMocks()'s DetailsRepository
        // default: GetLocationInfoByAssetIdsAsync's join chain NREs on a genuinely MISSING details row
        // under MockQueryable's LINQ-to-Objects execution, so every asset id read directly via
        // repository.GetQueryable() (bypassing Create/UpdateAsync's own AddAsync calls) needs one.
        detailsRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetDetailsEntity>
        {
            new() { AssetId = 1 }, new() { AssetId = 2 }, new() { AssetId = 3 }
        }.BuildMockDbSet().Object);

        // fd2/fd3 share FloorId=101 -- proves TotalFloors counts DISTINCT floors (2), not row count (3).
        var fd1 = new SubUnitsDetailsEntity { Id = 100, AssetId = 2, FloorId = 100, IsActive = true, Asset = child2 };
        var fd2 = new SubUnitsDetailsEntity { Id = 101, AssetId = 3, FloorId = 101, IsActive = true, Asset = child3 };
        var fd3 = new SubUnitsDetailsEntity { Id = 102, AssetId = 3, FloorId = 101, IsActive = true, Asset = child3 };
        floorDetailsRepository.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity> { fd1, fd2, fd3 }.BuildMockDbSet().Object);

        var dto = await service.GetByIdAsync(1, CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(2, dto!.TotalUnits);
        Assert.Equal(2, dto.TotalFloors);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var service = CreateService(out _, out _, out _, out _, out _, out _, out _, out _, out _);

        var result = await service.GetByIdAsync(999, CancellationToken.None);

        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ExcludesFieldValues_ForPerformance()
    {
        var service = CreateService(out var repository, out _, out _, out _, out var detailsRepository, out _, out _, out _, out _);

        var asset = new AssetMasterEntity
        {
            Id = 1,
            AssetName = "Asset One",
            IsActive = true,
            ParentAssetId = null,
            FieldValues = new List<AssetFieldValueEntity>
            {
                new() { Id = 1, AssetId = 1, FieldName = "Color", FieldValue = "Red", IsActive = true }
            }
        };
        repository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { asset }.BuildMockDbSet().Object);
        // See NewMocks()'s DetailsRepository comment: EnrichLocationAsync's join chain needs a details
        // row (even blank) present for every asset id it reads.
        detailsRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetDetailsEntity> { new() { AssetId = 1 } }.BuildMockDbSet().Object);

        var result = await service.GetAllAsync(new AssetMasterQueryParameters(), CancellationToken.None);

        var dto = Assert.Single(result.Items);
        // The list-view projection in GetAllInternalAsync never sets FieldValues -- per the method's
        // own doc comment, use GetByIdAsync to get FieldValues for a specific asset.
        Assert.Null(dto.FieldValues);
    }

    [Fact]
    public async Task GetAllAsync_WithParentAssetIdInQuery_ReturnsChildAssets()
    {
        var service = CreateService(out var repository, out _, out _, out _, out var detailsRepository, out _, out _, out _, out _);

        var parent = new AssetMasterEntity { Id = 1, AssetName = "Parent", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        var child = new AssetMasterEntity { Id = 2, ParentAssetId = 1, AssetName = "Child", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        repository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { parent, child }.BuildMockDbSet().Object);
        detailsRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetDetailsEntity> { new() { AssetId = 1 }, new() { AssetId = 2 } }.BuildMockDbSet().Object);

        var result = await service.GetAllAsync(new AssetMasterQueryParameters { ParentAssetId = 1 }, CancellationToken.None);

        var dto = Assert.Single(result.Items);
        Assert.Equal(2, dto.Id);
    }

    [Fact]
    public async Task GetAllAsync_WithoutParentAssetId_ReturnsOnlyTopLevelAssets()
    {
        var service = CreateService(out var repository, out _, out _, out _, out var detailsRepository, out _, out _, out _, out _);

        var parent = new AssetMasterEntity { Id = 1, AssetName = "Parent", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        var child = new AssetMasterEntity { Id = 2, ParentAssetId = 1, AssetName = "Child", IsActive = true, FieldValues = new List<AssetFieldValueEntity>() };
        repository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { parent, child }.BuildMockDbSet().Object);
        detailsRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetDetailsEntity> { new() { AssetId = 1 }, new() { AssetId = 2 } }.BuildMockDbSet().Object);

        var result = await service.GetAllAsync(new AssetMasterQueryParameters(), CancellationToken.None);

        var dto = Assert.Single(result.Items);
        Assert.Equal(1, dto.Id);
    }

    #endregion

    #region GetByIdForUserAsync -- documented scoping gap

    /// <summary>
    /// REGRESSION TEST documenting the same gap as (removed) <c>GetAllForUserAsync</c> used to
    /// (AssetMaster-TestCoverage-Roadmap.md Section B item 1). <see cref="IAssetMasterService.GetByIdForUserAsync"/>'s
    /// doc comment is explicit: it claims to return null "if the asset's owning department is outside
    /// currentUserId's allowed scope (prevents IDOR -- a non-admin user guessing/incrementing ids
    /// outside their departments)". The actual implementation
    /// (<c>GetByIdForUserAsync(...) =&gt; GetByIdAsync(id, cancellationToken)</c>) never checks this --
    /// there is no access-scope service injected or consulted anywhere in this class (and no existing
    /// data model connects users to the owning-department table this would need to filter on; see the
    /// roadmap for detail). This test proves CURRENT behavior; it does not endorse it. Needs a real
    /// product decision before this can be treated as an IDOR-safe entry point.
    /// </summary>
    [Fact]
    public async Task GetByIdForUserAsync_IgnoresCurrentUserId_DelegatesToUnscopedGetByIdAsync()
    {
        var service = CreateService(out var repository, out _, out _, out _, out var detailsRepository, out _, out _, out _, out _);

        var assetOutsideCallerScope = new AssetMasterEntity
        {
            Id = 7,
            AssetName = "Other Dept Asset",
            IsActive = true,
            DepartmentId = 999,
            FieldValues = new List<AssetFieldValueEntity>()
        };
        repository.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { assetOutsideCallerScope }.BuildMockDbSet().Object);
        detailsRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetDetailsEntity> { new() { AssetId = 7 } }.BuildMockDbSet().Object);

        var result = await service.GetByIdForUserAsync(7, currentUserId: 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(7, result!.Id);
    }

    #endregion
}
