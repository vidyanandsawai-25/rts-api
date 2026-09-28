using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.PropertyBuildingInformation;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class PropertyServiceBuildingSearchTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _propertyRepository;
    private readonly Mock<IRepository<PropertyMastOldEntity, int>> _propertyOldRepository;
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _societyRepository;
    private readonly Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>> _roomWiseRepository;
    private readonly Mock<IRepository<PropertyMapDetailEntity, int>> _propertyMapDetailRepository;

    public PropertyServiceBuildingSearchTests()
    {
        _propertyRepository = new Mock<IRepository<PropertyEntity, int>>();
        _propertyOldRepository = new Mock<IRepository<PropertyMastOldEntity, int>>();
        _societyRepository = new Mock<IRepository<SocietyDetailsEntity, int>>();
        _roomWiseRepository = new Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>>();
        _propertyMapDetailRepository = new Mock<IRepository<PropertyMapDetailEntity, int>>();
    }

    [Fact]
    public async Task SearchBuildingInformationAsync_WithNullOrEmptyDtos_ReturnsEmptyList()
    {
        var service = CreateService();

        var resultNull = await service.SearchBuildingInformationAsync(null!);
        var resultEmpty = await service.SearchBuildingInformationAsync(new List<SearchBuildingInformationDto>());

        Assert.NotNull(resultNull);
        Assert.Empty(resultNull);
        Assert.NotNull(resultEmpty);
        Assert.Empty(resultEmpty);
    }

    [Fact]
    public async Task SearchBuildingInformationAsync_WithValidCriteria_WhenNoPropertyMastLinkage_ReturnsPropertyMastOldData()
    {
        // Arrange
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 101,
                OldWardNo = "1",
                OldSocietyName = "Ganesh Krupa",
                OldPropertyNo = "PROP001",
                OldWing = "A",
                OldFlatOrShopNumber = "101",
                OldPropertyTypeId = 1,
                OldOwnerName = "Ramesh Patil",
                OldRV = 15000,
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        var mapDetails = new List<PropertyMapDetailEntity>
        {
            new()
            {
                PropertyIdOld = 101,
                Status = "ACTIVE",
                IsActive = true
            }
        };

        SetupRepositories(oldProperties: oldProperties, mapDetails: mapDetails);

        var service = CreateService();

        var dtos = new List<SearchBuildingInformationDto>
        {
            new() { OldWardNo = "1", OldSocietyName = "Ganesh Krupa" }
        };

        // Act
        var result = await service.SearchBuildingInformationAsync(dtos);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var item = result[0];
        Assert.Equal(101, item.Id);
        Assert.Equal(0, item.PropertyId);
        Assert.Equal("Ganesh Krupa", item.SocietyName);
        Assert.Equal("PROP001", item.OldPropertyNo);
        Assert.Equal("A", item.OldWing);
        Assert.Equal("101", item.OldFlatOrShopNumber);
        Assert.True(item.Identify);
    }

    [Fact]
    public async Task SearchBuildingInformationAsync_WithValidCriteria_WhenPropertyMastLinkageExists_ReturnsFullData()
    {
        // Arrange
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 101,
                OldWardNo = "1",
                OldSocietyName = "Ganesh Krupa",
                OldPropertyNo = "PROP001",
                OldWing = "A",
                OldFlatOrShopNumber = "101",
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        var properties = new List<PropertyEntity>
        {
            new()
            {
                Id = 501,
                PropertyMastOldId = 101,
                PropertyNo = "NEW001",
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        var societies = new List<SocietyDetailsEntity>
        {
            new()
            {
                Id = 1,
                PropertyId = 501,
                SocietyName = "Ganesh Krupa New",
                BuilderName = "ABC Builders",
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        var roomWise = new List<RoomWiseSubmissionDetailsEntity>
        {
            new()
            {
                Id = 1,
                PropertyId = 501,
                AreaSqMtr = 55.5,
                TotalAreaSqMtr = 60.0,
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        var mapDetails = new List<PropertyMapDetailEntity>
        {
            new()
            {
                PropertyIdOld = 101,
                PropertyIdNew = 501,
                Status = "ACTIVE",
                IsActive = true
            }
        };

        SetupRepositories(
            properties: properties,
            oldProperties: oldProperties,
            societies: societies,
            roomWise: roomWise,
            mapDetails: mapDetails);

        var service = CreateService();

        var dtos = new List<SearchBuildingInformationDto>
        {
            new() { OldWardNo = "1", OldSocietyName = "Ganesh Krupa" }
        };

        // Act
        var result = await service.SearchBuildingInformationAsync(dtos);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var item = result[0];
        Assert.Equal(501, item.PropertyId);
        Assert.Equal(101, item.Id);
        Assert.Equal("ABC Builders", item.BuilderName);
        Assert.Equal(55.5m, item.AreaSqMtr);
        Assert.Equal(60.0m, item.TotalAreaSqMtr);
        Assert.True(item.Identify);
    }

    [Fact]
    public async Task SearchBuildingInformationAsync_SortsWingWiseAndFlatNumberCorrectly()
    {
        // Arrange
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new() { Id = 1, OldWardNo = "1", OldSocietyName = "Soc", OldWing = "B", OldFlatOrShopNumber = "102", IsActive = true },
            new() { Id = 2, OldWardNo = "1", OldSocietyName = "Soc", OldWing = "A", OldFlatOrShopNumber = "101", IsActive = true },
            new() { Id = 3, OldWardNo = "1", OldSocietyName = "Soc", OldWing = "A", OldFlatOrShopNumber = "12", IsActive = true },
            new() { Id = 4, OldWardNo = "1", OldSocietyName = "Soc", OldWing = "A", OldFlatOrShopNumber = "2", IsActive = true }
        };

        SetupRepositories(oldProperties: oldProperties);

        var service = CreateService();

        var dtos = new List<SearchBuildingInformationDto>
        {
            new() { OldWardNo = "1", OldSocietyName = "Soc" }
        };

        // Act
        var result = await service.SearchBuildingInformationAsync(dtos);

        // Assert: Expected order: A-2, A-12, A-101, B-102
        Assert.Equal(4, result.Count);
        Assert.Equal("A", result[0].OldWing);
        Assert.Equal("2", result[0].OldFlatOrShopNumber);
        Assert.Equal("12", result[1].OldFlatOrShopNumber);
        Assert.Equal("101", result[2].OldFlatOrShopNumber);
        Assert.Equal("B", result[3].OldWing);
        Assert.Equal("102", result[3].OldFlatOrShopNumber);
    }

    private void SetupRepositories(
        IEnumerable<PropertyEntity>? properties = null,
        IEnumerable<PropertyMastOldEntity>? oldProperties = null,
        IEnumerable<SocietyDetailsEntity>? societies = null,
        IEnumerable<RoomWiseSubmissionDetailsEntity>? roomWise = null,
        IEnumerable<PropertyMapDetailEntity>? mapDetails = null)
    {
        _propertyRepository.Setup(x => x.GetQueryable())
            .Returns((properties ?? []).ToList().BuildMock());

        _propertyOldRepository.Setup(x => x.GetQueryable())
            .Returns((oldProperties ?? []).ToList().BuildMock());

        _societyRepository.Setup(x => x.GetQueryable())
            .Returns((societies ?? []).ToList().BuildMock());

        _roomWiseRepository.Setup(x => x.GetQueryable())
            .Returns((roomWise ?? []).ToList().BuildMock());

        _propertyMapDetailRepository.Setup(x => x.GetQueryable())
            .Returns((mapDetails ?? []).ToList().BuildMock());
    }

    private PropertyService CreateService()
    {
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockMapper = new Mock<IMapper>();
        var mockPropRepo = new Mock<IPropertyRepository>();
        var mockLogger = new Mock<ILogger<PropertyService>>();
        var mockFlags = new Mock<IOptions<FeatureFlagsOptions>>();
        mockFlags.Setup(f => f.Value).Returns(new FeatureFlagsOptions());

        return new PropertyService(
            _propertyRepository.Object,
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockPropRepo.Object,
            mockLogger.Object,
            mockFlags.Object,
            new Mock<IRepository<WardEntity, int>>().Object,
            new Mock<IRepository<PropertyCategoryEntity, int>>().Object,
            _societyRepository.Object,
            new Mock<IRepository<PropertyDetailsEntity, int>>().Object,
            _roomWiseRepository.Object,
            new Mock<IRepository<PropertyAssessmentEntity, int>>().Object,
            new Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>().Object,
            new Mock<IRepository<OldWardMasterEntity, int>>().Object,
            new Mock<IRepository<PropertyMapMasterEntity, int>>().Object,
            _propertyMapDetailRepository.Object,
            new Mock<IRepository<WingEntity, int>>().Object,
            new Mock<IRepository<UserEntity, int>>().Object,
            _propertyOldRepository.Object,
            new Mock<IRepository<PropertyTypeMasterEntity, int>>().Object,
            new Mock<IRepository<CommunicationDetailsEntity, int>>().Object,
            new Mock<IRepository<PropertyPhotoEntity, int>>().Object,
            new Mock<IRepository<DocumentBindingEntity, int>>().Object,
            new Mock<IRepository<DocumentEntity, int>>().Object,
            new Mock<IRepository<PropertyPhotoTypeEntity, int>>().Object,
            new Mock<IRepository<OwnerTypeMasterEntity, int>>().Object,
            new Mock<IRepository<WingEntity, int>>().Object,
            new Mock<IRepository<SocietyWingDetailsEntity, int>>().Object,
            new Mock<IPropertyRuleApplicationLogService>().Object);
    }
}