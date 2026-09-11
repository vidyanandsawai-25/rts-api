using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Building3DView;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class Building3DViewServiceTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _repository;
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _societyRepository;
    private readonly Mock<IRepository<WingEntity, int>> _wingRepository;
    private readonly Mock<IRepository<WingDetailsMastEntity, int>> _wingDetailsMastRepository;
    private readonly Mock<IRepository<PropertyTypeMasterEntity, int>> _propertyTypeRepository;
    private readonly Mock<IRepository<PropertyAssessmentEntity, int>> _assessmentRepository;
    private readonly Mock<IRepository<PropertyDetailsEntity, int>> _propertyDetailsRepository;
    private readonly Mock<IRepository<FloorEntity, int>> _floorRepository;
    private readonly Mock<IRepository<PropertyWorkflowDetailsEntity, int>> _propertyWorkflowDetailsRepository;
    private readonly Mock<IRepository<PropertyWorkflowStageMasterEntity, int>> _workflowStageRepository;
    private readonly Mock<IRepository<PropertySurveyVisitEntity, int>> _propertySurveyVisitRepository;
    private readonly Mock<IRepository<PropertyMapDetailEntity, int>> _propertyMapDetailRepository;

    public Building3DViewServiceTests()
    {
        _repository = new Mock<IRepository<PropertyEntity, int>>();
        _societyRepository = new Mock<IRepository<SocietyDetailsEntity, int>>();
        _wingRepository = new Mock<IRepository<WingEntity, int>>();
        _wingDetailsMastRepository = new Mock<IRepository<WingDetailsMastEntity, int>>();
        _propertyTypeRepository = new Mock<IRepository<PropertyTypeMasterEntity, int>>();
        _assessmentRepository = new Mock<IRepository<PropertyAssessmentEntity, int>>();
        _propertyDetailsRepository = new Mock<IRepository<PropertyDetailsEntity, int>>();
        _floorRepository = new Mock<IRepository<FloorEntity, int>>();
        _propertyWorkflowDetailsRepository = new Mock<IRepository<PropertyWorkflowDetailsEntity, int>>();
        _workflowStageRepository = new Mock<IRepository<PropertyWorkflowStageMasterEntity, int>>();
        _propertySurveyVisitRepository = new Mock<IRepository<PropertySurveyVisitEntity, int>>();
        _propertyMapDetailRepository = new Mock<IRepository<PropertyMapDetailEntity, int>>();
    }

    private Building3DViewService CreateService()
    {
        return new Building3DViewService(
            _repository.Object,
            _societyRepository.Object,
            _wingRepository.Object,
            _wingDetailsMastRepository.Object,
            _propertyTypeRepository.Object,
            _assessmentRepository.Object,
            _propertyDetailsRepository.Object,
            _floorRepository.Object,
            _propertyWorkflowDetailsRepository.Object,
            _workflowStageRepository.Object,
            _propertySurveyVisitRepository.Object,
            _propertyMapDetailRepository.Object);
    }

    [Fact]
    public async Task GetBuilding3DViewAsync_WhenPropertyNotFound_ReturnsNull()
    {
        // Arrange
        var emptyPropertyList = new List<PropertyEntity>().BuildMock();
        _repository.Setup(x => x.GetQueryable()).Returns(emptyPropertyList);

        var emptySocietyList = new List<SocietyDetailsEntity>().BuildMock();
        _societyRepository.Setup(x => x.GetQueryable()).Returns(emptySocietyList);

        var service = CreateService();

        // Act
        var result = await service.GetBuilding3DViewAsync(new Building3DViewQueryParameters { PropertyId = 999 });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBuilding3DViewAsync_WhenPropertyExists_ReturnsBuilding3DViewDto()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new PropertyEntity
            {
                Id = 1,
                WardId = 10,
                PropertyNo = "P-101",
                PartitionNo = "0",
                OwnerName = "John Doe",
                IsActive = true,
                MarkedForDeletion = false,
                WingDetailId = 100
            },
            new PropertyEntity
            {
                Id = 2,
                WardId = 10,
                PropertyNo = "P-101",
                PartitionNo = "1",
                FlatOrShopNo = "101",
                OwnerName = "Jane Smith",
                IsActive = true,
                MarkedForDeletion = false,
                WingDetailId = 100
            }
        }.BuildMock();

        _repository.Setup(x => x.GetQueryable()).Returns(properties);

        var societies = new List<SocietyDetailsEntity>
        {
            new SocietyDetailsEntity
            {
                Id = 50,
                SocietyName = "Green Park Society",
                PropertyId = 1,
                IsActive = true,
                MarkedForDeletion = false
            }
        }.BuildMock();
        _societyRepository.Setup(x => x.GetQueryable()).Returns(societies);

        var wingDetails = new List<WingDetailsMastEntity>
        {
            new WingDetailsMastEntity
            {
                Id = 100,
                SocietyDetailsMastId = 50,
                WingMasterId = 5,
                WingName = "Wing A",
                IsActive = true,
                MarkedForDeletion = false
            }
        }.BuildMock();
        _wingDetailsMastRepository.Setup(x => x.GetQueryable()).Returns(wingDetails);

        var wings = new List<WingEntity>
        {
            new WingEntity
            {
                Id = 5,
                WingNo = "A",
                IsActive = true
            }
        }.BuildMock();
        _wingRepository.Setup(x => x.GetQueryable()).Returns(wings);

        var propertyTypes = new List<PropertyTypeMasterEntity>
        {
            new PropertyTypeMasterEntity
            {
                Id = 1,
                PartType = "Amenity",
                Type = "R",
                IsActive = true
            }
        }.BuildMock();
        _propertyTypeRepository.Setup(x => x.GetQueryable()).Returns(propertyTypes);

        var assessments = new List<PropertyAssessmentEntity>
        {
            new PropertyAssessmentEntity
            {
                Id = 1,
                PropertyId = 2,
                BHK = "2BHK",
                UnitGenerationType = "Residential",
                IsActive = true,
                MarkedForDeletion = false
            }
        }.BuildMock();
        _assessmentRepository.Setup(x => x.GetQueryable()).Returns(assessments);

        var propDetails = new List<PropertyDetailsEntity>
        {
            new PropertyDetailsEntity
            {
                Id = 1,
                PropertyId = 2,
                FloorId = 3,
                IsActive = true,
                MarkedForDeletion = false
            }
        }.BuildMock();
        _propertyDetailsRepository.Setup(x => x.GetQueryable()).Returns(propDetails);

        var floors = new List<FloorEntity>
        {
            new FloorEntity
            {
                Id = 3,
                FloorCode = "1F",
                Description = "First Floor",
                IsActive = true
            }
        }.BuildMock();
        _floorRepository.Setup(x => x.GetQueryable()).Returns(floors);

        var mapDetails = new List<PropertyMapDetailEntity>().BuildMock();
        _propertyMapDetailRepository.Setup(x => x.GetQueryable()).Returns(mapDetails);

        var workflowDetails = new List<PropertyWorkflowDetailsEntity>().BuildMock();
        _propertyWorkflowDetailsRepository.Setup(x => x.GetQueryable()).Returns(workflowDetails);

        var workflowStages = new List<PropertyWorkflowStageMasterEntity>().BuildMock();
        _workflowStageRepository.Setup(x => x.GetQueryable()).Returns(workflowStages);

        var surveyVisits = new List<PropertySurveyVisitEntity>().BuildMock();
        _propertySurveyVisitRepository.Setup(x => x.GetQueryable()).Returns(surveyVisits);

        var service = CreateService();

        // Act
        var result = await service.GetBuilding3DViewAsync(new Building3DViewQueryParameters { PropertyId = 1 });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.MainPropertyId);
        Assert.Equal("P-101", result.PropertyNo);
        Assert.Equal("John Doe", result.PropertyName);
        Assert.Single(result.Societies);

        var soc = result.Societies[0];
        Assert.Equal(50, soc.SocietyId);
        Assert.Equal("Green Park Society", soc.SocietyName);
        Assert.Equal(5, soc.WingId);
        Assert.Equal("Wing A", soc.WingName);
        Assert.Single(soc.Properties);

        var prop2 = soc.Properties.FirstOrDefault(p => p.PropertyId == 2);
        Assert.NotNull(prop2);
        Assert.Equal("101", prop2.FlatOrShopNo);
        Assert.Equal("2BHK", prop2.BHK);
        Assert.Equal("Residential", prop2.UnitGenerationType);
        Assert.Equal("First Floor", prop2.FloorDescription);
    }

    [Fact]
    public async Task GetBuilding3DViewAsync_WhenMainPropertyHasNoSocietyDetails_ResolvesSocietyFromWingDetails()
    {
        // Arrange: Main property has no direct society record (SocietyDetails record for property 1 does NOT exist)
        var properties = new List<PropertyEntity>
        {
            new PropertyEntity
            {
                Id = 1,
                WardId = 10,
                PropertyNo = "P-101",
                PartitionNo = "0",
                OwnerName = "Main Owner",
                IsActive = true,
                MarkedForDeletion = false,
                WingDetailId = null
            },
            new PropertyEntity
            {
                Id = 2,
                WardId = 10,
                PropertyNo = "P-101",
                PartitionNo = "1",
                FlatOrShopNo = "101",
                OwnerName = "Sibling Owner",
                IsActive = true,
                MarkedForDeletion = false,
                WingDetailId = 100
            }
        }.BuildMock();

        _repository.Setup(x => x.GetQueryable()).Returns(properties);

        // Society details is linked via WingDetailsMastId = 50, but propertyId is 99 (not main property 1)
        var societies = new List<SocietyDetailsEntity>
        {
            new SocietyDetailsEntity
            {
                Id = 50,
                SocietyName = "Sunrise Society",
                PropertyId = 99,
                IsActive = true,
                MarkedForDeletion = false
            }
        }.BuildMock();
        _societyRepository.Setup(x => x.GetQueryable()).Returns(societies);

        var wingDetails = new List<WingDetailsMastEntity>
        {
            new WingDetailsMastEntity
            {
                Id = 100,
                SocietyDetailsMastId = 50,
                WingMasterId = 5,
                WingName = "Wing B",
                IsActive = true,
                MarkedForDeletion = false
            }
        }.BuildMock();
        _wingDetailsMastRepository.Setup(x => x.GetQueryable()).Returns(wingDetails);

        var wings = new List<WingEntity>
        {
            new WingEntity
            {
                Id = 5,
                WingNo = "B",
                IsActive = true
            }
        }.BuildMock();
        _wingRepository.Setup(x => x.GetQueryable()).Returns(wings);

        _propertyTypeRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertyTypeMasterEntity>().BuildMock());
        _assessmentRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertyAssessmentEntity>().BuildMock());
        _propertyDetailsRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertyDetailsEntity>().BuildMock());
        _floorRepository.Setup(x => x.GetQueryable()).Returns(new List<FloorEntity>().BuildMock());
        _propertyMapDetailRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertyMapDetailEntity>().BuildMock());
        _propertyWorkflowDetailsRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertyWorkflowDetailsEntity>().BuildMock());
        _workflowStageRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertyWorkflowStageMasterEntity>().BuildMock());
        _propertySurveyVisitRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertySurveyVisitEntity>().BuildMock());

        var service = CreateService();

        // Act
        var result = await service.GetBuilding3DViewAsync(new Building3DViewQueryParameters { PropertyId = 1 });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.MainPropertyId);
        Assert.Single(result.Societies);
        var soc = result.Societies[0];
        Assert.Equal(50, soc.SocietyId);
        Assert.Equal("Sunrise Society", soc.SocietyName);
        Assert.Single(soc.Properties);
        Assert.Equal(2, soc.Properties[0].PropertyId);
    }

    [Fact]
    public async Task GetBuilding3DViewAsync_PopulatesMainPropertyDetails_ForNonSocietyRegularProperties()
    {
        // Arrange: Main property 1 and standalone regular unit 3 with no WingDetailId
        var properties = new List<PropertyEntity>
        {
            new PropertyEntity
            {
                Id = 1,
                WardId = 10,
                PropertyNo = "P-101",
                PartitionNo = "0",
                OwnerName = "Main Owner",
                IsActive = true,
                MarkedForDeletion = false,
                WingDetailId = null
            },
            new PropertyEntity
            {
                Id = 3,
                WardId = 10,
                PropertyNo = "P-101",
                PartitionNo = "2",
                FlatOrShopNo = "201",
                OwnerName = "Standalone Unit Owner",
                PropertyTypeId = 2, // Regular property type
                IsActive = true,
                MarkedForDeletion = false,
                WingDetailId = null
            }
        }.BuildMock();

        _repository.Setup(x => x.GetQueryable()).Returns(properties);
        _societyRepository.Setup(x => x.GetQueryable()).Returns(new List<SocietyDetailsEntity>().BuildMock());
        _wingDetailsMastRepository.Setup(x => x.GetQueryable()).Returns(new List<WingDetailsMastEntity>().BuildMock());
        _wingRepository.Setup(x => x.GetQueryable()).Returns(new List<WingEntity>().BuildMock());
        _propertyTypeRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertyTypeMasterEntity>().BuildMock());
        _assessmentRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertyAssessmentEntity>().BuildMock());
        _propertyDetailsRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertyDetailsEntity>().BuildMock());
        _floorRepository.Setup(x => x.GetQueryable()).Returns(new List<FloorEntity>().BuildMock());
        _propertyMapDetailRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertyMapDetailEntity>().BuildMock());
        _propertyWorkflowDetailsRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertyWorkflowDetailsEntity>().BuildMock());
        _workflowStageRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertyWorkflowStageMasterEntity>().BuildMock());
        _propertySurveyVisitRepository.Setup(x => x.GetQueryable()).Returns(new List<PropertySurveyVisitEntity>().BuildMock());

        var service = CreateService();

        // Act
        var result = await service.GetBuilding3DViewAsync(new Building3DViewQueryParameters { PropertyId = 1 });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.MainPropertyId);
        Assert.Empty(result.Societies);
        Assert.Single(result.MainPropertyDetails);
        Assert.Equal(3, result.MainPropertyDetails[0].PropertyId);
        Assert.Equal("201", result.MainPropertyDetails[0].FlatOrShopNo);
        Assert.Equal("Standalone Unit Owner", result.MainPropertyDetails[0].OwnerName);
    }
}
