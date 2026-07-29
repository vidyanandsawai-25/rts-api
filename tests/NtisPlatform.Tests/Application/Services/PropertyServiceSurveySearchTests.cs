using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.PropertySurveySearch;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class PropertyServiceSurveySearchTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IPropertyRepository> _mockPropertyRepository;
    private readonly Mock<ILogger<PropertyService>> _mockLogger;
    private readonly Mock<IOptions<FeatureFlagsOptions>> _mockFeatureFlags;

    private readonly Mock<IRepository<WardEntity, int>>
        _mockWardRepository;

    private readonly Mock<IRepository<PropertyCategoryEntity, int>>
        _mockCategoryRepository;

    private readonly Mock<IRepository<SocietyDetailsEntity, int>>
        _mockSocietyRepository;

    private readonly Mock<IRepository<PropertyDetailsEntity, int>>
        _mockPropertyDetailsRepository;

    private readonly Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>>
        _mockRoomWiseRepository;

    private readonly Mock<IRepository<PropertyAssessmentEntity, int>>
        _mockAssessmentRepository;

    private readonly Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>
        _mockWardAllocationRepository;

    private readonly Mock<IRepository<PropertyMapMasterEntity, int>>
        _mockPropertyMapMasterRepository;

    private readonly Mock<IRepository<PropertyMapDetailEntity, int>>
        _mockPropertyMapDetailRepository;

    private readonly Mock<IRepository<WingEntity, int>>
    _mockWingRepository;

    private readonly Mock<IRepository<UserEntity, int>>
        _mockUserRepository;

    private readonly Mock<IRepository<PropertyMastOldEntity, int>>
        _mockPropertyOldRepository;

    private readonly Mock<IRepository<PropertyTypeMasterEntity, int>>
        _mockPropertyTypeRepository;

    private readonly Mock<IPropertyRuleApplicationLogService>
        _mockRuleLogService;

    private readonly PropertyService _service;

    public PropertyServiceSurveySearchTests()
    {
        _mockRepository =
            new Mock<IRepository<PropertyEntity, int>>();

        _mockUnitOfWork =
            new Mock<IUnitOfWork>();

        _mockMapper =
            new Mock<IMapper>();

        _mockPropertyRepository =
            new Mock<IPropertyRepository>();

        _mockLogger =
            new Mock<ILogger<PropertyService>>();

        _mockFeatureFlags =
            new Mock<IOptions<FeatureFlagsOptions>>();

        _mockWardRepository =
            new Mock<IRepository<WardEntity, int>>();

        _mockCategoryRepository =
            new Mock<IRepository<PropertyCategoryEntity, int>>();

        _mockSocietyRepository =
            new Mock<IRepository<SocietyDetailsEntity, int>>();

        _mockPropertyDetailsRepository =
            new Mock<IRepository<PropertyDetailsEntity, int>>();

        _mockRoomWiseRepository =
            new Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>>();

        _mockAssessmentRepository =
            new Mock<IRepository<PropertyAssessmentEntity, int>>();

        _mockWardAllocationRepository =
            new Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>();

        _mockPropertyMapMasterRepository =
            new Mock<IRepository<PropertyMapMasterEntity, int>>();

        _mockPropertyMapDetailRepository =
            new Mock<IRepository<PropertyMapDetailEntity, int>>();

        _mockWingRepository =
           new Mock<IRepository<WingEntity, int>>();

        _mockUserRepository =
            new Mock<IRepository<UserEntity, int>>();

        _mockPropertyOldRepository =
            new Mock<IRepository<PropertyMastOldEntity, int>>();

        _mockPropertyTypeRepository =
            new Mock<IRepository<PropertyTypeMasterEntity, int>>();

        _mockRuleLogService =
            new Mock<IPropertyRuleApplicationLogService>();

        _mockFeatureFlags
            .Setup(x => x.Value)
            .Returns(new FeatureFlagsOptions());

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
            _mockPropertyMapMasterRepository.Object,
            _mockPropertyMapDetailRepository.Object,
            _mockWingRepository.Object,
            _mockUserRepository.Object,
            _mockPropertyOldRepository.Object,
            _mockPropertyTypeRepository.Object,
            _mockRuleLogService.Object);

        SetupEmptyRepositories();
    }

    private static IQueryable<T> BuildMockDbQuery<T>(
        IEnumerable<T> data)
        where T : class
    {
        return data.ToList().BuildMock<T>();
    }

    private void SetupSurveySearchRepositories(
        IEnumerable<PropertyEntity>? properties = null,
        IEnumerable<PropertyMastOldEntity>? oldProperties = null,
        IEnumerable<WardEntity>? wards = null,
        IEnumerable<PropertyCategoryEntity>? categories = null,
        IEnumerable<SocietyDetailsEntity>? societies = null,
        IEnumerable<PropertyTypeMasterEntity>? propertyTypes = null,
        IEnumerable<PropertyMapMasterEntity>? propertyMapMasters = null,
        IEnumerable<PropertyMapDetailEntity>? propertyMapDetails = null,
        IEnumerable<UserEntity>? users = null,
        IEnumerable<GlobalSurveyWardAllocationEntity>? wardAllocations = null)
    {
        _mockPropertyRepository
            .Setup(x => x.GetQueryable())
            .Returns(BuildMockDbQuery(properties ?? []));

        _mockRepository
            .Setup(x => x.GetQueryable())
            .Returns(BuildMockDbQuery(properties ?? []));

        _mockPropertyOldRepository
            .Setup(x => x.GetQueryable())
            .Returns(BuildMockDbQuery(oldProperties ?? []));

        _mockWardRepository
            .Setup(x => x.GetQueryable())
            .Returns(BuildMockDbQuery(wards ?? []));

        _mockCategoryRepository
            .Setup(x => x.GetQueryable())
            .Returns(BuildMockDbQuery(categories ?? []));

        _mockSocietyRepository
            .Setup(x => x.GetQueryable())
            .Returns(BuildMockDbQuery(societies ?? []));

        _mockPropertyTypeRepository
            .Setup(x => x.GetQueryable())
            .Returns(BuildMockDbQuery(propertyTypes ?? []));

        _mockPropertyMapMasterRepository
            .Setup(x => x.GetQueryable())
            .Returns(BuildMockDbQuery(propertyMapMasters ?? []));

        _mockPropertyMapDetailRepository
            .Setup(x => x.GetQueryable())
            .Returns(BuildMockDbQuery(propertyMapDetails ?? []));

        _mockUserRepository
            .Setup(x => x.GetQueryable())
            .Returns(BuildMockDbQuery(users ?? []));

        _mockWardAllocationRepository
            .Setup(x => x.GetQueryable())
            .Returns(BuildMockDbQuery(wardAllocations ?? []));
    }

    private void SetupEmptyRepositories()
    {
        SetupSurveySearchRepositories();
    }

    [Fact]
    public async Task SearchSurveyPropertiesAsync_ReturnsEmpty_WhenNoNewPropertiesFound()
    {
        var request = new PropertySurveySearchQueryParameters
        {
            WardNo = "W1",
            Status = "NEW",
            PageNumber = 1,
            PageSize = 10
        };

        SetupEmptyRepositories();

        var result = await _service.SearchSurveyPropertiesAsync(
            request,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Record not found.", result.Message);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items!.Data);
        Assert.Equal(0, result.Items.Count);
        Assert.False(result.Items.HasNext);
    }

    [Fact]
    public async Task SearchSurveyPropertiesAsync_ReturnsEmpty_WhenNoOldPropertiesFound()
    {
        var request = new PropertySurveySearchQueryParameters
        {
            WardNo = "W1",
            Status = "OLD",
            PageNumber = 1,
            PageSize = 10
        };

        SetupEmptyRepositories();

        var result = await _service.SearchSurveyPropertiesAsync(
            request,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Record not found.", result.Message);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items!.Data);
        Assert.Equal(0, result.Items.Count);
        Assert.False(result.Items.HasNext);
    }

    [Fact]
    public async Task SearchSurveyPropertiesAsync_ReturnsEmpty_WhenApartmentSearchHasNoRecords()
    {
        var request = new PropertySurveySearchQueryParameters
        {
            WardNo = "W1",
Status = "NEW",
            PropertyType = "APARTMENT",
            PageNumber = 1,
            PageSize = 10
        };

        SetupEmptyRepositories();

        var result = await _service.SearchSurveyPropertiesAsync(
            request,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Record not found.", result.Message);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items!.Data);
        Assert.Equal(0, result.Items.Count);
        Assert.False(result.Items.HasNext);
    }

    [Fact]
    public async Task SearchSurveyPropertiesAsync_ReturnsEmpty_WhenNonApartmentSearchHasNoRecords()
    {
        var request = new PropertySurveySearchQueryParameters
        {
            WardNo = "W1",
Status = "NEW",
            PropertyType = "INDIVIDUAL",
            PageNumber = 1,
            PageSize = 10
        };

        SetupEmptyRepositories();

        var result = await _service.SearchSurveyPropertiesAsync(
            request,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Record not found.", result.Message);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items!.Data);
        Assert.Equal(0, result.Items.Count);
        Assert.False(result.Items.HasNext);
    }

    [Fact]
    public async Task SearchSurveyPropertiesAsync_ReturnsEmpty_WhenSearchTextDoesNotMatch()
    {
        var request = new PropertySurveySearchQueryParameters
        {
            WardNo = "W1",
Status = "NEW",
            SearchText = "UNKNOWN-PROPERTY",
            PageNumber = 1,
            PageSize = 10
        };

        SetupEmptyRepositories();

        var result = await _service.SearchSurveyPropertiesAsync(
            request,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Record not found.", result.Message);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items!.Data);
        Assert.Equal(0, result.Items.Count);
        Assert.False(result.Items.HasNext);
    }

    [Fact]
    public async Task SearchSurveyPropertiesAsync_ThrowsValidationException_WhenPageSizeIsMinusOne()
    {
        var request = new PropertySurveySearchQueryParameters
        {
            WardNo = "W1",
            Status = "NEW",
            PageNumber = 1,
            PageSize = -1
        };

        var exception =
            await Assert.ThrowsAsync<PropertyValidationException>(
                () => _service.SearchSurveyPropertiesAsync(
                    request,
                    CancellationToken.None));

        Assert.Contains(
            "PageSize",
            exception.Message,
            System.StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(-10)]
    public async Task SearchSurveyPropertiesAsync_UsesDefaultPageSize_WhenPageSizeIsInvalid(
        int pageSize)
    {
        var request = new PropertySurveySearchQueryParameters
        {
            WardNo = "W1",
            Status = "NEW",
            PageNumber = 1,
            PageSize = pageSize
        };

        SetupEmptyRepositories();

        var result = await _service.SearchSurveyPropertiesAsync(
            request,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items!.Data);
        Assert.Equal(0, result.Items.Count);
        Assert.False(result.Items.HasNext);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task SearchSurveyPropertiesAsync_UsesDefaultPageNumber_WhenPageNumberIsInvalid(
        int pageNumber)
    {
        var request = new PropertySurveySearchQueryParameters
        {
            WardNo = "W1",
            Status = "NEW",
            PageNumber = pageNumber,
            PageSize = 10
        };

        SetupEmptyRepositories();

        var result = await _service.SearchSurveyPropertiesAsync(
            request,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items!.Data);
        Assert.Equal(0, result.Items.Count);
        Assert.False(result.Items.HasNext);
    }

    [Fact]
    public async Task SearchSurveyPropertiesAsync_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        var exception =
            await Assert.ThrowsAsync<System.ArgumentNullException>(
                () => _service.SearchSurveyPropertiesAsync(
                    null!,
                    CancellationToken.None));

        Assert.Equal("request", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("INVALID")]
    public async Task SearchSurveyPropertiesAsync_ReturnsEmpty_WhenStatusIsNotRecognized(
        string status)
    {
        var request = new PropertySurveySearchQueryParameters
        {
            WardNo = "W1",
            Status = status,
            PageNumber = 1,
            PageSize = 10
        };

        SetupEmptyRepositories();

        var result = await _service.SearchSurveyPropertiesAsync(
            request,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items!.Data);
        Assert.Equal(0, result.Items.Count);
        Assert.False(result.Items.HasNext);
    }

    [Fact]
    public async Task SearchSurveyPropertiesAsync_UsesNewPropertyRepository_WhenStatusIsNew()
    {
        var request = new PropertySurveySearchQueryParameters
        {
            WardNo = "W1",
Status = "NEW",
            PageNumber = 1,
            PageSize = 10
        };

        SetupEmptyRepositories();

        await _service.SearchSurveyPropertiesAsync(
            request,
            CancellationToken.None);

        _mockPropertyRepository.Verify(
            x => x.GetQueryable(),
            Times.AtLeastOnce);

        _mockPropertyOldRepository.Verify(
            x => x.GetQueryable(),
            Times.Never);
    }

    [Fact]
    public async Task SearchSurveyPropertiesAsync_UsesOldPropertyRepository_WhenStatusIsOld()
    {
        var request = new PropertySurveySearchQueryParameters
        {
            WardNo = "W1",
Status = "OLD",
            PageNumber = 1,
            PageSize = 10
        };

        SetupEmptyRepositories();

        await _service.SearchSurveyPropertiesAsync(
            request,
            CancellationToken.None);

        _mockPropertyOldRepository.Verify(
            x => x.GetQueryable(),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SearchSurveyPropertiesAsync_ReturnsNoNextPage_WhenResultIsEmpty()
    {
        var request = new PropertySurveySearchQueryParameters
        {
            WardNo = "W1",
Status = "NEW",
            PageNumber = 1,
            PageSize = 10
        };

        SetupEmptyRepositories();

        var result = await _service.SearchSurveyPropertiesAsync(
            request,
            CancellationToken.None);

        Assert.NotNull(result.Items);
        Assert.False(result.Items!.HasNext);
        Assert.Equal(0, result.Items.Count);
        Assert.Empty(result.Items.Data);
    }

    [Fact]
    public async Task SearchSurveyPropertiesAsync_ReturnsEmpty_WhenPageNumberIsGreaterThanOne()
    {
        var request = new PropertySurveySearchQueryParameters
        {
            WardNo = "W1",
Status = "NEW",
            PageNumber = 2,
            PageSize = 5
        };

        SetupEmptyRepositories();

        var result = await _service.SearchSurveyPropertiesAsync(
            request,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Items);
        Assert.Equal(0, result.Items!.Count);
        Assert.Empty(result.Items.Data);
        Assert.False(result.Items.HasNext);
    }
}