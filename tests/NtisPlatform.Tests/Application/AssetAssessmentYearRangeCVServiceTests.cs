using AutoMapper;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management.AssetAssessmentYearRangeMasterCV;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Service-level tests for <see cref="AssetAssessmentYearRangeCVService"/> covering the
/// FromYear/ToYear range validation and duplicate-range checks on both Create and Update,
/// plus the reference-validation gate on deactivation.
/// </summary>
public class AssetAssessmentYearRangeCVServiceTests
{
    private readonly Mock<IRepository<AssetAssessmentYearRangeMasterCVEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly IMapper _mapper;
    private readonly AssetAssessmentYearRangeCVService _service;

    public AssetAssessmentYearRangeCVServiceTests()
    {
        _mockRepository = new Mock<IRepository<AssetAssessmentYearRangeMasterCVEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetAssessmentYearRangeCVMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new AssetAssessmentYearRangeCVService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mapper,
            _mockReferenceValidator.Object);
    }

    private void SetupExistingRows(params AssetAssessmentYearRangeMasterCVEntity[] rows)
    {
        var mockQuery = rows.ToList().BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);
    }

    #region Create - Year Range Validation

    [Fact]
    public async Task CreateAsync_WithFromYearLessThanToYear_Succeeds()
    {
        SetupExistingRows();
        var createDto = new CreateAssetAssessmentYearRangeMasterCVDto { FromYear = 2000, ToYear = 2005 };

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2000, result.FromYear);
        Assert.Equal(2005, result.ToYear);
    }

    [Fact]
    public async Task CreateAsync_WithFromYearEqualToToYear_Succeeds()
    {
        SetupExistingRows();
        var createDto = new CreateAssetAssessmentYearRangeMasterCVDto { FromYear = 2005, ToYear = 2005 };

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2005, result.FromYear);
        Assert.Equal(2005, result.ToYear);
    }

    [Fact]
    public async Task CreateAsync_WithFromYearGreaterThanToYear_ThrowsValidationException()
    {
        SetupExistingRows();
        var createDto = new CreateAssetAssessmentYearRangeMasterCVDto { FromYear = 2010, ToYear = 2005 };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains("AssessmentYearRangeCV_ToYear_BeforeFromYear", ex.Errors.Values);
    }

    #endregion

    #region Create - Duplicate Range Validation

    [Fact]
    public async Task CreateAsync_WithDuplicateRange_ThrowsValidationException()
    {
        SetupExistingRows(new AssetAssessmentYearRangeMasterCVEntity
        {
            Id = 1,
            FromYear = 2000,
            ToYear = 2005,
            IsActive = true
        });

        var createDto = new CreateAssetAssessmentYearRangeMasterCVDto { FromYear = 2000, ToYear = 2005 };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains("AssessmentYearRangeCV_Range_Duplicate", ex.Errors.Values);
    }

    #endregion

    #region Update - Year Range Validation

    [Fact]
    public async Task UpdateAsync_WithFromYearLessThanToYear_Succeeds()
    {
        var existing = new AssetAssessmentYearRangeMasterCVEntity { Id = 1, FromYear = 2000, ToYear = 2005, IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var updateDto = new UpdateAssetAssessmentYearRangeMasterCVDto { FromYear = 2001, ToYear = 2006, IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2001, result!.FromYear);
        Assert.Equal(2006, result.ToYear);
    }

    [Fact]
    public async Task UpdateAsync_WithFromYearEqualToToYear_Succeeds()
    {
        var existing = new AssetAssessmentYearRangeMasterCVEntity { Id = 1, FromYear = 2000, ToYear = 2005, IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var updateDto = new UpdateAssetAssessmentYearRangeMasterCVDto { FromYear = 2007, ToYear = 2007, IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2007, result!.FromYear);
        Assert.Equal(2007, result.ToYear);
    }

    [Fact]
    public async Task UpdateAsync_WithFromYearGreaterThanToYear_ThrowsValidationException()
    {
        var existing = new AssetAssessmentYearRangeMasterCVEntity { Id = 1, FromYear = 2000, ToYear = 2005, IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var updateDto = new UpdateAssetAssessmentYearRangeMasterCVDto { FromYear = 2010, ToYear = 2005, IsActive = true };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains("AssessmentYearRangeCV_ToYear_BeforeFromYear", ex.Errors.Values);
    }

    #endregion

    #region Update - Duplicate Range Validation (excluding self)

    [Fact]
    public async Task UpdateAsync_WithRangeClashingAgainstAnotherRecord_ThrowsValidationException()
    {
        var recordBeingUpdated = new AssetAssessmentYearRangeMasterCVEntity { Id = 1, FromYear = 2000, ToYear = 2005, IsActive = true };
        var otherRecord = new AssetAssessmentYearRangeMasterCVEntity { Id = 2, FromYear = 2006, ToYear = 2010, IsActive = true };
        SetupExistingRows(recordBeingUpdated, otherRecord);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(recordBeingUpdated);

        // Attempt to change record 1's range so it collides with record 2's range
        var updateDto = new UpdateAssetAssessmentYearRangeMasterCVDto { FromYear = 2006, ToYear = 2010, IsActive = true };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains("AssessmentYearRangeCV_Range_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task UpdateAsync_KeepingOwnRangeUnchanged_DoesNotThrowDuplicateException()
    {
        var existing = new AssetAssessmentYearRangeMasterCVEntity { Id = 1, FromYear = 2000, ToYear = 2005, IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        // Same FromYear/ToYear as itself — the duplicate check must exclude the current ID
        var updateDto = new UpdateAssetAssessmentYearRangeMasterCVDto { FromYear = 2000, ToYear = 2005, IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2000, result!.FromYear);
        Assert.Equal(2005, result.ToYear);
    }

    #endregion

    #region Update - Deactivation Reference Validation

    [Fact]
    public async Task UpdateAsync_DeactivatingUnreferencedRecord_Succeeds()
    {
        var existing = new AssetAssessmentYearRangeMasterCVEntity { Id = 1, FromYear = 2000, ToYear = 2005, IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<AssetAssessmentYearRangeMasterCVEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        var updateDto = new UpdateAssetAssessmentYearRangeMasterCVDto { FromYear = 2000, ToYear = 2005, IsActive = false };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
        _mockReferenceValidator.Verify(
            v => v.ValidateReferencesAsync<AssetAssessmentYearRangeMasterCVEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingReferencedRecord_ThrowsValidationException()
    {
        var existing = new AssetAssessmentYearRangeMasterCVEntity { Id = 1, FromYear = 2000, ToYear = 2005, IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<AssetAssessmentYearRangeMasterCVEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Id", "Cannot deactivate - record is referenced by other entities"));

        var updateDto = new UpdateAssetAssessmentYearRangeMasterCVDto { FromYear = 2000, ToYear = 2005, IsActive = false };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WithCancellationToken_PropagatesTokenToDuplicateQuery()
    {
        var existing = new AssetAssessmentYearRangeMasterCVEntity { Id = 1, FromYear = 2000, ToYear = 2005, IsActive = true };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        using var cts = new CancellationTokenSource();
        var updateDto = new UpdateAssetAssessmentYearRangeMasterCVDto { FromYear = 2001, ToYear = 2006, IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, cts.Token);

        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, cts.Token), Times.Once);
    }

    #endregion
}
