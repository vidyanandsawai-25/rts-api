using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.OldSociety;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class PropertyServiceOldSocietiesTests
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

    private readonly PropertyService _service;

    public PropertyServiceOldSocietiesTests()
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
            _mockRuleLogService.Object);
    }

    private static IQueryable<T> BuildMockDbQuery<T>(IEnumerable<T> data) where T : class
    {
        return data.ToList().BuildMock<T>();
    }

    [Fact]
    public async Task GetOldSocietiesAsync_WhenWardNotFound_ReturnsEmptyResponse()
    {
        // Arrange
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(BuildMockDbQuery(new List<WardEntity>()));

        var dto = new SearchOldSocietyDto { WardNo = "NON_EXISTENT_WARD" };

        // Act
        var result = await _service.GetOldSocietiesAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public async Task GetOldSocietiesAsync_WhenNoAllocatedOldWards_ReturnsEmptyResponse()
    {
        // Arrange
        var wards = new List<WardEntity>
        {
            new() { Id = 6, WardNo = "RMC6", IsActive = true }
        };

        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(BuildMockDbQuery(wards));
        _mockWardAllocationRepository.Setup(r => r.GetQueryable()).Returns(BuildMockDbQuery(new List<GlobalSurveyWardAllocationEntity>()));
        _mockOldWardMasterRepository.Setup(r => r.GetQueryable()).Returns(BuildMockDbQuery(new List<OldWardMasterEntity>()));

        var dto = new SearchOldSocietyDto { WardNo = "RMC6" };

        // Act
        var result = await _service.GetOldSocietiesAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public async Task GetOldSocietiesAsync_WhenValidSocietiesExist_GroupsAndCalculatesCountsCorrectly()
    {
        // Arrange
        var wards = new List<WardEntity>
        {
            new() { Id = 6, WardNo = "RMC6", IsActive = true }
        };

        var allocations = new List<GlobalSurveyWardAllocationEntity>
        {
            new() { Id = 1, WardId = 6, OldWardId = 4, IsActive = true }
        };

        var oldWards = new List<OldWardMasterEntity>
        {
            new() { Id = 4, OldWardNo = "3", IsActive = true }
        };

        var mapDetails = new List<PropertyMapDetailEntity>();

        var oldProperties = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 1,
                OldWardNo = "3",
                OldSocietyName = "Shree Ganesh Society",
                OldAddress = "Main Road, Ratnagiri",
                OldWing = "A",
                OldFlatOrShopNumber = "101",
                IsActive = true,
                MarkedForDeletion = false
            },
            new()
            {
                Id = 2,
                OldWardNo = "3",
                OldSocietyName = "Shree Ganesh Society",
                OldAddress = "Main Road, Ratnagiri",
                OldWing = "A",
                OldFlatOrShopNumber = "102",
                IsActive = true,
                MarkedForDeletion = false
            },
            new()
            {
                Id = 3,
                OldWardNo = "3",
                OldSocietyName = "Shree Ganesh Society",
                OldAddress = "Main Road, Ratnagiri",
                OldWing = "B",
                OldFlatOrShopNumber = "101",
                IsActive = true,
                MarkedForDeletion = false
            },
            new()
            {
                Id = 4,
                OldWardNo = "3",
                OldSocietyName = "Sai Krupa Apartment",
                OldAddress = "Station Road, Ratnagiri",
                OldWing = "A",
                OldFlatOrShopNumber = "G-1",
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(BuildMockDbQuery(wards));
        _mockWardAllocationRepository.Setup(r => r.GetQueryable()).Returns(BuildMockDbQuery(allocations));
        _mockOldWardMasterRepository.Setup(r => r.GetQueryable()).Returns(BuildMockDbQuery(oldWards));
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(BuildMockDbQuery(mapDetails));
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(BuildMockDbQuery(oldProperties));

        var dto = new SearchOldSocietyDto
        {
            WardNo = "RMC6",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetOldSocietiesAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result.Data.Count);

        var saiKrupa = result.Data.FirstOrDefault(x => x.OldSocietyName == "Sai Krupa Apartment");
        Assert.NotNull(saiKrupa);
        Assert.Equal(1, saiKrupa.TotalWing);
        Assert.Equal(1, saiKrupa.TotalFlatOrShop);
        Assert.Equal("A", saiKrupa.Wings);

        var shreeGanesh = result.Data.FirstOrDefault(x => x.OldSocietyName == "Shree Ganesh Society");
        Assert.NotNull(shreeGanesh);
        Assert.Equal(2, shreeGanesh.TotalWing);
        Assert.Equal(3, shreeGanesh.TotalFlatOrShop); // A-101, A-102, B-101
        Assert.Equal("A, B", shreeGanesh.Wings);
    }

    [Fact]
    public async Task GetOldSocietiesAsync_WhenActiveMappedPropertiesExist_ExcludesActiveMappedProperties()
    {
        // Arrange
        var wards = new List<WardEntity>
        {
            new() { Id = 6, WardNo = "RMC6", IsActive = true }
        };

        var allocations = new List<GlobalSurveyWardAllocationEntity>
        {
            new() { Id = 1, WardId = 6, OldWardId = 4, IsActive = true }
        };

        var oldWards = new List<OldWardMasterEntity>
        {
            new() { Id = 4, OldWardNo = "3", IsActive = true }
        };

        var mapDetails = new List<PropertyMapDetailEntity>
        {
            new()
            {
                PropertyIdOld = 1,
                Status = "ACTIVE",
                IsActive = true
            }
        };

        var oldProperties = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 1,
                OldWardNo = "3",
                OldSocietyName = "Mapped Society",
                IsActive = true,
                MarkedForDeletion = false
            },
            new()
            {
                Id = 2,
                OldWardNo = "3",
                OldSocietyName = "Unmapped Society",
                OldWing = "A",
                OldFlatOrShopNumber = "1",
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(BuildMockDbQuery(wards));
        _mockWardAllocationRepository.Setup(r => r.GetQueryable()).Returns(BuildMockDbQuery(allocations));
        _mockOldWardMasterRepository.Setup(r => r.GetQueryable()).Returns(BuildMockDbQuery(oldWards));
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(BuildMockDbQuery(mapDetails));
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(BuildMockDbQuery(oldProperties));

        var dto = new SearchOldSocietyDto
        {
            WardNo = "RMC6",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetOldSocietiesAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.Equal("Unmapped Society", result.Data[0].OldSocietyName);
    }
}
