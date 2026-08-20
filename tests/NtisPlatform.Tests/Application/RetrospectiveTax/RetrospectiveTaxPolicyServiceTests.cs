using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxPolicy;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using Xunit;
using ValidationResult = NtisPlatform.Application.Models.ValidationResult;

namespace NtisPlatform.Tests.Application.RetrospectiveTax;

public class RetrospectiveTaxPolicyServiceTests
{
    private readonly Mock<IRepository<RetrospectiveTaxPolicyEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly RetrospectiveTaxPolicyService _service;

    public RetrospectiveTaxPolicyServiceTests()
    {
        _mockRepository = new Mock<IRepository<RetrospectiveTaxPolicyEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new RetrospectiveTaxPolicyService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    private static RetrospectiveTaxPolicyEntity CreateEntity(
        int id = 1,
        string percentageMode = "HISTORIC_YEAR_WISE",
        decimal? fixedPercentage = null,
        bool isActive = true)
    {
        return new RetrospectiveTaxPolicyEntity
        {
            Id = id,
            TaxPolicyCode = "TPC001",
            TaxPolicyName = "Standard Policy",
            RateMode = "HISTORIC_YEAR_WISE",
            PercentageMode = percentageMode,
            FixedPercentage = fixedPercentage,
            FinancialYearStartMonth = 4,
            FinancialYearStartDay = 1,
            IsActive = isActive,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };
    }

    private static RetrospectiveTaxPolicyDto MapToDto(RetrospectiveTaxPolicyEntity e)
    {
        return new RetrospectiveTaxPolicyDto
        {
            Id = e.Id,
            TaxPolicyCode = e.TaxPolicyCode,
            TaxPolicyName = e.TaxPolicyName,
            RateMode = e.RateMode,
            PercentageMode = e.PercentageMode,
            FixedPercentage = e.FixedPercentage,
            FinancialYearStartMonth = e.FinancialYearStartMonth,
            FinancialYearStartDay = e.FinancialYearStartDay,
            EffectiveFrom = e.EffectiveFrom,
            EffectiveTo = e.EffectiveTo,
            IsActive = e.IsActive,
            CreatedDate = e.CreatedDate,
            UpdatedDate = e.UpdatedDate
        };
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = CreateEntity();

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RetrospectiveTaxPolicyDto>(It.IsAny<RetrospectiveTaxPolicyEntity>()))
            .Returns((RetrospectiveTaxPolicyEntity e) => MapToDto(e));

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("TPC001", result.TaxPolicyCode);
        Assert.Equal("HISTORIC_YEAR_WISE", result.PercentageMode);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxPolicyEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult()
    {
        // Arrange
        var entities = new List<RetrospectiveTaxPolicyEntity>
        {
            CreateEntity(1, "HISTORIC_YEAR_WISE"),
            CreateEntity(2, "CURRENT_YEAR_FOR_ALL_YEARS")
        };
        entities[1].TaxPolicyCode = "TPC002";

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RetrospectiveTaxPolicyEntity, RetrospectiveTaxPolicyDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new RetrospectiveTaxPolicyService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new RetrospectiveTaxPolicyQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.TaxPolicyCode == "TPC001");
        Assert.Contains(items, x => x.TaxPolicyCode == "TPC002");
    }

