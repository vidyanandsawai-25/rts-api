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
using NtisPlatform.Application.DTOs.Asset_Management.AssetFieldValue;
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
/// Covers AssetMasterService.FieldValues.cs (BulkSaveFieldValuesAsync). See
/// tests/NtisPlatform.Tests/AssetMaster-TestCoverage-Roadmap.md, Section C5.
/// </summary>
public class AssetMasterServiceFieldValuesTests
{
    private static AssetMasterService CreateService(
        out Mock<IRepository<AssetFieldValueEntity, int>> fieldValueRepository,
        out Mock<IUnitOfWork> unitOfWork)
    {
        fieldValueRepository = new Mock<IRepository<AssetFieldValueEntity, int>>();
        unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var mapperConfig = new MapperConfiguration(
            cfg => cfg.AddProfile<AssetMasterMappingProfile>(),
            NullLoggerFactory.Instance);
        var mapper = mapperConfig.CreateMapper();

        return new AssetMasterService(
            repository: new Mock<IRepository<AssetMasterEntity, int>>().Object,
            unitOfWork: unitOfWork.Object,
            mapper: mapper,
            referenceValidator: new Mock<IReferenceValidationService>().Object,
            fieldValueRepository: fieldValueRepository.Object,
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
            conditionRepository: new Mock<IRepository<AssetConditionMasterEntity, int>>().Object,
            deptMasterRepository: new Mock<IRepository<DepartmentMasterEntity, int>>().Object,
            moduleMasterRepository: new Mock<IRepository<ModuleMasterEntity, int>>().Object,
            designationRepository: new Mock<IRepository<AssetDesignationEntity, int>>().Object,
            amsTypeOfUseRepository: new Mock<IRepository<AssetTypeOfUseMasterEntity, int>>().Object,
            amsSubTypeOfUseRepository: new Mock<IRepository<AssetSubTypeOfUseEntity, int>>().Object,
            logger: new Mock<ILogger<AssetMasterService>>().Object,
            inventoryBatchRepository: new Mock<IRepository<InventoryBatchEntity, int>>().Object,
            inventoryAssetDetailRepository: new Mock<IRepository<InventoryAssetDetailEntity, int>>().Object,
            inventoryCategoryRepository: new Mock<IRepository<InventoryItemCategoryEntity, int>>().Object,
            inventoryNameRepository: new Mock<IRepository<InventoryItemNameEntity, int>>().Object,
            inventoryModelRepository: new Mock<IRepository<InventoryItemModelEntity, int>>().Object,
            inventoryDepartmentRepository: new Mock<IRepository<OwningDepartmentEntity, int>>().Object,
            inventoryDocumentApplicationService: new Mock<IInventoryDocumentApplicationService>().Object,
            leaseRentDetailsRepository: new Mock<IRepository<AssetLeaseRentDetailsEntity, int>>().Object);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BulkSaveFieldValuesAsync_WithNullOrEmptyList_ReturnsTrueImmediately(bool useNull)
    {
        var service = CreateService(out var fieldValueRepository, out var unitOfWork);
        List<CreateAssetFieldValueDto>? fieldValues = useNull ? null : new List<CreateAssetFieldValueDto>();

        var result = await service.BulkSaveFieldValuesAsync(10, fieldValues!, CancellationToken.None);

        Assert.True(result);
        // The method short-circuits before touching either dependency at all.
        fieldValueRepository.VerifyNoOtherCalls();
        unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BulkSaveFieldValuesAsync_InsertsNewFieldValue_WhenFieldDefinitionIdNotInExisting()
    {
        var service = CreateService(out var fieldValueRepository, out var unitOfWork);

        fieldValueRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetFieldValueEntity>().BuildMockDbSet().Object);

        var dto = new CreateAssetFieldValueDto
        {
            AssetId = 10,
            FieldDefinitionId = 5,
            FieldName = "Color",
            FieldValue = "Red"
        };

        var result = await service.BulkSaveFieldValuesAsync(10, new List<CreateAssetFieldValueDto> { dto }, CancellationToken.None);

        Assert.True(result);
        fieldValueRepository.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<AssetFieldValueEntity>>(entities =>
                entities.Count() == 1 &&
                entities.First().AssetId == 10 &&
                entities.First().FieldDefinitionId == 5 &&
                entities.First().FieldName == "Color" &&
                entities.First().FieldValue == "Red"),
            It.IsAny<CancellationToken>()), Times.Once);

        fieldValueRepository.Verify(r => r.UpdateAsync(It.IsAny<AssetFieldValueEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkSaveFieldValuesAsync_UpdatesExistingFieldValue_WhenFieldDefinitionIdMatches()
    {
        var service = CreateService(out var fieldValueRepository, out var unitOfWork);

        var existing = new AssetFieldValueEntity
        {
            Id = 1,
            AssetId = 10,
            FieldDefinitionId = 5,
            FieldName = "Color",
            FieldValue = "Red"
        };

        fieldValueRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetFieldValueEntity> { existing }.BuildMockDbSet().Object);

        var dto = new CreateAssetFieldValueDto
        {
            AssetId = 10,
            FieldDefinitionId = 5,
            FieldName = "Colour",
            FieldValue = "Blue"
        };

        var result = await service.BulkSaveFieldValuesAsync(10, new List<CreateAssetFieldValueDto> { dto }, CancellationToken.None);

        Assert.True(result);
        fieldValueRepository.Verify(r => r.UpdateAsync(
            It.Is<AssetFieldValueEntity>(e =>
                e.FieldDefinitionId == 5 &&
                e.FieldName == "Colour" &&
                e.FieldValue == "Blue"),
            It.IsAny<CancellationToken>()), Times.Once);

        fieldValueRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<AssetFieldValueEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkSaveFieldValuesAsync_WrapsInTransaction()
    {
        var service = CreateService(out var fieldValueRepository, out var unitOfWork);

        fieldValueRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetFieldValueEntity>().BuildMockDbSet().Object);

        var dto = new CreateAssetFieldValueDto { AssetId = 10, FieldDefinitionId = 1, FieldName = "F", FieldValue = "V" };

        var result = await service.BulkSaveFieldValuesAsync(10, new List<CreateAssetFieldValueDto> { dto }, CancellationToken.None);

        Assert.True(result);
        unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkSaveFieldValuesAsync_OnPersistenceFailure_ReturnsFalse_DoesNotThrow()
    {
        var service = CreateService(out var fieldValueRepository, out var unitOfWork);

        fieldValueRepository.Setup(r => r.GetQueryable())
            .Returns(new List<AssetFieldValueEntity>().BuildMockDbSet().Object);

        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated DB failure"));

        var dto = new CreateAssetFieldValueDto { AssetId = 10, FieldDefinitionId = 1, FieldName = "F", FieldValue = "V" };

        // This is a deliberate contract, not an accidental catch-all: the source method
        // wraps persistence in try/catch, logs the error, rolls back, and returns false —
        // callers that only check the boolean must be able to rely on that behavior.
        var result = await service.BulkSaveFieldValuesAsync(10, new List<CreateAssetFieldValueDto> { dto }, CancellationToken.None);

        Assert.False(result);
        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
