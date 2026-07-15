using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable;
using Moq;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Comprehensive tests for PropertyService SplitProperty functionality.
/// </summary>
public class PropertyServiceSplitTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IPropertyRepository> _mockPropertyRepository;
    private readonly Mock<ILogger<PropertyService>> _mockLogger;
    private readonly Mock<IOptions<FeatureFlagsOptions>> _mockFeatureFlags;
    
    private readonly Mock<IRepository<WardEntity, int>> _mockWardRepository;
    private readonly Mock<IRepository<PropertyCategoryEntity, int>> _mockCategoryRepository;
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _mockSocietyRepository;
    private readonly Mock<IRepository<PropertyDetailsEntity, int>> _mockPropertyDetailsRepository;
    private readonly Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>> _mockRoomWiseRepository;
    private readonly Mock<IRepository<PropertyAssessmentEntity, int>> _mockAssessmentRepository;
    private readonly Mock<IRepository<GlobalSurveyWardAllocationEntity, int>> _mockWardAllocationRepository;
    private readonly Mock<IRepository<PropertyMapMasterEntity, int>> _mockPropertyMapMasterRepository;
    private readonly Mock<IRepository<PropertyMapDetailEntity, int>> _mockPropertyMapDetailRepository;
    private readonly Mock<IRepository<UserEntity, int>> _mockUserRepository;
    private readonly Mock<IPropertyRuleApplicationLogService> _mockRuleLogService;

    private readonly PropertyService _service;

    public PropertyServiceSplitTests()
    {
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockPropertyRepository = new Mock<IPropertyRepository>();
        _mockLogger = new Mock<ILogger<PropertyService>>();
        _mockFeatureFlags = new Mock<IOptions<FeatureFlagsOptions>>();
        
        _mockWardRepository = new Mock<IRepository<WardEntity, int>>();
        _mockCategoryRepository = new Mock<IRepository<PropertyCategoryEntity, int>>();
        _mockSocietyRepository = new Mock<IRepository<SocietyDetailsEntity, int>>();
        _mockPropertyDetailsRepository = new Mock<IRepository<PropertyDetailsEntity, int>>();
        _mockRoomWiseRepository = new Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>>();
        _mockAssessmentRepository = new Mock<IRepository<PropertyAssessmentEntity, int>>();
        _mockWardAllocationRepository = new Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>();
        _mockPropertyMapMasterRepository = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        _mockPropertyMapDetailRepository = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        _mockUserRepository = new Mock<IRepository<UserEntity, int>>();
        _mockRuleLogService = new Mock<IPropertyRuleApplicationLogService>();

        _mockFeatureFlags.Setup(f => f.Value).Returns(new FeatureFlagsOptions());

        _service = new PropertyService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockPropertyRepository.Object,
            _mockLogger.Object,
            _mockFeatureFlags.Object,
            _mockWardRepository.Object,
            _mockCategoryRepository.Object,
            _mockSocietyRepository.Object,
            _mockPropertyDetailsRepository.Object,
            _mockRoomWiseRepository.Object,
            _mockAssessmentRepository.Object,
            _mockWardAllocationRepository.Object,
            _mockPropertyMapMasterRepository.Object,
            _mockPropertyMapDetailRepository.Object,
            _mockUserRepository.Object,
            _mockRuleLogService.Object
        );
    }

    private void SetupValidUserAndWard(int userId, int wardId)
    {
        var users = new List<UserEntity>
        {
            new UserEntity { Id = userId, IsActive = true, MarkedForDeletion = false }
        };
        _mockUserRepository.Setup(x => x.GetQueryable()).Returns(users.BuildMock());

        var allocations = new List<GlobalSurveyWardAllocationEntity>
        {
            new GlobalSurveyWardAllocationEntity { UserId = userId, WardId = wardId, IsActive = true }
        };
        _mockWardAllocationRepository.Setup(x => x.GetQueryable()).Returns(allocations.BuildMock());
    }

    [Fact]
    public async Task SplitProperty_EmptyPropertyNo_ThrowsArgumentException()
    {
        // Arrange
        var dto = new PropertySplitCreateDto { PropertyNo = string.Empty, NoOfSplit = 1, UserId = 1, WardId = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SplitProperty(dto));
        Assert.Equal("PROPERTY_NO_REQUIRED", ex.Message);
    }

    [Fact]
    public async Task SplitProperty_ZeroSplits_ThrowsArgumentException()
    {
        // Arrange
        var dto = new PropertySplitCreateDto { PropertyNo = "P1", NoOfSplit = 0, UserId = 1, WardId = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SplitProperty(dto));
        Assert.Equal("NO_OF_SPLITS_GREATER_THAN_ZERO", ex.Message);
    }

    [Fact]
    public async Task SplitProperty_InvalidUserId_ThrowsArgumentException()
    {
        // Arrange
        var dto = new PropertySplitCreateDto { PropertyNo = "P1", NoOfSplit = 1, UserId = 0, WardId = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SplitProperty(dto));
        Assert.Equal("USER_ID_GREATER_THAN_ZERO", ex.Message);
    }

    [Fact]
    public async Task SplitProperty_InvalidWardId_ThrowsArgumentException()
    {
        // Arrange
        var dto = new PropertySplitCreateDto { PropertyNo = "P1", NoOfSplit = 1, UserId = 1, WardId = 0 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SplitProperty(dto));
        Assert.Equal("WARD_ID_GREATER_THAN_ZERO", ex.Message);
    }

    [Fact]
    public async Task SplitProperty_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PropertySplitCreateDto { PropertyNo = "P1", NoOfSplit = 1, UserId = 1, WardId = 1 };
        
        _mockUserRepository.Setup(x => x.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _mockWardAllocationRepository.Setup(x => x.GetQueryable()).Returns(new List<GlobalSurveyWardAllocationEntity>().BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.SplitProperty(dto));
        Assert.Equal("USER_NOT_EXIST_OR_INACTIVE", ex.Message);
    }

    [Fact]
    public async Task SplitProperty_WardNotAllocated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PropertySplitCreateDto { PropertyNo = "P1", NoOfSplit = 1, UserId = 1, WardId = 1 };
        
        var users = new List<UserEntity>
        {
            new UserEntity { Id = 1, IsActive = true, MarkedForDeletion = false }
        };
        _mockUserRepository.Setup(x => x.GetQueryable()).Returns(users.BuildMock());
        _mockWardAllocationRepository.Setup(x => x.GetQueryable()).Returns(new List<GlobalSurveyWardAllocationEntity>().BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.SplitProperty(dto));
        Assert.Equal("WARD_NOT_ALLOCATED_TO_USER", ex.Message);
    }

    [Fact]
    public async Task SplitProperty_MainPropertyNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new PropertySplitCreateDto { PropertyNo = "P1", NoOfSplit = 1, UserId = 1, WardId = 1 };
        SetupValidUserAndWard(1, 1);
        
        _mockRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertyEntity>().BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SplitProperty(dto));
        Assert.Equal("BASE_PROPERTY_NOT_EXIST_OR_INACTIVE", ex.Message);
    }
    
    [Fact]
    public async Task SplitProperty_IsPartitionProperty_MissingPartitionNo_ThrowsArgumentException()
    {
        // Arrange
        var dto = new PropertySplitCreateDto { PropertyNo = "P1", NoOfSplit = 1, UserId = 1, WardId = 1, IsPartitionProperty = true, PartitionNo = "" };
        SetupValidUserAndWard(1, 1);
        
        var properties = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 1, PropertyNo = "P1", WardId = 1, IsActive = true }
        };
        _mockRepository.Setup(x => x.GetQueryable()).Returns(properties.BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SplitProperty(dto));
        Assert.Equal("PARTITION_NO_REQUIRED", ex.Message);
    }

    [Fact]
    public async Task SplitProperty_IsPartitionProperty_Success()
    {
        // Arrange
        var dto = new PropertySplitCreateDto { PropertyNo = "P1", NoOfSplit = 2, UserId = 1, WardId = 1, IsPartitionProperty = true, PartitionNo = "A1", IsMainPropertyDataAttach = false, CreatedBy = 1, IsActive = true };
        SetupValidUserAndWard(1, 1);
        
        var properties = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 1, PropertyNo = "P1", WardId = 1, PartitionNo = "A1", IsActive = true, PropertySeqNo = 1, TaxZoneId = 1 },
            new PropertyEntity { Id = 2, PropertyNo = "P1", WardId = 1, PartitionNo = "A1A", IsActive = true } // Existing split
        };
        _mockRepository.Setup(x => x.GetQueryable()).Returns(properties.BuildMock());
        _mockWardRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new WardEntity { Id = 1, WardNo = "10" });
        
        _mockPropertyMapMasterRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertyMapMasterEntity>().BuildMock());

        _mockRepository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<PropertyEntity>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockPropertyMapDetailRepository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<PropertyMapDetailEntity>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.SplitProperty(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Skipped);
        Assert.Equal(2, result.Created.Count);
        
        Assert.Equal("A1B", result.Created[0].GeneratedPartitionNo);
        Assert.Equal("A1C", result.Created[1].GeneratedPartitionNo);

        _mockRepository.Verify(x => x.AddRangeAsync(It.Is<IEnumerable<PropertyEntity>>(list => list.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
        _mockPropertyMapDetailRepository.Verify(x => x.AddRangeAsync(It.Is<IEnumerable<PropertyMapDetailEntity>>(list => list.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task SplitProperty_IsNotPartitionProperty_Success_WithDataAttached()
    {
        // Arrange
        var dto = new PropertySplitCreateDto { PropertyNo = "P123", NoOfSplit = 2, UserId = 1, WardId = 1, IsPartitionProperty = false, IsMainPropertyDataAttach = true, CreatedBy = 1, IsActive = true };
        SetupValidUserAndWard(1, 1);
        
        var properties = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 1, PropertyNo = "P123", WardId = 1, PartitionNo = "A1", IsActive = true, PropertySeqNo = 1, TaxZoneId = 1 },
            new PropertyEntity { Id = 2, PropertyNo = "P123A", WardId = 1, IsActive = true } // Existing split
        };
        _mockRepository.Setup(x => x.GetQueryable()).Returns(properties.BuildMock());
        _mockWardRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new WardEntity { Id = 1, WardNo = "10" });
        
        _mockPropertyMapMasterRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertyMapMasterEntity>().BuildMock());
        
        _mockMapper.Setup(x => x.Map<PropertyEntity>(It.IsAny<PropertyEntity>())).Returns((PropertyEntity src) => new PropertyEntity { PropertyNo = src.PropertyNo, WardId = src.WardId, PartitionNo = src.PartitionNo });

        _mockRepository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<PropertyEntity>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockPropertyMapDetailRepository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<PropertyMapDetailEntity>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.SplitProperty(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Skipped);
        Assert.Equal(2, result.Created.Count);
        
        Assert.Equal("P123B", result.Created[0].GeneratedPropertyNo);
        Assert.Equal("P123C", result.Created[1].GeneratedPropertyNo);

        _mockRepository.Verify(x => x.AddRangeAsync(It.Is<IEnumerable<PropertyEntity>>(list => list.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
        _mockPropertyMapDetailRepository.Verify(x => x.AddRangeAsync(It.Is<IEnumerable<PropertyMapDetailEntity>>(list => list.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
