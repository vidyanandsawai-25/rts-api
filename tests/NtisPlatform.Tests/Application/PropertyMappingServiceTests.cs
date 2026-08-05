using AutoMapper;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.PropertyMapDetails;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive unit tests for <see cref="PropertyMappingService"/> focusing on
/// <see cref="PropertyMappingService.GetPropertyMatchingDetailsAsync"/>.
/// </summary>
public class PropertyMappingServiceTests
{
    private readonly Mock<IRepository<PropertyMapMasterEntity, int>> _mockPropertyMapMasterRepository;
    private readonly Mock<IRepository<PropertyMastOldEntity, int>> _mockPropertyOldRepository;
    private readonly Mock<IRepository<PropertyMapDetailEntity, int>> _mockPropertyMapDetailRepository;
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IRepository<WardEntity, int>> _mockWardRepository;
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _mockSocietyRepository;
    private readonly Mock<IRepository<PropertyAssessmentEntity, int>> _mockAssessmentRepository;
    private readonly Mock<IRepository<PropertyDetailsEntity, int>> _mockPropertyDetailsRepository;
    private readonly Mock<IRepository<PropertyTypeMasterEntity, int>> _mockPropertyTypeRepository;
    private readonly Mock<IRepository<SubTypeOfUseEntity, int>> _mockSubTypeOfUseRepository;
    private readonly Mock<IRepository<ConstructionTypeEntity, int>> _mockConstructionTypeRepository;
    private readonly Mock<IRepository<UserEntity, int>> _mockUserRepository;
    private readonly Mock<IRepository<WingEntity, int>> _mockWingMasterRepository;
    private readonly Mock<IRepository<TypeOfUseEntity, int>> _mockTypeOfUseRepository;
    private readonly Mock<IRepository<FloorEntity, int>> _mockFloorRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<PropertyMappingService>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertyMappingService _service;

    public PropertyMappingServiceTests()
    {
        _mockPropertyMapMasterRepository = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        _mockPropertyOldRepository = new Mock<IRepository<PropertyMastOldEntity, int>>();
        _mockPropertyMapDetailRepository = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockWardRepository = new Mock<IRepository<WardEntity, int>>();
        _mockSocietyRepository = new Mock<IRepository<SocietyDetailsEntity, int>>();
        _mockAssessmentRepository = new Mock<IRepository<PropertyAssessmentEntity, int>>();
        _mockPropertyDetailsRepository = new Mock<IRepository<PropertyDetailsEntity, int>>();
        _mockPropertyTypeRepository = new Mock<IRepository<PropertyTypeMasterEntity, int>>();
        _mockSubTypeOfUseRepository = new Mock<IRepository<SubTypeOfUseEntity, int>>();
        _mockConstructionTypeRepository = new Mock<IRepository<ConstructionTypeEntity, int>>();
        _mockUserRepository = new Mock<IRepository<UserEntity, int>>();
        _mockWingMasterRepository = new Mock<IRepository<WingEntity, int>>();
        _mockTypeOfUseRepository = new Mock<IRepository<TypeOfUseEntity, int>>();
        _mockFloorRepository = new Mock<IRepository<FloorEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<PropertyMappingService>>();
        _mockMapper = new Mock<IMapper>();

        SetDefaultEmptyRepositories();

        _service = new PropertyMappingService(
            _mockPropertyMapMasterRepository.Object,
            _mockPropertyOldRepository.Object,
            _mockPropertyMapDetailRepository.Object,
            _mockRepository.Object,
            _mockWardRepository.Object,
            _mockSocietyRepository.Object,
            _mockAssessmentRepository.Object,
            _mockPropertyDetailsRepository.Object,
            _mockPropertyTypeRepository.Object,
            _mockSubTypeOfUseRepository.Object,
            _mockConstructionTypeRepository.Object,
            _mockUserRepository.Object,
            _mockWingMasterRepository.Object,
            _mockTypeOfUseRepository.Object,
            _mockFloorRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockMapper.Object);
    }

