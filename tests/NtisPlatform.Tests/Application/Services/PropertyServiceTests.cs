using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using Moq;
using Xunit;
using AutoMapper;
using NtisPlatform.Application.DTOs.Range;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Comprehensive tests for PropertyService to achieve 100% code coverage
/// </summary>
public class PropertyServiceTests
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
    private readonly PropertyService _service;

    public PropertyServiceTests()
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

        // Setup feature flag - allow deletion without payment validation in tests
        _mockFeatureFlags.Setup(f => f.Value).Returns(new FeatureFlagsOptions
        {
            AllowPropertyDeletionWithoutPaymentValidation = true
        });

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
            _mockAssessmentRepository.Object);
    }


    #region GetBasicDetailsAsync Tests

    [Fact]
    public async Task GetBasicDetailsAsync_ReturnsBasicDetails()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertyBasicDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.GetBasicDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetBasicDetailsAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
        _mockPropertyRepository.Verify(x => x.GetBasicDetailsAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateBasicDetailsAsync Tests

    [Fact]
    public async Task UpdateBasicDetailsAsync_UpdatesAndReturnsBasicDetails()
    {
        // Arrange
        var propertyId = 1;
        var dto = new UpdatePropertyBasicDetailsDto();
        var expectedDto = new PropertyBasicDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.UpdateBasicDetailsAsync(propertyId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.UpdateBasicDetailsAsync(propertyId, dto);

        // Assert
        Assert.Equal(expectedDto, result);
        _mockPropertyRepository.Verify(x => x.UpdateBasicDetailsAsync(propertyId, dto, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetSocietyDetailsAsync Tests

    [Fact]
    public async Task GetSocietyDetailsAsync_ReturnsSocietyDetails()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertySocietyDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.GetSocietyDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetSocietyDetailsAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region UpdateSocietyDetailsAsync Tests

    [Fact]
    public async Task UpdateSocietyDetailsAsync_UpdatesAndReturnsSocietyDetails()
    {
        // Arrange
        var propertyId = 1;
        var dto = new UpdatePropertySocietyDetailsDto();
        var expectedDto = new PropertySocietyDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.UpdateSocietyDetailsAsync(propertyId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.UpdateSocietyDetailsAsync(propertyId, dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetKycDetailsAsync Tests

    [Fact]
    public async Task GetKycDetailsAsync_ReturnsKycDetails()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertyKycDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.GetKycDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetKycDetailsAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region UpdateKycDetailsAsync Tests

    [Fact]
    public async Task UpdateKycDetailsAsync_UpdatesAndReturnsKycDetails()
    {
        // Arrange
        var propertyId = 1;
        var dto = new UpdatePropertyKycDetailsDto();
        var expectedDto = new PropertyKycDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.UpdateKycDetailsAsync(propertyId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.UpdateKycDetailsAsync(propertyId, dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region UpdateOldDetailsAsync Tests

    [Fact]
    public async Task UpdateOldDetailsAsync_UpdatesAndReturnsOldDetails()
    {
        // Arrange
        var propertyId = 1;
        var dto = new UpdatePropertyOldDetailsDto();
        var expectedDto = new PropertyOldDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.UpdateOldDetailsAsync(propertyId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.UpdateOldDetailsAsync(propertyId, dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetOldDetailsAsync Tests

    [Fact]
    public async Task GetOldDetailsAsync_ReturnsOldDetails()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertyOldDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.GetOldDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetOldDetailsAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetTaxDetailsAsync Tests

    [Fact]
    public async Task GetTaxDetailsAsync_ReturnsTaxDetails()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertyTaxDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.GetTaxDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetTaxDetailsAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetTaxDetailsCVAsync Tests

    [Fact]
    public async Task GetTaxDetailsCVAsync_ReturnsTaxDetailsCV()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertyTaxDetailsCVDto();
        _mockPropertyRepository
            .Setup(x => x.GetTaxDetailsCVAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetTaxDetailsCVAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetOldTaxesDetailsAsync Tests

    [Fact]
    public async Task GetOldTaxesDetailsAsync_ReturnsOldTaxesDetails()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertyOldTaxesDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.GetOldTaxesDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetOldTaxesDetailsAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region UpdateOldTaxesDetailsAsync Tests

    [Fact]
    public async Task UpdateOldTaxesDetailsAsync_UpdatesAndReturnsOldTaxesDetails()
    {
        // Arrange
        var propertyId = 1;
        var dto = new UpdatePropertyOldTaxesDetailsDto();
        var expectedDto = new PropertyOldTaxesDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.UpdateOldTaxesDetailsAsync(propertyId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.UpdateOldTaxesDetailsAsync(propertyId, dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetFloorDetailsOldAsync Tests

    [Fact]
    public async Task GetFloorDetailsOldAsync_ReturnsFloorDetailsOld()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertyDetailsOldListDto();
        _mockPropertyRepository
            .Setup(x => x.GetFloorDetailsOldAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetFloorDetailsOldAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetFloorDetailsOldPagedAsync Tests

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_ReturnsPaginatedFloorDetailsOld()
    {
        // Arrange
        var propertyId = 1;
        var queryParameters = new NtisPlatform.Application.DTOs.PropertyDetails.FloorDetailsOldQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            OldFloorId = 1
        };

        var repoResult = new FloorDetailsOldPagedResult
        {
            Items = new List<PropertyDetailsOldDto>
            {
                new PropertyDetailsOldDto { Id = 1, PropertyId = propertyId },
                new PropertyDetailsOldDto { Id = 2, PropertyId = propertyId }
            },
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _mockPropertyRepository
            .Setup(x => x.GetFloorDetailsOldPagedAsync(
                propertyId,
                It.Is<FloorDetailsOldQuery>(q =>
                    q.PageNumber == 1 &&
                    q.PageSize == 10 &&
                    q.OldFloorId == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(repoResult);

        // Act
        var result = await _service.GetFloorDetailsOldPagedAsync(propertyId, queryParameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_WithNullResult_ReturnsNull()
    {
        // Arrange
        var propertyId = 1;
        var queryParameters = new NtisPlatform.Application.DTOs.PropertyDetails.FloorDetailsOldQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        _mockPropertyRepository
            .Setup(x => x.GetFloorDetailsOldPagedAsync(
                propertyId,
                It.IsAny<FloorDetailsOldQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((FloorDetailsOldPagedResult?)null);

        // Act
        var result = await _service.GetFloorDetailsOldPagedAsync(propertyId, queryParameters);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldPagedAsync_MapsAllQueryParameters()
    {
        // Arrange
        var propertyId = 1;
        var queryParameters = new NtisPlatform.Application.DTOs.PropertyDetails.FloorDetailsOldQueryParameters
        {
            PageNumber = 2,
            PageSize = 20,
            SearchTerm = "test",
            SortBy = "OldFloorId",
            SortOrder = "desc",
            OldFloorId = 1,
            OldSubFloorId = 2,
            OldConstructionTypeId = 3,
            OldTypeOfUseId = 4,
            OldSubTypeOfUseId = 5,
            OldConstructionYear = "2020",
            OldAssessmentYear = "2021"
        };

        var repoResult = new FloorDetailsOldPagedResult
        {
            Items = new List<PropertyDetailsOldDto>(),
            TotalCount = 0,
            PageNumber = 2,
            PageSize = 20
        };

        _mockPropertyRepository
            .Setup(x => x.GetFloorDetailsOldPagedAsync(
                propertyId,
                It.Is<FloorDetailsOldQuery>(q =>
                    q.PageNumber == 2 &&
                    q.PageSize == 20 &&
                    q.SearchTerm == "test" &&
                    q.SortBy == "OldFloorId" &&
                    q.SortOrder == "desc" &&
                    q.OldFloorId == 1 &&
                    q.OldSubFloorId == 2 &&
                    q.OldConstructionTypeId == 3 &&
                    q.OldTypeOfUseId == 4 &&
                    q.OldSubTypeOfUseId == 5 &&
                    q.OldConstructionYear == "2020" &&
                    q.OldAssessmentYear == "2021"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(repoResult);

        // Act
        var result = await _service.GetFloorDetailsOldPagedAsync(propertyId, queryParameters);

        // Assert
        Assert.NotNull(result);
        _mockPropertyRepository.Verify(x => x.GetFloorDetailsOldPagedAsync(
            propertyId,
            It.IsAny<FloorDetailsOldQuery>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetFloorDetailsOldByIdAsync Tests

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_ReturnsFloorDetailsOldById()
    {
        // Arrange
        var propertyId = 1;
        var floorId = 2;
        var expectedDto = new PropertyDetailsOldDto();
        _mockPropertyRepository
            .Setup(x => x.GetFloorDetailsOldByIdAsync(propertyId, floorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetFloorDetailsOldByIdAsync(propertyId, floorId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region AddFloorDetailsOldAsync Tests

    [Fact]
    public async Task AddFloorDetailsOldAsync_AddsAndReturnsFloorDetailsOld()
    {
        // Arrange
        var propertyId = 1;
        var dto = new AddPropertyDetailsOldDto();
        var expectedDto = new PropertyDetailsOldDto();
        _mockPropertyRepository
            .Setup(x => x.AddFloorDetailsOldAsync(propertyId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.AddFloorDetailsOldAsync(propertyId, dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region UpdateFloorDetailsOldAsync Tests

    [Fact]
    public async Task UpdateFloorDetailsOldAsync_UpdatesAndReturnsFloorDetailsOld()
    {
        // Arrange
        var propertyId = 1;
        var floorId = 2;
        var dto = new UpdatePropertyDetailsOldDto();
        var expectedDto = new PropertyDetailsOldDto();
        _mockPropertyRepository
            .Setup(x => x.UpdateFloorDetailsOldAsync(propertyId, floorId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.UpdateFloorDetailsOldAsync(propertyId, floorId, dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region DeleteFloorDetailsOldAsync Tests

    [Fact]
    public async Task DeleteFloorDetailsOldAsync_DeletesFloorDetailsOld()
    {
        // Arrange
        var propertyId = 1;
        var floorId = 2;
        _mockPropertyRepository
            .Setup(x => x.DeleteFloorDetailsOldAsync(propertyId, floorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteFloorDetailsOldAsync(propertyId, floorId);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region GetAggregatedPropertyTaxDetailsAsync Tests

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsAsync_ReturnsApartmentPropertyTaxDetails()
    {
        // Arrange
        var dto = new PropertyApartmentTaxRequestDto();
        var expectedDto = new PropertyTaxApartmentDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.GetAggregatedPropertyTaxDetailsAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetAggregatedPropertyTaxDetailsAsync(dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetAggregatedPropertyTaxDetailsCVAsync Tests

    [Fact]
    public async Task GetAggregatedPropertyTaxDetailsCVAsync_ReturnsApartmentPropertyTaxDetailsCV()
    {
        // Arrange
        var dto = new PropertyApartmentTaxRequestDto();
        var expectedDto = new PropertyTaxApartmentDetailsCVDto();
        _mockPropertyRepository
            .Setup(x => x.GetAggregatedPropertyTaxDetailsCVAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetAggregatedPropertyTaxDetailsCVAsync(dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetGenerateBuildingStructureAsync Tests

    [Fact]
    public async Task GetGenerateBuildingStructureAsync_ReturnsGenerateBuildingStructure()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto();
        var expectedList = new List<BuildingGenerateStructureDto>();
        _mockPropertyRepository
            .Setup(x => x.GetGenerateBuildingStructureAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        // Act
        var result = await _service.GetGenerateBuildingStructureAsync(dto);

        // Assert
        Assert.Equal(expectedList, result);
    }

    #endregion

    #region CreatePropertiesFromRangeAsync Tests

    /// <summary>
    /// Helper to setup mocks for range creation tests.
    /// Mocks AutoMapper, repositories (ward, category, assessment), and UnitOfWork 
    /// to simulate the service-layer property creation flow.
    /// </summary>
    private void SetupRangeCreationMocks(
        WardEntity? ward = null,
        PropertyCategoryEntity? category = null,
        bool propertyExists = false)
    {
        // Setup IMapper for PropertyEntity
        var propertyIdCounter = 100;
        _mockMapper.Setup(m => m.Map<PropertyEntity>(It.IsAny<CreateNewPropertyDto>()))
            .Returns(() => new PropertyEntity { Id = propertyIdCounter++, WardId = 1 });

        // Setup IMapper for PropertyAssessmentEntity
        _mockMapper.Setup(m => m.Map<PropertyAssessmentEntity>(It.IsAny<CreateNewPropertyDto>()))
            .Returns(new PropertyAssessmentEntity());

        // Setup ward repository
        _mockWardRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ward ?? new WardEntity { Id = 1, WardNo = "W01" });

        // Setup category repository
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(category ?? new PropertyCategoryEntity { Id = 1, PropertyCategoryName = "Residential" });

        // Setup IsPropertyExists
        _mockPropertyRepository.Setup(r => r.IsPropertyExists(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(propertyExists);

        // Setup UnitOfWork
        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.CreatePropertiesFromRangeAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithNullTemplate_ReturnsError()
    {
        // Arrange
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = null,
            RangeFrom = "1",
            RangeTo = "3"
        };

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains("Template cannot be null.", result.Errors);
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithValidRequest_CreatesProperties()
    {
        // Arrange
        SetupRangeCreationMocks();
        var template = new CreateNewPropertyDto { WardId = 1, CategoryId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2",
            Prefix = null,
            Suffix = null,
            StartSequenceNo = 1
        };

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithCancellationRequested_RollsBackAndReturnsError()
    {
        // Arrange
        SetupRangeCreationMocks();
        var template = new CreateNewPropertyDto { WardId = 1, CategoryId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, cts.Token);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Operation cancelled"));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithEmptyRangeValue_ThrowsArgumentException()
    {
        // Arrange
        var template = new CreateNewPropertyDto { WardId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "",
            RangeTo = ""
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithDuplicateProperty_RollsBackAndReturnsError()
    {
        // Arrange — all properties exist
        SetupRangeCreationMocks(propertyExists: true);
        var template = new CreateNewPropertyDto { WardId = 1, CategoryId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(2, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors!, e => e.Contains("Property already exists"));
        _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithDbUpdateException_ReturnsError()
    {
        // Arrange
        SetupRangeCreationMocks();
        // Override AddAsync to throw
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Database error"));

        var template = new CreateNewPropertyDto { WardId = 1, CategoryId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Database error"));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithOperationCanceledException_ReturnsError()
    {
        // Arrange
        SetupRangeCreationMocks();
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Operation cancelled"));

        var template = new CreateNewPropertyDto { WardId = 1, CategoryId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Operation cancelled"));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithArgumentException_ReturnsError()
    {
        // Arrange
        SetupRangeCreationMocks();
        _mockMapper.Setup(m => m.Map<PropertyEntity>(It.IsAny<CreateNewPropertyDto>()))
            .Throws(new ArgumentException("Invalid argument"));

        var template = new CreateNewPropertyDto { WardId = 1, CategoryId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Invalid argument"));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithGenericException_ReturnsError()
    {
        // Arrange
        SetupRangeCreationMocks();
        _mockMapper.Setup(m => m.Map<PropertyEntity>(It.IsAny<CreateNewPropertyDto>()))
            .Throws(new InvalidOperationException("Invalid operation"));

        var template = new CreateNewPropertyDto { WardId = 1, CategoryId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Invalid operation"));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithRollbackFailure_IncludesRollbackError()
    {
        // Arrange
        SetupRangeCreationMocks();
        _mockMapper.Setup(m => m.Map<PropertyEntity>(It.IsAny<CreateNewPropertyDto>()))
            .Throws(new InvalidOperationException("Test error"));

        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Rollback failed"));

        var template = new CreateNewPropertyDto { WardId = 1, CategoryId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Rollback error"));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithPropertyExists_IncludesProperMessage()
    {
        // Arrange — duplicate detection via IsPropertyExists
        SetupRangeCreationMocks(propertyExists: true);
        var template = new CreateNewPropertyDto { WardId = 1, CategoryId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
    }

    #endregion


    #region BulkCreateAsync Tests

    // Helper method to setup common mocks for BulkCreateAsync tests
    private void SetupBulkCreateMocks(CreateBulkPropertyDto[] items, string categoryName = "Residential")
    {
        var buildingEntity = new PropertyEntity { Id = 1, WardId = items[0].WardId, Address = "Test Address", AddressEnglish = "Test Address English", Location = "Test Location", LocationEnglish = "Test Location English", PropertySeqNo = 1 };
        var categoryEntity = new PropertyCategoryEntity { Id = items[0].CategoryId, PropertyCategoryName = categoryName };

        _mockPropertyRepository
            .Setup(x => x.CheckBuildingIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingEntity);

        _mockPropertyRepository
            .Setup(x => x.GetBuildingCategory(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoryEntity);

        _mockPropertyRepository
            .Setup(x => x.CheckPropertyIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockPropertyRepository
            .Setup(x => x.CheckPropertyFlatIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task BulkCreateAsync_WithEmptyArray_ReturnsEmptyResult()
    {
        // Arrange
        var items = Array.Empty<CreateBulkPropertyDto>();

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Results);
    }

    [Fact]
    public async Task BulkCreateAsync_WithValidItems_ReturnsSuccessResult()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", PartitionNo = "A1", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 },
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", PartitionNo = "A2", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        SetupBulkCreateMocks(items);

        var response1 = new CreateBulkPropertyResponseDto { Success = true, PropertyId = 1 };
        var response2 = new CreateBulkPropertyResponseDto { Success = true, PropertyId = 2 };

        _mockPropertyRepository
            .SetupSequence(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response1)
            .ReturnsAsync(response2);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(2, result.Results.Count);
        Assert.True(result.AllSucceeded);
        Assert.False(result.HasFailures);
    }

    [Fact]
    public async Task BulkCreateAsync_WithBuildingNotFound_ReturnsError()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        _mockPropertyRepository
            .Setup(x => x.CheckBuildingIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyEntity?)null);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Building Not Found"));
    }

    [Fact]
    public async Task BulkCreateAsync_WithInvalidCategory_ReturnsError()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 999 }
        };

        var buildingEntity = new PropertyEntity { Id = 1, WardId = 1 };

        _mockPropertyRepository
            .Setup(x => x.CheckBuildingIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingEntity);

        _mockPropertyRepository
            .Setup(x => x.GetBuildingCategory(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyCategoryEntity?)null);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Invalid CategoryId"));
    }

    [Fact]
    public async Task BulkCreateAsync_WithApartmentCategoryWithoutSocietyDetailId_ReturnsError()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1, SocietyDetailId = null }
        };

        var buildingEntity = new PropertyEntity { Id = 1, WardId = 1 };
        var categoryEntity = new PropertyCategoryEntity { Id = 1, PropertyCategoryName = "Apartment" };

        _mockPropertyRepository
            .Setup(x => x.CheckBuildingIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingEntity);

        _mockPropertyRepository
            .Setup(x => x.GetBuildingCategory(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoryEntity);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Society Wing Details"));
    }

    [Fact]
    public async Task BulkCreateAsync_WithExistingPropertyAndFlat_ReturnsError()
    {
        // Arrange - Both property partition AND flat exist
        // Note: Due to current implementation, property-only duplicates are not caught early,
        // so we test with both property and flat existing
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", PartitionNo = "A1", FlatOrShopNo = "101", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        var buildingEntity = new PropertyEntity { Id = 1, WardId = 1 };
        var categoryEntity = new PropertyCategoryEntity { Id = 1, PropertyCategoryName = "Residential" };

        _mockPropertyRepository
            .Setup(x => x.CheckBuildingIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingEntity);

        _mockPropertyRepository
            .Setup(x => x.GetBuildingCategory(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoryEntity);

        _mockPropertyRepository
            .Setup(x => x.CheckPropertyIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockPropertyRepository
            .Setup(x => x.CheckPropertyFlatIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(1, result.FailedCount);
            Assert.NotNull(result.Errors);
            Assert.Contains(result.Errors, e => e.Contains("this property flat already exists in this wing"));
        }

        [Fact]
        public async Task BulkCreateAsync_WithExistingFlat_ReturnsError()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", PartitionNo = "A1", FlatOrShopNo = "101", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        var buildingEntity = new PropertyEntity { Id = 1, WardId = 1 };
        var categoryEntity = new PropertyCategoryEntity { Id = 1, PropertyCategoryName = "Residential" };

        _mockPropertyRepository
            .Setup(x => x.CheckBuildingIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingEntity);

        _mockPropertyRepository
            .Setup(x => x.GetBuildingCategory(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoryEntity);

        _mockPropertyRepository
            .Setup(x => x.CheckPropertyIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockPropertyRepository
            .Setup(x => x.CheckPropertyFlatIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(1, result.FailedCount);
            Assert.NotNull(result.Errors);
            Assert.Contains(result.Errors, e => e.Contains("this property flat already exists in this wing"));
        }

        [Fact]
        public async Task BulkCreateAsync_WithFailedRepositoryResponse_RollsBackAndReturnsError()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        SetupBulkCreateMocks(items);

        var response = new CreateBulkPropertyResponseDto { Success = false, Message = "Property creation failed" };

        _mockPropertyRepository
            .Setup(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Property creation failed"));
        _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreateAsync_WithNullRepositoryResponse_RollsBackAndReturnsError()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        SetupBulkCreateMocks(items);

        _mockPropertyRepository
            .Setup(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateBulkPropertyResponseDto?)null);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.NotNull(result.Errors);
        _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreateAsync_WithException_RollsBackAndReturnsTransactionError()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        SetupBulkCreateMocks(items);

        _mockPropertyRepository
            .Setup(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Transaction failed"));
        _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreateAsync_WithPartialSuccess_RollsBackOnFirstFailure()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", PartitionNo = "A1", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 },
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", PartitionNo = "A2", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        SetupBulkCreateMocks(items);

        var successResponse = new CreateBulkPropertyResponseDto { Success = true, PropertyId = 1 };
        var failureResponse = new CreateBulkPropertyResponseDto { Success = false, Message = "Duplicate property" };

        _mockPropertyRepository
            .SetupSequence(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResponse)
            .ReturnsAsync(failureResponse);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(2, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate property"));
        _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkCreateAsync_WithSingleItem_ReturnsSuccessResult()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        SetupBulkCreateMocks(items);

        var response = new CreateBulkPropertyResponseDto { Success = true, PropertyId = 1 };

        _mockPropertyRepository
            .Setup(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Single(result.Results);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreateAsync_WithMultipleItems_CommitsOnAllSuccess()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", PartitionNo = "A1", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 },
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", PartitionNo = "A2", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 },
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", PartitionNo = "A3", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        SetupBulkCreateMocks(items);

        var response = new CreateBulkPropertyResponseDto { Success = true, PropertyId = 1 };

        _mockPropertyRepository
            .Setup(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(3, result.Results.Count);
        Assert.Null(result.Errors);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkCreateAsync_FailureResponseWithNullMessage_HandlesGracefully()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        SetupBulkCreateMocks(items);

        var response = new CreateBulkPropertyResponseDto { Success = false, Message = null };

        _mockPropertyRepository
            .Setup(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Unknown error"));
    }

    [Fact]
    public async Task BulkCreateAsync_WithPlotCategory_SetsOpenPlotTrue()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        var buildingEntity = new PropertyEntity { Id = 1, WardId = 1, Address = "Test", AddressEnglish = "Test", Location = "Test", LocationEnglish = "Test", PropertySeqNo = 1 };
        var categoryEntity = new PropertyCategoryEntity { Id = 1, PropertyCategoryName = "Plot" };

        _mockPropertyRepository
            .Setup(x => x.CheckBuildingIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingEntity);

        _mockPropertyRepository
            .Setup(x => x.GetBuildingCategory(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoryEntity);

        _mockPropertyRepository
            .Setup(x => x.CheckPropertyIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockPropertyRepository
            .Setup(x => x.CheckPropertyFlatIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var response = new CreateBulkPropertyResponseDto { Success = true, PropertyId = 1 };

        _mockPropertyRepository
            .Setup(x => x.CreateBulkPropertyAsync(It.Is<CreateBulkPropertyDto>(dto => dto.OpenPlot == true), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.SuccessCount);
        _mockPropertyRepository.Verify(x => x.CreateBulkPropertyAsync(It.Is<CreateBulkPropertyDto>(dto => dto.OpenPlot == true), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreateAsync_WithApartmentCategoryAndSocietyDetailId_ReturnsSuccess()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1, SocietyDetailId = 5 }
        };

        var buildingEntity = new PropertyEntity { Id = 1, WardId = 1, Address = "Test", AddressEnglish = "Test", Location = "Test", LocationEnglish = "Test", PropertySeqNo = 1 };
        var categoryEntity = new PropertyCategoryEntity { Id = 1, PropertyCategoryName = "Apartment" };

        _mockPropertyRepository
            .Setup(x => x.CheckBuildingIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingEntity);

        _mockPropertyRepository
            .Setup(x => x.GetBuildingCategory(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoryEntity);

        _mockPropertyRepository
            .Setup(x => x.CheckPropertyIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockPropertyRepository
            .Setup(x => x.CheckPropertyFlatIfExists(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var response = new CreateBulkPropertyResponseDto { Success = true, PropertyId = 1 };

        _mockPropertyRepository
            .Setup(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
    }

    #endregion

    #region SearchPropertiesAsync Tests

    [Fact]
    public async Task SearchPropertiesAsync_WithValidParameters_ReturnsResults()
    {
        // Arrange
        var queryParameters = new PropertySearchQueryParameters
        {
            ZoneId = 1,
            WardId = 2,
            PageNumber = 1,
            PageSize = 10
        };

        var expectedTuple = (
            TotalCount: 2,
            Items: new List<PropertySearchResponseDto>
            {
                new PropertySearchResponseDto { PropertyId = 1, PropertyNo = "001" },
                new PropertySearchResponseDto { PropertyId = 2, PropertyNo = "002" }
            }
        );

        _mockPropertyRepository
            .Setup(x => x.SearchPropertiesAsync(
                It.IsAny<PropertySearchRequestDto>(),
                queryParameters.PageNumber,
                queryParameters.PageSize,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTuple);

        // Act
        var result = await _service.SearchPropertiesAsync(queryParameters, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task SearchPropertiesAsync_WithPropertyNoFromOnly_PassesToRepository()
    {
        // Arrange
        var queryParameters = new PropertySearchQueryParameters
        {
            PropertyNoFrom = "050",
            PageNumber = 1,
            PageSize = 10
        };

        var expectedTuple = (
            TotalCount: 5,
            Items: new List<PropertySearchResponseDto>
            {
                new PropertySearchResponseDto { PropertyId = 1, PropertyNo = "050" }
            }
        );

        _mockPropertyRepository
            .Setup(x => x.SearchPropertiesAsync(
                It.Is<PropertySearchRequestDto>(req => req.PropertyNoFrom == "050" && req.PropertyNoTo == null),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTuple);

        // Act
        var result = await _service.SearchPropertiesAsync(queryParameters, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        _mockPropertyRepository.Verify(
            x => x.SearchPropertiesAsync(
                It.Is<PropertySearchRequestDto>(req => req.PropertyNoFrom == "050" && req.PropertyNoTo == null),
                1,
                10,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchPropertiesAsync_WithPropertyNoToOnly_PassesToRepository()
    {
        // Arrange
        var queryParameters = new PropertySearchQueryParameters
        {
            PropertyNoTo = "100",
            PageNumber = 1,
            PageSize = 10
        };

        var expectedTuple = (
            TotalCount: 3,
            Items: new List<PropertySearchResponseDto>
            {
                new PropertySearchResponseDto { PropertyId = 1, PropertyNo = "050" }
            }
        );

        _mockPropertyRepository
            .Setup(x => x.SearchPropertiesAsync(
                It.Is<PropertySearchRequestDto>(req => req.PropertyNoFrom == null && req.PropertyNoTo == "100"),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTuple);

        // Act
        var result = await _service.SearchPropertiesAsync(queryParameters, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        _mockPropertyRepository.Verify(
            x => x.SearchPropertiesAsync(
                It.Is<PropertySearchRequestDto>(req => req.PropertyNoFrom == null && req.PropertyNoTo == "100"),
                1,
                10,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchPropertiesAsync_WithPageSizeMinusOne_PassesToRepository()
    {
        // Arrange
        var queryParameters = new PropertySearchQueryParameters
        {
            PageNumber = 1,
            PageSize = -1
        };

        var expectedTuple = (
            TotalCount: 100,
            Items: Enumerable.Range(1, 100)
                .Select(i => new PropertySearchResponseDto { PropertyId = i })
                .ToList()
        );

        _mockPropertyRepository
            .Setup(x => x.SearchPropertiesAsync(
                It.IsAny<PropertySearchRequestDto>(),
                1,
                -1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTuple);

        // Act
        var result = await _service.SearchPropertiesAsync(queryParameters, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.TotalCount);
        Assert.Equal(100, result.Items.Count());
        _mockPropertyRepository.Verify(
            x => x.SearchPropertiesAsync(
                It.IsAny<PropertySearchRequestDto>(),
                1,
                -1,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchPropertiesAsync_WithOutOfRangePage_ReturnsEmptyResults()
    {
        // Arrange
        var queryParameters = new PropertySearchQueryParameters
        {
            PageNumber = 100,
            PageSize = 10
        };

        var expectedTuple = (
            TotalCount: 50,
            Items: new List<PropertySearchResponseDto>()
        );

        _mockPropertyRepository
            .Setup(x => x.SearchPropertiesAsync(
                It.IsAny<PropertySearchRequestDto>(),
                100,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTuple);

        // Act
        var result = await _service.SearchPropertiesAsync(queryParameters, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchPropertiesAsync_WithNoResults_ReturnsEmptyPagedResult()
    {
        // Arrange
        var queryParameters = new PropertySearchQueryParameters
        {
            ZoneId = 999,
            PageNumber = 1,
            PageSize = 10
        };

        var expectedTuple = (
            TotalCount: 0,
            Items: new List<PropertySearchResponseDto>()
        );

        _mockPropertyRepository
            .Setup(x => x.SearchPropertiesAsync(
                It.IsAny<PropertySearchRequestDto>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTuple);

        // Act
        var result = await _service.SearchPropertiesAsync(queryParameters, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchPropertiesAsync_WithAllFilters_PassesAllToRepository()
    {
        // Arrange
        var queryParameters = new PropertySearchQueryParameters
        {
            ZoneId = 1,
            WardId = 2,
            PropertyNoFrom = "001",
            PropertyNoTo = "100",
            CategoryId = 3,
            PropertyTypeId = 4,
            TypeOfUseId = 5,
            OldPropertyNo = "OLD-001",
            UPICId = "UPIC-001",
            CSN = "CSN-001",
            SubZoneNo = "SUB-001",
            PlotNo = "PLOT-001",
            PropertyAssessmentStatusId = 6,
            MobileNo = "1234567890",
            OwnerName = "John Doe",
            OccupierName = "Jane Doe",
            FlatOrShopName = "Shop A",
            SocietyName = "Society ABC",
            Address = "123 Main St",
            PageNumber = 1,
            PageSize = 10
        };

        var expectedTuple = (
            TotalCount: 1,
            Items: new List<PropertySearchResponseDto>
            {
                new PropertySearchResponseDto { PropertyId = 1 }
            }
        );

        _mockPropertyRepository
            .Setup(x => x.SearchPropertiesAsync(
                It.Is<PropertySearchRequestDto>(req =>
                    req.ZoneId == 1 &&
                    req.WardId == 2 &&
                    req.PropertyNoFrom == "001" &&
                    req.PropertyNoTo == "100" &&
                    req.CategoryId == 3 &&
                    req.PropertyTypeId == 4 &&
                    req.TypeOfUseId == 5 &&
                    req.OldPropertyNo == "OLD-001" &&
                    req.UPICId == "UPIC-001" &&
                    req.CSN == "CSN-001" &&
                    req.SubZoneNo == "SUB-001" &&
                    req.PlotNo == "PLOT-001" &&
                    req.PropertyAssessmentStatusId == 6 &&
                    req.MobileNo == "1234567890" &&
                    req.OwnerName == "John Doe" &&
                    req.OccupierName == "Jane Doe" &&
                    req.FlatOrShopName == "Shop A" &&
                    req.SocietyName == "Society ABC" &&
                    req.Address == "123 Main St"),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTuple);

        // Act
        var result = await _service.SearchPropertiesAsync(queryParameters, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        _mockPropertyRepository.Verify(
            x => x.SearchPropertiesAsync(
                It.Is<PropertySearchRequestDto>(req =>
                    req.ZoneId == 1 &&
                    req.PropertyNoFrom == "001" &&
                    req.PropertyNoTo == "100"),
                1,
                10,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetPropertyDashboardStatsAsync Tests

    [Fact]
    public async Task GetPropertyDashboardStatsAsync_ReturnsStats()
    {
        // Arrange
        var expectedStats = new PropertyDashboardStatsDto
        {
            RegisteredPropertyCount = 100,
            GeoSequencingPropertyCount = 100,
            SurveyPropertyCount = 0,
            DataProcessingPropertyCount = 0,
            QualityAnalysisPropertyCount = 0,
            AssessmentCompletedPropertyCount = 0
        };

        _mockPropertyRepository
            .Setup(x => x.GetPropertyDashboardStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _service.GetPropertyDashboardStatsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.RegisteredPropertyCount);
        Assert.Equal(100, result.GeoSequencingPropertyCount);
        Assert.Equal(0, result.SurveyPropertyCount);
        Assert.Equal(0, result.DataProcessingPropertyCount);
        Assert.Equal(0, result.QualityAnalysisPropertyCount);
        Assert.Equal(0, result.AssessmentCompletedPropertyCount);
        _mockPropertyRepository.Verify(
            x => x.GetPropertyDashboardStatsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPropertyDashboardStatsAsync_WithZeroCounts_ReturnsZeros()
    {
        // Arrange
        var expectedStats = new PropertyDashboardStatsDto
        {
            RegisteredPropertyCount = 0,
            GeoSequencingPropertyCount = 0,
            SurveyPropertyCount = 0,
            DataProcessingPropertyCount = 0,
            QualityAnalysisPropertyCount = 0,
            AssessmentCompletedPropertyCount = 0
        };

        _mockPropertyRepository
            .Setup(x => x.GetPropertyDashboardStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _service.GetPropertyDashboardStatsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.RegisteredPropertyCount);
        Assert.Equal(0, result.GeoSequencingPropertyCount);
        Assert.Equal(0, result.SurveyPropertyCount);
        Assert.Equal(0, result.DataProcessingPropertyCount);
        Assert.Equal(0, result.QualityAnalysisPropertyCount);
        Assert.Equal(0, result.AssessmentCompletedPropertyCount);
    }

    [Fact]
    public async Task GetPropertyDashboardStatsAsync_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var expectedStats = new PropertyDashboardStatsDto();

        _mockPropertyRepository
            .Setup(x => x.GetPropertyDashboardStatsAsync(token))
            .ReturnsAsync(expectedStats);

        // Act
        await _service.GetPropertyDashboardStatsAsync(token);

        // Assert
        _mockPropertyRepository.Verify(
            x => x.GetPropertyDashboardStatsAsync(token),
            Times.Once);
    }

    #endregion
}

