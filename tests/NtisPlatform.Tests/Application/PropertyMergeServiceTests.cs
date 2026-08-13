using AutoMapper;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.PropertyMergeDetails;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive unit tests for <see cref="PropertyMergeService"/> covering all merge, demerge,
/// unmerge details retrieval, and batch operations exposed by PropertyMergeController.
/// </summary>
public class PropertyMergeServiceTests
{
    private readonly Mock<IRepository<PropertyMapMasterEntity, int>> _mockPropertyMapMasterRepository;
    private readonly Mock<IRepository<PropertyMastOldEntity, int>> _mockPropertyOldRepository;
    private readonly Mock<IRepository<PropertyMapDetailEntity, int>> _mockPropertyMapDetailRepository;
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IRepository<WardEntity, int>> _mockWardRepository;
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _mockSocietyRepository;
    private readonly Mock<IRepository<PropertyTypeMasterEntity, int>> _mockPropertyTypeRepository;
    private readonly Mock<IRepository<WingEntity, int>> _mockWingRepository;
    private readonly Mock<IRepository<PropertyAssessmentEntity, int>> _mockAssessmentRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<PropertyMergeService>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertyMergeService _service;

    private readonly List<PropertyMapMasterEntity> _defaultCategoryMasters;

    public PropertyMergeServiceTests()
    {
        _mockPropertyMapMasterRepository = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        _mockPropertyOldRepository = new Mock<IRepository<PropertyMastOldEntity, int>>();
        _mockPropertyMapDetailRepository = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockWardRepository = new Mock<IRepository<WardEntity, int>>();
        _mockSocietyRepository = new Mock<IRepository<SocietyDetailsEntity, int>>();
        _mockPropertyTypeRepository = new Mock<IRepository<PropertyTypeMasterEntity, int>>();
        _mockWingRepository = new Mock<IRepository<WingEntity, int>>();
        _mockAssessmentRepository = new Mock<IRepository<PropertyAssessmentEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<PropertyMergeService>>();
        _mockMapper = new Mock<IMapper>();

        _defaultCategoryMasters = new List<PropertyMapMasterEntity>
        {
            new() { Id = 1, MappingCategory = PropertyMappingCategory.OneToOneMappingCategory, IsActive = true },
            new() { Id = 2, MappingCategory = PropertyMappingCategory.SplitMappingCategory, IsActive = true },
            new() { Id = 3, MappingCategory = PropertyMappingCategory.MergeMappingCategory, IsActive = true }
        };

        _mockPropertyMapMasterRepository.Setup(r => r.GetQueryable())
            .Returns(_defaultCategoryMasters.BuildMock());

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new PropertyMergeService(
            _mockPropertyMapMasterRepository.Object,
            _mockPropertyOldRepository.Object,
            _mockPropertyMapDetailRepository.Object,
            _mockRepository.Object,
            _mockWardRepository.Object,
            _mockSocietyRepository.Object,
            _mockPropertyTypeRepository.Object,
            _mockWingRepository.Object,
            _mockAssessmentRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockMapper.Object);
    }

    #region CreateAsync Tests (Single, Split, Multiple)

