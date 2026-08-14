using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.PropertyMergeDetails;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using MockQueryable;
using MockQueryable.Moq;

namespace NtisPlatform.Tests.Application;

public class PropertyMergeServiceTests
{
    private readonly Mock<IRepository<PropertyMapMasterEntity, int>> _mockPropertyMapMasterRepository;
    private readonly Mock<IRepository<PropertyMastOldEntity, int>> _mockPropertyOldRepository;
    private readonly Mock<IRepository<PropertyMapDetailEntity, int>> _mockPropertyMapDetailRepository;
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IRepository<WardEntity, int>> _mockWardRepository;
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _mockSocietyRepository;
    private readonly Mock<IRepository<MergeDetailEntity, int>> _mockMergeDetailRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<PropertyMergeService>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertyMergeService _service;

    public PropertyMergeServiceTests()
    {
        _mockPropertyMapMasterRepository = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        _mockPropertyOldRepository = new Mock<IRepository<PropertyMastOldEntity, int>>();
        _mockPropertyMapDetailRepository = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockWardRepository = new Mock<IRepository<WardEntity, int>>();
        _mockSocietyRepository = new Mock<IRepository<SocietyDetailsEntity, int>>();
        _mockMergeDetailRepository = new Mock<IRepository<MergeDetailEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<PropertyMergeService>>();
        _mockMapper = new Mock<IMapper>();

        _mockMapper.Setup(m => m.Map<PropertyMapDetailEntity>(It.IsAny<PropertyMapDetailEntity>()))
            .Returns((PropertyMapDetailEntity src) => src);
        _mockMapper.Setup(m => m.Map<MergeDetailEntity>(It.IsAny<MergeDetailEntity>()))
            .Returns((MergeDetailEntity src) => src);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _service = new PropertyMergeService(
            _mockPropertyMapMasterRepository.Object,
            _mockPropertyOldRepository.Object,
            _mockPropertyMapDetailRepository.Object,
            _mockRepository.Object,
            _mockWardRepository.Object,
            _mockSocietyRepository.Object,
            _mockMergeDetailRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockMapper.Object);
    }

    #region CreateAsync Tests
    [Fact]
    public async Task CreateAsync_OldIdsCountLessThanTwo_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto { PropertyId = 1, PropertyOldIds = new List<int> { 100 } };
        var mapMaster = new PropertyMapMasterEntity { Id = 10, MappingCategory = PropertyMappingCategory.MergeMappingCategory, IsActive = true };
        _mockPropertyMapMasterRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapMasterEntity> { mapMaster }.BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("at least two", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_OldPropertiesNotFound_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto { PropertyId = 1, PropertyOldIds = new List<int> { 100, 101 } };
        
        var mapMaster = new PropertyMapMasterEntity { Id = 10, MappingCategory = PropertyMappingCategory.MergeMappingCategory, IsActive = true };
        _mockPropertyMapMasterRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapMasterEntity> { mapMaster }.BuildMock());
        
