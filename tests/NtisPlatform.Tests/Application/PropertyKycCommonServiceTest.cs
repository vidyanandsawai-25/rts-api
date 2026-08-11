using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.PropertyKyc;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Services.Property;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;
using NtisPlatform.Application.Interfaces.Property;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class PropertyKycCommonServiceTest
{
    private readonly Mock<IRepository<PropertyEntity, int>>
        _propertyRepositoryMock;

    private readonly Mock<IUnitOfWork>
        _unitOfWorkMock;

    private readonly Mock<IMapper>
        _mapperMock;

    private readonly Mock<IPropertyRepository>
        _customPropertyRepositoryMock;

    private readonly Mock<ILogger<PropertyKycService>>
        _loggerMock;

    private readonly Mock<IOptions<FeatureFlagsOptions>>
        _featureFlagsMock;

    private readonly Mock<IRepository<WardEntity, int>>
        _wardRepositoryMock;

    private readonly Mock<IRepository<PropertyCategoryEntity, int>>
        _categoryRepositoryMock;

    private readonly Mock<IRepository<SocietyDetailsEntity, int>>
        _societyRepositoryMock;

    private readonly Mock<IRepository<PropertyDetailsEntity, int>>
        _propertyDetailsRepositoryMock;

    private readonly Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>>
        _roomWiseRepositoryMock;

    private readonly Mock<IRepository<PropertyAssessmentEntity, int>>
        _assessmentRepositoryMock;

    private readonly Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>
        _wardAllocationRepositoryMock;

    private readonly Mock<IRepository<PropertyMapMasterEntity, int>>
        _propertyMapMasterRepositoryMock;

    private readonly Mock<IRepository<PropertyMapDetailEntity, int>>
        _propertyMapDetailRepositoryMock;

    private readonly Mock<IRepository<UserEntity, int>>
        _userRepositoryMock;

    private readonly Mock<IRepository<PropertyMastOldEntity, int>>
        _oldPropertyRepositoryMock;

    private readonly Mock<IRepository<PropertyTypeMasterEntity, int>>
        _propertyTypeRepositoryMock;

    private readonly Mock<IRepository<CommunicationDetailsEntity, int>>
        _communicationRepositoryMock;

    private readonly Mock<IRepository<PropertyPhotoEntity, int>>
        _propertyPhotoRepositoryMock;

    private readonly Mock<IRepository<DocumentBindingEntity, int>>
        _documentBindingRepositoryMock;

    private readonly Mock<IRepository<DocumentEntity, int>>
        _documentRepositoryMock;

    private readonly Mock<IRepository<PropertyPhotoTypeEntity, int>>
        _propertyPhotoTypeRepositoryMock;

    private readonly Mock<IRepository<OwnerTypeMasterEntity, int>>
        _ownerTypeRepositoryMock;

    private readonly Mock<IRepository<WingEntity, int>>
        _wingRepositoryMock;


    private readonly Mock<IRepository<WingEntity, int>>
    _wingMasterRepositoryMock;

    private readonly Mock<IPropertyRuleApplicationLogService>
        _ruleLogServiceMock;

    private readonly PropertyKycService _service;

    public PropertyKycCommonServiceTest()
    {
        _propertyRepositoryMock =
            new Mock<IRepository<PropertyEntity, int>>();

        _unitOfWorkMock =
            new Mock<IUnitOfWork>();

        _mapperMock =
            new Mock<IMapper>();

        _customPropertyRepositoryMock =
            new Mock<IPropertyRepository>();

        _loggerMock =
            new Mock<ILogger<PropertyKycService>>();

        _featureFlagsMock =
            new Mock<IOptions<FeatureFlagsOptions>>();

        _wardRepositoryMock =
            new Mock<IRepository<WardEntity, int>>();

        _categoryRepositoryMock =
            new Mock<IRepository<PropertyCategoryEntity, int>>();

        _societyRepositoryMock =
            new Mock<IRepository<SocietyDetailsEntity, int>>();

        _propertyDetailsRepositoryMock =
            new Mock<IRepository<PropertyDetailsEntity, int>>();

        _roomWiseRepositoryMock =
            new Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>>();

        _assessmentRepositoryMock =
            new Mock<IRepository<PropertyAssessmentEntity, int>>();

        _wardAllocationRepositoryMock =
            new Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>();

        _propertyMapMasterRepositoryMock =
            new Mock<IRepository<PropertyMapMasterEntity, int>>();

        _propertyMapDetailRepositoryMock =
            new Mock<IRepository<PropertyMapDetailEntity, int>>();

        _userRepositoryMock =
            new Mock<IRepository<UserEntity, int>>();

        _oldPropertyRepositoryMock =
            new Mock<IRepository<PropertyMastOldEntity, int>>();

        _propertyTypeRepositoryMock =
            new Mock<IRepository<PropertyTypeMasterEntity, int>>();

        _communicationRepositoryMock =
            new Mock<IRepository<CommunicationDetailsEntity, int>>();

        _propertyPhotoRepositoryMock =
            new Mock<IRepository<PropertyPhotoEntity, int>>();

        _documentBindingRepositoryMock =
            new Mock<IRepository<DocumentBindingEntity, int>>();

        _documentRepositoryMock =
            new Mock<IRepository<DocumentEntity, int>>();

        _propertyPhotoTypeRepositoryMock =
            new Mock<IRepository<PropertyPhotoTypeEntity, int>>();

        _ownerTypeRepositoryMock =
            new Mock<IRepository<OwnerTypeMasterEntity, int>>();

        _wingRepositoryMock =
            new Mock<IRepository<WingEntity, int>>();


        _wingMasterRepositoryMock =
            new Mock<IRepository<WingEntity, int>>();

        _ruleLogServiceMock =
            new Mock<IPropertyRuleApplicationLogService>();

        _featureFlagsMock
            .Setup(x => x.Value)
            .Returns(new FeatureFlagsOptions
            {
                AllowPropertyDeletionWithoutPaymentValidation = true
            });

        SetupEmptyRepositories();

        _service = new PropertyKycService(
            new Mock<IPropertyKycRepository>().Object,
            _unitOfWorkMock.Object,
            new Mock<IPropertyMutationInvariantPolicy>().Object,
            _propertyRepositoryMock.Object,
            _assessmentRepositoryMock.Object,
            _ownerTypeRepositoryMock.Object,
            _societyRepositoryMock.Object,
            _wingRepositoryMock.Object,
            _roomWiseRepositoryMock.Object,
            _communicationRepositoryMock.Object,
            _propertyMapDetailRepositoryMock.Object,
            _oldPropertyRepositoryMock.Object,
            _loggerMock.Object);
    }

    private void SetupEmptyRepositories()
    {
        _propertyRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<PropertyEntity>().BuildMock());

        _assessmentRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<PropertyAssessmentEntity>().BuildMock());

        _ownerTypeRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<OwnerTypeMasterEntity>().BuildMock());

        _societyRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<SocietyDetailsEntity>().BuildMock());

        _wingRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<WingEntity>().BuildMock());

        _roomWiseRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<RoomWiseSubmissionDetailsEntity>().BuildMock());

        _communicationRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<CommunicationDetailsEntity>().BuildMock());

        _propertyMapDetailRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<PropertyMapDetailEntity>().BuildMock());

        _oldPropertyRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<PropertyMastOldEntity>().BuildMock());

        _propertyPhotoRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<PropertyPhotoEntity>().BuildMock());

        _documentBindingRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<DocumentBindingEntity>().BuildMock());

        _documentRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<DocumentEntity>().BuildMock());

        _propertyPhotoTypeRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<PropertyPhotoTypeEntity>().BuildMock());
    }

    [Fact]
    public async Task GetKycDetailsCommon_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Act and Assert
        var exception =
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _service.GetKycDetailsCommon(
                    null!,
                    CancellationToken.None));

        Assert.Equal("queryParameters", exception.ParamName);
    }

    [Fact]
    public async Task GetKycDetailsCommon_WhenPropertyDoesNotExist_ReturnsNull()
    {
        // Arrange
        var request = new PropertyKycDetailsQueryParameters
        {
            WardId = 89,
            PropertyNo = "99999",
            PartitionNo = null
        };

        _propertyRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<PropertyEntity>().BuildMock());

        // Act
        var result = await _service.GetKycDetailsCommon(
            request,
            CancellationToken.None);

        // Assert
        Assert.Null(result);

        _assessmentRepositoryMock.Verify(
            x => x.GetQueryable(),
            Times.Never);
    }

    [Fact]
    public async Task GetKycDetailsCommon_WhenPropertyExists_ReturnsBasicDetails()
    {
        // Arrange
        var property = new PropertyEntity
        {
            Id = 753362,
            WardId = 89,
            PropertyNo = "10",
            PartitionNo = null,
            PropertyTypeId = 1,
            CategoryId = 2,
            PlotNo = "P-101",
            CSN = "CSN-001",
            OwnerTitle = "Mr.",
            OwnerName = "Test Owner",
            OwnerTitleEnglish = "Mr.",
            OwnerNameEnglish = "Test Owner",
            Address = "Test Address",
            AddressEnglish = "Test Address",
            Location = "Test Location",
            LocationEnglish = "Test Location",
            MobileNo = "9876543210",
            EmailId = "owner@example.com",
            PinCode = "411001",
            IsActive = true,
            MarkedForDeletion = false
        };

        _propertyRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<PropertyEntity>
            {
                property
            }.BuildMock());

        var request = new PropertyKycDetailsQueryParameters
        {
            WardId = 89,

            // The service trims PropertyNo.
            PropertyNo = " 10 ",

            PartitionNo = null
        };

        // Act
        var result = await _service.GetKycDetailsCommon(
            request,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(753362, result.PropertyId);
        Assert.Equal(1, result.PropertyTypeId);
        Assert.Equal(2, result.CategoryId);
        Assert.Equal("P-101", result.PlotNo);
        Assert.Equal("CSN-001", result.CSN);
        Assert.Equal("Test Owner", result.OwnerName);
        Assert.Equal("9876543210", result.MobileNo);
        Assert.Equal("owner@example.com", result.EmailId);
        Assert.Equal("411001", result.PinCode);
    }

    [Fact]
    public async Task GetKycDetailsCommon_WithPartitionNo_ReturnsExactPartition()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new()
            {
                Id = 100,
                WardId = 89,
                PropertyNo = "10",
                PartitionNo = "A",
                OwnerName = "Partition A Owner",
                IsActive = true,
                MarkedForDeletion = false
            },
            new()
            {
                Id = 101,
                WardId = 89,
                PropertyNo = "10",
                PartitionNo = "B",
                OwnerName = "Partition B Owner",
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        _propertyRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(properties.BuildMock());

        var request = new PropertyKycDetailsQueryParameters
        {
            WardId = 89,
            PropertyNo = "10",
            PartitionNo = " B "
        };

        // Act
        var result = await _service.GetKycDetailsCommon(
            request,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(101, result.PropertyId);
        Assert.Equal("Partition B Owner", result.OwnerName);
    }

    [Fact]
    public async Task GetKycDetailsCommon_WhenPropertyIsInactiveOrDeleted_ReturnsNull()
    {
        // Arrange
        var properties = new List<PropertyEntity>
        {
            new()
            {
                Id = 100,
                WardId = 89,
                PropertyNo = "10",
                IsActive = false,
                MarkedForDeletion = false
            },
            new()
            {
                Id = 101,
                WardId = 89,
                PropertyNo = "10",
                IsActive = true,
                MarkedForDeletion = true
            }
        };

        _propertyRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(properties.BuildMock());

        var request = new PropertyKycDetailsQueryParameters
        {
            WardId = 89,
            PropertyNo = "10"
        };

        // Act
        var result = await _service.GetKycDetailsCommon(
            request,
            CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetKycDetailsCommon_WhenRelatedRecordsExist_ReturnsMappedDetails()
    {
        // Arrange
        const int propertyId = 753362;

        var property = new PropertyEntity
        {
            Id = propertyId,
            WardId = 89,
            PropertyNo = "10",
            PartitionNo = null,
            OwnerName = "Main Owner",
            SocietyDetailId = 50,
            IsActive = true,
            MarkedForDeletion = false
        };

        var assessment = new PropertyAssessmentEntity
        {
            Id = 1,
            PropertyId = propertyId,
            OwnerTypeId = 3,
            AdharCardNo = "123456789012",
            BlockNo = "BLOCK-A",
            SurveyRemark = "Survey completed",
            IsActive = true,
            MarkedForDeletion = false
        };

        var ownerType = new OwnerTypeMasterEntity
        {
            Id = 3,
            OwnerType = "Owner",
            IsActive = true
        };

        var society = new SocietyDetailsEntity
        {
            Id = 50,
            SocietyName = "Green Society",
            SocietyAddress = "Pune",
            WingId = 7,
            WingName = "Wing A",
            ManagerName = "Manager One",
            SecretaryName = "Secretary One",
            BuilderName = "Builder One",
            IsActive = true,
            MarkedForDeletion = false
        };

        var wing = new WingEntity
        {
            Id = 7,
            WingNo = "A",
            IsActive = true
        };

        var roomWiseDetails = new RoomWiseSubmissionDetailsEntity
        {
            Id = 10,
            PropertyId = propertyId,
            LengthMtr = 10,
            WidthMtr = 20,
            TotalAreaSqMtr = 200,
            IsActive = true,
            MarkedForDeletion = false
        };

        _propertyRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<PropertyEntity>
            {
                property
            }.BuildMock());

        _assessmentRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<PropertyAssessmentEntity>
            {
                assessment
            }.BuildMock());

        _ownerTypeRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<OwnerTypeMasterEntity>
            {
                ownerType
            }.BuildMock());

        _societyRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<SocietyDetailsEntity>
            {
                society
            }.BuildMock());

        _wingRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<WingEntity>
            {
                wing
            }.BuildMock());

        _roomWiseRepositoryMock
            .Setup(x => x.GetQueryable())
            .Returns(new List<RoomWiseSubmissionDetailsEntity>
            {
                roomWiseDetails
            }.BuildMock());

        var request = new PropertyKycDetailsQueryParameters
        {
            WardId = 89,
            PropertyNo = "10"
        };

        // Act
        var result = await _service.GetKycDetailsCommon(
            request,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(propertyId, result.PropertyId);

        Assert.Equal(3, result.OwnerTypeId);
        Assert.Equal("Owner", result.OwnerType);
        Assert.Equal("123456789012", result.AdharCardNo);
        Assert.Equal("BLOCK-A", result.BlockNo);
        Assert.Equal("Survey completed", result.SurveyRemark);

        Assert.Equal(50, result.SocietyDetailId);
        Assert.Equal("Green Society", result.SocietyName);
        Assert.Equal("Pune", result.SocietyAddress);

        Assert.Equal(7, result.WingId);
        Assert.Equal("A", result.WingNo);
        Assert.Equal("Wing A", result.WingName);

        Assert.Equal("Manager One", result.ManagerName);
        Assert.Equal("Secretary One", result.SecretaryName);
        Assert.Equal("Builder One", result.BuilderName);

        Assert.Equal(10d, result.PlotLength);
        Assert.Equal(20d, result.PlotWidth);
        Assert.Equal(200d, result.TotalArea);
    }
}