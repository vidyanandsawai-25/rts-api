using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class PropertyServiceMappedOldPropertyDetailsTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
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
    private readonly Mock<IRepository<OldWardMasterEntity, int>> _mockOldWardMasterRepository;
    private readonly Mock<IRepository<PropertyMapMasterEntity, int>> _mockPropertyMapMasterRepository;
    private readonly Mock<IRepository<PropertyMapDetailEntity, int>> _mockPropertyMapDetailRepository;
    private readonly Mock<IRepository<WingEntity, int>> _mockWingMasterRepository;
    private readonly Mock<IRepository<UserEntity, int>> _mockUserRepository;
    private readonly Mock<IRepository<PropertyMastOldEntity, int>> _mockPropertyOldRepository;
    private readonly Mock<IRepository<PropertyTypeMasterEntity, int>> _mockPropertyTypeRepository;
    private readonly Mock<IRepository<CommunicationDetailsEntity, int>> _mockCommunicationRepository;
    private readonly Mock<IRepository<PropertyPhotoEntity, int>> _mockPropertyPhotoRepository;
    private readonly Mock<IRepository<DocumentBindingEntity, int>> _mockDocumentBindingRepository;
    private readonly Mock<IRepository<DocumentEntity, int>> _mockDocumentRepository;
    private readonly Mock<IRepository<PropertyPhotoTypeEntity, int>> _mockPropertyPhotoTypeRepository;
    private readonly Mock<IRepository<OwnerTypeMasterEntity, int>> _mockOwnerTypeRepository;
    private readonly Mock<IRepository<WingEntity, int>> _mockWingRepository;
    private readonly Mock<IRepository<SocietyWingDetailsEntity, int>> _mockSocietyWingRepository;
    private readonly Mock<IPropertyRuleApplicationLogService> _mockRuleLogService;
    private readonly Mock<IRepository<PropertyDetailsOldEntity, int>> _mockPropertyDetailsOldRepository;

    private readonly PropertyService _service;

    public PropertyServiceMappedOldPropertyDetailsTests()
    {
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new MappedOldPropertyMappingProfile());
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = mappingConfig.CreateMapper();

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
        _mockOldWardMasterRepository = new Mock<IRepository<OldWardMasterEntity, int>>();
        _mockPropertyMapMasterRepository = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        _mockPropertyMapDetailRepository = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        _mockWingMasterRepository = new Mock<IRepository<WingEntity, int>>();
        _mockUserRepository = new Mock<IRepository<UserEntity, int>>();
        _mockPropertyOldRepository = new Mock<IRepository<PropertyMastOldEntity, int>>();
        _mockPropertyTypeRepository = new Mock<IRepository<PropertyTypeMasterEntity, int>>();
        _mockCommunicationRepository = new Mock<IRepository<CommunicationDetailsEntity, int>>();
        _mockPropertyPhotoRepository = new Mock<IRepository<PropertyPhotoEntity, int>>();
        _mockDocumentBindingRepository = new Mock<IRepository<DocumentBindingEntity, int>>();
        _mockDocumentRepository = new Mock<IRepository<DocumentEntity, int>>();
        _mockPropertyPhotoTypeRepository = new Mock<IRepository<PropertyPhotoTypeEntity, int>>();
        _mockOwnerTypeRepository = new Mock<IRepository<OwnerTypeMasterEntity, int>>();
        _mockWingRepository = new Mock<IRepository<WingEntity, int>>();
        _mockSocietyWingRepository = new Mock<IRepository<SocietyWingDetailsEntity, int>>();
        _mockRuleLogService = new Mock<IPropertyRuleApplicationLogService>();
        _mockPropertyDetailsOldRepository = new Mock<IRepository<PropertyDetailsOldEntity, int>>();

        _mockFeatureFlags.Setup(f => f.Value).Returns(new FeatureFlagsOptions());

        _service = new PropertyService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mapper,
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
            _mockOldWardMasterRepository.Object,
            _mockPropertyMapMasterRepository.Object,
            _mockPropertyMapDetailRepository.Object,
            _mockWingMasterRepository.Object,
            _mockUserRepository.Object,
            _mockPropertyOldRepository.Object,
            _mockPropertyTypeRepository.Object,
            _mockCommunicationRepository.Object,
            _mockPropertyPhotoRepository.Object,
            _mockDocumentBindingRepository.Object,
            _mockDocumentRepository.Object,
            _mockPropertyPhotoTypeRepository.Object,
            _mockOwnerTypeRepository.Object,
            _mockWingRepository.Object,
            _mockSocietyWingRepository.Object,
            _mockRuleLogService.Object,
            null,
            null,
            null,
            null,
            _mockPropertyDetailsOldRepository.Object);
    }

    private static IQueryable<T> BuildMockDbQuery<T>(IEnumerable<T> data) where T : class
    {
        return data.ToList().BuildMock<T>();
    }

    [Fact]
    public async Task GetMappedOldPropertyDetailsAsync_WhenNoMapDetailsFound_ReturnsEmptyPagedResult()
    {
        // Arrange
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(BuildMockDbQuery(new List<PropertyMapDetailEntity>()));

        var query = new MappedOldPropertyQueryParameters
        {
            PropertyId = 100,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetMappedOldPropertyDetailsAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetMappedOldPropertyDetailsAsync_WhenMappedOldPropertiesAndFloorDetailsExist_ReturnsPagedResultWithNestedFloors()
    {
        // Arrange
        int propertyIdNew = 101;
        int propertyIdOld = 501;

        var mapDetails = new List<PropertyMapDetailEntity>
        {
            new()
            {
                Id = 1,
                PropertyIdNew = propertyIdNew,
                PropertyIdOld = propertyIdOld,
                IsActive = true
            }
        };

        var oldMaster = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = propertyIdOld,
                OldWardNo = "W-01",
                OldPropertyNo = "P-123",
                OldOwnerName = "John Doe",
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        var oldFloors = new List<PropertyDetailsOldEntity>
        {
            new()
            {
                Id = 1001,
                PropertyMastOldId = propertyIdOld,
                OldFloorId = 1,
                OldCarpetAreaSqFeet = 450.5,
                IsActive = true,
                MarkedForDeletion = false
            },
            new()
            {
                Id = 1002,
                PropertyMastOldId = propertyIdOld,
                OldFloorId = 2,
                OldCarpetAreaSqFeet = 550.0,
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(BuildMockDbQuery(mapDetails));
        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(BuildMockDbQuery(oldMaster));
        _mockPropertyDetailsOldRepository.Setup(r => r.GetQueryable())
            .Returns(BuildMockDbQuery(oldFloors));

        var query = new MappedOldPropertyQueryParameters
        {
            PropertyId = propertyIdNew,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetMappedOldPropertyDetailsAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);

        var item = result.Items.First();
        Assert.Equal(propertyIdOld, item.Id);
        Assert.Equal(propertyIdNew, item.MappedNewBuildingId);
        Assert.Equal("W-01", item.OldWardNo);
        Assert.Equal("P-123", item.OldPropertyNo);
        Assert.Equal("John Doe", item.OldOwnerName);

        Assert.Equal(2, item.FloorDetails.Count);
        Assert.Equal(1001, item.FloorDetails[0].Id);
        Assert.Equal(propertyIdOld, item.FloorDetails[0].PropertyMastOldId);
        Assert.Equal(1002, item.FloorDetails[1].Id);
    }

    [Fact]
    public async Task GetMappedOldPropertyDetailsAsync_WhenMarkedForDeletion_FiltersOutDeletedRecords()
    {
        // Arrange
        int propertyIdNew = 102;
        int activeOldId = 502;
        int deletedOldId = 503;

        var mapDetails = new List<PropertyMapDetailEntity>
        {
            new() { Id = 1, PropertyIdNew = propertyIdNew, PropertyIdOld = activeOldId, IsActive = true },
            new() { Id = 2, PropertyIdNew = propertyIdNew, PropertyIdOld = deletedOldId, IsActive = true }
        };

        var oldMaster = new List<PropertyMastOldEntity>
        {
            new() { Id = activeOldId, OldPropertyNo = "ACTIVE-PROP", IsActive = true, MarkedForDeletion = false },
            new() { Id = deletedOldId, OldPropertyNo = "DELETED-PROP", IsActive = true, MarkedForDeletion = true }
        };

        var oldFloors = new List<PropertyDetailsOldEntity>
        {
            new() { Id = 2001, PropertyMastOldId = activeOldId, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2002, PropertyMastOldId = activeOldId, IsActive = true, MarkedForDeletion = true }
        };

        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(BuildMockDbQuery(mapDetails));
        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(BuildMockDbQuery(oldMaster));
        _mockPropertyDetailsOldRepository.Setup(r => r.GetQueryable())
            .Returns(BuildMockDbQuery(oldFloors));

        var query = new MappedOldPropertyQueryParameters
        {
            PropertyId = propertyIdNew,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetMappedOldPropertyDetailsAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        var activeItem = result.Items.First();
        Assert.Equal(activeOldId, activeItem.Id);
        Assert.Single(activeItem.FloorDetails);
        Assert.Equal(2001, activeItem.FloorDetails[0].Id);
    }

    [Fact]
    public async Task GetMappedOldPropertyDetailsAsync_WhenPaginationApplied_CorrectlyPagesData()
    {
        // Arrange
        int propertyIdNew = 103;

        var mapDetails = new List<PropertyMapDetailEntity>
        {
            new() { Id = 1, PropertyIdNew = propertyIdNew, PropertyIdOld = 601, IsActive = true },
            new() { Id = 2, PropertyIdNew = propertyIdNew, PropertyIdOld = 602, IsActive = true },
            new() { Id = 3, PropertyIdNew = propertyIdNew, PropertyIdOld = 603, IsActive = true }
        };

        var oldMaster = new List<PropertyMastOldEntity>
        {
            new() { Id = 601, OldPropertyNo = "P1", IsActive = true, MarkedForDeletion = false },
            new() { Id = 602, OldPropertyNo = "P2", IsActive = true, MarkedForDeletion = false },
            new() { Id = 603, OldPropertyNo = "P3", IsActive = true, MarkedForDeletion = false }
        };

        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable())
            .Returns(BuildMockDbQuery(mapDetails));
        _mockPropertyOldRepository.Setup(r => r.GetQueryable())
            .Returns(BuildMockDbQuery(oldMaster));
        _mockPropertyDetailsOldRepository.Setup(r => r.GetQueryable())
            .Returns(BuildMockDbQuery(new List<PropertyDetailsOldEntity>()));

        var query = new MappedOldPropertyQueryParameters
        {
            PropertyId = propertyIdNew,
            PageNumber = 2,
            PageSize = 1
        };

        // Act
        var result = await _service.GetMappedOldPropertyDetailsAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(1, result.PageSize);
        Assert.Single(result.Items);
        Assert.Equal(602, result.Items.First().Id);
    }
}