    [Fact]
    public async Task CreateAsync_PercentageModeNotFixed_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateRetrospectiveTaxPolicyDto
        {
            TaxPolicyCode = "TPC001",
            TaxPolicyName = "Standard Policy",
            RateMode = "HISTORIC_YEAR_WISE",
            PercentageMode = "HISTORIC_YEAR_WISE",
            FinancialYearStartMonth = 4,
            FinancialYearStartDay = 1,
            EffectiveFrom = new DateTime(2024, 4, 1),
            EffectiveTo = new DateTime(2025, 3, 31),
            CreatedBy = 1,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<RetrospectiveTaxPolicyEntity>(It.IsAny<CreateRetrospectiveTaxPolicyDto>()))
            .Returns((CreateRetrospectiveTaxPolicyDto dto) => new RetrospectiveTaxPolicyEntity
            {
                Id = 1,
                TaxPolicyCode = dto.TaxPolicyCode,
                TaxPolicyName = dto.TaxPolicyName,
                RateMode = dto.RateMode,
                PercentageMode = dto.PercentageMode,
                FixedPercentage = dto.FixedPercentage,
                FinancialYearStartMonth = dto.FinancialYearStartMonth,
                FinancialYearStartDay = dto.FinancialYearStartDay,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxPolicyEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RetrospectiveTaxPolicyDto>(It.IsAny<RetrospectiveTaxPolicyEntity>()))
            .Returns((RetrospectiveTaxPolicyEntity e) => MapToDto(e));

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("TPC001", result.TaxPolicyCode);
        Assert.Equal("HISTORIC_YEAR_WISE", result.PercentageMode);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_FixedPercentageModeWithoutFixedPercentage_ThrowsValidationException()
    {
        // Arrange
        var createDto = new CreateRetrospectiveTaxPolicyDto
        {
            TaxPolicyCode = "TPC001",
            TaxPolicyName = "Standard Policy",
            RateMode = "HISTORIC_YEAR_WISE",
            PercentageMode = "FIXED_PERCENTAGE",
            FixedPercentage = null,
            CreatedBy = 1,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<RetrospectiveTaxPolicyEntity>(It.IsAny<CreateRetrospectiveTaxPolicyDto>()))
            .Returns((CreateRetrospectiveTaxPolicyDto dto) => new RetrospectiveTaxPolicyEntity
            {
                TaxPolicyCode = dto.TaxPolicyCode,
                TaxPolicyName = dto.TaxPolicyName,
                RateMode = dto.RateMode,
                PercentageMode = dto.PercentageMode,
                FixedPercentage = dto.FixedPercentage
            });

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(createDto, CancellationToken.None));

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_FixedPercentageModeWithFixedPercentage_Succeeds()
    {
        // Arrange
        var createDto = new CreateRetrospectiveTaxPolicyDto
        {
            TaxPolicyCode = "TPC001",
            TaxPolicyName = "Standard Policy",
            RateMode = "HISTORIC_YEAR_WISE",
            PercentageMode = "FIXED_PERCENTAGE",
            FixedPercentage = 12.5m,
            CreatedBy = 1,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<RetrospectiveTaxPolicyEntity>(It.IsAny<CreateRetrospectiveTaxPolicyDto>()))
            .Returns((CreateRetrospectiveTaxPolicyDto dto) => new RetrospectiveTaxPolicyEntity
            {
                Id = 1,
                TaxPolicyCode = dto.TaxPolicyCode,
                TaxPolicyName = dto.TaxPolicyName,
                RateMode = dto.RateMode,
                PercentageMode = dto.PercentageMode,
                FixedPercentage = dto.FixedPercentage,
                CreatedDate = DateTime.Now,
                CreatedBy = dto.CreatedBy,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxPolicyEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RetrospectiveTaxPolicyDto>(It.IsAny<RetrospectiveTaxPolicyEntity>()))
            .Returns((RetrospectiveTaxPolicyEntity e) => MapToDto(e));

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("FIXED_PERCENTAGE", result.PercentageMode);
        Assert.Equal(12.5m, result.FixedPercentage);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRetrospectiveTaxPolicyDto
        {
            TaxPolicyCode = "TPC001_Updated",
            TaxPolicyName = "Standard Policy Updated",
            RateMode = "CURRENT_YEAR_FOR_ALL_YEARS",
            PercentageMode = "CURRENT_YEAR_FOR_ALL_YEARS",
            FinancialYearStartMonth = 4,
            FinancialYearStartDay = 1,
            EffectiveFrom = new DateTime(2024, 4, 1),
            EffectiveTo = new DateTime(2025, 3, 31),
            IsActive = true,
            UpdatedBy = 2
        };

        var existingEntity = CreateEntity();

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetrospectiveTaxPolicyDto>(), It.IsAny<RetrospectiveTaxPolicyEntity>()))
            .Callback((UpdateRetrospectiveTaxPolicyDto src, RetrospectiveTaxPolicyEntity dest) =>
            {
                dest.TaxPolicyCode = src.TaxPolicyCode;
                dest.TaxPolicyName = src.TaxPolicyName;
                dest.RateMode = src.RateMode;
                dest.PercentageMode = src.PercentageMode;
                dest.FixedPercentage = src.FixedPercentage;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        _mockMapper
            .Setup(m => m.Map<RetrospectiveTaxPolicyDto>(It.IsAny<RetrospectiveTaxPolicyEntity>()))
            .Returns((RetrospectiveTaxPolicyEntity e) => MapToDto(e));

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("TPC001_Updated", existingEntity.TaxPolicyCode);
        Assert.Equal("CURRENT_YEAR_FOR_ALL_YEARS", existingEntity.PercentageMode);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateRetrospectiveTaxPolicyDto
        {
            TaxPolicyCode = "TPC001",
            TaxPolicyName = "Standard Policy",
            RateMode = "HISTORIC_YEAR_WISE",
            PercentageMode = "HISTORIC_YEAR_WISE",
            IsActive = true,
            UpdatedBy = 2
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxPolicyEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_SetsFixedPercentageModeWithoutFixedPercentage_ThrowsValidationException()
    {
        // Arrange
        var updateDto = new UpdateRetrospectiveTaxPolicyDto
        {
            TaxPolicyCode = "TPC001",
            TaxPolicyName = "Standard Policy",
            RateMode = "HISTORIC_YEAR_WISE",
            PercentageMode = "FIXED_PERCENTAGE",
            FixedPercentage = null,
            IsActive = true,
            UpdatedBy = 2
        };

        var existingEntity = CreateEntity(isActive: true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetrospectiveTaxPolicyDto>(), It.IsAny<RetrospectiveTaxPolicyEntity>()))
            .Callback((UpdateRetrospectiveTaxPolicyDto src, RetrospectiveTaxPolicyEntity dest) =>
            {
                dest.PercentageMode = src.PercentageMode;
                dest.FixedPercentage = src.FixedPercentage;
                dest.IsActive = src.IsActive;
            });

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));

        _mockReferenceValidator.Verify(v => v.ValidateReferencesAsync<RetrospectiveTaxPolicyEntity>(1, It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        // Arrange
        var updateDto = new UpdateRetrospectiveTaxPolicyDto
        {
            TaxPolicyCode = "TPC001",
            TaxPolicyName = "Standard Policy",
            RateMode = "HISTORIC_YEAR_WISE",
            PercentageMode = "HISTORIC_YEAR_WISE",
            IsActive = false,
            UpdatedBy = 2
        };

        var existingEntity = CreateEntity(isActive: true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetrospectiveTaxPolicyDto>(), It.IsAny<RetrospectiveTaxPolicyEntity>()))
            .Callback((UpdateRetrospectiveTaxPolicyDto src, RetrospectiveTaxPolicyEntity dest) =>
            {
                dest.PercentageMode = src.PercentageMode;
                dest.IsActive = src.IsActive;
            });

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<RetrospectiveTaxPolicyEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot deactivate RetrospectiveTaxPolicy. It is referenced by other records."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));

        _mockReferenceValidator.Verify(v => v.ValidateReferencesAsync<RetrospectiveTaxPolicyEntity>(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = 1;
        var existingEntity = CreateEntity();

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<RetrospectiveTaxPolicyEntity>(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockReferenceValidator.Verify(r => r.ValidateReferencesAsync<RetrospectiveTaxPolicyEntity>(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxPolicyEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        // Arrange
        var idToDelete = 1;
        var existingEntity = CreateEntity();

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<RetrospectiveTaxPolicyEntity>(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot delete record because it is referenced elsewhere."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(idToDelete, CancellationToken.None));

        _mockReferenceValidator.Verify(v => v.ValidateReferencesAsync<RetrospectiveTaxPolicyEntity>(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #region CreateFromRangeAsync

    [Fact]
    public async Task CreateFromRangeAsync_GeneratesEntitiesWithTaxPolicyCodeFromRange()
    {
        var request = new NtisPlatform.Application.DTOs.Range.RangeCreateRequest<CreateRetrospectiveTaxPolicyDto>
        {
            RangeFrom = "1",
            RangeTo = "2",
            Template = new CreateRetrospectiveTaxPolicyDto
            {
                TaxPolicyName = "Policy {value}",
                RateMode = "CURRENT_YEAR_FOR_ALL_YEARS",
                PercentageMode = "HISTORIC_YEAR_WISE",
                IsActive = true,
                CreatedBy = 1
            }
        };

        _mockMapper
            .Setup(m => m.Map<RetrospectiveTaxPolicyEntity>(It.IsAny<CreateRetrospectiveTaxPolicyDto>()))
            .Returns((CreateRetrospectiveTaxPolicyDto dto) => new RetrospectiveTaxPolicyEntity { TaxPolicyCode = dto.TaxPolicyCode, TaxPolicyName = dto.TaxPolicyName });

        _mockMapper
            .Setup(m => m.Map<List<RetrospectiveTaxPolicyDto>>(It.IsAny<List<RetrospectiveTaxPolicyEntity>>()))
            .Returns((List<RetrospectiveTaxPolicyEntity> entities) => entities.Select(e => new RetrospectiveTaxPolicyDto { Id = e.Id, TaxPolicyCode = e.TaxPolicyCode }).ToList());

        _mockRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<RetrospectiveTaxPolicyEntity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateFromRangeAsync(request, CancellationToken.None);

        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        _mockRepository.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<RetrospectiveTaxPolicyEntity>>(list => list.Select(e => e.TaxPolicyCode).SequenceEqual(new[] { "1", "2" })
                && list.All(e => e.TaxPolicyName == "Policy 1" || e.TaxPolicyName == "Policy 2")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SaveAsync

    private static SaveRetrospectiveTaxPolicyDto BuildSaveRequest() => new()
    {
        RateMode = "CURRENT_YEAR_FOR_ALL_YEARS",
        PercentageMode = "HISTORIC_YEAR_WISE",
        FinancialYearStartMonth = 4,
        FinancialYearStartDay = 1,
        UpdatedBy = 9
    };

    [Fact]
    public async Task SaveAsync_NoExistingActivePolicy_CreatesNewPolicyWithDefaults()
    {
        var request = BuildSaveRequest();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxPolicyEntity e, CancellationToken _) => { e.Id = 1; return e; });
        _mockMapper.Setup(m => m.Map<RetrospectiveTaxPolicyDto>(It.IsAny<RetrospectiveTaxPolicyEntity>()))
            .Returns((RetrospectiveTaxPolicyEntity e) => MapToDto(e));

        var result = await _service.SaveAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("DEFAULT", result.TaxPolicyCode);
        Assert.Equal("Default Taxation Policy", result.TaxPolicyName);
        Assert.Equal("CURRENT_YEAR_FOR_ALL_YEARS", result.RateMode);
        _mockRepository.Verify(r => r.AddAsync(
            It.Is<RetrospectiveTaxPolicyEntity>(e => e.TaxPolicyCode == "DEFAULT" && e.CreatedBy == 9 && e.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ExistingActivePolicy_UpdatesInPlaceAndKeepsOmittedCodeAndName()
    {
        var request = BuildSaveRequest();
        var existing = CreateEntity();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity> { existing }.BuildMock());
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<RetrospectiveTaxPolicyDto>(It.IsAny<RetrospectiveTaxPolicyEntity>()))
            .Returns((RetrospectiveTaxPolicyEntity e) => MapToDto(e));

        var result = await _service.SaveAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("TPC001", existing.TaxPolicyCode);
        Assert.Equal("Standard Policy", existing.TaxPolicyName);
        Assert.Equal("CURRENT_YEAR_FOR_ALL_YEARS", existing.RateMode);
        Assert.Equal("HISTORIC_YEAR_WISE", existing.PercentageMode);
        Assert.Equal(9, existing.UpdatedBy);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ExistingActivePolicy_OverridesCodeAndNameWhenProvided()
    {
        var request = BuildSaveRequest();
        request.TaxPolicyCode = "NEWCODE";
        request.TaxPolicyName = "New Name";
        var existing = CreateEntity();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity> { existing }.BuildMock());
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<RetrospectiveTaxPolicyDto>(It.IsAny<RetrospectiveTaxPolicyEntity>()))
            .Returns((RetrospectiveTaxPolicyEntity e) => MapToDto(e));

        await _service.SaveAsync(request, CancellationToken.None);

        Assert.Equal("NEWCODE", existing.TaxPolicyCode);
        Assert.Equal("New Name", existing.TaxPolicyName);
    }

    [Fact]
    public async Task SaveAsync_FixedPercentageModeWithoutFixedPercentage_ThrowsValidationException()
    {
        var request = BuildSaveRequest();
        request.PercentageMode = "FIXED_PERCENTAGE";
        request.FixedPercentage = null;

        await Assert.ThrowsAsync<ValidationException>(() => _service.SaveAsync(request, CancellationToken.None));

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveTaxPolicyEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
