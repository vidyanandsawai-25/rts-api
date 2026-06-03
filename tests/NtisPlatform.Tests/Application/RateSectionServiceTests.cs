using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using ValidationResult = NtisPlatform.Application.Models.ValidationResult;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive tests for RateSectionService covering CRUD operations
/// </summary>
public class RateSectionServiceTests : IDisposable
{
    private readonly Mock<IRepository<RateSectionEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<LocalizationProcessor> _mockLocalizationProcessor;
    private readonly Mock<ILocalizedQueryService> _mockLocalizedQueryService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly RateSectionService _service;

    public RateSectionServiceTests()
    {
        _mockRepository = new Mock<IRepository<RateSectionEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLocalizedQueryService = new Mock<ILocalizedQueryService>();

        // Configure LocalizationProcessor mock
        var mockLocalization = new Mock<ILocalization>();
        _mockLocalizationProcessor = new Mock<LocalizationProcessor>(
            mockLocalization.Object,
            _mockHttpContextAccessor.Object,
            Options.Create(new LocalizationOptions { DefaultLanguage = "en" }))
        {
            CallBase = true
        };

        // Setup localization processor methods
        _mockLocalizationProcessor
            .Setup(x => x.ProcessGetAsync(It.IsAny<IEnumerable<RateSectionDto>>()))
            .Returns(Task.CompletedTask);

        _mockLocalizationProcessor
            .Setup(x => x.ProcessSaveAsync(It.IsAny<CreateRateSectionDto>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .Returns(Task.CompletedTask);

        _mockLocalizationProcessor
            .Setup(x => x.ProcessSaveAsync(It.IsAny<UpdateRateSectionDto>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .Returns(Task.CompletedTask);

        // Setup default UnitOfWork behavior
        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup default HttpContext
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextKeys.CurrentLanguage] = "en";
        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // ✅ Fixed: Proper service initialization with all dependencies
        _service = new RateSectionService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object,
            _mockLocalizationProcessor.Object,
            _mockLocalizedQueryService.Object,
            _mockHttpContextAccessor.Object);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new RateSectionEntity
        {
            Id = 1,
            Description = "Wakad",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = 31,
            UpdatedDate = DateTime.UtcNow,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper
            .Setup(m => m.Map<RateSectionDto>(It.IsAny<RateSectionEntity>()))
            .Returns(new RateSectionDto
            {
                Id = 1,
                Description = "Wakad",
                IsActive = true,
                CreatedDate = entity.CreatedDate,
                UpdatedDate = entity.UpdatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Wakad", result.Description);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<RateSectionDto>(It.IsAny<RateSectionEntity>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RateSectionEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(9999);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<RateSectionDto>(It.IsAny<RateSectionEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull(int invalidId)
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RateSectionEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithValidParameters_ReturnsPagedResults()
    {
        // Arrange
        var entities = new List<RateSectionEntity>
        {
            new() { Id = 1, Description = "Wakad", IsActive = true, CreatedBy = 31, CreatedDate = DateTime.UtcNow },
            new() { Id = 2, Description = "Moshi", IsActive = true, CreatedBy = 31, CreatedDate = DateTime.UtcNow },
            new() { Id = 3, Description = "Thergav", IsActive = true, CreatedBy = 31, CreatedDate = DateTime.UtcNow }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RateSectionEntity, RateSectionDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        var mapper = mapperConfig.CreateMapper();

        // ✅ Fixed: Proper service initialization
        var service = new RateSectionService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object,
            _mockLocalizationProcessor.Object,
            _mockLocalizedQueryService.Object,
            _mockHttpContextAccessor.Object);

        var queryParams = new RateSectionQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
            SearchTerm = null!,
            SortBy = null!
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
        Assert.Contains(result.Items, x => x.Description == "Wakad");
        Assert.Contains(result.Items, x => x.Description == "Moshi");
        Assert.Contains(result.Items, x => x.Description == "Thergav");
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entities = Enumerable.Range(1, 25).Select(i => new RateSectionEntity
        {
            Id = i,
            Description = $"Rate Section {i}",
            IsActive = true,
            CreatedBy = 31,
            CreatedDate = DateTime.UtcNow
        }).ToList();

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RateSectionEntity, RateSectionDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        var mapper = mapperConfig.CreateMapper();

        // ✅ Fixed: Added reference validator
        var service = new RateSectionService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object,
            _mockLocalizationProcessor.Object,
            _mockLocalizedQueryService.Object,
            _mockHttpContextAccessor.Object);

        var queryParams = new RateSectionQueryParameters
        {
            PageNumber = 2,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetAllAsync_WithEmptyDatabase_ReturnsEmptyResult()
    {
        // Arrange
        var entities = new List<RateSectionEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RateSectionEntity, RateSectionDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        var mapper = mapperConfig.CreateMapper();

        // ✅ Fixed: Added reference validator
        var service = new RateSectionService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object,
            _mockLocalizationProcessor.Object,
            _mockLocalizedQueryService.Object,
            _mockHttpContextAccessor.Object);

        var queryParams = new RateSectionQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesAndReturnsDto()
    {
        // Arrange
        var createDto = new CreateRateSectionDto
        {
            Description = "Wakad",
            IsActive = true,
            CreatedBy = 31
        };

        var createdEntity = new RateSectionEntity
        {
            Id = 1,
            Description = createDto.Description,
            IsActive = createDto.IsActive,
            CreatedBy = createDto.CreatedBy,
            CreatedDate = DateTime.UtcNow
        };

        _mockMapper
            .Setup(m => m.Map<RateSectionEntity>(It.IsAny<CreateRateSectionDto>()))
            .Returns(createdEntity);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEntity);

        _mockMapper
            .Setup(m => m.Map<RateSectionDto>(It.IsAny<RateSectionEntity>()))
            .Returns(new RateSectionDto
            {
                Id = 1,
                Description = createdEntity.Description,
                IsActive = createdEntity.IsActive,
                CreatedDate = createdEntity.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Wakad", result.Description);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInactiveRecord_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateRateSectionDto
        {
            Description = "Wakad",
            IsActive = false,
            CreatedBy = 31
        };

        var createdEntity = new RateSectionEntity
        {
            Id = 1,
            Description = createDto.Description,
            IsActive = false,
            CreatedBy = createDto.CreatedBy,
            CreatedDate = DateTime.UtcNow
        };

        _mockMapper
            .Setup(m => m.Map<RateSectionEntity>(It.IsAny<CreateRateSectionDto>()))
            .Returns(createdEntity);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEntity);

        _mockMapper
            .Setup(m => m.Map<RateSectionDto>(It.IsAny<RateSectionEntity>()))
            .Returns(new RateSectionDto
            {
                Id = 1,
                Description = createdEntity.Description,
                IsActive = false
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRateSectionDto
        {
            Description = "Wakad",
            IsActive = true,
            UpdatedBy = 31
        };

        var existingEntity = new RateSectionEntity
        {
            Id = 1,
            Description = "Wakad",
            IsActive = true,
            CreatedBy = 31,
            CreatedDate = DateTime.UtcNow.AddDays(-1),
            UpdatedBy = 31,
            UpdatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRateSectionDto>(), It.IsAny<RateSectionEntity>()))
            .Callback((UpdateRateSectionDto src, RateSectionEntity dest) =>
            {
                dest.Description = src.Description;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.UtcNow;
            });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.Equal("Wakad", existingEntity.Description);
        Assert.True(existingEntity.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdateRateSectionDto
        {
            Description = "Wakad",
            IsActive = true,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RateSectionEntity?)null);

        // Act
        await _service.UpdateAsync(9999, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRateSectionDto
        {
            Description = "Wakad",
            IsActive = false,
            UpdatedBy = 31
        };

        var existingEntity = new RateSectionEntity
        {
            Id = 1,
            Description = "Wakad",
            IsActive = true,
            CreatedBy = 31,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRateSectionDto>(), It.IsAny<RateSectionEntity>()))
            .Callback((UpdateRateSectionDto src, RateSectionEntity dest) =>
            {
                dest.Description = src.Description;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.UtcNow;
            });

        // ✅ Fixed: Setup reference validation to succeed
        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<RateSectionEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.False(existingEntity.IsActive);
        Assert.Equal("Wakad", existingEntity.Description);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        // Arrange
        var updateDto = new UpdateRateSectionDto
        {
            Description = "Wakad",
            IsActive = false
        };

        var existingEntity = new RateSectionEntity
        {
            Id = 1,
            Description = "Wakad",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRateSectionDto>(), It.IsAny<RateSectionEntity>()))
            .Callback((UpdateRateSectionDto src, RateSectionEntity dest) =>
            {
                dest.IsActive = src.IsActive;
            });

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<RateSectionEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot deactivate Rate Section. It is referenced by other records."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithoutReferences_Succeeds()
    {
        // Arrange
        var updateDto = new UpdateRateSectionDto
        {
            Description = "Wakad",
            IsActive = false
        };

        var existingEntity = new RateSectionEntity
        {
            Id = 1,
            Description = "Wakad",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRateSectionDto>(), It.IsAny<RateSectionEntity>()))
            .Callback((UpdateRateSectionDto src, RateSectionEntity dest) =>
            {
                dest.IsActive = src.IsActive;
            });

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<RateSectionEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.False(existingEntity.IsActive);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingEntity_DeletesAndReturnsTrue()
    {
        // Arrange
        var existingEntity = new RateSectionEntity
        {
            Id = 1,
            Description = "Wakad",
            IsActive = true,
            CreatedBy = 31,
            CreatedDate = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        // ✅ Fixed: Setup reference validation to succeed
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<RateSectionEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        Assert.True(result);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingEntity_ReturnsFalseAndDoesNotSave()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RateSectionEntity?)null);

        // Act
        var result = await _service.DeleteAsync(9999, CancellationToken.None);

        // Assert
        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse(int invalidId)
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RateSectionEntity?)null);

        // Act
        var result = await _service.DeleteAsync(invalidId, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new RateSectionEntity
        {
            Id = idToDelete,
            Description = "Wakad",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<RateSectionEntity>(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot delete Rate Section. It is referenced by other records."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(idToDelete, CancellationToken.None));

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithoutReferences_Succeeds()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new RateSectionEntity
        {
            Id = idToDelete,
            Description = "Wakad",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<RateSectionEntity>(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RateSectionEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_PassesTokenCorrectly()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var entity = new RateSectionEntity { Id = 1, Description = "Wakad", IsActive = true };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper
            .Setup(m => m.Map<RateSectionDto>(It.IsAny<RateSectionEntity>()))
            .Returns(new RateSectionDto { Id = 1, Description = "Wakad" });

        // Act
        var result = await _service.GetByIdAsync(1, cts.Token);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.Is<CancellationToken>(ct => ct == cts.Token)), Times.Once);
    }

    #endregion
}