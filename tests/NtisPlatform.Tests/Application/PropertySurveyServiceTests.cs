using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.PropertySurveySearch;
using NtisPlatform.Application.DTOs.PropertyVisitTracker;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class PropertySurveyServiceTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _mockPropertyRepo = new();
    private readonly Mock<IRepository<ModuleMasterEntity, int>> _mockModuleMasterRepo = new();
    private readonly Mock<IRepository<WardEntity, int>> _mockWardRepo = new();
    private readonly Mock<IRepository<PropertyCategoryEntity, int>> _mockCategoryRepo = new();
    private readonly Mock<IRepository<PropertyTypeMasterEntity, int>> _mockPropertyTypeRepo = new();
    private readonly Mock<IRepository<PropertyMapDetailEntity, int>> _mockPropertyMapDetailRepo = new();
    private readonly Mock<IRepository<PropertyMastOldEntity, int>> _mockPropertyOldRepo = new();
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _mockSocietyRepo = new();
    private readonly Mock<IRepository<WingEntity, int>> _mockWingMasterRepo = new();
    private readonly Mock<IRepository<PropertyPhotoEntity, int>> _mockPropertyPhotoRepo = new();
    private readonly Mock<IRepository<SocietyWingDetailsEntity, int>> _mockSocietyWingRepo = new();
    private readonly Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>> _mockRoomWiseRepo = new();

    private readonly Mock<IRepository<PropertyWorkflowDetailsEntity, int>> _mockWorkflowDetailsRepo = new();
    private readonly Mock<IRepository<PropertyWorkflowStageMasterEntity, int>> _mockWorkflowStageRepo = new();
    private readonly Mock<IRepository<UserEntity, int>> _mockUserRepo = new();
    private readonly Mock<IRepository<CommonRemarkDetailsEntity, int>> _mockCommonRemarkDetailsRepo = new();
    private readonly Mock<IRepository<PropertySurveyVisitEntity, int>> _mockPropertySurveyVisitRepo = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<PropertySurveyService>> _mockLogger = new();

    private readonly PropertySurveyService _service;

    public PropertySurveyServiceTests()
    {
        _service = new PropertySurveyService(
            _mockPropertyRepo.Object,
            _mockModuleMasterRepo.Object,
            _mockWardRepo.Object,
            _mockCategoryRepo.Object,
            _mockPropertyTypeRepo.Object,
            _mockPropertyMapDetailRepo.Object,
            _mockPropertyOldRepo.Object,
            _mockSocietyRepo.Object,
            _mockWingMasterRepo.Object,
            _mockPropertyPhotoRepo.Object,
            _mockSocietyWingRepo.Object,
            _mockRoomWiseRepo.Object,
            _mockWorkflowDetailsRepo.Object,
            _mockWorkflowStageRepo.Object,
            _mockUserRepo.Object,
            _mockCommonRemarkDetailsRepo.Object,
            _mockPropertySurveyVisitRepo.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    private void SetupEmptyRepositories()
    {
        _mockModuleMasterRepo.Setup(r => r.GetQueryable()).Returns(new List<ModuleMasterEntity>().BuildMock());
        _mockPropertyRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>().BuildMock());
        _mockWardRepo.Setup(r => r.GetQueryable()).Returns(new List<WardEntity>().BuildMock());
        _mockCategoryRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCategoryEntity>().BuildMock());
        _mockPropertyTypeRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyTypeMasterEntity>().BuildMock());
        _mockPropertyMapDetailRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyMapDetailEntity>().BuildMock());
        _mockPropertyOldRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockSocietyRepo.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity>().BuildMock());
        _mockWingMasterRepo.Setup(r => r.GetQueryable()).Returns(new List<WingEntity>().BuildMock());
        _mockPropertyPhotoRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyPhotoEntity>().BuildMock());
        _mockSocietyWingRepo.Setup(r => r.GetQueryable()).Returns(new List<SocietyWingDetailsEntity>().BuildMock());
        _mockRoomWiseRepo.Setup(r => r.GetQueryable()).Returns(new List<RoomWiseSubmissionDetailsEntity>().BuildMock());
        _mockWorkflowDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyWorkflowDetailsEntity>().BuildMock());
        _mockWorkflowStageRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyWorkflowStageMasterEntity>().BuildMock());
        _mockUserRepo.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _mockCommonRemarkDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<CommonRemarkDetailsEntity>().BuildMock());
        _mockPropertySurveyVisitRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertySurveyVisitEntity>().BuildMock());
    }

    [Fact]
    public async Task SearchNewlyCreatedPropertiesAsync_InactiveOrMissingModuleId_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreatedByUserPropertySearchRequestDto
        {
            ModuleId = 999,
            UserId = 1,
            WardId = 1,
            PageNumber = 1,
            PageSize = 10
        };

        var modules = new List<ModuleMasterEntity>().BuildMock();
        _mockModuleMasterRepo.Setup(r => r.GetQueryable()).Returns(modules);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SearchNewlyCreatedPropertiesAsync(request, CancellationToken.None));

        Assert.Contains("Invalid or inactive ModuleId: 999", ex.Message);
    }

    [Fact]
    public async Task SearchNewlyCreatedPropertiesAsync_ValidRequest_ReturnsExpectedResponse()
    {
        // Arrange
        var request = new CreatedByUserPropertySearchRequestDto
        {
            ModuleId = 1,
            UserId = 10,
            WardId = 5,
            PageNumber = 1,
            PageSize = 10,
            SearchText = "TestProperty"
        };

        var modules = new List<ModuleMasterEntity>
        {
            new() { Id = 1, IsActive = true }
        }.BuildMock();

        var properties = new List<PropertyEntity>
        {
            new()
            {
                Id = 100,
                CreatedBy = 10,
                WardId = 5,
                IsActive = true,
                MarkedForDeletion = false,
                PropertyNo = "TestPropertyNo_1",
                PartitionNo = "A",
                CategoryId = 2,
                PropertyTypeId = 3,
                OwnerName = "TestPropertyOwner",
                PropertySeqNo = 10
            }
        }.BuildMock();

        var wards = new List<WardEntity>
        {
            new() { Id = 5, WardNo = "Ward_05" }
        }.BuildMock();

        var categories = new List<PropertyCategoryEntity>
        {
            new() { Id = 2, PropertyCategoryName = "Residential Category" }
        }.BuildMock();

        var propertyTypes = new List<PropertyTypeMasterEntity>
        {
            new() { Id = 3, PropertyDescription = "Residential Type", PartType = "NotAmenity", Type = "Ratable" }
        }.BuildMock();

        var mapDetails = new List<PropertyMapDetailEntity>().BuildMock();
        var oldProperties = new List<PropertyMastOldEntity>().BuildMock();
        var societies = new List<SocietyDetailsEntity>().BuildMock();
        var wings = new List<WingEntity>().BuildMock();
        var photos = new List<PropertyPhotoEntity>().BuildMock();
        var societyWings = new List<SocietyWingDetailsEntity>().BuildMock();
        var roomWiseSubmissions = new List<RoomWiseSubmissionDetailsEntity>().BuildMock();

        _mockModuleMasterRepo.Setup(r => r.GetQueryable()).Returns(modules);
        _mockPropertyRepo.Setup(r => r.GetQueryable()).Returns(properties);
        _mockWardRepo.Setup(r => r.GetQueryable()).Returns(wards);
        _mockCategoryRepo.Setup(r => r.GetQueryable()).Returns(categories);
        _mockPropertyTypeRepo.Setup(r => r.GetQueryable()).Returns(propertyTypes);
        _mockPropertyMapDetailRepo.Setup(r => r.GetQueryable()).Returns(mapDetails);
        _mockPropertyOldRepo.Setup(r => r.GetQueryable()).Returns(oldProperties);
        _mockSocietyRepo.Setup(r => r.GetQueryable()).Returns(societies);
        _mockWingMasterRepo.Setup(r => r.GetQueryable()).Returns(wings);
        _mockPropertyPhotoRepo.Setup(r => r.GetQueryable()).Returns(photos);
        _mockSocietyWingRepo.Setup(r => r.GetQueryable()).Returns(societyWings);
        _mockRoomWiseRepo.Setup(r => r.GetQueryable()).Returns(roomWiseSubmissions);

        // Act
        var result = await _service.SearchNewlyCreatedPropertiesAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        var item = result.Items.First();
        Assert.Equal(100, item.Id);
        Assert.Equal("Ward_05", item.WardNo);
        Assert.Equal("Residential Category", item.CategoryName);
        Assert.Equal("Residential Type", item.PropertyDescription);
        Assert.True(item.CanDelete); // Should be true since it has the max PropertySeqNo
    }

    [Fact]
    public async Task CreateVisitAsync_WithValidDetails_CreatesVisitAndReturnsResponse()
    {
        // Arrange
        SetupEmptyRepositories();
        var request = new CreatePropertyVisitTrackerDto
        {
            PropertyId = 1,
            WorkflowStageId = 2,
            ModuleId = 3
        };

        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, IsActive = true, MarkedForDeletion = false }
        }.BuildMock();
        _mockPropertyRepo.Setup(r => r.GetQueryable()).Returns(properties);

        var stages = new List<PropertyWorkflowStageMasterEntity>
        {
            new() { Id = 2, StageName = "Stage 2", IsActive = true }
        }.BuildMock();
        _mockWorkflowStageRepo.Setup(r => r.GetQueryable()).Returns(stages);

        // Act
        var result = await _service.CreateVisitAsync(request, 10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Status);
        Assert.Equal("Property visit recorded successfully.", result.Message);
        Assert.Equal(1, result.PropertyId);
        Assert.Equal(2, result.WorkflowStageId);
        Assert.Equal("Stage 2", result.WorkflowStageName);
        _mockWorkflowDetailsRepo.Verify(r => r.AddAsync(It.IsAny<PropertyWorkflowDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateVisitAsync_WithInvalidPropertyId_ThrowsKeyNotFoundException()
    {
        // Arrange
        SetupEmptyRepositories();
        var request = new CreatePropertyVisitTrackerDto
        {
            PropertyId = 999,
            WorkflowStageId = 2
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.CreateVisitAsync(request, 10, CancellationToken.None));
    }

    [Fact]
    public async Task CreateVisitAsync_WithInvalidWorkflowStageId_ThrowsArgumentException()
    {
        // Arrange
        SetupEmptyRepositories();
        var request = new CreatePropertyVisitTrackerDto
        {
            PropertyId = 1,
            WorkflowStageId = 999
        };

        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, IsActive = true, MarkedForDeletion = false }
        }.BuildMock();
        _mockPropertyRepo.Setup(r => r.GetQueryable()).Returns(properties);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateVisitAsync(request, 10, CancellationToken.None));
    }

    [Fact]
    public async Task GetVisitsAsync_WithValidParameters_ReturnsResponse()
    {
        // Arrange
        SetupEmptyRepositories();
        var queryParams = new PropertyVisitTrackerQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var workflow = new PropertyWorkflowDetailsEntity
        {
            Id = 100,
            PropertyId = 1,
            WorkflowStageId = 2,
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        var stage = new PropertyWorkflowStageMasterEntity
        {
            Id = 2,
            StageName = "StageName",
            IsActive = true
        };

        var property = new PropertyEntity
        {
            Id = 1,
            PropertyNo = "P123",
            IsActive = true,
            MarkedForDeletion = false,
            WardId = 5
        };

        var ward = new WardEntity
        {
            Id = 5,
            WardNo = "W05"
        };

        _mockWorkflowDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyWorkflowDetailsEntity> { workflow }.BuildMock());
        _mockWorkflowStageRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyWorkflowStageMasterEntity> { stage }.BuildMock());
        _mockPropertyRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity> { property }.BuildMock());
        _mockWardRepo.Setup(r => r.GetQueryable()).Returns(new List<WardEntity> { ward }.BuildMock());

        // Act
        var result = await _service.GetVisitsAsync(queryParams, 10, "ADMIN", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Status);
        Assert.Single(result.VisitList);
        Assert.Equal(100, result.VisitList[0].VisitId);
    }

    [Fact]
    public async Task CreateSurveyVisitAsync_WithValidRequest_RecordsVisit()
    {
        // Arrange
        SetupEmptyRepositories();
        var request = new CreatePropertySurveyVisitDto
        {
            PropertyId = 1,
            WorkflowStageId = 2,
            RemarkText = "Visit remark",
            Latitude = 12.34m,
            Longitude = 56.78m,
            Location = "Test Location"
        };

        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, IsActive = true, MarkedForDeletion = false }
        }.BuildMock();
        _mockPropertyRepo.Setup(r => r.GetQueryable()).Returns(properties);

        var stages = new List<PropertyWorkflowStageMasterEntity>
        {
            new() { Id = 2, StageName = "Stage 2", IsActive = true }
        }.BuildMock();
        _mockWorkflowStageRepo.Setup(r => r.GetQueryable()).Returns(stages);

        // Act
        var result = await _service.CreateSurveyVisitAsync(request, 10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Status);
        Assert.Equal("Property survey visit recorded successfully.", result.Message);
        Assert.Equal(12.34m, result.Latitude);
        Assert.Equal(56.78m, result.Longitude);
        Assert.Equal("Test Location", result.Location);
        _mockPropertySurveyVisitRepo.Verify(r => r.AddAsync(It.IsAny<PropertySurveyVisitEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyPropertySurveyVisitAsync_WithoutPhoto_ThrowsArgumentException()
    {
        // Arrange
        SetupEmptyRepositories();
        var request = new VerifyPropertySurveyVisitDto
        {
            PropertyId = 1,
            WorkflowStageId = 2
        };

        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, IsActive = true, MarkedForDeletion = false }
        }.BuildMock();
        _mockPropertyRepo.Setup(r => r.GetQueryable()).Returns(properties);

        var stages = new List<PropertyWorkflowStageMasterEntity>
        {
            new() { Id = 2, IsActive = true }
        }.BuildMock();
        _mockWorkflowStageRepo.Setup(r => r.GetQueryable()).Returns(stages);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<PropertyValidationException>(() =>
            _service.VerifyPropertySurveyVisitAsync(request, 10, CancellationToken.None));
        Assert.Contains("Please click photo before property verification.", ex.Message);
    }

    [Fact]
    public async Task VerifyPropertySurveyVisitAsync_InvalidWorkflowStageId_ThrowsArgumentException()
    {
        // Arrange
        SetupEmptyRepositories();
        var request = new VerifyPropertySurveyVisitDto
        {
            PropertyId = 1,
            WorkflowStageId = 999
        };

        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, IsActive = true, MarkedForDeletion = false }
        }.BuildMock();
        _mockPropertyRepo.Setup(r => r.GetQueryable()).Returns(properties);

        var stages = new List<PropertyWorkflowStageMasterEntity>().BuildMock();
        _mockWorkflowStageRepo.Setup(r => r.GetQueryable()).Returns(stages);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.VerifyPropertySurveyVisitAsync(request, 10, CancellationToken.None));
        Assert.Contains("Invalid or inactive WorkflowStageId: 999", ex.Message);
    }

    [Fact]
    public async Task VerifyPropertySurveyVisitAsync_PhotoMarkedForDeletion_ThrowsArgumentException()
    {
        // Arrange
        SetupEmptyRepositories();
        var request = new VerifyPropertySurveyVisitDto
        {
            PropertyId = 1,
            WorkflowStageId = 2
        };

        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, IsActive = true, MarkedForDeletion = false }
        }.BuildMock();
        _mockPropertyRepo.Setup(r => r.GetQueryable()).Returns(properties);

        var stages = new List<PropertyWorkflowStageMasterEntity>
        {
            new() { Id = 2, IsActive = true }
        }.BuildMock();
        _mockWorkflowStageRepo.Setup(r => r.GetQueryable()).Returns(stages);

        var photos = new List<PropertyPhotoEntity>
        {
            new PropertyPhotoEntity(propertyId: 1, photoTypeId: 2, markedForDeletion: true)
        }.BuildMock();
        _mockPropertyPhotoRepo.Setup(r => r.GetQueryable()).Returns(photos);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<PropertyValidationException>(() =>
            _service.VerifyPropertySurveyVisitAsync(request, 10, CancellationToken.None));
        Assert.Contains("Please click photo before property verification.", ex.Message);
    }

    [Fact]
    public async Task UnverifyPropertySurveyVisitAsync_ValidRequest_ReturnsTrue()
    {
        // Arrange
        SetupEmptyRepositories();
        var request = new UnverifyPropertySurveyVisitDto
        {
            PropertyId = 1,
            RemarkId = 3,
            RemarkText = "Unverify"
        };

        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, IsActive = true, MarkedForDeletion = false }
        }.BuildMock();
        _mockPropertyRepo.Setup(r => r.GetQueryable()).Returns(properties);

        var workflows = new List<PropertyWorkflowDetailsEntity>
        {
            new() { Id = 10, PropertyId = 1, IsActive = true }
        }.BuildMock();
        _mockWorkflowDetailsRepo.Setup(r => r.GetQueryable()).Returns(workflows);

        var visits = new List<PropertySurveyVisitEntity>
        {
            new() { Id = 20, PropertyWorkflowDetailsId = 10, IsActive = true, Latitude = 10, Longitude = 20 }
        }.BuildMock();
        _mockPropertySurveyVisitRepo.Setup(r => r.GetQueryable()).Returns(visits);

        // Act
        var result = await _service.UnverifyPropertySurveyVisitAsync(request, 10, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
