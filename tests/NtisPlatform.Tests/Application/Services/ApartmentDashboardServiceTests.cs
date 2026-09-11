using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Property.ApartmentQC.ApartmentDashboard;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class ApartmentDashboardServiceTests
{
    private readonly Mock<ILogger<ApartmentDashboardService>> _mockLogger;

    public ApartmentDashboardServiceTests()
    {
        _mockLogger = new Mock<ILogger<ApartmentDashboardService>>();
    }

    [Fact]
    public async Task GetAllAsync_NoProperties_ReturnsEmptyDashboardDto()
    {
        var mockPropertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        mockPropertyRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>().BuildMock());

        var mockWingRepo = new Mock<IRepository<WingDetailsMastEntity, int>>();
        mockWingRepo.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity>().BuildMock());

        var mockPropertyMapRepo = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        mockPropertyMapRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyMapDetailEntity>().BuildMock());

        var mockPropertyDetailsRepo = new Mock<IRepository<PropertyDetailsEntity, int>>();
        mockPropertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyDetailsEntity>().BuildMock());

        var mockPropertyTypeRepo = new Mock<IRepository<PropertyTypeMasterEntity, int>>();
        mockPropertyTypeRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyTypeMasterEntity>().BuildMock());

        var mockTypeOfUseRepo = new Mock<IRepository<TypeOfUseEntity, int>>();
        mockTypeOfUseRepo.Setup(r => r.GetQueryable()).Returns(new List<TypeOfUseEntity>().BuildMock());

        var mockTypeOfUseCatRepo = new Mock<IRepository<TypeOfUseCategoryEntity, int>>();
        mockTypeOfUseCatRepo.Setup(r => r.GetQueryable()).Returns(new List<TypeOfUseCategoryEntity>().BuildMock());

        var mockPropertySocialRepo = new Mock<IRepository<PropertySocialDetailsEntity, int>>();
        mockPropertySocialRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertySocialDetailsEntity>().BuildMock());

        var mockSocialAttrRepo = new Mock<IRepository<SocialAttributeEntity, int>>();
        mockSocialAttrRepo.Setup(r => r.GetQueryable()).Returns(new List<SocialAttributeEntity>().BuildMock());

        var mockWorkflowRepo = new Mock<IRepository<PropertyWorkflowDetailsEntity, int>>();
        mockWorkflowRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyWorkflowDetailsEntity>().BuildMock());

        var mockSurveyRepo = new Mock<IRepository<PropertySurveyVisitEntity, int>>();
        mockSurveyRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertySurveyVisitEntity>().BuildMock());

        var service = new ApartmentDashboardService(
            _mockLogger.Object,
            mockPropertyRepo.Object,
            mockWingRepo.Object,
            mockPropertyMapRepo.Object,
            mockPropertyDetailsRepo.Object,
            mockPropertyTypeRepo.Object,
            mockTypeOfUseRepo.Object,
            mockTypeOfUseCatRepo.Object,
            mockPropertySocialRepo.Object,
            mockSocialAttrRepo.Object,
            mockWorkflowRepo.Object,
            mockSurveyRepo.Object);

        var queryParams = new ApartmentDashboardQueryParameters { SocietyDetailsId = 1, WingId = 0 };

        var result = await service.GetAllAsync(queryParams);

        Assert.NotNull(result);
        Assert.Equal(1, result.SocietyId);
        Assert.Equal(0, result.TotalProperties);
    }

    [Fact]
    public async Task GetAllAsync_CalculatesMetricsCorrectly()
    {
        // Arrange
        var societyId = 10;
        var wingId = 5;

        var wingDetails = new List<WingDetailsMastEntity>
        {
            new WingDetailsMastEntity { Id = 1, SocietyDetailsMastId = societyId, WingMasterId = wingId, IsActive = true }
        };
        
        var properties = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 101, WingDetailId = 1, PropertyTypeId = 1, IsActive = true },
            new PropertyEntity { Id = 102, WingDetailId = 1, PropertyTypeId = 2, IsActive = true },
            new PropertyEntity { Id = 103, WingDetailId = 1, PropertyTypeId = 1, IsActive = true }
        };

        var propertyMapDetails = new List<PropertyMapDetailEntity>
        {
            new PropertyMapDetailEntity { Id = 1, PropertyIdNew = 101, Status = "ACTIVE", IsActive = true }
        };

        var propertyWorkflows = new List<PropertyWorkflowDetailsEntity>
        {
            new PropertyWorkflowDetailsEntity { Id = 1, PropertyId = 101, IsActive = true }
        };

        var propertySurveys = new List<PropertySurveyVisitEntity>
        {
            new PropertySurveyVisitEntity { Id = 1, PropertyWorkflowDetailsId = 1, InternalSurveyVerified = true, IsActive = true }
        };

        var propertyDetails = new List<PropertyDetailsEntity>
        {
            new PropertyDetailsEntity { Id = 1, PropertyId = 101, FloorId = 1, CarpetAreaSqMeter = 10, BuiltupAreaSqMeter = 12, IsActive = true },
            new PropertyDetailsEntity { Id = 2, PropertyId = 103, TypeOfUseId = 1, FloorId = 2, IsActive = true }
        };

        var socialAttributes = new List<SocialAttributeEntity>
        {
            new SocialAttributeEntity { Id = 1, SocialAttributeCode = "HAS_LIFT", IsActive = true }
        };

        var propertySocialDetails = new List<PropertySocialDetailsEntity>
        {
            new PropertySocialDetailsEntity { Id = 1, PropertyId = 101, SocialAttributeId = 1, IntValue = 2, IsActive = true }
        };

        var propertyTypeMasters = new List<PropertyTypeMasterEntity>
        {
            new PropertyTypeMasterEntity { Id = 2, PartType = "Amenity", IsActive = true }
        };

        var typeOfUseCategories = new List<TypeOfUseCategoryEntity>
        {
            new TypeOfUseCategoryEntity { Id = 1, TypeOfUseCategoryName = "PARKING", IsActive = true }
        };

        var typeOfUses = new List<TypeOfUseEntity>
        {
            new TypeOfUseEntity { Id = 1, TypeOfUseCategoryId = 1, IsActive = true }
        };

        var mockPropertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        mockPropertyRepo.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());

        var mockWingRepo = new Mock<IRepository<WingDetailsMastEntity, int>>();
        mockWingRepo.Setup(r => r.GetQueryable()).Returns(wingDetails.BuildMock());

        var mockPropertyMapRepo = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        mockPropertyMapRepo.Setup(r => r.GetQueryable()).Returns(propertyMapDetails.BuildMock());

        var mockPropertyDetailsRepo = new Mock<IRepository<PropertyDetailsEntity, int>>();
        mockPropertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(propertyDetails.BuildMock());

        var mockPropertyTypeRepo = new Mock<IRepository<PropertyTypeMasterEntity, int>>();
        mockPropertyTypeRepo.Setup(r => r.GetQueryable()).Returns(propertyTypeMasters.BuildMock());

        var mockTypeOfUseRepo = new Mock<IRepository<TypeOfUseEntity, int>>();
        mockTypeOfUseRepo.Setup(r => r.GetQueryable()).Returns(typeOfUses.BuildMock());

        var mockTypeOfUseCatRepo = new Mock<IRepository<TypeOfUseCategoryEntity, int>>();
        mockTypeOfUseCatRepo.Setup(r => r.GetQueryable()).Returns(typeOfUseCategories.BuildMock());

        var mockPropertySocialRepo = new Mock<IRepository<PropertySocialDetailsEntity, int>>();
        mockPropertySocialRepo.Setup(r => r.GetQueryable()).Returns(propertySocialDetails.BuildMock());

        var mockSocialAttrRepo = new Mock<IRepository<SocialAttributeEntity, int>>();
        mockSocialAttrRepo.Setup(r => r.GetQueryable()).Returns(socialAttributes.BuildMock());

        var mockWorkflowRepo = new Mock<IRepository<PropertyWorkflowDetailsEntity, int>>();
        mockWorkflowRepo.Setup(r => r.GetQueryable()).Returns(propertyWorkflows.BuildMock());

        var mockSurveyRepo = new Mock<IRepository<PropertySurveyVisitEntity, int>>();
        mockSurveyRepo.Setup(r => r.GetQueryable()).Returns(propertySurveys.BuildMock());

        var service = new ApartmentDashboardService(
            _mockLogger.Object,
            mockPropertyRepo.Object,
            mockWingRepo.Object,
            mockPropertyMapRepo.Object,
            mockPropertyDetailsRepo.Object,
            mockPropertyTypeRepo.Object,
            mockTypeOfUseRepo.Object,
            mockTypeOfUseCatRepo.Object,
            mockPropertySocialRepo.Object,
            mockSocialAttrRepo.Object,
            mockWorkflowRepo.Object,
            mockSurveyRepo.Object);

        var queryParams = new ApartmentDashboardQueryParameters { SocietyDetailsId = societyId, WingId = wingId };

        // Act
        var result = await service.GetAllAsync(queryParams);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(societyId, result.SocietyId);
        Assert.Equal(3, result.TotalProperties);
        Assert.Equal(1, result.TotalWings);
        Assert.Equal(2, result.TotalFloors); // FloorId 1 and 2

        Assert.Equal(1, result.Assessed);
        Assert.Equal(2, result.Unassessed);
        
        Assert.Equal(1, result.TotalAmenities);
        Assert.Equal(1, result.TotalParking);
        Assert.Equal(2, result.TotalLifts);

        Assert.Equal(1, result.InternalSurveyVerified);
        Assert.Equal(2, result.InternalSurveyPending);
        Assert.Equal(1, result.InternalSurveyVerifiedAssessed);
        
        Assert.Equal(1, result.SubmissionComplete);
        Assert.Equal(2, result.SubmissionPending);
        Assert.Equal(1, result.SubmissionCompleteAssessed);
    }
}
