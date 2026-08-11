using AutoMapper;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management.AssetUseFactorCVMaster;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Service-level tests for <see cref="AssetUseFactorCVService"/> covering the
/// TypeOfUseId/SubTypeOfUseId/YearRangeCVId duplicate-combination checks on both Create and
/// Update, plus the reference-validation gate on deactivation.
/// </summary>
public class AssetUseFactorCVServiceTests
{
    private readonly Mock<IRepository<AssetUseFactorCVMasterEntity, int>> _mockRepository;
    private readonly Mock<IRepository<AssetTypeOfUseMasterEntity, int>> _mockTypeOfUseRepository;
    private readonly Mock<IRepository<AssetSubTypeOfUseEntity, int>> _mockSubTypeOfUseRepository;
    private readonly Mock<IRepository<AssetAssessmentYearRangeMasterCVEntity, int>> _mockYearRangeRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly IMapper _mapper;
    private readonly AssetUseFactorCVService _service;

    public AssetUseFactorCVServiceTests()
    {
        _mockRepository = new Mock<IRepository<AssetUseFactorCVMasterEntity, int>>();
        _mockTypeOfUseRepository = new Mock<IRepository<AssetTypeOfUseMasterEntity, int>>();
        _mockSubTypeOfUseRepository = new Mock<IRepository<AssetSubTypeOfUseEntity, int>>();
        _mockYearRangeRepository = new Mock<IRepository<AssetAssessmentYearRangeMasterCVEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetUseFactorCVMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var typesOfUse = new List<AssetTypeOfUseMasterEntity>
        {
            new() { Id = 1, TypeOfUseCode = "RES", IsActive = true }
        }.BuildMockDbSet();
        _mockTypeOfUseRepository.Setup(r => r.GetQueryable()).Returns(typesOfUse.Object);

        var subTypesOfUse = new List<AssetSubTypeOfUseEntity>
        {
            new() { Id = 1, TypeOfUseId = 1, Description = "Sub A", IsActive = true },
            new() { Id = 2, TypeOfUseId = 1, Description = "Sub B", IsActive = true }
        }.BuildMockDbSet();
        _mockSubTypeOfUseRepository.Setup(r => r.GetQueryable()).Returns(subTypesOfUse.Object);

        var yearRanges = new List<AssetAssessmentYearRangeMasterCVEntity>
        {
            new() { Id = 1, FromYear = 2000, ToYear = 2020, IsActive = true }
        }.BuildMockDbSet();
        _mockYearRangeRepository.Setup(r => r.GetQueryable()).Returns(yearRanges.Object);

        _service = new AssetUseFactorCVService(
            _mockRepository.Object,
            _mockTypeOfUseRepository.Object,
            _mockSubTypeOfUseRepository.Object,
            _mockYearRangeRepository.Object,
            _mockUnitOfWork.Object,
            _mapper,
            _mockReferenceValidator.Object);
    }

