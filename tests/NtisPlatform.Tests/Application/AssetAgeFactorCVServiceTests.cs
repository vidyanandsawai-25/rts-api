using AutoMapper;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management.AssetAgeFactorCVMaster;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Service-level tests for <see cref="AssetAgeFactorCVService"/> covering the AgeFrom/AgeTo
/// range validation and duplicate-combination checks on both Create and Update.
/// </summary>
public class AssetAgeFactorCVServiceTests
{
    private readonly Mock<IRepository<AssetAgeFactorCVMasterEntity, int>> _mockRepository;
    private readonly Mock<IRepository<ConstructionTypeEntity, int>> _mockConstructionTypeRepository;
    private readonly Mock<IRepository<AssetAssessmentYearRangeMasterCVEntity, int>> _mockYearRangeRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly IMapper _mapper;
    private readonly AssetAgeFactorCVService _service;

    public AssetAgeFactorCVServiceTests()
    {
        _mockRepository = new Mock<IRepository<AssetAgeFactorCVMasterEntity, int>>();
        _mockConstructionTypeRepository = new Mock<IRepository<ConstructionTypeEntity, int>>();
        _mockYearRangeRepository = new Mock<IRepository<AssetAssessmentYearRangeMasterCVEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetAgeFactorCVMappingProfile>();
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

        _service = new AssetAgeFactorCVService(
            _mockRepository.Object,
            _mockConstructionTypeRepository.Object,
            _mockYearRangeRepository.Object,
            _mockUnitOfWork.Object,
            _mapper,
            _mockReferenceValidator.Object);
    }

