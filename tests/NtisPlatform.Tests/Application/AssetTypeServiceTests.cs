using NtisPlatform.Application.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Mappings;
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
/// Comprehensive tests for AssetTypeService.
/// Tests all CRUD operations with various scenarios.
/// </summary>
public class AssetTypeServiceTests
{
    private readonly Mock<IRepository<AssetTypeEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly AssetTypeService _service;

    public AssetTypeServiceTests()
    {
        _mockRepository = new Mock<IRepository<AssetTypeEntity, int>>();
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

        _mockUnitOfWork
            .Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<AssetTypeEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());

        _service = new AssetTypeService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object, _mockReferenceValidator.Object);
    }

    private static IMapper CreateRealMapper()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetTypeMappingProfile>();
            cfg.AllowNullDestinationValues = true;
        }, NullLoggerFactory.Instance);

        // Skip validation to allow unmapped destination members (like Id)
        return mapperConfig.CreateMapper();
    }

    private static AssetTypeEntity CreateEntity(
        int id = 1,
        int AssetCategoryId = 10,
        string typeCode = "BLD",
        string typeName = "Building",
        string? typeNameLocal = null,
        string? description = null,
        string? icon = null,
        string codeFormat = "BLD-{000}",
        int lastSequence = 0,
        bool isActive = true,
        bool markedForDeletion = false)
    {
        return new AssetTypeEntity
        {
            Id = id,
            AssetCategoryId = AssetCategoryId,
            TypeCode = typeCode,
            TypeName = typeName,
            TypeNameLocal = typeNameLocal,
            Description = description,
            Icon = icon,
            CodeFormat = codeFormat,
            LastSequence = lastSequence,
            RowVersion = null,
            IsActive = isActive,
            MarkedForDeletion = markedForDeletion,
            MarkedForDeletionDate = markedForDeletion ? DateTime.Now : null,
            CreatedBy = 1,
            UpdatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var service = new AssetTypeService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object, _mockReferenceValidator.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = CreateEntity(
            id: 1,
            AssetCategoryId: 100,
            typeName: "Vehicle",
            typeCode: "VEH",
            codeFormat: "VEH-{000}");

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns(new AssetTypeDto
            {
                Id = 1,
                AssetCategoryId = 100,
                TypeName = "Vehicle",
                TypeCode = "VEH",
                CodeFormat = "VEH-{000}",
                IsActive = true,
                CreatedBy = 1,
                UpdatedBy = 1
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(100, result.AssetCategoryId);
        Assert.Equal("Vehicle", result.TypeName);
        Assert.Equal("VEH", result.TypeCode);
        Assert.Equal("VEH-{000}", result.CodeFormat);
        Assert.True(result.IsActive);
        Assert.Equal(1, result.CreatedBy);
        Assert.Equal(1, result.UpdatedBy);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetTypeEntity?)null);

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
        _mockRepository
            .Setup(r => r.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetTypeEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(0);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_PassesCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var entity = CreateEntity(id: 1, typeName: "Test");

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, cancellationToken))
            .ReturnsAsync(entity);

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(entity))
            .Returns(new AssetTypeDto { Id = 1, TypeName = "Test", CodeFormat = "TST-{000}" });

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
        var entities = new List<AssetTypeEntity>
        {
            CreateEntity(id: 1, AssetCategoryId: 10, typeName: "Building", typeCode: "BLD", codeFormat: "BLD-{000}"),
            CreateEntity(id: 2, AssetCategoryId: 20, typeName: "Vehicle", typeCode: "VEH", codeFormat: "VEH-{000}")
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var service = new AssetTypeService(_mockRepository.Object, _mockUnitOfWork.Object, CreateRealMapper(), _mockReferenceValidator.Object);

        var queryParams = new AssetTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = FilterLogic.And,
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
        Assert.Contains(items, x => x.TypeName == "Building" && x.TypeCode == "BLD");
        Assert.Contains(items, x => x.TypeName == "Vehicle" && x.TypeCode == "VEH");
    }

    [Fact]
    public async Task GetAllAsync_EmptyRepository_ReturnsEmptyResult()
    {
        // Arrange
        var entities = new List<AssetTypeEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var service = new AssetTypeService(_mockRepository.Object, _mockUnitOfWork.Object, CreateRealMapper(), _mockReferenceValidator.Object);

        var queryParams = new AssetTypeQueryParameters
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
        var entities = new List<AssetTypeEntity>();
        for (int i = 1; i <= 25; i++)
        {
            entities.Add(CreateEntity(
                id: i,
                AssetCategoryId: 100 + i,
                typeName: $"Asset Type {i}",
                typeCode: $"AT{i}",
                codeFormat: $"AT{i}-{{000}}"));
        }

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var service = new AssetTypeService(_mockRepository.Object, _mockUnitOfWork.Object, CreateRealMapper(), _mockReferenceValidator.Object);

        var queryParams = new AssetTypeQueryParameters
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
    public async Task GetAllAsync_WithIsActiveFilter_ReturnsFilteredResults()
    {
        // Arrange
        var entities = new List<AssetTypeEntity>
        {
            CreateEntity(id: 1, typeName: "Building", typeCode: "BLD", isActive: true),
            CreateEntity(id: 2, typeName: "Vehicle", typeCode: "VEH", isActive: true),
            CreateEntity(id: 3, typeName: "Inactive Type", typeCode: "INA", isActive: false)
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var service = new AssetTypeService(_mockRepository.Object, _mockUnitOfWork.Object, CreateRealMapper(), _mockReferenceValidator.Object);

        var queryParams = new AssetTypeQueryParameters
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
    public async Task GetAllAsync_WithTypeNameFilter_ReturnsFilteredResults()
    {
        // Arrange
        var entities = new List<AssetTypeEntity>
        {
            CreateEntity(id: 1, typeName: "Building", typeCode: "BLD"),
            CreateEntity(id: 2, typeName: "Vehicle", typeCode: "VEH"),
            CreateEntity(id: 3, typeName: "Furniture", typeCode: "FUR")
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var service = new AssetTypeService(_mockRepository.Object, _mockUnitOfWork.Object, CreateRealMapper(), _mockReferenceValidator.Object);

        var queryParams = new AssetTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            TypeName = "Vehicle"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Vehicle", result.Items.First().TypeName);
    }

    [Fact]
    public async Task GetAllAsync_WithTypeCodeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var entities = new List<AssetTypeEntity>
        {
            CreateEntity(id: 1, typeName: "Building", typeCode: "BLD"),
            CreateEntity(id: 2, typeName: "Vehicle", typeCode: "VEH"),
            CreateEntity(id: 3, typeName: "Furniture", typeCode: "FUR")
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var service = new AssetTypeService(_mockRepository.Object, _mockUnitOfWork.Object, CreateRealMapper(), _mockReferenceValidator.Object);

        var queryParams = new AssetTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            TypeCode = "FUR"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("FUR", result.Items.First().TypeCode);
    }

    [Fact]
    public async Task GetAllAsync_PassesCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var entities = new List<AssetTypeEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var service = new AssetTypeService(_mockRepository.Object, _mockUnitOfWork.Object, CreateRealMapper(), _mockReferenceValidator.Object);

        var queryParams = new AssetTypeQueryParameters
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
        var createDto = new CreateAssetTypeDto
        {
            AssetCategoryId = 10,
            TypeName = "Building",
            TypeCode = "BLD",
            CodeFormat = "BLD-{000}",
            IsSubUnit = true,
            AllowUnitRegistration = true,
            AllowRoomRegistration = false,
            AssetWardNo = "Ward-1"
        };

        _mockMapper
            .Setup(m => m.Map<AssetTypeEntity>(It.IsAny<CreateAssetTypeDto>()))
            .Returns((CreateAssetTypeDto dto) => new AssetTypeEntity
            {
                AssetCategoryId = dto.AssetCategoryId ?? 0,
                TypeName = dto.TypeName!,
                TypeCode = dto.TypeCode!,
                CodeFormat = dto.CodeFormat!,
                CreatedBy = 1,
                CreatedDate = DateTime.Now,
                IsActive = true,
                IsSubUnit = dto.IsSubUnit,
                AllowUnitRegistration = dto.AllowUnitRegistration,
                AllowRoomRegistration = dto.AllowRoomRegistration,
                AssetWardNo = dto.AssetWardNo
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetTypeEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns((AssetTypeEntity e) => new AssetTypeDto
            {
                AssetCategoryId = e.AssetCategoryId,
                TypeName = e.TypeName,
                TypeCode = e.TypeCode,
                CodeFormat = e.CodeFormat,
                IsActive = true,
                IsSubUnit = e.IsSubUnit,
                AllowUnitRegistration = e.AllowUnitRegistration,
                AllowRoomRegistration = e.AllowRoomRegistration,
                AssetWardNo = e.AssetWardNo
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.AssetCategoryId);
        Assert.Equal("Building", result.TypeName);
        Assert.Equal("BLD", result.TypeCode);
        Assert.Equal("BLD-{000}", result.CodeFormat);
        Assert.True(result.IsActive);
        Assert.True(result.IsSubUnit);
        Assert.True(result.AllowUnitRegistration);
        Assert.False(result.AllowRoomRegistration);
        Assert.Equal("Ward-1", result.AssetWardNo);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithLongCodeFormat_CreatesSuccessfully()
    {
        // Arrange
        var longCodeFormat = $"EQP-{new string('X', 50)}-{{000}}";
        var createDto = new CreateAssetTypeDto
        {
            AssetCategoryId = 30,
            TypeName = "Equipment",
            TypeCode = "EQP",
            CodeFormat = longCodeFormat
        };

        _mockMapper
            .Setup(m => m.Map<AssetTypeEntity>(It.IsAny<CreateAssetTypeDto>()))
            .Returns(new AssetTypeEntity
            {
                AssetCategoryId = createDto.AssetCategoryId.Value, // FIX: Added .Value
                TypeName = createDto.TypeName!,
                TypeCode = createDto.TypeCode!,
                CodeFormat = createDto.CodeFormat!,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetTypeEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns(new AssetTypeDto
            {
                AssetCategoryId = createDto.AssetCategoryId.Value,
                TypeName = createDto.TypeName!,
                TypeCode = createDto.TypeCode!,
                CodeFormat = createDto.CodeFormat!
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Equipment", result.TypeName);
        Assert.Equal(longCodeFormat, result.CodeFormat);
    }

    [Fact]
    public async Task CreateAsync_CallsMapperWithCorrectDto()
    {
        // Arrange
        var createDto = new CreateAssetTypeDto
        {
            AssetCategoryId = 40,
            TypeName = "Furniture",
            TypeCode = "FUR",
            CodeFormat = "FUR-{000}"
        };

        CreateAssetTypeDto? capturedDto = null;

        _mockMapper
            .Setup(m => m.Map<AssetTypeEntity>(It.IsAny<object>()))
            .Callback<object>(dto => capturedDto = dto as CreateAssetTypeDto)
            .Returns(new AssetTypeEntity());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetTypeEntity());

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns(new AssetTypeDto { TypeName = "Furniture", CodeFormat = "FUR-{000}" });

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedDto);
        Assert.Equal(40, capturedDto.AssetCategoryId);
        Assert.Equal("Furniture", capturedDto.TypeName);
        Assert.Equal("FUR", capturedDto.TypeCode);
        Assert.Equal("FUR-{000}", capturedDto.CodeFormat);
    }

    [Fact]
    public async Task CreateAsync_PassesCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var createDto = new CreateAssetTypeDto
        {
            AssetCategoryId = 10,
            TypeName = "Test Type",
            TypeCode = "TST",
            CodeFormat = "TST-{000}"
        };

        _mockMapper
            .Setup(m => m.Map<AssetTypeEntity>(It.IsAny<CreateAssetTypeDto>()))
            .Returns(new AssetTypeEntity());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AssetTypeEntity>(), cancellationToken))
            .ReturnsAsync(new AssetTypeEntity());

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns(new AssetTypeDto { TypeName = "Test Type", CodeFormat = "TST-{000}" });

        // Act
        await _service.CreateAsync(createDto, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<AssetTypeEntity>(), cancellationToken), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithSpecialCharacters_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateAssetTypeDto
        {
            AssetCategoryId = 50,
            TypeName = "Plant & Machinery",
            TypeCode = "P&M",
            CodeFormat = "P&M/{YYYY}/{000}"
        };

        _mockMapper
            .Setup(m => m.Map<AssetTypeEntity>(It.IsAny<CreateAssetTypeDto>()))
            .Returns(new AssetTypeEntity
            {
                AssetCategoryId = createDto.AssetCategoryId.Value, // FIX: Added .Value
                TypeName = createDto.TypeName!,
                TypeCode = createDto.TypeCode!,
                CodeFormat = createDto.CodeFormat!,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetTypeEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns(new AssetTypeDto
            {
                AssetCategoryId = createDto.AssetCategoryId.Value,
                TypeName = createDto.TypeName!,
                TypeCode = createDto.TypeCode!,
                CodeFormat = createDto.CodeFormat!
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Plant & Machinery", result.TypeName);
        Assert.Equal("P&M", result.TypeCode);
        Assert.Equal("P&M/{YYYY}/{000}", result.CodeFormat);
    }

    [Fact]
    public async Task CreateAsync_WithTypeNameLocal_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateAssetTypeDto
        {
            AssetCategoryId = 60,
            TypeCode = "VEH",
            TypeName = "Vehicle",
            TypeNameLocal = "????",
            CodeFormat = "VEH-{000}"
        };

        _mockMapper
            .Setup(m => m.Map<AssetTypeEntity>(It.IsAny<CreateAssetTypeDto>()))
            .Returns(new AssetTypeEntity
            {
                AssetCategoryId = createDto.AssetCategoryId.Value, // FIX: Added .Value
                TypeCode = createDto.TypeCode!,
                TypeName = createDto.TypeName!,
                TypeNameLocal = createDto.TypeNameLocal,
                CodeFormat = createDto.CodeFormat!,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetTypeEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns(new AssetTypeDto
            {
                AssetCategoryId = createDto.AssetCategoryId.Value,
                TypeCode = createDto.TypeCode!,
                TypeName = createDto.TypeName!,
                TypeNameLocal = createDto.TypeNameLocal,
                CodeFormat = createDto.CodeFormat!
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Vehicle", result.TypeName);
        Assert.Equal("????", result.TypeNameLocal);
        Assert.Equal("VEH", result.TypeCode);
    }

    [Fact]
    public async Task CreateAsync_WithIcon_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateAssetTypeDto
        {
            AssetCategoryId = 70,
            TypeCode = "IT",
            TypeName = "IT Equipment",
            Icon = "fas fa-laptop",
            CodeFormat = "IT-{000}"
        };

        _mockMapper
            .Setup(m => m.Map<AssetTypeEntity>(It.IsAny<CreateAssetTypeDto>()))
            .Returns(new AssetTypeEntity
            {
                AssetCategoryId = createDto.AssetCategoryId.Value, // FIX: Added .Value
                TypeCode = createDto.TypeCode!,
                TypeName = createDto.TypeName!,
                Icon = createDto.Icon,
                CodeFormat = createDto.CodeFormat!,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetTypeEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns(new AssetTypeDto
            {
                AssetCategoryId = createDto.AssetCategoryId.Value,
                TypeCode = createDto.TypeCode!,
                TypeName = createDto.TypeName!,
                Icon = createDto.Icon,
                CodeFormat = createDto.CodeFormat!
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("IT Equipment", result.TypeName);
        Assert.Equal("fas fa-laptop", result.Icon);
        Assert.Equal("IT", result.TypeCode);
    }

    [Fact]
    public async Task CreateAsync_WithAllNewFields_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateAssetTypeDto
        {
            AssetCategoryId = 80,
            TypeCode = "OFF",
            TypeName = "Office Furniture",
            TypeNameLocal = "???????? ???????",
            Description = "Furniture for office use",
            Icon = "fas fa-chair",
            CodeFormat = "OFF-{000}"
        };

        _mockMapper
            .Setup(m => m.Map<AssetTypeEntity>(It.IsAny<CreateAssetTypeDto>()))
            .Returns(new AssetTypeEntity
            {
                AssetCategoryId = createDto.AssetCategoryId.Value, // FIX: Added .Value
                TypeCode = createDto.TypeCode!,
                TypeName = createDto.TypeName!,
                TypeNameLocal = createDto.TypeNameLocal,
                Description = createDto.Description,
                Icon = createDto.Icon,
                CodeFormat = createDto.CodeFormat!,
                LastSequence = 0,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetTypeEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns(new AssetTypeDto
            {
                AssetCategoryId = createDto.AssetCategoryId.Value,
                TypeCode = createDto.TypeCode!,
                TypeName = createDto.TypeName!,
                TypeNameLocal = createDto.TypeNameLocal,
                Icon = createDto.Icon,
                CodeFormat = createDto.CodeFormat!,
                LastSequence = 0
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Office Furniture", result.TypeName);
        Assert.Equal("???????? ???????", result.TypeNameLocal);
        Assert.Equal("fas fa-chair", result.Icon);
        Assert.Equal("OFF", result.TypeCode);
        Assert.Equal(0, result.LastSequence);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateAssetTypeDto
        {
            AssetCategoryId = 15,
            TypeName = "Updated Building",
            TypeCode = "UBLD",
            TypeNameLocal = "Updated Local Name",
            Description = "Updated Description",
            Icon = "updated-icon.png",
            CodeFormat = "UBLD-{000}",
            IsSubUnit = false,
            AllowUnitRegistration = true,
            AllowRoomRegistration = true,
            AssetWardNo = "Ward-15",
            IsActive = true
        };

        var existingEntity = CreateEntity(
            id: 1,
            AssetCategoryId: 10,
            typeName: "Old Building",
            typeCode: "OLD",
            codeFormat: "OLD-{000}");

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssetTypeDto>(), It.IsAny<AssetTypeEntity>()))
            .Callback((UpdateAssetTypeDto src, AssetTypeEntity dest) =>
            {
                dest.AssetCategoryId = src.AssetCategoryId ?? dest.AssetCategoryId;
                dest.TypeName = src.TypeName!;
                dest.TypeCode = src.TypeCode!;
                dest.TypeNameLocal = src.TypeNameLocal;
                dest.Description = src.Description;
                dest.Icon = src.Icon;
                dest.CodeFormat = src.CodeFormat!;
                dest.IsActive = src.IsActive;
                dest.IsSubUnit = src.IsSubUnit;
                dest.AllowUnitRegistration = src.AllowUnitRegistration;
                dest.AllowRoomRegistration = src.AllowRoomRegistration;
                dest.AssetWardNo = src.AssetWardNo;
            });

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns((AssetTypeEntity e) => new AssetTypeDto
            {
                Id = e.Id,
                AssetCategoryId = e.AssetCategoryId,
                TypeName = e.TypeName,
                TypeCode = e.TypeCode,
                TypeNameLocal = e.TypeNameLocal,
                Description = e.Description,
                Icon = e.Icon,
                CodeFormat = e.CodeFormat,
                IsActive = e.IsActive,
                IsSubUnit = e.IsSubUnit,
                AllowUnitRegistration = e.AllowUnitRegistration,
                AllowRoomRegistration = e.AllowRoomRegistration,
                AssetWardNo = e.AssetWardNo
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(15, result.AssetCategoryId);
        Assert.Equal("Updated Building", result.TypeName);
        Assert.Equal("UBLD", result.TypeCode);
        Assert.False(result.IsSubUnit);
        Assert.True(result.AllowUnitRegistration);
        Assert.True(result.AllowRoomRegistration);
        Assert.Equal("Ward-15", result.AssetWardNo);
        Assert.Equal("Updated Local Name", result.TypeNameLocal);
        Assert.Equal("Updated Description", result.Description);
        Assert.Equal("updated-icon.png", result.Icon);
        Assert.Equal("UBLD-{000}", result.CodeFormat);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal(15, existingEntity.AssetCategoryId);
        Assert.Equal("Updated Building", existingEntity.TypeName);
        Assert.Equal("UBLD", existingEntity.TypeCode);
        Assert.Equal("Updated Local Name", existingEntity.TypeNameLocal);
        Assert.Equal("Updated Description", existingEntity.Description);
        Assert.Equal("updated-icon.png", existingEntity.Icon);
        Assert.Equal("UBLD-{000}", existingEntity.CodeFormat);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateAssetTypeDto
        {
            TypeName = "Updated Type",
            TypeCode = "UPD",
            CodeFormat = "UPD-{000}"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetTypeEntity?)null);

        // Act
        var result = await _service.UpdateAsync(9999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithDifferentTypeCode_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateAssetTypeDto
        {
            TypeName = "Furniture",
            TypeCode = "FUR",
            CodeFormat = "FUR-{000}"
        };

        var existingEntity = CreateEntity(
            id: 3,
            AssetCategoryId: 30,
            typeName: "Old Furniture",
            typeCode: "OLDFUR",
            codeFormat: "OLDFUR-{000}");

        _mockRepository
            .Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssetTypeDto>(), It.IsAny<AssetTypeEntity>()))
            .Callback((UpdateAssetTypeDto src, AssetTypeEntity dest) =>
            {
                dest.TypeName = src.TypeName!;
                dest.TypeCode = src.TypeCode!;
                dest.CodeFormat = src.CodeFormat!;
            });

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns((AssetTypeEntity e) => new AssetTypeDto
            {
                Id = e.Id,
                AssetCategoryId = e.AssetCategoryId,
                TypeName = e.TypeName,
                TypeCode = e.TypeCode,
                CodeFormat = e.CodeFormat
            });

        // Act
        var result = await _service.UpdateAsync(3, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Furniture", result.TypeName);
        Assert.Equal("FUR", result.TypeCode);
        Assert.Equal("FUR-{000}", result.CodeFormat);
    }

    [Fact]
    public async Task UpdateAsync_PassesCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var updateDto = new UpdateAssetTypeDto
        {
            TypeName = "Test Type",
            TypeCode = "TST",
            CodeFormat = "TST-{000}"
        };

        var existingEntity = CreateEntity(id: 1);

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, cancellationToken))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<AssetTypeEntity>(), cancellationToken))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssetTypeDto>(), It.IsAny<AssetTypeEntity>()));

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns(new AssetTypeDto { TypeName = "Test Type", CodeFormat = "TST-{000}" });

        // Act
        await _service.UpdateAsync(1, updateDto, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, cancellationToken), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<AssetTypeEntity>(), cancellationToken), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var updateDto = new UpdateAssetTypeDto
        {
            TypeName = "New Name",
            TypeCode = "NEW",
            CodeFormat = "NEW-{000}"
        };

        var existingEntity = CreateEntity(
            id: 10,
            AssetCategoryId: 99,
            typeName: "Old Name",
            typeCode: "OLD",
            codeFormat: "OLD-{000}");

        _mockRepository
            .Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssetTypeDto>(), It.IsAny<AssetTypeEntity>()))
            .Callback((UpdateAssetTypeDto src, AssetTypeEntity dest) =>
            {
                dest.TypeName = src.TypeName!;
                dest.TypeCode = src.TypeCode!;
                dest.CodeFormat = src.CodeFormat!;
            });

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns((AssetTypeEntity e) => new AssetTypeDto
            {
                Id = e.Id,
                AssetCategoryId = e.AssetCategoryId,
                TypeName = e.TypeName,
                TypeCode = e.TypeCode,
                CodeFormat = e.CodeFormat,
                IsActive = e.IsActive,
                CreatedBy = e.CreatedBy
            });

        // Act
        var result = await _service.UpdateAsync(10, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal(99, result.AssetCategoryId);
        Assert.Equal("New Name", result.TypeName);
        Assert.Equal("NEW", result.TypeCode);
        Assert.Equal("NEW-{000}", result.CodeFormat);
        Assert.True(result.IsActive);
        Assert.Equal(1, result.CreatedBy);
    }

    [Fact]
    public async Task UpdateAsync_MultipleUpdatesOnSameEntity_LastUpdateWins()
    {
        // Arrange
        var firstUpdateDto = new UpdateAssetTypeDto
        {
            TypeName = "First Update",
            TypeCode = "FST",
            CodeFormat = "FST-{000}"
        };

        var secondUpdateDto = new UpdateAssetTypeDto
        {
            TypeName = "Second Update",
            TypeCode = "SND",
            CodeFormat = "SND-{000}"
        };

        var existingEntity = CreateEntity(
            id: 7,
            typeName: "Original",
            typeCode: "ORG",
            codeFormat: "ORG-{000}");

        _mockRepository
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssetTypeDto>(), It.IsAny<AssetTypeEntity>()))
            .Callback((UpdateAssetTypeDto src, AssetTypeEntity dest) =>
            {
                dest.TypeName = src.TypeName!;
                dest.TypeCode = src.TypeCode!;
                dest.CodeFormat = src.CodeFormat!;
            });

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns((AssetTypeEntity e) => new AssetTypeDto
            {
                Id = e.Id,
                AssetCategoryId = e.AssetCategoryId,
                TypeName = e.TypeName,
                TypeCode = e.TypeCode,
                CodeFormat = e.CodeFormat
            });

        // Act
        await _service.UpdateAsync(7, firstUpdateDto, CancellationToken.None);
        var result = await _service.UpdateAsync(7, secondUpdateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Second Update", result.TypeName);
        Assert.Equal("SND", result.TypeCode);
        Assert.Equal("SND-{000}", result.CodeFormat);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingEntityWithReferences_ThrowsValidationException()
    {
        // Arrange
        var updateDto = new UpdateAssetTypeDto
        {
            TypeName = "Updated Building",
            TypeCode = "UBLD",
            CodeFormat = "UBLD-{000}",
            IsActive = false
        };

        var existingEntity = CreateEntity(
            id: 1,
            AssetCategoryId: 10,
            typeName: "Old Building",
            typeCode: "OLD",
            codeFormat: "OLD-{000}",
            isActive: true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(rv => rv.ValidateReferencesAsync<AssetTypeEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Failure("Cannot deactivate asset type with active references"));

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssetTypeDto>(), It.IsAny<AssetTypeEntity>()))
            .Callback((UpdateAssetTypeDto src, AssetTypeEntity dest) =>
            {
                dest.TypeName = src.TypeName!;
                dest.TypeCode = src.TypeCode!;
                dest.CodeFormat = src.CodeFormat!;
                dest.IsActive = src.IsActive;
            });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains("Cannot deactivate", exception.Message);
        _mockReferenceValidator.Verify(
            rv => rv.ValidateReferencesAsync<AssetTypeEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingEntityWithoutReferences_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateAssetTypeDto
        {
            TypeName = "Updated Building",
            TypeCode = "UBLD",
            CodeFormat = "UBLD-{000}",
            IsActive = false
        };

        var existingEntity = CreateEntity(
            id: 1,
            AssetCategoryId: 10,
            typeName: "Old Building",
            typeCode: "OLD",
            codeFormat: "OLD-{000}",
            isActive: true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(rv => rv.ValidateReferencesAsync<AssetTypeEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssetTypeDto>(), It.IsAny<AssetTypeEntity>()))
            .Callback((UpdateAssetTypeDto src, AssetTypeEntity dest) =>
            {
                dest.TypeName = src.TypeName!;
                dest.TypeCode = src.TypeCode!;
                dest.CodeFormat = src.CodeFormat!;
                dest.IsActive = src.IsActive;
            });

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns((AssetTypeEntity e) => new AssetTypeDto
            {
                Id = e.Id,
                AssetCategoryId = e.AssetCategoryId,
                TypeName = e.TypeName,
                TypeCode = e.TypeCode,
                CodeFormat = e.CodeFormat,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        _mockReferenceValidator.Verify(
            rv => rv.ValidateReferencesAsync<AssetTypeEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndReturnsTrue()
    {
        // Arrange
        int idToDelete = 1;
        var existingEntity = CreateEntity(id: idToDelete, typeName: "Type to Delete");

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()))
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
            .ReturnsAsync((AssetTypeEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
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
            .ReturnsAsync((AssetTypeEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_PassesCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        int idToDelete = 5;
        var existingEntity = CreateEntity(id: idToDelete);

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, cancellationToken))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(existingEntity, cancellationToken))
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
            var entity = CreateEntity(id: id, typeName: $"Asset Type {id}");

            _mockRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockRepository
                .Setup(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>()))
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
        var activeEntity = CreateEntity(id: idToDelete, typeName: "Active Type", isActive: true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(activeEntity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(activeEntity, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CreateThenGet_ReturnsCreatedEntity()
    {
        // Arrange
        var createDto = new CreateAssetTypeDto
        {
            AssetCategoryId = 10,
            TypeName = "Integration Test Type",
            TypeCode = "ITT",
            CodeFormat = "ITT-{000}"
        };

        var createdEntity = CreateEntity(
            id: 100,
            AssetCategoryId: createDto.AssetCategoryId.Value, // FIX: Added .Value
            typeName: createDto.TypeName!,
            typeCode: createDto.TypeCode!,
            codeFormat: createDto.CodeFormat!);

        _mockMapper
            .Setup(m => m.Map<AssetTypeEntity>(It.IsAny<CreateAssetTypeDto>()))
            .Returns(createdEntity);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEntity);

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns(new AssetTypeDto
            {
                Id = 100,
                AssetCategoryId = createdEntity.AssetCategoryId,
                TypeName = createdEntity.TypeName,
                TypeCode = createdEntity.TypeCode,
                CodeFormat = createdEntity.CodeFormat
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
        Assert.Equal(createResult.TypeName, getResult.TypeName);
        Assert.Equal(createResult.TypeCode, getResult.TypeCode);
        Assert.Equal(createResult.CodeFormat, getResult.CodeFormat);
    }

    [Fact]
    public async Task UpdateThenGet_ReturnsUpdatedEntity()
    {
        // Arrange
        var existingEntity = CreateEntity(
            id: 50,
            typeName: "Original Name",
            typeCode: "ORG",
            codeFormat: "ORG-{000}");

        var updateDto = new UpdateAssetTypeDto
        {
            TypeName = "Updated Name",
            TypeCode = "UPD",
            CodeFormat = "UPD-{000}"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssetTypeDto>(), It.IsAny<AssetTypeEntity>()))
            .Callback((UpdateAssetTypeDto src, AssetTypeEntity dest) =>
            {
                dest.TypeName = src.TypeName!;
                dest.TypeCode = src.TypeCode!;
                dest.CodeFormat = src.CodeFormat!;
            });

        _mockMapper
            .Setup(m => m.Map<AssetTypeDto>(It.IsAny<AssetTypeEntity>()))
            .Returns((AssetTypeEntity e) => new AssetTypeDto
            {
                Id = e.Id,
                AssetCategoryId = e.AssetCategoryId,
                TypeName = e.TypeName,
                TypeCode = e.TypeCode,
                CodeFormat = e.CodeFormat
            });

        // Act
        var updateResult = await _service.UpdateAsync(50, updateDto, CancellationToken.None);
        var getResult = await _service.GetByIdAsync(50, CancellationToken.None);

        // Assert
        Assert.NotNull(updateResult);
        Assert.NotNull(getResult);
        Assert.Equal("Updated Name", updateResult.TypeName);
        Assert.Equal("UPD", updateResult.TypeCode);
        Assert.Equal("UPD-{000}", updateResult.CodeFormat);
        Assert.Equal(updateResult.TypeName, getResult.TypeName);
        Assert.Equal(updateResult.TypeCode, getResult.TypeCode);
    }

    [Fact]
    public async Task DeleteThenGet_ReturnsNull()
    {
        // Arrange
        int idToDelete = 75;
        var existingEntity = CreateEntity(id: idToDelete, typeName: "To Be Deleted");

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(existingEntity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() =>
            {
                _mockRepository
                    .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((AssetTypeEntity?)null);
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
            new CreateAssetTypeDto { AssetCategoryId = 1, TypeName = "Type 1", TypeCode = "T1", CodeFormat = "T1-{000}" },
            new CreateAssetTypeDto { AssetCategoryId = 2, TypeName = "Type 2", TypeCode = "T2", CodeFormat = "T2-{000}" },
            new CreateAssetTypeDto { AssetCategoryId = 3, TypeName = "Type 3", TypeCode = "T3", CodeFormat = "T3-{000}" }
        };

        foreach (var dto in createDtos)
        {
            var entity = new AssetTypeEntity
            {
                AssetCategoryId = dto.AssetCategoryId.Value, // FIX: Added .Value
                TypeName = dto.TypeName!,
                TypeCode = dto.TypeCode!,
                CodeFormat = dto.CodeFormat!
            };

            _mockMapper
                .Setup(m => m.Map<AssetTypeEntity>(It.Is<CreateAssetTypeDto>(d => d.TypeName == dto.TypeName)))
                .Returns(entity);

            _mockRepository
                .Setup(r => r.AddAsync(It.Is<AssetTypeEntity>(e => e.TypeName == dto.TypeName), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockMapper
                .Setup(m => m.Map<AssetTypeDto>(It.Is<AssetTypeEntity>(e => e.TypeName == dto.TypeName)))
                .Returns(new AssetTypeDto
                {
                    AssetCategoryId = entity.AssetCategoryId,
                    TypeName = entity.TypeName,
                    TypeCode = entity.TypeCode,
                    CodeFormat = entity.CodeFormat
                });
        }

        // Act
        var results = new List<AssetTypeDto>();
        foreach (var dto in createDtos)
        {
            var result = await _service.CreateAsync(dto, CancellationToken.None);
            results.Add(result);
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.TypeName == "Type 1" && r.TypeCode == "T1");
        Assert.Contains(results, r => r.TypeName == "Type 2" && r.TypeCode == "T2");
        Assert.Contains(results, r => r.TypeName == "Type 3" && r.TypeCode == "T3");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<AssetTypeEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    #endregion

    #region Entity Properties Coverage Test

    [Fact]
    public void TestAllProperties_ToEnsureCodeCoverage()
    {
        var entity = new AssetTypeEntity
        {
            Id = 1,
            AssetCategoryId = 2,
            TypeCode = "EQP",
            TypeName = "Equipment",
            TypeNameLocal = "Local Name",
            Description = "Description",
            Icon = "icon.png",
            CodeFormat = "FORMAT",
            LastSequence = 5,
            RowVersion = new byte[] { 1, 2, 3 },
            AllowUnitRegistration = true,
            AllowRoomRegistration = true,
            AssetWardNo = "Ward 1",
            IsActive = true,
            MarkedForDeletion = false
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(2, entity.AssetCategoryId);
        Assert.Equal("EQP", entity.TypeCode);
        Assert.Equal("Equipment", entity.TypeName);
        Assert.Equal("Local Name", entity.TypeNameLocal);
        Assert.Equal("Description", entity.Description);
        Assert.Equal("icon.png", entity.Icon);
        Assert.Equal("FORMAT", entity.CodeFormat);
        Assert.Equal(5, entity.LastSequence);
        Assert.NotNull(entity.RowVersion);
        Assert.True(entity.AllowUnitRegistration);
        Assert.True(entity.AllowRoomRegistration);
        Assert.Equal("Ward 1", entity.AssetWardNo);
        Assert.True(entity.IsActive);
        Assert.False(entity.MarkedForDeletion);
    }

    #endregion
}

public class AssetTypeControllerTests
{
    private static AssetTypeController Create(
        out Mock<IAssetTypeService> service,
        out Mock<IHardDeleteCleanupService> cleanupService,
        out Mock<IReferenceValidationService> referenceValidationService)
    {
        service = new Mock<IAssetTypeService>();
        cleanupService = new Mock<IHardDeleteCleanupService>();
        referenceValidationService = new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<AssetTypeController>>();
        return new AssetTypeController(service.Object, cleanupService.Object, referenceValidationService.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        var query = new AssetTypeQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AssetTypeDto>(new List<AssetTypeDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetTypeDto { Id = 1 });

        var result = await controller.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        var dto = new CreateAssetTypeDto { TypeName = "Equipment" };
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new AssetTypeDto { Id = 1 });

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var controller = Create(out var service, out _, out _);
        var dto = new UpdateAssetTypeDto { TypeName = "Updated Equipment" };
        service.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>())).ReturnsAsync(new AssetTypeDto { Id = 1 });

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
