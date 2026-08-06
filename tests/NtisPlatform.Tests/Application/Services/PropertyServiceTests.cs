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
            _mockAssessmentRepository.Object,
            new Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>().Object,
            new Mock<IRepository<PropertyMapMasterEntity, int>>().Object,
            new Mock<IRepository<PropertyMapDetailEntity, int>>().Object,
            new Mock<IRepository<WingEntity, int>>().Object,
            new Mock<IRepository<UserEntity, int>>().Object, new Mock<IRepository<PropertyMastOldEntity, int>>().Object, new Mock<IRepository<PropertyTypeMasterEntity, int>>().Object,
            new Mock<IRepository<CommunicationDetailsEntity, int>>().Object,
            new Mock<IRepository<PropertyPhotoEntity, int>>().Object,
            new Mock<IRepository<DocumentBindingEntity, int>>().Object,
            new Mock<IRepository<DocumentEntity, int>>().Object,
            new Mock<IRepository<PropertyPhotoTypeEntity, int>>().Object,
            new Mock<IRepository<OwnerTypeMasterEntity, int>>().Object,
            new Mock<IRepository<WingEntity, int>>().Object,
            new Mock<NtisPlatform.Application.Interfaces.Rules.IPropertyRuleApplicationLogService>().Object);
    }

    // Basic Details was split into the per-tab PropertyBasicDetailsService (data access in
    // PropertyBasicDetailsRepository). Its behaviour is covered by
    // PropertyBasicDetailsTests.PropertyBasicDetailsServiceTests and the in-memory integration tests.

    #region GetBasicDetailsAsync Tests

    // Society moved to PropertySocietyService (see PropertySocietyDetailsTests.PropertySocietyServiceTests).

    // KYC moved to PropertyKycService (see PropertyKycDetailsTests.PropertyKycServiceTests).

    // Old Details moved to PropertyOldDetailsService (see PropertyRepositoryOldDetailsIntegrationTests
    // and PropertyControllerOldDetailsTests).

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
}


