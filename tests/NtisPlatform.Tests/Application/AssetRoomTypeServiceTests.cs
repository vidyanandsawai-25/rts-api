using AutoMapper;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Master.AssetRoomType;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Service-level tests for <see cref="AssetRoomTypeService"/> covering the AssetTypeId-scoped
/// RoomTypeName/RoomTypeCode duplicate checks — shared between Create and Update via
/// ValidateUniqueRoomTypeAsync — plus the reference-validation gate on deactivation.
/// </summary>
public class AssetRoomTypeServiceTests
{
    private readonly Mock<IRepository<AssetRoomTypeMasterEntity, int>> _mockRepository;
    private readonly Mock<IRepository<AssetTypeEntity, int>> _mockAssetTypeRepository;
    private readonly Mock<IRepository<AssetCategoryEntity, int>> _mockAssetCategoryRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly IMapper _mapper;
    private readonly AssetRoomTypeService _service;

    public AssetRoomTypeServiceTests()
    {
        _mockRepository = new Mock<IRepository<AssetRoomTypeMasterEntity, int>>();
        _mockAssetTypeRepository = new Mock<IRepository<AssetTypeEntity, int>>();
        _mockAssetCategoryRepository = new Mock<IRepository<AssetCategoryEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetRoomTypeMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var assetTypes = new List<AssetTypeEntity>
        {
            new() { Id = 1, AssetCategoryId = 1, TypeCode = "T1", TypeName = "Type One", IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, AssetCategoryId = 1, TypeCode = "T2", TypeName = "Type Two", IsActive = true, MarkedForDeletion = false }
        }.BuildMockDbSet();
        _mockAssetTypeRepository.Setup(r => r.GetQueryable()).Returns(assetTypes.Object);

        var assetCategories = new List<AssetCategoryEntity>
        {
            new() { Id = 1, CategoryCode = "C1", CategoryName = "Category One", IsActive = true, MarkedForDeletion = false }
        }.BuildMockDbSet();
        _mockAssetCategoryRepository.Setup(r => r.GetQueryable()).Returns(assetCategories.Object);

        _service = new AssetRoomTypeService(
            _mockRepository.Object,
            _mockAssetTypeRepository.Object,
            _mockAssetCategoryRepository.Object,
            _mockUnitOfWork.Object,
            _mapper,
            _mockReferenceValidator.Object);
    }

    private void SetupExistingRows(params AssetRoomTypeMasterEntity[] rows)
    {
        var mockQuery = rows.ToList().BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);
    }

    #region Create - Duplicate Name/Code Validation

    [Fact]
    public async Task CreateAsync_WithUniqueNameAndCode_Succeeds()
    {
        SetupExistingRows();
        var createDto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR" };

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Bedroom", result.RoomTypeName);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateNameUnderSameAssetType_ThrowsValidationException()
    {
        SetupExistingRows(new AssetRoomTypeMasterEntity { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR1", IsActive = true });
        var createDto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR2" };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains("AssetRoomType_RoomTypeName_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCodeUnderSameAssetType_ThrowsValidationException()
    {
        SetupExistingRows(new AssetRoomTypeMasterEntity { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR", IsActive = true });
        var createDto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Guest Room", RoomTypeCode = "BR" };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains("AssetRoomType_RoomTypeCode_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task CreateAsync_WithSameNameUnderDifferentAssetType_Succeeds()
    {
        SetupExistingRows(new AssetRoomTypeMasterEntity { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR1", IsActive = true });
        var createDto = new CreateAssetRoomTypeDto { AssetTypeId = 2, RoomTypeName = "Bedroom", RoomTypeCode = "BR2" };

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.AssetTypeId);
    }

    [Fact]
    public async Task CreateAsync_WithSameCodeUnderDifferentAssetType_Succeeds()
    {
        SetupExistingRows(new AssetRoomTypeMasterEntity { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR", IsActive = true });
        var createDto = new CreateAssetRoomTypeDto { AssetTypeId = 2, RoomTypeName = "Guest Room", RoomTypeCode = "BR" };

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("BR", result.RoomTypeCode);
    }

    #endregion

    #region Update - Duplicate Name/Code Validation (excluding self)

    [Fact]
    public async Task UpdateAsync_WithUniqueNameAndCode_Succeeds()
    {
        var existing = new AssetRoomTypeMasterEntity { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR", IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var updateDto = new UpdateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Master Bedroom", RoomTypeCode = "MBR", IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Master Bedroom", result!.RoomTypeName);
    }

    [Fact]
    public async Task UpdateAsync_WithNameClashingAgainstAnotherRecord_ThrowsValidationException()
    {
        var recordBeingUpdated = new AssetRoomTypeMasterEntity { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR", IsActive = true };
        var otherRecord = new AssetRoomTypeMasterEntity { Id = 2, AssetTypeId = 1, RoomTypeName = "Guest Room", RoomTypeCode = "GR", IsActive = true };
        SetupExistingRows(recordBeingUpdated, otherRecord);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(recordBeingUpdated);

        // Attempt to rename record 1 to record 2's name
        var updateDto = new UpdateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Guest Room", RoomTypeCode = "BR", IsActive = true };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains("AssetRoomType_RoomTypeName_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task UpdateAsync_WithCodeClashingAgainstAnotherRecord_ThrowsValidationException()
    {
        var recordBeingUpdated = new AssetRoomTypeMasterEntity { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR", IsActive = true };
        var otherRecord = new AssetRoomTypeMasterEntity { Id = 2, AssetTypeId = 1, RoomTypeName = "Guest Room", RoomTypeCode = "GR", IsActive = true };
        SetupExistingRows(recordBeingUpdated, otherRecord);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(recordBeingUpdated);

        // Attempt to change record 1's code to record 2's code
        var updateDto = new UpdateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "GR", IsActive = true };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains("AssetRoomType_RoomTypeCode_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task UpdateAsync_KeepingOwnNameAndCodeUnchanged_DoesNotThrowDuplicateException()
    {
        var existing = new AssetRoomTypeMasterEntity { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR", IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        // Same AssetTypeId/RoomTypeName/RoomTypeCode as itself — only Description changes.
        // This also proves the duplicate query excludes the current record ID (x.Id != id).
        var updateDto = new UpdateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR", Description = "Updated description", IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Updated description", result!.Description);
    }

    #endregion

    #region Update - Deactivation Reference Validation

    [Fact]
    public async Task UpdateAsync_DeactivatingUnreferencedRecord_Succeeds()
    {
        var existing = new AssetRoomTypeMasterEntity { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR", IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<AssetRoomTypeMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        var updateDto = new UpdateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR", IsActive = false };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
        _mockReferenceValidator.Verify(
            v => v.ValidateReferencesAsync<AssetRoomTypeMasterEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingReferencedRecord_ThrowsValidationException()
    {
        var existing = new AssetRoomTypeMasterEntity { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR", IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<AssetRoomTypeMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Id", "Cannot deactivate - record is referenced by other entities"));

        var updateDto = new UpdateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR", IsActive = false };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WithCancellationToken_PropagatesTokenToRepository()
    {
        var existing = new AssetRoomTypeMasterEntity { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom", RoomTypeCode = "BR", IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        using var cts = new CancellationTokenSource();
        var updateDto = new UpdateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Master Bedroom", RoomTypeCode = "MBR", IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, cts.Token);

        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, cts.Token), Times.Once);
    }

    #endregion
}
