using AutoMapper;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management.AssetNatureFactorCVMaster;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Service-level tests for <see cref="AssetNatureFactorCVService"/> covering the
/// ConstructionTypeId/YearRangeCVId duplicate-combination checks on both Create and Update,
/// plus the reference-validation gate on deactivation.
/// </summary>
public class AssetNatureFactorCVServiceTests
{
    private readonly Mock<IRepository<AssetNatureFactorCVMasterEntity, int>> _mockRepository;
    private readonly Mock<IRepository<ConstructionTypeEntity, int>> _mockConstructionTypeRepository;
    private readonly Mock<IRepository<AssetAssessmentYearRangeMasterCVEntity, int>> _mockYearRangeRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly IMapper _mapper;
    private readonly AssetNatureFactorCVService _service;

    public AssetNatureFactorCVServiceTests()
    {
        _mockRepository = new Mock<IRepository<AssetNatureFactorCVMasterEntity, int>>();
        _mockConstructionTypeRepository = new Mock<IRepository<ConstructionTypeEntity, int>>();
        _mockYearRangeRepository = new Mock<IRepository<AssetAssessmentYearRangeMasterCVEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetNatureFactorCVMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var constructionTypes = new List<ConstructionTypeEntity>
        {
            new() { Id = 1, ConstructionCode = "A", Description = "Type A", IsActive = true }
        }.BuildMockDbSet();
        _mockConstructionTypeRepository.Setup(r => r.GetQueryable()).Returns(constructionTypes.Object);

        var yearRanges = new List<AssetAssessmentYearRangeMasterCVEntity>
        {
            new() { Id = 1, FromYear = 2000, ToYear = 2020, IsActive = true }
        }.BuildMockDbSet();
        _mockYearRangeRepository.Setup(r => r.GetQueryable()).Returns(yearRanges.Object);

        _service = new AssetNatureFactorCVService(
            _mockRepository.Object,
            _mockConstructionTypeRepository.Object,
            _mockYearRangeRepository.Object,
            _mockUnitOfWork.Object,
            _mapper,
            _mockReferenceValidator.Object);
    }

    private void SetupExistingRows(params AssetNatureFactorCVMasterEntity[] rows)
    {
        var mockQuery = rows.ToList().BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);
    }

    #region Create - Duplicate Combination Validation

    [Fact]
    public async Task CreateAsync_WithUniqueCombination_Succeeds()
    {
        SetupExistingRows();
        var createDto = new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 1.0m, YearRangeCVId = 1 };

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.ConstructionTypeId);
        Assert.Equal(1, result.YearRangeCVId);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCombination_ThrowsValidationException()
    {
        SetupExistingRows(new AssetNatureFactorCVMasterEntity
        {
            Id = 1,
            ConstructionTypeId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        });

        var createDto = new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 2.0m, YearRangeCVId = 1 };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains("NatureFactorCV_Combination_Duplicate", ex.Errors.Values);
    }

    #endregion

    #region Update - Duplicate Combination Validation (excluding self)

    [Fact]
    public async Task UpdateAsync_WithUniqueCombination_Succeeds()
    {
        var existing = new AssetNatureFactorCVMasterEntity
        {
            Id = 1,
            ConstructionTypeId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var updateDto = new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 1.5m, YearRangeCVId = 1, IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1.5m, result!.Factor);
    }

    [Fact]
    public async Task UpdateAsync_WithCombinationClashingAgainstAnotherRecord_ThrowsValidationException()
    {
        var recordBeingUpdated = new AssetNatureFactorCVMasterEntity
        {
            Id = 1,
            ConstructionTypeId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        var otherRecord = new AssetNatureFactorCVMasterEntity
        {
            Id = 2,
            ConstructionTypeId = 1,
            Factor = 2.0m,
            YearRangeCVId = 2,
            IsActive = true
        };
        SetupExistingRows(recordBeingUpdated, otherRecord);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(recordBeingUpdated);

        // The year-range repository must also see YearRangeCVId = 2 as an active, valid FK target.
        var yearRanges = new List<AssetAssessmentYearRangeMasterCVEntity>
        {
            new() { Id = 1, FromYear = 2000, ToYear = 2020, IsActive = true },
            new() { Id = 2, FromYear = 2021, ToYear = 2030, IsActive = true }
        }.BuildMockDbSet();
        _mockYearRangeRepository.Setup(r => r.GetQueryable()).Returns(yearRanges.Object);

        // Attempt to change record 1's YearRangeCVId so it collides with record 2's combination
        var updateDto = new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 1.5m, YearRangeCVId = 2, IsActive = true };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains("NatureFactorCV_Combination_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task UpdateAsync_KeepingOwnCombinationUnchanged_DoesNotThrowDuplicateException()
    {
        var existing = new AssetNatureFactorCVMasterEntity
        {
            Id = 1,
            ConstructionTypeId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        // Same ConstructionTypeId/YearRangeCVId as itself — only Factor changes.
        // This also proves the duplicate query excludes the current record ID (x.Id != id).
        var updateDto = new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 1.25m, YearRangeCVId = 1, IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1.25m, result!.Factor);
    }

    #endregion

    #region Update - Deactivation Reference Validation

    [Fact]
    public async Task UpdateAsync_DeactivatingUnreferencedRecord_Succeeds()
    {
        var existing = new AssetNatureFactorCVMasterEntity
        {
            Id = 1,
            ConstructionTypeId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<AssetNatureFactorCVMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        var updateDto = new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 1.0m, YearRangeCVId = 1, IsActive = false };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
        _mockReferenceValidator.Verify(
            v => v.ValidateReferencesAsync<AssetNatureFactorCVMasterEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingReferencedRecord_ThrowsValidationException()
    {
        var existing = new AssetNatureFactorCVMasterEntity
        {
            Id = 1,
            ConstructionTypeId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<AssetNatureFactorCVMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Id", "Cannot deactivate - record is referenced by other entities"));

        var updateDto = new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 1.0m, YearRangeCVId = 1, IsActive = false };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WithCancellationToken_PropagatesTokenToRepository()
    {
        var existing = new AssetNatureFactorCVMasterEntity
        {
            Id = 1,
            ConstructionTypeId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        using var cts = new CancellationTokenSource();
        var updateDto = new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 1.5m, YearRangeCVId = 1, IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, cts.Token);

        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, cts.Token), Times.Once);
    }

    #endregion
}