    private void SetupExistingRows(params AssetAgeFactorCVMasterEntity[] rows)
    {
        var mockQuery = rows.ToList().BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);
    }

    #region GetAllAsync - ConstructionTypeDescription Enrichment

    [Fact]
    public async Task GetAllAsync_WithMatchingConstructionType_PopulatesConstructionTypeDescription()
    {
        // ConstructionType Id=1/"Type A" is already wired up in the constructor's default setup.
        SetupExistingRows(new AssetAgeFactorCVMasterEntity
        {
            Id = 1,
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        });

        var result = await _service.GetAllAsync(new AssetAgeFactorCVMasterQueryParameters(), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.Equal("Type A", item.ConstructionTypeDescription);
    }

    [Fact]
    public async Task GetAllAsync_WithNoMatchingConstructionType_ConstructionTypeDescriptionIsEmpty()
    {
        // ConstructionTypeId 999 doesn't exist in the construction-type repository - the LEFT JOIN
        // must not throw and must leave ConstructionTypeDescription empty.
        SetupExistingRows(new AssetAgeFactorCVMasterEntity
        {
            Id = 1,
            ConstructionTypeId = 999,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        });

        var result = await _service.GetAllAsync(new AssetAgeFactorCVMasterQueryParameters(), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(string.Empty, item.ConstructionTypeDescription);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPageAndTotalCount()
    {
        var rows = Enumerable.Range(1, 15)
            .Select(i => new AssetAgeFactorCVMasterEntity { Id = i, ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1, IsActive = true })
            .ToArray();
        SetupExistingRows(rows);

        var result = await _service.GetAllAsync(
            new AssetAgeFactorCVMasterQueryParameters { PageNumber = 2, PageSize = 10 }, CancellationToken.None);

        Assert.Equal(15, result.TotalCount);
        Assert.Equal(5, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
        Assert.All(result.Items, item => Assert.Equal("Type A", item.ConstructionTypeDescription));
    }

    [Fact]
    public async Task GetAllAsync_WithConstructionTypeDescriptionNull_ReturnsEmptyStringNotNull()
    {
        // ConstructionTypeEntity.Description is a nullable string - a matched row whose Description
        // is null must still coalesce to string.Empty rather than leaking null into the DTO.
        var constructionTypes = new List<ConstructionTypeEntity>
        {
            new() { Id = 1, ConstructionCode = "A", Description = null, IsActive = true }
        }.BuildMockDbSet();
        _mockConstructionTypeRepository.Setup(r => r.GetQueryable()).Returns(constructionTypes.Object);

        SetupExistingRows(new AssetAgeFactorCVMasterEntity
        {
            Id = 1,
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        });

        var result = await _service.GetAllAsync(new AssetAgeFactorCVMasterQueryParameters(), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(string.Empty, item.ConstructionTypeDescription);
    }

    [Fact]
    public async Task GetAllAsync_WithConstructionTypeIdFilter_ReturnsFilteredResults()
    {
        SetupExistingRows(
            new AssetAgeFactorCVMasterEntity { Id = 1, ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1, IsActive = true },
            new AssetAgeFactorCVMasterEntity { Id = 2, ConstructionTypeId = 2, AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1, IsActive = true });

        var result = await _service.GetAllAsync(
            new AssetAgeFactorCVMasterQueryParameters { ConstructionTypeId = 1 }, CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(1, item.Id);
    }

    #endregion

    #region Create - Age Range Validation

    [Fact]
    public async Task CreateAsync_WithAgeFromLessThanAgeTo_Succeeds()
    {
        SetupExistingRows();
        var createDto = new CreateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1 };

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, result.AgeFrom);
        Assert.Equal(5, result.AgeTo);
    }

    [Fact]
    public async Task CreateAsync_WithAgeFromEqualToAgeTo_Succeeds()
    {
        SetupExistingRows();
        var createDto = new CreateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 5, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1 };

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(5, result.AgeFrom);
        Assert.Equal(5, result.AgeTo);
    }

    [Fact]
    public async Task CreateAsync_WithAgeFromGreaterThanAgeTo_ThrowsValidationException()
    {
        SetupExistingRows();
        var createDto = new CreateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 10, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1 };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains("AgeFactorCV_AgeRange_Invalid", ex.Errors.Values);
    }

    #endregion

    #region Create - Duplicate Combination Validation

    [Fact]
    public async Task CreateAsync_WithDuplicateCombination_ThrowsValidationException()
    {
        SetupExistingRows(new AssetAgeFactorCVMasterEntity
        {
            Id = 1,
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        });

        var createDto = new CreateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 2.0m, YearRangeCVId = 1 };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains("AgeFactorCV_Combination_Duplicate", ex.Errors.Values);
    }

    #endregion

    #region Update - Age Range Validation

    [Fact]
    public async Task UpdateAsync_WithAgeFromGreaterThanAgeTo_ThrowsValidationException()
    {
        var existing = new AssetAgeFactorCVMasterEntity
        {
            Id = 1,
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var updateDto = new UpdateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 10, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1, IsActive = true };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains("AgeFactorCV_AgeRange_Invalid", ex.Errors.Values);
    }

    #endregion

    #region Update - Duplicate Combination Validation (excluding self)

    [Fact]
    public async Task UpdateAsync_WithCombinationClashingAgainstAnotherRecord_ThrowsValidationException()
    {
        var recordBeingUpdated = new AssetAgeFactorCVMasterEntity
        {
            Id = 1,
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        var otherRecord = new AssetAgeFactorCVMasterEntity
        {
            Id = 2,
            ConstructionTypeId = 1,
            AgeFrom = 6,
            AgeTo = 10,
            Factor = 2.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        SetupExistingRows(recordBeingUpdated, otherRecord);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(recordBeingUpdated);

        // Attempt to change record 1's range so it collides with record 2's combination
        var updateDto = new UpdateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 6, AgeTo = 10, Factor = 1.5m, YearRangeCVId = 1, IsActive = true };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains("AgeFactorCV_Combination_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task UpdateAsync_KeepingOwnCombinationUnchanged_DoesNotThrowDuplicateException()
    {
        var existing = new AssetAgeFactorCVMasterEntity
        {
            Id = 1,
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };
        SetupExistingRows(existing);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        // Same ConstructionTypeId/AgeFrom/AgeTo/YearRangeCVId as itself — only Factor changes
        var updateDto = new UpdateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.25m, YearRangeCVId = 1, IsActive = true };

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1.25m, result!.Factor);
    }

    #endregion
}