    private void SetupExistingRows(params AssetUseFactorCVMasterEntity[] rows)
    {
        var mockQuery = rows.ToList().BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);
    }

    #region GetAllAsync - TypeOfUse/SubTypeOfUse Description Enrichment

    [Fact]
    public async Task GetAllAsync_WithMatchingTypeAndSubType_PopulatesBothDescriptions()
    {
        var typesOfUse = new List<AssetTypeOfUseMasterEntity>
        {
            new() { Id = 1, TypeOfUseCode = "RES", Description = "Residential", IsActive = true }
        }.BuildMockDbSet();
        _mockTypeOfUseRepository.Setup(r => r.GetQueryable()).Returns(typesOfUse.Object);

        SetupExistingRows(new AssetUseFactorCVMasterEntity
        {
            Id = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        });

        var result = await _service.GetAllAsync(new AssetUseFactorCVMasterQueryParameters(), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.Equal("Residential", item.TypeOfUseDescription);
        Assert.Equal("Sub A", item.SubTypeOfUseDescription);
    }

    [Fact]
    public async Task GetAllAsync_WithNoMatchingTypeOrSubType_DescriptionsAreEmpty()
    {
        // TypeOfUseId 999 / SubTypeOfUseId 999 don't exist - both LEFT JOINs must not throw and
        // must leave the corresponding description empty.
        SetupExistingRows(new AssetUseFactorCVMasterEntity
        {
            Id = 1,
            TypeOfUseId = 999,
            SubTypeOfUseId = 999,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        });

        var result = await _service.GetAllAsync(new AssetUseFactorCVMasterQueryParameters(), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(string.Empty, item.TypeOfUseDescription);
        Assert.Equal(string.Empty, item.SubTypeOfUseDescription);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPageAndTotalCount()
    {
        var rows = Enumerable.Range(1, 15)
            .Select(i => new AssetUseFactorCVMasterEntity { Id = i, TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.0m, YearRangeCVId = 1, IsActive = true })
            .ToArray();
        SetupExistingRows(rows);

        var result = await _service.GetAllAsync(
            new AssetUseFactorCVMasterQueryParameters { PageNumber = 2, PageSize = 10 }, CancellationToken.None);

        Assert.Equal(15, result.TotalCount);
        Assert.Equal(5, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
        Assert.All(result.Items, item => Assert.Equal("Sub A", item.SubTypeOfUseDescription));
    }

    [Fact]
    public async Task GetAllAsync_WithTypeOfUseDescriptionNull_ReturnsEmptyStringNotNull()
    {
        // AssetTypeOfUseMasterEntity.Description is a nullable string - a matched row whose
        // Description is null must still coalesce to string.Empty rather than leaking null into the DTO.
        var typesOfUse = new List<AssetTypeOfUseMasterEntity>
        {
            new() { Id = 1, TypeOfUseCode = "RES", Description = null, IsActive = true }
        }.BuildMockDbSet();
        _mockTypeOfUseRepository.Setup(r => r.GetQueryable()).Returns(typesOfUse.Object);

        SetupExistingRows(new AssetUseFactorCVMasterEntity
        {
            Id = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        });

        var result = await _service.GetAllAsync(new AssetUseFactorCVMasterQueryParameters(), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(string.Empty, item.TypeOfUseDescription);
    }

    [Fact]
    public async Task GetAllAsync_WithTypeOfUseIdFilter_ReturnsFilteredResults()
    {
        var typesOfUse = new List<AssetTypeOfUseMasterEntity>
        {
            new() { Id = 1, TypeOfUseCode = "RES", Description = "Residential", IsActive = true },
            new() { Id = 2, TypeOfUseCode = "COM", Description = "Commercial", IsActive = true }
        }.BuildMockDbSet();
        _mockTypeOfUseRepository.Setup(r => r.GetQueryable()).Returns(typesOfUse.Object);

        SetupExistingRows(
            new AssetUseFactorCVMasterEntity { Id = 1, TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.0m, YearRangeCVId = 1, IsActive = true },
            new AssetUseFactorCVMasterEntity { Id = 2, TypeOfUseId = 2, SubTypeOfUseId = 1, Factor = 1.0m, YearRangeCVId = 1, IsActive = true });

        var result = await _service.GetAllAsync(
            new AssetUseFactorCVMasterQueryParameters { TypeOfUseId = 1 }, CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(1, item.Id);
        Assert.Equal("Residential", item.TypeOfUseDescription);
    }

    #endregion

    #region Create - Duplicate Combination Validation

    [Fact]
    public async Task CreateAsync_WithUniqueCombination_Succeeds()
    {
        SetupExistingRows();
        var createDto = new CreateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.0m, YearRangeCVId = 1 };

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.TypeOfUseId);
        Assert.Equal(1, result.SubTypeOfUseId);
        Assert.Equal(1, result.YearRangeCVId);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCombination_ThrowsValidationException()
    {
        SetupExistingRows(new AssetUseFactorCVMasterEntity
        {
            Id = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        });

        var createDto = new CreateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 2.0m, YearRangeCVId = 1 };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains("UseFactorCV_Combination_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentTypeOfUseId_ThrowsValidationException()
    {
        SetupExistingRows();
        var createDto = new CreateAssetUseFactorCVMasterDto { TypeOfUseId = 999, SubTypeOfUseId = 1, Factor = 1.0m, YearRangeCVId = 1 };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains(nameof(CreateAssetUseFactorCVMasterDto.TypeOfUseId), ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentSubTypeOfUseId_ThrowsValidationException()
    {
        SetupExistingRows();
        var createDto = new CreateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 999, Factor = 1.0m, YearRangeCVId = 1 };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains(nameof(CreateAssetUseFactorCVMasterDto.SubTypeOfUseId), ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_WithSubTypeOfUseBelongingToDifferentTypeOfUse_ThrowsValidationException()
    {
        // TypeOfUseId 2 exists and is active, and SubTypeOfUseId 1 exists and is active — but
        // SubTypeOfUseId 1 belongs to TypeOfUseId 1 (see constructor setup), not 2. Both FKs are
        // independently valid, so only the TypeOfUseId/SubTypeOfUseId relationship check should catch this.
        SetupExistingRows();
        var typesOfUse = new List<AssetTypeOfUseMasterEntity>
        {
            new() { Id = 1, TypeOfUseCode = "RES", IsActive = true },
            new() { Id = 2, TypeOfUseCode = "COM", IsActive = true }
        }.BuildMockDbSet();
        _mockTypeOfUseRepository.Setup(r => r.GetQueryable()).Returns(typesOfUse.Object);

        var createDto = new CreateAssetUseFactorCVMasterDto { TypeOfUseId = 2, SubTypeOfUseId = 1, Factor = 1.0m, YearRangeCVId = 1 };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains(nameof(CreateAssetUseFactorCVMasterDto.SubTypeOfUseId), ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentYearRangeCVId_ThrowsValidationException()
    {
        SetupExistingRows();
        var createDto = new CreateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.0m, YearRangeCVId = 999 };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains(nameof(CreateAssetUseFactorCVMasterDto.YearRangeCVId), ex.Errors.Keys);
    }

    #endregion

    #region Update - Duplicate Combination Validation (excluding self)

    [Fact]
    public async Task UpdateAsync_WithUniqueCombination_Succeeds()
    {
        var existing = new AssetUseFactorCVMasterEntity
        {
            Id = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var updateDto = new UpdateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.5m, YearRangeCVId = 1, IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1.5m, result!.Factor);
    }

    [Fact]
    public async Task UpdateAsync_WithCombinationClashingAgainstAnotherRecord_ThrowsValidationException()
    {
        var recordBeingUpdated = new AssetUseFactorCVMasterEntity
        {
            Id = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        var otherRecord = new AssetUseFactorCVMasterEntity
        {
            Id = 2,
            TypeOfUseId = 1,
            SubTypeOfUseId = 2,
            Factor = 2.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        SetupExistingRows(recordBeingUpdated, otherRecord);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(recordBeingUpdated);

        // Attempt to change record 1's SubTypeOfUseId so it collides with record 2's combination
        var updateDto = new UpdateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 2, Factor = 1.5m, YearRangeCVId = 1, IsActive = true };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains("UseFactorCV_Combination_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task UpdateAsync_KeepingOwnCombinationUnchanged_DoesNotThrowDuplicateException()
    {
        var existing = new AssetUseFactorCVMasterEntity
        {
            Id = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        // Same TypeOfUseId/SubTypeOfUseId/YearRangeCVId as itself — only Factor changes.
        // This also proves the duplicate query excludes the current record ID (x.Id != id).
        var updateDto = new UpdateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.25m, YearRangeCVId = 1, IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1.25m, result!.Factor);
    }

    #endregion

    #region Update - Deactivation Reference Validation

    [Fact]
    public async Task UpdateAsync_DeactivatingUnreferencedRecord_Succeeds()
    {
        var existing = new AssetUseFactorCVMasterEntity
        {
            Id = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<AssetUseFactorCVMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        var updateDto = new UpdateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.0m, YearRangeCVId = 1, IsActive = false };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
        _mockReferenceValidator.Verify(
            v => v.ValidateReferencesAsync<AssetUseFactorCVMasterEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingReferencedRecord_ThrowsValidationException()
    {
        var existing = new AssetUseFactorCVMasterEntity
        {
            Id = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<AssetUseFactorCVMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Id", "Cannot deactivate - record is referenced by other entities"));

        var updateDto = new UpdateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.0m, YearRangeCVId = 1, IsActive = false };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WithCancellationToken_PropagatesTokenToRepository()
    {
        var existing = new AssetUseFactorCVMasterEntity
        {
            Id = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        using var cts = new CancellationTokenSource();
        var updateDto = new UpdateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.5m, YearRangeCVId = 1, IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, cts.Token);

        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, cts.Token), Times.Once);
    }

    #endregion
}
