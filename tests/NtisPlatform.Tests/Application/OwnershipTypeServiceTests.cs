using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive tests for OwnershipTypeService
/// Tests all CRUD operations with various scenarios
/// </summary>
public class OwnershipTypeServiceTests
{
    private readonly Mock<IRepository<OwnershipTypeEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly OwnershipTypeService _service;

    public OwnershipTypeServiceTests()
    {
        _mockRepository = new Mock<IRepository<OwnershipTypeEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<OwnershipTypeEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());

        // Default: no existing rows, so the duplicate-name check in ValidateForCreateAsync /
        // ValidateForDeactivationAsync passes for every test that doesn't care about it.
        // Tests that DO care override this with their own GetQueryable() setup.
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<OwnershipTypeEntity>().BuildMock());

        _service = new OwnershipTypeService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object, _mockReferenceValidator.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var service = new OwnershipTypeService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object, _mockReferenceValidator.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new OwnershipTypeEntity
        {
            Id = 1,
            OwnershipTypeName = "Government",
            Description = "Government Owned Assets",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns(new OwnershipTypeDto
            {
                Id = 1,
                OwnershipTypeName = "Government",
                Description = "Government Owned Assets",
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Government", result.OwnershipTypeName);
        Assert.Equal("Government Owned Assets", result.Description);
        Assert.True(result.IsActive);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwnershipTypeEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(9999);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ZeroId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwnershipTypeEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(0);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithNullDescription_ReturnsDto()
    {
        // Arrange
        var entity = new OwnershipTypeEntity
        {
            Id = 2,
            OwnershipTypeName = "Private",
            Description = null,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns(new OwnershipTypeDto
            {
                Id = 2,
                OwnershipTypeName = "Private",
                Description = null,
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("Private", result.OwnershipTypeName);
        Assert.Null(result.Description);
    }

    [Fact]
    public async Task GetByIdAsync_PassesCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var entity = new OwnershipTypeEntity { Id = 1, OwnershipTypeName = "Test" };

        _mockRepository.Setup(r => r.GetByIdAsync(1, cancellationToken))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<OwnershipTypeDto>(entity))
            .Returns(new OwnershipTypeDto { Id = 1 });

        // Act
        await _service.GetByIdAsync(1, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, cancellationToken), Times.Once);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<OwnershipTypeEntity>
        {
            new() { Id = 1, OwnershipTypeName = "Government", Description = "Government Owned", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true },
            new() { Id = 2, OwnershipTypeName = "Private", Description = "Private Owned", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OwnershipTypeEntity, OwnershipTypeDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        // Skip validation to allow unmapped destination members (like Id)
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new OwnershipTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var queryParams = new OwnershipTypeQueryParameters
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
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.OwnershipTypeName == "Government");
        Assert.Contains(items, x => x.OwnershipTypeName == "Private");
    }

    [Fact]
    public async Task GetAllAsync_EmptyRepository_ReturnsEmptyResult()
    {
        // Arrange
        var entities = new List<OwnershipTypeEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OwnershipTypeEntity, OwnershipTypeDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var service = new OwnershipTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var queryParams = new OwnershipTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entities = new List<OwnershipTypeEntity>();
        for (int i = 1; i <= 25; i++)
        {
            entities.Add(new OwnershipTypeEntity
            {
                Id = i,
                OwnershipTypeName = $"Ownership Type {i}",
                Description = $"Description {i}",
                IsActive = true,
                CreatedDate = DateTime.Now
            });
        }

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OwnershipTypeEntity, OwnershipTypeDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var service = new OwnershipTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var queryParams = new OwnershipTypeQueryParameters
        {
            PageNumber = 2,
            PageSize = 10
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
    public async Task GetAllAsync_WithFilter_ReturnsFilteredResults()
    {
        // Arrange
        var entities = new List<OwnershipTypeEntity>
        {
            new() { Id = 1, OwnershipTypeName = "Government", Description = "Gov", IsActive = true, CreatedDate = DateTime.Now },
            new() { Id = 2, OwnershipTypeName = "Private", Description = "Pvt", IsActive = true, CreatedDate = DateTime.Now },
            new() { Id = 3, OwnershipTypeName = "Public", Description = "Pub", IsActive = false, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OwnershipTypeEntity, OwnershipTypeDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var service = new OwnershipTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var queryParams = new OwnershipTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    [Fact]
    public async Task GetAllAsync_WithNameFilter_ReturnsFilteredResults()
    {
        // Arrange
        var entities = new List<OwnershipTypeEntity>
        {
            new() { Id = 1, OwnershipTypeName = "Government", Description = "Gov", IsActive = true, CreatedDate = DateTime.Now },
            new() { Id = 2, OwnershipTypeName = "Private", Description = "Pvt", IsActive = true, CreatedDate = DateTime.Now },
            new() { Id = 3, OwnershipTypeName = "Public", Description = "Pub", IsActive = true, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OwnershipTypeEntity, OwnershipTypeDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var service = new OwnershipTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var queryParams = new OwnershipTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            OwnershipTypeName = "Government"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Government", result.Items.First().OwnershipTypeName);
    }

    [Fact]
    public async Task GetAllAsync_PassesCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var entities = new List<OwnershipTypeEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OwnershipTypeEntity, OwnershipTypeDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var service = new OwnershipTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var queryParams = new OwnershipTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        await service.GetAllAsync(queryParams, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.GetQueryable(), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateOwnershipTypeDto
        {
            OwnershipTypeName = "Municipal",
            Description = "Municipal Owned Assets"
        };

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeEntity>(It.IsAny<CreateOwnershipTypeDto>()))
            .Returns((CreateOwnershipTypeDto dto) => new OwnershipTypeEntity
            {
                OwnershipTypeName = dto.OwnershipTypeName,
                Description = dto.Description,
                CreatedBy = 1,
                CreatedDate = DateTime.Now,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwnershipTypeEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns((OwnershipTypeEntity e) => new OwnershipTypeDto
            {
                OwnershipTypeName = e.OwnershipTypeName,
                Description = e.Description,
                IsActive = true
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Municipal", result.OwnershipTypeName);
        Assert.Equal("Municipal Owned Assets", result.Description);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithNullDescription_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateOwnershipTypeDto
        {
            OwnershipTypeName = "Leased",
            Description = null
        };

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeEntity>(It.IsAny<CreateOwnershipTypeDto>()))
            .Returns(new OwnershipTypeEntity
            {
                OwnershipTypeName = createDto.OwnershipTypeName,
                Description = null,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwnershipTypeEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns(new OwnershipTypeDto
            {
                OwnershipTypeName = createDto.OwnershipTypeName,
                Description = null
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Leased", result.OwnershipTypeName);
        Assert.Null(result.Description);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithLongDescription_CreatesSuccessfully()
    {
        // Arrange
        var longDescription = new string('A', 500);
        var createDto = new CreateOwnershipTypeDto
        {
            OwnershipTypeName = "Public-Private Partnership",
            Description = longDescription
        };

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeEntity>(It.IsAny<CreateOwnershipTypeDto>()))
            .Returns(new OwnershipTypeEntity
            {
                OwnershipTypeName = createDto.OwnershipTypeName,
                Description = createDto.Description,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwnershipTypeEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns(new OwnershipTypeDto
            {
                OwnershipTypeName = createDto.OwnershipTypeName,
                Description = createDto.Description
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Public-Private Partnership", result.OwnershipTypeName);
        Assert.Equal(longDescription, result.Description);
    }

    [Fact]
    public async Task CreateAsync_CallsMapperWithCorrectDto()
    {
        // Arrange
        var createDto = new CreateOwnershipTypeDto
        {
            OwnershipTypeName = "Cooperative",
            Description = "Cooperative Ownership"
        };

        CreateOwnershipTypeDto? capturedDto = null;

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeEntity>(It.IsAny<object>()))
            .Callback<object>(dto => capturedDto = dto as CreateOwnershipTypeDto)
            .Returns(new OwnershipTypeEntity());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OwnershipTypeEntity());

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns(new OwnershipTypeDto());

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedDto);
        Assert.Equal("Cooperative", capturedDto.OwnershipTypeName);
        Assert.Equal("Cooperative Ownership", capturedDto.Description);
    }

    [Fact]
    public async Task CreateAsync_PassesCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var createDto = new CreateOwnershipTypeDto
        {
            OwnershipTypeName = "Test Type"
        };

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeEntity>(It.IsAny<CreateOwnershipTypeDto>()))
            .Returns(new OwnershipTypeEntity());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OwnershipTypeEntity>(), cancellationToken))
            .ReturnsAsync(new OwnershipTypeEntity());

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns(new OwnershipTypeDto());

        // Act
        await _service.CreateAsync(createDto, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<OwnershipTypeEntity>(), cancellationToken), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithSpecialCharacters_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateOwnershipTypeDto
        {
            OwnershipTypeName = "State/Central Government",
            Description = "Assets owned by State & Central Government"
        };

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeEntity>(It.IsAny<CreateOwnershipTypeDto>()))
            .Returns(new OwnershipTypeEntity
            {
                OwnershipTypeName = createDto.OwnershipTypeName,
                Description = createDto.Description,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwnershipTypeEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns(new OwnershipTypeDto
            {
                OwnershipTypeName = createDto.OwnershipTypeName,
                Description = createDto.Description
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("State/Central Government", result.OwnershipTypeName);
        Assert.Equal("Assets owned by State & Central Government", result.Description);
    }

    [Fact]
    public async Task CreateAsync_DuplicateOwnershipTypeName_ThrowsValidationException()
    {
        // Arrange
        var existing = new List<OwnershipTypeEntity>
        {
            new() { Id = 1, OwnershipTypeName = "Government", MarkedForDeletion = false }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existing.BuildMock());

        var createDto = new CreateOwnershipTypeDto { OwnershipTypeName = "Government" };

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeEntity>(It.IsAny<CreateOwnershipTypeDto>()))
            .Returns(new OwnershipTypeEntity { OwnershipTypeName = "Government" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));
        Assert.Contains("OwnershipType_OwnershipTypeName_Duplicate", ex.Errors.Values);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicateExcludedByMarkedForDeletion_Succeeds()
    {
        // Arrange
        var existing = new List<OwnershipTypeEntity>
        {
            new() { Id = 1, OwnershipTypeName = "Government", MarkedForDeletion = true }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existing.BuildMock());

        var createDto = new CreateOwnershipTypeDto { OwnershipTypeName = "Government" };

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeEntity>(It.IsAny<CreateOwnershipTypeDto>()))
            .Returns(new OwnershipTypeEntity { OwnershipTypeName = "Government" });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwnershipTypeEntity e, CancellationToken _) => { e.Id = 2; return e; });

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns(new OwnershipTypeDto { Id = 2, OwnershipTypeName = "Government" });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateOwnershipTypeDto
        {
            OwnershipTypeName = "Updated Government",
            Description = "Updated Description",
        };

        var existingEntity = new OwnershipTypeEntity
        {
            Id = 1,
            OwnershipTypeName = "Old Government",
            Description = "Old Description",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwnershipTypeDto>(), It.IsAny<OwnershipTypeEntity>()))
            .Callback((UpdateOwnershipTypeDto src, OwnershipTypeEntity dest) =>
            {
                dest.OwnershipTypeName = src.OwnershipTypeName;
                dest.Description = src.Description;
            });

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns((OwnershipTypeEntity e) => new OwnershipTypeDto
            {
                Id = e.Id,
                OwnershipTypeName = e.OwnershipTypeName,
                Description = e.Description
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Government", result.OwnershipTypeName);
        Assert.Equal("Updated Description", result.Description);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("Updated Government", existingEntity.OwnershipTypeName);
        Assert.Equal("Updated Description", existingEntity.Description);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateOwnershipTypeDto
        {
            OwnershipTypeName = "Updated Type",
            Description = "Updated Description"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwnershipTypeEntity?)null);

        // Act
        var result = await _service.UpdateAsync(9999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithNullDescription_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateOwnershipTypeDto
        {
            OwnershipTypeName = "Private Corporation",
            Description = null
        };

        var existingEntity = new OwnershipTypeEntity
        {
            Id = 5,
            OwnershipTypeName = "Old Private",
            Description = "Old Description",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwnershipTypeDto>(), It.IsAny<OwnershipTypeEntity>()))
            .Callback((UpdateOwnershipTypeDto src, OwnershipTypeEntity dest) =>
            {
                dest.OwnershipTypeName = src.OwnershipTypeName;
                dest.Description = src.Description;
            });

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns((OwnershipTypeEntity e) => new OwnershipTypeDto
            {
                Id = e.Id,
                OwnershipTypeName = e.OwnershipTypeName,
                Description = e.Description
            });

        // Act
        var result = await _service.UpdateAsync(5, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Private Corporation", result.OwnershipTypeName);
        Assert.Null(result.Description);
        Assert.Null(existingEntity.Description);
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyDescription_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateOwnershipTypeDto
        {
            OwnershipTypeName = "Community Owned",
            Description = string.Empty
        };

        var existingEntity = new OwnershipTypeEntity
        {
            Id = 3,
            OwnershipTypeName = "Old Community",
            Description = "Old Description",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwnershipTypeDto>(), It.IsAny<OwnershipTypeEntity>()))
            .Callback((UpdateOwnershipTypeDto src, OwnershipTypeEntity dest) =>
            {
                dest.OwnershipTypeName = src.OwnershipTypeName;
                dest.Description = src.Description;
            });

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns((OwnershipTypeEntity e) => new OwnershipTypeDto
            {
                Id = e.Id,
                OwnershipTypeName = e.OwnershipTypeName,
                Description = e.Description
            });

        // Act
        var result = await _service.UpdateAsync(3, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Community Owned", result.OwnershipTypeName);
        Assert.Equal(string.Empty, result.Description);
    }

    [Fact]
    public async Task UpdateAsync_PassesCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var updateDto = new UpdateOwnershipTypeDto
        {
            OwnershipTypeName = "Test Type"
        };

        var existingEntity = new OwnershipTypeEntity { Id = 1 };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, cancellationToken))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OwnershipTypeEntity>(), cancellationToken))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwnershipTypeDto>(), It.IsAny<OwnershipTypeEntity>()));

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns(new OwnershipTypeDto());

        // Act
        await _service.UpdateAsync(1, updateDto, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, cancellationToken), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<OwnershipTypeEntity>(), cancellationToken), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var updateDto = new UpdateOwnershipTypeDto
        {
            OwnershipTypeName = "New Name",
            Description = "New Description"
        };

        var existingEntity = new OwnershipTypeEntity
        {
            Id = 10,
            OwnershipTypeName = "Old Name",
            Description = "Old Description",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-30)
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwnershipTypeDto>(), It.IsAny<OwnershipTypeEntity>()))
            .Callback((UpdateOwnershipTypeDto src, OwnershipTypeEntity dest) =>
            {
                dest.OwnershipTypeName = src.OwnershipTypeName;
                dest.Description = src.Description;
            });

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns((OwnershipTypeEntity e) => new OwnershipTypeDto
            {
                Id = e.Id,
                OwnershipTypeName = e.OwnershipTypeName,
                Description = e.Description,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(10, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal("New Name", result.OwnershipTypeName);
        Assert.Equal("New Description", result.Description);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_MultipleUpdatesOnSameEntity_LastUpdateWins()
    {
        // Arrange
        var firstUpdateDto = new UpdateOwnershipTypeDto
        {
            OwnershipTypeName = "First Update",
            Description = "First Description"
        };

        var secondUpdateDto = new UpdateOwnershipTypeDto
        {
            OwnershipTypeName = "Second Update",
            Description = "Second Description"
        };

        var existingEntity = new OwnershipTypeEntity
        {
            Id = 7,
            OwnershipTypeName = "Original",
            Description = "Original Description",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwnershipTypeDto>(), It.IsAny<OwnershipTypeEntity>()))
            .Callback((UpdateOwnershipTypeDto src, OwnershipTypeEntity dest) =>
            {
                dest.OwnershipTypeName = src.OwnershipTypeName;
                dest.Description = src.Description;
            });

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns((OwnershipTypeEntity e) => new OwnershipTypeDto
            {
                Id = e.Id,
                OwnershipTypeName = e.OwnershipTypeName,
                Description = e.Description
            });

        // Act
        await _service.UpdateAsync(7, firstUpdateDto, CancellationToken.None);
        var result = await _service.UpdateAsync(7, secondUpdateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Second Update", result.OwnershipTypeName);
        Assert.Equal("Second Description", result.Description);
    }

    [Fact]
    public async Task UpdateAsync_RenameToAnotherRowsName_ThrowsValidationException()
    {
        // Arrange
        var existingEntity = new OwnershipTypeEntity { Id = 1, OwnershipTypeName = "Government", IsActive = true };
        var other = new OwnershipTypeEntity { Id = 2, OwnershipTypeName = "Private", IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<OwnershipTypeEntity> { existingEntity, other }.BuildMock());

        var updateDto = new UpdateOwnershipTypeDto { OwnershipTypeName = "Private", IsActive = true };

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwnershipTypeDto>(), It.IsAny<OwnershipTypeEntity>()))
            .Callback((UpdateOwnershipTypeDto src, OwnershipTypeEntity dest) =>
            {
                dest.OwnershipTypeName = src.OwnershipTypeName;
            });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));
        Assert.Contains("OwnershipType_OwnershipTypeName_Duplicate", ex.Errors.Values);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingReferencedRecord_ThrowsValidationException()
    {
        // Arrange
        var existingEntity = new OwnershipTypeEntity { Id = 1, OwnershipTypeName = "Government", IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<OwnershipTypeEntity> { existingEntity }.BuildMock());
        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<OwnershipTypeEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Failure("Referenced elsewhere"));

        var updateDto = new UpdateOwnershipTypeDto { OwnershipTypeName = "Government", IsActive = false };

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwnershipTypeDto>(), It.IsAny<OwnershipTypeEntity>()))
            .Callback((UpdateOwnershipTypeDto src, OwnershipTypeEntity dest) =>
            {
                dest.OwnershipTypeName = src.OwnershipTypeName;
                dest.IsActive = src.IsActive;
            });

        // Act & Assert
        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingUnreferencedRecord_CallsReferenceValidation()
    {
        // Arrange
        var existingEntity = new OwnershipTypeEntity { Id = 1, OwnershipTypeName = "Government", IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<OwnershipTypeEntity> { existingEntity }.BuildMock());
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var updateDto = new UpdateOwnershipTypeDto { OwnershipTypeName = "Government", IsActive = false };

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwnershipTypeDto>(), It.IsAny<OwnershipTypeEntity>()))
            .Callback((UpdateOwnershipTypeDto src, OwnershipTypeEntity dest) =>
            {
                dest.OwnershipTypeName = src.OwnershipTypeName;
                dest.IsActive = src.IsActive;
            });

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns((OwnershipTypeEntity e) => new OwnershipTypeDto { Id = e.Id, OwnershipTypeName = e.OwnershipTypeName, IsActive = e.IsActive });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        _mockReferenceValidator.Verify(
            v => v.ValidateReferencesAsync<OwnershipTypeEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndReturnsTrue()
    {
        // Arrange
        int idToDelete = 1;

        var existingEntity = new OwnershipTypeEntity
        {
            Id = idToDelete,
            OwnershipTypeName = "Type to Delete",
            Description = "Description",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        // Arrange
        int idToDelete = 9999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwnershipTypeEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ZeroId_ReturnsFalse()
    {
        // Arrange
        int idToDelete = 0;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwnershipTypeEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_PassesCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        int idToDelete = 5;

        var existingEntity = new OwnershipTypeEntity { Id = idToDelete };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, cancellationToken))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<OwnershipTypeEntity>(), cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(idToDelete, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, cancellationToken), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(existingEntity, cancellationToken), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_MultipleIds_DeletesCorrectEntities()
    {
        // Arrange
        var testIds = new[] { 1, 5, 10 };

        foreach (var id in testIds)
        {
            var entity = new OwnershipTypeEntity
            {
                Id = id,
                OwnershipTypeName = $"Type {id}"
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockRepository
                .Setup(r => r.DeleteAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(id, CancellationToken.None);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task DeleteAsync_ActiveEntity_DeletesSuccessfully()
    {
        // Arrange
        int idToDelete = 20;

        var activeEntity = new OwnershipTypeEntity
        {
            Id = idToDelete,
            OwnershipTypeName = "Active Type",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(activeEntity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsReferenceValidation()
    {
        // Arrange
        var entity = new OwnershipTypeEntity { Id = 1, OwnershipTypeName = "Government" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        _mockReferenceValidator.Verify(
            v => v.ValidateReferencesAsync<OwnershipTypeEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReferencedRecord_ThrowsValidationException()
    {
        // Arrange
        var entity = new OwnershipTypeEntity { Id = 1, OwnershipTypeName = "Government" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<OwnershipTypeEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Failure("Referenced elsewhere"));

        // Act & Assert
        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.DeleteAsync(1, CancellationToken.None));
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CreateThenGet_ReturnsCreatedEntity()
    {
        // Arrange
        var createDto = new CreateOwnershipTypeDto
        {
            OwnershipTypeName = "Integration Test Type",
            Description = "Integration Test Description"
        };

        var createdEntity = new OwnershipTypeEntity
        {
            Id = 100,
            OwnershipTypeName = createDto.OwnershipTypeName,
            Description = createDto.Description,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeEntity>(It.IsAny<CreateOwnershipTypeDto>()))
            .Returns(createdEntity);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEntity);

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns(new OwnershipTypeDto
            {
                Id = 100,
                OwnershipTypeName = createdEntity.OwnershipTypeName,
                Description = createdEntity.Description
            });

        _mockRepository
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEntity);

        // Act
        var createResult = await _service.CreateAsync(createDto, CancellationToken.None);
        var getResult = await _service.GetByIdAsync(100, CancellationToken.None);

        // Assert
        Assert.NotNull(createResult);
        Assert.NotNull(getResult);
        Assert.Equal(createResult.OwnershipTypeName, getResult.OwnershipTypeName);
        Assert.Equal(createResult.Description, getResult.Description);
    }

    [Fact]
    public async Task UpdateThenGet_ReturnsUpdatedEntity()
    {
        // Arrange
        var existingEntity = new OwnershipTypeEntity
        {
            Id = 50,
            OwnershipTypeName = "Original Name",
            Description = "Original Description"
        };

        var updateDto = new UpdateOwnershipTypeDto
        {
            OwnershipTypeName = "Updated Name",
            Description = "Updated Description"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwnershipTypeDto>(), It.IsAny<OwnershipTypeEntity>()))
            .Callback((UpdateOwnershipTypeDto src, OwnershipTypeEntity dest) =>
            {
                dest.OwnershipTypeName = src.OwnershipTypeName;
                dest.Description = src.Description;
            });

        _mockMapper
            .Setup(m => m.Map<OwnershipTypeDto>(It.IsAny<OwnershipTypeEntity>()))
            .Returns((OwnershipTypeEntity e) => new OwnershipTypeDto
            {
                Id = e.Id,
                OwnershipTypeName = e.OwnershipTypeName,
                Description = e.Description
            });

        // Act
        var updateResult = await _service.UpdateAsync(50, updateDto, CancellationToken.None);
        var getResult = await _service.GetByIdAsync(50, CancellationToken.None);

        // Assert
        Assert.NotNull(updateResult);
        Assert.NotNull(getResult);
        Assert.Equal("Updated Name", updateResult.OwnershipTypeName);
        Assert.Equal("Updated Description", updateResult.Description);
        Assert.Equal(updateResult.OwnershipTypeName, getResult.OwnershipTypeName);
    }

    [Fact]
    public async Task DeleteThenGet_ReturnsNull()
    {
        // Arrange
        int idToDelete = 75;
        var existingEntity = new OwnershipTypeEntity
        {
            Id = idToDelete,
            OwnershipTypeName = "To Be Deleted"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<OwnershipTypeEntity, CancellationToken>((ent, ct) =>
            {
                _mockRepository
                    .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((OwnershipTypeEntity?)null);
            });

        // Act
        var deleteResult = await _service.DeleteAsync(idToDelete, CancellationToken.None);
        var getResult = await _service.GetByIdAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(deleteResult);
        Assert.Null(getResult);
    }

    [Fact]
    public async Task CreateMultipleEntities_AllCreatedSuccessfully()
    {
        // Arrange
        var createDtos = new[]
        {
            new CreateOwnershipTypeDto { OwnershipTypeName = "Type 1", Description = "Desc 1" },
            new CreateOwnershipTypeDto { OwnershipTypeName = "Type 2", Description = "Desc 2" },
            new CreateOwnershipTypeDto { OwnershipTypeName = "Type 3", Description = "Desc 3" }
        };

        var createdEntities = new List<OwnershipTypeEntity>();

        foreach (var dto in createDtos)
        {
            var entity = new OwnershipTypeEntity
            {
                OwnershipTypeName = dto.OwnershipTypeName,
                Description = dto.Description
            };

            _mockMapper
                .Setup(m => m.Map<OwnershipTypeEntity>(It.Is<CreateOwnershipTypeDto>(d => d.OwnershipTypeName == dto.OwnershipTypeName)))
                .Returns(entity);

            _mockRepository
                .Setup(r => r.AddAsync(It.Is<OwnershipTypeEntity>(e => e.OwnershipTypeName == dto.OwnershipTypeName), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockMapper
                .Setup(m => m.Map<OwnershipTypeDto>(It.Is<OwnershipTypeEntity>(e => e.OwnershipTypeName == dto.OwnershipTypeName)))
                .Returns(new OwnershipTypeDto
                {
                    OwnershipTypeName = entity.OwnershipTypeName,
                    Description = entity.Description
                });

            createdEntities.Add(entity);
        }

        // Act
        var results = new List<OwnershipTypeDto>();
        foreach (var dto in createDtos)
        {
            var result = await _service.CreateAsync(dto, CancellationToken.None);
            results.Add(result);
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.OwnershipTypeName == "Type 1");
        Assert.Contains(results, r => r.OwnershipTypeName == "Type 2");
        Assert.Contains(results, r => r.OwnershipTypeName == "Type 3");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<OwnershipTypeEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    #endregion
}


