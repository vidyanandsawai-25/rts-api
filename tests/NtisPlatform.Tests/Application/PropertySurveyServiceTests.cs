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
            _mockRoomWiseRepo.Object);
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
}
