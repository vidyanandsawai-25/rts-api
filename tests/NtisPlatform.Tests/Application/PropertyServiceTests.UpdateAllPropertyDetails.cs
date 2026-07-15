using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using Xunit;
using AutoMapper;

namespace NtisPlatform.Tests.Application;

public class PropertyServiceUpdateAllPropertyDetailsTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _mockPropertyRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IPropertyRepository> _mockCustomPropertyRepo;
    private readonly Mock<ILogger<PropertyService>> _mockLogger;
    private readonly Mock<IOptions<FeatureFlagsOptions>> _mockFeatureFlags;
    private readonly Mock<IRepository<WardEntity, int>> _mockWardRepo;
    private readonly Mock<IRepository<PropertyCategoryEntity, int>> _mockCategoryRepo;
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _mockSocietyRepo;
    private readonly Mock<IRepository<PropertyDetailsEntity, int>> _mockPropertyDetailsRepo;
    private readonly Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>> _mockRoomWiseRepo;
    private readonly Mock<IRepository<PropertyAssessmentEntity, int>> _mockAssessmentRepo;

    private readonly PropertyService _service;

    public PropertyServiceUpdateAllPropertyDetailsTests()
    {
        _mockPropertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockCustomPropertyRepo = new Mock<IPropertyRepository>();
        _mockLogger = new Mock<ILogger<PropertyService>>();
        _mockFeatureFlags = new Mock<IOptions<FeatureFlagsOptions>>();
        _mockWardRepo = new Mock<IRepository<WardEntity, int>>();
        _mockCategoryRepo = new Mock<IRepository<PropertyCategoryEntity, int>>();
        _mockSocietyRepo = new Mock<IRepository<SocietyDetailsEntity, int>>();
        _mockPropertyDetailsRepo = new Mock<IRepository<PropertyDetailsEntity, int>>();
        _mockRoomWiseRepo = new Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>>();
        _mockAssessmentRepo = new Mock<IRepository<PropertyAssessmentEntity, int>>();

        _mockFeatureFlags.Setup(f => f.Value).Returns(new FeatureFlagsOptions());

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
        _service = new PropertyService(
            _mockPropertyRepo.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockCustomPropertyRepo.Object,
            _mockLogger.Object,
            _mockFeatureFlags.Object,
            _mockWardRepo.Object,
            _mockCategoryRepo.Object,
            _mockSocietyRepo.Object,
            _mockPropertyDetailsRepo.Object,
            _mockRoomWiseRepo.Object,
            _mockAssessmentRepo.Object,
            new Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>().Object,
            new Mock<IRepository<PropertyMapMasterEntity, int>>().Object,
            new Mock<IRepository<PropertyMapDetailEntity, int>>().Object,
            new Mock<IRepository<UserEntity, int>>().Object);
    }

    private void SetupBasicMocks(PropertyEntity property, UpdateAllPropertyDetailsDto dto)
    {
        _mockPropertyRepo.Setup(r => r.GetByIdAsync(property.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);
        _mockMapper.Setup(m => m.Map(dto, property)).Returns(property);
    }

    [Fact]
    public async Task UpdatePropertyAsync_WhenPropertyNotFound_ReturnsErrorResponse()
    {
        // Arrange
        int propertyId = 1;
        var dto = new UpdateAllPropertyDetailsDto();

        _mockPropertyRepo.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyEntity?)null);

        // Act
        var result = await _service.UpdatePropertyAsync(propertyId, dto, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(PropertyConstants.ErrorMessages.NotFound);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePropertyAsync_OnException_RollsbackTransaction_AndReturnsError()
    {
        // Arrange
        int propertyId = 1;
        var dto = new UpdateAllPropertyDetailsDto();
        var property = new PropertyEntity { Id = propertyId, UPICId = "UPIC123" };

        SetupBasicMocks(property, dto);

        _mockPropertyRepo.Setup(r => r.UpdateAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.UpdatePropertyAsync(propertyId, dto, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(PropertyConstants.ErrorMessages.UpdateFailed);
        result.UPICID.Should().Be("UPIC123");
        
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePropertyAsync_WithApartmentCategory_AndExistingSociety_UpdatesSociety()
    {
        // Arrange
        int propertyId = 1;
        var dto = new UpdateAllPropertyDetailsDto { CategoryId = 2 };
        var property = new PropertyEntity { Id = propertyId, UPICId = "UPIC123", SocietyDetailId = 99 };
        
        SetupBasicMocks(property, dto);

        var categories = new List<PropertyCategoryEntity> { new PropertyCategoryEntity { Id = 2, PropertyCategoryName = "Apartment/Flat" } }.BuildMock();
        _mockCategoryRepo.Setup(r => r.GetQueryable()).Returns(categories);

        var existingSociety = new SocietyDetailsEntity { Id = 99 };
        _mockSocietyRepo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(existingSociety);
        _mockMapper.Setup(m => m.Map(dto, existingSociety)).Returns(existingSociety);

        _mockAssessmentRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyAssessmentEntity>().BuildMock());
        _mockPropertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyDetailsEntity>().BuildMock());
        _mockRoomWiseRepo.Setup(r => r.GetQueryable()).Returns(new List<RoomWiseSubmissionDetailsEntity>().BuildMock());

        _mockMapper.Setup(m => m.Map<PropertyAssessmentEntity>(dto)).Returns(new PropertyAssessmentEntity());
        _mockMapper.Setup(m => m.Map<PropertyDetailsEntity>(dto)).Returns(new PropertyDetailsEntity());
        _mockMapper.Setup(m => m.Map<RoomWiseSubmissionDetailsEntity>(dto)).Returns(new RoomWiseSubmissionDetailsEntity());

        // Act
        var result = await _service.UpdatePropertyAsync(propertyId, dto, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockSocietyRepo.Verify(r => r.UpdateAsync(existingSociety, It.IsAny<CancellationToken>()), Times.Once);
        _mockSocietyRepo.Verify(r => r.AddAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePropertyAsync_WithApartmentCategory_AndNoSociety_CreatesSociety()
    {
        // Arrange
        int propertyId = 1;
        var dto = new UpdateAllPropertyDetailsDto { CategoryId = 2 };
        var property = new PropertyEntity { Id = propertyId, UPICId = "UPIC123", SocietyDetailId = null };
        
        SetupBasicMocks(property, dto);

        var categories = new List<PropertyCategoryEntity> { new PropertyCategoryEntity { Id = 2, PropertyCategoryName = "Apartment/Flat" } }.BuildMock();
        _mockCategoryRepo.Setup(r => r.GetQueryable()).Returns(categories);

        var newSociety = new SocietyDetailsEntity { Id = 100 };
        _mockMapper.Setup(m => m.Map<SocietyDetailsEntity>(dto)).Returns(newSociety);

        _mockAssessmentRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyAssessmentEntity>().BuildMock());
        _mockPropertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyDetailsEntity>().BuildMock());
        _mockRoomWiseRepo.Setup(r => r.GetQueryable()).Returns(new List<RoomWiseSubmissionDetailsEntity>().BuildMock());

        _mockMapper.Setup(m => m.Map<PropertyAssessmentEntity>(dto)).Returns(new PropertyAssessmentEntity());
        _mockMapper.Setup(m => m.Map<PropertyDetailsEntity>(dto)).Returns(new PropertyDetailsEntity());
        _mockMapper.Setup(m => m.Map<RoomWiseSubmissionDetailsEntity>(dto)).Returns(new RoomWiseSubmissionDetailsEntity());

        // Act
        var result = await _service.UpdatePropertyAsync(propertyId, dto, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockSocietyRepo.Verify(r => r.AddAsync(newSociety, It.IsAny<CancellationToken>()), Times.Once);
        // Verify property is updated to link new society ID
        property.SocietyDetailId.Should().Be(100);
        _mockPropertyRepo.Verify(r => r.UpdateAsync(property, It.IsAny<CancellationToken>()), Times.Exactly(2)); // First time mapping, second time linking
    }

    [Fact]
    public async Task UpdatePropertyAsync_WithExistingAssessment_UpdatesAssessment()
    {
        // Arrange
        int propertyId = 1;
        var dto = new UpdateAllPropertyDetailsDto();
        var property = new PropertyEntity { Id = propertyId, UPICId = "UPIC123" };
        
        SetupBasicMocks(property, dto);

        _mockCategoryRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCategoryEntity>().BuildMock());

        var existingAssessment = new PropertyAssessmentEntity { Id = 10, PropertyId = propertyId };
        var assessments = new List<PropertyAssessmentEntity> { existingAssessment }.BuildMock();
        _mockAssessmentRepo.Setup(r => r.GetQueryable()).Returns(assessments);
        
        _mockMapper.Setup(m => m.Map(dto, existingAssessment)).Returns(existingAssessment);

        _mockPropertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyDetailsEntity>().BuildMock());
        _mockRoomWiseRepo.Setup(r => r.GetQueryable()).Returns(new List<RoomWiseSubmissionDetailsEntity>().BuildMock());
        _mockMapper.Setup(m => m.Map<PropertyDetailsEntity>(dto)).Returns(new PropertyDetailsEntity());
        _mockMapper.Setup(m => m.Map<RoomWiseSubmissionDetailsEntity>(dto)).Returns(new RoomWiseSubmissionDetailsEntity());

        // Act
        var result = await _service.UpdatePropertyAsync(propertyId, dto, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockAssessmentRepo.Verify(r => r.UpdateAsync(existingAssessment, It.IsAny<CancellationToken>()), Times.Once);
        _mockAssessmentRepo.Verify(r => r.AddAsync(It.IsAny<PropertyAssessmentEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePropertyAsync_WithExistingPropertyDetails_AndExistingRoomWise_UpdatesBoth()
    {
        // Arrange
        int propertyId = 1;
        var dto = new UpdateAllPropertyDetailsDto();
        var property = new PropertyEntity { Id = propertyId, UPICId = "UPIC123" };
        
        SetupBasicMocks(property, dto);

        _mockCategoryRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCategoryEntity>().BuildMock());
        _mockAssessmentRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyAssessmentEntity>().BuildMock());
        _mockMapper.Setup(m => m.Map<PropertyAssessmentEntity>(dto)).Returns(new PropertyAssessmentEntity());

        var existingDetails = new PropertyDetailsEntity { Id = 20, PropertyId = propertyId };
        var propertyDetails = new List<PropertyDetailsEntity> { existingDetails }.BuildMock();
        _mockPropertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(propertyDetails);
        _mockMapper.Setup(m => m.Map(dto, existingDetails)).Returns(existingDetails);

        var existingRoomWise = new RoomWiseSubmissionDetailsEntity { Id = 30, PropertyId = propertyId, PropertyDetailsId = 20 };
        var roomWiseList = new List<RoomWiseSubmissionDetailsEntity> { existingRoomWise }.BuildMock();
        _mockRoomWiseRepo.Setup(r => r.GetQueryable()).Returns(roomWiseList);
        _mockMapper.Setup(m => m.Map(dto, existingRoomWise)).Returns(existingRoomWise);

        // Act
        var result = await _service.UpdatePropertyAsync(propertyId, dto, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockPropertyDetailsRepo.Verify(r => r.UpdateAsync(existingDetails, It.IsAny<CancellationToken>()), Times.Once);
        _mockRoomWiseRepo.Verify(r => r.UpdateAsync(existingRoomWise, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePropertyAsync_WithExistingPropertyDetails_AndNoRoomWise_CreatesRoomWise()
    {
        // Arrange
        int propertyId = 1;
        var dto = new UpdateAllPropertyDetailsDto();
        var property = new PropertyEntity { Id = propertyId, UPICId = "UPIC123" };
        
        SetupBasicMocks(property, dto);

        _mockCategoryRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCategoryEntity>().BuildMock());
        _mockAssessmentRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyAssessmentEntity>().BuildMock());
        _mockMapper.Setup(m => m.Map<PropertyAssessmentEntity>(dto)).Returns(new PropertyAssessmentEntity());

        var existingDetails = new PropertyDetailsEntity { Id = 20, PropertyId = propertyId };
        var propertyDetails = new List<PropertyDetailsEntity> { existingDetails }.BuildMock();
        _mockPropertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(propertyDetails);
        _mockMapper.Setup(m => m.Map(dto, existingDetails)).Returns(existingDetails);

        var roomWiseList = new List<RoomWiseSubmissionDetailsEntity>().BuildMock();
        _mockRoomWiseRepo.Setup(r => r.GetQueryable()).Returns(roomWiseList);
        
        var newRoomWise = new RoomWiseSubmissionDetailsEntity();
        _mockMapper.Setup(m => m.Map<RoomWiseSubmissionDetailsEntity>(dto)).Returns(newRoomWise);

        // Act
        var result = await _service.UpdatePropertyAsync(propertyId, dto, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockPropertyDetailsRepo.Verify(r => r.UpdateAsync(existingDetails, It.IsAny<CancellationToken>()), Times.Once);
        _mockRoomWiseRepo.Verify(r => r.AddAsync(newRoomWise, It.IsAny<CancellationToken>()), Times.Once);
        newRoomWise.PropertyDetailsId.Should().Be(20);
    }

    [Fact]
    public async Task UpdatePropertyAsync_WithoutExistingPropertyDetails_CreatesBoth()
    {
        // Arrange
        int propertyId = 1;
        var dto = new UpdateAllPropertyDetailsDto();
        var property = new PropertyEntity { Id = propertyId, UPICId = "UPIC123" };
        
        SetupBasicMocks(property, dto);

        _mockCategoryRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCategoryEntity>().BuildMock());
        _mockAssessmentRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyAssessmentEntity>().BuildMock());
        _mockMapper.Setup(m => m.Map<PropertyAssessmentEntity>(dto)).Returns(new PropertyAssessmentEntity());

        var propertyDetails = new List<PropertyDetailsEntity>().BuildMock();
        _mockPropertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(propertyDetails);

        var newPropertyDetails = new PropertyDetailsEntity { Id = 0 }; // Gets ID after DB Save
        _mockMapper.Setup(m => m.Map<PropertyDetailsEntity>(dto)).Returns(newPropertyDetails);

        var newRoomWise = new RoomWiseSubmissionDetailsEntity();
        _mockMapper.Setup(m => m.Map<RoomWiseSubmissionDetailsEntity>(dto)).Returns(newRoomWise);

        // Act
        var result = await _service.UpdatePropertyAsync(propertyId, dto, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockPropertyDetailsRepo.Verify(r => r.AddAsync(newPropertyDetails, It.IsAny<CancellationToken>()), Times.Once);
        _mockRoomWiseRepo.Verify(r => r.AddAsync(newRoomWise, It.IsAny<CancellationToken>()), Times.Once);
        newRoomWise.PropertyDetails.Should().Be(newPropertyDetails);
    }
}
