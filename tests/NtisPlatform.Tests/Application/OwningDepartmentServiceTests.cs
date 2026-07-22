using NtisPlatform.Application.Interfaces;
using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;


namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive tests for OwningDepartmentService
/// Tests all CRUD operations with various scenarios
/// </summary>
public class OwningDepartmentServiceTests
{
    private readonly Mock<IRepository<OwningDepartmentEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly OwningDepartmentService _service;

    public OwningDepartmentServiceTests()
    {
        _mockRepository = new Mock<IRepository<OwningDepartmentEntity, int>>();
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
            .Setup(v => v.ValidateReferencesAsync<OwningDepartmentEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());

        _service = new OwningDepartmentService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object, _mockReferenceValidator.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var service = new OwningDepartmentService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object, _mockReferenceValidator.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new OwningDepartmentEntity
        {
            Id = 1,
            OwningDepartmentName = "IT Department",
            Description = "Information Technology Department",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns(new OwningDepartmentDto
            {
                Id = 1,
                OwningDepartmentName = "IT Department",
                Description = "Information Technology Department",
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("IT Department", result.OwningDepartmentName);
        Assert.Equal("Information Technology Department", result.Description);
        Assert.True(result.IsActive);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwningDepartmentEntity?)null);

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
            .ReturnsAsync((OwningDepartmentEntity?)null);

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
        var entity = new OwningDepartmentEntity
        {
            Id = 2,
            OwningDepartmentName = "HR Department",
            Description = null,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns(new OwningDepartmentDto
            {
                Id = 2,
                OwningDepartmentName = "HR Department",
                Description = null,
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("HR Department", result.OwningDepartmentName);
        Assert.Null(result.Description);
    }

    [Fact]
    public async Task GetByIdAsync_PassesCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var entity = new OwningDepartmentEntity { Id = 1, OwningDepartmentName = "Test" };

        _mockRepository.Setup(r => r.GetByIdAsync(1, cancellationToken))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<OwningDepartmentDto>(entity))
            .Returns(new OwningDepartmentDto { Id = 1 });

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
        var entities = new List<OwningDepartmentEntity>
        {
            new() { Id = 1, OwningDepartmentName = "IT Department", Description = "IT Dept", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true },
            new() { Id = 2, OwningDepartmentName = "HR Department", Description = "HR Dept", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OwningDepartmentEntity, OwningDepartmentDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        // Skip validation to allow unmapped destination members (like Id)
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new OwningDepartmentService(_mockRepository.Object, _mockUnitOfWork.Object, mapper, _mockReferenceValidator.Object);

        var queryParams = new OwningDepartmentQueryParameters
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
        Assert.Contains(items, x => x.OwningDepartmentName == "IT Department");
        Assert.Contains(items, x => x.OwningDepartmentName == "HR Department");
    }

    [Fact]
    public async Task GetAllAsync_EmptyRepository_ReturnsEmptyResult()
    {
        // Arrange
        var entities = new List<OwningDepartmentEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OwningDepartmentEntity, OwningDepartmentDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var service = new OwningDepartmentService(_mockRepository.Object, _mockUnitOfWork.Object, mapper, _mockReferenceValidator.Object);

        var queryParams = new OwningDepartmentQueryParameters
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
        var entities = new List<OwningDepartmentEntity>();
        for (int i = 1; i <= 25; i++)
        {
            entities.Add(new OwningDepartmentEntity
            {
                Id = i,
                OwningDepartmentName = $"Department {i}",
                Description = $"Description {i}",
                IsActive = true,
                CreatedDate = DateTime.Now
            });
        }

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OwningDepartmentEntity, OwningDepartmentDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var service = new OwningDepartmentService(_mockRepository.Object, _mockUnitOfWork.Object, mapper, _mockReferenceValidator.Object);

        var queryParams = new OwningDepartmentQueryParameters
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
        var entities = new List<OwningDepartmentEntity>
        {
            new() { Id = 1, OwningDepartmentName = "IT Department", Description = "IT", IsActive = true, CreatedDate = DateTime.Now },
            new() { Id = 2, OwningDepartmentName = "HR Department", Description = "HR", IsActive = true, CreatedDate = DateTime.Now },
            new() { Id = 3, OwningDepartmentName = "Finance Department", Description = "Finance", IsActive = false, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OwningDepartmentEntity, OwningDepartmentDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var service = new OwningDepartmentService(_mockRepository.Object, _mockUnitOfWork.Object, mapper, _mockReferenceValidator.Object);

        var queryParams = new OwningDepartmentQueryParameters
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
    public async Task GetAllAsync_PassesCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var entities = new List<OwningDepartmentEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OwningDepartmentEntity, OwningDepartmentDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var service = new OwningDepartmentService(_mockRepository.Object, _mockUnitOfWork.Object, mapper, _mockReferenceValidator.Object);

        var queryParams = new OwningDepartmentQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        await service.GetAllAsync(queryParams, cancellationToken);

        // Assert - verification that the service called the repository
        _mockRepository.Verify(r => r.GetQueryable(), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateOwningDepartmentDto
        {
            OwningDepartmentName = "New Department",
            Description = "New Department Description"
        };

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentEntity>(It.IsAny<CreateOwningDepartmentDto>()))
            .Returns((CreateOwningDepartmentDto dto) => new OwningDepartmentEntity
            {
                OwningDepartmentName = dto.OwningDepartmentName,
                Description = dto.Description,
                CreatedBy = 1,
                CreatedDate = DateTime.Now,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwningDepartmentEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns((OwningDepartmentEntity e) => new OwningDepartmentDto
            {
                OwningDepartmentName = e.OwningDepartmentName,
                Description = e.Description,
                IsActive = true
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Department", result.OwningDepartmentName);
        Assert.Equal("New Department Description", result.Description);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithNullDescription_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateOwningDepartmentDto
        {
            OwningDepartmentName = "Legal Department",
            Description = null
        };

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentEntity>(It.IsAny<CreateOwningDepartmentDto>()))
            .Returns(new OwningDepartmentEntity
            {
                OwningDepartmentName = createDto.OwningDepartmentName,
                Description = null,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwningDepartmentEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns(new OwningDepartmentDto
            {
                OwningDepartmentName = createDto.OwningDepartmentName,
                Description = null
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Legal Department", result.OwningDepartmentName);
        Assert.Null(result.Description);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithLongDescription_CreatesSuccessfully()
    {
        // Arrange
        var longDescription = new string('A', 500);
        var createDto = new CreateOwningDepartmentDto
        {
            OwningDepartmentName = "Research Department",
            Description = longDescription
        };

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentEntity>(It.IsAny<CreateOwningDepartmentDto>()))
            .Returns(new OwningDepartmentEntity
            {
                OwningDepartmentName = createDto.OwningDepartmentName,
                Description = createDto.Description,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwningDepartmentEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns(new OwningDepartmentDto
            {
                OwningDepartmentName = createDto.OwningDepartmentName,
                Description = createDto.Description
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Research Department", result.OwningDepartmentName);
        Assert.Equal(longDescription, result.Description);
    }

    [Fact]
    public async Task CreateAsync_CallsMapperWithCorrectDto()
    {
        // Arrange
        var createDto = new CreateOwningDepartmentDto
        {
            OwningDepartmentName = "Operations",
            Description = "Operations Department"
        };

        CreateOwningDepartmentDto? capturedDto = null;

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentEntity>(It.IsAny<object>()))
            .Callback<object>(dto => capturedDto = dto as CreateOwningDepartmentDto)
            .Returns(new OwningDepartmentEntity());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OwningDepartmentEntity());

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns(new OwningDepartmentDto());

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedDto);
        Assert.Equal("Operations", capturedDto.OwningDepartmentName);
        Assert.Equal("Operations Department", capturedDto.Description);
    }

    [Fact]
    public async Task CreateAsync_PassesCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var createDto = new CreateOwningDepartmentDto
        {
            OwningDepartmentName = "Test Department"
        };

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentEntity>(It.IsAny<CreateOwningDepartmentDto>()))
            .Returns(new OwningDepartmentEntity());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OwningDepartmentEntity>(), cancellationToken))
            .ReturnsAsync(new OwningDepartmentEntity());

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns(new OwningDepartmentDto());

        // Act
        await _service.CreateAsync(createDto, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<OwningDepartmentEntity>(), cancellationToken), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(cancellationToken), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateOwningDepartmentDto
        {
            OwningDepartmentName = "Updated Department",
            Description = "Updated Description"
        };

        var existingEntity = new OwningDepartmentEntity
        {
            Id = 1,
            OwningDepartmentName = "Old Department",
            Description = "Old Description",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwningDepartmentDto>(), It.IsAny<OwningDepartmentEntity>()))
            .Callback((UpdateOwningDepartmentDto src, OwningDepartmentEntity dest) =>
            {
                dest.OwningDepartmentName = src.OwningDepartmentName;
                dest.Description = src.Description;
            });

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns((OwningDepartmentEntity e) => new OwningDepartmentDto
            {
                Id = e.Id,
                OwningDepartmentName = e.OwningDepartmentName,
                Description = e.Description
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Department", result.OwningDepartmentName);
        Assert.Equal("Updated Description", result.Description);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("Updated Department", existingEntity.OwningDepartmentName);
        Assert.Equal("Updated Description", existingEntity.Description);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateOwningDepartmentDto
        {
            OwningDepartmentName = "Updated Department",
            Description = "Updated Description"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwningDepartmentEntity?)null);

        // Act
        var result = await _service.UpdateAsync(9999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithNullDescription_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateOwningDepartmentDto
        {
            OwningDepartmentName = "Sales Department",
            Description = null
        };

        var existingEntity = new OwningDepartmentEntity
        {
            Id = 5,
            OwningDepartmentName = "Old Sales",
            Description = "Old Description",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwningDepartmentDto>(), It.IsAny<OwningDepartmentEntity>()))
            .Callback((UpdateOwningDepartmentDto src, OwningDepartmentEntity dest) =>
            {
                dest.OwningDepartmentName = src.OwningDepartmentName;
                dest.Description = src.Description;
            });

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns((OwningDepartmentEntity e) => new OwningDepartmentDto
            {
                Id = e.Id,
                OwningDepartmentName = e.OwningDepartmentName,
                Description = e.Description
            });

        // Act
        var result = await _service.UpdateAsync(5, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Sales Department", result.OwningDepartmentName);
        Assert.Null(result.Description);
        Assert.Null(existingEntity.Description);
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyDescription_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateOwningDepartmentDto
        {
            OwningDepartmentName = "Marketing Department",
            Description = string.Empty
        };

        var existingEntity = new OwningDepartmentEntity
        {
            Id = 3,
            OwningDepartmentName = "Old Marketing",
            Description = "Old Description",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwningDepartmentDto>(), It.IsAny<OwningDepartmentEntity>()))
            .Callback((UpdateOwningDepartmentDto src, OwningDepartmentEntity dest) =>
            {
                dest.OwningDepartmentName = src.OwningDepartmentName;
                dest.Description = src.Description;
            });

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns((OwningDepartmentEntity e) => new OwningDepartmentDto
            {
                Id = e.Id,
                OwningDepartmentName = e.OwningDepartmentName,
                Description = e.Description
            });

        // Act
        var result = await _service.UpdateAsync(3, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Marketing Department", result.OwningDepartmentName);
        Assert.Equal(string.Empty, result.Description);
    }

    [Fact]
    public async Task UpdateAsync_PassesCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var updateDto = new UpdateOwningDepartmentDto
        {
            OwningDepartmentName = "Test Department"
        };

        var existingEntity = new OwningDepartmentEntity { Id = 1 };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, cancellationToken))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OwningDepartmentEntity>(), cancellationToken))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwningDepartmentDto>(), It.IsAny<OwningDepartmentEntity>()));

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns(new OwningDepartmentDto());

        // Act
        await _service.UpdateAsync(1, updateDto, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, cancellationToken), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<OwningDepartmentEntity>(), cancellationToken), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var updateDto = new UpdateOwningDepartmentDto
        {
            OwningDepartmentName = "New Name",
            Description = "New Description"
        };

        var existingEntity = new OwningDepartmentEntity
        {
            Id = 10,
            OwningDepartmentName = "Old Name",
            Description = "Old Description",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-30)
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwningDepartmentDto>(), It.IsAny<OwningDepartmentEntity>()))
            .Callback((UpdateOwningDepartmentDto src, OwningDepartmentEntity dest) =>
            {
                dest.OwningDepartmentName = src.OwningDepartmentName;
                dest.Description = src.Description;
            });

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns((OwningDepartmentEntity e) => new OwningDepartmentDto
            {
                Id = e.Id,
                OwningDepartmentName = e.OwningDepartmentName,
                Description = e.Description,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(10, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal("New Name", result.OwningDepartmentName);
        Assert.Equal("New Description", result.Description);
        Assert.True(result.IsActive);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndReturnsTrue()
    {
        // Arrange
        int idToDelete = 1;

        var existingEntity = new OwningDepartmentEntity
        {
            Id = idToDelete,
            OwningDepartmentName = "Department to Delete",
            Description = "Description",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
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
            .ReturnsAsync((OwningDepartmentEntity?)null);

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
            .ReturnsAsync((OwningDepartmentEntity?)null);

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

        var existingEntity = new OwningDepartmentEntity { Id = idToDelete };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, cancellationToken))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<OwningDepartmentEntity>(), cancellationToken))
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
            var entity = new OwningDepartmentEntity
            {
                Id = id,
                OwningDepartmentName = $"Department {id}"
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockRepository
                .Setup(r => r.DeleteAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(id, CancellationToken.None);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CreateThenGet_ReturnsCreatedEntity()
    {
        // Arrange
        var createDto = new CreateOwningDepartmentDto
        {
            OwningDepartmentName = "Integration Test Department",
            Description = "Integration Test Description"
        };

        var createdEntity = new OwningDepartmentEntity
        {
            Id = 100,
            OwningDepartmentName = createDto.OwningDepartmentName,
            Description = createDto.Description,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentEntity>(It.IsAny<CreateOwningDepartmentDto>()))
            .Returns(createdEntity);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEntity);

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns(new OwningDepartmentDto
            {
                Id = 100,
                OwningDepartmentName = createdEntity.OwningDepartmentName,
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
        Assert.Equal(createResult.OwningDepartmentName, getResult.OwningDepartmentName);
        Assert.Equal(createResult.Description, getResult.Description);
    }

    [Fact]
    public async Task UpdateThenGet_ReturnsUpdatedEntity()
    {
        // Arrange
        var existingEntity = new OwningDepartmentEntity
        {
            Id = 50,
            OwningDepartmentName = "Original Name",
            Description = "Original Description"
        };

        var updateDto = new UpdateOwningDepartmentDto
        {
            OwningDepartmentName = "Updated Name",
            Description = "Updated Description"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwningDepartmentDto>(), It.IsAny<OwningDepartmentEntity>()))
            .Callback((UpdateOwningDepartmentDto src, OwningDepartmentEntity dest) =>
            {
                dest.OwningDepartmentName = src.OwningDepartmentName;
                dest.Description = src.Description;
            });

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns((OwningDepartmentEntity e) => new OwningDepartmentDto
            {
                Id = e.Id,
                OwningDepartmentName = e.OwningDepartmentName,
                Description = e.Description
            });

        // Act
        var updateResult = await _service.UpdateAsync(50, updateDto, CancellationToken.None);
        var getResult = await _service.GetByIdAsync(50, CancellationToken.None);

        // Assert
        Assert.NotNull(updateResult);
        Assert.NotNull(getResult);
        Assert.Equal("Updated Name", updateResult.OwningDepartmentName);
        Assert.Equal("Updated Description", updateResult.Description);
        Assert.Equal(updateResult.OwningDepartmentName, getResult.OwningDepartmentName);
    }

    [Fact]
    public async Task DeleteThenGet_ReturnsNull()
    {
        // Arrange
        int idToDelete = 75;
        var existingEntity = new OwningDepartmentEntity
        {
            Id = idToDelete,
            OwningDepartmentName = "To Be Deleted"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<OwningDepartmentEntity, CancellationToken>((ent, ct) =>
            {
                _mockRepository
                    .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((OwningDepartmentEntity?)null);
            });

        // Act
        var deleteResult = await _service.DeleteAsync(idToDelete, CancellationToken.None);
        var getResult = await _service.GetByIdAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(deleteResult);
        Assert.Null(getResult);
    }

    #endregion

    #region IHardDeletable Tests

    [Fact]
    public void Entity_ImplementsIHardDeletable()
    {
        // Arrange & Act
        var entity = new OwningDepartmentEntity();

        // Assert
        Assert.IsAssignableFrom<IHardDeletable>(entity);
    }

    [Fact]
    public void Entity_MarkedForDeletion_DefaultsToFalse()
    {
        // Arrange & Act
        var entity = new OwningDepartmentEntity
        {
            OwningDepartmentName = "Test Department"
        };

        // Assert
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Entity_CanSetMarkedForDeletion()
    {
        // Arrange
        var entity = new OwningDepartmentEntity
        {
            OwningDepartmentName = "Test Department"
        };
        var deletionDate = DateTime.Now;

        // Act
        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = deletionDate;

        // Assert
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(deletionDate, entity.MarkedForDeletionDate);
    }

    [Fact]
    public async Task DeleteAsync_SoftDelete_MarksEntityForDeletion()
    {
        // Arrange
        var entity = new OwningDepartmentEntity
        {
            Id = 1,
            OwningDepartmentName = "IT Department",
            Description = "Information Technology",
            IsActive = true,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
            .Callback<OwningDepartmentEntity, CancellationToken>((e, ct) =>
            {
                if (e is IHardDeletable hardDeletable)
                {
                    hardDeletable.MarkedForDeletion = true;
                    hardDeletable.MarkedForDeletionDate = DateTime.Now;
                }
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        Assert.True(entity.MarkedForDeletion);
        Assert.NotNull(entity.MarkedForDeletionDate);
        Assert.True((DateTime.Now - entity.MarkedForDeletionDate.Value).TotalSeconds < 2);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_AlreadyMarkedForDeletion_StillSucceeds()
    {
        // Arrange
        var originalDeletionDate = DateTime.Now.AddDays(-1);
        var entity = new OwningDepartmentEntity
        {
            Id = 1,
            OwningDepartmentName = "IT Department",
            MarkedForDeletion = true,
            MarkedForDeletionDate = originalDeletionDate
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        Assert.True(entity.MarkedForDeletion);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DoesNotSetMarkedForDeletion()
    {
        // Arrange
        var createDto = new CreateOwningDepartmentDto
        {
            OwningDepartmentName = "New Department",
            Description = "New Description",
            CreatedBy = 1
        };

        var createdEntity = new OwningDepartmentEntity
        {
            Id = 100,
            OwningDepartmentName = createDto.OwningDepartmentName,
            Description = createDto.Description,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentEntity>(It.IsAny<CreateOwningDepartmentDto>()))
            .Returns(createdEntity);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwningDepartmentEntity e, CancellationToken _) =>
            {
                e.Id = 100;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns(new OwningDepartmentDto
            {
                Id = 100,
                OwningDepartmentName = createdEntity.OwningDepartmentName,
                Description = createdEntity.Description
            });

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.False(createdEntity.MarkedForDeletion);
        Assert.Null(createdEntity.MarkedForDeletionDate);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotModifyMarkedForDeletion()
    {
        // Arrange
        var existingEntity = new OwningDepartmentEntity
        {
            Id = 1,
            OwningDepartmentName = "Original Name",
            Description = "Original Description",
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };

        var updateDto = new UpdateOwningDepartmentDto
        {
            OwningDepartmentName = "Updated Name",
            Description = "Updated Description",
            UpdatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOwningDepartmentDto>(), It.IsAny<OwningDepartmentEntity>()))
            .Callback<UpdateOwningDepartmentDto, OwningDepartmentEntity>((dto, entity) =>
            {
                entity.OwningDepartmentName = dto.OwningDepartmentName;
                entity.Description = dto.Description;
                // MarkedForDeletion should remain unchanged
            });

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OwningDepartmentEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map<OwningDepartmentDto>(It.IsAny<OwningDepartmentEntity>()))
            .Returns(new OwningDepartmentDto
            {
                Id = 1,
                OwningDepartmentName = "Updated Name",
                Description = "Updated Description"
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.False(existingEntity.MarkedForDeletion);
        Assert.Null(existingEntity.MarkedForDeletionDate);
    }

    #endregion
}

public class OwningDepartmentControllerTests
{
    private static OwningDepartmentController Create(
        out Mock<IOwningDepartmentService> service,
        out Mock<IHardDeleteCleanupService> cleanupService,
        out Mock<IReferenceValidationService> referenceValidationService)
    {
        service = new Mock<IOwningDepartmentService>();
        cleanupService = new Mock<IHardDeleteCleanupService>();
        referenceValidationService = new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<OwningDepartmentController>>();
        return new OwningDepartmentController(service.Object, cleanupService.Object, referenceValidationService.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        var query = new OwningDepartmentQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<OwningDepartmentDto>(new List<OwningDepartmentDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OwningDepartmentDto { Id = 1 });

        var result = await controller.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        var dto = new CreateOwningDepartmentDto { OwningDepartmentName = "IT" };
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new OwningDepartmentDto { Id = 1 });

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        var dto = new UpdateOwningDepartmentDto { OwningDepartmentName = "Updated IT" };
        service.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>())).ReturnsAsync(new OwningDepartmentDto { Id = 1 });

        var result = await controller.Update(1, dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}