    [Fact]
    public async Task CreateAsync_WithNullDto_ThrowsValidationException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(null!, CancellationToken.None));
        Assert.Contains("Property details are required", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyPropertyIds_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int>(),
            PropertyOldIds = new List<int> { 100 }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, CancellationToken.None));
        Assert.Contains("Property details are required", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyPropertyOldIds_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int>()
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, CancellationToken.None));
        Assert.Contains("Property details are required", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithMultipleOldAndMultipleNew_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1, 2 },
            PropertyOldIds = new List<int> { 101, 102 }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, CancellationToken.None));
        Assert.Contains("Multiple old properties cannot be merged with multiple new properties", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenCategoryMasterNotFound_ThrowsValidationException()
    {
        // Arrange
        _mockPropertyMapMasterRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapMasterEntity>().BuildMock());

        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, CancellationToken.None));
        Assert.Contains("property mapping category not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_SingleMerge_WhenOldPropertyNotFound_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 999 }
        };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity> { new() { Id = 1, WardId = 1, PropertyNo = "P1", IsActive = true } }.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { new() { Id = 1, WardNo = "W1", IsActive = true } }.BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, CancellationToken.None));
        Assert.Contains("Old Property not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_SingleMerge_WhenNewPropertyNotFound_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 999 },
            PropertyOldIds = new List<int> { 100 }
        };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity> { new() { Id = 100, OldPropertyNo = "OP1", IsActive = true } }.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity>().BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { new() { Id = 1, WardNo = "W1", IsActive = true } }.BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, CancellationToken.None));
        Assert.Contains("New Property not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_SingleMerge_WhenOldPropertyAlreadyMerged_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 }
        };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity> { new() { Id = 100, OldWardNo = "W1", OldPropertyNo = "OP1", IsActive = true } }.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity> { new() { Id = 1, WardId = 1, PropertyNo = "P1", IsActive = true } }.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { new() { Id = 1, WardNo = "W1", IsActive = true } }.BuildMock());

        var existingMergeDetails = new List<PropertyMapDetailEntity>
        {
            new() { Id = 10, PropertyIdOld = 100, PropertyNoOld = "W1-OP1", PropertyNoNew = "W1-P99", IsActive = true, Status = PropertyMapStatus.Active }
        };
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(existingMergeDetails.BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, CancellationToken.None));
        Assert.Contains("already merged", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_SingleMerge_Success_CreatesMappingAndMergesOwnerName()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 },
            Latitude = "18.5204",
            Longitude = "73.8567",
            Location = "Pune",
            CreatedBy = 10
        };

        var oldProp = new PropertyMastOldEntity
        {
            Id = 100,
            OldWardNo = "W1",
            OldPropertyNo = "100",
            OldPartitionNo = "0",
            OldOwnerName = "Jane Doe",
            OldOccupierName = "Jane Occupier",
            IsActive = true
        };
        var newProp = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            PropertyNo = "200",
            PartitionNo = "0",
            OwnerName = "The Holder, John Doe",
            OccupierName = "John Occupier",
            IsActive = true
        };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity> { oldProp }.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity> { newProp }.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { ward }.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapDetailEntity>().BuildMock());

        PropertyMapDetailEntity? capturedEntity = null;
        _mockPropertyMapDetailRepository.Setup(r => r.AddAsync(It.IsAny<PropertyMapDetailEntity>(), It.IsAny<CancellationToken>()))
            .Callback<PropertyMapDetailEntity, CancellationToken>((entity, _) => capturedEntity = entity)
            .ReturnsAsync((PropertyMapDetailEntity e, CancellationToken _) => e);

        // Act
        var result = await _service.CreateAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("merge successful", result.Message);
        Assert.NotNull(capturedEntity);
        Assert.Equal(1, capturedEntity.PropertyIdNew);
        Assert.Equal(100, capturedEntity.PropertyIdOld);
        Assert.Equal("W1-200-0", capturedEntity.PropertyNoNew);
        Assert.Equal("W1-100-0", capturedEntity.PropertyNoOld);
        Assert.Equal(PropertyMapStatus.Active, capturedEntity.Status);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SingleMerge_WhenNewPropertyHasExistingMerges_UpdatesCategoryToMerge()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 101 },
            CreatedBy = 10
        };

        var oldProp = new PropertyMastOldEntity { Id = 101, OldWardNo = "W1", OldPropertyNo = "101", IsActive = true };
        var newProp = new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "200", IsActive = true };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity> { oldProp }.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity> { newProp }.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { ward }.BuildMock());

        var existingMapDetails = new List<PropertyMapDetailEntity>
        {
            new() { Id = 50, PropertyIdNew = 1, PropertyIdOld = 100, IsActive = true, Status = PropertyMapStatus.Active }
        };
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(existingMapDetails.BuildMock());

        // Act
        var result = await _service.CreateAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        _mockPropertyMapDetailRepository.Verify(r => r.AddAsync(It.IsAny<PropertyMapDetailEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SplitMerge_WhenOldPropertyNotFound_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1, 2 },
            PropertyOldIds = new List<int> { 999 }
        };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity>().BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity>().BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapDetailEntity>().BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, CancellationToken.None));
        Assert.Contains("Old Property not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_SplitMerge_WhenNewPropertiesCountMismatch_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1, 2 },
            PropertyOldIds = new List<int> { 100 }
        };

        var oldProp = new PropertyMastOldEntity { Id = 100, OldWardNo = "W1", OldPropertyNo = "100", IsActive = true };
        var newProp = new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "201", IsActive = true };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity> { oldProp }.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity> { newProp }.BuildMock()); // only 1 returned instead of 2
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { ward }.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapDetailEntity>().BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, CancellationToken.None));
        Assert.Contains("Selected new properties were not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_SplitMerge_WhenNewPropertyAlreadyMerged_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1, 2 },
            PropertyOldIds = new List<int> { 100 }
        };

        var oldProp = new PropertyMastOldEntity { Id = 100, OldWardNo = "W1", OldPropertyNo = "100", IsActive = true };
        var newProps = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, PropertyNo = "201", IsActive = true },
            new() { Id = 2, WardId = 1, PropertyNo = "202", IsActive = true }
        };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity> { oldProp }.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(newProps.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { ward }.BuildMock());

        var existingMapDetails = new List<PropertyMapDetailEntity>
        {
            new() { Id = 1, PropertyIdNew = 1, PropertyNoNew = "W1-201", PropertyNoOld = "W1-90", IsActive = true, Status = PropertyMapStatus.Active }
        };
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(existingMapDetails.BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, CancellationToken.None));
        Assert.Contains("already merged", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_SplitMerge_Success_CreatesMultipleMappings()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1, 2 },
            PropertyOldIds = new List<int> { 100 },
            CreatedBy = 10
        };

        var oldProp = new PropertyMastOldEntity { Id = 100, OldWardNo = "W1", OldPropertyNo = "100", IsActive = true };
        var newProps = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, PropertyNo = "201", IsActive = true },
            new() { Id = 2, WardId = 1, PropertyNo = "202", IsActive = true }
        };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity> { oldProp }.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(newProps.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { ward }.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapDetailEntity>().BuildMock());

        List<PropertyMapDetailEntity>? capturedEntities = null;
        _mockPropertyMapDetailRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PropertyMapDetailEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PropertyMapDetailEntity>, CancellationToken>((entities, _) => capturedEntities = entities.ToList())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("split successfully", result.Message);
        Assert.NotNull(capturedEntities);
        Assert.Equal(2, capturedEntities.Count);
        Assert.All(capturedEntities, e => Assert.Equal("Property Merged - Split Old Property", e.Remark));
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MergeMultiple_WhenNewPropertyNotFound_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 999 },
            PropertyOldIds = new List<int> { 101, 102 }
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity>().BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity>().BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapDetailEntity>().BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, CancellationToken.None));
        Assert.Contains("New Property not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_MergeMultiple_WhenOldPropertiesCountMismatch_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 101, 102 }
        };

        var newProp = new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "200", IsActive = true };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };
        var oldProps = new List<PropertyMastOldEntity>
        {
            new() { Id = 101, OldWardNo = "W1", OldPropertyNo = "101", IsActive = true } // only 1 returned
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity> { newProp }.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { ward }.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(oldProps.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapDetailEntity>().BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, CancellationToken.None));
        Assert.Contains("Selected old properties were not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_MergeMultiple_WhenOldPropertyAlreadyMerged_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 101, 102 }
        };

        var newProp = new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "200", IsActive = true };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };
        var oldProps = new List<PropertyMastOldEntity>
        {
            new() { Id = 101, OldWardNo = "W1", OldPropertyNo = "101", IsActive = true },
            new() { Id = 102, OldWardNo = "W1", OldPropertyNo = "102", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity> { newProp }.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { ward }.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(oldProps.BuildMock());

        var existingMapDetails = new List<PropertyMapDetailEntity>
        {
            new() { Id = 1, PropertyIdOld = 101, PropertyNoOld = "W1-101", PropertyNoNew = "W1-99", IsActive = true, Status = PropertyMapStatus.Active }
        };
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(existingMapDetails.BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, CancellationToken.None));
        Assert.Contains("already merged", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_MergeMultiple_Success_CreatesMultipleMappings()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 101, 102 },
            CreatedBy = 10
        };

        var newProp = new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "200", IsActive = true };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };
        var oldProps = new List<PropertyMastOldEntity>
        {
            new() { Id = 101, OldWardNo = "W1", OldPropertyNo = "101", IsActive = true },
            new() { Id = 102, OldWardNo = "W1", OldPropertyNo = "102", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity> { newProp }.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { ward }.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(oldProps.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapDetailEntity>().BuildMock());

        List<PropertyMapDetailEntity>? capturedEntities = null;
        _mockPropertyMapDetailRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PropertyMapDetailEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PropertyMapDetailEntity>, CancellationToken>((entities, _) => capturedEntities = entities.ToList())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("merged successfully", result.Message);
        Assert.NotNull(capturedEntities);
        Assert.Equal(2, capturedEntities.Count);
        Assert.All(capturedEntities, e => Assert.Equal("Property Merged - Multiple Old Properties", e.Remark));
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests (Demerge Single/Split)

    [Fact]
    public async Task UpdateAsync_WithNullDto_ReturnsFailedResponse()
    {
        // Act
        var result = await _service.UpdateAsync(1, null!, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Invalid request data", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyPropertyIds_ReturnsFailedResponse()
    {
        // Arrange
        var dto = new UpdatePropertyMergeDto
        {
            PropertyIds = new List<int>(),
            PropertyOldIds = new List<int> { 100 }
        };

        // Act
        var result = await _service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Invalid propertyNo", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyPropertyOldIds_ReturnsFailedResponse()
    {
        // Arrange
        var dto = new UpdatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int>()
        };

        // Act
        var result = await _service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Invalid old propertyNo", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_FromNewSide_WithNoPositiveOldIds_ReturnsFailedResponse()
    {
        // Arrange
        var dto = new UpdatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 0, -1 },
            PropertySide = "New"
        };

        // Act
        var result = await _service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Invalid old property number", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_FromNewSide_WhenNoActiveMappingAndPropertyExists_ReturnsFailedResponse()
    {
        // Arrange
        var dto = new UpdatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 },
            PropertySide = "New"
        };

        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapDetailEntity>().BuildMock());
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity> { new() { Id = 1, IsActive = true } }.BuildMock());

        // Act
        var result = await _service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("No merge details found to demerge", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_FromNewSide_WhenNoActiveMappingAndPropertyNotExists_ReturnsFailedResponse()
    {
        // Arrange
        var dto = new UpdatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 },
            PropertySide = "New"
        };

        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapDetailEntity>().BuildMock());
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity>().BuildMock());

        // Act
        var result = await _service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Property not found", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_FromNewSide_Success_DemergesAndConvertsRemainingToOneToOne()
    {
        // Arrange
        var dto = new UpdatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 },
            PropertySide = "New",
            UpdatedBy = 5
        };

        var mappingDetails = new List<PropertyMapDetailEntity>
        {
            new() { Id = 10, PropertyIdNew = 1, PropertyIdOld = 100, PropertyNoNew = "W1-200", PropertyNoOld = "W1-100", IsActive = true, Status = PropertyMapStatus.Active },
            new() { Id = 11, PropertyIdNew = 1, PropertyIdOld = 101, PropertyNoNew = "W1-200", PropertyNoOld = "W1-101", IsActive = true, Status = PropertyMapStatus.Active }
        };

        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(mappingDetails.BuildMock());

        // Act
        var result = await _service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("demerged successfully", result.Message);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_FromOldSide_WhenNoActiveMappingAndOldPropertyExists_ReturnsFailedResponse()
    {
        // Arrange
        var dto = new UpdatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1, 2 },
            PropertyOldIds = new List<int> { 100 },
            PropertySide = "Old"
        };

        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapDetailEntity>().BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity> { new() { Id = 100, IsActive = true } }.BuildMock());

        // Act
        var result = await _service.UpdateAsync(100, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("No split details found to demerge", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_FromOldSide_Success_DemergesSplit()
    {
        // Arrange
        var dto = new UpdatePropertyMergeDto
        {
            PropertyIds = new List<int> { 1 },
            PropertyOldIds = new List<int> { 100 },
            PropertySide = "Old",
            UpdatedBy = 5
        };

        var mappingDetails = new List<PropertyMapDetailEntity>
        {
            new() { Id = 10, PropertyIdNew = 1, PropertyIdOld = 100, PropertyNoNew = "W1-201", PropertyNoOld = "W1-100", IsActive = true, Status = PropertyMapStatus.Active },
            new() { Id = 11, PropertyIdNew = 2, PropertyIdOld = 100, PropertyNoNew = "W1-202", PropertyNoOld = "W1-100", IsActive = true, Status = PropertyMapStatus.Active }
        };

        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(mappingDetails.BuildMock());

        // Act
        var result = await _service.UpdateAsync(100, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("demerged successfully", result.Message);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetByIdAsync Tests (Merge Details)

    [Fact]
    public async Task GetByIdAsync_WhenNoMergeDetailsExist_ReturnsSuccessFalse()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>().BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMapDetailEntity>().BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(new List<WardEntity>().BuildMock());

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("No merge details found", result.Message);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMergeDetailsExist_ReturnsSuccessTrueWithData()
    {
        // Arrange
        var prop = new PropertyEntity { Id = 1, WardId = 10, PropertyNo = "200", PartitionNo = "0", IsActive = true };
        var ward = new WardEntity { Id = 10, WardNo = "W10", IsActive = true };
        var oldProp = new PropertyMastOldEntity
        {
            Id = 100,
            OldWardNo = "W10",
            OldPropertyNo = "100",
            OldPartitionNo = "0",
            OldOwnerName = "John Doe",
            OldOccupierName = "Jane Doe",
            OldMobileNo = "9876543210",
            OldAddress = "Street 1",
            OldSocietyName = "Society A",
            OldRV = 12000,
            OldTotalTax = 1500,
            OldGeneralTax = 1000,
            OldPlotArea = 500,
            OldConstructionYear = "2015",
            OldConstructionArea = 450,
            IsActive = true
        };
        var mapDetail = new PropertyMapDetailEntity
        {
            Id = 1,
            PropertyIdNew = 1,
            PropertyIdOld = 100,
            IsActive = true,
            Status = PropertyMapStatus.Active
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity> { prop }.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(new List<WardEntity> { ward }.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity> { oldProp }.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMapDetailEntity> { mapDetail }.BuildMock());

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("Found 1 merge detail", result.Message);
        Assert.NotNull(result.Data);
        var detail = Assert.Single(result.Data);
        Assert.Equal(1, detail.Id);
        Assert.Equal("200", detail.PropertyNo);
        Assert.Equal("100", detail.OldPropertyNo);
        Assert.Equal("John Doe", detail.OldOwnerName);
        Assert.Equal(2015, detail.OldConstructionYear);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExceptionOccurs_ReturnsSuccessFalse()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetQueryable()).Throws(new InvalidOperationException("DB error"));

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Error retrieving merge details", result.Message);
    }

    #endregion

    #region GetAllAsync Tests (Unmerged Properties Pagination)

    [Fact]
    public async Task GetAllAsync_WithNullQueryParams_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetAllAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllAsync_WithInvalidPropertyType_ThrowsFilterValidationException()
    {
        // Arrange
        var queryParams = new PropertyMergeQueryParameters
        {
            PropertyId = 1,
            PropertyType = "UNKNOWN"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FilterValidationException>(() => _service.GetAllAsync(queryParams, CancellationToken.None));
        Assert.Contains("PropertyType", ex.Message);
    }

    [Fact]
    public async Task GetAllAsync_ForNewProperties_WhenTargetPropertyNotFound_ReturnsEmptyPagedResult()
    {
        // Arrange
        var queryParams = new PropertyMergeQueryParameters
        {
            PropertyId = 999,
            PropertyType = SurveySearchStatus.New,
            PageNumber = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>().BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.False(item.Success);
        Assert.Contains("Property not found", item.Message);
    }

    [Fact]
    public async Task GetAllAsync_ForNewProperties_Success_ReturnsUnmergedNewProperties()
    {
        // Arrange
        var targetProp = new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "P100", SocietyDetailId = 5, IsActive = true };
        var candidateProp = new PropertyEntity
        {
            Id = 2,
            WardId = 1,
            PropertyNo = "P100",
            PartitionNo = "A-1",
            OwnerName = "Alice",
            OccupierName = "Bob",
            Address = "Road 1",
            MobileNo = "9999999999",
            Type = "Residential",
            SocietyDetailId = 5,
            PropertyTypeId = 2,
            FlatOrShopNo = "101",
            IsActive = true
        };
        var society = new SocietyDetailsEntity { Id = 5, SocietyName = "Green Society", WingId = 1, WingName = "A Wing", IsActive = true };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };
        var propType = new PropertyTypeMasterEntity { Id = 2, PartType = "Flat", PropertyDescription = "Residential Flat", IsActive = true };
        var wing = new WingEntity { Id = 1, WingNo = "A", IsActive = true };
        var assessment = new PropertyAssessmentEntity { Id = 1, PropertyId = 2, BHK = "2BHK", IsActive = true };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity> { targetProp, candidateProp }.BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity> { society }.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(new List<WardEntity> { ward }.BuildMock());
        _mockPropertyTypeRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyTypeMasterEntity> { propType }.BuildMock());
        _mockWingRepository.Setup(r => r.GetQueryable()).Returns(new List<WingEntity> { wing }.BuildMock());
        _mockAssessmentRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyAssessmentEntity> { assessment }.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMapDetailEntity>().BuildMock());

        var queryParams = new PropertyMergeQueryParameters
        {
            PropertyId = 1,
            PropertyType = SurveySearchStatus.New,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.True(item.Success);
        Assert.NotNull(item.NewData);
        var detail = Assert.Single(item.NewData);
        Assert.Equal(2, detail.PropertyId);
        Assert.Equal("P100", detail.PropertyNo);
        Assert.Equal("A-1", detail.PartitionNo);
        Assert.Equal("Green Society", detail.SocietyName);
        Assert.Equal("2BHK", detail.BHK);
    }

    [Fact]
    public async Task GetAllAsync_ForOldProperties_Success_ReturnsUnmergedOldProperties()
    {
        // Arrange
        var draftMap = new PropertyMapDetailEntity
        {
            Id = 1,
            PropertyIdNew = 1,
            PropertyIdOld = 100,
            IsActive = true,
            Status = PropertyMapStatus.Draft
        };
        var linkedOldProp = new PropertyMastOldEntity
        {
            Id = 100,
            OldSocietyName = "Green Society",
            OldWardNo = "W1",
            OldPropertyNo = "OP100",
            OldPartitionNo = "0",
            IsActive = true
        };
        var unmergedOldProp = new PropertyMastOldEntity
        {
            Id = 101,
            OldSocietyName = "Green Society",
            OldWing = "A",
            OldWardNo = "W1",
            OldPropertyNo = "OP101",
            OldPartitionNo = "0",
            OldOwnerName = "Old Owner",
            OldOccupierName = "Old Occupier",
            OldAddress = "Old Address",
            OldRV = 5000,
            OldTotalTax = 600,
            OldConstructionYear = "2010",
            OldConstructionArea = 300,
            IsActive = true
        };

        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMapDetailEntity> { draftMap }.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity> { linkedOldProp, unmergedOldProp }.BuildMock());

        var queryParams = new PropertyMergeQueryParameters
        {
            PropertyId = 1,
            PropertyType = SurveySearchStatus.Old,
            WingName = "A",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.True(item.Success);
        Assert.NotNull(item.OldData);
        var detail = Assert.Single(item.OldData);
        Assert.Equal(101, detail.PropertyOldId);
        Assert.Equal("OP101", detail.OldPropertyNo);
        Assert.Equal("Old Owner", detail.OldOwnerName);
        Assert.Equal(2010, detail.OldConstructionYear);
    }

    #endregion

    #region MergeMultiplePropertyAsync Tests (Batch 1-to-1 Merge)

    [Fact]
    public async Task MergeMultiplePropertyAsync_WithNullDto_ThrowsValidationException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.MergeMultiplePropertyAsync(null!, CancellationToken.None));
        Assert.Contains("Invalid Request", ex.Message);
    }

    [Fact]
    public async Task MergeMultiplePropertyAsync_WithEmptyList_ThrowsValidationException()
    {
        // Arrange
        var dto = new PropertyMergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto>(),
            CreatedBy = 1
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.MergeMultiplePropertyAsync(dto, CancellationToken.None));
        Assert.Contains("Invalid Request", ex.Message);
    }

    [Fact]
    public async Task MergeMultiplePropertyAsync_WithInvalidCreatedBy_ThrowsValidationException()
    {
        // Arrange
        var dto = new PropertyMergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto> { new() { PropertyId = 1, PropertyOldId = 100 } },
            CreatedBy = 0
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.MergeMultiplePropertyAsync(dto, CancellationToken.None));
        Assert.Contains("Invalid User", ex.Message);
    }

    [Fact]
    public async Task MergeMultiplePropertyAsync_WithInvalidPairIds_ThrowsValidationException()
    {
        // Arrange
        var dto = new PropertyMergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto> { new() { PropertyId = -1, PropertyOldId = 100 } },
            CreatedBy = 1
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.MergeMultiplePropertyAsync(dto, CancellationToken.None));
        Assert.Contains("Invalid data found", ex.Message);
    }

    [Fact]
    public async Task MergeMultiplePropertyAsync_WithDuplicateOldPropertyIds_ThrowsValidationException()
    {
        // Arrange
        var dto = new PropertyMergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto>
            {
                new() { PropertyId = 1, PropertyOldId = 100 },
                new() { PropertyId = 2, PropertyOldId = 100 }
            },
            CreatedBy = 1
        };

        var oldProp = new PropertyMastOldEntity { Id = 100, OldWardNo = "W1", OldPropertyNo = "100", IsActive = true };
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity> { oldProp }.BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.MergeMultiplePropertyAsync(dto, CancellationToken.None));
        Assert.Contains("The old property numbers are repeated", ex.Message);
    }

    [Fact]
    public async Task MergeMultiplePropertyAsync_WithDuplicateNewPropertyIds_ThrowsValidationException()
    {
        // Arrange
        var dto = new PropertyMergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto>
            {
                new() { PropertyId = 1, PropertyOldId = 100 },
                new() { PropertyId = 1, PropertyOldId = 101 }
            },
            CreatedBy = 1
        };

        var newProp = new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "200", IsActive = true };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity> { newProp }.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(new List<WardEntity> { ward }.BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.MergeMultiplePropertyAsync(dto, CancellationToken.None));
        Assert.Contains("The new property numbers are repeated", ex.Message);
    }

    [Fact]
    public async Task MergeMultiplePropertyAsync_WhenOldPropertiesNotFound_ThrowsValidationException()
    {
        // Arrange
        var dto = new PropertyMergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto> { new() { PropertyId = 1, PropertyOldId = 999 } },
            CreatedBy = 1
        };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.MergeMultiplePropertyAsync(dto, CancellationToken.None));
        Assert.Contains("Old properties not found", ex.Message);
    }

    [Fact]
    public async Task MergeMultiplePropertyAsync_WhenNewPropertiesNotFound_ThrowsValidationException()
    {
        // Arrange
        var dto = new PropertyMergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto> { new() { PropertyId = 999, PropertyOldId = 100 } },
            CreatedBy = 1
        };

        var oldProp = new PropertyMastOldEntity { Id = 100, OldWardNo = "W1", OldPropertyNo = "100", IsActive = true };
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity> { oldProp }.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>().BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(new List<WardEntity>().BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.MergeMultiplePropertyAsync(dto, CancellationToken.None));
        Assert.Contains("New properties not found", ex.Message);
    }

    [Fact]
    public async Task MergeMultiplePropertyAsync_WhenOldPropertyAlreadyMerged_ThrowsValidationException()
    {
        // Arrange
        var dto = new PropertyMergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto> { new() { PropertyId = 1, PropertyOldId = 100 } },
            CreatedBy = 1
        };

        var oldProp = new PropertyMastOldEntity { Id = 100, OldWardNo = "W1", OldPropertyNo = "100", IsActive = true };
        var newProp = new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "200", IsActive = true };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity> { oldProp }.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity> { newProp }.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(new List<WardEntity> { ward }.BuildMock());

        var existingMappings = new List<PropertyMapDetailEntity>
        {
            new() { Id = 1, PropertyIdOld = 100, PropertyNoOld = "W1-100", PropertyNoNew = "W1-99", IsActive = true, Status = PropertyMapStatus.Active }
        };
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(existingMappings.BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.MergeMultiplePropertyAsync(dto, CancellationToken.None));
        Assert.Contains("Old properties already merged", ex.Message);
    }

    [Fact]
    public async Task MergeMultiplePropertyAsync_WhenNewPropertyAlreadyMerged_ThrowsValidationException()
    {
        // Arrange
        var dto = new PropertyMergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto> { new() { PropertyId = 1, PropertyOldId = 100 } },
            CreatedBy = 1
        };

        var oldProp = new PropertyMastOldEntity { Id = 100, OldWardNo = "W1", OldPropertyNo = "100", IsActive = true };
        var newProp = new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "200", IsActive = true };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity> { oldProp }.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity> { newProp }.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(new List<WardEntity> { ward }.BuildMock());

        var existingMappings = new List<PropertyMapDetailEntity>
        {
            new() { Id = 1, PropertyIdNew = 1, PropertyNoNew = "W1-200", PropertyNoOld = "W1-88", IsActive = true, Status = PropertyMapStatus.Active }
        };
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(existingMappings.BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.MergeMultiplePropertyAsync(dto, CancellationToken.None));
        Assert.Contains("already merged", ex.Message);
    }

    [Fact]
    public async Task MergeMultiplePropertyAsync_Success_MergesPropertiesAndUpdatesOwnerName()
    {
        // Arrange
        var dto = new PropertyMergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto>
            {
                new() { PropertyId = 1, PropertyOldId = 100 },
                new() { PropertyId = 2, PropertyOldId = 101 }
            },
            CreatedBy = 10,
            Latitude = "18.52",
            Longitude = "73.85",
            Location = "Main Street"
        };

        var oldProps = new List<PropertyMastOldEntity>
        {
            new() { Id = 100, OldWardNo = "W1", OldPropertyNo = "100", OldPartitionNo = "0", OldOwnerName = "Old Owner 1", IsActive = true },
            new() { Id = 101, OldWardNo = "W1", OldPropertyNo = "101", OldPartitionNo = "0", OldOwnerName = "Old Owner 2", IsActive = true }
        };
        var newProps = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, PropertyNo = "200", PartitionNo = "0", OwnerName = "Existing 1", IsActive = true },
            new() { Id = 2, WardId = 1, PropertyNo = "201", PartitionNo = "0", OwnerName = "Existing 2", IsActive = true }
        };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(oldProps.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable()).Returns(newProps.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(new List<WardEntity> { ward }.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMapDetailEntity>().BuildMock());

        List<PropertyMapDetailEntity>? addedMappings = null;
        _mockPropertyMapDetailRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PropertyMapDetailEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PropertyMapDetailEntity>, CancellationToken>((entities, _) => addedMappings = entities.ToList())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.MergeMultiplePropertyAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("properties merged successfully", result.Message);
        Assert.NotNull(addedMappings);
        Assert.Equal(2, addedMappings.Count);
        Assert.All(addedMappings, m => Assert.Equal("Property Merged - Multiple One To One New Property", m.Remark));
        Assert.Equal("Old Owner 1", newProps[0].OwnerName);
        Assert.Equal("Old Owner 2", newProps[1].OwnerName);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DemergeMultiplePropertyAsync Tests (Batch Demerge)

    [Fact]
    public async Task DemergeMultiplePropertyAsync_WithNullDto_ThrowsValidationException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.DemergeMultiplePropertyAsync(null!, CancellationToken.None));
        Assert.Contains("Invalid request", ex.Message);
    }

    [Fact]
    public async Task DemergeMultiplePropertyAsync_WithEmptyList_ThrowsValidationException()
    {
        // Arrange
        var dto = new PropertyDemergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto>(),
            UpdatedBy = 1
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.DemergeMultiplePropertyAsync(dto, CancellationToken.None));
        Assert.Contains("At least one property pair is required", ex.Message);
    }

    [Fact]
    public async Task DemergeMultiplePropertyAsync_WithInvalidUpdatedBy_ThrowsValidationException()
    {
        // Arrange
        var dto = new PropertyDemergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto> { new() { PropertyId = 1, PropertyOldId = 100 } },
            UpdatedBy = 0
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.DemergeMultiplePropertyAsync(dto, CancellationToken.None));
        Assert.Contains("Invalid user", ex.Message);
    }

    [Fact]
    public async Task DemergeMultiplePropertyAsync_WithNoValidPairs_ThrowsValidationException()
    {
        // Arrange
        var dto = new PropertyDemergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto> { new() { PropertyId = 0, PropertyOldId = -1 } },
            UpdatedBy = 1
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.DemergeMultiplePropertyAsync(dto, CancellationToken.None));
        Assert.Contains("Invalid property data found", ex.Message);
    }

    [Fact]
    public async Task DemergeMultiplePropertyAsync_WhenNoMappingRecordsFound_ThrowsValidationException()
    {
        // Arrange
        var dto = new PropertyDemergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto> { new() { PropertyId = 1, PropertyOldId = 100 } },
            UpdatedBy = 1
        };

        var oldProp = new PropertyMastOldEntity { Id = 100, OldWardNo = "W1", OldPropertyNo = "100", IsActive = true };
        var newProp = new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "200", IsActive = true };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity> { oldProp }.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity> { newProp }.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(new List<WardEntity> { ward }.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMapDetailEntity>().BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.DemergeMultiplePropertyAsync(dto, CancellationToken.None));
        Assert.Contains("No property merging details found", ex.Message);
    }

    [Fact]
    public async Task DemergeMultiplePropertyAsync_WhenMissingPairsInMappings_ThrowsValidationException()
    {
        // Arrange
        var dto = new PropertyDemergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto>
            {
                new() { PropertyId = 1, PropertyOldId = 100 },
                new() { PropertyId = 2, PropertyOldId = 101 } // this pair is missing from mappings
            },
            UpdatedBy = 1
        };

        var oldProps = new List<PropertyMastOldEntity>
        {
            new() { Id = 100, OldWardNo = "W1", OldPropertyNo = "100", IsActive = true },
            new() { Id = 101, OldWardNo = "W1", OldPropertyNo = "101", IsActive = true }
        };
        var newProps = new List<PropertyEntity>
        {
            new() { Id = 1, WardId = 1, PropertyNo = "200", IsActive = true },
            new() { Id = 2, WardId = 1, PropertyNo = "201", IsActive = true }
        };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(oldProps.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable()).Returns(newProps.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(new List<WardEntity> { ward }.BuildMock());

        var mappings = new List<PropertyMapDetailEntity>
        {
            new() { Id = 10, PropertyIdNew = 1, PropertyIdOld = 100, PropertyNoNew = "W1-200", PropertyNoOld = "W1-100", IsActive = true, Status = PropertyMapStatus.Active }
        };
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(mappings.BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.DemergeMultiplePropertyAsync(dto, CancellationToken.None));
        Assert.Contains("Property merging details not found for property no", ex.Message);
    }

    [Fact]
    public async Task DemergeMultiplePropertyAsync_Success_DemergesAndRecalculatesRemainingCategories()
    {
        // Arrange
        var dto = new PropertyDemergeMultipleDto
        {
            PropertyIdList = new List<PropertyMergeMultipleListDto>
            {
                new() { PropertyId = 1, PropertyOldId = 100 }
            },
            UpdatedBy = 5
        };

        var oldProp = new PropertyMastOldEntity { Id = 100, OldWardNo = "W1", OldPropertyNo = "100", OldPartitionNo = "0", IsActive = true };
        var newProp = new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "200", PartitionNo = "0", IsActive = true };
        var ward = new WardEntity { Id = 1, WardNo = "W1", IsActive = true };

        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity> { oldProp }.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity> { newProp }.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(new List<WardEntity> { ward }.BuildMock());

        var mappings = new List<PropertyMapDetailEntity>
        {
            new() { Id = 10, PropertyMapId = 3, PropertyIdNew = 1, PropertyIdOld = 100, PropertyNoNew = "W1-200-0", PropertyNoOld = "W1-100-0", IsActive = true, Status = PropertyMapStatus.Active },
            new() { Id = 11, PropertyMapId = 3, PropertyIdNew = 1, PropertyIdOld = 101, PropertyNoNew = "W1-200-0", PropertyNoOld = "W1-101-0", IsActive = true, Status = PropertyMapStatus.Active }
        };
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(mappings.BuildMock());

        // Act
        var result = await _service.DemergeMultiplePropertyAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("Properties demerged successfully", result.Message);
        Assert.Contains("W1-100-0 -> W1-200-0", result.Message);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