        // Return only one property
        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity> { new() { Id = 100, IsActive = true } }.BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("Old properties not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_NewPropertyNotFound_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto { PropertyId = 1, PropertyOldIds = new List<int> { 100, 101 } };
        
        var mapMaster = new PropertyMapMasterEntity { Id = 10, MappingCategory = PropertyMappingCategory.MergeMappingCategory, IsActive = true };
        _mockPropertyMapMasterRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapMasterEntity> { mapMaster }.BuildMock());
        
        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity> { 
                new() { Id = 100, IsActive = true },
                new() { Id = 101, IsActive = true }
            }.BuildMock());

        // Empty repository for new property
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity>().BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity>().BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("New Property not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_Success_MergesProperties()
    {
        // Arrange
        var dto = new CreatePropertyMergeDto { PropertyId = 1, PropertyOldIds = new List<int> { 100, 101 } };
        
        var mapMaster = new PropertyMapMasterEntity { Id = 10, MappingCategory = PropertyMappingCategory.MergeMappingCategory, IsActive = true };
        _mockPropertyMapMasterRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapMasterEntity> { mapMaster }.BuildMock());
        
        var oldProps = new List<PropertyMastOldEntity> {
            new() { Id = 100, IsActive = true, OldOwnerName = "John Doe", OldPropertyNo = "100" },
            new() { Id = 101, IsActive = true, OldOwnerName = "Jane Doe", OldPropertyNo = "101" }
        };
        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(oldProps.BuildMock());

        var newProp = new PropertyEntity { Id = 1, IsActive = true, WardId = 5, OwnerName = "Builder", PropertyNo = "1000" };
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity> { newProp }.BuildMock());
            
        var ward = new WardEntity { Id = 5, WardNo = "W5", IsActive = true };
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { ward }.BuildMock());
            
        _mockSocietyRepository.Setup(r => r.GetQueryable())
            .Returns(new List<SocietyDetailsEntity>().BuildMock());

        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapDetailEntity>().BuildMock());
            
        _mockPropertyMapDetailRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PropertyMapDetailEntity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("merged successfully", result.Message);
        
        // Verify property was updated with merged names
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    #endregion

    #region UpdateAsync Tests
    [Fact]
    public async Task UpdateAsync_OldIdsCountLessThanTwo_ThrowsValidationException()
    {
        // Arrange
        var dto = new UpdatePropertyMergeDto { PropertyId = 1, PropertyOldIds = new List<int> { 100 } };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, dto));
        Assert.Contains("at least two old properties", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_NoActiveMappings_ThrowsValidationException()
    {
        // Arrange
        var dto = new UpdatePropertyMergeDto { PropertyId = 1, PropertyOldIds = new List<int> { 100, 101 } };
        
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapDetailEntity>().BuildMock());
            
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity> { new() { Id = 1, IsActive = true } }.BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, dto));
        Assert.Contains("No merge details found to demerge", ex.Message);
    }
    #endregion
    
    #region GetByIdAsync Tests
    [Fact]
    public async Task GetByIdAsync_PropertyNotFound_ReturnsFailedResult()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity>().BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity>().BuildMock());

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("Property not found", result.Message);
    }
    
    [Fact]
    public async Task GetByIdAsync_NoMergeDetails_ReturnsFailedResult()
    {
        // Arrange
        var newProp = new PropertyEntity { Id = 1, IsActive = true, WardId = 5, PropertyNo = "1000" };
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity> { newProp }.BuildMock());
            
        var ward = new WardEntity { Id = 5, WardNo = "W5", IsActive = true };
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { ward }.BuildMock());
            
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMapDetailEntity>().BuildMock());
            
        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyMastOldEntity>().BuildMock());

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("No merge details found for the specified property", result.Message);
    }
    
    [Fact]
    public async Task GetByIdAsync_Success_ReturnsPropertyWithDetails()
    {
        // Arrange
        var newProp = new PropertyEntity { Id = 1, IsActive = true, WardId = 5, PropertyNo = "1000" };
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyEntity> { newProp }.BuildMock());
            
        var ward = new WardEntity { Id = 5, WardNo = "W5", IsActive = true };
        _mockWardRepository.Setup(r => r.GetQueryable())
            .Returns(new List<WardEntity> { ward }.BuildMock());
            
        var mapDetails = new List<PropertyMapDetailEntity> {
            new() { Id = 10, PropertyIdNew = 1, PropertyIdOld = 100, IsActive = true, Status = PropertyMapStatus.Active },
            new() { Id = 11, PropertyIdNew = 1, PropertyIdOld = 101, IsActive = true, Status = PropertyMapStatus.Active }
        };
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(mapDetails.BuildMock());
            
        var oldProps = new List<PropertyMastOldEntity> {
            new() { Id = 100, IsActive = true, OldPropertyNo = "100", OldOwnerName = "John" },
            new() { Id = 101, IsActive = true, OldPropertyNo = "101", OldOwnerName = "Jane" }
        };
        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(oldProps.BuildMock());

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.NotNull(result.Data);
        var data = Assert.IsType<PropertyMergeDetailDto>(result.Data);
        Assert.Equal(1, data.Id);
        Assert.NotNull(data.PropertyOldDetails);
        Assert.Equal(2, data.PropertyOldDetails!.Count);
        Assert.Contains(data.PropertyOldDetails, x => x.OldPropertyNo == "100");
        Assert.Contains(data.PropertyOldDetails, x => x.OldPropertyNo == "101");
    }
    #endregion
}