    private void SetDefaultEmptyRepositories()
    {
        _mockPropertyMapMasterRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMapMasterEntity>().BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMastOldEntity>().BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMapDetailEntity>().BuildMock());
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>().BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(new List<WardEntity>().BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity>().BuildMock());
        _mockAssessmentRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyAssessmentEntity>().BuildMock());
        _mockPropertyDetailsRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyDetailsEntity>().BuildMock());
        _mockPropertyTypeRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyTypeMasterEntity>().BuildMock());
        _mockSubTypeOfUseRepository.Setup(r => r.GetQueryable()).Returns(new List<SubTypeOfUseEntity>().BuildMock());
        _mockConstructionTypeRepository.Setup(r => r.GetQueryable()).Returns(new List<ConstructionTypeEntity>().BuildMock());
        _mockUserRepository.Setup(r => r.GetQueryable()).Returns(new List<UserEntity>().BuildMock());
        _mockWingMasterRepository.Setup(r => r.GetQueryable()).Returns(new List<WingEntity>().BuildMock());
        _mockTypeOfUseRepository.Setup(r => r.GetQueryable()).Returns(new List<TypeOfUseEntity>().BuildMock());
        _mockFloorRepository.Setup(r => r.GetQueryable()).Returns(new List<FloorEntity>().BuildMock());
    }

    #region Step 1 & Step 2 Validation Tests

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_WhenPropertyNotFound_ReturnsEmptyList()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 999, CreatedBy = 1 };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>().BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_WhenPropertyInactiveOrMarkedForDeletion_ReturnsEmptyList()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, CreatedBy = 1 };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 10, WardId = 1, PropertyNo = "P-100", IsActive = false, MarkedForDeletion = false },
            new() { Id = 11, WardId = 1, PropertyNo = "P-101", IsActive = true, MarkedForDeletion = true }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_WhenNoBasePropertiesFound_ReturnsEmptyList()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, CreatedBy = 1 };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 10, WardId = 1, PropertyNo = "P-100", IsActive = true, MarkedForDeletion = false }
        };
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(properties.Where(p => p.Id == 10).ToList().BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_WhenBasePropertiesHaveNoSocietyDetailId_ReturnsEmptyList()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, CreatedBy = 1 };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 10, WardId = 1, PropertyNo = "P-100", SocietyDetailId = null, IsActive = true, MarkedForDeletion = false }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Step 3 Wing Key Filter Tests

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_WhenWingIdSpecifiedAndWingNotFound_ReturnsEmptyList()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, SocietyId = 5, CreatedBy = 1 };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 10, WardId = 1, PropertyNo = "P-100", SocietyDetailId = 50, PartitionNo = "101", IsActive = true, MarkedForDeletion = false }
        };
        var societies = new List<SocietyDetailsEntity>
        {
            new() { Id = 50, WingId = 99, WingName = "Wing B", IsActive = true, MarkedForDeletion = false }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(societies.BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Step 4 New Property Query & Filter Tests

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_WhenNoRawNewPropertiesMatch_ReturnsEmptyList()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, CreatedBy = 1 };
        var properties = new List<PropertyEntity>
        {
            // PartitionNo is whitespace -> should be excluded in Step 4
            new() { Id = 10, WardId = 1, PropertyNo = "P-100", SocietyDetailId = 50, PartitionNo = "   ", IsActive = true, MarkedForDeletion = false }
        };
        var societies = new List<SocietyDetailsEntity>
        {
            new() { Id = 50, WingId = 1, WingName = "A", IsActive = true, MarkedForDeletion = false }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(societies.BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_WhenPropertyTypeIsAmenity_ExcludesProperty()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, CreatedBy = 1 };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 10, WardId = 1, PropertyNo = "P-100", SocietyDetailId = 50, PartitionNo = "A-1", PropertyTypeId = 100, IsActive = true, MarkedForDeletion = false }
        };
        var societies = new List<SocietyDetailsEntity>
        {
            new() { Id = 50, WingId = 1, WingName = "A", IsActive = true, MarkedForDeletion = false }
        };
        var propertyTypes = new List<PropertyTypeMasterEntity>
        {
            new() { Id = 100, PropertyDescription = "Club House", PartType = "Amenity", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(societies.BuildMock());
        _mockPropertyTypeRepository.Setup(r => r.GetQueryable()).Returns(propertyTypes.BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_WhenPartitionNoMatchesWingMasterWingNo_ExcludesProperty()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, CreatedBy = 1 };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 10, WardId = 1, PropertyNo = "P-100", SocietyDetailId = 50, PartitionNo = "WING-A", IsActive = true, MarkedForDeletion = false }
        };
        var societies = new List<SocietyDetailsEntity>
        {
            new() { Id = 50, WingId = 1, WingName = "WING-A", IsActive = true, MarkedForDeletion = false }
        };
        var wings = new List<WingEntity>
        {
            new() { Id = 1, WingNo = "WING-A", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(societies.BuildMock());
        _mockWingMasterRepository.Setup(r => r.GetQueryable()).Returns(wings.BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Detail Enrichments & Deduplication Tests

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_OnlyNewProperties_EnrichesAssessmentsAndLookupsCorrectly()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, CreatedBy = 1 };
        var properties = new List<PropertyEntity>
        {
            new()
            {
                Id = 10,
                WardId = 1,
                PropertyNo = "P-100",
                PartitionNo = "101",
                SocietyDetailId = 50,
                FlatOrShopNo = "101",
                FlatOrShopName = "Galaxy",
                MobileNo = "9876543210",
                PropertyTypeId = 2,
                OwnerName = "John Doe",
                OccupierName = "Jane Doe",
                Type = "Residential",
                IsActive = true,
                MarkedForDeletion = false
            }
        };
        var societies = new List<SocietyDetailsEntity>
        {
            new() { Id = 50, WingId = 1, WingName = "A Wing", IsActive = true, MarkedForDeletion = false }
        };
        var assessments = new List<PropertyAssessmentEntity>
        {
            new() { Id = 1, PropertyId = 10, BHK = "1BHK", IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, PropertyId = 10, BHK = "2BHK", IsActive = true, MarkedForDeletion = false } // Latest assessment
        };
        var propertyDetails = new List<PropertyDetailsEntity>
        {
            new() { Id = 1, PropertyId = 10, TypeOfUseId = 1, FloorId = 1, AssessmentYear = "2023", ConstructionYear = "2020", SubTypeOfUseId = 1, ConstructionTypeId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, PropertyId = 10, TypeOfUseId = 10, FloorId = 20, AssessmentYear = "2024", ConstructionYear = "2021", SubTypeOfUseId = 30, ConstructionTypeId = 40, IsActive = true, MarkedForDeletion = false } // Latest detail
        };
        var typeOfUses = new List<TypeOfUseEntity>
        {
            new() { Id = 10, Description = "Residential Use", IsActive = true }
        };
        var floors = new List<FloorEntity>
        {
            new() { Id = 20, Description = "2nd Floor", IsActive = true }
        };
        var propertyTypes = new List<PropertyTypeMasterEntity>
        {
            new() { Id = 2, PropertyDescription = "Flat", PartType = "Residential", IsActive = true }
        };
        var subTypeOfUses = new List<SubTypeOfUseEntity>
        {
            new() { Id = 30, Description = "Standard Flat", IsActive = true }
        };
        var constructionTypes = new List<ConstructionTypeEntity>
        {
            new() { Id = 40, Description = "RCC", IsActive = true }
        };
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new() { Id = 200, OldSocietyName = "Sunrise Society", OldWing = "Z", OldFlatOrShopNumber = "999", IsActive = true, MarkedForDeletion = false }
        };
        var draftMappings = new List<PropertyMapDetailEntity>
        {
            new() { Id = 1, PropertyIdOld = 200, PropertyIdNew = 10, Status = PropertyMapStatus.Draft, UpdatedBy = 1, IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(societies.BuildMock());
        _mockAssessmentRepository.Setup(r => r.GetQueryable()).Returns(assessments.BuildMock());
        _mockPropertyDetailsRepository.Setup(r => r.GetQueryable()).Returns(propertyDetails.BuildMock());
        _mockTypeOfUseRepository.Setup(r => r.GetQueryable()).Returns(typeOfUses.BuildMock());
        _mockFloorRepository.Setup(r => r.GetQueryable()).Returns(floors.BuildMock());
        _mockPropertyTypeRepository.Setup(r => r.GetQueryable()).Returns(propertyTypes.BuildMock());
        _mockSubTypeOfUseRepository.Setup(r => r.GetQueryable()).Returns(subTypeOfUses.BuildMock());
        _mockConstructionTypeRepository.Setup(r => r.GetQueryable()).Returns(constructionTypes.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(oldProperties.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(draftMappings.BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var item = result.FirstOrDefault(r => r.RowSource == "NEW");
        Assert.NotNull(item);

        Assert.Equal("NEW", item.RowSource);
        Assert.Equal(10, item.PropertyId);
        Assert.Equal("1", item.WardNo);
        Assert.Equal("P-100", item.PropertyNo);
        Assert.Equal("101", item.PartitionNo);
        Assert.Equal("A Wing", item.WingName);
        Assert.Equal("101", item.FlatShopNo);
        Assert.Equal("Galaxy", item.ShopName);
        Assert.Equal("9876543210", item.MobileNo);
        Assert.Equal("2BHK", item.BHK); // From latest assessment (Id=2)
        Assert.Equal(10, item.TypeOfUseId);
        Assert.Equal("Residential Use", item.TypeOfUse);
        Assert.Equal(20, item.FloorId);
        Assert.Equal("2nd Floor", item.Floor);
        Assert.Equal("2024", item.AssessmentYear);
        Assert.Equal("2021", item.ConstructionYear);
        Assert.Equal(2, item.PropertyTypeId);
        Assert.Equal("Flat", item.PropertyTypeDescription);
        Assert.Equal(30, item.SubTypeOfUseId);
        Assert.Equal("Standard Flat", item.SubTypeOfUse);
        Assert.Equal(40, item.ConstructionTypeId);
        Assert.Equal("RCC", item.ConstructionType);
        Assert.Equal("John Doe", item.OwnerName);
        Assert.Equal("Jane Doe", item.OccupierName);
        Assert.Equal("Residential", item.Type);
        Assert.False(item.IsMatchProperty);
        Assert.False(item.IsMerge);
        Assert.False(item.Identify);
    }

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_DeduplicatesNewPropertiesByWingKeyAndFlatKey()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, CreatedBy = 1 };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 10, WardId = 1, PropertyNo = "P-100", PartitionNo = "101", SocietyDetailId = 50, FlatOrShopNo = "101", OwnerName = "First Owner", IsActive = true, MarkedForDeletion = false },
            new() { Id = 11, WardId = 1, PropertyNo = "P-100", PartitionNo = "101-DUP", SocietyDetailId = 50, FlatOrShopNo = "101", OwnerName = "Duplicate Owner", IsActive = true, MarkedForDeletion = false }
        };
        var societies = new List<SocietyDetailsEntity>
        {
            new() { Id = 50, WingId = 1, WingName = "A", IsActive = true, MarkedForDeletion = false }
        };
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new() { Id = 200, OldSocietyName = "Sunrise Society", OldWing = "Z", OldFlatOrShopNumber = "999", IsActive = true, MarkedForDeletion = false }
        };
        var draftMappings = new List<PropertyMapDetailEntity>
        {
            new() { Id = 1, PropertyIdOld = 200, PropertyIdNew = 10, Status = PropertyMapStatus.Draft, UpdatedBy = 1, IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(societies.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(oldProperties.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(draftMappings.BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var newRows = result.Where(r => r.RowSource == "NEW").ToList();
        Assert.Single(newRows);
        Assert.Equal(10, newRows[0].PropertyId); // Keeps lowest Id
        Assert.Equal("First Owner", newRows[0].OwnerName);
    }

    #endregion

    #region MATCHED, OLD, NEW and RowSource Tests

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_WhenOldAndNewMatch_ReturnsMatchedRowSource()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, CreatedBy = 1 };
        var properties = new List<PropertyEntity>
        {
            new()
            {
                Id = 10,
                WardId = 1,
                PropertyNo = "P-100",
                PartitionNo = "101",
                SocietyDetailId = 50,
                FlatOrShopNo = "101",
                OwnerName = "New Owner",
                IsActive = true,
                MarkedForDeletion = false
            }
        };
        var societies = new List<SocietyDetailsEntity>
        {
            new() { Id = 50, WingId = 1, WingName = "A", IsActive = true, MarkedForDeletion = false }
        };
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 100,
                OldSocietyName = "Sunrise Society",
                OldWardNo = "1",
                OldPropertyNo = "OP-10",
                OldPartitionNo = "01",
                OldWing = "A",
                OldFlatOrShopNumber = "101",
                OldOwnerName = "Old Owner",
                OldOccupierName = "Old Occupier",
                OldRV = 5000,
                OldTotalTax = 1200,
                OldGeneralTax = 800,
                OldAddress = "123 Old St",
                IsActive = true,
                MarkedForDeletion = false
            }
        };
        var draftMappings = new List<PropertyMapDetailEntity>
        {
            new()
            {
                Id = 1,
                PropertyIdOld = 100,
                PropertyIdNew = 10,
                Status = PropertyMapStatus.Draft,
                UpdatedBy = 1,
                IsActive = true
            }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(societies.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(oldProperties.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(draftMappings.BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var item = result[0];
        Assert.Equal("MATCHED", item.RowSource);
        Assert.True(item.IsMatchProperty);
        Assert.False(item.IsMerge);
        Assert.Equal(10, item.PropertyId);
        Assert.Equal("New Owner", item.OwnerName);
        Assert.Equal(100, item.OldPropertyId);
        Assert.Equal("Sunrise Society", item.OldSocietyName);
        Assert.Equal("Old Owner", item.OldOwnerName);
        Assert.Equal("5000", item.OldRv);
        Assert.Equal(1200m, item.OldTotalTax);
        Assert.Equal(800m, item.OldPropertyTax);
        Assert.Equal("123 Old St", item.OldAddress);
        Assert.Equal("A", item.OldWingName);
        Assert.Equal("101", item.OldFlatShopNo);
    }

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_WhenOnlyOldExists_ReturnsOldRowSource()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, CreatedBy = 1 };
        var properties = new List<PropertyEntity>
        {
            new()
            {
                Id = 10,
                WardId = 1,
                PropertyNo = "P-100",
                PartitionNo = "101",
                SocietyDetailId = 50,
                FlatOrShopNo = "101",
                IsActive = true,
                MarkedForDeletion = false
            }
        };
        var societies = new List<SocietyDetailsEntity>
        {
            new() { Id = 50, WingId = 1, WingName = "A", IsActive = true, MarkedForDeletion = false }
        };
        var oldProperties = new List<PropertyMastOldEntity>
        {
            // Old property in Wing B Flat 202 (does not match new property in Wing A Flat 101)
            new()
            {
                Id = 200,
                OldSocietyName = "Sunrise Society",
                OldWardNo = "1",
                OldPropertyNo = "OP-20",
                OldWing = "B",
                OldFlatOrShopNumber = "202",
                OldOwnerName = "Old Only Owner",
                IsActive = true,
                MarkedForDeletion = false
            }
        };
        var draftMappings = new List<PropertyMapDetailEntity>
        {
            new()
            {
                Id = 1,
                PropertyIdOld = 200,
                PropertyIdNew = 10,
                Status = PropertyMapStatus.Draft,
                UpdatedBy = 1,
                IsActive = true
            }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(societies.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(oldProperties.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(draftMappings.BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var newRow = result.FirstOrDefault(r => r.RowSource == "NEW");
        var oldRow = result.FirstOrDefault(r => r.RowSource == "OLD");

        Assert.NotNull(newRow);
        Assert.Equal(10, newRow.PropertyId);
        Assert.False(newRow.IsMatchProperty);

        Assert.NotNull(oldRow);
        Assert.Equal(200, oldRow.OldPropertyId);
        Assert.Equal("Old Only Owner", oldRow.OldOwnerName);
        Assert.False(oldRow.IsMatchProperty);
    }

    #endregion

    #region Merge Logic Tests

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_WhenMergeMappingExists_ReturnsMergeRowSource()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, CreatedBy = 1 };
        var properties = new List<PropertyEntity>
        {
            new()
            {
                Id = 10,
                WardId = 1,
                PropertyNo = "P-100",
                PartitionNo = "101",
                SocietyDetailId = 50,
                FlatOrShopNo = "101",
                OwnerName = "Merged New Owner",
                IsActive = true,
                MarkedForDeletion = false
            }
        };
        var societies = new List<SocietyDetailsEntity>
        {
            new() { Id = 50, WingId = 1, WingName = "A", IsActive = true, MarkedForDeletion = false }
        };
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 300,
                OldSocietyName = "Sunrise Society",
                OldWardNo = "1",
                OldPropertyNo = "OP-30",
                OldWing = "A",
                OldFlatOrShopNumber = "101",
                OldOwnerName = "Merged Old Owner",
                IsActive = true,
                MarkedForDeletion = false
            }
        };
        var mapDetails = new List<PropertyMapDetailEntity>
        {
            // Draft mapping to fetch OldSocietyName
            new()
            {
                Id = 1,
                PropertyIdOld = 300,
                PropertyIdNew = 10,
                Status = PropertyMapStatus.Draft,
                UpdatedBy = 1,
                IsActive = true
            },
            // Active mapping indicating Merge
            new()
            {
                Id = 2,
                PropertyMapId = 5,
                PropertyIdOld = 300,
                PropertyIdNew = 10,
                Status = PropertyMapStatus.Active,
                IsActive = true,
                IsCurrent = true,
                UpdatedDate = DateTime.UtcNow
            }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(societies.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(oldProperties.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(mapDetails.BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var item = result[0];
        Assert.Equal("Merge", item.RowSource);
        Assert.True(item.IsMerge);
        Assert.True(item.IsMatchProperty);
        Assert.Equal(10, item.PropertyId);
        Assert.Equal(300, item.OldPropertyId);
        Assert.Equal("Merged New Owner", item.OwnerName);
        Assert.Equal("Merged Old Owner", item.OldOwnerName);
    }

    #endregion

    #region Identify & User Enrichment Tests

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_EnrichesIdentifyAndUserNameCorrectly()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, CreatedBy = 99 };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 10, WardId = 1, PropertyNo = "P-100", PartitionNo = "101", SocietyDetailId = 50, FlatOrShopNo = "101", IsActive = true, MarkedForDeletion = false }
        };
        var societies = new List<SocietyDetailsEntity>
        {
            new() { Id = 50, WingId = 1, WingName = "A", IsActive = true, MarkedForDeletion = false }
        };
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new() { Id = 400, OldSocietyName = "Sunrise Society", OldWing = "B", OldFlatOrShopNumber = "202", IsActive = true, MarkedForDeletion = false }
        };
        var identifyDate = new DateTime(2025, 5, 20, 10, 30, 0, DateTimeKind.Utc);
        var mapDetails = new List<PropertyMapDetailEntity>
        {
            // Draft mapping with UpdatedBy = 99
            new()
            {
                Id = 10,
                PropertyIdOld = 400,
                PropertyIdNew = 10,
                Status = PropertyMapStatus.Draft,
                UpdatedBy = 99,
                UpdatedDate = identifyDate,
                IsActive = true
            }
        };
        var users = new List<UserEntity>
        {
            new() { Id = 99, UserName = "taxofficer", FirstName = "Alice", LastName = "Smith", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(societies.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(oldProperties.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(mapDetails.BuildMock());
        _mockUserRepository.Setup(r => r.GetQueryable()).Returns(users.BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var oldRow = result.FirstOrDefault(r => r.RowSource == "OLD");
        Assert.NotNull(oldRow);
        Assert.True(oldRow.Identify);
        Assert.Equal("Alice Smith", oldRow.IdentifyName);
        Assert.Equal(identifyDate, oldRow.IdentifyDate);
    }

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_EnrichesIdentifyName_WhenUserHasNoLastName()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, CreatedBy = 99 };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 10, WardId = 1, PropertyNo = "P-100", PartitionNo = "101", SocietyDetailId = 50, FlatOrShopNo = "101", IsActive = true, MarkedForDeletion = false }
        };
        var societies = new List<SocietyDetailsEntity>
        {
            new() { Id = 50, WingId = 1, WingName = "A", IsActive = true, MarkedForDeletion = false }
        };
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new() { Id = 400, OldSocietyName = "Sunrise Society", OldWing = "B", OldFlatOrShopNumber = "202", IsActive = true, MarkedForDeletion = false }
        };
        var mapDetails = new List<PropertyMapDetailEntity>
        {
            new()
            {
                Id = 10,
                PropertyIdOld = 400,
                PropertyIdNew = 10,
                Status = PropertyMapStatus.Draft,
                UpdatedBy = 99,
                IsActive = true
            }
        };
        var users = new List<UserEntity>
        {
            new() { Id = 99, UserName = "taxofficer", FirstName = "Bob", LastName = null, IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(societies.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(oldProperties.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(mapDetails.BuildMock());
        _mockUserRepository.Setup(r => r.GetQueryable()).Returns(users.BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var oldRow = result.FirstOrDefault(r => r.RowSource == "OLD");
        Assert.NotNull(oldRow);
        Assert.Equal("Bob", oldRow.IdentifyName);
    }

    #endregion

    #region Wing Filter on Old Properties Tests

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_WithWingIdSpecified_FiltersOldPropertiesByWingKey()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, SocietyId = 1, CreatedBy = 1 };
        var properties = new List<PropertyEntity>
        {
            new() { Id = 10, WardId = 1, PropertyNo = "P-100", PartitionNo = "101", SocietyDetailId = 50, FlatOrShopNo = "101", IsActive = true, MarkedForDeletion = false }
        };
        var societies = new List<SocietyDetailsEntity>
        {
            new() { Id = 50, WingId = 1, WingName = "WING A", IsActive = true, MarkedForDeletion = false }
        };
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new() { Id = 101, OldSocietyName = "Sunrise Society", OldWing = "WING A", OldFlatOrShopNumber = "101", IsActive = true, MarkedForDeletion = false },
            new() { Id = 102, OldSocietyName = "Sunrise Society", OldWing = "WING B", OldFlatOrShopNumber = "101", IsActive = true, MarkedForDeletion = false }
        };
        var draftMappings = new List<PropertyMapDetailEntity>
        {
            new() { Id = 1, PropertyIdOld = 101, PropertyIdNew = 10, Status = PropertyMapStatus.Draft, UpdatedBy = 1, IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(societies.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(oldProperties.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(draftMappings.BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Only WING A should match; WING B is filtered out
        Assert.Single(result);
        Assert.Equal("MATCHED", result[0].RowSource);
        Assert.Equal(101, result[0].OldPropertyId);
    }

    #endregion

    #region Multi-tier Ordering Tests

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_MixedRecords_SortsBySourceWingFlatOldIdNewId()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, CreatedBy = 1 };
        var properties = new List<PropertyEntity>
        {
            // Will match with old prop 100 -> MATCHED (SortSource 1)
            new() { Id = 10, WardId = 1, PropertyNo = "P-100", PartitionNo = "101", SocietyDetailId = 50, FlatOrShopNo = "101", IsActive = true, MarkedForDeletion = false },
            // Merged with old prop 200 -> Merge (SortSource 2)
            new() { Id = 11, WardId = 1, PropertyNo = "P-100", PartitionNo = "102", SocietyDetailId = 50, FlatOrShopNo = "102", IsActive = true, MarkedForDeletion = false },
            // Only New -> NEW (SortSource 3)
            new() { Id = 12, WardId = 1, PropertyNo = "P-100", PartitionNo = "103", SocietyDetailId = 50, FlatOrShopNo = "103", IsActive = true, MarkedForDeletion = false }
        };
        var societies = new List<SocietyDetailsEntity>
        {
            new() { Id = 50, WingId = 1, WingName = "A", IsActive = true, MarkedForDeletion = false }
        };
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new() { Id = 100, OldSocietyName = "Sunrise Society", OldWing = "A", OldFlatOrShopNumber = "101", IsActive = true, MarkedForDeletion = false },
            new() { Id = 200, OldSocietyName = "Sunrise Society", OldWing = "A", OldFlatOrShopNumber = "102", IsActive = true, MarkedForDeletion = false },
            // Only Old -> OLD (SortSource 4)
            new() { Id = 300, OldSocietyName = "Sunrise Society", OldWing = "B", OldFlatOrShopNumber = "201", IsActive = true, MarkedForDeletion = false }
        };
        var mapDetails = new List<PropertyMapDetailEntity>
        {
            new() { Id = 1, PropertyIdOld = 100, PropertyIdNew = 10, Status = PropertyMapStatus.Draft, UpdatedBy = 1, IsActive = true },
            new() { Id = 2, PropertyIdOld = 200, PropertyIdNew = 10, Status = PropertyMapStatus.Draft, UpdatedBy = 1, IsActive = true },
            new() { Id = 3, PropertyIdOld = 300, PropertyIdNew = 10, Status = PropertyMapStatus.Draft, UpdatedBy = 1, IsActive = true },
            // Active merge for 200 -> 11
            new() { Id = 4, PropertyIdOld = 200, PropertyIdNew = 11, Status = PropertyMapStatus.Active, IsActive = true, IsCurrent = true, UpdatedDate = DateTime.UtcNow }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(societies.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(oldProperties.BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(mapDetails.BuildMock());

        // Act
        var result = await _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);

        // Sort order must be: MATCHED (1) -> Merge (2) -> NEW (3) -> OLD (4)
        Assert.Equal("MATCHED", result[0].RowSource);
        Assert.Equal("Merge", result[1].RowSource);
        Assert.Equal("NEW", result[2].RowSource);
        Assert.Equal("OLD", result[3].RowSource);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task GetPropertyMatchingDetailsAsync_WhenRepositoryThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var request = new PropertyMapDetailsQueryParameters { PropertyId = 10, CreatedBy = 1 };
        _mockRepository.Setup(r => r.GetQueryable()).Throws(new InvalidOperationException("DB error"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GetPropertyMatchingDetailsAsync(request, CancellationToken.None));

        Assert.Equal("DB error", ex.Message);
    }

    #endregion
}
