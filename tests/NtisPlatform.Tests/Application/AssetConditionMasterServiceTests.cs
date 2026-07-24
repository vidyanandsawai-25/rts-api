using AutoMapper;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Service-level tests for <see cref="AssetConditionMasterService"/> covering the
/// ConditionCategory/CategoryId/ConditionName duplicate-combination checks on both Create and
/// Update, plus the reference-validation gate on deactivation.
/// </summary>
public class AssetConditionMasterServiceTests
{
    private readonly Mock<IRepository<AssetConditionMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly IMapper _mapper;
    private readonly AssetConditionMasterService _service;

    public AssetConditionMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<AssetConditionMasterEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetConditionMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new AssetConditionMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mapper,
            _mockReferenceValidator.Object);
    }

    private void SetupExistingRows(params AssetConditionMasterEntity[] rows)
    {
        var mockQuery = rows.ToList().BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);
    }

    #region Create - Duplicate Combination Validation

    [Fact]
    public async Task CreateAsync_WithUniqueCombination_Succeeds()
    {
        SetupExistingRows();
        var createDto = new CreateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good" };

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Good", result.ConditionName);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCombination_ThrowsValidationException()
    {
        SetupExistingRows(new AssetConditionMasterEntity { Id = 1, ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good", IsActive = true });
        var createDto = new CreateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good" };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains("AssetConditionMaster_ConditionName_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task CreateAsync_WithSameNameUnderDifferentCategory_Succeeds()
    {
        SetupExistingRows(new AssetConditionMasterEntity { Id = 1, ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good", IsActive = true });
        var createDto = new CreateAssetConditionMasterDto { ConditionCategory = "Inventory", CategoryId = 1, ConditionName = "Good" };

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Inventory", result.ConditionCategory);
    }

    #endregion

    #region Update - Duplicate Combination Validation (excluding self)

    [Fact]
    public async Task UpdateAsync_WithUniqueCombination_Succeeds()
    {
        var existing = new AssetConditionMasterEntity { Id = 1, ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good", IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var updateDto = new UpdateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Excellent", IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Excellent", result!.ConditionName);
    }

    [Fact]
    public async Task UpdateAsync_WithCombinationClashingAgainstAnotherRecord_ThrowsValidationException()
    {
        var recordBeingUpdated = new AssetConditionMasterEntity { Id = 1, ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good", IsActive = true };
        var otherRecord = new AssetConditionMasterEntity { Id = 2, ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Poor", IsActive = true };
        SetupExistingRows(recordBeingUpdated, otherRecord);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(recordBeingUpdated);

        // Attempt to rename record 1 to record 2's (ConditionCategory, CategoryId, ConditionName) combination
        var updateDto = new UpdateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Poor", IsActive = true };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains("AssetConditionMaster_ConditionName_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task UpdateAsync_KeepingOwnCombinationUnchanged_DoesNotThrowDuplicateException()
    {
        var existing = new AssetConditionMasterEntity { Id = 1, ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good", IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        // Same ConditionCategory/CategoryId/ConditionName as itself — only Description changes.
        // This also proves the duplicate query excludes the current record ID (x.Id != id).
        var updateDto = new UpdateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good", Description = "Updated description", IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Updated description", result!.Description);
    }

    #endregion

    #region Update - Deactivation Reference Validation

    [Fact]
    public async Task UpdateAsync_DeactivatingUnreferencedRecord_Succeeds()
    {
        var existing = new AssetConditionMasterEntity { Id = 1, ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good", IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<AssetConditionMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        var updateDto = new UpdateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good", IsActive = false };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
        _mockReferenceValidator.Verify(
            v => v.ValidateReferencesAsync<AssetConditionMasterEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingReferencedRecord_ThrowsValidationException()
    {
        var existing = new AssetConditionMasterEntity { Id = 1, ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good", IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<AssetConditionMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Id", "Cannot deactivate - record is referenced by other entities"));

        var updateDto = new UpdateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good", IsActive = false };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WithCancellationToken_PropagatesTokenToRepository()
    {
        var existing = new AssetConditionMasterEntity { Id = 1, ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good", IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        using var cts = new CancellationTokenSource();
        var updateDto = new UpdateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Excellent", IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, cts.Token);

        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, cts.Token), Times.Once);
    }

    #endregion
}
