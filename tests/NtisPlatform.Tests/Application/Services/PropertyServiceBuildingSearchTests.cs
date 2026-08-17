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
    private readonly Mock<IRepository<PropertyEntity, int>>
        _propertyRepository;

    private readonly Mock<IRepository<PropertyMastOldEntity, int>>
        _propertyOldRepository;

    private readonly Mock<IRepository<SocietyDetailsEntity, int>>
        _societyRepository;

    private readonly Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>>
        _roomWiseRepository;

    private readonly Mock<IRepository<PropertyMapDetailEntity, int>>
        _propertyMapDetailRepository;

    public PropertyServiceBuildingSearchTests()
    {
        _propertyRepository =
            new Mock<IRepository<PropertyEntity, int>>();

        _propertyOldRepository =
            new Mock<IRepository<PropertyMastOldEntity, int>>();

        _societyRepository =
            new Mock<IRepository<SocietyDetailsEntity, int>>();

        _roomWiseRepository =
            new Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>>();

        _propertyMapDetailRepository =
            new Mock<IRepository<PropertyMapDetailEntity, int>>();
    }

    [Fact]
    public async Task SearchBuildingInformationAsync_WithNullParameters_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act and Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.SearchBuildingInformationAsync(
                null!,
                CancellationToken.None));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchBuildingInformationAsync_WithoutOldWardNo_ThrowsInvalidOperationException(
        string? oldWardNo)
    {
        // Arrange
        var service = CreateService();

        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = oldWardNo
        };

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SearchBuildingInformationAsync(
                queryParameters,
                CancellationToken.None));

        // Assert
        Assert.Equal(
            "BuildingInformation_OldWardNo_Required",
            exception.Message);
    }

    [Fact]
    public async Task SearchBuildingInformationAsync_WithMatchingRecords_ReturnsPagedResult()
    {
        // Arrange
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 501,
                OldWardNo = "W1",
                OldSocietyName = "ABC Society",
                OldPropertyNo = "OLD-001",
                OldWing = "A",
                OldFlatOrShopNumber = "101",
                OldOwnerName = "Owner One",
                OldMobileNo = "9999999999",
                OldRV = 1000,
                OldTotalTax = 250,
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        var properties = new List<PropertyEntity>
        {
            new()
            {
                Id = 101,
                PropertyMastOldId = 501,
                SocietyDetailId = 201,
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        var societies = new List<SocietyDetailsEntity>
        {
            new()
            {
                Id = 201,
                BuilderName = "Builder One",
                BuilderNameEnglish = "Builder One",
                BuilderMobileNo = "8888888888",
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        var roomDetails = new List<RoomWiseSubmissionDetailsEntity>
        {
            new()
            {
                Id = 301,
                PropertyId = 101,
                AreaSqMtr = 50,
                TotalAreaSqMtr = 60,
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        var mapDetails = new List<PropertyMapDetailEntity>
        {
            new()
            {
                Id = 401,
                PropertyMapId = 10,
                PropertyIdOld = 501,
                Status = "ACTIVE",
                IsActive = true,
                CreatedDate = DateTime.Now
            }
        };

        SetupQueryableRepositories(
            oldProperties,
            properties,
            societies,
            roomDetails,
            mapDetails);

        var service = CreateService();

        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = " W1 ",
            OldSocietyName = "ABC",
            MapId = 10,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.SearchBuildingInformationAsync(
            queryParameters,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Single(result.Items);

        var item = result.Items.First();

        Assert.Equal(101, item.PropertyId);
        Assert.Equal(501, item.Id);
        Assert.Equal("OLD-001", item.OldPropertyNo);
        Assert.Equal("A", item.OldWing);
        Assert.Equal("101", item.OldFlatOrShopNumber);
        Assert.Equal("Owner One", item.OldOwnerName);
        Assert.Equal("Builder One", item.BuilderName);
        Assert.Equal(50m, item.AreaSqMtr);
        Assert.Equal(60m, item.TotalAreaSqMtr);
        Assert.True(item.Identify);
    }

    [Fact]
    public async Task SearchBuildingInformationAsync_WithoutLinkedProperty_ReturnsOldPropertyWithZeroPropertyId()
    {
        // Arrange
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 501,
                OldWardNo = "W1",
                OldPropertyNo = "OLD-001",
                OldOwnerName = "Owner One",
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        SetupQueryableRepositories(
            oldProperties,
            [],
            [],
            [],
            []);

        var service = CreateService();

        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = "W1",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.SearchBuildingInformationAsync(
            queryParameters,
            CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);

        var item = result.Items.First();

        Assert.Equal(0, item.PropertyId);
        Assert.Equal(501, item.Id);
        Assert.Equal("OLD-001", item.OldPropertyNo);
        Assert.Null(item.BuilderName);
        Assert.Null(item.AreaSqMtr);
        Assert.False(item.Identify);
    }

    [Fact]
    public async Task SearchBuildingInformationAsync_WithDifferentMapId_ReturnsNoRecords()
    {
        // Arrange
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 501,
                OldWardNo = "W1",
                OldPropertyNo = "OLD-001",
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        var mapDetails = new List<PropertyMapDetailEntity>
        {
            new()
            {
                Id = 401,
                PropertyMapId = 20,
                PropertyIdOld = 501,
                Status = "ACTIVE",
                IsActive = true,
                CreatedDate = DateTime.Now
            }
        };

        SetupQueryableRepositories(
            oldProperties,
            [],
            [],
            [],
            mapDetails);

        var service = CreateService();

        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = "W1",
            MapId = 10,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.SearchBuildingInformationAsync(
            queryParameters,
            CancellationToken.None);

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchBuildingInformationAsync_WithInactiveOldProperty_DoesNotReturnRecord()
    {
        // Arrange
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 501,
                OldWardNo = "W1",
                OldPropertyNo = "OLD-001",
                IsActive = false,
                MarkedForDeletion = false
            }
        };

        SetupQueryableRepositories(
            oldProperties,
            [],
            [],
            [],
            []);

        var service = CreateService();

        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = "W1",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.SearchBuildingInformationAsync(
            queryParameters,
            CancellationToken.None);

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchBuildingInformationAsync_WithDeletedOldProperty_DoesNotReturnRecord()
    {
        // Arrange
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 501,
                OldWardNo = "W1",
                OldPropertyNo = "OLD-001",
                IsActive = true,
                MarkedForDeletion = true
            }
        };

        SetupQueryableRepositories(
            oldProperties,
            [],
            [],
            [],
            []);

        var service = CreateService();

        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = "W1",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.SearchBuildingInformationAsync(
            queryParameters,
            CancellationToken.None);

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchBuildingInformationAsync_WithSocietyNameFilter_ReturnsMatchingRecords()
    {
        // Arrange
        var oldProperties = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 501,
                OldWardNo = "W1",
                OldSocietyName = "ABC Society",
                OldPropertyNo = "OLD-001",
                IsActive = true,
                MarkedForDeletion = false
            },
            new()
            {
                Id = 502,
                OldWardNo = "W1",
                OldSocietyName = "XYZ Society",
                OldPropertyNo = "OLD-002",
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        SetupQueryableRepositories(
            oldProperties,
            [],
            [],
            [],
            []);

        var service = CreateService();

        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = "W1",
            OldSocietyName = "ABC",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.SearchBuildingInformationAsync(
            queryParameters,
            CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("OLD-001", result.Items.First().OldPropertyNo);
    }

    [Fact]
    public async Task SearchBuildingInformationAsync_WithInvalidPaging_UsesDefaultPaging()
    {
        // Arrange
        SetupQueryableRepositories(
            [],
            [],
            [],
            [],
            []);

        var service = CreateService();

        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = "W1",
            PageNumber = 0,
            PageSize = 0
        };

        // Act
        var result = await service.SearchBuildingInformationAsync(
            queryParameters,
            CancellationToken.None);

        // Assert
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task SearchBuildingInformationAsync_AppliesPagination()
    {
        // Arrange
        var oldProperties = Enumerable.Range(1, 15)
            .Select(index => new PropertyMastOldEntity
            {
                Id = index,
                OldWardNo = "W1",
                OldPropertyNo = $"OLD-{index:000}",
                IsActive = true,
                MarkedForDeletion = false
            })
            .ToList();

        SetupQueryableRepositories(
            oldProperties,
            [],
            [],
            [],
            []);

        var service = CreateService();

        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = "W1",
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await service.SearchBuildingInformationAsync(
            queryParameters,
            CancellationToken.None);

        // Assert
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(5, result.Items.Count());
    }

    [Fact]
    public async Task SearchBuildingInformationAsync_PropagatesCancellationToken()
    {
        // Arrange
        SetupQueryableRepositories(
            [],
            [],
            [],
            [],
            []);

        var service = CreateService();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var queryParameters = new BuildingInformationQueryParameters
        {
            OldWardNo = "W1"
        };

        // Act
        var result = await service.SearchBuildingInformationAsync(
            queryParameters,
            cancellationTokenSource.Token);

        // Assert
        Assert.NotNull(result);
    }

    private void SetupQueryableRepositories(
        List<PropertyMastOldEntity> oldProperties,
        List<PropertyEntity> properties,
        List<SocietyDetailsEntity> societies,
        List<RoomWiseSubmissionDetailsEntity> roomDetails,
        List<PropertyMapDetailEntity> mapDetails)
    {
        var oldPropertiesQueryable = oldProperties
            .BuildMock();

        var propertiesQueryable = properties
            .BuildMock();

        var societiesQueryable = societies
            .BuildMock();

        var roomDetailsQueryable = roomDetails
            .BuildMock();

        var mapDetailsQueryable = mapDetails
            .BuildMock();

        _propertyOldRepository
            .Setup(repository => repository.GetQueryable())
            .Returns(oldPropertiesQueryable);

        _propertyRepository
            .Setup(repository => repository.GetQueryable())
            .Returns(propertiesQueryable);

        _societyRepository
            .Setup(repository => repository.GetQueryable())
            .Returns(societiesQueryable);

        _roomWiseRepository
            .Setup(repository => repository.GetQueryable())
            .Returns(roomDetailsQueryable);

        _propertyMapDetailRepository
            .Setup(repository => repository.GetQueryable())
            .Returns(mapDetailsQueryable);
    }

    private PropertyService CreateService()
    {
        var unitOfWork =
            new Mock<IUnitOfWork>();

        var mapper =
            new Mock<IMapper>();

        var customPropertyRepository =
            new Mock<IPropertyRepository>();

        var logger =
            new Mock<ILogger<PropertyService>>();

        var featureFlags = Options.Create(
            new FeatureFlagsOptions());

        var wardRepository =
            new Mock<IRepository<WardEntity, int>>();

        var categoryRepository =
            new Mock<IRepository<PropertyCategoryEntity, int>>();

        var propertyDetailsRepository =
            new Mock<IRepository<PropertyDetailsEntity, int>>();

        var assessmentRepository =
            new Mock<IRepository<PropertyAssessmentEntity, int>>();

        var wardAllocationRepository =
            new Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>();

        var propertyMapMasterRepository =
            new Mock<IRepository<PropertyMapMasterEntity, int>>();

        var userRepository =
            new Mock<IRepository<UserEntity, int>>();

        var propertyTypeRepository =
            new Mock<IRepository<PropertyTypeMasterEntity, int>>();

        var ruleLogService =
            new Mock<IPropertyRuleApplicationLogService>();

        return new PropertyService(
            _propertyRepository.Object,
            unitOfWork.Object,
            mapper.Object,
            customPropertyRepository.Object,
            logger.Object,
            featureFlags,
            wardRepository.Object,
            categoryRepository.Object,
            _societyRepository.Object,
            propertyDetailsRepository.Object,
            _roomWiseRepository.Object,
            assessmentRepository.Object,
            wardAllocationRepository.Object,
            new Mock<IRepository<OldWardMasterEntity, int>>().Object,
            propertyMapMasterRepository.Object,
            _propertyMapDetailRepository.Object,
            new Mock<IRepository<WingEntity, int>>().Object,
            userRepository.Object,
            _propertyOldRepository.Object,
            propertyTypeRepository.Object,
            new Mock<IRepository<CommunicationDetailsEntity, int>>().Object,
            new Mock<IRepository<PropertyPhotoEntity, int>>().Object,
            new Mock<IRepository<DocumentBindingEntity, int>>().Object,
            new Mock<IRepository<DocumentEntity, int>>().Object,
            new Mock<IRepository<PropertyPhotoTypeEntity, int>>().Object,
            new Mock<IRepository<OwnerTypeMasterEntity, int>>().Object,
            new Mock<IRepository<WingEntity, int>>().Object,
            ruleLogService.Object);
    }
}